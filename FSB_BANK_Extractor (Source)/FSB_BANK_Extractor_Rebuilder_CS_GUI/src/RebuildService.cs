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
 *  - Last Update: 2025-12-30
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
        private const string EXTENSION_WAV = ".wav";

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

        // Fsbankcl.exe argument strings.
        private const string FSBANKCL_FORMAT_VORBIS = "vorbis";
        private const string FSBANKCL_FORMAT_FADPCM = "fadpcm";
        private const string FSBANKCL_FORMAT_PCM = "pcm";
        private const string FSBANKCL_QUALITY_PREFIX = "-q ";

        // Status and Log Messages.
        private const string LOG_STATUS_PREFIX = "[STATUS] ";
        private const string PHASE_PREPARING = "[1/4 PREPARING]";
        private const string PHASE_BUILDING = "[2/4 BUILDING]";
        private const string PHASE_PATCHING = "[3/4 PATCHING]";
        private const string PHASE_CLEANUP = "[4/4 CLEANUP]";
        private const string MSG_WORKSPACE_INIT = "Creating temporary workspace...";
        private const string MSG_REPLACING_FILES_FORMAT = "Replacing {0} audio files in workspace...";
        private const string MSG_REUSING_FILE = "[SKIPPED] Reusing previously built file.";
        private const string MSG_PATCHING_FILE = "Writing new FSB data into the final file...";
        private const string MSG_FINALIZING = "Finalizing operation...";
        private const string MSG_ERROR_FSB_SIZE = "[ERROR] Could not determine original FSB size.";
        private const string MSG_OPTIMIZING_FORMAT = "Optimizing (Trial #{0} at {1}% Quality): {2}";
        private const string MSG_PADDING_FSB_FORMAT = "Padding FSB with {0} bytes...";
        private const string MSG_OPTIMAL_QUALITY_FORMAT = "Optimal quality found: {0}%. Finalizing...";

        // Binary Signatures.
        /// <summary>
        /// The "FSB5" signature bytes used for scanning stream boundaries.
        /// </summary>
        private static readonly byte[] FSB5_SIGNATURE_BYTES = { 0x46, 0x53, 0x42, 0x35 };

        /// <summary>
        /// The shared FMOD Core System instance for audio operations.
        /// </summary>
        private readonly FMOD.System _coreSystem;

        /// <summary>
        /// A lock object to synchronize access to the non-thread-safe FMOD Core System.
        /// </summary>
        private readonly object _coreSystemLock;

        /// <summary>
        /// The service used to perform audio data extraction into WAV format.
        /// </summary>
        private readonly ExtractionService _extractionService;

        /// <summary>
        /// Tracks the currently running external process (fsbankcl.exe) to allow forced termination.
        /// </summary>
        private volatile Process _activeChildProcess;

        /// <summary>
        /// Occurs when a log line is produced by the rebuild process.
        /// </summary>
        public event Action<string> OnLogReceived;

        #endregion

        #region 2. Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="RebuildService"/> class.
        /// </summary>
        /// <param name="coreSystem">The FMOD Core System instance. Must not be null.</param>
        /// <param name="syncLock">The synchronization lock for FMOD operations. Must not be null.</param>
        /// <param name="extractionService">The service used for audio extraction. Must not be null.</param>
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
        /// Any exceptions during termination (e.g., process already exited) are silently ignored to guarantee the shutdown process is not interrupted.
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
        /// <param name="targetNode">The audio data node representing the target FSB container. Must not be null.</param>
        /// <param name="batchReplacements">A list of batch items containing replacement details. Must not be null and should contain at least one item.</param>
        /// <param name="finalSavePath">The full path where the rebuilt file will be saved. Must be a valid and writable file path.</param>
        /// <param name="options">Configuration options for the rebuild process. Must not be null.</param>
        /// <param name="progress">An object to report progress updates to the UI. Can be null if not needed.</param>
        /// <param name="forceOversize">If set to <c>true</c>, proceeds even if the file size exceeds the original; otherwise, requires user confirmation.</param>
        /// <param name="previousResult">The result of a previous attempt to allow workspace reuse. Can be null on the first attempt.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains a <see cref="RebuildResult"/> indicating success or failure.</returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Calculate the exact size of the original FSB chunk.
        ///  2) Initialize the temporary workspace and extract original audio assets if not reusing a previous build.
        ///  3) Run the build tool, using binary search optimization for Vorbis.
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

            // Define a local helper for logging and reporting to centralize the logic.
            void LogAndReport(string status, int percentage)
            {
                OnLogReceived?.Invoke(LOG_STATUS_PREFIX + status);
                progress?.Report(new ProgressReport(status, percentage));
            }

            try
            {
                string rebuiltFsbPath;
                long originalFsbSize = 0;

                // Step 1: Calculate the exact size of the original FSB chunk.
                using (var fs = new FileStream(targetNode.CachedAudio.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, AppConstants.BufferSizeSmall, true))
                {
                    originalFsbSize = await CalculateFsbLengthAsync(fs, targetNode.FsbChunkOffset);
                }

                // Step 2: Initialize workspace and build, or reuse a previous result.
                if (previousResult == null || string.IsNullOrEmpty(previousResult.TemporaryFsbPath))
                {
                    LogAndReport(PHASE_PREPARING + " " + MSG_WORKSPACE_INIT, 0);

                    // Create a progress handler that scopes updates to the "Preparing" phase.
                    var prepareProgressHandler = new Progress<ProgressReport>(report =>
                    {
                        string phaseStatus = $"{PHASE_PREPARING} {report.Status}";
                        int overallProgress = (int)(report.Percentage * PROGRESS_WEIGHT_PREPARE);
                        LogAndReport(phaseStatus, overallProgress);
                    });

                    workspacePath = await SetupWorkspaceAsync(targetNode, prepareProgressHandler);

                    string replaceMsg = string.Format(MSG_REPLACING_FILES_FORMAT, batchReplacements.Count);
                    LogAndReport($"{PHASE_PREPARING} {replaceMsg}", PROGRESS_OFFSET_BUILD);
                    await ReplaceAudioInWorkspaceAsync(workspacePath, batchReplacements, options);

                    rebuiltFsbPath = Path.Combine(workspacePath, REBUILT_FSB_FILE_NAME);
                    string buildListPath = Path.Combine(workspacePath, BUILD_LIST_FILE_NAME);

                    // Force garbage collection to release memory before starting the build process.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (originalFsbSize <= 0)
                    {
                        LogAndReport(MSG_ERROR_FSB_SIZE, PROGRESS_OFFSET_BUILD);
                        return new RebuildResult { Status = RebuildStatus.Failed, Message = "Could not determine original FSB size.", WorkspacePath = workspacePath };
                    }

                    // Step 3: Run the build tool with optimization.
                    var buildProgressHandler = new Progress<ProgressReport>(report =>
                    {
                        string phaseStatus = $"{PHASE_BUILDING} {report.Status}";
                        int overallProgress = -1;
                        if (report.Percentage >= 0)
                        {
                            overallProgress = PROGRESS_OFFSET_BUILD + (int)(report.Percentage / 100.0 * (PROGRESS_WEIGHT_BUILD * 100));
                        }
                        LogAndReport(phaseStatus, overallProgress);
                    });

                    var buildResult = await RunFsBankClWithSizeModeAsync_BinarySearch(
                        buildListPath,
                        rebuiltFsbPath,
                        options,
                        originalFsbSize,
                        buildProgressHandler,
                        forceOversize
                    );

                    buildResult.WorkspacePath = workspacePath;

                    // If the build failed or requires user confirmation, return immediately.
                    if (!buildResult.Success)
                    {
                        return buildResult;
                    }
                }
                else
                {
                    LogAndReport(MSG_REUSING_FILE, PROGRESS_OFFSET_BUILD);
                    rebuiltFsbPath = previousResult.TemporaryFsbPath;
                    workspacePath = previousResult.WorkspacePath;
                }

                // Step 4: Patch the newly built FSB data back into the final container file.
                LogAndReport(PHASE_PATCHING + " " + MSG_PATCHING_FILE, PROGRESS_OFFSET_PATCH);
                await PatchFileWithNewFsbAsync(targetNode, rebuiltFsbPath, finalSavePath);

                // Step 5: Finalize the operation and perform cleanup.
                LogAndReport(PHASE_CLEANUP + " " + MSG_FINALIZING, PROGRESS_OFFSET_CLEANUP);
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
        /// <param name="targetNode">The target audio node containing source file information. Must not be null.</param>
        /// <param name="progress">The progress reporter to update the UI. Can be null.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains the full path to the created workspace directory.</returns>
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
                var finalPaths = new List<string>();

                int totalNumSubSounds = 0;
                SOUND_TYPE buildType = SOUND_TYPE.UNKNOWN;

                // Analyze structure to get count and format.
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
                    Utilities.SafeRelease(ref analysisSound);
                }

                finalManifest.BuildFormat = buildType;
                if (totalNumSubSounds == 0)
                {
                    return new { Manifest = finalManifest, Paths = finalPaths };
                }

                // Prepare for parallel extraction.
                int processedCount = 0;
                var concurrentResults = new System.Collections.Concurrent.ConcurrentBag<(SubSoundManifestInfo Info, string Path)>();
                var partitioner = System.Collections.Concurrent.Partitioner.Create(0, totalNumSubSounds);

                // Configure parallelism multiplier for oversubscription.
                int maxParallelism = Environment.ProcessorCount * THREAD_MULTIPLIER;

                Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, range =>
                {
                    // Each thread gets its own file handle for concurrent decoding.
                    Sound threadLocalFsb = new Sound();
                    bool isThreadFsbLoaded = false;
                    try
                    {
                        lock (_coreSystemLock)
                        {
                            CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                            isThreadFsbLoaded = _coreSystem.createSound(tempFsbPath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS | MODE.ACCURATETIME, ref ex, out threadLocalFsb) == RESULT.OK;
                        }

                        if (isThreadFsbLoaded)
                        {
                            for (int i = range.Item1; i < range.Item2; i++)
                            {
                                Sound subSound = new Sound();
                                try
                                {
                                    // No global lock is needed here, allowing true parallel decoding.
                                    threadLocalFsb.getSubSound(i, out subSound);
                                    subSound.getLength(out uint lenBytes, TIMEUNIT.PCMBYTES);
                                    subSound.getFormat(out _, out SOUND_FORMAT fmt, out int ch, out int bits);
                                    subSound.getDefaults(out float rate, out _);
                                    subSound.getLoopPoints(out uint loopStart, TIMEUNIT.MS, out uint loopEnd, TIMEUNIT.MS);
                                    subSound.getMode(out MODE mode);
                                    subSound.getName(out string name, MAX_NAME_LENGTH);

                                    // Validate and correct sample rate if necessary.
                                    rate = (rate < MIN_SAMPLE_RATE) ? DEFAULT_SAMPLE_RATE : rate;
                                    string indexFolder = i.ToString("D3");
                                    string subDirectoryPath = Path.Combine(audioSourcePath, indexFolder);
                                    Directory.CreateDirectory(subDirectoryPath);

                                    string fileNameOnly = Utilities.SanitizeFileName(name) + EXTENSION_WAV;
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
                                            if (read == 0) break;
                                            wavFs.Write(buf, 0, (int)read);
                                            totalRead += read;
                                        }
                                    }

                                    // Store the result safely.
                                    concurrentResults.Add((new SubSoundManifestInfo
                                    {
                                        Index = i,
                                        Name = name,
                                        OriginalFileName = Path.Combine(indexFolder, fileNameOnly),
                                        Looping = (mode & MODE.LOOP_NORMAL) != 0,
                                        LoopStart = loopStart,
                                        LoopEnd = loopEnd,
                                    }, fullWavPath));

                                    int currentCount = System.Threading.Interlocked.Increment(ref processedCount);
                                    int subProgress = 15 + (int)(((float)currentCount / totalNumSubSounds) * 80);
                                    progress?.Report(new ProgressReport($"Extracting original sound {currentCount}/{totalNumSubSounds}...", subProgress));
                                }
                                finally
                                {
                                    Utilities.SafeRelease(ref subSound);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Utilities.SafeRelease(ref threadLocalFsb);
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
        /// <param name="workspacePath">The path to the workspace directory. Must not be null or empty.</param>
        /// <param name="replacements">A list of items to replace. Must not be null.</param>
        /// <param name="options">The rebuild configuration options. Must not be null.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
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
                    if (targetSubSound == null) continue;

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
                Utilities.SafeRelease(ref newSound);
            }

            manifest.BuildFormat = options.EncodingFormat;
            await Utilities.WriteAllTextAsync(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        /// <summary>
        /// Executes a binary search for the optimal encoding quality that fits within the target size.
        /// This optimized version reuses the best successful build from the search process instead of performing a final build.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the source audio file list. Must not be null or empty.</param>
        /// <param name="outputPath">The path for the output FSB. Must not be null or empty.</param>
        /// <param name="options">The rebuild options. Must not be null.</param>
        /// <param name="targetSize">The maximum allowed size in bytes. Must be greater than zero.</param>
        /// <param name="progress">The progress reporter. Can be null.</param>
        /// <param name="forceOversize">Allow oversized output if true.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains a <see cref="RebuildResult"/> indicating the build outcome.</returns>
        private async Task<RebuildResult> RunFsBankClWithSizeModeAsync_BinarySearch(
            string sourceAudioPath,
            string outputPath,
            RebuildOptions options,
            long targetSize,
            IProgress<ProgressReport> progress,
            bool forceOversize)
        {
            bool canAdjustQuality = options.EncodingFormat == SOUND_TYPE.VORBIS;

            // Handle fixed-format builds (non-Vorbis).
            if (!canAdjustQuality)
            {
                var buildProgress = new Progress<ProgressReport>(report =>
                {
                    string detailedStatus = $"Building with fixed format: {report.Status}";
                    progress?.Report(new ProgressReport(detailedStatus, report.Percentage));
                });
                long newSize = await BuildAndGetSizeAsync(sourceAudioPath, outputPath, options, options.Quality, buildProgress);

                if (newSize == -1)
                {
                    return new RebuildResult { Status = RebuildStatus.Failed, Message = "fsbankcl.exe build failed." };
                }

                // If the file is oversized, require user confirmation.
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

                // Pad the file to match the original size if it's smaller.
                if (newSize < targetSize)
                {
                    progress?.Report(new ProgressReport(string.Format(MSG_PADDING_FSB_FORMAT, targetSize - newSize), 90));
                    using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                    {
                        fs.SetLength(targetSize);
                    }
                }
                return new RebuildResult { Status = RebuildStatus.Success };
            }

            // Perform binary search for optimal Vorbis quality.
            int minQuality = 0;
            int maxQuality = 100;
            int bestKnownQuality = -1;
            string bestKnownGoodFilePath = null;
            int attempts = 0;

            progress?.Report(new ProgressReport("Starting binary search for optimal quality...", 0));

            while (minQuality <= maxQuality && attempts < BINARY_SEARCH_MAX_ATTEMPTS)
            {
                attempts++;
                int midQuality = minQuality + (maxQuality - minQuality) / 2;
                string tempBuildPath = outputPath + EXTENSION_TEMP;

                // Create a progress handler that scopes updates to the current optimization trial.
                var trialProgress = new Progress<ProgressReport>(report =>
                {
                    double progressWithinBuildPhase = ((double)(attempts - 1) / BINARY_SEARCH_MAX_ATTEMPTS) + (report.Percentage / 100.0 / BINARY_SEARCH_MAX_ATTEMPTS);
                    int overallPercentage = (int)(progressWithinBuildPhase * 100);
                    string detailedStatus = string.Format(MSG_OPTIMIZING_FORMAT, attempts, midQuality, report.Status);
                    progress?.Report(new ProgressReport(detailedStatus, overallPercentage));
                });

                long currentSize = await BuildAndGetSizeAsync(sourceAudioPath, tempBuildPath, options, midQuality, trialProgress);

                if (currentSize != -1 && currentSize <= targetSize)
                {
                    bestKnownQuality = midQuality;
                    minQuality = midQuality + 1;
                    if (File.Exists(bestKnownGoodFilePath)) File.Delete(bestKnownGoodFilePath);
                    bestKnownGoodFilePath = outputPath + EXTENSION_GOOD;
                    File.Move(tempBuildPath, bestKnownGoodFilePath);
                }
                else
                {
                    maxQuality = midQuality - 1;
                    if (File.Exists(tempBuildPath)) File.Delete(tempBuildPath);
                }
            }

            if (bestKnownQuality == -1)
            {
                string msg = $"Could not find any quality that fits within {targetSize} bytes.";
                progress?.Report(new ProgressReport(msg, 100));
                if (File.Exists(bestKnownGoodFilePath)) File.Delete(bestKnownGoodFilePath);
                return new RebuildResult { Status = RebuildStatus.Failed, Message = msg };
            }

            progress?.Report(new ProgressReport(string.Format(MSG_OPTIMAL_QUALITY_FORMAT, bestKnownQuality), 95));

            if (File.Exists(bestKnownGoodFilePath))
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(bestKnownGoodFilePath, outputPath);
            }
            else
            {
                return new RebuildResult { Status = RebuildStatus.Failed, Message = "Internal error: Best build file was not found." };
            }

            long finalSize = new FileInfo(outputPath).Length;
            if (finalSize < targetSize)
            {
                progress?.Report(new ProgressReport(string.Format(MSG_PADDING_FSB_FORMAT, targetSize - finalSize), 98));
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
        /// <param name="targetNode">The target audio node. Must not be null.</param>
        /// <param name="newFsbPath">The path to the new FSB file. Must not be null or empty.</param>
        /// <param name="finalSavePath">The output file path. Must not be null or empty.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        private async Task PatchFileWithNewFsbAsync(AudioDataNode targetNode, string newFsbPath, string finalSavePath)
        {
            string sourcePath = targetNode.CachedAudio.SourcePath;
            long fsbOffset = targetNode.FsbChunkOffset;

            if (!File.Exists(newFsbPath))
            {
                throw new FileNotFoundException("Rebuilt FSB file not found.", newFsbPath);
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
        /// Copies a specified number of bytes from an input stream to an output stream.
        /// </summary>
        /// <param name="input">The source stream. Must be readable and positioned correctly.</param>
        /// <param name="output">The destination stream. Must be writable.</param>
        /// <param name="bytesToCopy">The total number of bytes to copy. Must be non-negative.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous copy operation.</returns>
        private async Task CopyStreamRangeAsync(Stream input, Stream output, long bytesToCopy)
        {
            byte[] buffer = new byte[AppConstants.BufferSizeXLarge];
            long totalRead = 0;
            while (totalRead < bytesToCopy)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesToCopy - totalRead);
                int read = await input.ReadAsync(buffer, 0, toRead).ConfigureAwait(false);
                if (read == 0) break;
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                totalRead += read;
            }
        }

        #endregion

        #region 5. Private Helper Methods

        /// <summary>
        /// Runs fsbankcl.exe to build the FSB and returns the output file size.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the input file list. Must not be null.</param>
        /// <param name="outputPath">The target output path for the .fsb file. Must not be null.</param>
        /// <param name="options">Configuration options for encoding and quality. Must not be null.</param>
        /// <param name="quality">The encoding quality to use for the build (0-100).</param>
        /// <param name="progress">The progress reporter. Can be null.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result is the size of the output file in bytes, or -1 if the build fails.</returns>
        private async Task<long> BuildAndGetSizeAsync(string sourceAudioPath, string outputPath, RebuildOptions options, int quality, IProgress<ProgressReport> progress)
        {
            var tempOptions = new RebuildOptions
            {
                EncodingFormat = options.EncodingFormat,
                Quality = quality
            };

            bool success = await RunFsBankClAsync(sourceAudioPath, outputPath, tempOptions, progress);
            if (success && File.Exists(outputPath))
            {
                return new FileInfo(outputPath).Length;
            }
            return -1;
        }

        /// <summary>
        /// Asynchronously reads lines from a stream reader and reports progress updates.
        /// </summary>
        /// <param name="reader">The stream reader to consume output from. Must not be null.</param>
        /// <param name="totalFiles">The total number of files being processed, used for percentage calculation. Use -1 if not applicable.</param>
        /// <param name="progress">The progress reporter to update the UI. Can be null.</param>
        /// <param name="fullOutput">A string builder to capture the full log output. Must not be null.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        private async Task ConsumeStreamReaderAsync(StreamReader reader, int totalFiles, IProgress<ProgressReport> progress, StringBuilder fullOutput)
        {
            string line;
            var stopwatch = Stopwatch.StartNew();
            long lastReportTime = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                fullOutput.AppendLine(line);
                OnLogReceived?.Invoke(line);

                if (progress == null) continue;

                // Throttle UI updates to prevent freezing.
                long currentTime = stopwatch.ElapsedMilliseconds;
                if (currentTime - lastReportTime >= UI_THROTTLE_INTERVAL_MS)
                {
                    string status = line.Trim();
                    int percentage = -1;

                    if (totalFiles > 0)
                    {
                        var parts = status.Split(new[] { ':' }, 2);
                        if (parts.Length > 0 && int.TryParse(parts[0], out int currentIndex))
                        {
                            percentage = (int)(((double)currentIndex + 1) / totalFiles * 100);
                        }
                    }

                    progress.Report(new ProgressReport(status, percentage));
                    lastReportTime = currentTime;
                }
            }
            stopwatch.Stop();
        }

        /// <summary>
        /// Executes the external fsbankcl.exe tool to compile the audio files.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the input file list or directory. Must not be null.</param>
        /// <param name="outputPath">The target output path for the .fsb file. Must not be null.</param>
        /// <param name="options">Configuration options for encoding and quality. Must not be null.</param>
        /// <param name="progress">The progress reporter. Can be null.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result is <c>true</c> if the process completes successfully; otherwise, <c>false</c>.</returns>
        private async Task<bool> RunFsBankClAsync(string sourceAudioPath, string outputPath, RebuildOptions options, IProgress<ProgressReport> progress)
        {
            string formatArg;
            switch (options.EncodingFormat)
            {
                case SOUND_TYPE.VORBIS:
                    formatArg = FSBANKCL_FORMAT_VORBIS;
                    break;
                case SOUND_TYPE.FADPCM:
                    formatArg = FSBANKCL_FORMAT_FADPCM;
                    break;
                default:
                    formatArg = FSBANKCL_FORMAT_PCM;
                    break;
            }

            string qualityArg = options.EncodingFormat == SOUND_TYPE.VORBIS ? FSBANKCL_QUALITY_PREFIX + options.Quality : "";
            var totalFiles = File.ReadLines(sourceAudioPath).Count();

            using (var process = new Process())
            {
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
                    _activeChildProcess = null;
                }
            }
        }

        /// <summary>
        /// Calculates the accurate length of an FSB chunk within a stream.
        /// If header parsing fails, it scans for the next 'FSB5' signature to find the boundary.
        /// </summary>
        /// <param name="stream">The file stream to read from. Must be readable and seekable.</param>
        /// <param name="startOffset">The starting offset of the FSB chunk within the stream.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result is the calculated length of the FSB chunk in bytes.</returns>
        /// <remarks>
        /// This method first attempts to read the size directly from the FSB5 header fields for efficiency.
        /// As a fallback, it scans the stream for the next "FSB5" signature. This fallback is crucial for correctly handling concatenated .bank files where multiple FSBs are stored sequentially and header size fields may not be reliable for determining boundaries.
        /// </remarks>
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
                // Not enough data for a valid header, so assume the chunk extends to the end of the file.
                return stream.Length - startOffset;
            }

            // Attempt to parse the size directly from the FSB5 header first.
            try
            {
                uint totalChunkSize = BitConverter.ToUInt32(header, 8);
                uint sampleHeadersSize = BitConverter.ToUInt32(header, 12);
                uint dataSize = BitConverter.ToUInt32(header, 16);

                bool isValidSize = totalChunkSize > 0 &&
                                   totalChunkSize >= FsbSpecs.HeaderSize_FSB5 + sampleHeadersSize + dataSize &&
                                   startOffset + totalChunkSize <= stream.Length;

                if (isValidSize)
                {
                    return totalChunkSize;
                }
            }
            catch
            {
                // Silently fall back to manual scanning if standard header parsing fails.
            }

            // As a fallback, scan for the next "FSB5" signature to determine the chunk boundary.
            long currentPos = startOffset + FsbSpecs.SignatureLength;
            byte[] buffer = new byte[AppConstants.BufferSizeLarge];
            byte[] signature = FSB5_SIGNATURE_BYTES;

            stream.Seek(currentPos, SeekOrigin.Begin);

            while (currentPos < stream.Length)
            {
                // Use ConfigureAwait(false) to ensure the UI remains responsive during long scans.
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0) break;

                // Scan the currently read buffer for the signature.
                for (int i = 0; i < bytesRead - (signature.Length - 1); i++)
                {
                    if (buffer[i] == signature[0] &&
                        buffer[i + 1] == signature[1] &&
                        buffer[i + 2] == signature[2] &&
                        buffer[i + 3] == signature[3])
                    {
                        // Found the start of the next FSB chunk. The length is the distance from the start to this point.
                        return (currentPos + i) - startOffset;
                    }
                }

                // Handle cases where the signature might span across two buffer reads.
                if (bytesRead == buffer.Length)
                {
                    currentPos += bytesRead - (signature.Length - 1);
                    stream.Seek(currentPos, SeekOrigin.Begin);
                }
                else
                {
                    currentPos += bytesRead;
                }
            }

            // If no subsequent "FSB5" header is found, assume this chunk extends to the end of the file.
            return stream.Length - startOffset;
        }

        #endregion
    }
}