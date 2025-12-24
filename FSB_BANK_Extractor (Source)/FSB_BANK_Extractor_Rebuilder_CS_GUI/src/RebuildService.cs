/**
 * @file RebuildService.cs
 * @brief Provides core logic for rebuilding FMOD Sound Bank (.fsb) containers.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This service class orchestrates the entire FSB rebuilding process. It manages a temporary
 * workspace, extracts original audio data, replaces specified files, and integrates with the
 * external 'fsbankcl.exe' tool to generate a new FSB file.
 *
 * Key Features:
 *  - Comprehensive Logging: Records all operational steps (Prepare, Extract, Build, Patch) to the log file.
 *  - Memory Optimization: Uses stream-based processing to handle large files (GB+) without high RAM usage.
 *  - Multi-FSB Support: Correctly handles .bank files containing multiple concatenated FSB containers.
 *  - Binary Search: Optimizes Vorbis quality by finding the best value that fits within the original file size.
 *  - Parallel Processing: Utilizes multi-threading with oversubscription to maximize CPU and Disk I/O usage.
 *  - Process Management: Tracks and manages the external fsbankcl.exe process lifecycle.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Key Dependencies: Newtonsoft.Json, FMOD Core API
 *  - Last Update: 2025-12-24
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FMOD; // Core API
using Newtonsoft.Json; // Required for manifest generation.

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Provides services for rebuilding FSB containers and managing external build tools.
    /// </summary>
    public class RebuildService
    {
        #region 1. Constants and Fields

        // File and Directory Names.
        private const string TEMP_ROOT_FOLDER_NAME = "FsbRebuildTool";
        private const string AUDIO_SOURCE_FOLDER_NAME = "AudioSource";
        private const string SOURCE_FSB_FILE_NAME = "source.fsb";
        private const string REBUILT_FSB_FILE_NAME = "rebuilt.fsb";
        private const string MANIFEST_FILE_NAME = "manifest.json";
        private const string BUILD_LIST_FILE_NAME = "buildlist.txt";
        private const string EXTENSION_TEMP = ".tmp";
        private const string EXTENSION_GOOD = ".good";

        // Configuration Constants.
        /// <summary>
        /// Multiplier for thread allocation to allow oversubscription in I/O bound tasks.
        /// </summary>
        private const int THREAD_MULTIPLIER = 4;

        /// <summary>
        /// Maximum number of iterations for the binary search quality optimization.
        /// </summary>
        private const int BINARY_SEARCH_MAX_ATTEMPTS = 8;

        /// <summary>
        /// Minimum interval in milliseconds between UI progress updates to prevent freezing.
        /// </summary>
        private const long UI_THROTTLE_INTERVAL_MS = 33;

        /// <summary>
        /// Minimum byte size required to parse an FSB header.
        /// </summary>
        private const int MIN_FSB_HEADER_SIZE = 24;

        // Audio Processing Constants.
        private const int MAX_NAME_LENGTH = 256;
        private const int MIN_SAMPLE_RATE = 100;
        private const float DEFAULT_SAMPLE_RATE = 44100f;
        private const int DEFAULT_BIT_DEPTH = 16;

        // Progress Calculation Weights & Offsets.
        private const double PROGRESS_WEIGHT_PREPARE = 0.30;
        private const double PROGRESS_WEIGHT_BUILD = 0.60;
        private const int PROGRESS_OFFSET_BUILD = 30;
        private const int PROGRESS_OFFSET_PATCH = 95;
        private const int PROGRESS_OFFSET_CLEANUP = 99;

        // Binary Signatures.
        /// <summary>
        /// The "FSB5" signature bytes used for scanning stream boundaries.
        /// </summary>
        private static readonly byte[] FSB5_SIGNATURE_BYTES = { 0x46, 0x53, 0x42, 0x35 };

        // FMOD System references.
        private readonly FMOD.System _coreSystem;
        private readonly object _coreSystemLock;
        private readonly ExtractionService _extractionService;

        /// <summary>
        /// Tracks the currently running external process (fsbankcl.exe) to allow forced termination.
        /// </summary>
        private volatile Process _activeChildProcess;

        /// <summary>
        /// Occurs when a log line is received from the external fsbankcl process or internal status updates.
        /// </summary>
        public event Action<string> OnLogReceived;

        #endregion

        #region 2. Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="RebuildService"/> class.
        /// </summary>
        /// <param name="coreSystem">The FMOD Core System instance.</param>
        /// <param name="syncLock">The synchronization lock for FMOD operations.</param>
        /// <param name="extractionService">The service used for audio extraction.</param>
        public RebuildService(FMOD.System coreSystem, object syncLock, ExtractionService extractionService)
        {
            _coreSystem = coreSystem;
            _coreSystemLock = syncLock;
            _extractionService = extractionService;
        }

        #endregion

        #region 3. Public Methods

        /// <summary>
        /// Forces the termination of the currently active child process (fsbankcl.exe), if any.
        /// </summary>
        /// <remarks>
        /// This method is primarily used during application shutdown to ensure no orphaned processes remain.
        /// </remarks>
        public void ForceKillChildProcess()
        {
            try
            {
                // Capture the reference locally to avoid race conditions.
                var proc = _activeChildProcess;
                if (proc != null && !proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch
            {
                // Silently ignore errors (e.g., access denied or process already exited).
            }
        }

        /// <summary>
        /// Asynchronously rebuilds the FSB container by replacing specified audio files.
        /// </summary>
        /// <param name="targetNode">The audio data node representing the target FSB container.</param>
        /// <param name="batchReplacements">A list of batch items containing replacement details.</param>
        /// <param name="finalSavePath">The full path where the rebuilt file will be saved.</param>
        /// <param name="options">Configuration options for the rebuild process.</param>
        /// <param name="progress">An object to report progress updates to the UI. Can be null.</param>
        /// <param name="forceOversize">If set to <c>true</c>, proceeds even if the file size exceeds the original.</param>
        /// <param name="previousResult">The result of a previous attempt to allow workspace reuse.</param>
        /// <returns>A <see cref="RebuildResult"/> indicating success or failure.</returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Calculate the exact size of the original FSB chunk.
        ///  2) Initialize the temporary workspace and extract original audio assets.
        ///  3) Run the build tool with binary search optimization.
        ///  4) Patch the newly built FSB data back into the final container file.
        ///  5) Finalize the operation and perform cleanup.
        /// </remarks>
        public async Task<RebuildResult> RebuildAsync(
            AudioDataNode targetNode,
            List<BatchItem> batchReplacements,
            string finalSavePath,
            RebuildOptions options,
            IProgress<ProgressReport> progress,
            bool forceOversize = false,
            RebuildResult previousResult = null)
        {
            string workspacePath = previousResult?.WorkspacePath;

            // Local helper function to broadcast logs and update UI simultaneously.
            // This ensures every status update is recorded in the log file with a timestamp.
            void LogAndReport(string status, int percentage)
            {
                // Format the log entry with a timestamp to match the user's expected format.
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | [STATUS] {status}";
                OnLogReceived?.Invoke(logEntry);

                // Update the UI via the progress interface.
                progress?.Report(new ProgressReport(status, percentage));
            }

            try
            {
                string rebuiltFsbPath;
                long originalFsbSize = 0;

                // Step 1: Calculate the exact size of the original FSB chunk.
                // We perform this first to ensure we have a valid target size for optimization.
                using (var fs = new FileStream(targetNode.CachedAudio.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, AppConstants.BufferSizeSmall, true))
                {
                    originalFsbSize = await CalculateFsbLengthAsync(fs, targetNode.FsbChunkOffset);
                }

                if (previousResult == null || string.IsNullOrEmpty(previousResult.TemporaryFsbPath))
                {
                    LogAndReport("[1/4 PREPARING] Creating temporary workspace...", 0);

                    // Create a specialized progress handler for the preparation phase.
                    // This handler wraps the main logging logic to capture internal steps of SetupWorkspaceAsync.
                    var prepareProgressHandler = new Progress<ProgressReport>(report =>
                    {
                        string phaseStatus = $"[1/4 PREPARING] {report.Status}";
                        int overallProgress = (int)(report.Percentage * PROGRESS_WEIGHT_PREPARE);
                        LogAndReport(phaseStatus, overallProgress);
                    });

                    // Step 2: Initialize the workspace and extract original audio assets.
                    // This involves reading the source FSB and extracting all WAVs to disk.
                    workspacePath = await SetupWorkspaceAsync(targetNode, prepareProgressHandler);

                    LogAndReport($"[1/4 PREPARING] Replacing {batchReplacements.Count} audio files in workspace...", PROGRESS_OFFSET_BUILD);
                    await ReplaceAudioInWorkspaceAsync(workspacePath, batchReplacements, options);

                    rebuiltFsbPath = Path.Combine(workspacePath, REBUILT_FSB_FILE_NAME);
                    string buildListPath = Path.Combine(workspacePath, BUILD_LIST_FILE_NAME);

                    // Explicitly call garbage collection to free memory before the heavy build process.
                    // Large byte arrays used during extraction can cause heap fragmentation if not cleared.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (originalFsbSize <= 0)
                    {
                        LogAndReport("[ERROR] Could not determine original FSB size.", PROGRESS_OFFSET_BUILD);
                        return new RebuildResult { Status = RebuildStatus.Failed, Message = "Could not determine original FSB size.", WorkspacePath = workspacePath };
                    }

                    // Create a specialized progress handler for the build phase.
                    var buildProgressHandler = new Progress<ProgressReport>(report =>
                    {
                        string phaseStatus = $"[2/4 BUILDING] {report.Status}";
                        int overallProgress = PROGRESS_OFFSET_BUILD + (int)((report.Percentage / 100.0) * (PROGRESS_WEIGHT_BUILD * 100));
                        LogAndReport(phaseStatus, overallProgress);
                    });

                    // Step 3: Run the build tool with binary search optimization.
                    // This process tries multiple quality settings to fit the FSB within the original size.
                    var buildResult = await RunFsBankClWithSizeModeAsync_BinarySearch(
                        buildListPath,
                        rebuiltFsbPath,
                        options,
                        originalFsbSize,
                        buildProgressHandler,
                        forceOversize
                    );

                    buildResult.WorkspacePath = workspacePath;

                    if (!buildResult.Success)
                    {
                        return buildResult;
                    }
                }
                else
                {
                    LogAndReport("[SKIPPED] Reusing previously built file.", PROGRESS_OFFSET_BUILD);
                    rebuiltFsbPath = previousResult.TemporaryFsbPath;
                    workspacePath = previousResult.WorkspacePath;
                }

                // Step 4: Patch the newly built FSB data back into the final container file.
                LogAndReport("[3/4 PATCHING] Writing new FSB data into the final file...", PROGRESS_OFFSET_PATCH);
                await PatchFileWithNewFsbAsync(targetNode, rebuiltFsbPath, finalSavePath);

                // Step 5: Finalize the operation and perform cleanup.
                LogAndReport("[4/4 CLEANUP] Finalizing operation...", PROGRESS_OFFSET_CLEANUP);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                return new RebuildResult { Status = RebuildStatus.Success, WorkspacePath = workspacePath };
            }
            catch (Exception ex)
            {
                LogAndReport($"[ERROR] {ex.Message}", 100);
                return new RebuildResult { Status = RebuildStatus.Failed, Message = ex.Message, WorkspacePath = workspacePath };
            }
        }

        #endregion

        #region 4. Private Rebuild Workflow Methods

        /// <summary>
        /// Prepares the temporary workspace directory and extracts all original sub-sounds using parallel processing.
        /// </summary>
        /// <param name="targetNode">The target audio node containing source file information.</param>
        /// <param name="progress">The progress reporter to update the UI.</param>
        /// <returns>The full path to the created workspace directory.</returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Initialize the workspace directory structure.
        ///  2) Extract the raw FSB chunk from the source container to a temp file.
        ///  3) Analyze the FSB and extract sub-sounds in parallel (WAV conversion).
        ///  4) Generate build configuration files (manifest.json, buildlist.txt).
        /// </remarks>
        private async Task<string> SetupWorkspaceAsync(AudioDataNode targetNode, IProgress<ProgressReport> progress)
        {
            var audioInfo = targetNode.CachedAudio;
            string sourcePath = audioInfo.SourcePath;
            long fsbOffset = targetNode.FsbChunkOffset;

            // Generate a unique workspace name based on the file name and offset.
            string workspaceName = Utilities.SanitizeFileName($"{Path.GetFileName(sourcePath)}_{fsbOffset}_workspace");
            string workspacePath = Path.Combine(Path.GetTempPath(), TEMP_ROOT_FOLDER_NAME, workspaceName);

            // Step 1: Initialize the workspace directory.
            progress?.Report(new ProgressReport("Initializing workspace...", 5));
            if (Directory.Exists(workspacePath))
            {
                // Use ConfigureAwait(false) to prevent blocking the UI thread.
                await Task.Run(() => Directory.Delete(workspacePath, true)).ConfigureAwait(false);
            }
            Directory.CreateDirectory(workspacePath);

            string audioSourcePath = Path.Combine(workspacePath, AUDIO_SOURCE_FOLDER_NAME);
            Directory.CreateDirectory(audioSourcePath);

            // Step 2: Extract the raw FSB chunk to a temporary file.
            progress?.Report(new ProgressReport("Reading source FSB data...", 10));
            string tempFsbPath = Path.Combine(workspacePath, SOURCE_FSB_FILE_NAME);

            using (var sourceFs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, AppConstants.BufferSizeSmall, true))
            using (var destFs = new FileStream(tempFsbPath, FileMode.Create, FileAccess.Write, FileShare.None, AppConstants.BufferSizeSmall, true))
            {
                long lengthToRead = await CalculateFsbLengthAsync(sourceFs, fsbOffset).ConfigureAwait(false);
                sourceFs.Seek(fsbOffset, SeekOrigin.Begin);

                byte[] buffer = new byte[AppConstants.BufferSizeXLarge];
                long totalCopied = 0;
                while (totalCopied < lengthToRead)
                {
                    int toRead = (int)Math.Min(buffer.Length, lengthToRead - totalCopied);

                    // Use ConfigureAwait(false) to enforce execution on the thread pool, avoiding UI freezes during heavy I/O.
                    int read = await sourceFs.ReadAsync(buffer, 0, toRead).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }
                    await destFs.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    totalCopied += read;
                }
            }

            // Step 3: Analyze FSB and extract sub-sounds in PARALLEL.
            // This task is CPU-intensive and benefits from offloading to a background thread.
            var buildData = await Task.Run(() =>
            {
                var finalManifest = new FsbManifest { SubSounds = new List<SubSoundManifestInfo>() };
                var finalPaths = new List<string>(); // Use thread-safe collection logic below.

                int totalNumSubSounds = 0;
                SOUND_TYPE buildType = SOUND_TYPE.UNKNOWN;

                // 3-1. Analyze structure (Single threaded first to get count/type).
                Sound analysisSound = new Sound();
                try
                {
                    lock (_coreSystemLock)
                    {
                        CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                        if (_coreSystem.createSound(tempFsbPath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS, ref ex, out analysisSound) == RESULT.OK)
                        {
                            analysisSound.getNumSubSounds(out totalNumSubSounds);
                            if (totalNumSubSounds > 0)
                            {
                                Sound firstSub = new Sound();
                                analysisSound.getSubSound(0, out firstSub);
                                firstSub.getFormat(out buildType, out _, out _, out _);
                                firstSub.release();
                            }
                        }
                    }
                }
                finally
                {
                    lock (_coreSystemLock)
                    {
                        if (analysisSound.hasHandle())
                        {
                            analysisSound.release();
                        }
                    }
                }

                finalManifest.BuildFormat = buildType;
                if (totalNumSubSounds == 0)
                {
                    return new { Manifest = finalManifest, Paths = finalPaths };
                }

                // 3-2. Prepare for Parallel Extraction.
                int processedCount = 0;
                var concurrentResults = new System.Collections.Concurrent.ConcurrentBag<(SubSoundManifestInfo Info, string Path)>();
                var partitioner = System.Collections.Concurrent.Partitioner.Create(0, totalNumSubSounds);

                // Configure parallelism multiplier.
                // Using a multiplier (Oversubscription) helps saturate I/O and CPU when individual tasks are blocked by latency.
                int maxParallelism = Environment.ProcessorCount * THREAD_MULTIPLIER;

                // Execute in parallel.
                Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, range =>
                {
                    // Each thread opens its own handle to the FSB file to allow concurrent access.
                    Sound threadLocalFsb = new Sound();
                    bool isThreadFsbLoaded = false;

                    try
                    {
                        // Lock only for creation.
                        lock (_coreSystemLock)
                        {
                            CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                            if (_coreSystem.createSound(tempFsbPath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS | MODE.ACCURATETIME, ref ex, out threadLocalFsb) == RESULT.OK)
                            {
                                isThreadFsbLoaded = true;
                            }
                        }

                        if (isThreadFsbLoaded)
                        {
                            // Loop through the assigned range for this thread.
                            for (int i = range.Item1; i < range.Item2; i++)
                            {
                                Sound subSound = new Sound();
                                try
                                {
                                    // Retrieve the sub-sound from the thread-local parent handle.
                                    // No global lock is needed here, allowing true parallel decoding.
                                    threadLocalFsb.getSubSound(i, out subSound);

                                    subSound.getLength(out uint lenBytes, TIMEUNIT.PCMBYTES);
                                    subSound.getFormat(out _, out SOUND_FORMAT fmt, out int ch, out int bits);
                                    subSound.getDefaults(out float rate, out _);
                                    subSound.getLoopPoints(out uint loopStart, TIMEUNIT.MS, out uint loopEnd, TIMEUNIT.MS);
                                    subSound.getMode(out MODE mode);
                                    subSound.getName(out string name, MAX_NAME_LENGTH);

                                    // Validate and correct sample rate if necessary.
                                    if (rate < MIN_SAMPLE_RATE)
                                    {
                                        rate = DEFAULT_SAMPLE_RATE;
                                    }

                                    string indexFolder = i.ToString("D3");
                                    string subDirectoryPath = Path.Combine(audioSourcePath, indexFolder);
                                    Directory.CreateDirectory(subDirectoryPath);

                                    string fileNameOnly = Utilities.SanitizeFileName($"{name}.wav");
                                    string fullWavPath = Path.Combine(subDirectoryPath, fileNameOnly);

                                    using (FileStream wavFs = new FileStream(fullWavPath, FileMode.Create, FileAccess.Write, FileShare.None, AppConstants.BufferSizeMedium))
                                    {
                                        int bitDepth = bits > 0 ? bits : DEFAULT_BIT_DEPTH;
                                        byte[] header = Utilities.CreateWavHeader((int)lenBytes, (int)rate, ch, bitDepth, fmt == SOUND_FORMAT.PCMFLOAT);
                                        wavFs.Write(header, 0, header.Length);

                                        subSound.seekData(0);
                                        byte[] buf = new byte[AppConstants.BufferSizeMedium];
                                        uint totalRead = 0;

                                        while (totalRead < lenBytes)
                                        {
                                            subSound.readData(buf, out uint read);
                                            if (read == 0)
                                            {
                                                break;
                                            }
                                            wavFs.Write(buf, 0, (int)read);
                                            totalRead += read;
                                        }
                                    }

                                    // Store the result safely.
                                    string relativePath = Path.Combine(indexFolder, fileNameOnly);
                                    concurrentResults.Add((new SubSoundManifestInfo
                                    {
                                        Index = i,
                                        Name = name,
                                        OriginalFileName = relativePath,
                                        Looping = (mode & MODE.LOOP_NORMAL) != 0,
                                        LoopStart = loopStart,
                                        LoopEnd = loopEnd,
                                    }, fullWavPath));

                                    // Update progress for every single file.
                                    int currentCount = System.Threading.Interlocked.Increment(ref processedCount);
                                    int subProgress = 15 + (int)(((float)currentCount / totalNumSubSounds) * 80);
                                    progress?.Report(new ProgressReport($"Extracting original sound {currentCount}/{totalNumSubSounds}...", subProgress));
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to extract subsound {i}: {ex.Message}");
                                }
                                finally
                                {
                                    if (subSound.hasHandle())
                                    {
                                        subSound.release();
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        lock (_coreSystemLock)
                        {
                            if (threadLocalFsb.hasHandle())
                            {
                                threadLocalFsb.release();
                            }
                        }
                    }
                });

                // Sort results by Index to maintain original order.
                var sortedResults = concurrentResults.OrderBy(r => r.Info.Index).ToList();
                finalManifest.SubSounds = sortedResults.Select(r => r.Info).ToList();
                finalPaths.AddRange(sortedResults.Select(r => r.Path));

                return new { Manifest = finalManifest, Paths = finalPaths };
            }).ConfigureAwait(false);

            // Step 4: Generate build configuration files.
            progress?.Report(new ProgressReport("Generating build files...", 95));

            string buildListFile = Path.Combine(workspacePath, BUILD_LIST_FILE_NAME);
            await Utilities.WriteAllTextAsync(buildListFile, string.Join(Environment.NewLine, buildData.Paths)).ConfigureAwait(false);

            string manifestPath = Path.Combine(workspacePath, MANIFEST_FILE_NAME);
            await Utilities.WriteAllTextAsync(manifestPath, JsonConvert.SerializeObject(buildData.Manifest, Formatting.Indented)).ConfigureAwait(false);

            progress?.Report(new ProgressReport("Workspace ready.", 100));
            return workspacePath;
        }

        /// <summary>
        /// Updates the manifest and replaces original audio files in the workspace with new ones.
        /// </summary>
        /// <param name="workspacePath">The path to the workspace directory.</param>
        /// <param name="replacements">A list of items to replace.</param>
        /// <param name="options">The rebuild configuration options.</param>
        private async Task ReplaceAudioInWorkspaceAsync(string workspacePath, List<BatchItem> replacements, RebuildOptions options)
        {
            string manifestPath = Path.Combine(workspacePath, MANIFEST_FILE_NAME);
            var manifestText = await Utilities.ReadAllTextAsync(manifestPath);
            var manifest = JsonConvert.DeserializeObject<FsbManifest>(manifestText);

            string audioSourcePath = Path.Combine(workspacePath, AUDIO_SOURCE_FOLDER_NAME);

            Sound newSound = new Sound();
            try
            {
                foreach (var item in replacements)
                {
                    var targetSubSound = manifest.SubSounds.FirstOrDefault(s => s.Index == item.TargetIndex);
                    if (targetSubSound == null)
                    {
                        continue;
                    }

                    string targetWavPath = Path.Combine(audioSourcePath, targetSubSound.OriginalFileName);

                    AudioInfo tempInfo;
                    lock (_coreSystemLock)
                    {
                        CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                        Utilities.CheckFmodResult(_coreSystem.createSound(item.NewFilePath, MODE.CREATESTREAM, ref exinfo, out newSound));
                        tempInfo = Utilities.GetAudioInfo(newSound, 0, item.NewFilePath, 0);
                        Utilities.SafeRelease(ref newSound);
                    }

                    await _extractionService.ExtractSingleWavAsync(tempInfo, targetWavPath);
                }
            }
            finally
            {
                lock (_coreSystemLock)
                {
                    Utilities.SafeRelease(ref newSound);
                }
            }

            manifest.BuildFormat = options.EncodingFormat;
            await Utilities.WriteAllTextAsync(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        /// <summary>
        /// Executes a binary search for the optimal encoding quality that fits within the target size.
        /// This optimized version reuses the best successful build from the search process instead of performing a final build.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the source audio file list.</param>
        /// <param name="outputPath">The path for the output FSB.</param>
        /// <param name="options">The rebuild options.</param>
        /// <param name="targetSize">The maximum allowed size in bytes.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <param name="forceOversize">Allow oversized output if true.</param>
        /// <returns>A <see cref="RebuildResult"/> indicating the build outcome.</returns>
        private async Task<RebuildResult> RunFsBankClWithSizeModeAsync_BinarySearch(
            string sourceAudioPath,
            string outputPath,
            RebuildOptions options,
            long targetSize,
            IProgress<ProgressReport> progress,
            bool forceOversize)
        {
            bool canAdjustQuality = options.EncodingFormat == SOUND_TYPE.VORBIS;

            // If the format does not support quality adjustments (e.g., PCM, FADPCM), perform a single build.
            if (!canAdjustQuality)
            {
                progress?.Report(new ProgressReport($"Building with fixed format ({options.EncodingFormat})...", 10));

                var buildProgress = new Progress<ProgressReport>(report =>
                {
                    string detailedStatus = $"Building with fixed format: {report.Status}";
                    progress?.Report(new ProgressReport(detailedStatus, 10 + (int)(report.Percentage * 0.8)));
                });
                long newSize = await BuildAndGetSizeAsync(sourceAudioPath, outputPath, options, options.Quality, buildProgress);

                if (newSize == -1)
                {
                    return new RebuildResult { Status = RebuildStatus.Failed, Message = "fsbankcl.exe build failed." };
                }

                // If the file is oversized and not forced, return a special status to ask for user confirmation.
                if (newSize > targetSize && !forceOversize)
                {
                    progress?.Report(new ProgressReport("Build resulted in oversized file. Awaiting user confirmation...", 50));
                    return new RebuildResult
                    {
                        Status = RebuildStatus.OversizedConfirmationNeeded,
                        OriginalFsbSize = targetSize,
                        NewFsbSize = newSize,
                        TemporaryFsbPath = outputPath
                    };
                }

                // If the file is smaller than the original, pad it with null bytes to match the exact size.
                if (newSize < targetSize)
                {
                    progress?.Report(new ProgressReport($"Padding FSB with {targetSize - newSize} bytes...", 90));
                    using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                    {
                        fs.SetLength(targetSize);
                    }
                }
                return new RebuildResult { Status = RebuildStatus.Success };
            }

            // --- Binary Search for Optimal Vorbis Quality ---
            int minQuality = 0;
            int maxQuality = 100;
            int bestKnownQuality = -1;
            string bestKnownGoodFilePath = null; // Path to the best valid FSB built so far.
            int attempts = 0;

            // Limit the number of binary search iterations to prevent infinite loops.
            const int maxAttempts = BINARY_SEARCH_MAX_ATTEMPTS;

            progress?.Report(new ProgressReport("Starting binary search for optimal quality...", 0));

            while (minQuality <= maxQuality && attempts < maxAttempts)
            {
                attempts++;
                int overallSearchProgress = (int)((float)attempts / maxAttempts * 90);
                int midQuality = minQuality + (maxQuality - minQuality) / 2;
                string tempBuildPath = outputPath + EXTENSION_TEMP;

                var trialProgress = new Progress<ProgressReport>(report =>
                {
                    string detailedStatus = $"Optimizing (Trial #{attempts} at {midQuality}% Quality): {report.Status}";
                    progress?.Report(new ProgressReport(detailedStatus, overallSearchProgress));
                });

                long currentSize = await BuildAndGetSizeAsync(sourceAudioPath, tempBuildPath, options, midQuality, trialProgress);

                if (currentSize != -1 && currentSize <= targetSize)
                {
                    // The build was successful and fits within the target size.
                    bestKnownQuality = midQuality; // This is a potentially optimal quality.
                    minQuality = midQuality + 1;  // Try for even better quality.

                    // Preserve this successful build file for potential final use.
                    try
                    {
                        // If a previously saved "best" file exists, it's now obsolete. Delete it.
                        if (File.Exists(bestKnownGoodFilePath))
                        {
                            File.Delete(bestKnownGoodFilePath);
                        }

                        // Define the path for the new "best" file and move the temporary build to it.
                        bestKnownGoodFilePath = outputPath + EXTENSION_GOOD;
                        File.Move(tempBuildPath, bestKnownGoodFilePath);
                    }
                    catch (IOException ex)
                    {
                        // Handle potential file operation errors gracefully.
                        progress?.Report(new ProgressReport($"[WARNING] Failed to manage temporary file: {ex.Message}", overallSearchProgress));
                    }
                }
                else
                {
                    // The build exceeded the target size or failed.
                    maxQuality = midQuality - 1; // Lower the quality for the next attempt.

                    // The temporary file is oversized or invalid and no longer needed.
                    if (File.Exists(tempBuildPath))
                    {
                        try
                        {
                            File.Delete(tempBuildPath);
                        }
                        catch
                        {
                            // Silently ignore deletion failure.
                        }
                    }
                }
            }

            // If no suitable quality was ever found, the build fails.
            if (bestKnownQuality == -1)
            {
                string msg = $"Could not find any quality that fits within {targetSize} bytes.";
                progress?.Report(new ProgressReport(msg, 100));

                // Ensure any lingering .good file is cleaned up on failure.
                if (File.Exists(bestKnownGoodFilePath))
                {
                    try
                    {
                        File.Delete(bestKnownGoodFilePath);
                    }
                    catch
                    {
                        // Silently ignore deletion failure.
                    }
                }
                return new RebuildResult { Status = RebuildStatus.Failed, Message = msg };
            }

            // The binary search is complete. Use the best found file instead of rebuilding.
            progress?.Report(new ProgressReport($"Optimal quality found: {bestKnownQuality}%. Finalizing...", 95));

            // Move the best successful build to the final output path.
            if (File.Exists(bestKnownGoodFilePath))
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.Move(bestKnownGoodFilePath, outputPath);
            }
            else
            {
                // This case should be rare, but indicates a logic error or file system issue.
                string msg = "Internal error: Best build file was not found for finalization.";
                progress?.Report(new ProgressReport(msg, 100));
                return new RebuildResult { Status = RebuildStatus.Failed, Message = msg };
            }

            long finalSize = new FileInfo(outputPath).Length;

            // Pad the final file if it's smaller than the target size.
            if (finalSize < targetSize)
            {
                progress?.Report(new ProgressReport($"Padding final FSB with {targetSize - finalSize} bytes...", 98));
                using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                {
                    fs.SetLength(targetSize);
                }
            }

            progress?.Report(new ProgressReport("Build successful.", 100));
            return new RebuildResult { Status = RebuildStatus.Success };
        }

        /// <summary>
        /// Inserts the newly built FSB data back into the original container file.
        /// </summary>
        /// <param name="targetNode">The target audio node.</param>
        /// <param name="newFsbPath">The path to the new FSB file.</param>
        /// <param name="finalSavePath">The output file path.</param>
        private async Task PatchFileWithNewFsbAsync(AudioDataNode targetNode, string newFsbPath, string finalSavePath)
        {
            string sourcePath = targetNode.CachedAudio.SourcePath;
            long fsbOffset = targetNode.FsbChunkOffset;

            if (!File.Exists(newFsbPath))
            {
                throw new FileNotFoundException("Rebuilt FSB file not found", newFsbPath);
            }

            using (FileStream sourceFs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream newFsbFs = new FileStream(newFsbPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream destFs = new FileStream(finalSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Copy data from the source file before the FSB chunk.
                if (fsbOffset > 0)
                {
                    await CopyStreamRangeAsync(sourceFs, destFs, fsbOffset).ConfigureAwait(false);
                }

                // Write the new FSB data.
                await newFsbFs.CopyToAsync(destFs).ConfigureAwait(false);

                // Calculate where the original FSB chunk ended.
                long oldFsbLength = await CalculateFsbLengthAsync(sourceFs, fsbOffset).ConfigureAwait(false);
                long suffixStart = fsbOffset + oldFsbLength;

                // Copy data from the source file that appeared after the original FSB chunk.
                if (suffixStart < sourceFs.Length)
                {
                    sourceFs.Seek(suffixStart, SeekOrigin.Begin);
                    await sourceFs.CopyToAsync(destFs).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Copies a specified range of bytes from one stream to another.
        /// </summary>
        private async Task CopyStreamRangeAsync(Stream input, Stream output, long bytesToCopy)
        {
            byte[] buffer = new byte[AppConstants.BufferSizeXLarge];
            long totalRead = 0;
            int read;

            while (totalRead < bytesToCopy)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesToCopy - totalRead);

                // Use ConfigureAwait(false) to ensure the continuation runs on the thread pool.
                // This is critical to prevent UI freezes when processing large files.
                read = await input.ReadAsync(buffer, 0, toRead).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                totalRead += read;
            }
        }

        #endregion

        #region 5. Private Helper Methods

        /// <summary>
        /// Runs fsbankcl.exe to build the FSB and returns the output file size.
        /// </summary>
        /// <returns>The size of the output file in bytes, or -1 if failed.</returns>
        private async Task<long> BuildAndGetSizeAsync(string sourceAudioPath, string outputPath, RebuildOptions options, int quality, IProgress<ProgressReport> progress)
        {
            var tempOptions = new RebuildOptions
            {
                EncodingFormat = options.EncodingFormat,
                Quality = quality
            };

            try
            {
                bool success = await RunFsBankClAsync(sourceAudioPath, outputPath, tempOptions, progress);
                if (success && File.Exists(outputPath))
                {
                    long size = new FileInfo(outputPath).Length;
                    return size;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("fsbankcl.exe failed") || ex.Message.Contains("Failed to execute"))
                {
                    throw;
                }
                System.Diagnostics.Debug.WriteLine($"Build attempt failed: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Asynchronously reads lines from a stream reader and reports progress updates.
        /// </summary>
        /// <param name="reader">The stream reader to consume output from.</param>
        /// <param name="totalFiles">The total number of files being processed, used for percentage calculation.</param>
        /// <param name="progress">The progress reporter to update the UI.</param>
        /// <param name="fullOutput">A string builder to capture the full log output.</param>
        private async Task ConsumeStreamReaderAsync(StreamReader reader, int totalFiles, IProgress<ProgressReport> progress, StringBuilder fullOutput)
        {
            string line;

            // Initialize the stopwatch to manage the throttling of UI updates.
            // Updates are limited to occur approximately every 33ms to prevent UI freezing.
            var stopwatch = Stopwatch.StartNew();
            long lastReportTime = 0;
            const long ReportIntervalMs = UI_THROTTLE_INTERVAL_MS;

            // Read the output stream line by line until the end of the stream.
            // Note: The Task.Delay(1) has been removed to maximize processing speed.
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Capture the raw output for the full log history.
                fullOutput.AppendLine(line);

                // Trigger the external log event to ensure all lines are recorded in the file.
                // This ensures comprehensive logging even when UI updates are skipped.
                OnLogReceived?.Invoke(line);

                // Validate the progress reporter and total file count before processing.
                if (progress == null || totalFiles <= 0)
                {
                    continue;
                }

                line = line.Trim();

                // Initialize report variables using default values to avoid struct nullability issues.
                ProgressReport currentReport = default;
                bool hasValidReport = false;

                // Attempt to parse the specific progress format used by fsbankcl.exe (e.g., "[1/10]: Compressing...").
                if (line.StartsWith("[") && line.Contains("]:"))
                {
                    try
                    {
                        // Extract the current file index from the brackets.
                        int endIndex = line.IndexOf("]:");
                        string numberStr = line.Substring(1, endIndex - 1);

                        // Parse the index and calculate the progress percentage.
                        if (int.TryParse(numberStr, out int currentIndex))
                        {
                            int percentage = (int)(((double)currentIndex + 1) / totalFiles * 100);
                            string status = $"[{currentIndex + 1}/{totalFiles}] {line.Substring(endIndex + 2).Trim()}";

                            currentReport = new ProgressReport(status, percentage);
                            hasValidReport = true;
                        }
                    }
                    catch
                    {
                        // Fallback to reporting the raw line if parsing fails.
                        currentReport = new ProgressReport(line, -1);
                        hasValidReport = true;
                    }
                }
                else
                {
                    // Report standard output lines without updating the percentage.
                    currentReport = new ProgressReport(line, -1);
                    hasValidReport = true;
                }

                // Report the latest status to the UI if the time interval has elapsed.
                if (hasValidReport)
                {
                    long currentTime = stopwatch.ElapsedMilliseconds;

                    // Update the UI only if enough time has passed since the last report or the operation is complete.
                    // This uses 'currentReport', which contains the data from the most recently read line.
                    if ((currentTime - lastReportTime >= ReportIntervalMs) || (currentReport.Percentage == 100))
                    {
                        progress.Report(currentReport);
                        lastReportTime = currentTime;
                    }
                }
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// Executes the external fsbankcl.exe tool to compile the audio files.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the input file list or directory.</param>
        /// <param name="outputPath">The target output path for the .fsb file.</param>
        /// <param name="options">Configuration options for encoding and quality.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns><c>true</c> if the process completes successfully; otherwise, <c>false</c>.</returns>
        private async Task<bool> RunFsBankClAsync(string sourceAudioPath, string outputPath, RebuildOptions options, IProgress<ProgressReport> progress)
        {
            string formatArg;
            switch (options.EncodingFormat)
            {
                case SOUND_TYPE.VORBIS:
                    formatArg = "vorbis";
                    break;
                case SOUND_TYPE.FADPCM:
                    formatArg = "fadpcm";
                    break;
                default:
                    formatArg = "pcm";
                    break;
            }

            string qualityArg = options.EncodingFormat == SOUND_TYPE.VORBIS ? $"-q {options.Quality}" : "";

            try
            {
                var totalFiles = File.ReadLines(sourceAudioPath).Count();

                using (var process = new Process())
                {
                    // Store the active process instance to allow forced termination by the main app.
                    _activeChildProcess = process;

                    try
                    {
                        process.StartInfo.FileName = AppConstants.FsBankExecutable;
                        process.StartInfo.Arguments = $"-o \"{outputPath}\" -format {formatArg} {qualityArg} \"{sourceAudioPath}\"";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

                        var processOutput = new StringBuilder();
                        var processError = new StringBuilder();

                        process.Start();

                        var outputTask = ConsumeStreamReaderAsync(process.StandardOutput, totalFiles, progress, processOutput);
                        var errorTask = ConsumeStreamReaderAsync(process.StandardError, -1, null, processError);

                        await Task.WhenAll(outputTask, errorTask);
                        await Task.Run(() => process.WaitForExit());

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"fsbankcl.exe failed with Exit Code {process.ExitCode}.\n[STDERR]: {processError}\n[STDOUT]: {processOutput}");
                        }
                        return true;
                    }
                    finally
                    {
                        // Clear the reference when the process ends or throws.
                        _activeChildProcess = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("fsbankcl.exe failed"))
                {
                    throw;
                }
                System.Diagnostics.Debug.WriteLine($"An exception occurred while running fsbankcl.exe: {ex.Message}");
                throw new Exception($"Failed to execute build tool: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates the accurate length of an FSB chunk within a stream.
        /// If header parsing fails, it scans for the next 'FSB5' signature to find the boundary.
        /// </summary>
        private async Task<long> CalculateFsbLengthAsync(FileStream stream, long startOffset)
        {
            if (startOffset >= stream.Length)
            {
                return 0;
            }

            stream.Seek(startOffset, SeekOrigin.Begin);
            byte[] header = new byte[AppConstants.BufferSizeLarge];

            // Use ConfigureAwait(false) to prevent blocking the UI context during the read operation.
            int read = await stream.ReadAsync(header, 0, header.Length).ConfigureAwait(false);

            if (read < MIN_FSB_HEADER_SIZE)
            {
                // Not enough data for header.
                return stream.Length - startOffset;
            }

            // Try standard header parsing first.
            try
            {
                uint totalChunkSize = BitConverter.ToUInt32(header, 8);
                uint sampleHeadersSize = BitConverter.ToUInt32(header, 12);
                uint dataSize = BitConverter.ToUInt32(header, 16);

                if (totalChunkSize > 0 &&
                    totalChunkSize >= FsbSpecs.HeaderSize_FSB5 + sampleHeadersSize + dataSize &&
                    startOffset + totalChunkSize <= stream.Length)
                {
                    return totalChunkSize;
                }
            }
            catch
            {
                // Silently fall back to manual scanning if parsing fails.
            }

            // Fallback: Scan for the next FSB5 header.
            long currentPos = startOffset + FsbSpecs.SignatureLength; // Start scanning after the current "FSB5" signature.
            byte[] buffer = new byte[AppConstants.BufferSizeLarge]; // 64KB buffer.

            // Use the predefined signature constant for scanning.
            byte[] signature = FSB5_SIGNATURE_BYTES;

            stream.Seek(currentPos, SeekOrigin.Begin);

            while (currentPos < stream.Length)
            {
                // Use ConfigureAwait(false) inside the loop to ensure the UI remains responsive during long scans.
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                // Scan the buffer for the signature.
                for (int i = 0; i < bytesRead - (int)(FsbSpecs.SignatureLength - 1); i++)
                {
                    if (buffer[i] == signature[0] &&
                        buffer[i + 1] == signature[1] &&
                        buffer[i + 2] == signature[2] &&
                        buffer[i + 3] == signature[3])
                    {
                        // Found next header! The length is from start to here.
                        long nextHeaderOffset = currentPos + i;
                        return nextHeaderOffset - startOffset;
                    }
                }

                // Handle boundary overlap: seek back 3 bytes so we don't miss a split signature.
                if (bytesRead == buffer.Length)
                {
                    currentPos += bytesRead - (long)(FsbSpecs.SignatureLength - 1);
                    stream.Seek(currentPos, SeekOrigin.Begin);
                }
                else
                {
                    currentPos += bytesRead;
                }
            }

            // No next header found, assume it goes to the end of the file.
            return stream.Length - startOffset;
        }

        #endregion
    }
}