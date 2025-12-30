/**
 * @file FSB_BANK_Extractor_Rebuilder_CS_GUI.cs
 * @brief Main graphical user interface for the FMOD FSB/BANK Extractor and Rebuilder application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file defines the primary UI form and its logic. It serves as the central hub, orchestrating user interactions,
 * managing the application's state, and delegating complex tasks like file parsing, audio playback,
 * extraction, and rebuilding to dedicated service classes.
 *
 * Key Features:
 *  - Main Application Window: Implements the primary user interface including menus, playback controls, and data views.
 *  - File and Folder Loading: Handles user input for loading .bank and .fsb files via menus or Drag & Drop.
 *  - UI State Management: Controls the enabled/disabled state of UI elements based on the application's operational state.
 *  - Event Orchestration: Connects UI events to backend services (FmodManager, ExtractionService, RebuildService).
 *  - Data Display: Manages the TreeView for hierarchy, ListView for search results, and details panel.
 *  - User Convenience Tools: Implements keyword search, index-based selection, and CSV export.
 *
 * @acknowledgements
 * This project was originally inspired by 'fsb_aud_extr.exe' from id-daemon (zenhax.com) and was re-implemented in C#.
 * The implementation of the legacy FSB parser was also heavily guided by the logic of 'fsbext' v0.3.5 by Luigi Auriemma.
 *
 *  - Initial Inspiration: id-daemon (https://zenhax.com/viewtopic.php?t=1901)
 *  - Legacy Parser Reference: Luigi Auriemma (http://aluigi.altervista.org/)
 *    - GitHub Mirror: https://github.com/gdawg/fsbext
 *    
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
 *  - Target Framework: .NET Framework 4.8
 *  - Architecture: Any CPU (Tested primarily on x64)
 *  - Key Dependencies: Newtonsoft.Json
 *  - Primary Test Platform: Windows 10 64-bit
 *  - Last Update: 2025-12-30
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FMOD; // Core API
using FMOD.Studio; // Studio API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Represents the main window and orchestrates the primary logic for the FSB/BANK Extractor and Rebuilder application.
    /// </summary>
    public partial class FSB_BANK_Extractor_Rebuilder_CS_GUI : Form
    {
        #region 1. Constants & Configuration

        /// <summary>
        /// Defines image list indices for TreeView and ListView icons.
        /// </summary>
        public static class ImageIndex
        {
            /// <summary>
            /// Icon index for a generic file.
            /// </summary>
            public const int File = 0;
            /// <summary>
            /// Icon index for a folder or container.
            /// </summary>
            public const int Folder = 1;
            /// <summary>
            /// Icon index for an FMOD Event.
            /// </summary>
            public const int Event = 2;
            /// <summary>
            /// Icon index for an FMOD Parameter.
            /// </summary>
            public const int Param = 3;
            /// <summary>
            /// Icon index for an FMOD Bus.
            /// </summary>
            public const int Bus = 4;
            /// <summary>
            /// Icon index for an FMOD VCA.
            /// </summary>
            public const int Vca = 5;
            /// <summary>
            /// Icon index for a playable audio asset.
            /// </summary>
            public const int Audio = 6;
        }

        /// <summary>
        /// Defines UI string constants to prevent magic strings in code.
        /// </summary>
        private static class UiConstants
        {
            // Dialog Titles and Messages
            public const string MsgCriticalError = "Critical Error";
            public const string MsgError = "Error";
            public const string MsgSuccess = "Success";
            public const string MsgInfo = "Info";
            public const string MsgWarning = "Warning";
            public const string MsgExtractionError = "Extraction Error";
            public const string MsgExportError = "Export Error";
            public const string MsgRebuildError = "Rebuild Error";
            public const string MsgToolNotFound = "Tool Not Found";
            public const string MsgInvalidSelection = "Invalid Selection";
            public const string MsgUnsupportedFormat = "Unsupported Format";
            public const string TitleProgramInfo = "Program Information";
            public const string TitleExtractionReport = "Extraction Report";

            // Status Bar Messages
            public const string StatusReady = "[READY] Waiting for next operation.";
            public const string StatusInitializing = "[INITIALIZING] Preparing to load files...";
            public const string StatusPlaybackLoading = "[PLAYBACK] Loading audio stream...";
            public const string StatusExtractionCancelled = "[CANCELLED] Extraction cancelled by user.";
            public const string StatusRebuildCancelled = "[CANCELLED] Rebuild cancelled by user.";

            // Menu Item Texts
            public const string MenuSelectAll = "Select All";
            public const string MenuIndexTools = "Index Tools (Select/Jump)...";
            public const string MenuLoadStrings = "Load Strings Bank (Manual)...";
            public const string MenuRebuildManager = "Rebuild Manager...";
            public const string MenuRebuildLegacy = "Rebuild Sound with fsbankcl...";
            public const string MenuTools = "Tools";
            public const string MenuAudioAnalyzer = "Audio Analyzer...";
            public const string MenuHelp = "Help";
            public const string MenuAbout = "About";

            // Context Menu (Search Results) Texts
            public const string CtxMenuOpenFileLocation = "Open File Location";
            public const string CtxMenuExtractItem = "Extract This Item...";
            public const string CtxMenuRebuildItem = "Rebuild This Item...";
            public const string CtxMenuCopyName = "Copy Name";
            public const string CtxMenuCopyPath = "Copy Path";
            public const string CtxMenuCopyGuid = "Copy GUID";

            // Extraction Location ComboBox Items
            public const string ExtractModeSame = "Same as source file";
            public const string ExtractModeCustom = "Custom path";
            public const string ExtractModeAsk = "Ask every time";

            // Playback Button Texts
            public const string BtnTextPause = "Pause (||)";
            public const string BtnTextPlay = "Play (▶)";

            // Label Text Formats
            public const string LblFormatVolume = "Volume: {0}%";
            public const string LblFormatElapsedShort = "Elapsed: {0:hh\\:mm\\:ss\\.ff}";
            public const string LblFormatElapsedLong = "Elapsed: {0:D2}:{1:D2}:{2:D2}.{3:D2}";
            public const string LblFormatTime = "{0:mm\\:ss\\.fff} / {1:mm\\:ss\\.fff}";
            public const string LblFormatTimeZero = "00:00.000 / 00:00.000";
        }

        /// <summary>
        /// Defines file and path related constants.
        /// </summary>
        private static class FileConstants
        {
            // File Dialog Filters
            public const string FilterFmodFiles = "FMOD Files|*.bank;*.fsb";
            public const string FilterStringsBank = "FMOD Strings Bank|*.strings.bank";
            public const string FilterCsv = "CSV|*.csv";
            public const string FilterWav = "WAV File|*.wav";

            // File Extensions
            public const string ExtWav = ".wav";

            // Default File Name Formats
            public const string NameFormatCsvExport = "FmodExport_{0:yyyy-MM-dd_HH-mm-ss-fff}.csv";
            public const string NameFormatExtractionLog = "ExtractionLog_Single_{0:yyyy-MM-dd_HH-mm-ss}.log";
            public const string NameFormatErrorLog = "ErrorLog_{0:yyyy-MM-dd_HH-mm-ss-fff}.log";
            public const string NameFormatRebuildLog = "RebuildLog_{0:yyyy-MM-dd_HH-mm-ss}.log";

            // Special Path Markers
            public const string PathMarkerSameAsSource = "##SAME_AS_SOURCE##";

            // Temporary Directory Names
            public const string TempCleanupDirPrefix = "FsbRebuildTool_Trash_";
            public const string TempDirName = "FsbRebuildTool";
        }

        /// <summary>
        /// Defines constants for timers and UI controls.
        /// </summary>
        private static class ControlConstants
        {
            /// <summary>
            /// The interval in milliseconds for the main UI update timer (approx. 30 FPS).
            /// </summary>
            public const int MainUpdateTimerInterval = 33;
            /// <summary>
            /// The delay in milliseconds for the auto-play debounce timer.
            /// </summary>
            public const int AutoPlayDebounceTimerInterval = 250;
            /// <summary>
            /// The maximum value for the playback seek bar, providing a consistent range for position calculation.
            /// </summary>
            public const int SeekBarMax = 1000;
        }

        /// <summary>
        /// Defines formatting constants for log files.
        /// </summary>
        private static class LogConstants
        {
            /// <summary>
            /// A standard separator line used to structure log file headers and sections.
            /// </summary>
            public const string SeparatorLine = "================================================================";
        }

        /// <summary>
        /// Defines the available modes for specifying the extraction location.
        /// </summary>
        private enum ExtractLocationMode
        {
            /// <summary>
            /// Extracts files to the same directory as the source .bank/.fsb file.
            /// </summary>
            SameAsSource,
            /// <summary>
            /// Extracts files to a user-specified custom directory.
            /// </summary>
            CustomPath,
            /// <summary>
            /// Prompts the user to select a directory for each extraction operation.
            /// </summary>
            AskEveryTime
        }

        /// <summary>
        /// Defines the possible operational states of the application.
        /// </summary>
        private enum ApplicationState
        {
            /// <summary>
            /// The application is idle and ready for user input.
            /// </summary>
            Idle,
            /// <summary>
            /// The application is busy loading and analyzing asset files.
            /// </summary>
            Loading,
            /// <summary>
            /// The application is actively extracting audio data to files.
            /// </summary>
            Extracting,
            /// <summary>
            /// The application is actively rebuilding an FSB container.
            /// </summary>
            Rebuilding
        }

        /// <summary>
        /// The public version number of the application.
        /// </summary>
        public const string AppVersion = "3.3.1";
        /// <summary>
        /// The date of the last significant update to the application.
        /// </summary>
        public const string AppLastUpdate = "2025-12-30";
        /// <summary>
        /// The name or alias of the application's developer.
        /// </summary>
        public const string AppDeveloper = "(GitHub) IZH318";
        /// <summary>
        /// The primary website or repository for the application.
        /// </summary>
        public const string AppWebsite = "https://github.com/IZH318";

        /// <summary>
        /// The version of the FMOD Core and Studio API used by the application.
        /// </summary>
        public const string FmodApiVersion = "v2.03.11";
        /// <summary>
        /// The specific build number of the FMOD API library.
        /// </summary>
        public const string FmodBuildNumber = "158528";

        /// <summary>
        /// Gets the full formatted FMOD version string, including the build number.
        /// </summary>
        public static string FmodFullVersion => $"{FmodApiVersion} (Build {FmodBuildNumber})";

        #endregion

        #region 2. Fields & State

        // Service and Controller instances for managing application logic.
        /// <summary>
        /// Manages all FMOD audio engine interactions, including initialization, playback, and resource management.
        /// </summary>
        private readonly FmodManager _fmodManager;
        /// <summary>
        /// Handles the logic for extracting audio data to WAV files and exporting metadata to CSV.
        /// </summary>
        private readonly ExtractionService _extractionService;
        /// <summary>
        /// Manages the complex process of rebuilding FSB containers, including workspace setup and external tool invocation.
        /// </summary>
        private readonly RebuildService _rebuildService;
        /// <summary>
        /// Controls the display of detailed properties for the selected node in the details ListView.
        /// </summary>
        private readonly DetailsViewController _detailsViewController;
        /// <summary>
        /// Manages search functionality, including filtering nodes and updating the search results view.
        /// </summary>
        private readonly SearchController _searchController;

        // UI and data state variables.
        /// <summary>
        /// Stores the data object for the currently selected node in either the TreeView or search results.
        /// </summary>
        private NodeData _currentSelection = null;
        /// <summary>
        /// A complete, unfiltered list of the root-level TreeNodes, serving as the master data source for searching.
        /// </summary>
        private readonly List<TreeNode> _originalNodes = new List<TreeNode>();

        // Child form instances.
        /// <summary>
        /// Holds the instance of the Audio Analyzer form, allowing it to be reused and managed.
        /// </summary>
        private AudioAnalyzerForm _audioAnalyzer;

        // Timers and diagnostics.
        /// <summary>
        /// A UI timer responsible for periodic updates, such as playback status.
        /// </summary>
        private readonly System.Windows.Forms.Timer _mainUpdateTimer;
        /// <summary>
        /// A timer used to delay auto-play, preventing rapid playback triggers during quick selections.
        /// </summary>
        private readonly System.Windows.Forms.Timer _autoPlayDebounceTimer;
        /// <summary>
        /// A stopwatch to measure the duration of long-running operations like loading, extraction, and rebuilding.
        /// </summary>
        private readonly Stopwatch _scanStopwatch = new Stopwatch();

        // Application state management.
        /// <summary>
        /// Tracks the current operational state of the application (e.g., Idle, Loading).
        /// </summary>
        private ApplicationState _currentState = ApplicationState.Idle;
        /// <summary>
        /// A flag that indicates the application is in the process of closing, used to gracefully terminate background tasks.
        /// </summary>
        private volatile bool _isClosing = false;
        /// <summary>
        /// A flag to prevent recursive firing of the TreeView's AfterCheck event during programmatic check state changes.
        /// </summary>
        private bool _isUpdatingChecks = false;
        /// <summary>
        /// A flag indicating that the user is currently dragging the playback seek bar, used to prevent automatic updates.
        /// </summary>
        private bool _isDraggingSeek = false;

        // Logging and path management.
        /// <summary>
        /// The main log writer instance used for detailed session logging during extraction or rebuilding.
        /// </summary>
        private LogWriter _logger;
        /// <summary>
        /// Stores the user-defined custom path for file extractions.
        /// </summary>
        private string _customExtractPath = string.Empty;

        #endregion

        #region 3. Initialization & Cleanup

        /// <summary>
        /// Initializes a new instance of the <see cref="FSB_BANK_Extractor_Rebuilder_CS_GUI"/> class.
        /// </summary>
        public FSB_BANK_Extractor_Rebuilder_CS_GUI()
        {
            InitializeComponent();

            // Perform initial setup on launch.
            CleanupOrphanedTempFiles();
            SetupIcons();
            InitializeUiLogic();

            // Disable the default ContextMenuStrip assigned in the designer to allow
            // the dynamic search results menu (handled in MouseClick) to function correctly.
            lvSearchResults.ContextMenuStrip = null;

            // Initialize core services and controllers to delegate application logic.
            _detailsViewController = new DetailsViewController(listViewDetails);
            _searchController = new SearchController();
            _fmodManager = new FmodManager();
            _fmodManager.Initialize();
            _extractionService = new ExtractionService(_fmodManager.CoreSystem, _fmodManager.SyncLock);
            _rebuildService = new RebuildService(_fmodManager.CoreSystem, _fmodManager.SyncLock, _extractionService);

            // Configure search controller events for MVVM-style interaction.
            _searchController.OnResultsUpdated += UpdateSearchResultsUI;
            _searchController.OnStatusChanged += (msg) => lblStatus.Text = msg;
            _searchController.OnProgressChanged += (isMarquee, max) =>
            {
                progressBar.Style = isMarquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
                progressBar.Value = 0;
                if (max > 0)
                {
                    progressBar.Maximum = max;
                }
            };

            // Bind UI events to the search controller.
            txtSearch.TextChanged += (s, e) => _searchController.UpdateSearchText(txtSearch.Text);

            // Set up timers and event handlers for UI updates and interactions.
            treeViewInfo.AfterCheck += TreeViewInfo_AfterCheck;

            // Main UI update timer (approx. 30fps).
            _mainUpdateTimer = new System.Windows.Forms.Timer { Interval = ControlConstants.MainUpdateTimerInterval };
            _mainUpdateTimer.Tick += MainUpdateTimer_Tick;
            _mainUpdateTimer.Start();

            // Auto-play debounce timer (approx. 250ms delay).
            // This prevents rapid playback triggering when scrolling or clicking quickly through the list.
            _autoPlayDebounceTimer = new System.Windows.Forms.Timer { Interval = ControlConstants.AutoPlayDebounceTimerInterval };
            _autoPlayDebounceTimer.Tick += AutoPlayDebounceTimer_Tick;

            // Configure Drag & Drop functionality for the main form.
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDropAsync;

            // Configure playback controls.
            trackSeek.Minimum = 0;
            trackSeek.Maximum = ControlConstants.SeekBarMax;
            chkLoop.CheckedChanged += chkLoop_CheckedChanged;

            // Enable global key previews for shortcuts like Ctrl+F.
            this.KeyPreview = true;

            // Configure search results list view events.
            lvSearchResults.SelectedIndexChanged += LvSearchResults_SelectedIndexChanged;
            lvSearchResults.DoubleClick += LvSearchResults_DoubleClick;
            lvSearchResults.MouseClick += LvSearchResults_MouseClick;
            lvSearchResults.ColumnClick += LvSearchResults_ColumnClick;

            // Configure the extraction location ComboBox using constants.
            cmbExtractLocation.Items.AddRange(new object[] {
                UiConstants.ExtractModeSame,
                UiConstants.ExtractModeCustom,
                UiConstants.ExtractModeAsk
            });
            cmbExtractLocation.SelectedIndex = 0;
            cmbExtractLocation.SelectedIndexChanged += CmbExtractLocation_SelectedIndexChanged;

            // Register the form closing event for resource cleanup.
            this.FormClosing += OnFormClosing;

            // Set the initial application state to Idle.
            SetApplicationState(ApplicationState.Idle);
        }

        /// <summary>
        /// Handles the FormClosing event of the Form control.
        /// </summary>
        /// <remarks>
        /// This method ensures a clean shutdown by performing the following steps in order:
        ///  1) Kill any running external processes to prevent orphans.
        ///  2) Dispose of FMOD resources to release audio driver locks.
        ///  3) Forcefully terminate the application process to ensure all threads are stopped.
        /// </remarks>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            // Set a flag to signal background tasks to terminate gracefully.
            _isClosing = true;

            try
            {
                // Step 1: Ensure any running external build tools (fsbankcl.exe) are killed immediately.
                // This prevents orphaned processes if the user force-closes during a rebuild.
                if (_rebuildService != null)
                {
                    _rebuildService.ForceKillChildProcess();
                }

                // Step 2: Dispose of FMOD resources to prevent audio driver locks.
                if (_fmodManager != null)
                {
                    _fmodManager.Dispose();
                }

                // Dispose of other managed resources.
                _searchController?.Dispose();

                // Stop and dispose timers.
                _mainUpdateTimer?.Stop();
                _mainUpdateTimer?.Dispose();

                _autoPlayDebounceTimer?.Stop();
                _autoPlayDebounceTimer?.Dispose();
            }
            catch
            {
                // Ignore errors during final cleanup as the process is about to be killed.
            }
            finally
            {
                // Step 3: Forcefully kill the current process to ensure immediate termination of all threads.
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Initializes custom UI elements like context menus and menu strip items.
        /// </summary>
        private void InitializeUiLogic()
        {
            SetupAdditionalContextMenu();
            SetupManualBankLoader();
            SetupHelpMenu();
            SetupAnalyzerMenu();
        }

        /// <summary>
        /// Configures additional items for the TreeView's context menu.
        /// </summary>
        private void SetupAdditionalContextMenu()
        {
            if (treeViewContextMenu != null)
            {
                // Hide designer-placed items that are managed dynamically.
                if (playContextMenuItem != null)
                {
                    playContextMenuItem.Visible = false;
                }
                if (stopContextMenuItem != null)
                {
                    stopContextMenuItem.Visible = false;
                }

                // Add "Select All" functionality.
                ToolStripMenuItem selectAllItem = new ToolStripMenuItem(UiConstants.MenuSelectAll);
                selectAllItem.Click += (s, e) => CheckAllInCurrentView();
                treeViewContextMenu.Items.Insert(0, selectAllItem);
                treeViewContextMenu.Items.Insert(1, new ToolStripSeparator());

                // Add "Index Tools" for advanced selection.
                ToolStripMenuItem indexToolItem = new ToolStripMenuItem(UiConstants.MenuIndexTools);
                indexToolItem.Click += IndexToolItem_Click;
                treeViewContextMenu.Items.Insert(2, indexToolItem);
                treeViewContextMenu.Items.Insert(3, new ToolStripSeparator());
            }
        }

        /// <summary>
        /// Adds a menu item for manually loading a strings.bank file.
        /// </summary>
        private void SetupManualBankLoader()
        {
            ToolStripMenuItem manualLoadItem = new ToolStripMenuItem(UiConstants.MenuLoadStrings);
            manualLoadItem.Click += (s, e) => LoadStringsBankManually();

            // Insert the item into the "File" menu.
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
            if (menuStrip1 == null || menuStrip1.Items.Count == 0)
            {
                return;
            }

            if (menuStrip1.Items[0] is ToolStripMenuItem fileMenu)
            {
                // Create Help menu item with F1 shortcut.
                ToolStripMenuItem helpItem = new ToolStripMenuItem(UiConstants.MenuHelp);
                helpItem.ShortcutKeys = Keys.F1;
                helpItem.Click += (s, e) => ShowHelpForm();

                // Create About menu item.
                ToolStripMenuItem aboutItem = new ToolStripMenuItem(UiConstants.MenuAbout);
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
                    // Fallback if "Exit" is not found.
                    fileMenu.DropDownItems.Add(new ToolStripSeparator());
                    fileMenu.DropDownItems.Add(helpItem);
                    fileMenu.DropDownItems.Add(aboutItem);
                }
            }
        }

        /// <summary>
        /// Adds the "Audio Analyzer" tool to the "Tools" menu.
        /// </summary>
        private void SetupAnalyzerMenu()
        {
            ToolStripMenuItem toolsMenu = null;

            // Find an existing "Tools" menu.
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Text == UiConstants.MenuTools)
                {
                    toolsMenu = (ToolStripMenuItem)item;
                    break;
                }
            }

            // If it doesn't exist, create it.
            if (toolsMenu == null)
            {
                toolsMenu = new ToolStripMenuItem(UiConstants.MenuTools);
                menuStrip1.Items.Insert(1, toolsMenu);
            }

            // Add the analyzer menu item.
            ToolStripMenuItem analyzerItem = new ToolStripMenuItem(UiConstants.MenuAudioAnalyzer);
            analyzerItem.Click += (s, e) => ShowAudioAnalyzer();
            toolsMenu.DropDownItems.Add(analyzerItem);
        }

        /// <summary>
        /// Shows the Audio Analyzer window, creating a new instance if necessary, or brings it to the front.
        /// </summary>
        private void ShowAudioAnalyzer()
        {
            // If the form is null or disposed, create a new instance.
            if (_audioAnalyzer == null || _audioAnalyzer.IsDisposed)
            {
                _audioAnalyzer = new AudioAnalyzerForm();
                _audioAnalyzer.Show();

                // If audio is currently playing, attach the analyzer to the stream immediately.
                if (_fmodManager.IsPlaying)
                {
                    _audioAnalyzer.AttachToAudio(
                        _fmodManager.CoreSystem,
                        _fmodManager.CurrentChannel,
                        _fmodManager.CurrentSound
                    );
                }
            }
            else
            {
                // If the form already exists, bring it to the front.
                _audioAnalyzer.BringToFront();
            }
        }

        #endregion

        #region 4. UI Interaction & Input

        /// <summary>
        /// Processes command keys for application-wide shortcuts.
        /// </summary>
        /// <param name="msg">A <see cref="Message"/>, passed by reference, that represents the window message to process.</param>
        /// <param name="keyData">One of the <see cref="Keys"/> values that represents the key to process.</param>
        /// <returns><c>true</c> if the keystroke was processed and consumed by the control; otherwise, <c>false</c> to allow further processing.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle Ctrl+F shortcut to focus the search box.
            if (keyData == (Keys.Control | Keys.F))
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Sets the application's global state and updates the UI accordingly.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        private void SetApplicationState(ApplicationState newState)
        {
            // Ensure this method is executed on the UI thread.
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetApplicationState(newState)));
                return;
            }

            _currentState = newState;

            switch (_currentState)
            {
                case ApplicationState.Idle:
                    // Enable all UI controls and reset progress indicators.
                    SetUiState(true);
                    progressBar.Value = 0;
                    if (_scanStopwatch.IsRunning)
                    {
                        _scanStopwatch.Stop();
                        lblElapsedTime.Text = string.Format(UiConstants.LblFormatElapsedShort, _scanStopwatch.Elapsed);
                    }
                    lblStatus.Text = UiConstants.StatusReady;
                    break;

                case ApplicationState.Loading:
                case ApplicationState.Extracting:
                case ApplicationState.Rebuilding:
                    // Disable UI controls, show a wait cursor, and start the timer.
                    SetUiState(false);
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = 0;

                    // Reset the progress bar scale to ensure standard percentage updates (0-100).
                    // This prevents range exceptions if a previous operation (e.g., Search) altered the maximum value.
                    progressBar.Maximum = 100;

                    _scanStopwatch.Restart();
                    break;
            }
        }

        /// <summary>
        /// Enables or disables UI controls based on the application's busy state.
        /// </summary>
        /// <param name="enabled"><c>true</c> to enable controls; <c>false</c> to disable them.</param>
        private void SetUiState(bool enabled)
        {
            // Prevent updates if the form is closing.
            if (_isClosing || this.IsDisposed)
            {
                return;
            }

            // Toggle the enabled state of all major interactive UI elements.
            menuStrip1.Enabled = enabled;
            panelPlayback.Enabled = enabled;
            treeViewInfo.Enabled = enabled;
            lvSearchResults.Enabled = enabled;
            listViewDetails.Enabled = enabled;
            panelSearch.Enabled = enabled;

            // Change the cursor to indicate the application's state.
            Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
        }

        /// <summary>
        /// Handles the Tick event of the mainUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void MainUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isClosing || this.IsDisposed)
            {
                return;
            }

            // Update FMOD and playback UI only when idle to prevent conflicts.
            if (_currentState == ApplicationState.Idle)
            {
                _fmodManager.Update();
                UpdatePlaybackStatus();
            }

            // Update the elapsed time label continuously during long operations.
            if (_currentState != ApplicationState.Idle)
            {
                TimeSpan ts = _scanStopwatch.Elapsed;
                lblElapsedTime.Text = string.Format(UiConstants.LblFormatElapsedLong, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            }
        }

        /// <summary>
        /// Handles the Tick event of the _autoPlayDebounceTimer control. Executes playback after the user has stopped changing selection for a short period.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void AutoPlayDebounceTimer_Tick(object sender, EventArgs e)
        {
            _autoPlayDebounceTimer.Stop();

            if (_isClosing || this.IsDisposed)
            {
                return;
            }

            // Force stop any existing playback before starting the new one.
            // This ensures FMOD resources are cleared and prevents "ERR_NOTREADY" during rapid switching.
            _fmodManager.Stop();

            if (_currentState == ApplicationState.Idle && chkAutoPlay.Checked && _currentSelection != null)
            {
                PlaySelection();
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
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = FileConstants.FilterFmodFiles, Multiselect = true })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        await LoadContextAsync(ofd.FileNames);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", UiConstants.MsgCriticalError, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        await LoadContextAsync(new string[] { fbd.SelectedPath });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", UiConstants.MsgCriticalError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the DragEnter event of the main Form control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // Show a "copy" cursor effect if the dragged data is a file drop.
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        /// Handles the DragDrop event of the main Form control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private async void MainForm_DragDropAsync(object sender, DragEventArgs e)
        {
            try
            {
                // Retrieve the list of dropped files and start the loading process.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    await LoadContextAsync(files);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", UiConstants.MsgCriticalError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Asynchronously loads and analyzes a list of input files or directories.
        /// </summary>
        /// <remarks>
        /// Processing steps:
        ///  1) Reset playback and clear UI data.
        ///  2) Unload previous FMOD banks.
        ///  3) Perform asynchronous asset loading via AssetLoader.
        ///  4) Populate the TreeView with results.
        ///  5) Handle any errors that occurred during the process.
        /// </remarks>
        /// <param name="inputPaths">An enumeration of file or directory paths to load.</param>
        private async Task LoadContextAsync(IEnumerable<string> inputPaths)
        {
            // Step 1: Stop any current playback and reset the UI.
            // This ensures a clean state before loading new data.
            _fmodManager.Stop();
            lblTime.Text = UiConstants.LblFormatTimeZero;
            _searchController.ClearSearch();

            // Set the application state to Loading to disable UI and show progress.
            SetApplicationState(ApplicationState.Loading);
            lblStatus.Text = UiConstants.StatusInitializing;

            // Clear existing data from the UI controls.
            treeViewInfo.BeginUpdate();
            treeViewInfo.Nodes.Clear();
            listViewDetails.Items.Clear();
            _originalNodes.Clear();
            treeViewInfo.EndUpdate();

            // Step 2: Unload all previously loaded banks to ensure a clean slate for FMOD system.
            if (_fmodManager.StudioSystem.isValid())
            {
                _fmodManager.StudioSystem.unloadAll();
            }

            var failedFiles = new ConcurrentBag<(string FilePath, Exception ex)>();
            List<TreeNode> loadedNodes = new List<TreeNode>();

            try
            {
                var assetLoader = new AssetLoader(_fmodManager);
                var cts = new CancellationTokenSource();

                // Set up a progress handler to receive updates from the background task.
                var progressHandler = new Progress<ProgressReport>(report =>
                {
                    if (this.IsDisposed || !this.IsHandleCreated || _isClosing)
                    {
                        return;
                    }
                    if (_currentState != ApplicationState.Loading)
                    {
                        return;
                    }

                    lblStatus.Text = report.Status;
                    if (report.Percentage >= 0)
                    {
                        // Update progress bar safely by clamping the value to the current maximum.
                        int safeValue = Math.Min(report.Percentage, progressBar.Maximum);
                        progressBar.Value = safeValue;
                    }
                });

                // Step 3: Asynchronously load all assets.
                // This delegates the heavy lifting of file parsing to the AssetLoader service.
                var result = await assetLoader.LoadAssetsAsync(inputPaths, progressHandler, cts.Token);

                loadedNodes = result.Nodes;
                foreach (var failure in result.FailedFiles)
                {
                    failedFiles.Add(failure);
                }

                // Step 4: Populate the TreeView with the loaded nodes.
                // Batch updates are used to improve performance.
                treeViewInfo.BeginUpdate();
                treeViewInfo.Nodes.AddRange(loadedNodes.ToArray());
                foreach (TreeNode n in treeViewInfo.Nodes)
                {
                    _originalNodes.Add(n);
                }
                _searchController.SetDataSource(_originalNodes);
                treeViewInfo.EndUpdate();

                // Ensure the progress bar reflects completion safely.
                progressBar.Value = progressBar.Maximum;
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    failedFiles.Add(("<General Operation>", ex));
                    MessageBox.Show($"Error processing files: {ex.Message}", UiConstants.MsgError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!_isClosing)
                {
                    // Step 5: If any errors occurred, log them and notify the user.
                    if (!failedFiles.IsEmpty)
                    {
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

                    // Reset the application state to Idle.
                    SetApplicationState(ApplicationState.Idle);
                    lblStatus.Text = $"[READY] {loadedNodes.Count} files loaded. ({failedFiles.Count} failures)";
                }
            }
        }

        /// <summary>
        /// Allows the user to manually select and load a strings.bank file to resolve display names.
        /// </summary>
        private void LoadStringsBankManually()
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = FileConstants.FilterStringsBank,
                Title = "Select Strings Bank (e.g. Master.strings.bank)"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_fmodManager.StudioSystem.isValid())
                        {
                            // Load the bank file into the FMOD Studio system.
                            RESULT res = _fmodManager.StudioSystem.loadBankFile(ofd.FileName, LOAD_BANK_FLAGS.NORMAL, out Bank sb);
                            if (res == RESULT.OK || res == RESULT.ERR_EVENT_ALREADY_LOADED)
                            {
                                // Refresh the names of all nodes in the TreeView.
                                treeViewInfo.BeginUpdate();
                                RefreshNodeNamesRecursive(treeViewInfo.Nodes);
                                treeViewInfo.EndUpdate();
                                MessageBox.Show("Strings Bank loaded. Node names have been refreshed.", UiConstants.MsgSuccess, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                Utilities.CheckFmodResult(res);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load strings bank: {ex.Message}", UiConstants.MsgError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively traverses the TreeView and updates node names based on loaded FMOD data.
        /// </summary>
        /// <param name="nodes">The collection of nodes to refresh.</param>
        private void RefreshNodeNamesRecursive(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is NodeData data)
                {
                    string newName = null;

                    // Attempt to get the path/name from the FMOD object.
                    if (data is EventNode eventNode && eventNode.EventObject.isValid())
                    {
                        eventNode.EventObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p))
                        {
                            newName = p.Substring(p.LastIndexOf('/') + 1);
                        }
                    }
                    else if (data is BankNode bankNode && bankNode.BankObject.isValid())
                    {
                        bankNode.BankObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p))
                        {
                            newName = Path.GetFileName(p);
                        }
                    }
                    else if (data is BusNode busNode && busNode.BusObject.isValid())
                    {
                        busNode.BusObject.getPath(out string p);
                        if (!string.IsNullOrEmpty(p))
                        {
                            newName = p.Substring(p.LastIndexOf('/') + 1);
                        }
                    }

                    // Update the node's text if a new name was found.
                    if (!string.IsNullOrEmpty(newName) && newName != node.Text)
                    {
                        node.Text = newName;
                    }
                }

                // Recurse through child nodes.
                if (node.Nodes.Count > 0)
                {
                    RefreshNodeNamesRecursive(node.Nodes);
                }
            }
        }

        #endregion

        #region 6. Playback Logic

        /// <summary>
        /// Updates the playback status, including the time label and seek bar position.
        /// </summary>
        private void UpdatePlaybackStatus()
        {
            var (isPlaying, currentPos, totalLength) = _fmodManager.GetPlaybackStatus();
            btnPlay.Text = isPlaying ? UiConstants.BtnTextPause : UiConstants.BtnTextPlay;

            if (totalLength > 0)
            {
                lblTime.Text = string.Format(UiConstants.LblFormatTime, TimeSpan.FromMilliseconds(currentPos), TimeSpan.FromMilliseconds(totalLength));

                // Update the seek bar only if the user is not actively dragging it.
                if (!_isDraggingSeek)
                {
                    int newVal = (int)((float)currentPos / totalLength * ControlConstants.SeekBarMax);
                    trackSeek.Value = Math.Min(Math.Max(0, newVal), ControlConstants.SeekBarMax);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnPlay control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnPlay_Click(object sender, EventArgs e)
        {
            TogglePause();
        }

        /// <summary>
        /// Handles the Click event of the btnStop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            _fmodManager.Stop();
        }

        /// <summary>
        /// Toggles the pause state of the current playback or starts playback if stopped.
        /// </summary>
        private void TogglePause()
        {
            // If FMOD manager reports that toggling pause did nothing (i.e., not playing), start new playback.
            if (!_fmodManager.TogglePause())
            {
                PlaySelection();
            }
        }

        /// <summary>
        /// Asynchronously starts playback for the currently selected item.
        /// </summary>
        private async void PlaySelection()
        {
            if (_currentSelection == null || _isClosing)
            {
                return;
            }

            float targetVolume = trackVol.Value / 100.0f;
            bool isLooping = chkLoop.Checked;

            lblStatus.Text = UiConstants.StatusPlaybackLoading;

            try
            {
                // Define a callback to attach the audio analyzer when playback starts.
                Action<FMOD.System, FMOD.Channel, FMOD.Sound> onPlaybackStart = (core, channel, sound) =>
                {
                    if (_audioAnalyzer != null && !_audioAnalyzer.IsDisposed)
                    {
                        // Invoke on the UI thread to prevent cross-thread exceptions.
                        this.BeginInvoke(new Action(() =>
                        {
                            if (_audioAnalyzer != null && !_audioAnalyzer.IsDisposed)
                            {
                                _audioAnalyzer.AttachToAudio(core, channel, sound);
                            }
                        }));
                    }
                };

                // Start playback via the FMOD manager.
                await _fmodManager.PlaySelectionAsync(_currentSelection, targetVolume, isLooping, onPlaybackStart);

                // Update the status label with the name of the playing sound.
                if (_currentSelection is AudioDataNode adn)
                {
                    lblStatus.Text = $"[PLAYBACK] Now Playing: {adn.CachedAudio.Name}";
                }
                else if (_currentSelection is EventNode)
                {
                    lblStatus.Text = "[PLAYBACK] Playing FMOD Event";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"[ERROR] Playback failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Handles the Scroll event of the trackVol control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackVol_Scroll(object sender, EventArgs e)
        {
            lblVol.Text = string.Format(UiConstants.LblFormatVolume, trackVol.Value);
            float vol = trackVol.Value / 100.0f;
            _fmodManager.SetVolume(vol);
        }

        /// <summary>
        /// Handles the MouseDown event of the trackSeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void trackSeek_MouseDown(object sender, MouseEventArgs e)
        {
            _isDraggingSeek = true;
        }

        /// <summary>
        /// Handles the MouseUp event of the trackSeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void trackSeek_MouseUp(object sender, MouseEventArgs e)
        {
            uint totalLength = _fmodManager.GetPlaybackStatus().TotalLength;
            if (totalLength > 0)
            {
                // Calculate and set the new playback position.
                uint newPos = (uint)((float)trackSeek.Value / ControlConstants.SeekBarMax * totalLength);
                _fmodManager.SetPosition(newPos);
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
            _fmodManager.SetLooping(chkLoop.Checked);
        }

        #endregion

        #region 7. Selection & Details Logic

        /// <summary>
        /// Handles the AfterCheck event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void TreeViewInfo_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Use a flag to prevent recursive event firing.
            if (_isUpdatingChecks)
            {
                return;
            }

            _isUpdatingChecks = true;
            CheckAllChildren(e.Node, e.Node.Checked);
            _isUpdatingChecks = false;
        }

        /// <summary>
        /// Recursively sets the checked state of all child nodes.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="isChecked">The checked state to apply.</param>
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
            // Show context menu on right-click.
            if (e.Button == MouseButtons.Right)
            {
                treeViewInfo.SelectedNode = e.Node;
                if (e.Node.Tag is NodeData data)
                {
                    SetupContextMenu(data);
                }
            }
        }

        /// <summary>
        /// Configures the visibility and enabled state of context menu items based on the selected node type.
        /// </summary>
        /// <param name="data">The <see cref="NodeData"/> of the selected node.</param>
        private void SetupContextMenu(NodeData data)
        {
            // Determine the type of the selected node.
            bool isAudio = data is AudioDataNode;
            bool isContainer = data is FsbFileNode || data is BankNode;
            bool hasGuid = data is EventNode || data is BankNode;

            // Enable/disable menu items based on context.
            extractContextMenuItem.Enabled = isAudio;
            copyGuidContextMenuItem.Enabled = hasGuid;

            // Dynamically add or find the "Rebuild Manager" menu item.
            var managerItem = treeViewContextMenu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == UiConstants.MenuRebuildManager);
            if (managerItem == null)
            {
                managerItem = new ToolStripMenuItem(UiConstants.MenuRebuildManager);
                managerItem.Click += RebuildManagerContextMenuItem_Click;
                treeViewContextMenu.Items.Insert(4, managerItem);
            }

            managerItem.Visible = true;
            managerItem.Enabled = (isContainer || isAudio);

            // Hide the legacy rebuild item.
            var legacyRebuildItem = treeViewContextMenu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == UiConstants.MenuRebuildLegacy);
            if (legacyRebuildItem != null)
            {
                legacyRebuildItem.Visible = false;
            }
        }

        /// <summary>
        /// Handles the AfterSelect event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void treeViewInfo_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_isClosing)
            {
                return;
            }

            // Do NOT call Stop() immediately.
            // The logic inside FmodManager's PlaySelectionAsync handles cleanup, and calling Stop() here
            // can cause race conditions during rapid selection changes.
            _currentSelection = e.Node.Tag as NodeData;
            UpdateDetailsView();
        }

        /// <summary>
        // Handles the NodeMouseDoubleClick event of the treeViewInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeNodeMouseClickEventArgs"/> instance containing the event data.</param>
        private void treeViewInfo_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            PlaySelection();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvSearchResults.SelectedItems.Count > 0)
            {
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
            if (lvSearchResults.SelectedItems.Count > 0)
            {
                PlaySelection();
            }
        }

        /// <summary>
        /// Handles the MouseClick event of the lvSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void LvSearchResults_MouseClick(object sender, MouseEventArgs e)
        {
            // Show context menu for search results on right-click.
            if (e.Button == MouseButtons.Right)
            {
                // Retrieve the item directly under the mouse cursor to ensure accurate selection.
                ListViewItem targetItem = lvSearchResults.GetItemAt(e.X, e.Y);

                if (targetItem != null)
                {
                    TreeNode targetNode = targetItem.Tag as TreeNode;
                    NodeData data = targetNode?.Tag as NodeData;

                    // Create and configure a dynamic context menu.
                    ContextMenuStrip searchMenu = new ContextMenuStrip();

                    ToolStripMenuItem selectAllItem = new ToolStripMenuItem(UiConstants.MenuSelectAll);
                    selectAllItem.Click += (s, args) => CheckAllInCurrentView();
                    searchMenu.Items.Add(selectAllItem);
                    searchMenu.Items.Add(new ToolStripSeparator());

                    ToolStripMenuItem openLocItem = new ToolStripMenuItem(UiConstants.CtxMenuOpenFileLocation);
                    openLocItem.Click += (s, args) =>
                    {
                        if (targetNode != null)
                        {
                            // Clear the search query to exit search mode and properly restore the TreeView.
                            txtSearch.Text = string.Empty;

                            // Switch back to the TreeView and select the corresponding node.
                            lvSearchResults.Visible = false;
                            treeViewInfo.Visible = true;
                            treeViewInfo.SelectedNode = targetNode;
                            targetNode.EnsureVisible();
                            treeViewInfo.Focus();
                        }
                    };
                    searchMenu.Items.Add(openLocItem);
                    searchMenu.Items.Add(new ToolStripSeparator());

                    // Configure extraction and rebuild options.
                    ToolStripMenuItem extractItem = new ToolStripMenuItem(UiConstants.CtxMenuExtractItem);
                    if (data is AudioDataNode)
                    {
                        extractItem.Click += (s, args) =>
                        {
                            treeViewInfo.SelectedNode = targetNode;
                            extractContextMenuItem_Click(s, args);
                        };
                    }
                    else
                    {
                        extractItem.Enabled = false;
                    }
                    searchMenu.Items.Add(extractItem);

                    ToolStripMenuItem rebuildItem = new ToolStripMenuItem(UiConstants.CtxMenuRebuildItem);
                    if (data is AudioDataNode)
                    {
                        rebuildItem.Click += (s, args) =>
                        {
                            _currentSelection = data;
                            RebuildManagerContextMenuItem_Click(s, args);
                        };
                    }
                    else
                    {
                        rebuildItem.Enabled = false;
                    }
                    searchMenu.Items.Add(rebuildItem);
                    searchMenu.Items.Add(new ToolStripSeparator());

                    // Configure "Copy" options.
                    ToolStripMenuItem copyName = new ToolStripMenuItem(UiConstants.CtxMenuCopyName);
                    copyName.Click += (s, args) => Clipboard.SetText(targetNode != null ? targetNode.Text : targetItem.Text);
                    searchMenu.Items.Add(copyName);

                    ToolStripMenuItem copyPath = new ToolStripMenuItem(UiConstants.CtxMenuCopyPath);
                    copyPath.Click += (s, args) => Clipboard.SetText(targetNode != null ? targetNode.FullPath : targetItem.SubItems[3].Text);
                    searchMenu.Items.Add(copyPath);

                    ToolStripMenuItem copyGuid = new ToolStripMenuItem(UiConstants.CtxMenuCopyGuid);
                    bool hasGuid = false;
                    if (data is EventNode eventNode && eventNode.EventObject.isValid())
                    {
                        eventNode.EventObject.getID(out GUID id);
                        copyGuid.Click += (s, args) => Clipboard.SetText(Utilities.GuidToString(id));
                        hasGuid = true;
                    }
                    else if (data is BankNode bankNode && bankNode.BankObject.isValid())
                    {
                        bankNode.BankObject.getID(out GUID id);
                        copyGuid.Click += (s, args) => Clipboard.SetText(Utilities.GuidToString(id));
                        hasGuid = true;
                    }
                    copyGuid.Enabled = hasGuid;
                    searchMenu.Items.Add(copyGuid);

                    // Display the context menu at the cursor's position.
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
            // Allow checking all items by clicking the checkbox column header.
            if (e.Column == 0)
            {
                CheckAllInCurrentView();
            }
        }

        /// <summary>
        /// Checks or unchecks all visible items in either the search results or the main TreeView.
        /// </summary>
        private void CheckAllInCurrentView()
        {
            if (lvSearchResults.Visible)
            {
                // Determine the target state (check all if any are unchecked).
                bool anyUnchecked = false;
                foreach (ListViewItem item in lvSearchResults.Items)
                {
                    if (!item.Checked)
                    {
                        anyUnchecked = true;
                        break;
                    }
                }

                // Apply the new checked state to all items.
                lvSearchResults.BeginUpdate();
                foreach (ListViewItem item in lvSearchResults.Items)
                {
                    item.Checked = anyUnchecked;
                }
                lvSearchResults.EndUpdate();
            }
            else
            {
                // Determine the target state for the TreeView.
                bool anyUnchecked = false;
                foreach (TreeNode node in treeViewInfo.Nodes)
                {
                    if (!node.Checked)
                    {
                        anyUnchecked = true;
                        break;
                    }
                }

                // Apply the new checked state recursively.
                treeViewInfo.BeginUpdate();
                _isUpdatingChecks = true;
                foreach (TreeNode node in treeViewInfo.Nodes)
                {
                    node.Checked = anyUnchecked;
                    CheckAllChildren(node, anyUnchecked);
                }
                _isUpdatingChecks = false;
                treeViewInfo.EndUpdate();
            }
        }

        /// <summary>
        /// Updates the search results ListView based on data from the SearchController.
        /// </summary>
        private void UpdateSearchResultsUI()
        {
            lvSearchResults.BeginUpdate();
            lvSearchResults.Items.Clear();

            var results = _searchController.Results;

            if (results.Count > 0)
            {
                // Initialize columns if not already present.
                if (lvSearchResults.Columns.Count == 0)
                {
                    lvSearchResults.Columns.Add("", 20, HorizontalAlignment.Center);
                    lvSearchResults.Columns.Add("Name", 220);
                    lvSearchResults.Columns.Add("Type", 80);
                    lvSearchResults.Columns.Add("Path", 300);
                }

                // Populate the ListView with search results.
                foreach (var item in results)
                {
                    var lvItem = new ListViewItem("");
                    lvItem.Checked = item.Checked;
                    lvItem.SubItems.Add(item.Name);
                    lvItem.SubItems.Add(item.Type);
                    lvItem.SubItems.Add(item.FullPath);
                    lvItem.Tag = item.Tag; // Store reference to original TreeNode.

                    lvSearchResults.Items.Add(lvItem);
                }
            }

            // Toggle visibility between search results and the main tree view.
            bool hasQuery = !string.IsNullOrWhiteSpace(txtSearch.Text);
            lvSearchResults.Visible = hasQuery;
            treeViewInfo.Visible = !hasQuery;

            lvSearchResults.EndUpdate();

            // Ensure progress bar shows completion if results were found.
            if (results.Count > 0)
            {
                progressBar.Value = progressBar.Maximum;
            }
        }

        #endregion

        #region 8. Details View & Properties

        /// <summary>
        /// Updates the details view with information about the currently selected node.
        /// </summary>
        private void UpdateDetailsView()
        {
            // Delegate UI updates to the DetailsViewController.
            _detailsViewController.UpdateDetails(_currentSelection);

            if (_currentSelection == null)
            {
                return;
            }

            // Update playback length information based on the selection.
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

            // Set the total length in the FMOD manager and update the time label.
            _fmodManager.SetCurrentTotalLength(len);
            lblTime.Text = string.Format(UiConstants.LblFormatTime, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(len));

            // Automatically start playback if the auto-play option is enabled.
            // This is implemented with a debounce timer to handle rapid selection changes gracefully.
            if (chkAutoPlay.Checked)
            {
                // Reset the timer to delay playback until the selection has settled.
                _autoPlayDebounceTimer.Stop();
                _autoPlayDebounceTimer.Start();
            }
            else
            {
                _autoPlayDebounceTimer.Stop();
            }
        }

        #endregion

        #region 9. Extraction & Rebuilding

        /// <summary>
        /// Handles the Click event of the exportCsvToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void exportCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeViewInfo.Nodes.Count == 0)
                {
                    MessageBox.Show("Nothing to export.", UiConstants.MsgInfo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Prompt the user for a save location.
                string defaultName = string.Format(FileConstants.NameFormatCsvExport, DateTime.Now);
                using (SaveFileDialog sfd = new SaveFileDialog { Filter = FileConstants.FilterCsv, FileName = defaultName })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Delegate the export task to the ExtractionService.
                        await _extractionService.ExportToCsvAsync(treeViewInfo.Nodes, sfd.FileName);

                        // Show a detailed success message with the standardized format.
                        string successMessage = $"CSV Export Complete!\n\nSaved to: {sfd.FileName}";
                        MessageBox.Show(successMessage, UiConstants.MsgSuccess, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred while exporting to CSV: {ex.Message}", UiConstants.MsgExportError, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                PerformExtractionAsync(onlyChecked: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred during extraction: {ex.Message}", UiConstants.MsgExtractionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                PerformExtractionAsync(onlyChecked: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An unexpected error occurred during extraction: {ex.Message}", UiConstants.MsgExtractionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RestoreUiAfterError();
            }
        }

        /// <summary>
        /// Handles the Click event of the exitToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cmbExtractLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CmbExtractLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Store the previous selection to revert if the user cancels.
            int previousIndex = (cmbExtractLocation.Tag is int idx) ? idx : 0;
            var selectedMode = (ExtractLocationMode)cmbExtractLocation.SelectedIndex;

            if (selectedMode == ExtractLocationMode.CustomPath)
            {
                // Prompt the user to select a custom folder.
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select a custom folder for all future extractions.";
                    if (!string.IsNullOrEmpty(_customExtractPath) && Directory.Exists(_customExtractPath))
                    {
                        fbd.SelectedPath = _customExtractPath;
                    }

                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        // Save the selected path.
                        _customExtractPath = fbd.SelectedPath;
                        lblStatus.Text = $"[INFO] Custom extraction path set to: {_customExtractPath}";
                        cmbExtractLocation.Tag = (int)selectedMode;
                    }
                    else
                    {
                        // If canceled, revert to the previous selection.
                        lblStatus.Text = "[INFO] Custom path selection cancelled.";
                        cmbExtractLocation.SelectedIndex = previousIndex;
                    }
                }
            }
            else
            {
                // For other modes, just save the selection.
                cmbExtractLocation.Tag = (int)selectedMode;
            }
        }

        /// <summary>
        /// Determines the base extraction path based on the user's selected mode.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the selected base path, a special marker for "Same as Source", or null if the operation was cancelled.</returns>
        private async Task<string> GetBasePathForExtractionAsync()
        {
            var selectedMode = (ExtractLocationMode)cmbExtractLocation.SelectedIndex;
            string basePath = null;

            switch (selectedMode)
            {
                case ExtractLocationMode.AskEveryTime:
                    // Show a folder browser dialog for each extraction.
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "Select a folder where the extracted files will be saved.";

                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            basePath = fbd.SelectedPath;
                        }
                    }
                    break;

                case ExtractLocationMode.CustomPath:
                    // Use the previously saved custom path.
                    if (string.IsNullOrEmpty(_customExtractPath) || !Directory.Exists(_customExtractPath))
                    {
                        // If the path is invalid, re-prompt the user.
                        MessageBox.Show("The previously set custom path is no longer valid. Please select a new one.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        CmbExtractLocation_SelectedIndexChanged(null, null);

                        if (string.IsNullOrEmpty(_customExtractPath) || !Directory.Exists(_customExtractPath))
                        {
                            basePath = null;
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
                    // Return a special marker to indicate this mode.
                    return FileConstants.PathMarkerSameAsSource;
            }

            // If the user canceled the folder selection, return null.
            if ((selectedMode == ExtractLocationMode.AskEveryTime || selectedMode == ExtractLocationMode.CustomPath) && string.IsNullOrEmpty(basePath))
            {
                lblStatus.Text = UiConstants.StatusExtractionCancelled;
                return null;
            }

            return basePath;
        }

        /// <summary>
        /// Asynchronously performs the extraction process for selected or all audio items.
        /// </summary>
        /// <remarks>
        /// Processing steps:
        ///  1) Identify and collect all valid audio nodes for extraction.
        ///  2) Resolve the destination path based on user settings.
        ///  3) Initialize extraction state and progress monitoring.
        ///  4) Execute the batch extraction logic.
        ///  5) Report results and cleanup.
        /// </remarks>
        /// <param name="onlyChecked"><c>true</c> to extract only checked items; <c>false</c> to extract all.</param>
        private async void PerformExtractionAsync(bool onlyChecked)
        {
            // Step 1: Collect the list of nodes to be extracted.
            // This filters the selection based on checkbox state and view mode (Tree vs Search).
            List<TreeNode> extractList = new List<TreeNode>();
            if (onlyChecked == false)
            {
                FindCheckedAudioNodesRecursive(_originalNodes, extractList, false);
            }
            else
            {
                if (lvSearchResults.Visible)
                {
                    // Collect from search results if visible.
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
                    // Collect from the main TreeView.
                    if (treeViewInfo.Nodes.Count == 0)
                    {
                        return;
                    }
                    FindCheckedAudioNodesRecursive(treeViewInfo.Nodes, extractList, true);
                }
            }

            if (extractList.Count == 0)
            {
                MessageBox.Show("No audio items selected for extraction.", UiConstants.MsgInfo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step 2: Get the user-defined output path.
            // Abort early if the user cancels the directory selection dialog.
            string userSelectedPath = await GetBasePathForExtractionAsync();
            if (userSelectedPath == null)
            {
                return;
            }

            // Determine the root path for the final report message.
            string reportRootPath = userSelectedPath;
            if (reportRootPath == FileConstants.PathMarkerSameAsSource)
            {
                if (extractList.Count > 0 && extractList[0].Tag is AudioDataNode firstData)
                {
                    reportRootPath = Path.GetDirectoryName(firstData.CachedAudio.SourcePath);
                }
                else
                {
                    reportRootPath = Directory.GetCurrentDirectory();
                }
            }

            // Step 3: Stop playback and prepare UI for extraction.
            _fmodManager.Stop();
            SetApplicationState(ApplicationState.Extracting);
            _extractionService.SetTotalFilesForSession(extractList.Count);

            // Set up a progress handler for UI updates.
            var progressHandler = new Progress<ProgressReport>(report =>
            {
                if (this.IsDisposed || !this.IsHandleCreated || _isClosing)
                {
                    return;
                }
                if (_currentState != ApplicationState.Extracting)
                {
                    return;
                }

                lblStatus.Text = report.Status;
                if (report.Percentage >= 0)
                {
                    // Clamp value against Maximum to prevent crashes if Percentage exceeds 100 or Maximum is incorrect.
                    int safeValue = Math.Min(report.Percentage, progressBar.Maximum);
                    progressBar.Value = safeValue;
                }
            });

            // Step 4: Delegate the extraction task to the ExtractionService.
            var (successCount, failCount, totalBytes) = await _extractionService.ExtractAsync(
                extractList,
                userSelectedPath,
                chkVerboseLog.Checked,
                progressHandler
            );

            // Step 5: Finalize process and report results.
            progressBar.Value = progressBar.Maximum;
            SetApplicationState(ApplicationState.Idle);
            lblStatus.Text = $"[COMPLETE] Extraction finished. Success: {successCount}, Failed: {failCount}";

            string reportMessage = $"Process Complete!\n\n" +
                                   $"Total Processed: {extractList.Count}\n" +
                                   $"Success: {successCount}\n" +
                                   $"Failed: {failCount}\n\n" +
                                   $"Elapsed Time: {_scanStopwatch.Elapsed:hh\\:mm\\:ss\\.ff}\n\n" +
                                   $"Output Location:\n{reportRootPath}";

            MessageBoxIcon icon = failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(reportMessage, UiConstants.TitleExtractionReport, MessageBoxButtons.OK, icon);

            SetApplicationState(ApplicationState.Idle);
        }

        /// <summary>
        /// Recursively finds all audio nodes, optionally filtering by their checked state.
        /// </summary>
        /// <param name="nodes">The collection of nodes to search through.</param>
        /// <param name="foundNodes">A list to store the found audio nodes.</param>
        /// <param name="onlyChecked"><c>true</c> to only add checked nodes; <c>false</c> to add all audio nodes.</param>
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
                    FindCheckedAudioNodesRecursive(n.Nodes, foundNodes, onlyChecked);
                }
            }
        }

        #endregion

        #region 10. Context Menus & Dialogs

        /// <summary>
        /// Handles the Click event of the expandAllToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewInfo.ExpandAll();
        }

        /// <summary>
        /// Handles the Click event of the collapseAllToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewInfo.CollapseAll();
        }

        /// <summary>
        /// Handles the Click event of the copyNameContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyNameContextMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewInfo.SelectedNode != null)
            {
                Clipboard.SetText(treeViewInfo.SelectedNode.Text);
            }
        }

        /// <summary>
        /// Handles the Click event of the copyPathContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyPathContextMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewInfo.SelectedNode != null)
            {
                Clipboard.SetText(treeViewInfo.SelectedNode.FullPath);
            }
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
                    Clipboard.SetText(Utilities.GuidToString(id));
                }
                else if (d is BankNode bankNode && bankNode.BankObject.isValid())
                {
                    bankNode.BankObject.getID(out GUID id);
                    Clipboard.SetText(Utilities.GuidToString(id));
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the playContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void playContextMenuItem_Click(object sender, EventArgs e)
        {
            TogglePause();
        }

        /// <summary>
        /// Handles the Click event of the stopContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void stopContextMenuItem_Click(object sender, EventArgs e)
        {
            _fmodManager.Stop();
        }

        /// <summary>
        /// Handles the Click event of the extractContextMenuItem control.
        /// </summary>
        /// <remarks>
        /// Processing steps for single file extraction:
        ///  1) Verify that a valid audio node is selected.
        ///  2) Prompt the user for a save file path.
        ///  3) Initialize the UI state and a verbose logger if enabled.
        ///  4) Execute the asynchronous extraction via ExtractionService.
        ///  5) Process the result, log details, and provide user feedback.
        ///  6) Handle any exceptions and ensure final cleanup.
        /// </remarks>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void extractContextMenuItem_Click(object sender, EventArgs e)
        {
            // Initialize core variables for the single file extraction session.
            var selectedNode = treeViewInfo.SelectedNode;
            string finalFilePath = string.Empty;
            LogWriter localLogger = null;

            try
            {
                // Step 1: Verify that the selection is valid and contains audio data before proceeding.
                if (selectedNode?.Tag is AudioDataNode data)
                {
                    // Step 2: Open a Save File Dialog to define the output path for the WAV file.
                    using (var sfd = new SaveFileDialog())
                    {
                        // Set default file name and filters for the dialog.
                        sfd.Title = "Save Audio File";
                        sfd.Filter = FileConstants.FilterWav;
                        sfd.FileName = Utilities.SanitizeFileName(selectedNode.Text) + FileConstants.ExtWav;

                        // If the user confirms the save path.
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            finalFilePath = sfd.FileName;
                            string outputDir = Path.GetDirectoryName(finalFilePath);
                            string soundName = data.CachedAudio.Name;

                            // Step 3: Transition the application to the Extracting state and initialize logging.
                            _fmodManager.Stop();
                            SetApplicationState(ApplicationState.Extracting);

                            // Initialize the detailed session logger if enabled.
                            if (chkVerboseLog.Checked)
                            {
                                string logFile = Path.Combine(outputDir, string.Format(FileConstants.NameFormatExtractionLog, DateTime.Now));
                                localLogger = new LogWriter(logFile);

                                // Write the standard log header for session identification.
                                localLogger.WriteRaw(LogConstants.SeparatorLine);
                                localLogger.WriteRaw($"[SESSION] Single File Extraction Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                localLogger.WriteRaw(LogConstants.SeparatorLine);
                                localLogger.WriteRaw($"[TOOL]    App Version:     {AppVersion} ({AppLastUpdate})");
                                localLogger.WriteRaw($"[TOOL]    Developer:       {AppDeveloper}");
                                localLogger.WriteRaw($"[ENGINE]  FMOD API:        {FmodFullVersion}");
                                localLogger.WriteRaw($"[SOURCE]  File:        {data.CachedAudio.SourcePath}");
                                localLogger.WriteRaw($"[SOURCE]  Sound:       {data.CachedAudio.Name} (Index: {data.CachedAudio.Index})");
                                localLogger.WriteRaw($"[SYSTEM]  OS Version:      {Environment.OSVersion}");
                                localLogger.WriteRaw($"[SYSTEM]  Processor Count: {Environment.ProcessorCount} Cores");
                                localLogger.WriteRaw($"[TARGET]  Output File:     {finalFilePath}");
                                localLogger.WriteRaw(LogConstants.SeparatorLine);
                                localLogger.WriteRaw("");
                                localLogger.WriteRaw("Timestamp\tLevel\tSourceFile\tSoundName\tResult\tEncoding\tContainer\tChannels\tBits\tFrequency(Hz)\tDuration(ms)\tLoopRange(ms)\tDataOffset\tOutputPath\tTimeTaken(ms)");
                            }

                            // Define a progress reporter to provide rich real-time feedback on the status bar.
                            var progressHandler = new Progress<ProgressReport>(report =>
                            {
                                // Ensure UI updates are only performed if the form is still alive.
                                if (this.IsDisposed || !this.IsHandleCreated || _isClosing)
                                {
                                    return;
                                }

                                // Construct a detailed status string for clarity.
                                string detailedStatus = $"[EXTRACTING] {soundName} | {report.Status} ({report.Percentage}%)";
                                lblStatus.Text = detailedStatus;

                                // Synchronize the progress bar value with the report, safely clamping to Maximum.
                                if (report.Percentage >= 0)
                                {
                                    int safeValue = Math.Min(report.Percentage, progressBar.Maximum);
                                    progressBar.Value = safeValue;
                                }
                            });

                            // Step 4: Start the extraction process asynchronously on a background thread.
                            Stopwatch sw = Stopwatch.StartNew();
                            long bytesWritten = await _extractionService.ExtractSingleWavAsync(data.CachedAudio, finalFilePath, progressHandler);
                            sw.Stop();

                            // Step 5: Process the result and provide final feedback to the user.
                            if (bytesWritten >= 0)
                            {
                                // Log technical metadata if verbose logging is active.
                                if (localLogger != null)
                                {
                                    var details = data.GetDetails();

                                    // This local function safely extracts a specific detail value from a list.
                                    // It prevents repetitive code and handles cases where a detail might be missing.
                                    // - group: The category of the detail (e.g., "Format").
                                    // - propName: The name of the property to find (e.g., "Encoding").
                                    // - Returns: The found value as a string, or "N/A" if not found.
                                    string GetDetailValue(string group, string propName)
                                    {
                                        var foundDetail = details.FirstOrDefault(d => d.Key.Equals(group, StringComparison.OrdinalIgnoreCase) && d.Value.StartsWith(propName, StringComparison.OrdinalIgnoreCase));
                                        if (foundDetail.Equals(default(KeyValuePair<string, string>)))
                                        {
                                            return "N/A";
                                        }

                                        var parts = foundDetail.Value.Split(new[] { ':' }, 2);
                                        if (parts.Length > 1)
                                        {
                                            return parts[1].Trim();
                                        }
                                        return "N/A";
                                    }

                                    var audioInfo = data.CachedAudio;

                                    localLogger.LogTSV(LogWriter.LogLevel.INFO,
                                        Path.GetFileName(audioInfo.SourcePath), audioInfo.Name, "OK",
                                        GetDetailValue("Format", "Encoding"), GetDetailValue("Format", "Container"),
                                        GetDetailValue("Format", "Channels"), GetDetailValue("Format", "Bits"),
                                        GetDetailValue("Format", "Frequency").Replace(" Hz", ""), GetDetailValue("Time", "Duration (ms)"),
                                        GetDetailValue("Looping", "Loop Range (ms)"), $"0x{audioInfo.DataOffset:X}",
                                        finalFilePath, sw.ElapsedMilliseconds.ToString());
                                }

                                // Set a final summary status message on the bar.
                                double fileSizeMb = bytesWritten / AppConstants.BytesToMegabytes;
                                lblStatus.Text = $"[COMPLETE] {soundName} | {fileSizeMb:F2} MB saved in {sw.Elapsed.TotalSeconds:F2}s";

                                // Show a modal completion message to the user.
                                MessageBox.Show($"File successfully saved to:\n{finalFilePath}", "Extraction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                // Update status and notify the user on failure.
                                lblStatus.Text = $"[ERROR] Failed to extract: {soundName}";
                                MessageBox.Show("Failed to extract audio file.", UiConstants.MsgError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Step 6: Capture and log any unexpected errors during the process.
                if (localLogger != null)
                {
                    localLogger.WriteRaw($"[ERROR] An exception occurred: {ex.Message}\n{ex.StackTrace}");
                }

                string context = $"Single file extraction of '{(selectedNode != null ? selectedNode.FullPath : "Unknown")}' to '{(string.IsNullOrEmpty(finalFilePath) ? "N/A" : finalFilePath)}'";
                string logFilePath = await LogOperationErrorAsync("Single File Extraction", new[] { (context, ex) });

                string userMessage = "An unexpected error occurred during extraction.\n\n" +
                                        $"Technical details have been saved to the log file:\n{Path.GetFileName(logFilePath)}";
                MessageBox.Show(this, userMessage, UiConstants.MsgExtractionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Reset the application to an Idle state and clean up the logger.
                SetApplicationState(ApplicationState.Idle);
                localLogger?.Dispose();
            }
        }

        /// <summary>
        /// Handles the Click event of the RebuildManagerContextMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void RebuildManagerContextMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeViewInfo.SelectedNode;
            if (selectedNode == null)
            {
                return;
            }

            // Check if the required external tool exists.
            if (!File.Exists(AppConstants.FsBankExecutable))
            {
                MessageBox.Show($"Rebuild tool '{AppConstants.FsBankExecutable}' not found in the application directory.\nPlease place it alongside the extractor.", UiConstants.MsgToolNotFound, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Collect all audio items from the selected node and its children.
            List<AudioInfo> audioList = new List<AudioInfo>();
            AudioDataNode refNodeData = null;

            if (selectedNode.Tag is AudioDataNode singleAudio)
            {
                audioList.Add(singleAudio.CachedAudio);
                refNodeData = singleAudio;
            }
            else if (selectedNode.Tag is NodeData)
            {
                // Recursive function to find all audio nodes under the selected container.
                void CollectAudio(TreeNode parent)
                {
                    foreach (TreeNode child in parent.Nodes)
                    {
                        if (child.Tag is AudioDataNode aNode)
                        {
                            audioList.Add(aNode.CachedAudio);
                            if (refNodeData == null)
                            {
                                refNodeData = aNode;
                            }
                        }
                        if (child.Nodes.Count > 0)
                        {
                            CollectAudio(child);
                        }
                    }
                }
                CollectAudio(selectedNode);
            }

            if (audioList.Count == 0 || refNodeData == null)
            {
                MessageBox.Show("No audio files found in selection to rebuild.", UiConstants.MsgInfo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Rebuilding is only supported for FSB5 format.
            char fsbVersion = Utilities.GetFsbVersion(refNodeData.CachedAudio.SourcePath, refNodeData.FsbChunkOffset);
            if (fsbVersion != '5')
            {
                MessageBox.Show($"Rebuilding is only supported for FSB5 format files.\nThe selected container appears to be FSB version '{fsbVersion}'.", UiConstants.MsgUnsupportedFormat, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Determine the display name for the Rebuild Manager window.
            string bankName = Path.GetFileName(refNodeData.CachedAudio.SourcePath);
            string fsbName = "";
            TreeNode parentNode = selectedNode.Tag is AudioDataNode ? selectedNode.Parent : selectedNode;

            if (parentNode != null && parentNode.Tag is FsbFileNode)
            {
                fsbName = parentNode.Text;
            }
            else if (selectedNode.Tag is FsbFileNode)
            {
                fsbName = selectedNode.Text;
            }

            string displayTargetName = string.IsNullOrEmpty(fsbName) ? bankName : $"{bankName} / {fsbName}";

            // Open the Rebuild Manager form.
            using (var mgr = new RebuildManagerForm(displayTargetName, audioList, _fmodManager.CoreSystem, _fmodManager.SyncLock))
            {
                if (mgr.ShowDialog(this) == DialogResult.OK)
                {
                    var batchList = mgr.ResultBatchItems;
                    var options = mgr.ResultOptions;

                    if (batchList.Count == 0)
                    {
                        return;
                    }

                    // Prompt for save location and execute the rebuild process.
                    using (SaveFileDialog sfd = new SaveFileDialog { Filter = FileConstants.FilterFmodFiles, FileName = bankName })
                    {
                        if (sfd.ShowDialog(this) == DialogResult.OK)
                        {
                            ExecuteBatchRebuild(refNodeData, batchList, sfd.FileName, options);
                        }
                    }
                }
            }

            // Force garbage collection to clean up the large amount of UI objects generated by the RebuildManagerForm.
            // This addresses the issue where opening the menu repeatedly causes significant RAM accumulation.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Asynchronously executes the batch rebuild process using the RebuildService.
        /// </summary>
        /// <remarks>
        /// Processing steps:
        ///  1) Initialize logger and subscribe to service events.
        ///  2) Delegate rebuilding to RebuildService (first attempt).
        ///  3) Handle "Oversize" scenarios by prompting user and retrying if necessary.
        ///  4) Finalize operation, cleanup resources, and update UI.
        /// </remarks>
        /// <param name="refNode">A reference audio node used to get source file information.</param>
        /// <param name="batchList">A list of items to be replaced.</param>
        /// <param name="savePath">The final path to save the rebuilt file.</param>
        /// <param name="options">The rebuilding options (e.g., encoding format).</param>
        private async void ExecuteBatchRebuild(AudioDataNode refNode, List<BatchItem> batchList, string savePath, RebuildOptions options)
        {
            // Step 1: Initialize environment and logging.
            // Stop playback and set state to Rebuilding to block other inputs.
            _fmodManager.Stop();
            SetApplicationState(ApplicationState.Rebuilding);
            lblStatus.Text = "[INITIALIZING] Starting rebuild process...";

            // If verbose logging is enabled, initialize the logger with a standard header.
            if (chkVerboseLog.Checked)
            {
                try
                {
                    string outputDir = Path.GetDirectoryName(savePath);
                    string logFileName = string.Format(FileConstants.NameFormatRebuildLog, DateTime.Now);
                    string logPath = Path.Combine(outputDir, logFileName);
                    _logger = new LogWriter(logPath);

                    // Write the standard log header to provide context and maintain a consistent log format.
                    _logger.WriteRaw(LogConstants.SeparatorLine);
                    _logger.WriteRaw($"[SESSION] Rebuild Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    _logger.WriteRaw(LogConstants.SeparatorLine);
                    _logger.WriteRaw($"[TOOL]    App Version:     {AppVersion} ({AppLastUpdate})");
                    _logger.WriteRaw($"[TOOL]    Developer:       {AppDeveloper}");
                    _logger.WriteRaw($"[ENGINE]  FMOD API:        {FmodFullVersion}");
                    _logger.WriteRaw($"[SYSTEM]  OS Version:      {Environment.OSVersion}");
                    _logger.WriteRaw($"[SYSTEM]  Processor Count: {Environment.ProcessorCount} Cores");
                    _logger.WriteRaw($"[SOURCE]  Base File:       {refNode.CachedAudio.SourcePath}");
                    _logger.WriteRaw($"[SOURCE]  Offset:          0x{refNode.FsbChunkOffset:X}");
                    _logger.WriteRaw($"[TARGET]  Output File:     {savePath}");
                    _logger.WriteRaw($"[CONFIG]  Format:          {options.EncodingFormat}");
                    _logger.WriteRaw($"[CONFIG]  Quality:         {(options.EncodingFormat == SOUND_TYPE.VORBIS ? "Auto-Optimized" : "Fixed")}");
                    _logger.WriteRaw($"[CONFIG]  Replacements:    {batchList.Count} files");
                    _logger.WriteRaw(LogConstants.SeparatorLine);
                    _logger.WriteRaw("");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
                }
            }

            // Define a local handler for the log event to ensure proper subscription and unsubscription.
            Action<string> onLogReceived = (msg) =>
            {
                // This handler is now the single point of responsibility for formatting
                // all log messages with a timestamp before writing them to the file.
                if (_logger != null)
                {
                    string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {msg}";
                    _logger.WriteRawNoTimestamp(formattedMessage);
                }
            };

            // Subscribe to the log event to capture detailed logs from the service.
            if (_logger != null)
            {
                _rebuildService.OnLogReceived += onLogReceived;
            }

            // Initialize variables to track the state and result of the rebuild operation.
            bool success = false;
            RebuildResult result = null;

            try
            {
                // Step 2: Delegate the primary rebuild task to the RebuildService.
                // This includes setting up the progress reporting callback.
                var progressHandler = new Progress<ProgressReport>(report =>
                {
                    if (this.IsDisposed || !this.IsHandleCreated || _isClosing)
                    {
                        return;
                    }

                    if (_currentState != ApplicationState.Rebuilding)
                    {
                        return;
                    }

                    lblStatus.Text = report.Status;
                    if (report.Percentage >= 0)
                    {
                        // Ensure the value does not exceed the current Maximum to avoid exceptions.
                        int safeValue = Math.Min(report.Percentage, progressBar.Maximum);
                        progressBar.Value = safeValue;
                    }
                });

                result = await _rebuildService.RebuildAsync(
                    refNode, batchList, savePath, options,
                    progressHandler,
                    forceOversize: false
                );

                // Step 3: Handle oversized file scenarios.
                // If the rebuilt file is larger than the original, user confirmation is required to avoid corruption.
                if (result.Status == RebuildStatus.OversizedConfirmationNeeded)
                {
                    string warningMessage = "Rebuild Warning: The resulting file is larger than the original.\n\n" +
                                            $" • Original Size: {result.OriginalFsbSize} bytes\n" +
                                            $" • New Size:      {result.NewFsbSize} bytes (+{result.NewFsbSize - result.OriginalFsbSize} bytes)\n\n" +
                                            "Patching this oversized FSB into a .bank file will likely corrupt it.\n" +
                                            "This is only safe if you are saving it as a standalone .fsb file.\n\n" +
                                            "Do you want to proceed and save the oversized file?";

                    // If the user confirms, retry the build with the forceOversize flag.
                    if (MessageBox.Show(this, warningMessage, "Oversized File Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        lblStatus.Text = "[CONFIRMED] Retrying build with oversized file...";
                        Application.DoEvents();

                        result = await _rebuildService.RebuildAsync(
                            refNode, batchList, savePath, options,
                            progressHandler,
                            forceOversize: true,
                            previousResult: result
                        );
                    }
                    else
                    {
                        // If the user cancels, update the status accordingly.
                        result.Status = RebuildStatus.CancelledByUser;
                        lblStatus.Text = UiConstants.StatusRebuildCancelled;
                    }
                }

                // Update the success flag and log any final failure messages.
                success = result.Success;
                if (!success && result.Status != RebuildStatus.CancelledByUser)
                {
                    _logger?.WriteRaw($"[ERROR] Rebuild failed. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                // Catch any critical exceptions during the rebuild process and log the error.
                string logFile = await LogOperationErrorAsync("Rebuild Process", new[] { ("Batch Operation", ex) });
                MessageBox.Show($"A critical error occurred during the rebuild process.\n\nDetails have been saved to:\n{logFile}", UiConstants.MsgRebuildError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Step 4: Finalize and cleanup.
                // Unsubscribe from the log event to prevent memory leaks.
                if (_logger != null)
                {
                    _rebuildService.OnLogReceived -= onLogReceived;
                }

                // Clean up the temporary workspace directory.
                if (result != null && result.WorkspacePath != null && Directory.Exists(result.WorkspacePath))
                {
                    try
                    {
                        await Task.Run(() => Directory.Delete(result.WorkspacePath, true));
                    }
                    catch (Exception cleanupEx)
                    {
                        // Silently ignore cleanup errors to prevent issues if a file is locked or permissions are denied.
                        System.Diagnostics.Debug.WriteLine($"Failed to clean up workspace '{result.WorkspacePath}': {cleanupEx.Message}");
                    }
                }

                // Force garbage collection to release memory used by large byte arrays during the build.
                if (_logger != null)
                {
                    _logger.WriteRaw("[INFO] Forcing garbage collection to release memory...");
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (_logger != null)
                {
                    _logger.WriteRaw("[INFO] Memory released.");
                }

                // Update the UI based on the final result of the operation.
                progressBar.Value = success ? progressBar.Maximum : 0;
                SetApplicationState(ApplicationState.Idle);
                lblStatus.Text = success ? "[COMPLETE] Rebuild successful." : "[ERROR] Rebuild failed or was cancelled.";

                // Show the final success message only after all cleanup and UI updates are complete.
                if (success)
                {
                    MessageBox.Show($"Rebuild Complete!\n\nSaved to: {savePath}", UiConstants.MsgSuccess, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the IndexToolItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IndexToolItem_Click(object sender, EventArgs e)
        {
            TreeNode targetNode = treeViewInfo.SelectedNode;
            if (targetNode == null)
            {
                return;
            }

            // If an audio node is selected, use its parent container.
            if (targetNode.Tag is AudioDataNode)
            {
                targetNode = targetNode.Parent;
            }

            // Validate that the selected node is a valid container of audio files.
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
                    UiConstants.MsgInvalidSelection, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open the Index Tool form to get user input.
            using (var form = new IndexToolForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string input = form.InputString;
                    if (form.IsJumpMode)
                    {
                        // Jump to a specific index.
                        int target = ExtractFirstNumber(input);
                        if (target != -1)
                        {
                            PerformJumpToIndex(targetNode, target);
                        }
                        else
                        {
                            MessageBox.Show("Invalid number format for Jump.", UiConstants.MsgError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        // Select multiple items based on a range string.
                        HashSet<int> targets = ParseRangeString(input);
                        PerformSmartSelect(targetNode, targets);
                    }
                }
            }
        }

        #endregion

        #region 11. Helper Methods & Dialogs

        /// <summary>
        /// Extracts the first numerical value from a string.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>The first integer found, or -1 if none is found.</returns>
        private int ExtractFirstNumber(string input)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int val))
            {
                return val;
            }
            return -1;
        }

        /// <summary>
        /// Parses a string containing numbers and ranges (e.g., "1, 3, 5-10") into a set of integers.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>A <see cref="HashSet{T}"/> containing all parsed integers.</returns>
        private HashSet<int> ParseRangeString(string input)
        {
            HashSet<int> result = new HashSet<int>();
            string[] parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string p = part.Trim();
                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }

                // Handle ranges (e.g., "5-10").
                if (p.Contains("-"))
                {
                    string[] rangeParts = p.Split('-');
                    if (rangeParts.Length >= 2 &&
                        int.TryParse(rangeParts[0], out int start) &&
                        int.TryParse(rangeParts[1], out int end))
                    {
                        int min = Math.Min(start, end);
                        int max = Math.Max(start, end);
                        for (int i = min; i <= max; i++)
                        {
                            result.Add(i);
                        }
                    }
                }
                else
                {
                    // Handle single numbers.
                    if (int.TryParse(p, out int val))
                    {
                        result.Add(val);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Selects and focuses an audio node within a parent container by its index.
        /// </summary>
        /// <param name="parent">The parent container node.</param>
        /// <param name="targetIndex">The index of the audio node to find.</param>
        private void PerformJumpToIndex(TreeNode parent, int targetIndex)
        {
            foreach (TreeNode node in parent.Nodes)
            {
                if (node.Tag is AudioDataNode nd)
                {
                    if (nd.CachedAudio.Index == targetIndex)
                    {
                        treeViewInfo.SelectedNode = node;
                        node.EnsureVisible();
                        treeViewInfo.Focus();
                        return;
                    }
                }
            }
            MessageBox.Show($"Index {targetIndex} not found in this file.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Checks all audio nodes within a parent container whose indices are in the target set.
        /// </summary>
        /// <param name="parent">The parent container node.</param>
        /// <param name="targets">A set of indices to select.</param>
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
        /// Displays the Help form as a modal dialog.
        /// </summary>
        private void ShowHelpForm()
        {
            using (HelpForm helpForm = new HelpForm())
            {
                helpForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// Displays the About dialog with application information.
        /// </summary>
        private void ShowAboutDialog()
        {
            string copyrightYear = AppLastUpdate.Length >= 4 ? AppLastUpdate.Substring(0, 4) : DateTime.Now.Year.ToString();
            MessageBox.Show($"FSB/BANK Extractor & Rebuilder (GUI)\n" +
                $"Version: {AppVersion}\n" +
                $"Update: {AppLastUpdate}\n\n" +
                $"Developer: {AppDeveloper}\n" +
                $"Website: {AppWebsite}\n\n" +
                $"Using FMOD Studio API version {FmodApiVersion}\n" +
                $" - Studio API minor release (build {FmodBuildNumber})\n\n" +
                $"© {copyrightYear} {AppDeveloper}. All rights reserved.", UiConstants.TitleProgramInfo);
        }

        /// <summary>
        /// Asynchronously logs details of exceptions that occurred during an operation.
        /// </summary>
        /// <param name="operationName">The name of the operation where the error occurred.</param>
        /// <param name="failedItems">A collection of tuples containing context and the exception object.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the path to the log file.</returns>
        private async Task<string> LogOperationErrorAsync(string operationName, IEnumerable<(string Context, Exception ex)> failedItems)
        {
            string logFileName = string.Format(FileConstants.NameFormatErrorLog, DateTime.Now);
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
            var sb = new StringBuilder();

            try
            {
                // Build the log content using the standardized application log format.
                sb.AppendLine(LogConstants.SeparatorLine);
                sb.AppendLine($"[SESSION] {operationName} Error Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine(LogConstants.SeparatorLine);
                sb.AppendLine($"[TOOL]    App Version:     {AppVersion} ({AppLastUpdate})");
                sb.AppendLine($"[TOOL]    Developer:       {AppDeveloper}");
                sb.AppendLine($"[ENGINE]  FMOD API:        {FmodFullVersion}");
                sb.AppendLine($"[SYSTEM]  OS Version:      {Environment.OSVersion}");
                sb.AppendLine($"[SYSTEM]  Processor Count: {Environment.ProcessorCount} Cores");
                sb.AppendLine($"[PATH]    Exec Path:       {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"[REPORT]  Total Errors:    {failedItems.Count()}");
                sb.AppendLine(LogConstants.SeparatorLine);
                sb.AppendLine("");

                int errorCount = 1;
                foreach (var (context, ex) in failedItems)
                {
                    sb.AppendLine($"[ERROR ITEM #{errorCount++}]");
                    sb.AppendLine($"Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    sb.AppendLine($"Context   : {context}");
                    sb.AppendLine($"Exception : {ex.GetType().Name}");
                    sb.AppendLine($"Message   : {ex.Message}");
                    sb.AppendLine("Stack Trace :");
                    sb.AppendLine(ex.StackTrace ?? "No stack trace available.");
                    sb.AppendLine("----------------------------------------------------------------");
                    sb.AppendLine("");
                }

                // Write the log content to a file.
                await Utilities.WriteAllTextAsync(logFilePath, sb.ToString());
            }
            catch (Exception writeEx)
            {
                // If logging fails, show the log content in a message box as a fallback.
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
        /// Restores the UI to a safe, idle state after a critical error.
        /// </summary>
        private void RestoreUiAfterError()
        {
            if (_currentState != ApplicationState.Idle)
            {
                SetApplicationState(ApplicationState.Idle);
                lblStatus.Text = "Ready after encountering an error.";
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Cleans up temporary files and directories left over from previous application sessions.
        /// </summary>
        private void CleanupOrphanedTempFiles()
        {
            string tempPath = Path.GetTempPath();
            string targetDir = Path.Combine(tempPath, FileConstants.TempDirName);

            // Quickly move the old directory to a "trash" location to avoid blocking startup.
            if (Directory.Exists(targetDir))
            {
                string newTrashDir = Path.Combine(tempPath, $"{FileConstants.TempCleanupDirPrefix}{Guid.NewGuid()}");
                try
                {
                    Directory.Move(targetDir, newTrashDir);
                }
                catch (IOException)
                {
                    // If moving fails (e.g., file locked), the background task will attempt cleanup later.
                }
            }

            // Perform the actual deletion in a background thread so it doesn't slow down the UI.
            Task.Run(() =>
            {
                try
                {
                    string[] trashFolders = Directory.GetDirectories(tempPath, $"{FileConstants.TempCleanupDirPrefix}*");

                    foreach (string trash in trashFolders)
                    {
                        try
                        {
                            // Use an external process (cmd.exe) for robust deletion of potentially locked files.
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c rmdir /s /q \"{trash}\"",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            Process.Start(psi);
                        }
                        catch
                        {
                            // Silently ignore errors for individual folders; they will be retried on the next run.
                        }
                    }
                }
                catch
                {
                    // Silently ignore errors during directory enumeration.
                }
            });
        }

        /// <summary>
        /// Populates the ImageList with system icons for the TreeView and ListView.
        /// </summary>
        private void SetupIcons()
        {
            // This check prevents re-adding icons if the method is called multiple times.
            if (imageList1.Images.Count == 0)
            {
                // Using system icons provides a familiar look and feel.
                imageList1.Images.Add("file", SystemIcons.WinLogo);
                imageList1.Images.Add("folder", SystemIcons.Shield);
                imageList1.Images.Add("event", SystemIcons.Exclamation);
                imageList1.Images.Add("param", SystemIcons.Question);
                imageList1.Images.Add("bus", SystemIcons.Application);
                imageList1.Images.Add("vca", SystemIcons.Hand);
                imageList1.Images.Add("audio", SystemIcons.Information);
            }
        }

        #endregion
    }
}