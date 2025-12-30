/**
 * @file ExtractionService.cs
 * @brief Encapsulates the logic for extracting audio data from FMOD containers to standard file formats.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This service class is responsible for all audio extraction operations. It handles both batch processing
 * of multiple selected audio nodes and single-file extractions. The service is designed to be run on a
 * background thread, providing progress reports to the UI. It also includes functionality for verbose
 * logging and exporting the file structure to a CSV report.
 *
 * Key Features:
 *  - Batch Extraction: Processes a list of audio nodes and saves them as .wav files.
 *  - Single File Extraction: Provides a method to save a single audio stream to a specified path.
 *  - Container Reuse Strategy: Optimizes batch extraction by opening the parent FSB container once
 *    and retrieving sub-sounds, significantly reducing overhead for formats like Vorbis.
 *  - Dynamic Path Generation: Automatically creates a logical folder structure for extracted files.
 *  - Fallback Decoding: Uses a robust in-memory decoding strategy for legacy or problematic audio formats.
 *  - Verbose Logging: Generates detailed TSV log files for each extraction session if enabled.
 *  - CSV Export: Dumps the entire hierarchical structure of loaded banks into a CSV file for analysis.
 *
 * @acknowledgements
 * The fallback decoding strategy for legacy formats like IMA ADPCM conceptually aligns with techniques
 * demonstrated in tools like 'fsbext' v0.3.5 by Luigi Auriemma, which also handles various compressed formats.
 *
 *  - Legacy Parser Reference: Luigi Auriemma (http://aluigi.altervista.org/)
 *    - GitHub Mirror: https://github.com/gdawg/fsbext
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Key Dependencies: FMOD Core API
 *  - Last Update: 2025-12-30
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FMOD; // Core API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Provides services for extracting audio data and generating reports.
    /// </summary>
    public class ExtractionService
    {
        #region 1. Constants

        /// <summary>
        /// A flag indicating that the user selected to save files in the source directory.
        /// </summary>
        private const string PATH_FLAG_SAME_AS_SOURCE = "##SAME_AS_SOURCE##";

        /// <summary>
        /// The minimum valid sample rate (Hz) to accept from FMOD defaults. 
        /// Values below this usually indicate invalid metadata.
        /// </summary>
        private const float MIN_VALID_SAMPLE_RATE = 100.0f;

        /// <summary>
        /// The file extension for extracted Waveform Audio files.
        /// </summary>
        private const string WAV_EXTENSION = ".wav";

        /// <summary>
        /// The prefix for the extraction log file name.
        /// </summary>
        private const string LOG_FILE_PREFIX = "ExtractionLog_";

        /// <summary>
        /// The file extension for log files.
        /// </summary>
        private const string LOG_FILE_EXTENSION = ".log";

        /// <summary>
        /// A separator line used in log file headers for visual distinction.
        /// </summary>
        private const string LOG_HEADER_SEPARATOR = "================================================================";

        /// <summary>
        /// The header row for the CSV export file.
        /// </summary>
        private const string CSV_HEADER = "Type,Path,Name,Index,Source,Duration(ms),Length(pcm),Encoding,Container,Channels,Bits,Frequency(Hz),Priority,Mode,LoopStart,LoopEnd,3D_MinDistance,3D_MaxDistance,3D_ConeInsideAngle,3D_ConeOutsideAngle,3D_ConeOutsideVolume,Music_Channels,Music_Speed,Tags,SyncPoints,GUID";

        /// <summary>
        /// The header row for the extraction log TSV file.
        /// </summary>
        private const string LOG_TSV_HEADER = "Timestamp\tLevel\tSourceFile\tSoundName\tResult\tEncoding\tContainer\tChannels\tBits\tFrequency(Hz)\tDuration(ms)\tLoopRange(ms)\tDataOffset\tOutputPath\tTimeTaken(ms)";

        #endregion

        #region 2. Fields

        /// <summary>
        /// The FMOD Core System instance used for audio processing.
        /// </summary>
        private readonly FMOD.System _coreSystem;

        /// <summary>
        /// A lock object to ensure thread-safe access to the FMOD Core System from background tasks.
        /// </summary>
        private readonly object _coreSystemLock;

        /// <summary>
        /// A thread-safe dictionary to manage LogWriter instances for different output directories.
        /// </summary>
        private ConcurrentDictionary<string, LogWriter> _loggers;

        /// <summary>
        /// The total number of files to be processed in the current batch session.
        /// </summary>
        private int _totalFilesForSession;

        #endregion

        #region 3. Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractionService"/> class.
        /// </summary>
        /// <param name="coreSystem">The shared FMOD Core System instance. Must not be null.</param>
        /// <param name="syncLock">The lock object for synchronizing FMOD API calls. Must not be null.</param>
        public ExtractionService(FMOD.System coreSystem, object syncLock)
        {
            _coreSystem = coreSystem;
            _coreSystemLock = syncLock;
            _loggers = new ConcurrentDictionary<string, LogWriter>();
        }

        #endregion

        #region 4. Public Methods

        /// <summary>
        /// Sets the total number of files for the current batch extraction session.
        /// </summary>
        /// <param name="total">The total count of files to be processed. Must be a non-negative number.</param>
        public void SetTotalFilesForSession(int total)
        {
            _totalFilesForSession = total >= 0 ? total : 0;
        }

        /// <summary>
        /// Extracts a list of audio nodes asynchronously to the specified location.
        /// </summary>
        /// <remarks>
        /// Processing steps:
        ///  1) Group nodes by their source FSB container to enable reuse.
        ///  2) Iterate through each container group.
        ///  3) Open the parent FSB container once per group.
        ///  4) For each audio item in the group, determine output path and initialize logger.
        ///  5) Extract the sub-sound using the open container handle.
        ///  6) Fallback to individual file extraction if container reuse fails.
        ///  7) Log the result and handle exceptions.
        /// </remarks>
        /// <param name="extractList">The list of TreeNodes containing AudioDataNode tags to extract. Must not be null.</param>
        /// <param name="userSelectedPath">The base output path. Can be a specific folder or the "##SAME_AS_SOURCE##" flag. Must not be null or empty.</param>
        /// <param name="enableVerboseLog">A flag to indicate whether detailed logging should be enabled.</param>
        /// <param name="progress">An IProgress instance to report progress back to the UI. Can be null if progress reporting is not required.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a tuple with the count of successful extractions,
        /// the count of failed extractions, and the total bytes written to disk.
        /// </returns>
        public async Task<(int SuccessCount, int FailCount, long TotalBytes)> ExtractAsync(
            List<TreeNode> extractList,
            string userSelectedPath,
            bool enableVerboseLog,
            IProgress<ProgressReport> progress)
        {
            long totalExtractedBytes = 0;
            var failedExtractions = new ConcurrentBag<(string Context, Exception ex)>();

            // Reset state for the new extraction session.
            _loggers.Clear();
            int totalFiles = extractList.Count;
            int processedCount = 0;

            // Step 1: Group nodes by their source FSB container to enable reuse.
            // This is a key optimization: by opening a container once, we avoid repeated file I/O
            // and header parsing for every single sub-sound within it.
            var groupedNodes = extractList
                .Where(n => n.Tag is AudioDataNode)
                .GroupBy(n =>
                {
                    var data = (AudioDataNode)n.Tag;
                    return (SourcePath: data.CachedAudio.SourcePath, Offset: data.CachedAudio.FileOffset);
                })
                .ToList();

            // This entire block is executed on a background thread.
            await Task.Run(async () =>
            {
                // Step 2: Iterate through each container group.
                foreach (var group in groupedNodes)
                {
                    string containerPath = group.Key.SourcePath;
                    long containerOffset = group.Key.Offset;
                    Sound parentFsbSound = new Sound();
                    bool parentLoaded = false;

                    try
                    {
                        // Step 3: Open the parent FSB container once per group.
                        lock (_coreSystemLock)
                        {
                            CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO
                            {
                                cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                                fileoffset = (uint)containerOffset
                            };

                            RESULT res = _coreSystem.createSound(containerPath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS, ref ex, out parentFsbSound);
                            if (res == RESULT.OK)
                            {
                                parentLoaded = true;
                            }
                        }

                        // Iterate through all items belonging to this container.
                        foreach (var treeNode in group)
                        {
                            var audioNode = (AudioDataNode)treeNode.Tag;
                            var audioInfo = audioNode.CachedAudio;
                            int currentFileIndex = Interlocked.Increment(ref processedCount);

                            try
                            {
                                // Step 4: For each audio item, determine output path and initialize logger.
                                string finalDir, outputFileName, logDirectory;
                                bool isStandaloneFsb = treeNode.Parent?.Parent == null;

                                // Determine the final output directory and log file path based on user settings.
                                if (userSelectedPath == PATH_FLAG_SAME_AS_SOURCE)
                                {
                                    string sourceDir = Path.GetDirectoryName(audioInfo.SourcePath);
                                    if (isStandaloneFsb)
                                    {
                                        string fsbFolderName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(audioInfo.SourcePath));
                                        finalDir = Path.Combine(sourceDir, fsbFolderName);
                                        logDirectory = finalDir;
                                    }
                                    else
                                    {
                                        string bankFileName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(treeNode.Parent.Parent.Text));
                                        string fsbNodeName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(treeNode.Parent.Text));
                                        string bankFolder = Path.Combine(sourceDir, bankFileName);
                                        finalDir = Path.Combine(bankFolder, fsbNodeName);
                                        logDirectory = bankFolder;
                                    }
                                }
                                else
                                {
                                    if (isStandaloneFsb)
                                    {
                                        string fsbFolderName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(audioInfo.SourcePath));
                                        finalDir = Path.Combine(userSelectedPath, fsbFolderName);
                                        logDirectory = finalDir;
                                    }
                                    else
                                    {
                                        string bankFileName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(treeNode.Parent.Parent.Text));
                                        string fsbNodeName = Utilities.SanitizeFileName(Path.GetFileNameWithoutExtension(treeNode.Parent.Text));
                                        string bankFolder = Path.Combine(userSelectedPath, bankFileName);
                                        finalDir = Path.Combine(bankFolder, fsbNodeName);
                                        logDirectory = bankFolder;
                                    }
                                }
                                outputFileName = Utilities.SanitizeFileName(audioInfo.Name) + WAV_EXTENSION;
                                string outputPath = Path.Combine(finalDir, outputFileName);
                                Directory.CreateDirectory(finalDir);

                                // Initialize a logger for the target directory if verbose logging is enabled.
                                if (enableVerboseLog)
                                {
                                    _loggers.GetOrAdd(logDirectory, path =>
                                    {
                                        string logFileName = $"{LOG_FILE_PREFIX}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{LOG_FILE_EXTENSION}";
                                        string logFile = Path.Combine(path, logFileName);
                                        var newLogger = new LogWriter(logFile);
                                        newLogger.WriteRaw(LOG_HEADER_SEPARATOR);
                                        newLogger.WriteRaw($"[SESSION] Extraction Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                        newLogger.WriteRaw(LOG_HEADER_SEPARATOR);
                                        newLogger.WriteRaw($"[TOOL]    App Version:     {FSB_BANK_Extractor_Rebuilder_CS_GUI.AppVersion} ({FSB_BANK_Extractor_Rebuilder_CS_GUI.AppLastUpdate})");
                                        newLogger.WriteRaw($"[TOOL]    Developer:       {FSB_BANK_Extractor_Rebuilder_CS_GUI.AppDeveloper}");
                                        newLogger.WriteRaw($"[ENGINE]  FMOD API:        {FSB_BANK_Extractor_Rebuilder_CS_GUI.FmodFullVersion}");
                                        newLogger.WriteRaw($"[SYSTEM]  OS Version:      {Environment.OSVersion}");
                                        newLogger.WriteRaw($"[SYSTEM]  Processor Count: {Environment.ProcessorCount} Cores");
                                        newLogger.WriteRaw($"[PATH]    Exec Path:       {AppDomain.CurrentDomain.BaseDirectory}");
                                        newLogger.WriteRaw($"[TARGET]  Output Root:     {path}");
                                        newLogger.WriteRaw($"[QUEUE]   Total Files:     {_totalFilesForSession}");
                                        newLogger.WriteRaw(LOG_HEADER_SEPARATOR);
                                        newLogger.WriteRaw("");
                                        newLogger.WriteRaw(LOG_TSV_HEADER);
                                        return newLogger;
                                    });
                                }

                                Stopwatch sw = Stopwatch.StartNew();
                                long writtenBytes = -1;

                                // Create a progress handler to scale sub-progress to the overall progress range.
                                var singleFileProgressHandler = new Progress<ProgressReport>(report =>
                                {
                                    double fileProgressStart = ((double)(currentFileIndex - 1) / totalFiles) * 100.0;
                                    double fileProgressRange = 100.0 / totalFiles;
                                    int overallProgress = (int)(fileProgressStart + (report.Percentage / 100.0 * fileProgressRange));
                                    string statusText = $"[EXTRACTING] [{currentFileIndex}/{totalFiles}] {audioInfo.Name} | {report.Status}";
                                    progress?.Report(new ProgressReport(statusText, overallProgress));
                                });

                                // Step 5: Extract the sub-sound using the open container handle.
                                if (parentLoaded)
                                {
                                    Sound subSound = new Sound();
                                    bool subRetrieved = false;

                                    lock (_coreSystemLock)
                                    {
                                        if (parentFsbSound.getSubSound(audioInfo.Index, out subSound) == RESULT.OK)
                                        {
                                            subRetrieved = true;
                                        }
                                    }

                                    if (subRetrieved)
                                    {
                                        try
                                        {
                                            writtenBytes = await ExtractSoundDataAsync(subSound, audioInfo, outputPath, singleFileProgressHandler);
                                        }
                                        finally
                                        {
                                            lock (_coreSystemLock)
                                            {
                                                Utilities.SafeRelease(ref subSound);
                                            }
                                        }
                                    }
                                }

                                // Step 6: Fallback to individual file extraction if container reuse fails.
                                if (writtenBytes < 0)
                                {
                                    writtenBytes = await ExtractSingleWavAsync(audioInfo, outputPath, singleFileProgressHandler);
                                }

                                sw.Stop();

                                // Step 7: Log the result of the operation.
                                if (writtenBytes >= 0 && enableVerboseLog && _loggers.TryGetValue(logDirectory, out var logger))
                                {
                                    // Log detailed information about the successful extraction.
                                    var details = audioNode.GetDetails();
                                    string GetDetailValue(string group, string propName)
                                    {
                                        var detail = details.FirstOrDefault(d => d.Key.Equals(group, StringComparison.OrdinalIgnoreCase) && d.Value.StartsWith(propName, StringComparison.OrdinalIgnoreCase));
                                        return detail.Value?.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "";
                                    }

                                    string encoding = GetDetailValue("Format", "Encoding");
                                    if (string.IsNullOrEmpty(encoding))
                                    {
                                        encoding = audioInfo.Type.ToString();
                                    }
                                    string container = GetDetailValue("Format", "Container");
                                    if (string.IsNullOrEmpty(container))
                                    {
                                        container = audioInfo.Format.ToString();
                                    }
                                    string frequency = GetDetailValue("Format", "Frequency").Replace(" Hz", "");
                                    if (string.IsNullOrEmpty(frequency))
                                    {
                                        frequency = audioInfo.Frequency.ToString();
                                    }
                                    string duration = GetDetailValue("Time", "Duration (ms)");
                                    if (string.IsNullOrEmpty(duration))
                                    {
                                        duration = audioInfo.LengthMs.ToString();
                                    }
                                    string loopRange = $"{audioInfo.LoopStart} - {audioInfo.LoopEnd}";

                                    logger.LogTSV(LogWriter.LogLevel.INFO,
                                        Path.GetFileName(audioInfo.SourcePath),
                                        audioInfo.Name,
                                        "OK",
                                        encoding,
                                        container,
                                        audioInfo.Channels.ToString(),
                                        audioInfo.Bits.ToString(),
                                        frequency,
                                        duration,
                                        loopRange,
                                        $"0x{audioInfo.DataOffset:X}",
                                        outputPath,
                                        sw.ElapsedMilliseconds.ToString());

                                    Interlocked.Add(ref totalExtractedBytes, writtenBytes);
                                }
                            }
                            catch (Exception ex)
                            {
                                failedExtractions.Add((audioInfo.Name, ex));
                            }
                        }
                    }
                    finally
                    {
                        // Clean up the parent container after processing all items in the group.
                        lock (_coreSystemLock)
                        {
                            Utilities.SafeRelease(ref parentFsbSound);
                        }
                    }
                }
            });

            // Ensure all logger streams are closed and disposed properly.
            foreach (var logger in _loggers.Values)
            {
                logger?.Dispose();
            }

            int failCount = failedExtractions.Count;
            return (totalFiles - failCount, failCount, totalExtractedBytes);
        }

        /// <summary>
        /// Asynchronously exports the structure of all loaded nodes to a CSV file.
        /// </summary>
        /// <param name="nodes">The root collection of TreeNodes to export. Must not be null.</param>
        /// <param name="filePath">The path where the CSV file will be saved. Must be a valid and writable path.</param>
        public async Task ExportToCsvAsync(TreeNodeCollection nodes, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CSV_HEADER);
            ExportNodesRecursive(nodes, sb, "");
            await Utilities.WriteAllTextAsync(filePath, sb.ToString());
        }

        /// <summary>
        /// Extracts a single audio stream asynchronously to a .wav file.
        /// </summary>
        /// <remarks>
        /// This method serves as the core extraction unit and is also used as a fallback by the batch processor.
        /// It determines the optimal extraction strategy (streaming vs. in-memory) based on the audio format.
        /// A fallback to in-memory decoding is used for legacy formats (e.g., MPEG, IMA ADPCM) that may not
        /// stream correctly via FMOD's file-to-file API, ensuring maximum compatibility.
        /// </remarks>
        /// <param name="info">The <see cref="AudioInfo"/> object describing the audio to extract. Must not be null.</param>
        /// <param name="outputPath">The full path of the output .wav file. Must not be null or empty.</param>
        /// <param name="progress">An optional IProgress instance to report sub-progress. Can be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the total number of bytes written to the file.</returns>
        /// <exception cref="Exception">Thrown when both primary and fallback extraction methods fail.</exception>
        public async Task<long> ExtractSingleWavAsync(AudioInfo info, string outputPath, IProgress<ProgressReport> progress = null)
        {
            long bytesWritten = -1;

            await Task.Run(async () =>
            {
                Sound s = new Sound();
                Sound sub = new Sound();
                bool streamSuccess = false;

                // Determine the extraction strategy.
                bool useInMemoryFallback = info.Type == SOUND_TYPE.MPEG || ((uint)info.Mode & (uint)FsbModeFlags.ImaAdpcm) != 0;

                // Attempt the primary, high-performance stream-to-disk method first.
                if (!useInMemoryFallback)
                {
                    try
                    {
                        // Lock the FMOD Core System to ensure thread-safe API calls from this background task.
                        lock (_coreSystemLock)
                        {
                            CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO
                            {
                                cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                                fileoffset = (uint)info.FileOffset
                            };
                            RESULT res = _coreSystem.createSound(info.SourcePath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS, ref ex, out s);

                            if (res == RESULT.OK)
                            {
                                s.getNumSubSounds(out int num);
                                if (info.Index < num)
                                {
                                    s.getSubSound(info.Index, out sub);
                                }
                                else
                                {
                                    sub = s;
                                }
                            }
                        }

                        if (sub.hasHandle())
                        {
                            bytesWritten = await ExtractSoundDataAsync(sub, info, outputPath, progress);
                            streamSuccess = (bytesWritten >= 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the failure and allow the process to continue to the fallback method.
                        System.Diagnostics.Debug.WriteLine($"Synchronous extraction failed: {ex.Message}");
                        streamSuccess = false;
                    }
                    finally
                    {
                        // Ensure FMOD resources are always released safely.
                        lock (_coreSystemLock)
                        {
                            if (sub.hasHandle() && sub.handle != s.handle)
                            {
                                sub.release();
                            }
                            Utilities.SafeRelease(ref s);
                        }
                    }
                }

                // If the primary streaming method failed or was bypassed, attempt the in-memory fallback.
                if (!streamSuccess)
                {
                    try
                    {
                        progress?.Report(new ProgressReport("Using fallback decoder...", 30));

                        // The fallback method decodes the entire audio stream into a byte array first.
                        byte[] wavData = Utilities.GetDecodedWavBytes(_coreSystem, _coreSystemLock, info);

                        if (wavData != null)
                        {
                            double sizeMb = wavData.Length / AppConstants.BytesToMegabytes;
                            progress?.Report(new ProgressReport($"Writing {sizeMb:F2} MB to disk...", 80));

                            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, AppConstants.BufferSizeSmall, FileOptions.Asynchronous))
                            {
                                await fs.WriteAsync(wavData, 0, wavData.Length);
                            }
                            bytesWritten = wavData.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fallback extraction failed: {ex.Message}");
                        bytesWritten = -1;
                    }
                }
            });

            progress?.Report(new ProgressReport("Complete", 100));
            if (bytesWritten <= 0)
            {
                throw new Exception($"Failed to extract audio: {info.Name}");
            }
            return bytesWritten;
        }

        #endregion

        #region 5. Private Helper Methods

        /// <summary>
        /// Extracts decoded PCM audio data from a given FMOD Sound handle and writes it to a WAV file.
        /// </summary>
        /// <param name="sound">The FMOD Sound handle (typically a subsound) to extract. Must be a valid handle.</param>
        /// <param name="info">Metadata about the audio, used for fallback values like frequency. Must not be null.</param>
        /// <param name="outputPath">The destination path for the WAV file. Must not be null.</param>
        /// <param name="progress">The progress reporter. Can be null.</param>
        /// <returns>The number of bytes written to the file, or -1 on failure.</returns>
        private async Task<long> ExtractSoundDataAsync(Sound sound, AudioInfo info, string outputPath, IProgress<ProgressReport> progress)
        {
            long bytesWritten = 0;
            try
            {
                // Retrieve audio format metadata directly from the FMOD sound handle.
                sound.getLength(out uint lenBytes, TIMEUNIT.PCMBYTES);
                sound.getFormat(out _, out SOUND_FORMAT fmt, out int ch, out int bits);
                sound.getDefaults(out float rate, out _);

                // FMOD can sometimes return an invalid default rate; fall back to our parsed metadata.
                if (rate < MIN_VALID_SAMPLE_RATE)
                {
                    rate = info.Frequency;
                }

                progress?.Report(new ProgressReport("Writing to disk...", 0));

                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, AppConstants.BufferSizeLarge, FileOptions.Asynchronous))
                {
                    // Create and write the WAV header first.
                    byte[] header = Utilities.CreateWavHeader((int)lenBytes, (int)rate, ch, bits > 0 ? bits : 16, fmt == SOUND_FORMAT.PCMFLOAT);
                    await fs.WriteAsync(header, 0, header.Length);

                    // Reset the read position to the start of the sub-sound's data.
                    // This is critical when reusing a parent container handle to prevent reading stale or incorrect data.
                    lock (_coreSystemLock)
                    {
                        sound.seekData(0);
                    }

                    // Read the decoded PCM data in chunks and write to the file.
                    byte[] buf = new byte[AppConstants.BufferSizeLarge];
                    uint totalRead = 0;

                    while (totalRead < lenBytes)
                    {
                        uint read = 0;
                        lock (_coreSystemLock)
                        {
                            sound.readData(buf, out read);
                        }
                        if (read == 0)
                        {
                            break;
                        }

                        await fs.WriteAsync(buf, 0, (int)read);
                        totalRead += read;

                        // Report real-time progress in megabytes for better user feedback on large files.
                        if (lenBytes > 0)
                        {
                            double currentMb = totalRead / AppConstants.BytesToMegabytes;
                            double totalMb = lenBytes / AppConstants.BytesToMegabytes;
                            int percent = (int)((double)totalRead / lenBytes * 100);
                            progress?.Report(new ProgressReport($"{currentMb:F2} MB / {totalMb:F2} MB", percent));
                        }
                    }
                    bytesWritten = fs.Length;
                }
                return bytesWritten;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExtractSoundDataAsync Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Recursively traverses a collection of TreeNodes and appends their data to a StringBuilder for CSV export.
        /// </summary>
        /// <param name="nodes">The collection of TreeNodes to process.</param>
        /// <param name="sb">The StringBuilder to append CSV lines to. Must not be null.</param>
        /// <param name="path">The current hierarchical path string, used to build the full path for the current node.</param>
        private void ExportNodesRecursive(TreeNodeCollection nodes, StringBuilder sb, string path)
        {
            foreach (TreeNode node in nodes)
            {
                string currentPath = string.IsNullOrEmpty(path) ? node.Text : $"{path}/{node.Text}";
                if (node.Tag is NodeData data)
                {
                    var fields = new List<string>(26);

                    // Common fields.
                    fields.Add(data.Type.ToString());
                    fields.Add(Utilities.SanitizeCsvField(currentPath));
                    fields.Add(Utilities.SanitizeCsvField(node.Text));

                    if (data is AudioDataNode audioNode)
                    {
                        AudioInfo info = audioNode.CachedAudio;
                        var details = audioNode.GetDetails();
                        string GetDetailValue(string group, string propName)
                        {
                            var detail = details.FirstOrDefault(d => d.Key.Equals(group, StringComparison.OrdinalIgnoreCase) && d.Value.StartsWith(propName, StringComparison.OrdinalIgnoreCase));
                            return detail.Value?.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "";
                        }
                        string encoding = GetDetailValue("Format", "Encoding");
                        string container = GetDetailValue("Format", "Container");

                        // Add fields in the exact order of the header.
                        fields.Add(info.Index.ToString());
                        fields.Add(Utilities.SanitizeCsvField(Path.GetFileName(info.SourcePath)));
                        fields.Add(info.LengthMs.ToString());
                        fields.Add(info.LengthPcm.ToString());
                        fields.Add(encoding);
                        fields.Add(container);
                        fields.Add(info.Channels.ToString());
                        fields.Add(info.Bits.ToString());
                        fields.Add(info.Frequency.ToString());
                        fields.Add(info.Priority.ToString());
                        fields.Add(Utilities.SanitizeCsvField(info.Mode.ToString()));
                        fields.Add(info.LoopStart.ToString());
                        fields.Add(info.LoopEnd.ToString());
                        fields.Add(info.MinDistance3D.ToString("F2"));
                        fields.Add(info.MaxDistance3D.ToString("F2"));
                        fields.Add(info.InsideConeAngle.ToString("F2"));
                        fields.Add(info.OutsideConeAngle.ToString("F2"));
                        fields.Add(info.OutsideVolume.ToString("F2"));
                        fields.Add(info.MusicChannelCount > 0 ? info.MusicChannelCount.ToString() : "");
                        fields.Add(info.MusicChannelCount > 0 ? info.MusicSpeed.ToString("F2") : "");
                        fields.Add((info.Tags != null && info.Tags.Count > 0) ? Utilities.SanitizeCsvField(string.Join("; ", info.Tags)) : "");
                        fields.Add((info.SyncPoints != null && info.SyncPoints.Count > 0) ? Utilities.SanitizeCsvField(string.Join("; ", info.SyncPoints)) : "");
                        fields.Add("");
                    }
                    else
                    {
                        string guid = "";
                        string source = "";
                        string duration = "";

                        if (data is EventNode eventNode && eventNode.EventObject.isValid())
                        {
                            eventNode.EventObject.getID(out GUID id);
                            guid = Utilities.GuidToString(id);
                            eventNode.EventObject.getLength(out int len);
                            duration = len.ToString();
                        }
                        else if (data is BankNode bankNode)
                        {
                            if (bankNode.BankObject.isValid())
                            {
                                bankNode.BankObject.getID(out GUID id);
                                guid = Utilities.GuidToString(id);
                            }
                            source = Utilities.SanitizeCsvField(data.ExtraInfo);
                        }

                        // Correctly pad with empty fields for non-audio nodes to align the GUID.
                        fields.Add(""); // Index
                        fields.Add(source); // Source
                        fields.Add(duration); // Duration
                        fields.Add(""); // Length(pcm)
                        fields.Add(""); // Encoding
                        fields.Add(""); // Container
                        fields.Add(""); // Channels
                        fields.Add(""); // Bits
                        fields.Add(""); // Frequency(Hz)
                        fields.Add(""); // Priority
                        fields.Add(""); // Mode
                        fields.Add(""); // LoopStart
                        fields.Add(""); // LoopEnd
                        fields.Add(""); // 3D_MinDistance
                        fields.Add(""); // 3D_MaxDistance
                        fields.Add(""); // 3D_ConeInsideAngle
                        fields.Add(""); // 3D_ConeOutsideAngle
                        fields.Add(""); // 3D_ConeOutsideVolume
                        fields.Add(""); // Music_Channels
                        fields.Add(""); // Music_Speed
                        fields.Add(""); // Tags
                        fields.Add(""); // SyncPoints
                        fields.Add(guid); // GUID
                    }
                    sb.AppendLine(string.Join(",", fields));
                }
                if (node.Nodes.Count > 0)
                {
                    ExportNodesRecursive(node.Nodes, sb, currentPath);
                }
            }
        }

        #endregion
    }
}