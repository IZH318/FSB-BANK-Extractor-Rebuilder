/**
 * @file AssetLoader.cs
 * @brief Handles the loading, parsing, and analysis of FMOD .bank and .fsb files.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This class is responsible for asynchronously discovering and analyzing audio assets from user-provided paths.
 * It employs a hybrid approach, using the FMOD API for modern FSB5 containers and a custom binary parser for
 * legacy FSB3 and FSB4 formats. The results are structured into a hierarchical collection of TreeNode objects
 * suitable for display in a TreeView control.
 *
 * Key Features:
 *  - Asynchronous Loading: Performs all file I/O and parsing on background threads to prevent UI freezing.
 *  - Parallel Analysis: Utilizes parallel processing to scan multiple files concurrently.
 *  - Hybrid Parsing Engine: Dispatches parsing logic between FMOD API (FSB5) and manual binary reading (FSB3/4).
 *  - Recursive Discovery: Scans directories recursively to find all compatible audio containers.
 *  - Structural Marshaling: Uses defined structs to safely read legacy binary formats.
 *
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11
 *  - Target Framework: .NET Framework 4.8
 *  - Architecture: Any CPU (Optimized for x64)
 *  - Last Update: 2025-01-08
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FMOD; // Core API
using FMOD.Studio; // Studio API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Manages the discovery, loading, and parsing of FMOD audio assets.
    /// </summary>
    public class AssetLoader
    {
        #region 1. Constants & Fields

        /// <summary>
        /// Defines the maximum number of threads to use when analyzing files in parallel.
        /// </summary>
        /// <remarks>
        /// Limiting this to the processor count avoids context switching overhead, although I/O is often the bottleneck.
        /// </remarks>
        private static readonly int MAX_PARALLEL_FILES = Environment.ProcessorCount;

        /// <summary>
        /// Defines the minimum number of bytes required to validate a potential FSB header.
        /// </summary>
        private const int MIN_HEADER_CHECK_SIZE = 32;

        /// <summary>
        /// Defines the default sample rate to use if the legacy header specifies zero.
        /// </summary>
        private const int DEFAULT_FREQUENCY = 44100;

        /// <summary>
        /// Defines the buffer size for retrieving internal FMOD names.
        /// </summary>
        private const int MAX_NAME_BUFFER = 256;

        /// <summary>
        /// The FMOD Studio System instance for high-level operations.
        /// </summary>
        private readonly FMOD.Studio.System _studioSystem;

        /// <summary>
        /// The FMOD Core System instance for low-level audio processing.
        /// </summary>
        private readonly FMOD.System _coreSystem;

        /// <summary>
        /// A synchronization lock object to ensure thread-safe access to FMOD API calls.
        /// </summary>
        private readonly object _fmodLock;

        /// <summary>
        /// A volatile flag to signal that the application is closing, used for graceful cancellation.
        /// </summary>
        private volatile bool _isClosing = false;

        #endregion

        #region 2. Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetLoader"/> class.
        /// </summary>
        /// <param name="studioSystem">The shared FMOD Studio System instance.</param>
        /// <param name="coreSystem">The shared FMOD Core System instance.</param>
        /// <param name="syncLock">The synchronization object for thread-safe FMOD calls.</param>
        public AssetLoader(FMOD.Studio.System studioSystem, FMOD.System coreSystem, object syncLock)
        {
            _studioSystem = studioSystem;
            _coreSystem = coreSystem;
            _fmodLock = syncLock;
        }

        #endregion

        #region 3. Public API

        /// <summary>
        /// Asynchronously loads and analyzes all FMOD assets from the specified input paths.
        /// </summary>
        /// <param name="inputPaths">An enumerable collection of file and/or directory paths to scan.</param>
        /// <param name="progress">An IProgress provider to report status and percentage updates to the UI.</param>
        /// <param name="token">A CancellationToken to signal when the operation should be aborted.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is a tuple containing:
        /// - A list of root <see cref="TreeNode"/> objects representing the loaded assets.
        /// - A thread-safe bag of tuples, each containing a file path that failed to load and the associated exception.
        /// </returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Unload all previously loaded banks to ensure a clean state.
        ///  2) Discover all relevant .bank and .fsb files from the input paths.
        ///  3) Pre-load all strings banks to resolve event and bus names automatically.
        ///  4) Run the main analysis loop on a background thread using parallel processing.
        ///  5) Perform final logical post-processing and update the UI structure.
        /// </remarks>
        public async Task<(List<TreeNode> Nodes, ConcurrentBag<(string FilePath, Exception ex)> FailedFiles)> LoadAssetsAsync(
            IEnumerable<string> inputPaths,
            IProgress<ProgressReport> progress,
            CancellationToken token)
        {
            var failedFiles = new ConcurrentBag<(string FilePath, Exception ex)>();
            token.Register(() => _isClosing = true);

            // Step 1: Unload all previously loaded banks to ensure a clean state.
            // This prevents ID collisions and ensures we aren't referencing stale data.
            if (_studioSystem.isValid())
            {
                _studioSystem.unloadAll();
            }

            progress?.Report(new ProgressReport("[SCANNING] Discovering files in selected paths...", 2));

            // Step 2: Discover all relevant .bank and .fsb files from the input paths.
            // This separates file discovery logic from analysis logic for cleaner separation of concerns.
            var (allContentFiles, allStringsBanks) = await DiscoverFilesAsync(inputPaths, failedFiles);
            int totalFilesToScan = allContentFiles.Count;

            if (totalFilesToScan == 0)
            {
                progress?.Report(new ProgressReport("[INFO] No compatible .bank or .fsb files were found.", 100));
                return (new List<TreeNode>(), failedFiles);
            }

            // Step 3: Pre-load all strings banks to resolve event and bus names automatically.
            // Strings banks must be loaded before main banks so FMOD can map GUIDs to paths/names.
            progress?.Report(new ProgressReport($"[PRE-PROCESSING] Loading {allStringsBanks.Count} strings bank(s)...", 7));
            foreach (string sb in allStringsBanks)
            {
                if (_studioSystem.isValid())
                {
                    _studioSystem.loadBankFile(sb, LOAD_BANK_FLAGS.NORMAL, out _);
                }
            }

            progress?.Report(new ProgressReport("[ANALYZING] Starting parallel file analysis...", 10));

            var resultNodes = new ConcurrentBag<TreeNode>();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MAX_PARALLEL_FILES, CancellationToken = token };
            int processedFilesCount = 0;

            // Step 4: Run the main analysis loop on a background thread using parallel processing.
            // Parallel.ForEach is used to maximize CPU usage during parsing, though disk I/O may still be a bottleneck.
            await Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(allContentFiles, parallelOptions, (filePath) =>
                    {
                        token.ThrowIfCancellationRequested();

                        // Thread-safe counter increment.
                        int currentFileIndex = Interlocked.Increment(ref processedFilesCount);
                        string fileName = Path.GetFileName(filePath);

                        // Define a local progress reporter for this specific file context.
                        Action<string, int> singleFileProgress = (subStatus, subProgress) =>
                        {
                            double fileProgressStart = AppConstants.ProgressWeightInit + ((double)(currentFileIndex - 1) / totalFilesToScan * AppConstants.ProgressWeightAnalysis);
                            double fileProgressRange = AppConstants.ProgressWeightAnalysis / totalFilesToScan;
                            int overallProgress = subProgress >= 0 ? (int)(fileProgressStart + (subProgress / 100.0 * fileProgressRange)) : -1;
                            string statusText = $"[ANALYZING] [{currentFileIndex}/{totalFilesToScan}] {fileName} | {subStatus}";
                            progress?.Report(new ProgressReport(statusText, overallProgress));
                        };

                        try
                        {
                            singleFileProgress("Initializing analysis...", 0);

                            TreeNode rootNode = new TreeNode(fileName, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.File, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.File);
                            string ext = Path.GetExtension(filePath).ToLower();

                            if (ext == ".bank")
                            {
                                AnalyzeBankFile(filePath, rootNode, singleFileProgress);
                            }
                            else if (ext == ".fsb")
                            {
                                AnalyzeFsbFile(filePath, rootNode, singleFileProgress);
                            }

                            singleFileProgress("Analysis complete.", 100);
                            resultNodes.Add(rootNode);
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add((filePath, ex));
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    _isClosing = true;
                }
            }, token);

            if (_isClosing)
            {
                return (new List<TreeNode>(), failedFiles);
            }

            // Step 5: Perform final logical post-processing and update the UI structure.
            // We sort nodes alphabetically and attach FMOD Studio logical data (events) to Bank nodes.
            var nodesList = resultNodes.OrderBy(n => n.Text).ToList();

            progress?.Report(new ProgressReport("[FINALIZING] Building logical structure...", 98));

            foreach (TreeNode node in nodesList)
            {
                if (_isClosing) break;
                if (node.Tag is BankNode data)
                {
                    AnalyzeBankLogic(data.ExtraInfo, node);
                }
            }

            return (nodesList, failedFiles);
        }

        #endregion

        #region 4. File Discovery & Analysis Workflow

        /// <summary>
        /// Discovers content files (.bank, .fsb) and strings banks from a list of input paths.
        /// </summary>
        /// <param name="inputPaths">A collection of file and directory paths.</param>
        /// <param name="failedFiles">A concurrent bag to store paths that could not be accessed.</param>
        /// <returns>A tuple containing a list of content files and a list of strings banks.</returns>
        private async Task<(List<string> contentFiles, List<string> stringsBanks)> DiscoverFilesAsync(IEnumerable<string> inputPaths, ConcurrentBag<(string FilePath, Exception ex)> failedFiles)
        {
            var allStringsBanks = new List<string>();
            var allContentFiles = new List<string>();

            await Task.Run(() =>
            {
                foreach (string path in inputPaths)
                {
                    if (_isClosing) break;

                    if (Directory.Exists(path))
                    {
                        try
                        {
                            allStringsBanks.AddRange(Directory.GetFiles(path, "*.strings.bank", SearchOption.AllDirectories));
                            allContentFiles.AddRange(Directory.GetFiles(path, "*.bank", SearchOption.AllDirectories));
                            allContentFiles.AddRange(Directory.GetFiles(path, "*.fsb", SearchOption.AllDirectories));
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add((path, ex));
                        }
                    }
                    else if (File.Exists(path))
                    {
                        string name = Path.GetFileName(path).ToLower();
                        if (name.EndsWith(".strings.bank"))
                        {
                            allStringsBanks.Add(path);
                        }
                        else if (name.EndsWith(".bank") || name.EndsWith(".fsb"))
                        {
                            allContentFiles.Add(path);
                        }
                    }
                }
            });

            return (
                allContentFiles.Where(f => !f.ToLower().EndsWith(".strings.bank")).Distinct().ToList(),
                allStringsBanks.Distinct().ToList()
            );
        }

        /// <summary>
        /// Analyzes a .bank file by scanning for embedded FSB data chunks.
        /// </summary>
        /// <param name="path">The full path to the .bank file.</param>
        /// <param name="root">The parent <see cref="TreeNode"/> for this bank.</param>
        /// <param name="progressReporter">A delegate for reporting progress updates.</param>
        private void AnalyzeBankFile(string path, TreeNode root, Action<string, int> progressReporter)
        {
            if (_isClosing) return;

            root.Tag = new BankNode(path);
            var fsbOffsets = new List<uint>();

            progressReporter("[ANALYZING] Scanning for embedded FSB chunks...", 10);

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // If the file is smaller than the overlap size, reliable scanning is not possible.
                    if (fs.Length < FsbSpecs.ScanOverlapSize) return;

                    byte[] buffer = new byte[AppConstants.BufferSizeLarge];
                    int bytesRead;

                    // Scan the file linearly to find the "FSB" signature.
                    // An overlap strategy is used to ensure signatures crossing buffer boundaries are not missed.
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        long bufferStartOffset = fs.Position - bytesRead;
                        int scanLimit = bytesRead - FsbSpecs.SignatureFSB5.Length;

                        for (int i = 0; i < scanLimit; i++)
                        {
                            // Check for 'F', 'S', 'B' sequence.
                            if (buffer[i] == 'F' && buffer[i + 1] == 'S' && buffer[i + 2] == 'B')
                            {
                                if (IsValidFsbHeader(buffer, i))
                                {
                                    fsbOffsets.Add((uint)(bufferStartOffset + i));
                                }
                            }
                        }

                        // Move the file pointer back by the overlap size to handle boundary cases.
                        if (fs.Position < fs.Length)
                        {
                            fs.Seek(-FsbSpecs.ScanOverlapSize, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // File access errors are expected if the file is locked or corrupt.
                // We simply return, as the file cannot be analyzed further.
                return;
            }

            progressReporter($"[ANALYZING] Found {fsbOffsets.Count} potential FSB chunk(s). Validating...", 30);
            if (fsbOffsets.Count == 0) return;

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int fsbCounter = 0;

            // Iterate through validated offsets to create nodes for each embedded FSB.
            foreach (var offset in fsbOffsets)
            {
                fsbCounter++;
                int subProgress = 30 + (int)(((float)fsbCounter / fsbOffsets.Count) * 60);
                progressReporter($"[ANALYZING] Validating chunk {fsbCounter}/{fsbOffsets.Count}...", subProgress);

                // Attempt to retrieve the internal name stored within the FSB header.
                string rawName = GetFsbInternalName(path, offset);

                if (rawName != null)
                {
                    string baseName = !string.IsNullOrEmpty(rawName)
                        ? Path.GetFileNameWithoutExtension(rawName)
                        : $"{Path.GetFileNameWithoutExtension(path)}_{offset:X}";

                    // Ensure unique names for UI display.
                    string finalName = baseName + ".fsb";
                    int dupeCounter = 1;
                    while (usedNames.Contains(finalName))
                    {
                        finalName = $"{baseName}_{dupeCounter++}.fsb";
                    }
                    usedNames.Add(finalName);

                    TreeNode fsbNode = new TreeNode(finalName, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Folder, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Folder)
                    {
                        Tag = new FsbFileNode(path, offset)
                    };
                    root.Nodes.Add(fsbNode);

                    ParseFsbFromSource(path, offset, fsbNode, progressReporter);
                }
            }
        }

        /// <summary>
        /// Validates if the data at a given buffer index represents a valid FSB header.
        /// </summary>
        /// <param name="buffer">The byte buffer containing the potential header data.</param>
        /// <param name="index">The starting index of the "FSB" signature in the buffer.</param>
        /// <returns><c>true</c> if the header is valid; otherwise, <c>false</c>.</returns>
        private bool IsValidFsbHeader(byte[] buffer, int index)
        {
            try
            {
                // Ensure there is enough data in the buffer to read a minimal header.
                if (index + MIN_HEADER_CHECK_SIZE > buffer.Length) return false;

                // Double-check signature.
                if (buffer[index] != 'F' || buffer[index + 1] != 'S' || buffer[index + 2] != 'B') return false;

                byte versionChar = buffer[index + 3];

                if (versionChar == '5')
                {
                    int numSamples = BitConverter.ToInt32(buffer, index + FsbSpecs.Offset_0x08);
                    if (numSamples <= 0) return false;

                    uint sampleHeadersSize = BitConverter.ToUInt32(buffer, index + FsbSpecs.Offset_0x0C);
                    uint dataSize = BitConverter.ToUInt32(buffer, index + FsbSpecs.Offset_0x10);

                    if (sampleHeadersSize == 0 || dataSize == 0) return false;
                    return true;
                }
                else if (versionChar >= '2' && versionChar <= '4')
                {
                    int numSamples = BitConverter.ToInt32(buffer, index + FsbSpecs.Offset_FSB4_NumSamples);
                    int shdrSize = BitConverter.ToInt32(buffer, index + FsbSpecs.Offset_FSB4_SHdrSize);
                    int dataSize = BitConverter.ToInt32(buffer, index + FsbSpecs.Offset_FSB4_DataSize);

                    if (numSamples <= 0 || shdrSize <= 0 || dataSize <= 0) return false;

                    // FSB3/4 requires sample headers to be aligned.
                    if (shdrSize % numSamples != 0) return false;

                    int singleHeaderSize = shdrSize / numSamples;
                    if (singleHeaderSize < FsbSpecs.MinSampleHeaderSize || singleHeaderSize > FsbSpecs.MaxSampleHeaderSize) return false;

                    return true;
                }
                else if (versionChar == '1')
                {
                    // FSB1 validation is less strict due to format simplicity.
                    int numSamples = BitConverter.ToInt32(buffer, index + FsbSpecs.Offset_FSB3_NumSamples);
                    if (numSamples <= 0) return false;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Any exception during parsing indicates an invalid header.
                return false;
            }
        }

        /// <summary>
        /// Analyzes a standalone .fsb file.
        /// </summary>
        /// <param name="path">The full path to the .fsb file.</param>
        /// <param name="root">The parent <see cref="TreeNode"/> for this FSB.</param>
        /// <param name="progressReporter">A delegate for reporting progress updates.</param>
        private void AnalyzeFsbFile(string path, TreeNode root, Action<string, int> progressReporter)
        {
            if (_isClosing) return;
            root.Tag = new FsbFileNode(path, 0);
            ParseFsbFromSource(path, 0, root, progressReporter);
        }

        #endregion

        #region 5. FSB Parsing Methods

        /// <summary>
        /// Determines the FSB version and dispatches to the appropriate parser.
        /// </summary>
        /// <param name="path">The path to the source file containing the FSB data.</param>
        /// <param name="offset">The starting offset of the FSB data within the file.</param>
        /// <param name="parentNode">The parent <see cref="TreeNode"/> to which sub-sounds will be added.</param>
        /// <param name="progressReporter">A delegate for reporting progress updates.</param>
        private void ParseFsbFromSource(string path, uint offset, TreeNode parentNode, Action<string, int> progressReporter)
        {
            if (_isClosing) return;

            // Populate FsbFileNode with container-level details.
            if (parentNode.Tag is FsbFileNode fsbNode)
            {
                Sound containerSound = new Sound();
                try
                {
                    lock (_fmodLock)
                    {
                        var exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };
                        if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM, ref exinfo, out containerSound) == RESULT.OK)
                        {
                            fsbNode.ContainerInfo = Utilities.GetFsbContainerInfo(containerSound);
                        }
                    }
                }
                finally
                {
                    Utilities.SafeRelease(ref containerSound);
                }
            }

            char version = Utilities.GetFsbVersion(path, offset);
            if (version == '\0')
            {
                progressReporter($"[ERROR] Could not determine FSB version at offset 0x{offset:X}", 100);
                return;
            }

            switch (version)
            {
                case '5':
                    // FSB5 is modern and well-supported by the FMOD API.
                    ParseFsbViaFmod(path, offset, parentNode, progressReporter);
                    break;

                case '3':
                case '4':
                    // Legacy versions (FSB3/4) often fail in the modern FMOD API.
                    // We dispatch to the custom binary parser for robust handling.
                    bool success = ParseLegacyFsb(path, offset, parentNode, version, progressReporter);
                    if (!success)
                    {
                        progressReporter($"[ERROR] Failed to manually parse FSB version '{version}'.", 100);
                    }
                    break;

                default:
                    // For unknown or very old versions, attempt the API as a fallback.
                    progressReporter($"[WARNING] Unsupported FSB version '{version}'. Attempting to parse with FMOD API...", 50);
                    ParseFsbViaFmod(path, offset, parentNode, progressReporter);
                    break;
            }
        }

        /// <summary>
        /// Parses an FSB container using the FMOD Core API.
        /// </summary>
        /// <param name="path">The path to the source file.</param>
        /// <param name="offset">The starting offset of the FSB data.</param>
        /// <param name="parentNode">The parent node for the sub-sounds.</param>
        /// <param name="progressReporter">A delegate for reporting progress.</param>
        private void ParseFsbViaFmod(string path, uint offset, TreeNode parentNode, Action<string, int> progressReporter)
        {
            Sound sound = new Sound();
            Sound subSound = new Sound();

            try
            {
                List<TreeNode> newNodes = new List<TreeNode>();

                lock (_fmodLock)
                {
                    var exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };

                    // Open the sound ignoring tags to speed up loading and avoid metadata parsing overhead.
                    if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM | MODE.IGNORETAGS, ref exinfo, out sound) == RESULT.OK)
                    {
                        sound.getNumSubSounds(out int numSub);

                        if (numSub > 0)
                        {
                            for (int i = 0; i < numSub; i++)
                            {
                                if (_isClosing) break;

                                int subSoundProgress = (int)((float)(i + 1) / numSub * 100);
                                progressReporter($"Processing sub-sound {i + 1}/{numSub}", subSoundProgress);

                                sound.getSubSound(i, out subSound);
                                AudioInfo info = Utilities.GetAudioInfo(subSound, i, path, offset);

                                string displayName = string.IsNullOrEmpty(info.Name) ? $"Sub_{i}" : info.Name;
                                TreeNode node = new TreeNode(displayName, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio)
                                {
                                    Tag = new AudioDataNode(info, offset, path)
                                };
                                newNodes.Add(node);

                                Utilities.SafeRelease(ref subSound);
                            }
                        }
                        else
                        {
                            // Handle cases where the FSB is a single stream without sub-sounds.
                            AudioInfo info = Utilities.GetAudioInfo(sound, 0, path, offset);
                            if (info.LengthMs > 0)
                            {
                                string displayName = string.IsNullOrEmpty(info.Name) ? Path.GetFileNameWithoutExtension(path) : info.Name;
                                TreeNode node = new TreeNode(displayName, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio)
                                {
                                    Tag = new AudioDataNode(info, offset, path)
                                };
                                newNodes.Add(node);
                            }
                        }
                    }
                }

                if (newNodes.Count > 0)
                {
                    parentNode.Nodes.AddRange(newNodes.ToArray());
                }
            }
            finally
            {
                Utilities.SafeRelease(ref sound);
            }
        }

        #region Internal Structs for Legacy Parsing

        /// <summary>
        /// Represents the binary header structure for FSB version 4 files.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FSB4Header
        {
            /// <summary>
            /// The 4-byte file signature (e.g., "FSB4").
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;

            /// <summary>
            /// The number of samples contained in the file.
            /// </summary>
            public int NumSamples;

            /// <summary>
            /// The size of the sample headers block in bytes.
            /// </summary>
            public int SHdrSize;

            /// <summary>
            /// The size of the data block in bytes.
            /// </summary>
            public int DataSize;

            /// <summary>
            /// The version identifier.
            /// </summary>
            public uint Version;

            /// <summary>
            /// The mode flags defining the format characteristics.
            /// </summary>
            public uint Mode;

            /// <summary>
            /// Hash/GUID skipped in legacy parsing.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] _reserved;
        }

        /// <summary>
        /// Represents the binary header structure for FSB version 3 files.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FSB3Header
        {
            /// <summary>
            /// The 4-byte file signature (e.g., "FSB3").
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;

            /// <summary>
            /// The number of samples contained in the file.
            /// </summary>
            public int NumSamples;

            /// <summary>
            /// The size of the sample headers block in bytes.
            /// </summary>
            public int SHdrSize;

            /// <summary>
            /// The size of the data block in bytes.
            /// </summary>
            public int DataSize;

            /// <summary>
            /// The version identifier.
            /// </summary>
            public uint Version;

            /// <summary>
            /// The mode flags defining the format characteristics.
            /// </summary>
            public uint Mode;
        }

        /// <summary>
        /// Represents the header information for a single sample in legacy FSB formats.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LegacySampleHeader
        {
            /// <summary>
            /// The size of this header structure in bytes (varies by version).
            /// </summary>
            public ushort Size;

            /// <summary>
            /// The name of the sample.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FsbSpecs.NameFieldLength)]
            public byte[] Name;

            /// <summary>
            /// The length of the audio data in samples (PCM).
            /// </summary>
            public uint LengthSamples;

            /// <summary>
            /// The compressed size of the audio data in bytes.
            /// </summary>
            public uint LengthCompressedBytes;

            /// <summary>
            /// The loop start point in samples.
            /// </summary>
            public uint LoopStart;

            /// <summary>
            /// The loop end point in samples.
            /// </summary>
            public uint LoopEnd;

            /// <summary>
            /// The mode flags specific to this sample.
            /// </summary>
            public uint Mode;

            /// <summary>
            /// The default playback frequency (sample rate).
            /// </summary>
            public int DefFreq;

            /// <summary>
            /// The default volume level.
            /// </summary>
            public ushort DefVol;

            /// <summary>
            /// The default pan position.
            /// </summary>
            public ushort DefPan;

            /// <summary>
            /// The default priority.
            /// </summary>
            public ushort DefPri;

            /// <summary>
            /// The number of audio channels.
            /// </summary>
            public ushort NumChannels;
        }

        #endregion

        /// <summary>
        /// Parses a legacy FSB (version 3 or 4) container by reading its binary structure using Marshaling.
        /// </summary>
        /// <param name="path">The path to the source file.</param>
        /// <param name="offset">The starting offset of the FSB data.</param>
        /// <param name="parentNode">The parent node for the sub-sounds.</param>
        /// <param name="version">The FSB version character ('3' or '4').</param>
        /// <param name="progressReporter">A delegate for reporting progress.</param>
        /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
        private bool ParseLegacyFsb(string path, uint offset, TreeNode parentNode, char version, Action<string, int> progressReporter)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek(offset, SeekOrigin.Begin);

                    int numSamples = 0;
                    int shdrSize = 0;
                    long sampleHeaderStart = 0;
                    long dataStartOffset = 0;
                    uint globalMode = 0;

                    // Step 1: Read the main FSB header based on the version.
                    // The structures differ slightly between FSB3 and FSB4, requiring separate marshaling logic.
                    if (version == '4')
                    {
                        var header = Utilities.ReadStruct<FSB4Header>(br);
                        numSamples = header.NumSamples;
                        shdrSize = header.SHdrSize;
                        globalMode = header.Mode;
                        sampleHeaderStart = fs.Position;
                        dataStartOffset = offset + FsbSpecs.HeaderSize_FSB4 + shdrSize;
                    }
                    else if (version == '3')
                    {
                        var header = Utilities.ReadStruct<FSB3Header>(br);
                        numSamples = header.NumSamples;
                        shdrSize = header.SHdrSize;
                        globalMode = header.Mode;
                        sampleHeaderStart = fs.Position;
                        dataStartOffset = offset + FsbSpecs.HeaderSize_FSB3 + shdrSize;
                    }
                    else
                    {
                        return false;
                    }

                    long currentDataPointer = dataStartOffset;

                    // Step 2: Iterate through all sample headers.
                    // Legacy formats store all headers contiguously before the audio data block.
                    for (int i = 0; i < numSamples; i++)
                    {
                        fs.Seek(sampleHeaderStart, SeekOrigin.Begin);

                        var sampleHeader = Utilities.ReadStruct<LegacySampleHeader>(br);
                        int nextHeaderOffset = (int)fs.Position - Marshal.SizeOf(typeof(LegacySampleHeader)) + sampleHeader.Size;
                        string name = Encoding.ASCII.GetString(sampleHeader.Name).TrimEnd('\0');

                        // Construct the AudioInfo object from the raw header data.
                        var info = new AudioInfo
                        {
                            Name = string.IsNullOrEmpty(name) ? $"Sample_{i}" : name,
                            Index = i,
                            SourcePath = path,
                            FileOffset = offset,
                            DataOffset = (uint)(currentDataPointer - offset),
                            DataLength = sampleHeader.LengthCompressedBytes,
                            Channels = sampleHeader.NumChannels > 0 ? sampleHeader.NumChannels : 1,
                            LengthPcm = sampleHeader.LengthSamples,
                            Bits = 16,
                            Frequency = sampleHeader.DefFreq > 0 ? sampleHeader.DefFreq : DEFAULT_FREQUENCY,
                            Type = SOUND_TYPE.RAW,
                            Format = SOUND_FORMAT.NONE,
                            Mode = (MODE)sampleHeader.Mode,
                            LoopStart = sampleHeader.LoopStart,
                            LoopEnd = sampleHeader.LoopEnd > sampleHeader.LengthSamples ? sampleHeader.LengthSamples : sampleHeader.LoopEnd
                        };

                        // Decode flag bits to determine audio properties.
                        if ((sampleHeader.Mode & (uint)FsbModeFlags.Bits8) != 0)
                        {
                            info.Bits = 8;
                        }
                        if ((sampleHeader.Mode & (uint)FsbModeFlags.Mono) != 0)
                        {
                            info.Channels = 1;
                        }
                        if ((sampleHeader.Mode & (uint)FsbModeFlags.Stereo) != 0)
                        {
                            info.Channels = 2;
                        }

                        // Convert loop points from samples to milliseconds for consistency.
                        if (info.Frequency > 0)
                        {
                            info.LoopStart = (uint)(info.LoopStart * 1000.0 / info.Frequency);
                            info.LoopEnd = (uint)(info.LoopEnd * 1000.0 / info.Frequency);
                        }
                        else
                        {
                            info.LoopStart = 0;
                            info.LoopEnd = 0;
                        }

                        // Determine the specific audio format (e.g., MPEG, ADPCM, XMA).
                        if ((sampleHeader.Mode & (uint)FsbModeFlags.MpegPadded) != 0 || (sampleHeader.Mode & (uint)FsbModeFlags.Mpeg) != 0)
                        {
                            info.Type = SOUND_TYPE.MPEG;
                            info.Format = SOUND_FORMAT.BITSTREAM;
                        }
                        else if ((sampleHeader.Mode & (uint)FsbModeFlags.ImaAdpcm) != 0)
                        {
                            info.Type = SOUND_TYPE.RAW;
                            info.Format = SOUND_FORMAT.BITSTREAM;
                        }
                        else if ((sampleHeader.Mode & (uint)FsbModeFlags.Xma) != 0)
                        {
                            info.Type = SOUND_TYPE.XMA;
                        }
                        else if ((sampleHeader.Mode & (uint)FsbModeFlags.Vag) != 0)
                        {
                            info.Format = SOUND_FORMAT.BITSTREAM;
                        }
                        else
                        {
                            if (info.Bits == 8)
                            {
                                info.Format = SOUND_FORMAT.PCM8;
                            }
                            else
                            {
                                info.Format = SOUND_FORMAT.PCM16;
                            }
                        }

                        var node = new TreeNode(info.Name, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Audio)
                        {
                            Tag = new AudioDataNode(info, offset, path)
                        };
                        parentNode.Nodes.Add(node);

                        // Advance pointers for the next iteration.
                        sampleHeaderStart = nextHeaderOffset;
                        currentDataPointer += sampleHeader.LengthCompressedBytes;

                        // Step 3: Handle data alignment.
                        // Some legacy formats enforce alignment boundaries (e.g., 32-byte) for data chunks.
                        if (version == '4' || (globalMode & (uint)FsbModeFlags.Stereo) != 0)
                        {
                            if ((currentDataPointer % FsbSpecs.LegacyAlignment) != 0)
                            {
                                currentDataPointer += FsbSpecs.LegacyAlignment - (currentDataPointer % FsbSpecs.LegacyAlignment);
                            }
                        }
                    }
                }

                progressReporter($"Manually parsed FSB{version}.", 100);
                return true;
            }
            catch (Exception)
            {
                // Silently fail on manual parse errors; the caller will handle the boolean result.
                return false;
            }
        }

        #endregion

        #region 6. FMOD Studio Logic Analysis

        /// <summary>
        /// Analyzes a loaded bank to extract high-level FMOD Studio information like events.
        /// </summary>
        /// <param name="path">The path to the bank file.</param>
        /// <param name="root">The root node representing the bank.</param>
        private void AnalyzeBankLogic(string path, TreeNode root)
        {
            if (_isClosing || !_studioSystem.isValid()) return;

            RESULT res = _studioSystem.loadBankFile(path, LOAD_BANK_FLAGS.NORMAL, out Bank bank);

            // Handle cases where the bank is already loaded in memory (e.g., as a strings bank).
            if (res == RESULT.ERR_EVENT_ALREADY_LOADED)
            {
                _studioSystem.getBankList(out Bank[] loaded);
                foreach (var b in loaded)
                {
                    b.getPath(out string p);
                    if (Path.GetFileName(p) == Path.GetFileName(path))
                    {
                        bank = b;
                        res = RESULT.OK;
                        break;
                    }
                }
            }

            if (res == RESULT.OK)
            {
                if (root.Tag is BankNode nd)
                {
                    nd.FmodObject = bank;
                }

                bank.getEventCount(out int evtCount);

                // Populate event nodes if events are present.
                if (evtCount > 0)
                {
                    var evtGroup = new TreeNode($"Events ({evtCount})", FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Folder, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Folder);
                    root.Nodes.Insert(0, evtGroup);

                    bank.getEventList(out EventDescription[] events);
                    foreach (var evt in events)
                    {
                        evt.getPath(out string p);
                        evt.getID(out GUID id);

                        // Use the full path if available; otherwise fall back to the GUID string.
                        string name = string.IsNullOrEmpty(p) ? Utilities.GuidToString(id) : p.Substring(p.LastIndexOf('/') + 1);

                        var node = new TreeNode(name, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Event, FSB_BANK_Extractor_Rebuilder_CS_GUI.ImageIndex.Event)
                        {
                            Tag = new EventNode(evt)
                        };
                        evtGroup.Nodes.Add(node);
                    }
                }
            }
        }

        #endregion

        #region 7. Helpers

        /// <summary>
        /// Retrieves the internal name of an FSB container, if available.
        /// </summary>
        /// <param name="path">The path to the file containing the FSB.</param>
        /// <param name="offset">The offset of the FSB data within the file.</param>
        /// <returns>The internal name of the FSB, or null if it cannot be determined.</returns>
        private string GetFsbInternalName(string path, uint offset)
        {
            string name = null;
            Sound sound = new Sound();
            try
            {
                lock (_fmodLock)
                {
                    var exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };

                    // Attempt to open the stream to read the header metadata.
                    if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM, ref exinfo, out sound) == RESULT.OK)
                    {
                        sound.getNumSubSounds(out int numSubSounds);
                        sound.getLength(out uint length, TIMEUNIT.MS);

                        if (numSubSounds > 0 && length > 0)
                        {
                            sound.getName(out name, MAX_NAME_BUFFER);
                        }
                    }
                }
            }
            finally
            {
                Utilities.SafeRelease(ref sound);
            }
            return name;
        }

        #endregion
    }
}