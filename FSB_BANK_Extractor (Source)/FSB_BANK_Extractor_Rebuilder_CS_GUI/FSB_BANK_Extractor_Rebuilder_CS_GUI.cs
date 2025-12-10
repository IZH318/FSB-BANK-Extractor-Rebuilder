/**
* @file FSB_BANK_Extractor_Rebuilder_CS_GUI.cs
* @brief GUI tool for browsing, playing, extracting, and rebuilding audio in FMOD Sound Bank (.fsb) and Bank (.bank) files.
* @author (Github) IZH318 (https://github.com/IZH318)
*
* @details
* This application utilizes the FMOD Studio & Core API to analyze, preview, process, and modify FMOD audio containers.
* It provides a user-friendly interface to explore and alter the internal structures of .bank and .fsb files.
*
* Key Features:
*  - Advanced Analysis: Parses .bank files to identify and extract embedded .fsb containers and sub-sounds.
*  - Playback System: Integrated audio player with Seek, Loop, and Volume controls for previewing assets.
*  - Extraction: Converts proprietary FMOD audio streams into standard Waveform Audio (.wav) files.
*  - Rebuilding & Repacking: Replaces audio data within a .bank/.fsb file by rebuilding the entire FSB container.
*  - Workspace-based Workflow: Extracts all sub-sounds of a target FSB into a temporary workspace.
*  - Metadata Preservation: Saves original format, quality, and loop settings to a `manifest.json`.
*  - External Tool Integration: Utilizes the official `fsbankcl.exe` for robust and compatible FSB rebuilding.
*  - Intelligent Size Patching: Uses binary search to optimize compression quality, ensuring the rebuilt file fits within original limits.
*  - User Convenience: Supports Drag & Drop (Import/Export), Keyword Search, and Index-based selection tools.
*
* Technical Environment:
*  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
*  - Target Framework: .NET Framework 4.8
*  - Key Dependencies: Newtonsoft.Json (Required for manifest generation)
*  - Primary Test Platform: Windows 10 64-bit
*  - Last Update: 2025-12-10
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FMOD; // Core API
using FMOD.Studio; // Studio API
using Newtonsoft.Json; // Required for manifest generation.

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class FSB_BANK_Extractor_Rebuilder_CS_GUI : Form
    {
        #region 1. Configuration & Constants

        // Buffer size for file reading operations (64KB).
        private const int FILE_READ_BUFFER_SIZE = 65536;

        // Maximum number of threads to use when scanning files in parallel.
        private const int MAX_PARALLEL_FILES = 4;

        // Name of the FMOD command-line tool for rebuilding FSBs.
        private const string FsBankClExecutable = "fsbankcl.exe";

        // Image indexes for the TreeView/ListView icons.
        private static class ImageIndex
        {
            public const int File = 0;
            public const int Folder = 1;
            public const int Event = 2;
            public const int Param = 3;
            public const int Bus = 4;
            public const int Vca = 5;
            public const int Audio = 6;
        }

        private enum ExtractLocationMode { SameAsSource, CustomPath, AskEveryTime }

        // Application Metadata & Versioning Constants.
        private const string AppVersion = "3.0.0";
        private const string AppLastUpdate = "2025-12-10";
        private const string AppDeveloper = "(GitHub) IZH318";
        private const string AppWebsite = "https://github.com/IZH318";

        // FMOD Engine Constants.
        private const string FmodApiVersion = "v2.03.11";
        private const string FmodBuildNumber = "158528";

        // Helper property for the full FMOD version string.
        private static string FmodFullVersion => $"{FmodApiVersion} (Build {FmodBuildNumber})";

        #endregion

        #region 2. Fields: FMOD & State

        // FMOD System Objects.
        private FMOD.Studio.System _studioSystem;
        private FMOD.System _coreSystem;

        // Lock object to ensure thread safety when accessing the Core System.
        private static readonly object _coreSystemLock = new object();

        // Playback Objects.
        private FMOD.Channel _currentChannel;
        private FMOD.Studio.EventInstance _currentEvent;
        private FMOD.Sound _loadedSound;

        // Playback State.
        private bool _isPlaying = false;
        private uint _currentTotalLengthMs = 0;
        private bool _isDraggingSeek = false;

        // UI & Selection State.
        private NodeData _currentSelection = null;
        private List<TreeNode> _originalNodes = new List<TreeNode>(); // Backup for search filtering.
        private List<string> _tempDirectories = new List<string>(); // Track temp folders for cleanup.
        private AudioAnalyzerForm _audioAnalyzer; // Instance of the audio analysis tool.

        // Timers & Utils.
        private readonly System.Windows.Forms.Timer _uiTimer; // For FMOD updates and non-critical UI.
        private readonly System.Threading.Timer _progressTimer; // High-priority timer for progress bar and elapsed time.
        private readonly System.Windows.Forms.Timer _searchDebounceTimer; // Delays search to prevent lag.
        private readonly Stopwatch _scanStopwatch = new Stopwatch(); // Measures operation time.

        // Process Flags.
        private volatile bool _isWorking = false; // General flag for any long-running operation.
        private volatile bool _isScanning = false; // Specific flag for file scanning progress.
        private volatile bool _isClosing = false;
        private bool _isUpdatingChecks = false; // Prevents recursive check events.

        // Progress Tracking.
        private int _totalFilesToScan = 0;
        private int _processedFilesCount = 0;
        private string _currentProcessingFile = "";

        // Logging Utility.
        private LogWriter _logger;

        // Extraction Path Management.
        private string _customExtractPath = string.Empty;

        #endregion        

        #region 3. Initialization & Cleanup

        /// <summary>
        /// Initializes a new instance of the FSB_BANK_Extractor_Rebuilder_CS_GUI form.
        /// </summary>
        public FSB_BANK_Extractor_Rebuilder_CS_GUI()
        {
            InitializeComponent();

            // Unlink the ContextMenu set in the Designer to allow for dynamic menu generation.
            lvSearchResults.ContextMenuStrip = null;

            // Set up UI elements such as icons and menus.
            SetupIcons();
            InitializeUiLogic();

            // Initialize the FMOD Studio and Core audio engine systems.
            InitializeFmod();

            // Register event listeners for UI controls.
            treeViewInfo.AfterCheck += TreeViewInfo_AfterCheck;

            // Start the main application timer for all UI and engine updates.
            // This single timer handles both FMOD updates and progress display.
            _progressTimer = new System.Threading.Timer(MainTimer_Callback, null, 0, 33);

            // Configure the search debounce timer to delay search execution.
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            // Enable drag and drop functionality for the main form.
            this.AllowDrop = true;
            this.DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            this.DragDrop += MainForm_DragDropAsync;

            // Set up initial states for playback controls and other UI components.
            trackSeek.Minimum = 0;
            trackSeek.Maximum = 1000;
            chkLoop.CheckedChanged += chkLoop_CheckedChanged;

            // Enable form-level key press preview.
            this.KeyPreview = true;

            // Register event handlers for the search results ListView.
            lvSearchResults.SelectedIndexChanged += LvSearchResults_SelectedIndexChanged;
            lvSearchResults.DoubleClick += LvSearchResults_DoubleClick;
            lvSearchResults.MouseClick += LvSearchResults_MouseClick;
            lvSearchResults.ColumnClick += LvSearchResults_ColumnClick;

            // Configure the extraction location ComboBox with its options.
            cmbExtractLocation.Items.AddRange(new object[] {
                "Same as source file",
                "Custom path",
                "Ask every time"
            });
            cmbExtractLocation.SelectedIndex = 0;
            cmbExtractLocation.SelectedIndexChanged += CmbExtractLocation_SelectedIndexChanged;
        }

        /// <summary>
        /// Populates the ImageList with system icons for the UI.
        /// </summary>
        private void SetupIcons()
        {
            if (imageList1.Images.Count == 0)
            {
                imageList1.Images.Add("file", SystemIcons.WinLogo);
                imageList1.Images.Add("folder", SystemIcons.Shield);
                imageList1.Images.Add("event", SystemIcons.Exclamation);
                imageList1.Images.Add("param", SystemIcons.Question);
                imageList1.Images.Add("bus", SystemIcons.Application);
                imageList1.Images.Add("vca", SystemIcons.Hand);
                imageList1.Images.Add("audio", SystemIcons.Information);
            }
        }

        /// <summary>
        /// Initializes the FMOD Studio and Core audio engine systems.
        /// </summary>
        private void InitializeFmod()
        {
            try
            {
                CheckFmodResult(FMOD.Studio.System.create(out _studioSystem));
                CheckFmodResult(_studioSystem.getCoreSystem(out _coreSystem));
                // Initialize with default flags and a maximum of 1024 channels.
                CheckFmodResult(_studioSystem.initialize(1024, FMOD.Studio.INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, IntPtr.Zero));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FMOD Initialization Error: {ex.Message}\nThe application will exit.",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        /// <summary>
        /// Sets up additional UI logic, such as context menus and menu items.
        /// </summary>
        private void InitializeUiLogic()
        {
            SetupAdditionalContextMenu();
            SetupManualBankLoader();
            SetupHelpMenu();
            SetupAnalyzerMenu();
        }

        /// <summary>
        /// Adds custom items to the TreeView context menu.
        /// </summary>
        private void SetupAdditionalContextMenu()
        {
            if (treeViewContextMenu != null)
            {
                // Hide default items if they need to be replaced by custom logic.
                if (playContextMenuItem != null) playContextMenuItem.Visible = false;
                if (stopContextMenuItem != null) stopContextMenuItem.Visible = false;

                // Add a "Select All" item to the context menu.
                ToolStripMenuItem selectAllItem = new ToolStripMenuItem("Select All");
                selectAllItem.Click += (s, e) => CheckAllInCurrentView();
                treeViewContextMenu.Items.Insert(0, selectAllItem);

                // Add an "Index Tools" item for range selection and jumping.
                treeViewContextMenu.Items.Insert(1, new ToolStripSeparator());
                ToolStripMenuItem indexToolItem = new ToolStripMenuItem("Index Tools (Select/Jump)...");
                indexToolItem.Click += IndexToolItem_Click;
                treeViewContextMenu.Items.Insert(2, indexToolItem);

                // Add a "Rebuild" item for repacking audio.
                treeViewContextMenu.Items.Insert(3, new ToolStripSeparator());
                ToolStripMenuItem rebuildItem = new ToolStripMenuItem("Rebuild Sound with fsbankcl...");
                rebuildItem.Click += rebuildSoundContextMenuItem_Click;
                treeViewContextMenu.Items.Insert(4, rebuildItem);
            }
        }

        /// <summary>
        /// Adds a menu item for manually loading a .strings.bank file.
        /// </summary>
        private void SetupManualBankLoader()
        {
            ToolStripMenuItem manualLoadItem = new ToolStripMenuItem("Load Strings Bank (Manual)...");
            manualLoadItem.Click += (s, e) => LoadStringsBankManually();

            // Insert the new item into the "File" menu.
            if (menuStrip1.Items.Count > 0 && menuStrip1.Items[0] is ToolStripMenuItem fileMenu)
            {
                fileMenu.DropDownItems.Insert(2, manualLoadItem);
            }
        }

        /// <summary>
        /// Adds "Help" and "About" items to the main menu strip.
        /// </summary>
        private void SetupHelpMenu()
        {
            if (menuStrip1 == null || menuStrip1.Items.Count == 0) return;

            if (menuStrip1.Items[0] is ToolStripMenuItem fileMenu)
            {
                // Create the Help menu item.
                ToolStripMenuItem helpItem = new ToolStripMenuItem("Help");
                helpItem.ShortcutKeys = Keys.F1;
                helpItem.Click += (s, e) => ShowHelpForm();

                // Create the About menu item.
                ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
                aboutItem.Click += (s, e) => ShowAboutDialog();

                // Insert the new items before the "Exit" menu item.
                ToolStripSeparator separator = new ToolStripSeparator();
                int exitIndex = fileMenu.DropDownItems.IndexOf(exitToolStripMenuItem);

                if (exitIndex != -1)
                {
                    fileMenu.DropDownItems.Insert(exitIndex, helpItem);
                    fileMenu.DropDownItems.Insert(exitIndex + 1, aboutItem);
                    fileMenu.DropDownItems.Insert(exitIndex + 2, separator);
                }
                else
                {
                    fileMenu.DropDownItems.Add(new ToolStripSeparator());
                    fileMenu.DropDownItems.Add(helpItem);
                    fileMenu.DropDownItems.Add(aboutItem);
                }
            }
        }

        /// <summary>
        /// Adds a menu item for opening the Audio Analyzer tool.
        /// </summary>
        private void SetupAnalyzerMenu()
        {
            // Check if a 'Tools' menu already exists.
            ToolStripMenuItem toolsMenu = null;
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Text == "Tools")
                {
                    toolsMenu = (ToolStripMenuItem)item;
                    break;
                }
            }

            // Create 'Tools' menu if it doesn't exist.
            if (toolsMenu == null)
            {
                toolsMenu = new ToolStripMenuItem("Tools");
                menuStrip1.Items.Insert(1, toolsMenu); // Insert after 'File'.
            }

            ToolStripMenuItem analyzerItem = new ToolStripMenuItem("Audio Analyzer...");
            analyzerItem.Click += (s, e) => ShowAudioAnalyzer();
            toolsMenu.DropDownItems.Add(analyzerItem);
        }

        /// <summary>
        /// Displays the Audio Analyzer form or brings it to the front if already open.
        /// </summary>
        private void ShowAudioAnalyzer()
        {
            if (_audioAnalyzer == null || _audioAnalyzer.IsDisposed)
            {
                _audioAnalyzer = new AudioAnalyzerForm();
                _audioAnalyzer.Show();

                // If audio is currently playing, attach the analyzer immediately.
                if (_isPlaying && _currentChannel.hasHandle() && _loadedSound.hasHandle())
                {
                    _audioAnalyzer.AttachToAudio(_coreSystem, _currentChannel, _loadedSound);
                }
            }
            else
            {
                _audioAnalyzer.BringToFront();
            }
        }

        /// <summary>
        /// Releases all resources when the form is closing.
        /// </summary>
        /// <param name="e">A <see cref="FormClosingEventArgs"/> that contains the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent re-entry if already closing.
            if (_isClosing)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true; // Prevent the form from closing immediately to allow for cleanup.
            _isClosing = true;

            // Stop all timers and audio playback.
            if (_uiTimer != null) _uiTimer.Stop();
            if (_progressTimer != null) _progressTimer.Dispose();
            StopAudio();

            // Close the analyzer form if it exists and is open.
            if (_audioAnalyzer != null && !_audioAnalyzer.IsDisposed)
            {
                _audioAnalyzer.Close();
            }

            // Dispose the logger.
            if (_logger != null)
            {
                _logger.Dispose();
                _logger = null;
            }

            // Update the UI to indicate that the application is closing.
            this.Enabled = false;
            if (lblStatus != null) lblStatus.Text = "Closing application... Cleaning up resources...";
            if (progressBar != null)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Visible = true;
            }
            Application.DoEvents();

            // Run final cleanup on a background task to avoid freezing the UI.
            Task.Run(async () =>
            {
                // Release the FMOD system.
                if (_studioSystem.isValid()) _studioSystem.release();

                // Clean up all temporary directories created during the session.
                if (_tempDirectories != null)
                {
                    foreach (var dir in _tempDirectories)
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                await Task.Run(() => Directory.Delete(dir, true));
                            }
                        }
                        catch { /* Ignore cleanup errors. */ }
                    }
                }

                // Forcefully exit the application.
                Environment.Exit(0);
            });
        }

        #endregion

        #region 4. UI Interaction & Input

        /// <summary>
        /// Processes command keys for shortcuts like Ctrl+F.
        /// </summary>
        /// <param name="msg">A <see cref="Message"/>, passed by reference, that represents the window message to process.</param>
        /// <param name="keyData">One of the <see cref="Keys"/> values that represents the key to process.</param>
        /// <returns>true if the keystroke was processed and consumed; otherwise, false to allow further processing.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle the search shortcut (Ctrl + F).
            if (keyData == (Keys.Control | Keys.F))
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Toggles the enabled state of the main UI components.
        /// </summary>
        /// <param name="enabled">A boolean indicating whether the UI should be enabled.</param>
        private void SetUiState(bool enabled)
        {
            if (_isClosing || this.IsDisposed) return;

            menuStrip1.Enabled = enabled;
            panelPlayback.Enabled = enabled;
            treeViewInfo.Enabled = enabled;
            lvSearchResults.Enabled = enabled;
            listViewDetails.Enabled = enabled;
            panelSearch.Enabled = enabled;

            Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
        }

        /// <summary>
        /// Main callback for the application-wide timer.
        /// Handles FMOD engine updates, playback status, and long-running operation progress.
        /// </summary>
        /// <param name="state">An object containing information to be used by the callback method.</param>
        private void MainTimer_Callback(object state)
        {
            if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                return;

            // --- FMOD Engine Update (formerly in _uiTimer) ---
            // This part runs on a background thread and does not require Invoke.
            if (_studioSystem.isValid()) _studioSystem.update();
            if (_coreSystem.hasHandle()) _coreSystem.update();

            try
            {
                // Use BeginInvoke for a non-blocking UI update request.
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (_isClosing || this.IsDisposed) return;

                    // --- Playback UI Update (formerly in _uiTimer) ---
                    // Update playback GUI elements like the seek bar and time display.
                    UpdatePlaybackStatus();

                    // --- Progress UI Update (formerly in _progressTimer) ---
                    // Update the elapsed time display if a long-running task is active.
                    if (_isWorking)
                    {
                        TimeSpan ts = _scanStopwatch.Elapsed;
                        lblElapsedTime.Text = $"Elapsed: {ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}";
                    }

                    // Update the progress bar if a scan is active.
                    if (_isScanning && _totalFilesToScan > 0)
                    {
                        int pct = (int)((float)Volatile.Read(ref _processedFilesCount) / _totalFilesToScan * 100);
                        if (progressBar.Value != pct) progressBar.Value = Math.Min(pct, 100);
                    }
                });
            }
            catch (Exception)
            {
                // Ignore exceptions that may occur during form closing.
            }
        }

        #endregion

        #region 5. File Analysis (Core Logic)

        /// <summary>
        /// Handles the Click event of the openFileToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "FMOD Files|*.bank;*.fsb", Multiselect = true };
                if (ofd.ShowDialog() == DialogResult.OK) await LoadContextAsync(ofd.FileNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the openFolderToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK) await LoadContextAsync(new string[] { fbd.SelectedPath });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the DragDrop event of the main form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private async void MainForm_DragDropAsync(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) await LoadContextAsync(files);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Loads and analyzes the given file or folder paths to populate the UI.
        /// </summary>
        /// <param name="inputPaths">An enumeration of file and folder paths to process.</param>
        private async Task LoadContextAsync(IEnumerable<string> inputPaths)
        {
            // Stop current playback and reset the UI to a clean state for loading.
            StopAudio();
            _currentTotalLengthMs = 0;
            lblTime.Text = "00:00.000 / 00:00.000";
            SetUiState(false);
            txtSearch.Text = "";

            // Initialize progress tracking variables and start the timers.
            _isWorking = true;
            _isScanning = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            _processedFilesCount = 0;
            _totalFilesToScan = 0;
            _currentProcessingFile = "Initializing...";
            _scanStopwatch.Restart();

            // Clear all nodes and list items from the primary UI views.
            treeViewInfo.BeginUpdate();
            treeViewInfo.Nodes.Clear();
            listViewDetails.Items.Clear();
            _originalNodes.Clear();
            lvSearchResults.Items.Clear();
            lvSearchResults.Visible = false;
            treeViewInfo.Visible = true;

            // Unload all previously loaded banks from the FMOD system.
            if (_studioSystem.isValid()) _studioSystem.unloadAll();

            List<string> allStringsBanks = new List<string>();
            var failedFiles = new ConcurrentBag<(string FilePath, Exception ex)>();

            try
            {
                // Discover all relevant files from the input paths in a background thread.
                List<string> allContentFiles = new List<string>();

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
                            catch (Exception ex) { failedFiles.Add((path, ex)); }
                        }
                        else if (File.Exists(path))
                        {
                            string name = Path.GetFileName(path).ToLower();
                            if (name.EndsWith(".strings.bank")) allStringsBanks.Add(path);
                            else if (name.EndsWith(".bank") || name.EndsWith(".fsb")) allContentFiles.Add(path);
                        }
                    }
                });

                // Remove duplicate files from the discovered lists.
                allStringsBanks = allStringsBanks.Distinct().ToList();
                allContentFiles = allContentFiles
                    .Where(f => !f.ToLower().EndsWith(".strings.bank"))
                    .Distinct()
                    .ToList();

                _totalFilesToScan = allContentFiles.Count;

                // Load all found strings banks to resolve names for events and other assets.
                foreach (string sb in allStringsBanks)
                {
                    if (_studioSystem.isValid())
                    {
                        _studioSystem.loadBankFile(sb, LOAD_BANK_FLAGS.NORMAL, out _);
                    }
                }

                // Analyze all content files in parallel to build the tree structure.
                var resultNodes = new ConcurrentBag<TreeNode>();
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MAX_PARALLEL_FILES };

                await Task.Run(() =>
                {
                    Parallel.ForEach(allContentFiles, parallelOptions, (filePath, loopState) =>
                    {
                        try
                        {
                            if (_isClosing) loopState.Stop();

                            string fileName = Path.GetFileName(filePath);
                            _currentProcessingFile = fileName;

                            // Update the status text during the analysis process.
                            int currentCount = Volatile.Read(ref _processedFilesCount) + 1;
                            int percent = (_totalFilesToScan > 0) ? (currentCount * 100 / _totalFilesToScan) : 0;
                            string statusText = $"[ANALYZING] [{currentCount}/{_totalFilesToScan}] ({percent}%) | {fileName}";

                            this.BeginInvoke((MethodInvoker)delegate { lblStatus.Text = statusText; });

                            TreeNode rootNode = new TreeNode(fileName, ImageIndex.File, ImageIndex.File);
                            string ext = Path.GetExtension(filePath).ToLower();

                            // Analyze the file based on its extension.
                            if (ext == ".bank") AnalyzeBankFile(filePath, rootNode);
                            else if (ext == ".fsb") AnalyzeFsbFile(filePath, rootNode);

                            resultNodes.Add(rootNode);
                            Interlocked.Increment(ref _processedFilesCount);
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add((filePath, ex));
                            Interlocked.Increment(ref _processedFilesCount);
                        }
                    });
                });

                if (_isClosing) return;

                // Populate the TreeView with the analyzed nodes.
                List<TreeNode> sortedNodes = resultNodes.OrderBy(n => n.Text).ToList();
                treeViewInfo.Nodes.AddRange(sortedNodes.ToArray());

                // Perform a post-processing step to resolve FMOD Studio events from loaded banks.
                foreach (TreeNode node in treeViewInfo.Nodes)
                {
                    if (_isClosing) return;
                    if (node.Tag is BankNode data)
                    {
                        AnalyzeBankLogic(data.ExtraInfo, node);
                    }
                }

                // Cache the top-level nodes for the search functionality.
                foreach (TreeNode n in treeViewInfo.Nodes) _originalNodes.Add(n);
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    failedFiles.Add(("<General Operation>", ex));
                    MessageBox.Show($"Error processing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                // Finalize the loading process and update the UI.
                _scanStopwatch.Stop();
                _isWorking = false;
                _isScanning = false;

                if (!_isClosing)
                {
                    if (!failedFiles.IsEmpty)
                    {
                        // Centralized error logging for file loading failures.
                        string logFileName = await LogOperationErrorAsync("File Loading", failedFiles);
                        MessageBox.Show(
                            this,
                            $"{failedFiles.Count} errors occurred during the file loading process.\n\n" +
                            $"All errors have been recorded in the following log file:\n" +
                            $"{logFileName}",
                            "File Load Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }

                    lblElapsedTime.Text = $"Elapsed: {_scanStopwatch.Elapsed:mm\\:ss\\.ff}";
                    lblStatus.Text = $"[READY] -> {_totalFilesToScan} files loaded. ({failedFiles.Count} failures)";
                    progressBar.Value = 0;
                    treeViewInfo.EndUpdate();
                    SetUiState(true);
                }
            }
        }

        /// <summary>
        /// Loads a .strings.bank file manually to refresh asset names in the UI.
        /// </summary>
        private void LoadStringsBankManually()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "FMOD Strings Bank|*.strings.bank",
                Title = "Select Strings Bank (e.g. Master.strings.bank)"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (_studioSystem.isValid())
                    {
                        RESULT res = _studioSystem.loadBankFile(ofd.FileName, LOAD_BANK_FLAGS.NORMAL, out Bank sb);
                        if (res == RESULT.OK || res == RESULT.ERR_EVENT_ALREADY_LOADED)
                        {
                            treeViewInfo.BeginUpdate();
                            RefreshNodeNamesRecursive(treeViewInfo.Nodes);
                            treeViewInfo.EndUpdate();
                            MessageBox.Show("Strings Bank loaded. Node names have been refreshed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            CheckFmodResult(res);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load strings bank: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Recursively traverses the tree and updates node names based on loaded FMOD data.
        /// </summary>
        /// <param name="nodes">The collection of nodes to refresh.</param>
        private void RefreshNodeNamesRecursive(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is NodeData data)
                {
                    string newName = null;
                    if (data is EventNode eventNode && eventNode.EventObject.isValid())
                    {
                        eventNode.EventObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p)) newName = p.Substring(p.LastIndexOf('/') + 1);
                    }
                    else if (data is BankNode bankNode && bankNode.BankObject.isValid())
                    {
                        bankNode.BankObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p)) newName = Path.GetFileName(p);
                    }
                    else if (data is BusNode busNode && busNode.BusObject.isValid())
                    {
                        busNode.BusObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p)) newName = p.Substring(p.LastIndexOf('/') + 1);
                    }

                    if (!string.IsNullOrEmpty(newName) && newName != node.Text)
                    {
                        node.Text = newName;
                    }
                }
                if (node.Nodes.Count > 0) RefreshNodeNamesRecursive(node.Nodes);
            }
        }

        /// <summary>
        /// Analyzes a .bank file to find and list embedded FSB5 audio containers.
        /// It uses a memory-efficient streaming approach to handle large files.
        /// </summary>
        /// <param name="path">The file path of the .bank file.</param>
        /// <param name="root">The root TreeNode to populate with results.</param>
        private void AnalyzeBankFile(string path, TreeNode root)
        {
            if (_isClosing) return;
            // Use the modern NodeData structure from the New code.
            root.Tag = new BankNode(path);

            var fsbOffsets = new List<uint>();

            try
            {
                // Adopt the memory-efficient streaming method from the Old code.
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length < 64) return;

                    byte[] buffer = new byte[FILE_READ_BUFFER_SIZE];
                    int bytesRead;

                    // Scan the file chunk by chunk.
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        long bufferStartOffset = fs.Position - bytesRead;

                        for (int i = 0; i < bytesRead - 3; i++)
                        {
                            if (buffer[i] == 'F' && buffer[i + 1] == 'S' && buffer[i + 2] == 'B' && buffer[i + 3] == '5')
                            {
                                // Validate using the buffer to prevent stream position conflicts.
                                if (IsValidFsbHeader(buffer, i))
                                {
                                    fsbOffsets.Add((uint)(bufferStartOffset + i));
                                }
                            }
                        }

                        // Seek back to handle signatures that might span across buffer boundaries.
                        if (fs.Position < fs.Length)
                        {
                            fs.Seek(-60, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch (IOException) { return; } // Handle file access errors.


            if (fsbOffsets.Count == 0) return;

            // The rest of the logic uses the New code's structure for creating nodes.
            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int fallbackIndex = 1;

            foreach (var offset in fsbOffsets)
            {
                string rawName = GetFsbInternalName(path, offset);
                string baseName = !string.IsNullOrEmpty(rawName) ?
                    Path.GetFileNameWithoutExtension(rawName) :
                    Path.GetFileNameWithoutExtension(path);

                if (string.IsNullOrEmpty(rawName))
                {
                    if (fallbackIndex > 1) baseName += $"_{fallbackIndex}";
                }

                string finalName = baseName + ".fsb";
                int dupeCounter = 1;

                // Resolve any potential name collisions.
                while (usedNames.Contains(finalName))
                {
                    finalName = $"{baseName}_{dupeCounter++}.fsb";
                }

                usedNames.Add(finalName);
                fallbackIndex++;

                // Create nodes using the New code's specific NodeData-derived classes.
                TreeNode fsbNode = new TreeNode(finalName, ImageIndex.Folder, ImageIndex.Folder);
                fsbNode.Tag = new FsbFileNode(path, offset);
                root.Nodes.Add(fsbNode);

                ParseFsbFromSource(path, offset, fsbNode);
            }
        }

        /// <summary>
        /// Validates if the data at a given index within a buffer corresponds to a valid FSB5 header.
        /// </summary>
        /// <param name="buffer">The byte array buffer containing the file chunk.</param>
        /// <param name="index">The starting index within the buffer where the potential header starts.</param>
        /// <returns>true if the header is valid; otherwise, false.</returns>
        private bool IsValidFsbHeader(byte[] buffer, int index)
        {
            try
            {
                // Ensure there is enough data remaining in the buffer to read key fields.
                if (index + 24 > buffer.Length) // Minimum size needed to read key fields
                {
                    return false;
                }

                // Header fields are read for logical validation.
                int numSamples = BitConverter.ToInt32(buffer, index + 8);

                // A negative sample count is a definitive sign of invalid data.
                if (numSamples < 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                // Any exception during reading indicates an invalid structure.
                return false;
            }
        }


        /// <summary>
        /// Analyzes a standalone .fsb file and populates the given root node.
        /// </summary>
        /// <param name="path">The file path of the .fsb file.</param>
        /// <param name="root">The root TreeNode to populate with results.</param>
        private void AnalyzeFsbFile(string path, TreeNode root)
        {
            if (_isClosing) return;
            root.Tag = new FsbFileNode(path, 0); // Standalone FSB has an offset of 0.
            ParseFsbFromSource(path, 0, root);
        }

        /// <summary>
        /// Uses the FMOD Core API to open an FSB data stream and list its sub-sounds.
        /// </summary>
        /// <param name="path">The path to the source file (.bank or .fsb).</param>
        /// <param name="offset">The offset of the FSB data within the source file.</param>
        /// <param name="parentNode">The parent TreeNode to which sub-sound nodes will be added.</param>
        private void ParseFsbFromSource(string path, uint offset, TreeNode parentNode)
        {
            if (_isClosing) return;

            Sound sound = new Sound();
            Sound subSound = new Sound();

            try
            {
                lock (_coreSystemLock)
                {
                    // Open the sound data as a stream without playing it.
                    CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };
                    if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM, ref exinfo, out sound) == RESULT.OK)
                    {
                        sound.getNumSubSounds(out int numSub);

                        if (numSub > 0)
                        {
                            for (int i = 0; i < numSub; i++)
                            {
                                if (_isClosing) break;
                                sound.getSubSound(i, out subSound);
                                AudioInfo info = GetAudioInfo(subSound, i, path, offset);

                                string displayName = string.IsNullOrEmpty(info.Name) ?
                                    $"{Path.GetFileNameWithoutExtension(path)}_{offset}_sub_{i}" : info.Name;

                                TreeNode node = new TreeNode(displayName, ImageIndex.Audio, ImageIndex.Audio)
                                {
                                    Tag = new AudioDataNode(info, offset, path)
                                };
                                parentNode.Nodes.Add(node);
                                SafeRelease(ref subSound);
                            }
                        }
                        else
                        {
                            // This case handles a single sound within the FSB file.
                            AudioInfo info = GetAudioInfo(sound, 0, path, offset);
                            if (info.LengthMs > 0)
                            {
                                string displayName = string.IsNullOrEmpty(info.Name) ?
                                    Path.GetFileNameWithoutExtension(path) : info.Name;
                                TreeNode node = new TreeNode(displayName, ImageIndex.Audio, ImageIndex.Audio)
                                {
                                    Tag = new AudioDataNode(info, offset, path)
                                };
                                parentNode.Nodes.Add(node);
                            }
                        }
                    }
                }
            }
            finally { SafeRelease(ref sound); }
        }

        /// <summary>
        /// Uses the FMOD Studio API to load a bank and identify its logical events.
        /// </summary>
        /// <param name="path">The file path of the .bank file.</param>
        /// <param name="root">The root TreeNode representing the bank.</param>
        private void AnalyzeBankLogic(string path, TreeNode root)
        {
            if (_isClosing || !_studioSystem.isValid()) return;

            RESULT res = _studioSystem.loadBankFile(path, LOAD_BANK_FLAGS.NORMAL, out Bank bank);

            // Handle the case where the bank is already loaded by the system.
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
                if (root.Tag is BankNode nd) nd.FmodObject = bank;

                bank.getEventCount(out int evtCount);
                if (evtCount > 0)
                {
                    TreeNode evtGroup = new TreeNode($"Events ({evtCount})", ImageIndex.Folder, ImageIndex.Folder);
                    root.Nodes.Insert(0, evtGroup);

                    bank.getEventList(out EventDescription[] events);
                    foreach (var evt in events)
                    {
                        evt.getPath(out string p);
                        evt.getID(out GUID id);
                        string name = string.IsNullOrEmpty(p) ? GuidToString(id) : p.Substring(p.LastIndexOf('/') + 1);

                        TreeNode node = new TreeNode(name, ImageIndex.Event, ImageIndex.Event)
                        {
                            Tag = new EventNode(evt)
                        };
                        evtGroup.Nodes.Add(node);
                    }
                }
            }
        }

        #endregion

        #region 6. Playback Logic

        /// <summary>
        /// Updates the playback status, including UI elements like the seek bar and time display.
        /// </summary>
        private void UpdatePlaybackStatus()
        {
            bool playing = false;
            uint currentPos = 0;

            // Check the status of the raw audio channel.
            if (_currentChannel.hasHandle())
            {
                _currentChannel.isPlaying(out playing);
                if (playing)
                {
                    _currentChannel.getPosition(out currentPos, TIMEUNIT.MS);
                    _currentChannel.getMode(out MODE mode);

                    // Manually handle stopping playback if the sound is not set to loop.
                    if ((mode & MODE.LOOP_NORMAL) == 0 && _currentTotalLengthMs > 0 && currentPos >= _currentTotalLengthMs)
                    {
                        playing = false;
                    }
                }
            }
            // Check the status of the FMOD Studio event instance.
            else if (_currentEvent.isValid())
            {
                _currentEvent.getPlaybackState(out PLAYBACK_STATE state);
                playing = (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING);
                if (playing)
                {
                    _currentEvent.getTimelinePosition(out int pos);
                    currentPos = (uint)pos;
                }
            }

            // Synchronize the internal playing state with the FMOD engine state.
            if (_isPlaying && !playing) StopAudio();

            // Update the text of the play/pause button.
            if (_isPlaying != playing)
            {
                _isPlaying = playing;
                btnPlay.Text = _isPlaying ? "Pause (||)" : "Play (▶)";
            }

            // Update the time display and seek bar position.
            if (playing && _currentTotalLengthMs > 0)
            {
                lblTime.Text = $"{TimeSpan.FromMilliseconds(currentPos):mm\\:ss\\.fff} / {TimeSpan.FromMilliseconds(_currentTotalLengthMs):mm\\:ss\\.fff}";

                if (!_isDraggingSeek)
                {
                    int newVal = (int)((float)currentPos / _currentTotalLengthMs * 1000);
                    trackSeek.Value = Math.Min(Math.Max(0, newVal), 1000);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnPlay control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnPlay_Click(object sender, EventArgs e) => TogglePause();

        /// <summary>
        /// Handles the Click event of the btnStop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnStop_Click(object sender, EventArgs e) => StopAudio();

        /// <summary>
        /// Toggles the paused state of the currently playing audio.
        /// </summary>
        private void TogglePause()
        {
            bool playing = false, paused = false;

            // Get the current playback and paused state from the FMOD engine.
            if (_currentChannel.hasHandle())
            {
                _currentChannel.isPlaying(out playing);
                _currentChannel.getPaused(out paused);
            }
            else if (_currentEvent.isValid())
            {
                _currentEvent.getPlaybackState(out PLAYBACK_STATE s);
                playing = (s == PLAYBACK_STATE.PLAYING);
                _currentEvent.getPaused(out paused);
            }

            // Apply the appropriate pause/resume action.
            if (playing && !paused)
            {
                if (_currentChannel.hasHandle()) _currentChannel.setPaused(true);
                if (_currentEvent.isValid()) _currentEvent.setPaused(true);
            }
            else if (paused)
            {
                if (_currentChannel.hasHandle()) _currentChannel.setPaused(false);
                if (_currentEvent.isValid()) _currentEvent.setPaused(false);
            }
            else
            {
                PlaySelection();
            }
        }

        /// <summary>
        /// Plays the currently selected audio item or event.
        /// </summary>
        private void PlaySelection()
        {
            if (_currentSelection == null || _isClosing) return;
            StopAudio();

            try
            {
                // Play raw audio data from an FSB.
                if (_currentSelection is AudioDataNode audioNode)
                {
                    AudioInfo info = audioNode.CachedAudio;
                    _currentTotalLengthMs = info.LengthMs;

                    lock (_coreSystemLock)
                    {
                        CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = (uint)info.FileOffset };
                        SafeRelease(ref _loadedSound);

                        CheckFmodResult(_coreSystem.createSound(info.SourcePath, MODE.CREATESTREAM | MODE.OPENONLY, ref ex, out _loadedSound));

                        Sound soundToPlay;
                        _loadedSound.getNumSubSounds(out int numSub);
                        if (info.Index < numSub) CheckFmodResult(_loadedSound.getSubSound(info.Index, out soundToPlay));
                        else soundToPlay = _loadedSound;

                        CheckFmodResult(_coreSystem.playSound(soundToPlay, new ChannelGroup(IntPtr.Zero), false, out _currentChannel));

                        if (_currentChannel.hasHandle())
                        {
                            _currentChannel.setMode(chkLoop.Checked ? MODE.LOOP_NORMAL : MODE.LOOP_OFF);
                            _currentChannel.setVolume(trackVol.Value / 100.0f);

                            // Attach the analyzer if it is open.
                            if (_audioAnalyzer != null && !_audioAnalyzer.IsDisposed)
                            {
                                _audioAnalyzer.AttachToAudio(_coreSystem, _currentChannel, soundToPlay);
                            }
                        }
                    }
                }
                // Play a logical FMOD Studio event.
                else if (_currentSelection is EventNode eventNode)
                {
                    EventDescription evt = eventNode.EventObject;
                    if (evt.isValid())
                    {
                        evt.getLength(out int len);
                        _currentTotalLengthMs = (uint)len;

                        evt.createInstance(out _currentEvent);
                        _currentEvent.setVolume(trackVol.Value / 100.0f);
                        _currentEvent.start();
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Playback Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Stops all currently playing audio.
        /// </summary>
        private void StopAudio()
        {
            if (_currentChannel.hasHandle())
            {
                _currentChannel.stop();
                _currentChannel.clearHandle();
            }

            if (_currentEvent.isValid())
            {
                _currentEvent.stop(STOP_MODE.IMMEDIATE);
                _currentEvent.release();
                _currentEvent.clearHandle();
            }

            SafeRelease(ref _loadedSound);

            _isPlaying = false;
            if (!IsDisposed)
            {
                btnPlay.Text = "Play (▶)";
                trackSeek.Value = 0;
                lblTime.Text = $"00:00.000 / {TimeSpan.FromMilliseconds(_currentTotalLengthMs):mm\\:ss\\.fff}";
            }
        }

        /// <summary>
        /// Handles the Scroll event of the trackVol control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackVol_Scroll(object sender, EventArgs e)
        {
            lblVol.Text = $"Volume: {trackVol.Value}%";
            float vol = trackVol.Value / 100.0f;
            if (_currentChannel.hasHandle()) _currentChannel.setVolume(vol);
            if (_currentEvent.isValid()) _currentEvent.setVolume(vol);
        }

        /// <summary>
        /// Handles the MouseDown event of the trackSeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void trackSeek_MouseDown(object sender, MouseEventArgs e) => _isDraggingSeek = true;

        /// <summary>
        /// Handles the MouseUp event of the trackSeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void trackSeek_MouseUp(object sender, MouseEventArgs e)
        {
            if (_currentTotalLengthMs > 0)
            {
                uint newPos = (uint)((float)trackSeek.Value / 1000 * _currentTotalLengthMs);
                if (_currentChannel.hasHandle()) _currentChannel.setPosition(newPos, TIMEUNIT.MS);
                else if (_currentEvent.isValid()) _currentEvent.setTimelinePosition((int)newPos);
            }
            _isDraggingSeek = false;
        }

        /// <summary>
        /// Handles the CheckedChanged event of the chkLoop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void chkLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (_currentChannel.hasHandle())
            {
                _currentChannel.setMode(chkLoop.Checked ? MODE.LOOP_NORMAL : MODE.LOOP_OFF);
            }
        }

        #endregion

        #region 7. Search Logic

        /// <summary>
        /// Handles the TextChanged event of the txtSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Reset the debounce timer on each keystroke.
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        /// <summary>
        /// Handles the Tick event of the _searchDebounceTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            PerformSearch(txtSearch.Text);
        }

        /// <summary>
        /// Performs a search based on the provided query and displays results in the ListView.
        /// </summary>
        /// <param name="query">The search string to filter results by.</param>
        private async void PerformSearch(string query)
        {
            string lowerQuery = query.ToLower();

            if (string.IsNullOrWhiteSpace(lowerQuery))
            {
                // Restore the TreeView and hide the search results if the query is empty.
                lvSearchResults.Visible = false;
                treeViewInfo.Visible = true;
                return;
            }

            // Switch the UI to the search results list view.
            treeViewInfo.Visible = false;
            lvSearchResults.Visible = true;
            lvSearchResults.Items.Clear();
            lvSearchResults.Clear(); // Clears both items and columns.
            lvSearchResults.Columns.Add("", 20, HorizontalAlignment.Center);
            lvSearchResults.Columns.Add("Name", 220);
            lvSearchResults.Columns.Add("Type", 80);
            lvSearchResults.Columns.Add("Path", 300);

            if (_originalNodes.Count > 0)
            {
                SetUiState(false);

                // Perform the search on a background thread to keep the UI responsive.
                List<ListViewItem> results = await Task.Run(() =>
                {
                    List<ListViewItem> items = new List<ListViewItem>();
                    SearchNodesRecursiveToList(_originalNodes, lowerQuery, items);
                    return items;
                });

                SetUiState(true);
                lvSearchResults.BeginUpdate();
                lvSearchResults.Items.AddRange(results.ToArray());
                lvSearchResults.EndUpdate();
            }
        }

        /// <summary>
        /// Recursively searches through a collection of nodes and adds matches to a list.
        /// </summary>
        /// <param name="nodes">The collection of TreeNodes to search.</param>
        /// <param name="query">The search query string.</param>
        /// <param name="results">The list to which matching ListViewItems will be added.</param>
        private void SearchNodesRecursiveToList(IEnumerable<TreeNode> nodes, string query, List<ListViewItem> results)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.ToLower().Contains(query))
                {
                    ListViewItem item = new ListViewItem("");
                    item.Checked = node.Checked;
                    item.SubItems.Add(node.Text);
                    string type = "Unknown";
                    if (node.Tag is NodeData data) type = data.Type.ToString();
                    item.SubItems.Add(type);
                    item.SubItems.Add(node.FullPath);
                    // Store the original TreeNode in the item's Tag for later use.
                    item.Tag = node;
                    results.Add(item);
                }

                if (node.Nodes.Count > 0) SearchNodesRecursiveToList(node.Nodes.Cast<TreeNode>(), query, results);
            }
        }

        #endregion

        #region 8. TreeView & Selection Logic

        /// <summary>
        /// Handles the AfterCheck event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void TreeViewInfo_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_isUpdatingChecks) return;
            _isUpdatingChecks = true;
            CheckAllChildren(e.Node, e.Node.Checked);
            _isUpdatingChecks = false;
        }

        /// <summary>
        /// Recursively checks or unchecks all child nodes of a given node.
        /// </summary>
        /// <param name="node">The parent TreeNode.</param>
        /// <param name="isChecked">The checked state to apply to all children.</param>
        private void CheckAllChildren(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                CheckAllChildren(child, isChecked);
            }
        }

        /// <summary>
        /// Handles the NodeMouseClick event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeNodeMouseClickEventArgs"/> instance containing the event data.</param>
        private void treeViewInfo_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeViewInfo.SelectedNode = e.Node;
                if (e.Node.Tag is NodeData data) SetupContextMenu(data);
            }
        }

        /// <summary>
        /// Sets up the context menu based on the type of the selected node.
        /// </summary>
        /// <param name="data">The NodeData associated with the selected node.</param>
        private void SetupContextMenu(NodeData data)
        {
            bool isAudio = data is AudioDataNode;
            bool hasGuid = data is EventNode || data is BankNode;

            extractContextMenuItem.Enabled = isAudio;

            // Dynamically find the "Rebuild" menu item to set its enabled state.
            var rebuildItem = treeViewContextMenu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == "Rebuild Sound with fsbankcl...");
            if (rebuildItem != null)
            {
                rebuildItem.Enabled = isAudio;
            }

            copyGuidContextMenuItem.Enabled = hasGuid;
        }

        /// <summary>
        /// Handles the AfterSelect event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void treeViewInfo_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_isClosing) return;
            StopAudio();
            _currentSelection = e.Node.Tag as NodeData;
            UpdateDetailsView();
        }

        /// <summary>
        /// Handles the NodeMouseDoubleClick event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeNodeMouseClickEventArgs"/> instance containing the event data.</param>
        private void treeViewInfo_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) => PlaySelection();

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvSearchResults.SelectedItems.Count > 0)
            {
                // Retrieve the stored TreeNode from the ListViewItem's Tag property.
                if (lvSearchResults.SelectedItems[0].Tag is TreeNode node)
                {
                    _currentSelection = node.Tag as NodeData;
                    UpdateDetailsView();
                }
            }
        }

        /// <summary>
        /// Handles the DoubleClick event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_DoubleClick(object sender, EventArgs e)
        {
            if (lvSearchResults.SelectedItems.Count > 0) PlaySelection();
        }

        /// <summary>
        /// Handles the MouseClick event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem focusedItem = lvSearchResults.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    // Retrieve the original TreeNode and its data from the item's Tag.
                    TreeNode targetNode = focusedItem.Tag as TreeNode;
                    NodeData data = targetNode?.Tag as NodeData;

                    // Create a dynamic context menu for the search results.
                    ContextMenuStrip searchMenu = new ContextMenuStrip();

                    // Create and add the "Select All" menu item.
                    ToolStripMenuItem selectAllItem = new ToolStripMenuItem("Select All");
                    selectAllItem.Click += (s, args) => CheckAllInCurrentView();
                    searchMenu.Items.Add(selectAllItem);
                    searchMenu.Items.Add(new ToolStripSeparator());

                    // Create and add the "Open File Location" menu item.
                    ToolStripMenuItem openLocItem = new ToolStripMenuItem("Open File Location");
                    openLocItem.Click += (s, args) =>
                    {
                        if (targetNode != null)
                        {
                            // Switch back to the TreeView and select the original node.
                            lvSearchResults.Visible = false;
                            treeViewInfo.Visible = true;
                            treeViewInfo.SelectedNode = targetNode;
                            targetNode.EnsureVisible();
                            treeViewInfo.Focus();
                        }
                    };
                    searchMenu.Items.Add(openLocItem);
                    searchMenu.Items.Add(new ToolStripSeparator());

                    // Create and add the "Extract This Item" menu item, enabled only for audio.
                    ToolStripMenuItem extractItem = new ToolStripMenuItem("Extract This Item...");
                    if (data is AudioDataNode)
                    {
                        extractItem.Click += (s, args) =>
                        {
                            // Set the selection to the right-clicked node before triggering the extract action.
                            treeViewInfo.SelectedNode = targetNode;
                            extractContextMenuItem_Click(s, args);
                        };
                    }
                    else
                    {
                        extractItem.Enabled = false;
                    }
                    searchMenu.Items.Add(extractItem);

                    // Create and add the "Rebuild This Item" menu item, enabled only for audio.
                    ToolStripMenuItem rebuildItem = new ToolStripMenuItem("Rebuild This Item...");
                    if (data is AudioDataNode)
                    {
                        rebuildItem.Click += (s, args) =>
                        {
                            _currentSelection = data;
                            rebuildSoundContextMenuItem_Click(s, args);
                        };
                    }
                    else
                    {
                        rebuildItem.Enabled = false;
                    }
                    searchMenu.Items.Add(rebuildItem);

                    searchMenu.Items.Add(new ToolStripSeparator());

                    // Create and add "Copy" menu items.
                    ToolStripMenuItem copyName = new ToolStripMenuItem("Copy Name");
                    copyName.Click += (s, args) => Clipboard.SetText(targetNode != null ? targetNode.Text : focusedItem.Text);
                    searchMenu.Items.Add(copyName);

                    ToolStripMenuItem copyPath = new ToolStripMenuItem("Copy Path");
                    copyPath.Click += (s, args) => Clipboard.SetText(targetNode != null ? targetNode.FullPath : focusedItem.SubItems[3].Text);
                    searchMenu.Items.Add(copyPath);

                    ToolStripMenuItem copyGuid = new ToolStripMenuItem("Copy GUID");
                    bool hasGuid = false;
                    if (data != null)
                    {
                        if (data is EventNode eventNode && eventNode.EventObject.isValid())
                        {
                            eventNode.EventObject.getID(out GUID id);
                            copyGuid.Click += (s, args) => Clipboard.SetText(GuidToString(id));
                            hasGuid = true;
                        }
                        else if (data is BankNode bankNode && bankNode.BankObject.isValid())
                        {
                            bankNode.BankObject.getID(out GUID id);
                            copyGuid.Click += (s, args) => Clipboard.SetText(GuidToString(id));
                            hasGuid = true;
                        }
                    }
                    copyGuid.Enabled = hasGuid;
                    searchMenu.Items.Add(copyGuid);

                    // Show the context menu at the cursor's position.
                    searchMenu.Show(Cursor.Position);
                }
            }
        }

        /// <summary>
        /// Handles the ColumnClick event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ColumnClickEventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0) CheckAllInCurrentView();
        }

        /// <summary>
        /// Checks or unchecks all items in the currently visible view (TreeView or ListView).
        /// </summary>
        private void CheckAllInCurrentView()
        {
            if (lvSearchResults.Visible)
            {
                bool anyUnchecked = false;
                foreach (ListViewItem item in lvSearchResults.Items) if (!item.Checked) { anyUnchecked = true; break; }

                lvSearchResults.BeginUpdate();
                foreach (ListViewItem item in lvSearchResults.Items) item.Checked = anyUnchecked;
                lvSearchResults.EndUpdate();
            }
            else
            {
                treeViewInfo.BeginUpdate();
                _isUpdatingChecks = true;
                bool anyUnchecked = false;
                foreach (TreeNode node in treeViewInfo.Nodes) if (!node.Checked) { anyUnchecked = true; break; }

                foreach (TreeNode node in treeViewInfo.Nodes)
                {
                    node.Checked = anyUnchecked;
                    CheckAllChildren(node, anyUnchecked);
                }
                _isUpdatingChecks = false;
                treeViewInfo.EndUpdate();
            }
        }

        #endregion

        #region 9. Details View & Properties

        /// <summary>
        /// Updates the details view with information about the currently selected item.
        /// This method leverages polymorphism to delegate detail generation to the NodeData objects.
        /// </summary>
        private void UpdateDetailsView()
        {
            if (_currentSelection == null) return;

            listViewDetails.Items.Clear();
            listViewDetails.Groups.Clear();
            listViewDetails.BeginUpdate();

            // Retrieve details by invoking the implemented GetDetails method, utilizing polymorphism to avoid large switch statements.
            List<KeyValuePair<string, string>> details = _currentSelection.GetDetails();

            foreach (var detail in details)
            {
                string groupName = detail.Key;

                // Split the string into property name and value based on the colon delimiter.
                string[] parts = detail.Value.Split(new[] { ':' }, 2);
                string propName = parts[0].Trim();
                string propValue = parts.Length > 1 ? parts[1].Trim() : "";

                AddDetailItem(groupName, propName, propValue);
            }

            listViewDetails.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewDetails.EndUpdate();

            // Calculate the total duration for the playback display.
            uint len = 0;
            if (_currentSelection is AudioDataNode audioNode)
            {
                len = audioNode.CachedAudio.LengthMs;
            }
            else if (_currentSelection is EventNode eventNode && eventNode.EventObject.isValid())
            {
                eventNode.EventObject.getLength(out int l);
                len = (uint)l;
            }

            _currentTotalLengthMs = len;
            lblTime.Text = $"00:00.000 / {TimeSpan.FromMilliseconds(_currentTotalLengthMs):mm\\:ss\\.fff}";

            if (chkAutoPlay.Checked) PlaySelection();
        }

        /// <summary>
        /// Adds a detail item to the properties ListView.
        /// </summary>
        /// <param name="groupName">The name of the group for the property.</param>
        /// <param name="propName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        private void AddDetailItem(string groupName, string propName, string value)
        {
            ListViewGroup grp = listViewDetails.Groups.Cast<ListViewGroup>().FirstOrDefault(x => x.Header == groupName);
            if (grp == null)
            {
                grp = new ListViewGroup(groupName, groupName);
                listViewDetails.Groups.Add(grp);
            }
            var item = new ListViewItem(new[] { groupName, propName, value }) { Group = grp };
            listViewDetails.Items.Add(item);
        }

        #endregion

        #region 10. Export, Extraction & Rebuilding

        /// <summary>
        /// Handles the Click event of the exportCsvToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void exportCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await ExportToCsvAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred while exporting to CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the extractCheckedToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void extractCheckedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PerformExtraction(onlyChecked: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred during extraction: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the extractAllToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PerformExtraction(onlyChecked: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred during extraction: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the exitToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cmbExtractLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CmbExtractLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Retrieve the previous index from the Tag property. Default to 0 (Same as source) if not set.
            // This allows us to revert to the specific previous selection (e.g., AskEveryTime) on cancellation.
            int previousIndex = (cmbExtractLocation.Tag is int idx) ? idx : 0;

            var selectedMode = (ExtractLocationMode)cmbExtractLocation.SelectedIndex;

            if (selectedMode == ExtractLocationMode.CustomPath)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select a custom folder for all future extractions.";
                    // Start the dialog from the previously set path, if it exists.
                    if (!string.IsNullOrEmpty(_customExtractPath) && Directory.Exists(_customExtractPath))
                    {
                        fbd.SelectedPath = _customExtractPath;
                    }

                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        _customExtractPath = fbd.SelectedPath;
                        lblStatus.Text = $"[INFO] Custom extraction path set to: {_customExtractPath}";

                        // Successfully changed, so update the history (Tag) to the current new mode.
                        cmbExtractLocation.Tag = (int)selectedMode;
                    }
                    else
                    {
                        // User cancelled. Revert to the previous selection.
                        lblStatus.Text = "[INFO] Custom path selection cancelled.";

                        // Reverting the index will trigger this event handler again recursively.
                        // In the recursive call, it will hit the 'else' block below and ensure the Tag matches the reverted state.
                        // If the previous state was CustomPath (re-selection), it stays CustomPath.
                        cmbExtractLocation.SelectedIndex = previousIndex;
                    }
                }
            }
            else
            {
                // For any other selection, simply update the 'previous' state history.
                cmbExtractLocation.Tag = (int)selectedMode;
            }
        }

        /// <summary>
        /// Exports the current tree structure to a CSV file.
        /// </summary>
        private async Task ExportToCsvAsync()
        {
            if (treeViewInfo.Nodes.Count == 0)
            {
                MessageBox.Show("Nothing to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string defaultName = $"FmodExport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.csv";
            SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = defaultName };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Type,Path,Name,Duration(ms),Source,Index,Encoding,Container,Channels,Bits,LoopStart,LoopEnd,Mode,GUID");
                    ExportNodesRecursive(treeViewInfo.Nodes, sb, "");

                    await WriteAllTextAsync(sfd.FileName, sb.ToString());
                    MessageBox.Show("Export Completed Successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Recursively traverses nodes to build the CSV export string.
        /// </summary>
        /// <param name="nodes">The collection of TreeNodes to export.</param>
        /// <param name="sb">The StringBuilder to append CSV lines to.</param>
        /// <param name="path">The current hierarchical path of the node.</param>
        private void ExportNodesRecursive(TreeNodeCollection nodes, StringBuilder sb, string path)
        {
            foreach (TreeNode node in nodes)
            {
                string currentPath = string.IsNullOrEmpty(path) ? node.Text : $"{path}/{node.Text}";

                if (node.Tag is NodeData data)
                {
                    string type = data.Type.ToString();
                    string name = SanitizeCsvField(node.Text);
                    string treePath = SanitizeCsvField(currentPath);
                    string duration = "", source = "", index = "", encoding = "", container = "";
                    string channels = "", bits = "", loopStart = "", loopEnd = "", mode = "", guid = "";

                    if (data is AudioDataNode audioNode)
                    {
                        AudioInfo info = audioNode.CachedAudio;
                        duration = info.LengthMs.ToString();
                        source = SanitizeCsvField(Path.GetFileName(info.SourcePath));
                        index = info.Index.ToString();
                        encoding = info.Type.ToString();
                        container = info.Format.ToString();
                        channels = info.Channels.ToString();
                        bits = info.Bits.ToString();
                        loopStart = info.LoopStart.ToString();
                        loopEnd = info.LoopEnd.ToString();
                        mode = SanitizeCsvField(info.Mode.ToString());
                    }
                    else if (data is EventNode eventNode && eventNode.EventObject.isValid())
                    {
                        eventNode.EventObject.getID(out GUID id);
                        guid = GuidToString(id);
                        eventNode.EventObject.getLength(out int len);
                        duration = len.ToString();
                    }
                    else if (data is BankNode bankNode)
                    {
                        if (bankNode.BankObject.isValid())
                        {
                            bankNode.BankObject.getID(out GUID id);
                            guid = GuidToString(id);
                        }
                        source = SanitizeCsvField(data.ExtraInfo);
                    }

                    sb.AppendLine($"{type},{treePath},{name},{duration},{source},{index},{encoding},{container},{channels},{bits},{loopStart},{loopEnd},{mode},{guid}");
                }

                if (node.Nodes.Count > 0) ExportNodesRecursive(node.Nodes, sb, currentPath);
            }
        }

        /// <summary>
        /// Sanitizes a string for use in a CSV field by quoting it if it contains a comma.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <returns>A CSV-safe string.</returns>
        private string SanitizeCsvField(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Contains(",") ? $"\"{s}\"" : s;
        }

        /// <summary>
        /// Determines the root directory for an extraction operation based on user settings.
        /// </summary>
        /// <returns>The base path for extraction, or null if the user cancels.</returns>
        private async Task<string> GetBasePathForExtractionAsync()
        {
            var selectedMode = (ExtractLocationMode)cmbExtractLocation.SelectedIndex;
            string basePath = null;

            switch (selectedMode)
            {
                case ExtractLocationMode.AskEveryTime:
                    using (var fbd = new FolderBrowserDialog())
                    {
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            basePath = fbd.SelectedPath;
                        }
                    }
                    break;

                case ExtractLocationMode.CustomPath:
                    // Validate that the custom path still exists.
                    if (string.IsNullOrEmpty(_customExtractPath) || !Directory.Exists(_customExtractPath))
                    {
                        MessageBox.Show("The previously set custom path is no longer valid. Please select a new one.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // Trigger the selection logic again to prompt the user.
                        CmbExtractLocation_SelectedIndexChanged(null, null);

                        // Check if a new path was successfully set.
                        if (string.IsNullOrEmpty(_customExtractPath) || !Directory.Exists(_customExtractPath))
                        {
                            basePath = null; // User cancelled the re-selection.
                        }
                        else
                        {
                            basePath = _customExtractPath;
                        }
                    }
                    else
                    {
                        basePath = _customExtractPath;
                    }
                    break;

                case ExtractLocationMode.SameAsSource:
                    return "##SAME_AS_SOURCE##";
            }

            // Return null if a path was required but not provided (due to user cancellation).
            if ((selectedMode == ExtractLocationMode.AskEveryTime || selectedMode == ExtractLocationMode.CustomPath) && string.IsNullOrEmpty(basePath))
            {
                lblStatus.Text = "[CANCELLED] -> Extraction cancelled by user.";
                return null;
            }

            return basePath;
        }


        /// <summary>
        /// Orchestrates the bulk extraction of audio files to the file system.
        /// </summary>
        /// <param name="onlyChecked">A boolean indicating whether to extract all audio or only checked items.</param>
        private async void PerformExtraction(bool onlyChecked)
        {
            List<TreeNode> extractList = new List<TreeNode>();

            // Determine the scope of items to be extracted.
            if (onlyChecked == false)
            {
                // "Extract All" ignores the current view and uses the full original tree.
                FindCheckedAudioNodesRecursive(_originalNodes, extractList, false);
            }
            else
            {
                // "Extract Checked" respects the currently visible view (tree or search results).
                if (lvSearchResults.Visible)
                {
                    foreach (ListViewItem item in lvSearchResults.CheckedItems)
                    {
                        if (item.Tag is TreeNode node && node.Tag is AudioDataNode)
                        {
                            extractList.Add(node);
                        }
                    }
                }
                else
                {
                    if (treeViewInfo.Nodes.Count == 0) return;
                    FindCheckedAudioNodesRecursive(treeViewInfo.Nodes, extractList, true);
                }
            }

            if (extractList.Count == 0)
            {
                MessageBox.Show("No audio items selected for extraction.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Determine the root directory for the extraction operation.
            string userSelectedPath = await GetBasePathForExtractionAsync();
            if (userSelectedPath == null) return; // User cancelled the folder selection.

            string targetRoot = userSelectedPath;

            // If "Same as source" is selected, create a folder named after the source file in its directory.
            // Example: "Desktop\test.bank" -> Target will be "Desktop\test\"
            if (userSelectedPath == "##SAME_AS_SOURCE##")
            {
                if (extractList[0].Tag is AudioDataNode firstData)
                {
                    string sourceDir = Path.GetDirectoryName(firstData.CachedAudio.SourcePath);
                    string sourceFileName = Path.GetFileNameWithoutExtension(firstData.CachedAudio.SourcePath);
                    targetRoot = Path.Combine(sourceDir, SanitizeFileName(sourceFileName));
                }
            }

            // Ensure the target root directory exists to store the log file and extracted content.
            try
            {
                Directory.CreateDirectory(targetRoot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create target directory: {targetRoot}\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            StopAudio();
            SetUiState(false);

            // Initialize progress tracking for the extraction process.
            _isWorking = true;
            _isScanning = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            _processedFilesCount = 0;
            _totalFilesToScan = extractList.Count;
            long totalExtractedBytes = 0;
            var failedExtractions = new ConcurrentBag<(string Context, Exception ex)>();

            _scanStopwatch.Restart();

            // Initialize the logger only if verbose logging is enabled.
            if (chkVerboseLog.Checked)
            {
                string logFile = Path.Combine(targetRoot, $"ExtractionLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                _logger = new LogWriter(logFile);

                _logger.WriteRaw("================================================================");
                _logger.WriteRaw($"[SESSION] Extraction Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.WriteRaw("================================================================");
                _logger.WriteRaw($"[TOOL]    App Version:     {AppVersion} ({AppLastUpdate})");
                _logger.WriteRaw($"[TOOL]    Developer:       {AppDeveloper}");
                _logger.WriteRaw($"[ENGINE]  FMOD API:        {FmodFullVersion}");
                _logger.WriteRaw($"[SYSTEM]  OS Version:      {Environment.OSVersion}");
                _logger.WriteRaw($"[SYSTEM]  Processor Count: {Environment.ProcessorCount} Cores (Parallel Processing)");
                _logger.WriteRaw($"[PATH]    Exec Path:       {AppDomain.CurrentDomain.BaseDirectory}");
                _logger.WriteRaw($"[TARGET]  Output Root:     {targetRoot}");
                _logger.WriteRaw($"[QUEUE]   Total Files:     {_totalFilesToScan}");
                _logger.WriteRaw("================================================================");
                _logger.WriteRaw("");
                _logger.WriteRaw("Timestamp\tLevel\tSourceFile\tEventName\tResult\tFormat\tLoopRange(ms)\tDataOffset\tDuration(ms)\tOutputPath\tTimeTaken(ms)");
            }

            // Perform the extraction on a background thread.
            await Task.Run(async () =>
            {
                foreach (var treeNode in extractList)
                {
                    var audioNode = treeNode.Tag as AudioDataNode;
                    var audioInfo = audioNode.CachedAudio;

                    // Update the status text in the UI.
                    int currentCount = Interlocked.Increment(ref _processedFilesCount);
                    int percent = (_totalFilesToScan > 0) ? (currentCount * 100 / _totalFilesToScan) : 0;
                    string statusText = $"[EXTRACTING] [{currentCount}/{_totalFilesToScan}] ({percent}%) | {Path.GetFileName(audioInfo.SourcePath)} -> {audioInfo.Name}";
                    this.BeginInvoke((MethodInvoker)delegate { lblStatus.Text = statusText; });

                    try
                    {
                        // Calculate the relative sub-path to avoid creating redundant folders.
                        string subPath = "";
                        if (treeNode.Parent != null && treeNode.Parent.Tag is FsbFileNode)
                        {
                            subPath = SanitizeFileName(treeNode.Parent.Text);
                        }

                        string finalDir = string.IsNullOrEmpty(subPath) ? targetRoot : Path.Combine(targetRoot, subPath);
                        Directory.CreateDirectory(finalDir);

                        // Extract a single audio item.
                        string outputPath = Path.Combine(finalDir, SanitizeFileName(audioInfo.Name) + ".wav");
                        Stopwatch sw = Stopwatch.StartNew();
                        long writtenBytes = await ExtractSingleWavAsync(audioInfo, outputPath);
                        sw.Stop();

                        // Log to verbose log if enabled.
                        if (writtenBytes >= 0)
                        {
                            string formatInfo = $"{audioInfo.Format}/{audioInfo.Channels}ch/{audioInfo.Bits}bit";
                            string loopInfo = $"{audioInfo.LoopStart}-{audioInfo.LoopEnd}";
                            string offsetInfo = $"0x{audioInfo.DataOffset:X}";
                            _logger?.LogTSV(LogWriter.LogLevel.INFO, Path.GetFileName(audioInfo.SourcePath), audioInfo.Name, "OK", formatInfo, loopInfo, offsetInfo, audioInfo.LengthMs.ToString(), outputPath, sw.ElapsedMilliseconds.ToString());
                            Interlocked.Add(ref totalExtractedBytes, writtenBytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Add any exceptions to the failure list for centralized error logging.
                        failedExtractions.Add((audioInfo.Name, ex));
                    }
                }
            });

            // Finalize the process and update the UI with the results.
            _isWorking = false;
            _isScanning = false;
            _scanStopwatch.Stop();
            int failedCount = failedExtractions.Count;

            // Dispose the verbose logger if it was created.
            if (_logger != null)
            {
                _logger.WriteRaw("");
                _logger.WriteRaw("[INFO] === Extraction Session Finished ===");
                _logger.WriteRaw($"[INFO] Total: {_totalFilesToScan} | Success: {_totalFilesToScan - failedCount} | Failed: {failedCount}");
                _logger.WriteRaw($"[INFO] Total Output Size: {totalExtractedBytes / 1024.0 / 1024.0:F2} MB");
                _logger.WriteRaw($"[INFO] Total Elapsed Time: {_scanStopwatch.Elapsed.TotalSeconds:F2} seconds");
                _logger.Dispose();
                _logger = null;
            }

            // If any errors occurred, write them to the central error log.
            if (!failedExtractions.IsEmpty)
            {
                string errorLogPath = await LogOperationErrorAsync("Audio Extraction", failedExtractions);
                MessageBox.Show(this,
                    $"{failedCount} errors occurred during extraction.\n\n" +
                    $"Details have been saved to the error log:\n{errorLogPath}",
                    "Extraction Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            progressBar.Value = 100;
            lblStatus.Text = $"[COMPLETE] -> Extraction finished. Success: {_totalFilesToScan - failedCount}, Failed: {failedCount}";
            lblElapsedTime.Text = $"Elapsed: {_scanStopwatch.Elapsed:mm\\:ss\\.ff}";
            Application.DoEvents();

            SetUiState(true);
            string reportMessage = $"Process Complete!\n\n" +
                                   $"Total Processed: {_totalFilesToScan}\n" +
                                   $"Success: {_totalFilesToScan - failedCount}\n" +
                                   $"Failed: {failedCount}\n\n" +
                                   $"Elapsed Time: {_scanStopwatch.Elapsed:mm\\:ss\\.ff}\n\n" +
                                   $"Output Location:\n{targetRoot}";

            MessageBoxIcon icon = failedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(reportMessage, "Extraction Report", MessageBoxButtons.OK, icon);

            progressBar.Value = 0;
            lblStatus.Text = "[READY] -> Waiting for next operation.";
        }

        /// <summary>
        /// Extracts a single audio item and writes a log entry for the operation if verbose logging is enabled.
        /// </summary>
        /// <param name="audioNode">The AudioDataNode of the item to extract.</param>
        /// <param name="outputDir">The directory where the WAV file will be saved.</param>
        /// <returns>A tuple containing a success flag and the number of bytes written.</returns>
        private async Task<(bool success, long bytesWritten)> ExtractItemWithLogAsync(AudioDataNode audioNode, string outputDir)
        {
            string p = Path.Combine(outputDir, SanitizeFileName(audioNode.CachedAudio.Name) + ".wav");

            Stopwatch sw = Stopwatch.StartNew();
            long writtenBytes = await ExtractSingleWavAsync(audioNode.CachedAudio, p);
            sw.Stop();

            string sourceName = Path.GetFileName(audioNode.CachedAudio.SourcePath);
            string formatInfo = $"{audioNode.CachedAudio.Format}/{audioNode.CachedAudio.Channels}ch/{audioNode.CachedAudio.Bits}bit";
            string loopInfo = $"{audioNode.CachedAudio.LoopStart}-{audioNode.CachedAudio.LoopEnd}";
            string offsetInfo = $"0x{audioNode.CachedAudio.DataOffset:X}";

            if (writtenBytes >= 0)
            {
                _logger?.LogTSV(LogWriter.LogLevel.INFO, sourceName, audioNode.CachedAudio.Name, "OK", formatInfo, loopInfo, offsetInfo, audioNode.CachedAudio.LengthMs.ToString(), p, sw.ElapsedMilliseconds.ToString());
                return (true, writtenBytes);
            }
            else
            {
                _logger?.LogTSV(LogWriter.LogLevel.ERROR, sourceName, audioNode.CachedAudio.Name, "FAIL", "-", "-", "-", "-", "Error Code Logged", sw.ElapsedMilliseconds.ToString());
                return (false, 0);
            }
        }

        /// <summary>
        /// Recursively finds all audio nodes that meet the specified criteria.
        /// </summary>
        /// <param name="nodes">An enumerable collection of nodes to search through.</param>
        /// <param name="foundNodes">The list where found audio nodes will be added.</param>
        /// <param name="onlyChecked">A boolean indicating whether to only include checked nodes.</param>
        private void FindCheckedAudioNodesRecursive(System.Collections.IEnumerable nodes, List<TreeNode> foundNodes, bool onlyChecked)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Tag is AudioDataNode && (!onlyChecked || n.Checked))
                {
                    foundNodes.Add(n);
                }
                if (n.Nodes.Count > 0)
                {
                    // Recursively call this method for child nodes.
                    FindCheckedAudioNodesRecursive(n.Nodes, foundNodes, onlyChecked);
                }
            }
        }


        /// <summary>
        /// Determines the relative sub-path for an extracted file based on its position in the tree.
        /// </summary>
        /// <param name="audioNode">The TreeNode representing the audio file being extracted.</param>
        /// <returns>A relative path string for the extraction directory.</returns>
        private string GetExtractionSubPath(TreeNode audioNode)
        {
            if (audioNode?.Parent == null || !(audioNode.Tag is NodeData))
            {
                return "Uncategorized";
            }

            var parentNode = audioNode.Parent;
            var parentData = parentNode.Tag as NodeData;

            // This case handles a root node (e.g., a standalone .fsb).
            if (parentNode.Parent == null)
            {
                string rootFileName = parentData.ExtraInfo as string;
                return SanitizeFileName(Path.GetFileNameWithoutExtension(rootFileName));
            }
            // This case handles an intermediate FSB node inside a .bank file.
            else
            {
                var grandparentNode = parentNode.Parent;
                var grandparentData = grandparentNode.Tag as NodeData;

                string bankFileName = grandparentData.ExtraInfo as string;
                string fsbNodeName = parentNode.Text;

                return Path.Combine(
                    SanitizeFileName(Path.GetFileNameWithoutExtension(bankFileName)),
                    SanitizeFileName(Path.GetFileNameWithoutExtension(fsbNodeName))
                );
            }
        }

        /// <summary>
        /// Reads audio data from FMOD and writes it to a standard WAV file.
        /// </summary>
        /// <param name="info">The AudioInfo for the sound to be extracted.</param>
        /// <param name="outputPath">The full path where the WAV file will be saved.</param>
        /// <returns>The number of bytes written to the file.</returns>
        private async Task<long> ExtractSingleWavAsync(AudioInfo info, string outputPath)
        {
            Sound s = new Sound();
            Sound sub = new Sound();
            long bytesWritten = -1;

            try
            {
                // This method now throws exceptions on FMOD/IO errors.
                // This allows the caller to catch and log them centrally.
                await Task.Run(() =>
                {
                    lock (_coreSystemLock)
                    {
                        CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = (uint)info.FileOffset };

                        RESULT res = _coreSystem.createSound(info.SourcePath, MODE.CREATESTREAM | MODE.OPENONLY, ref ex, out s);
                        if (res != RESULT.OK) throw new Exception($"FMOD createSound failed for {info.SourcePath} with error: {res}");

                        s.getNumSubSounds(out int num);
                        if (info.Index < num) s.getSubSound(info.Index, out sub);
                        else sub = s;

                        // Get the raw data format information from FMOD.
                        sub.getLength(out uint lenBytes, TIMEUNIT.PCMBYTES);
                        sub.getFormat(out _, out SOUND_FORMAT fmt, out int ch, out int bits);
                        sub.getDefaults(out float rate, out _);

                        using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096))
                        {
                            byte[] header = CreateWavHeader((int)lenBytes, (int)rate, ch, bits > 0 ? bits : 16, fmt == SOUND_FORMAT.PCMFLOAT);
                            fs.Write(header, 0, header.Length);

                            sub.seekData(0);
                            byte[] buf = new byte[4096];
                            uint totalRead = 0;

                            while (totalRead < lenBytes)
                            {
                                sub.readData(buf, out uint read);
                                if (read == 0) break;
                                fs.Write(buf, 0, (int)read);
                                totalRead += read;
                            }
                            bytesWritten = fs.Length;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Re-throw the exception to be caught by the calling method for central logging.
                throw new Exception($"Failed to extract '{info.Name}': {ex.Message}", ex);
            }
            finally
            {
                lock (_coreSystemLock)
                {
                    if (sub.hasHandle() && sub.handle != s.handle) sub.release();
                    SafeRelease(ref s);
                }
            }

            return bytesWritten;
        }

        /// <summary>
        /// Creates a valid WAV file header based on the provided audio format properties.
        /// </summary>
        /// <param name="length">The length of the raw audio data in bytes.</param>
        /// <param name="rate">The sample rate (e.g., 44100).</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <param name="bits">The number of bits per sample.</param>
        /// <param name="isFloat">A boolean indicating if the audio format is floating-point.</param>
        /// <returns>A byte array containing the WAV header.</returns>
        private byte[] CreateWavHeader(int length, int rate, int channels, int bits, bool isFloat)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + length);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((ushort)(isFloat ? 3 : 1));
                bw.Write((short)channels);
                bw.Write(rate);
                bw.Write(rate * channels * bits / 8);
                bw.Write((short)(channels * bits / 8));
                bw.Write((short)bits);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(length);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Sanitizes a string to create a valid and readable file name.
        /// </summary>
        /// <param name="name">The input string to sanitize.</param>
        /// <returns>A file-system-safe string.</returns>
        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "_";

            // Replace common invalid characters with visually similar Unicode counterparts.
            string sanitized = name
                .Replace(':', '：')
                .Replace('*', '＊')
                .Replace('?', '？')
                .Replace('"', '＂')
                .Replace('<', '〈')
                .Replace('>', '〉')
                .Replace('|', '｜')
                .Replace('/', '／')
                .Replace('\\', '＼');

            // Replace any remaining invalid characters with an underscore.
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(sanitized.Length);

            foreach (char c in sanitized)
            {
                sb.Append(invalidChars.Contains(c) ? '_' : c);
            }

            sanitized = sb.ToString();

            // Handle reserved system file names like "CON" or "PRN".
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            if (FileSystemDefs.ReservedFileNames.Contains(nameWithoutExtension))
            {
                sanitized = "_" + sanitized;
            }

            return sanitized;
        }

        #endregion

        #region 11. Context Menus & Dialogs

        /// <summary>
        /// Handles the Click event of the expandAllToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e) => treeViewInfo.ExpandAll();

        /// <summary>
        /// Handles the Click event of the collapseAllToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e) => treeViewInfo.CollapseAll();

        /// <summary>
        /// Handles the Click event of the copyNameContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyNameContextMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewInfo.SelectedNode != null) Clipboard.SetText(treeViewInfo.SelectedNode.Text);
        }

        /// <summary>
        /// Handles the Click event of the copyPathContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyPathContextMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewInfo.SelectedNode != null) Clipboard.SetText(treeViewInfo.SelectedNode.FullPath);
        }

        /// <summary>
        /// Handles the Click event of the copyGuidContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyGuidContextMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewInfo.SelectedNode?.Tag is NodeData d)
            {
                if (d is EventNode eventNode && eventNode.EventObject.isValid())
                {
                    eventNode.EventObject.getID(out GUID id);
                    Clipboard.SetText(GuidToString(id));
                }
                else if (d is BankNode bankNode && bankNode.BankObject.isValid())
                {
                    bankNode.BankObject.getID(out GUID id);
                    Clipboard.SetText(GuidToString(id));
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the playContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void playContextMenuItem_Click(object sender, EventArgs e) => TogglePause();

        /// <summary>
        /// Handles the Click event of the stopContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void stopContextMenuItem_Click(object sender, EventArgs e) => StopAudio();

        /// <summary>
        /// Handles the Click event of the extractContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void extractContextMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = treeViewInfo.SelectedNode;
            string finalDir = string.Empty; // Declare outside the try block for logging purposes.

            try
            {
                if (selectedNode?.Tag is AudioDataNode data)
                {
                    using (var fbd = new FolderBrowserDialog { Description = "Select a folder to extract the audio file into." })
                    {
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            // Construct the final output path.
                            string baseOutputDir = fbd.SelectedPath;
                            string subPath = GetExtractionSubPath(selectedNode);
                            finalDir = Path.Combine(baseOutputDir, subPath);
                            Directory.CreateDirectory(finalDir);

                            string finalFilePath = Path.Combine(finalDir, SanitizeFileName(selectedNode.Text) + ".wav");

                            // Extract the audio data to a .wav file.
                            long bytesWritten = await ExtractSingleWavAsync(data.CachedAudio, finalFilePath);
                            if (bytesWritten >= 0)
                            {
                                MessageBox.Show($"File successfully saved to:\n{finalFilePath}", "Extraction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                // This path is unlikely to be hit as errors throw exceptions, but is kept for robustness.
                                MessageBox.Show("Failed to extract audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception by logging it and notifying the user.
                string context = $"Single file extraction of '{(selectedNode != null ? selectedNode.FullPath : "Unknown")}' to '{(string.IsNullOrEmpty(finalDir) ? "N/A" : finalDir)}'";
                string logFilePath = await LogOperationErrorAsync("Single File Extraction", new[] { (context, ex) });

                string userMessage = "An unexpected error occurred during extraction.\n\n" +
                                     $"Technical details have been saved to the log file:\n{Path.GetFileName(logFilePath)}";

                MessageBox.Show(this, userMessage, "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the rebuildSoundContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void rebuildSoundContextMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Validate the selection and environment.
                if (!(_currentSelection is AudioDataNode audioNode))
                {
                    MessageBox.Show("Please select a single audio file to replace.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(FsBankClExecutable))
                {
                    MessageBox.Show($"Rebuild tool '{FsBankClExecutable}' not found in the application directory.\nPlease place it alongside the extractor.", "Tool Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var audioInfo = audioNode.CachedAudio;

                // 2. Prompt the user for the replacement audio file.
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Audio Files|*.wav;*.ogg;*.mp3;*.flac|All Files|*.*",
                    Title = "Select Replacement Audio File"
                };
                if (ofd.ShowDialog() != DialogResult.OK) return;

                string replacementAudioPath = ofd.FileName;

                // 3. Validate the replacement audio file and get its duration for a pre-check.
                uint newDurationMs = 0;
                Sound newSound = new Sound();
                try
                {
                    CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                    lock (_coreSystemLock)
                    {
                        CheckFmodResult(_coreSystem.createSound(replacementAudioPath, MODE.OPENONLY, ref exinfo, out newSound));
                    }

                    if (newSound.hasHandle())
                    {
                        newSound.getLength(out newDurationMs, TIMEUNIT.MS);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading replacement audio file properties: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    SafeRelease(ref newSound);
                }

                if (newDurationMs == 0)
                {
                    MessageBox.Show("Could not determine the duration of the replacement audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4. Warn the user if the replacement audio is longer than the original, and offer a choice.
                if (newDurationMs > audioInfo.LengthMs)
                {
                    var warningBuilder = new StringBuilder();
                    warningBuilder.AppendLine("Warning: Replacement audio is longer than the original.");
                    warningBuilder.AppendLine();
                    warningBuilder.AppendLine($"  • Original Duration:    {TimeSpan.FromMilliseconds(audioInfo.LengthMs):mm\\:ss\\.fff}");
                    warningBuilder.AppendLine($"  • Replacement Duration: {TimeSpan.FromMilliseconds(newDurationMs):mm\\:ss\\.fff}");
                    warningBuilder.AppendLine();
                    warningBuilder.AppendLine("Proceeding may cause unexpected behavior with game event timelines or looping, as these often rely on the original audio's duration. Stability is not guaranteed.");
                    warningBuilder.AppendLine();
                    warningBuilder.AppendLine("Do you want to continue anyway?");

                    if (MessageBox.Show(this, warningBuilder.ToString(), "Length Exceeded Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        lblStatus.Text = "Rebuild cancelled by user due to length mismatch.";
                        return; // Stop the process if user chooses No.
                    }
                }

                // 5. Show the options dialog to the user to get encoding settings.
                RebuildOptions rebuildOptions;
                using (var optionsForm = new RebuildOptionsForm(audioInfo, replacementAudioPath))
                {
                    if (optionsForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                    rebuildOptions = optionsForm.Options;
                }

                // 6. Build a dynamic confirmation message for the user to review.
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Please review the information before rebuilding:");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"  • Original Duration:      {TimeSpan.FromMilliseconds(audioInfo.LengthMs):mm\\:ss\\.fff}");
                messageBuilder.AppendLine($"  • Replacement Duration:   {TimeSpan.FromMilliseconds(newDurationMs):mm\\:ss\\.fff}");
                messageBuilder.AppendLine();

                if (newDurationMs < audioInfo.LengthMs)
                {
                    messageBuilder.AppendLine("NOTE: The new audio is SHORTER. The remaining time might be filled with silence, which could affect looping behavior.");
                }

                if ((audioInfo.Mode & MODE.LOOP_NORMAL) != 0 || audioInfo.LoopEnd > 0)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine("LOOPING WARNING: This sound has loop points based on the original timeline.");
                    messageBuilder.AppendLine("   The loop may behave unexpectedly if the new audio's length is different.");
                }

                messageBuilder.AppendLine();
                messageBuilder.AppendLine("Do you want to proceed with the rebuild?");

                // 7. Show the final confirmation dialog.
                if (MessageBox.Show(this, messageBuilder.ToString(), "Confirm Rebuild", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                {
                    lblStatus.Text = "Rebuild cancelled by user.";
                    return;
                }

                // 8. Prompt for the final save location.
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "FMOD Files|*.bank;*.fsb",
                    FileName = Path.GetFileName(audioInfo.SourcePath)
                };

                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    lblStatus.Text = "Rebuild cancelled by user.";
                    return;
                }

                string finalSavePath = sfd.FileName;

                // 9. Prepare the UI and start the long-running rebuild process.
                SetUiState(false);
                _isWorking = true;
                _isScanning = false;
                progressBar.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "Starting rebuild process...";
                StopAudio();

                _scanStopwatch.Restart();

                bool success = false;
                string workspacePath = null;

                // Standardized log file naming.
                string logFileName = $"RebuildLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                // [FIX 02] Save the log in the directory of the final output file, not the exe directory.
                string outputDirectory = Path.GetDirectoryName(finalSavePath);
                string logFilePath = Path.Combine(outputDirectory, logFileName);

                // [FIX 01] Initialize the logger ONLY if the Verbose Log checkbox is checked.
                if (chkVerboseLog.Checked)
                {
                    _logger = new LogWriter(logFilePath);

                    _logger.WriteRaw("================================================================");
                    _logger.WriteRaw($"[SESSION] Rebuild Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _logger.WriteRaw("================================================================");
                    _logger.WriteRaw($"[TOOL]    App Version:     {AppVersion} ({AppLastUpdate})");
                    _logger.WriteRaw($"[ENGINE]  FMOD API:        {FmodFullVersion}");
                    _logger.WriteRaw($"[SOURCE]  Original File:   {audioInfo.SourcePath}");
                    _logger.WriteRaw($"[SOURCE]  Target Sound:    {audioInfo.Name} (Index: {audioInfo.Index})");
                    _logger.WriteRaw($"[REPLACE] New Audio File:  {replacementAudioPath}");
                    _logger.WriteRaw($"[OUTPUT]  Destination:     {finalSavePath}");
                    _logger.WriteRaw($"[OPTIONS] Encoding:        {rebuildOptions.EncodingFormat}");
                    _logger.WriteRaw($"[OPTIONS] Quality Target:  {rebuildOptions.Quality}% (for VORBIS)");
                    _logger.WriteRaw("================================================================");
                    _logger.WriteRaw("");
                }

                try
                {
                    var (rebuildSuccess, path) = await PerformRebuildAndRepackAsync(audioNode, replacementAudioPath, finalSavePath, rebuildOptions, (status) =>
                    {
                        // Log status updates as they happen (safe to call even if _logger is null).
                        _logger?.WriteRaw($"[STATUS] {status}");
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() => { lblStatus.Text = status; }));
                        }
                        else
                        {
                            lblStatus.Text = status;
                        }
                    });

                    success = rebuildSuccess;
                    workspacePath = path;
                }
                catch (Exception ex)
                {
                    // Log critical rebuild errors to the central error log.
                    string errorLogPath = await LogOperationErrorAsync("Rebuild Process", new[] { (replacementAudioPath, ex) });

                    // Also write a brief note in the specific rebuild log if enabled.
                    _logger?.WriteRaw("----------------------------------------------------------------");
                    _logger?.WriteRaw($"[CRITICAL ERROR] An unhandled exception occurred. See the main error log for details: {errorLogPath}");
                    _logger?.WriteRaw($"[MESSAGE] {ex.Message}");
                    _logger?.WriteRaw("----------------------------------------------------------------");
                    MessageBox.Show($"A critical error occurred during the rebuild process:\n\n{ex.Message}\n\nSee '{Path.GetFileName(errorLogPath)}' for details.", "Rebuild Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    success = false;
                }
                finally
                {
                    // Clean up and restore the UI after the operation.
                    _isWorking = false;
                    _scanStopwatch.Stop();

                    if (workspacePath != null && Directory.Exists(workspacePath))
                    {
                        try { await Task.Run(() => Directory.Delete(workspacePath, true)); } catch { /* Ignore cleanup errors. */ }
                    }

                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = 0;
                    SetUiState(true);

                    lblStatus.Text = success ? "[COMPLETE] -> Rebuild successful." : "[ERROR] -> Rebuild failed.";
                    lblElapsedTime.Text = $"Elapsed: {_scanStopwatch.Elapsed:mm\\:ss\\.ff}";

                    if (success)
                    {
                        _logger?.WriteRaw("");
                        _logger?.WriteRaw("[RESULT] Rebuild operation finished successfully.");
                        MessageBox.Show($"Successfully rebuilt and saved to:\n{finalSavePath}", "Rebuild Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        _logger?.WriteRaw("");
                        _logger?.WriteRaw("[RESULT] Rebuild operation failed. See main error log for details if an exception occurred.");

                        // Construct failure message dynamically based on whether log was saved.
                        string failureMsg = "The rebuild process failed.";
                        if (chkVerboseLog.Checked)
                        {
                            failureMsg += $"\n\nA process log is available at:\n{logFilePath}";
                        }
                        else
                        {
                            failureMsg += "\n\n(Verbose Log was disabled. Enable it to see detailed steps next time.)";
                        }
                        failureMsg += "\n\nIf a critical error occurred, check the main ErrorLog file as well.";

                        MessageBox.Show(failureMsg, "Rebuild Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (_logger != null)
                    {
                        _logger.WriteRaw($"[SESSION] Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.");
                        _logger.Dispose();
                        _logger = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors and log them centrally.
                await LogOperationErrorAsync("Rebuild Process (Outer)", new[] { ("N/A", ex) });
                MessageBox.Show(this, $"An unexpected error occurred during the rebuild process: {ex.Message}", "Rebuild Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Ensure logger is disposed even on outer-level errors.
                if (_logger != null)
                {
                    _logger.Dispose();
                    _logger = null;
                }
                RestoreUiAfterError();
            }
        }

        #region Rebuild Helpers (Binary Search Implementation)

        /// <summary>
        /// Builds an FSB with a specified quality and returns its size.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the source audio file or build list.</param>
        /// <param name="outputPath">The path for the output FSB file.</param>
        /// <param name="options">The rebuild options, excluding quality.</param>
        /// <param name="quality">The quality level (0-100) to use for the build.</param>
        /// <param name="updateStatus">An action to update the UI status text.</param>
        /// <returns>The size of the built FSB in bytes, or -1 on failure.</returns>
        private async Task<long> BuildAndGetSizeAsync(string sourceAudioPath, string outputPath, RebuildOptions options, int quality, Action<string> updateStatus)
        {
            var tempOptions = new RebuildOptions
            {
                EncodingFormat = options.EncodingFormat,
                Quality = quality
            };

            updateStatus($"[REBUILDING] (Step 2 of 4) | Finding Quality -> Trial with {quality}%...");

            bool success = await RunFsBankClAsync(sourceAudioPath, outputPath, tempOptions);

            if (success && File.Exists(outputPath))
            {
                long size = new FileInfo(outputPath).Length;
                updateStatus($"[REBUILDING] (Step 2 of 4) | Finding Quality -> {quality}% = {size} bytes");
                return size;
            }
            else
            {
                updateStatus($"[REBUILDING] (Step 2 of 4) | Finding Quality -> Build failed at {quality}%");
                return -1;
            }
        }

        /// <summary>
        /// Finds the optimal quality to build an FSB that fits a target size using a binary search.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the source audio file or build list.</param>
        /// <param name="outputPath">The path for the final output FSB file.</param>
        /// <param name="options">The rebuild options.</param>
        /// <param name="targetSize">The maximum allowed size for the output FSB.</param>
        /// <param name="updateStatus">An action to update the UI status text.</param>
        /// <returns>true if a successful build matching the size constraint was created; otherwise, false.</returns>
        private async Task<bool> RunFsBankClWithSizeModeAsync_BinarySearch(string sourceAudioPath, string outputPath, RebuildOptions options, long targetSize, Action<string> updateStatus)
        {
            // The quality parameter is only supported for VORBIS format in fsbankcl.
            bool canAdjustQuality = options.EncodingFormat == SOUND_TYPE.VORBIS;

            if (!canAdjustQuality)
            {
                updateStatus($"[REBUILDING] (Step 2 of 4) | Building with fixed format ({options.EncodingFormat})...");
                long newSize = await BuildAndGetSizeAsync(sourceAudioPath, outputPath, options, options.Quality, (s) => { });

                if (newSize == -1)
                {
                    updateStatus($"[ERROR] -> Build failed. fsbankcl.exe may have encountered an error.");
                    return false;
                }

                // If the new file is larger, confirm with the user before proceeding.
                if (newSize > targetSize)
                {
                    updateStatus($"[WAITING] Awaiting user confirmation for oversized file...");

                    string warningMessage = "Rebuild Warning: The resulting file is larger than the original.\n\n" +
                                            $" • Original Size: {targetSize} bytes\n" +
                                            $" • New Size:      {newSize} bytes (+{newSize - targetSize} bytes)\n\n" +
                                            "Patching this oversized FSB into a .bank file will likely corrupt it.\n" +
                                            "This is only safe if you are saving it as a standalone .fsb file.\n\n" +
                                            "Do you want to proceed and save the oversized file?";

                    DialogResult result = MessageBox.Show(warningMessage, "Oversized File Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        updateStatus($"[CANCELLED] -> User cancelled the operation due to oversized file.");
                        return false;
                    }

                    updateStatus($"[PROCEEDING] -> User chose to proceed with the oversized file.");
                    return true;
                }

                if (newSize < targetSize)
                {
                    updateStatus($"[REBUILDING] (Step 3 of 4) | Padding FSB with {targetSize - newSize} bytes...");
                    using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                    {
                        fs.SetLength(targetSize);
                    }
                }
                return true;
            }

            int minQuality = 0;
            int maxQuality = 100;
            int bestKnownQuality = -1;

            updateStatus("[REBUILDING] (Step 2 of 4) | Starting binary search for optimal quality...");

            while (minQuality <= maxQuality)
            {
                int midQuality = minQuality + (maxQuality - minQuality) / 2;
                string tempBuildPath = outputPath + ".tmp";

                long currentSize = await BuildAndGetSizeAsync(sourceAudioPath, tempBuildPath, options, midQuality, updateStatus);

                if (File.Exists(tempBuildPath))
                {
                    try { File.Delete(tempBuildPath); } catch { }
                }

                if (currentSize != -1 && currentSize <= targetSize)
                {
                    bestKnownQuality = midQuality;
                    minQuality = midQuality + 1;
                }
                else
                {
                    maxQuality = midQuality - 1;
                }
            }

            if (bestKnownQuality == -1)
            {
                updateStatus($"[ERROR] -> Could not find any quality that fits within {targetSize} bytes.");
                return false;
            }

            updateStatus($"[REBUILDING] (Step 3 of 4) | Optimal quality found: {bestKnownQuality}%. Performing final build...");
            long finalSize = await BuildAndGetSizeAsync(sourceAudioPath, outputPath, options, bestKnownQuality, (s) => { });

            if (finalSize == -1 || finalSize > targetSize)
            {
                updateStatus($"[ERROR] -> Final build with quality {bestKnownQuality}% failed or exceeded size.");
                return false;
            }

            if (finalSize < targetSize)
            {
                updateStatus($"[REBUILDING] (Step 3 of 4) | Padding final FSB with {targetSize - finalSize} bytes...");
                using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                {
                    fs.SetLength(targetSize);
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Manages the entire process of rebuilding an FSB and patching it back into the source file.
        /// </summary>
        /// <param name="targetNode">The node representing the audio to be replaced.</param>
        /// <param name="replacementAudioPath">The path to the new audio file.</param>
        /// <param name="finalSavePath">The path where the final modified file will be saved.</param>
        /// <param name="options">The user-selected rebuild options.</param>
        /// <param name="updateStatus">An action to update the UI status text.</param>
        /// <returns>A tuple containing a success flag and the path to the temporary workspace.</returns>
        private async Task<(bool Success, string WorkspacePath)> PerformRebuildAndRepackAsync(AudioDataNode targetNode, string replacementAudioPath, string finalSavePath, RebuildOptions options, Action<string> updateStatus)
        {
            string workspacePath = null;

            try
            {
                updateStatus("[REBUILDING] (Step 1 of 4) | Preparing workspace...");
                workspacePath = await SetupWorkspaceAsync(targetNode, updateStatus);
                _tempDirectories.Add(workspacePath);

                updateStatus("[REBUILDING] (Step 1 of 4) | Replacing audio file...");
                await ReplaceAudioInWorkspaceAsync(workspacePath, targetNode.CachedAudio.Index, replacementAudioPath, options);

                string rebuiltFsbPath = Path.Combine(workspacePath, "rebuilt.fsb");
                string buildListPath = Path.Combine(workspacePath, "buildlist.txt");

                byte[] originalBankData = await ReadAllBytesAsync(targetNode.CachedAudio.SourcePath);
                long originalFsbSize = GetOriginalFsbLength(originalBankData, targetNode.FsbChunkOffset);

                if (originalFsbSize <= 0)
                {
                    updateStatus("[ERROR] -> Could not determine original FSB size.");
                    MessageBox.Show("Failed to determine the size of the original sound data. Cannot proceed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return (false, workspacePath);
                }

                updateStatus($"[REBUILDING] (Step 2 of 4) | Target size: {originalFsbSize} bytes. Finding optimal quality...");

                bool buildSuccess = await RunFsBankClWithSizeModeAsync_BinarySearch(buildListPath, rebuiltFsbPath, options, originalFsbSize, updateStatus);

                if (!buildSuccess)
                {
                    updateStatus("[ERROR] -> fsbankcl.exe failed to meet size constraints!");
                    return (false, workspacePath);
                }

                updateStatus("[REBUILDING] (Step 4 of 4) | Patching original file with new FSB...");
                await PatchFileWithNewFsbAsync(targetNode, rebuiltFsbPath, finalSavePath, originalBankData);

                return (true, workspacePath);
            }
            catch (Exception ex)
            {
                updateStatus($"[ERROR] -> {ex.Message}");
                return (false, workspacePath);
            }
        }

        /// <summary>
        /// Sets up a temporary workspace by extracting all sub-sounds from the target FSB.
        /// </summary>
        /// <param name="targetNode">The node representing the target FSB container.</param>
        /// <param name="updateStatus">An action to update the UI status text.</param>
        /// <returns>The path to the created workspace directory.</returns>
        private async Task<string> SetupWorkspaceAsync(AudioDataNode targetNode, Action<string> updateStatus)
        {
            var audioInfo = targetNode.CachedAudio;
            string sourcePath = audioInfo.SourcePath;
            long fsbOffset = targetNode.FsbChunkOffset;

            string workspaceName = SanitizeFileName($"{Path.GetFileName(sourcePath)}_{fsbOffset}");
            string workspacePath = Path.Combine(Path.GetTempPath(), "FsbRebuildTool", workspaceName);

            if (Directory.Exists(workspacePath))
            {
                await Task.Run(() => Directory.Delete(workspacePath, true));
            }
            Directory.CreateDirectory(workspacePath);

            string audioSourcePath = Path.Combine(workspacePath, "AudioSource");
            Directory.CreateDirectory(audioSourcePath);

            // Read the raw FSB data from the source file.
            byte[] fsbData;
            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                fs.Seek(fsbOffset, SeekOrigin.Begin);
                fsbData = new byte[fs.Length - fsbOffset];
                await fs.ReadAsync(fsbData, 0, fsbData.Length);
            }

            string tempFsbPath = Path.Combine(workspacePath, "source.fsb");
            await WriteAllBytesAsync(tempFsbPath, fsbData);

            List<string> buildListPaths = new List<string>();

            var manifest = await Task.Run(async () =>
            {
                var localManifest = new FsbManifest();
                var subSoundInfos = new List<SubSoundManifestInfo>();

                Sound fsbSound = new Sound();

                try
                {
                    CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };
                    CheckFmodResult(_coreSystem.createSound(tempFsbPath, MODE.OPENONLY, ref exinfo, out fsbSound));

                    fsbSound.getNumSubSounds(out int numSubSounds);
                    if (numSubSounds == 0) numSubSounds = 1;

                    for (int i = 0; i < numSubSounds; i++)
                    {
                        this.BeginInvoke((MethodInvoker)delegate { updateStatus($"[REBUILDING] (Step 1 of 4) | Extracting sub-sound {i + 1} of {numSubSounds}..."); });

                        Sound subSound = new Sound();
                        try
                        {
                            if (numSubSounds > 1) fsbSound.getSubSound(i, out subSound);
                            else subSound = fsbSound;

                            var subInfo = GetAudioInfo(subSound, i, tempFsbPath, 0);
                            if (i == 0) localManifest.BuildFormat = subInfo.Type;

                            string indexFolder = i.ToString("D3");
                            string subDirectoryPath = Path.Combine(audioSourcePath, indexFolder);
                            Directory.CreateDirectory(subDirectoryPath);

                            string fileNameOnly = SanitizeFileName($"{subInfo.Name}.wav");
                            string fullWavPath = Path.Combine(subDirectoryPath, fileNameOnly);

                            await ExtractSingleWavAsync(subInfo, fullWavPath);

                            buildListPaths.Add(fullWavPath);

                            string relativePath = Path.Combine(indexFolder, fileNameOnly);
                            subSoundInfos.Add(new SubSoundManifestInfo
                            {
                                Index = i,
                                Name = subInfo.Name,
                                OriginalFileName = relativePath,
                                Looping = (subInfo.Mode & MODE.LOOP_NORMAL) != 0,
                                LoopStart = subInfo.LoopStart,
                                LoopEnd = subInfo.LoopEnd,
                            });
                        }
                        finally
                        {
                            if (subSound.hasHandle() && subSound.handle != fsbSound.handle) subSound.release();
                        }
                    }
                    localManifest.SubSounds = subSoundInfos;
                    return localManifest;
                }
                finally
                {
                    if (fsbSound.hasHandle()) fsbSound.release();
                }
            });

            // Generate the buildlist.txt file required by fsbankcl.exe.
            string buildListFile = Path.Combine(workspacePath, "buildlist.txt");
            await WriteAllTextAsync(buildListFile, string.Join(Environment.NewLine, buildListPaths));

            // Generate the manifest.json file to store metadata.
            string manifestPath = Path.Combine(workspacePath, "manifest.json");
            await WriteAllTextAsync(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));

            return workspacePath;
        }

        /// <summary>
        /// Replaces a specific audio file in the workspace with the new user-provided audio.
        /// </summary>
        /// <param name="workspacePath">The path to the rebuild workspace.</param>
        /// <param name="targetIndex">The index of the sub-sound to replace.</param>
        /// <param name="newAudioPath">The path to the new audio file.</param>
        /// <param name="options">The user-selected rebuild options.</param>
        private async Task ReplaceAudioInWorkspaceAsync(string workspacePath, int targetIndex, string newAudioPath, RebuildOptions options)
        {
            string manifestPath = Path.Combine(workspacePath, "manifest.json");
            var manifestText = await ReadAllTextAsync(manifestPath);
            var manifest = JsonConvert.DeserializeObject<FsbManifest>(manifestText);

            var targetSubSound = manifest.SubSounds.FirstOrDefault(s => s.Index == targetIndex);
            if (targetSubSound == null)
            {
                throw new Exception($"Could not find sub-sound with index {targetIndex} in manifest.");
            }

            string audioSourcePath = Path.Combine(workspacePath, "AudioSource");
            string targetWavPath = Path.Combine(audioSourcePath, targetSubSound.OriginalFileName);

            Sound newSound;
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)) };

            lock (_coreSystemLock)
            {
                CheckFmodResult(_coreSystem.createSound(newAudioPath, MODE.CREATESTREAM, ref exinfo, out newSound));
            }

            // Extract the new audio as a WAV file to overwrite the old one.
            var tempInfo = GetAudioInfo(newSound, 0, newAudioPath, 0);
            await ExtractSingleWavAsync(tempInfo, targetWavPath);

            lock (_coreSystemLock)
            {
                newSound.release();
            }

            // Update the manifest with the new build format.
            manifest.BuildFormat = options.EncodingFormat;
            await WriteAllTextAsync(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        /// <summary>
        /// Executes the fsbankcl.exe command-line tool with the specified arguments.
        /// </summary>
        /// <param name="sourceAudioPath">The path to the source audio or build list.</param>
        /// <param name="outputPath">The path for the output FSB file.</param>
        /// <param name="options">The rebuild options containing format and quality.</param>
        /// <returns>true if the process completes successfully; otherwise, false.</returns>
        private async Task<bool> RunFsBankClAsync(string sourceAudioPath, string outputPath, RebuildOptions options)
        {
            string formatArg;
            switch (options.EncodingFormat)
            {
                case SOUND_TYPE.VORBIS: formatArg = "vorbis"; break;
                case SOUND_TYPE.FADPCM: formatArg = "fadpcm"; break;
                // FMOD's SOUND_TYPE.USER corresponds to uncompressed PCM data.
                case SOUND_TYPE.USER:
                default: formatArg = "pcm"; break;
            }

            string qualityArg = "";
            // Quality adjustment via binary search is only applicable to VORBIS encoding.
            if (options.EncodingFormat == SOUND_TYPE.VORBIS)
            {
                qualityArg = $"-q {options.Quality}";
            }

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = FsBankClExecutable;
                    process.StartInfo.Arguments = $"-o \"{outputPath}\" -format {formatArg} {qualityArg} \"{sourceAudioPath}\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    process.Start();

                    // Asynchronously read the output and error streams to prevent deadlocks.
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Asynchronously wait for the process to exit.
                    await Task.Run(() => process.WaitForExit());

                    // Wait for the stream reading to complete.
                    await Task.WhenAll(outputTask, errorTask);

                    string error = await errorTask;

                    if (process.ExitCode != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"fsbankcl.exe exited with code {process.ExitCode}.");
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            System.Diagnostics.Debug.WriteLine($"Error Output: {error}");
                        }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An exception occurred while running fsbankcl.exe: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Patches a source file (.bank or .fsb) with the newly generated FSB data.
        /// </summary>
        /// <param name="targetNode">The node containing information about the original file.</param>
        /// <param name="newFsbPath">The path to the newly built FSB file.</param>
        /// <param name="finalSavePath">The path where the final modified file will be saved.</param>
        /// <param name="originalBankData">The byte array of the original source file.</param>
        private async Task PatchFileWithNewFsbAsync(AudioDataNode targetNode, string newFsbPath, string finalSavePath, byte[] originalBankData)
        {
            string sourcePath = targetNode.CachedAudio.SourcePath;
            long fsbOffset = targetNode.FsbChunkOffset;
            byte[] newFsbData = await ReadAllBytesAsync(newFsbPath);

            // If the source was a standalone .fsb, simply overwrite it.
            if (fsbOffset == 0 && Path.GetExtension(sourcePath).ToLower() == ".fsb")
            {
                await WriteAllBytesAsync(finalSavePath, newFsbData);
                return;
            }

            long oldFsbLength = GetOriginalFsbLength(originalBankData, fsbOffset);

            // Construct the new file by combining the prefix, the new FSB, and the suffix.
            using (var fs = new FileStream(finalSavePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await fs.WriteAsync(originalBankData, 0, (int)fsbOffset);
                await fs.WriteAsync(newFsbData, 0, newFsbData.Length);

                long suffixStart = fsbOffset + oldFsbLength;
                if (suffixStart < originalBankData.Length)
                {
                    await fs.WriteAsync(originalBankData, (int)suffixStart, (int)(originalBankData.Length - suffixStart));
                }
            }
        }

        /// <summary>
        /// Gets the length of the original FSB data chunk from the FSB header.
        /// </summary>
        /// <param name="bankData">The byte array of the source file.</param>
        /// <param name="fsbOffset">The starting offset of the FSB chunk.</param>
        /// <returns>The length of the FSB chunk in bytes.</returns>
        private long GetOriginalFsbLength(byte[] bankData, long fsbOffset)
        {
            const int fileSizeOffset = 0x08;
            const int sampleHeadersSizeOffset = 0x0C;
            const int dataSizeOffset = 0x10;

            if (fsbOffset + 0x14 > bankData.Length)
            {
                return FallbackGetFsbLength(bankData, fsbOffset);
            }

            try
            {
                // Attempt to read the total size directly from the FSB header.
                uint totalChunkSize = BitConverter.ToUInt32(bankData, (int)(fsbOffset + fileSizeOffset));
                if (totalChunkSize > 0 && fsbOffset + totalChunkSize <= bankData.Length)
                {
                    uint sampleHeadersSize = BitConverter.ToUInt32(bankData, (int)(fsbOffset + sampleHeadersSizeOffset));
                    uint dataSize = BitConverter.ToUInt32(bankData, (int)(fsbOffset + dataSizeOffset));

                    if (totalChunkSize >= 64 + sampleHeadersSize + dataSize)
                    {
                        return totalChunkSize;
                    }
                }
            }
            catch { }

            // If header parsing fails, use a fallback method.
            return FallbackGetFsbLength(bankData, fsbOffset);
        }

        /// <summary>
        /// Provides a fallback method to determine FSB length by searching for the next FSB signature.
        /// </summary>
        /// <param name="bankData">The byte array of the source file.</param>
        /// <param name="fsbOffset">The starting offset of the FSB chunk.</param>
        /// <returns>The calculated length of the FSB chunk.</returns>
        private long FallbackGetFsbLength(byte[] bankData, long fsbOffset)
        {
            byte[] signature = { 0x46, 0x53, 0x42, 0x35 }; // "FSB5"

            for (long i = fsbOffset + signature.Length; i <= bankData.Length - signature.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (bankData[i + j] != signature[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i - fsbOffset;
                }
            }
            return bankData.Length - fsbOffset;
        }

        /// <summary>
        /// Handles the Click event of the IndexToolItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IndexToolItem_Click(object sender, EventArgs e)
        {
            TreeNode targetNode = treeViewInfo.SelectedNode;
            if (targetNode == null) return;

            if (targetNode.Tag is AudioDataNode)
            {
                targetNode = targetNode.Parent;
            }

            // Validate that the selected node is a container of audio files.
            bool isValidContainer = false;
            if (targetNode.Tag is BankNode || targetNode.Tag is FsbFileNode)
            {
                foreach (TreeNode child in targetNode.Nodes)
                {
                    if (child.Tag is AudioDataNode)
                    {
                        isValidContainer = true;
                        break;
                    }
                }
            }

            if (!isValidContainer)
            {
                MessageBox.Show("Please select a file/folder that directly contains audio files.\n(e.g., an FSB node or a Bank node with audio)",
                    "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new IndexToolForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string input = form.InputString;
                    if (form.IsJumpMode)
                    {
                        int target = ExtractFirstNumber(input);
                        if (target != -1) PerformJumpToIndex(targetNode, target);
                        else MessageBox.Show("Invalid number format for Jump.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        HashSet<int> targets = ParseRangeString(input);
                        PerformSmartSelect(targetNode, targets);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the first integer from a string.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>The first integer found, or -1 if no integer is found.</returns>
        private int ExtractFirstNumber(string input)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int val)) return val;
            return -1;
        }

        /// <summary>
        /// Parses a string containing numbers and ranges (e.g., "1, 3, 5-8") into a set of integers.
        /// </summary>
        /// <param name="input">The range string to parse.</param>
        /// <returns>A HashSet containing all integers in the specified ranges.</returns>
        private HashSet<int> ParseRangeString(string input)
        {
            HashSet<int> result = new HashSet<int>();
            string[] parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string p = part.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                if (p.Contains("-"))
                {
                    string[] rangeParts = p.Split('-');
                    if (rangeParts.Length >= 2 &&
                        int.TryParse(rangeParts[0], out int start) &&
                        int.TryParse(rangeParts[1], out int end))
                    {
                        int min = Math.Min(start, end);
                        int max = Math.Max(start, end);
                        for (int i = min; i <= max; i++) result.Add(i);
                    }
                }
                else
                {
                    if (int.TryParse(p, out int val)) result.Add(val);
                }
            }
            return result;
        }

        /// <summary>
        /// Jumps to and selects the audio node with the specified index.
        /// </summary>
        /// <param name="parent">The parent node containing the audio files.</param>
        /// <param name="targetIndex">The index of the audio file to find.</param>
        private void PerformJumpToIndex(TreeNode parent, int targetIndex)
        {
            foreach (TreeNode node in parent.Nodes)
            {
                if (node.Tag is AudioDataNode nd)
                {
                    if (nd.CachedAudio.Index == targetIndex)
                    {
                        treeViewInfo.SelectedNode = node;
                        node.EnsureVisible(); // Expand parent and scroll to the item.
                        treeViewInfo.Focus();
                        return;
                    }
                }
            }
            MessageBox.Show($"Index {targetIndex} not found in this file.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Selects (checks) all audio nodes whose indices are in the target set.
        /// </summary>
        /// <param name="parent">The parent node containing the audio files.</param>
        /// <param name="targets">A HashSet of indices to select.</param>
        private void PerformSmartSelect(TreeNode parent, HashSet<int> targets)
        {
            treeViewInfo.BeginUpdate();
            int count = 0;

            foreach (TreeNode node in parent.Nodes)
            {
                if (node.Tag is AudioDataNode nd)
                {
                    if (targets.Contains(nd.CachedAudio.Index))
                    {
                        node.Checked = true;
                        count++;
                    }
                }
            }

            treeViewInfo.EndUpdate();
            MessageBox.Show($"{count} items selected.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows the help form.
        /// </summary>
        private void ShowHelpForm()
        {
            HelpForm helpForm = new HelpForm();
            helpForm.Show();
        }

        /// <summary>
        /// Shows the about dialog.
        /// </summary>
        private void ShowAboutDialog()
        {
            // Extract the year from the update date for the copyright notice.
            string copyrightYear = AppLastUpdate.Length >= 4 ? AppLastUpdate.Substring(0, 4) : DateTime.Now.Year.ToString();

            MessageBox.Show($"FSB/BANK Extractor & Rebuilder (GUI)\n" +
                $"Version: {AppVersion}\n" +
                $"Update: {AppLastUpdate}\n\n" +
                $"Developer: {AppDeveloper}\n" +
                $"Website: {AppWebsite}\n\n" +
                $"Using FMOD Studio API version {FmodApiVersion}\n" +
                $" - Studio API minor release (build {FmodBuildNumber})\n\n" +
                $"© {copyrightYear} {AppDeveloper}. All rights reserved.", "Program Information");
        }

        #endregion

        #region 12. Helper Methods & Classes

        /// <summary>
        /// Reads the entire content of a file into a byte array asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A byte array containing the file's content.</returns>
        private async Task<byte[]> ReadAllBytesAsync(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (MemoryStream ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Writes a detailed log file for exceptions that occurred during an operation.
        /// This is the centralized method for all error logging.
        /// </summary>
        /// <param name="operationName">The name of the operation where the errors occurred (e.g., "File Loading", "Extraction").</param>
        /// <param name="failedItems">A collection of tuples containing the context (e.g., file path) and the exception.</param>
        /// <returns>The full path to the generated log file.</returns>
        private async Task<string> LogOperationErrorAsync(string operationName, IEnumerable<(string Context, Exception ex)> failedItems)
        {
            string logFileName = $"ErrorLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.log";
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
            var sb = new StringBuilder();

            try
            {
                sb.AppendLine($"===== FSB/BANK Extractor - {operationName} Error Log =====");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Program Version: {AppVersion}");
                sb.AppendLine($"Operating System: {Environment.OSVersion}");
                sb.AppendLine($"FMOD Engine Version: {FmodFullVersion}");
                sb.AppendLine("----------------------------------------");
                sb.AppendLine();
                sb.AppendLine($"Total Errors: {failedItems.Count()}");
                sb.AppendLine();

                int errorCount = 1;
                foreach (var (context, ex) in failedItems)
                {
                    sb.AppendLine($"--- Error #{errorCount++} ---");
                    sb.AppendLine($"Context/Source: {context}");
                    sb.AppendLine($"Exception Type: {ex.GetType().Name}");
                    sb.AppendLine($"Message: {ex.Message}");
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine(ex.StackTrace ?? "No stack trace available.");
                    sb.AppendLine("--------------------");
                    sb.AppendLine();
                }

                await WriteAllTextAsync(logFilePath, sb.ToString());
            }
            catch (Exception writeEx)
            {
                // Provide a fallback if writing the log file fails.
                MessageBox.Show(
                    this,
                    $"Failed to create the log file.\n\nError: {writeEx.Message}\n\n" +
                    "Please copy the following details manually:\n\n" + sb.ToString(),
                    "Log Creation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            return logFilePath;
        }

        /// <summary>
        /// Reads the entire content of a text file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A string containing the file's content.</returns>
        private async Task<string> ReadAllTextAsync(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                return await sr.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Writes a string to a file asynchronously.
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        /// <param name="contents">The string content to write.</param>
        private Task WriteAllTextAsync(string path, string contents)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(contents);
            return WriteAllBytesAsync(path, encodedText);
        }

        /// <summary>
        /// Writes a byte array to a file asynchronously.
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        /// <param name="data">The byte array to write.</param>
        private async Task WriteAllBytesAsync(string path, byte[] data)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await fs.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Resets the UI to a safe, interactive state after a critical error.
        /// </summary>
        private void RestoreUiAfterError()
        {
            if (_isWorking)
            {
                _isWorking = false;
                _isScanning = false;
                _scanStopwatch.Stop();
            }
            if (!this.IsDisposed && this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    SetUiState(true);
                    lblStatus.Text = "Ready after encountering an error.";
                    progressBar.Value = 0;
                    progressBar.Style = ProgressBarStyle.Blocks;
                    Cursor = Cursors.Default;
                });
            }
        }

        /// <summary>
        /// Checks an FMOD result and throws an exception if it indicates an error.
        /// </summary>
        /// <param name="result">The FMOD.RESULT code to check.</param>
        private void CheckFmodResult(RESULT result)
        {
            if (result != RESULT.OK) throw new Exception($"FMOD Error [{result}]: {FMOD.Error.String(result)}");
        }

        /// <summary>
        /// Safely releases an FMOD Sound handle.
        /// </summary>
        /// <param name="sound">The FMOD.Sound object to release.</param>
        private void SafeRelease(ref Sound sound)
        {
            if (sound.hasHandle())
            {
                sound.release();
                sound.clearHandle();
            }
        }

        /// <summary>
        /// Converts an FMOD GUID to its string representation.
        /// </summary>
        /// <param name="g">The FMOD.GUID to convert.</param>
        /// <returns>A string representation of the GUID.</returns>
        private string GuidToString(GUID g) => $"{g.Data1:X8}-{g.Data2:X4}-{g.Data3:X4}-{BitConverter.ToString(BitConverter.GetBytes(g.Data4)).Replace("-", "")}";

        /// <summary>
        /// Counts the number of sub-sounds in an FSB container.
        /// </summary>
        /// <param name="path">The path to the file containing the FSB.</param>
        /// <param name="offset">The offset of the FSB data within the file.</param>
        /// <returns>The number of sub-sounds.</returns>
        private int CountSubSounds(string path, uint offset)
        {
            int numSub = 0;
            Sound sound = new Sound();
            try
            {
                lock (_coreSystemLock)
                {
                    CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };
                    if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM, ref exinfo, out sound) == RESULT.OK)
                    {
                        sound.getNumSubSounds(out numSub);
                    }
                }
            }
            finally { SafeRelease(ref sound); }
            return numSub;
        }

        /// <summary>
        /// Gets the internal name of an FSB container.
        /// </summary>
        /// <param name="path">The path to the file containing the FSB.</param>
        /// <param name="offset">The offset of the FSB data within the file.</param>
        /// <returns>The internal name of the FSB.</returns>
        private string GetFsbInternalName(string path, uint offset)
        {
            string name = "";
            Sound sound = new Sound();
            try
            {
                lock (_coreSystemLock)
                {
                    CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), fileoffset = offset };
                    if (_coreSystem.createSound(path, MODE.OPENONLY | MODE.CREATESTREAM, ref exinfo, out sound) == RESULT.OK)
                    {
                        sound.getName(out name, 256);
                    }
                }
            }
            finally { SafeRelease(ref sound); }
            return name;
        }

        /// <summary>
        /// Gathers detailed information about an audio sample from FMOD.
        /// </summary>
        /// <param name="sub">The FMOD.Sound object representing the audio sample.</param>
        /// <param name="index">The index of the sub-sound.</param>
        /// <param name="path">The path to the source file.</param>
        /// <param name="fsbChunkOffset">The offset of the parent FSB chunk.</param>
        /// <returns>An AudioInfo struct containing the sound's properties.</returns>
        private AudioInfo GetAudioInfo(Sound sub, int index, string path, long fsbChunkOffset)
        {
            AudioInfo info = new AudioInfo { Index = index, SourcePath = path, FileOffset = fsbChunkOffset };

            sub.getName(out info.Name, 256);
            sub.getLength(out info.LengthMs, TIMEUNIT.MS);
            sub.getLength(out info.LengthPcm, TIMEUNIT.PCM);
            sub.getFormat(out info.Type, out info.Format, out info.Channels, out info.Bits);
            sub.getLoopPoints(out info.LoopStart, TIMEUNIT.MS, out info.LoopEnd, TIMEUNIT.MS);
            sub.getMode(out info.Mode);

            // Get the compressed data length using the FMOD API as a fallback.
            sub.getLength(out info.DataLength, TIMEUNIT.RAWBYTES);

            // Parse the FSB header to get the most accurate data offset and length.
            var (dataOffset, dataLength) = ParseFsbHeaderAndGetSampleInfo(path, (uint)fsbChunkOffset, index);

            info.DataOffset = dataOffset;

            // Prefer the parsed length if it is valid, as it is more reliable.
            if (dataLength > 0)
            {
                info.DataLength = dataLength;
            }

            return info;
        }

        /// <summary>
        /// Parses the FSB5 header to find the exact offset and length of a specific sample's data.
        /// </summary>
        /// <param name="filePath">The path to the file containing the FSB data.</param>
        /// <param name="fsbChunkOffset">The starting offset of the FSB chunk within the file.</param>
        /// <param name="sampleIndex">The index of the sample to find.</param>
        /// <returns>A tuple containing the data offset (relative to FSB start) and data length.</returns>
        private (uint, uint) ParseFsbHeaderAndGetSampleInfo(string filePath, uint fsbChunkOffset, int sampleIndex)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek(fsbChunkOffset, SeekOrigin.Begin);

                    // Read the main FSB5 header.
                    if (new string(br.ReadChars(4)) != "FSB5") return (0, 0);

                    br.ReadInt32(); // Version
                    int numSamples = br.ReadInt32();
                    if (sampleIndex >= numSamples) return (0, 0);

                    uint sampleHeadersSize = br.ReadUInt32();
                    uint dataSize = br.ReadUInt32();
                    uint headerVersion = br.ReadUInt32();

                    // Determine the size of each sample header entry based on the header version.
                    uint sampleHeaderEntrySize;
                    uint sampleHeaderFieldsOffset;

                    if (headerVersion == 0)
                    {
                        sampleHeaderEntrySize = 64;
                        sampleHeaderFieldsOffset = 52;
                    }
                    else
                    {
                        sampleHeaderEntrySize = 80;
                        sampleHeaderFieldsOffset = 68;
                    }

                    long sampleHeaderTableStart = fsbChunkOffset + 0x40;

                    // Read the sample header table to find the specific sample's header.
                    long sampleHeaderOffset = sampleHeaderTableStart + (sampleHeaderEntrySize * sampleIndex);
                    if (sampleHeaderOffset >= fs.Length) return (0, 0);

                    fs.Seek(sampleHeaderOffset, SeekOrigin.Begin);

                    // Parse the individual sample header to get the data offset and length.
                    fs.Seek(sampleHeaderFieldsOffset, SeekOrigin.Current);

                    uint sampleDataOffset = br.ReadUInt32();
                    uint sampleDataLength = br.ReadUInt32();

                    // Calculate where the main data section begins.
                    uint dataSectionStart = (uint)(sampleHeaderTableStart + sampleHeadersSize);

                    // Calculate the final data offset relative to the start of the FSB chunk.
                    uint dataOffsetInFsb = (dataSectionStart - (uint)fsbChunkOffset) + sampleDataOffset;

                    // Perform a final sanity check on the calculated offsets and lengths.
                    if (fsbChunkOffset + dataOffsetInFsb + sampleDataLength > fs.Length) return (0, 0);

                    return (dataOffsetInFsb, sampleDataLength);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FSB Header Parse Error: {ex.Message}");
                return (0, 0);
            }
        }

        #region Nested Data Structures

        /// <summary>
        /// Enumerates the types of nodes represented in the tree view.
        /// </summary>
        public enum NodeType { Bank, Event, Bus, VCA, FsbFile, SubSound, AudioData }

        /// <summary>
        /// Represents the base class for data attached to a TreeView Node or ListView Item.
        /// This abstract class defines the contract for providing details for the UI.
        /// </summary>
        public abstract class NodeData
        {
            /// <summary>
            /// Gets the type of the node.
            /// </summary>
            public abstract NodeType Type { get; }

            /// <summary>
            /// Gets or sets the raw FMOD object (e.g., Bank, EventDescription).
            /// </summary>
            public object FmodObject { get; set; }

            /// <summary>
            /// Gets or sets extra string information, typically a file path.
            /// </summary>
            public string ExtraInfo { get; set; }

            /// <summary>
            /// Gets or sets the start offset of the FSB chunk within its container file.
            /// </summary>
            public long FsbChunkOffset { get; set; }

            /// <summary>
            /// When overridden in a derived class, returns a list of properties for display in the details view.
            /// </summary>
            /// <returns>A list of KeyValuePairs, where the Key is the group name and the Value is the property line.</returns>
            public abstract List<KeyValuePair<string, string>> GetDetails();

            // Helper for converting FMOD GUIDs, accessible by all derived classes.
            protected string GuidToString(GUID g) => $"{g.Data1:X8}-{g.Data2:X4}-{g.Data3:X4}-{BitConverter.ToString(BitConverter.GetBytes(g.Data4)).Replace("-", "")}";
        }

        /// <summary>
        /// Represents data for a node that contains audio information.
        /// </summary>
        public class AudioDataNode : NodeData
        {
            public override NodeType Type => NodeType.AudioData;
            public AudioInfo CachedAudio { get; }

            public AudioDataNode(AudioInfo audioInfo, long fsbOffset, string sourcePath)
            {
                CachedAudio = audioInfo;
                FsbChunkOffset = fsbOffset;
                ExtraInfo = sourcePath;
            }

            public override List<KeyValuePair<string, string>> GetDetails()
            {
                var details = new List<KeyValuePair<string, string>>();
                var info = CachedAudio;

                details.Add(new KeyValuePair<string, string>("Audio Info", $"Name: {info.Name}"));
                details.Add(new KeyValuePair<string, string>("Audio Info", $"Source File: {Path.GetFileName(info.SourcePath)}"));
                details.Add(new KeyValuePair<string, string>("Audio Info", $"Sub-Sound Index: {info.Index}"));
                details.Add(new KeyValuePair<string, string>("Format", $"Encoding: {info.Type}"));
                details.Add(new KeyValuePair<string, string>("Format", $"Container: {info.Format}"));
                details.Add(new KeyValuePair<string, string>("Format", $"Channels: {info.Channels}"));
                details.Add(new KeyValuePair<string, string>("Format", $"Bits: {info.Bits}"));
                details.Add(new KeyValuePair<string, string>("Time", $"Duration (ms): {info.LengthMs}"));
                details.Add(new KeyValuePair<string, string>("Data", $"Data Size (Bytes): {info.DataLength}"));

                bool hasLoop = (info.Mode & MODE.LOOP_NORMAL) != 0 || (info.LoopStart != 0 || info.LoopEnd != 0);
                details.Add(new KeyValuePair<string, string>("Looping", $"Has Loop Points: {hasLoop}"));
                details.Add(new KeyValuePair<string, string>("Looping", $"Loop Range (ms): {info.LoopStart} - {info.LoopEnd}"));

                return details;
            }
        }

        /// <summary>
        /// Represents data for an FMOD Bank node.
        /// </summary>
        public class BankNode : NodeData
        {
            public override NodeType Type => NodeType.Bank;
            public Bank BankObject => (Bank)FmodObject;

            public BankNode(string path)
            {
                ExtraInfo = path;
            }

            public override List<KeyValuePair<string, string>> GetDetails()
            {
                var details = new List<KeyValuePair<string, string>>();
                if (FmodObject != null && ((Bank)FmodObject).isValid())
                {
                    var bank = (Bank)FmodObject;
                    bank.getID(out GUID id);
                    details.Add(new KeyValuePair<string, string>("Bank", $"Path: {ExtraInfo}"));
                    details.Add(new KeyValuePair<string, string>("Bank", $"GUID: {GuidToString(id)}"));
                }
                else
                {
                    details.Add(new KeyValuePair<string, string>("Bank", $"Path: {ExtraInfo}"));
                    details.Add(new KeyValuePair<string, string>("Bank", $"Status: Not loaded or invalid"));
                }
                return details;
            }
        }

        /// <summary>
        /// Represents data for an FMOD Event node.
        /// </summary>
        public class EventNode : NodeData
        {
            public override NodeType Type => NodeType.Event;
            public EventDescription EventObject => (EventDescription)FmodObject;

            public EventNode(EventDescription evt)
            {
                FmodObject = evt;
            }

            public override List<KeyValuePair<string, string>> GetDetails()
            {
                var details = new List<KeyValuePair<string, string>>();
                if (FmodObject != null && ((EventDescription)FmodObject).isValid())
                {
                    var evt = (EventDescription)FmodObject;
                    evt.getID(out GUID id);
                    evt.getPath(out string path);
                    details.Add(new KeyValuePair<string, string>("Event", $"Path: {path}"));
                    details.Add(new KeyValuePair<string, string>("Event", $"GUID: {GuidToString(id)}"));
                }
                return details;
            }
        }

        /// <summary>
        /// Represents data for an FMOD Bus node.
        /// </summary>
        public class BusNode : NodeData
        {
            public override NodeType Type => NodeType.Bus;
            public Bus BusObject => (Bus)FmodObject;

            public BusNode(Bus bus)
            {
                FmodObject = bus;
            }

            public override List<KeyValuePair<string, string>> GetDetails()
            {
                var details = new List<KeyValuePair<string, string>>();
                if (FmodObject != null && ((Bus)FmodObject).isValid())
                {
                    var bus = (Bus)FmodObject;
                    bus.getID(out GUID id);
                    bus.getPath(out string path);
                    details.Add(new KeyValuePair<string, string>("Bus", $"Path: {path}"));
                    details.Add(new KeyValuePair<string, string>("Bus", $"GUID: {GuidToString(id)}"));
                }
                return details;
            }
        }

        /// <summary>
        /// Represents data for a node that is a container for an FSB file.
        /// </summary>
        public class FsbFileNode : NodeData
        {
            public override NodeType Type => NodeType.FsbFile;

            public FsbFileNode(string sourcePath, long fsbOffset)
            {
                ExtraInfo = sourcePath;
                FsbChunkOffset = fsbOffset;
            }

            public override List<KeyValuePair<string, string>> GetDetails()
            {
                var details = new List<KeyValuePair<string, string>>();
                details.Add(new KeyValuePair<string, string>("FSB Container", $"Source File: {Path.GetFileName(ExtraInfo)}"));
                details.Add(new KeyValuePair<string, string>("FSB Container", $"File Offset: 0x{FsbChunkOffset:X}"));
                return details;
            }
        }

        /// <summary>
        /// Represents a snapshot of audio properties to avoid repeated FMOD API calls.
        /// </summary>
        public struct AudioInfo
        {
            public string Name;
            public uint LengthMs;
            public uint LengthPcm;
            public SOUND_TYPE Type;
            public SOUND_FORMAT Format;
            public int Channels;
            public int Bits;
            public uint LoopStart;
            public uint LoopEnd;
            public MODE Mode;
            public int Index;
            public string SourcePath;
            public long FileOffset; // Offset of the containing FSB chunk within the source file.
            public uint DataOffset; // Offset of the raw audio data within the FSB chunk.
            public uint DataLength; // Length in bytes of the raw audio data stream.
        }

        /// <summary>
        /// Represents the manifest file structure for FSB rebuilding.
        /// </summary>
        public class FsbManifest
        {
            [JsonProperty("build_format")]
            public SOUND_TYPE BuildFormat { get; set; } = SOUND_TYPE.VORBIS;

            [JsonProperty("sub_sounds")]
            public List<SubSoundManifestInfo> SubSounds { get; set; }
        }

        /// <summary>
        /// Represents the metadata for a single sub-sound in the rebuilding manifest.
        /// </summary>
        public class SubSoundManifestInfo
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("original_file_name")]
            public string OriginalFileName { get; set; }

            [JsonProperty("looping")]
            public bool Looping { get; set; }

            [JsonProperty("loop_start_ms")]
            public uint LoopStart { get; set; }

            [JsonProperty("loop_end_ms")]
            public uint LoopEnd { get; set; }
        }

        /// <summary>
        /// Provides functionality for logging messages to a file.
        /// </summary>
        public class LogWriter : IDisposable
        {
            public enum LogLevel { INFO, WARNING, ERROR }

            private StreamWriter _writer;
            private readonly object _lock = new object();

            /// <summary>
            /// Initializes a new instance of the <see cref="LogWriter"/> class.
            /// </summary>
            /// <param name="path">The file path where the log will be written.</param>
            public LogWriter(string path)
            {
                try
                {
                    _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
                }
                catch { } // Logging initialization failed.
            }

            /// <summary>
            /// Writes a raw string message to the log.
            /// </summary>
            /// <param name="message">The message to write.</param>
            public void WriteRaw(string message)
            {
                if (_writer == null) return;
                try
                {
                    lock (_lock) { _writer.WriteLine(message); }
                }
                catch { }
            }

            /// <summary>
            /// Writes a structured, tab-separated log entry.
            /// </summary>
            /// <param name="level">The severity level of the log.</param>
            /// <param name="sourceFile">The source file name.</param>
            /// <param name="eventName">The event or operation name.</param>
            /// <param name="result">The result of the operation.</param>
            /// <param name="format">The format information.</param>
            /// <param name="loopRange">The loop start and end points in milliseconds.</param>
            /// <param name="dataOffset">The hexadecimal offset of the audio data in the source file.</param>
            /// <param name="duration">The duration of the audio.</param>
            /// <param name="output">The output path.</param>
            /// <param name="timeTaken">The time taken for the operation in milliseconds.</param>
            public void LogTSV(LogLevel level, string sourceFile, string eventName, string result, string format, string loopRange, string dataOffset, string duration, string output, string timeTaken)
            {
                if (_writer == null) return;
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"{timestamp}\t{level}\t{sourceFile}\t{eventName}\t{result}\t{format}\t{loopRange}\t{dataOffset}\t{duration}\t{output}\t{timeTaken}";
                    lock (_lock) { _writer.WriteLine(line); }
                }
                catch { }
            }

            /// <summary>
            /// Releases all resources used by the <see cref="LogWriter"/>.
            /// </summary>
            public void Dispose()
            {
                if (_writer != null)
                {
                    try { _writer.Close(); _writer.Dispose(); } catch { }
                    _writer = null;
                }
            }
        }

        /// <summary>
        /// Defines sets of characters and names that are invalid for file systems.
        /// </summary>
        public static class FileSystemDefs
        {
            /// <summary>
            /// Contains device names reserved by Windows that cannot be used as file names.
            /// </summary>
            public static readonly HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };
        }

        #endregion
        #endregion
    }
}