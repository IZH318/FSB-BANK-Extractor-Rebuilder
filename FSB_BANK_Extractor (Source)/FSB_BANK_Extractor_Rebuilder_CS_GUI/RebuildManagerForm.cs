/**
 * @file RebuildManagerForm.cs
 * @brief Provides an integrated GUI for managing FMOD audio replacements in both single and batch modes.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form serves as a modal dialog for users to specify audio file replacements before rebuilding an FSB container.
 * It validates the duration of replacement files against the originals and provides clear warnings to the user.
 * All file I/O and analysis operations are performed asynchronously to maintain UI responsiveness.
 *
 * Key Features:
 *  - Batch & Single Item Replacement: Manages one or more audio file replacements in a DataGridView.
 *  - Real-time Duration Validation: Asynchronously checks if replacement audio is longer or shorter than the original.
 *  - Auto-Match Functionality: Scans a folder to automatically find and suggest replacement files.
 *  - Robust Error Handling: Uses explicit FMOD.RESULT checking and CancellationTokens for thread safety.
 *  - Resource Management: Implements the IDisposable pattern to correctly release FMOD systems and tasks.
 *
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-13
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FMOD; // Needed for SOUND_TYPE enum

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class RebuildManagerForm : Form
    {
        #region 1. Public Properties & Fields

        public List<FSB_BANK_Extractor_Rebuilder_CS_GUI.BatchItem> ResultBatchItems { get; private set; }
        public RebuildOptions ResultOptions { get; private set; }

        private readonly List<FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo> _sourceList;
        private readonly string _containerName;

        // FMOD System for analysis.
        private FMOD.System _coreSystem;
        private readonly object _fmodLock = new object();

        // Cancellation Token for managing async tasks.
        private CancellationTokenSource _cts;

        #endregion

        #region 2. Constants

        private const int COL_IDX = 0;
        private const int COL_NAME = 1;
        private const int COL_INFO = 2;
        private const int COL_PATH = 3;
        private const int COL_BTN_FIND = 4;
        private const int COL_STATE = 5;
        private const int COL_BTN_RESET = 6;

        private const string MSG_LONG_BODY = "Proceeding may cause unexpected behavior with game event timelines or looping, as these often rely on the original audio's duration. Stability is not guaranteed.";
        private const string MSG_SHORT_BODY = "NOTE: The new audio is SHORTER. The remaining time might be filled with silence, which could affect looping behavior.";
        private const string MSG_LOOP_BODY = "This sound has loop points based on the original timeline.\nThe loop may behave unexpectedly if the new audio's length is different.";

        private const string OK_MSG_LONG = "OK: No items exceed their original duration.";
        private const string OK_MSG_SHORT = "OK: No items are shorter than their original.";
        private const string OK_MSG_LOOP = "OK: No potential looping issues detected based on duration changes.";

        #endregion

        #region 3. Initialization & Cleanup

        /// <summary>
        /// Initializes a new instance of the RebuildManagerForm class.
        /// </summary>
        /// <param name="containerName">The name of the container being rebuilt.</param>
        /// <param name="audioItems">The list of original audio items to be managed.</param>
        public RebuildManagerForm(string containerName, List<FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo> audioItems)
        {
            _containerName = containerName;
            _sourceList = audioItems;
            ResultOptions = new RebuildOptions();
            ResultBatchItems = new List<FSB_BANK_Extractor_Rebuilder_CS_GUI.BatchItem>();

            // Initialize the cancellation token source.
            _cts = new CancellationTokenSource();

            // Initialize a local FMOD system for performing duration checks.
            Factory.System_Create(out _coreSystem);
            // Use STREAM_FROM_UPDATE as we are only analyzing metadata, not playing audio.
            _coreSystem.init(32, INITFLAGS.STREAM_FROM_UPDATE, IntPtr.Zero);

            InitializeComponent();

            // Register the asynchronous load event to populate data after the form is created.
            this.Load += RebuildManagerForm_Load;

            // Set initial UI text values.
            this.Text = "Rebuild Manager";
            lblTarget.Text = $"Target: {_containerName}";

            // Configure the UI controls.
            InitializeGrid();
            PopulateFormatComboBox();
            UpdateWarningPanels();
        }

        /// <summary>
        /// Handles the Load event of the RebuildManagerForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void RebuildManagerForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Pass the cancellation token to the asynchronous operation.
                await PopulateDataAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore the exception if the task was cancelled intentionally.
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cancel any currently running background tasks.
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                // Release the FMOD System resources.
                lock (_fmodLock)
                {
                    if (_coreSystem.hasHandle())
                    {
                        _coreSystem.close();
                        _coreSystem.release();
                        _coreSystem.clearHandle();
                    }
                }

                // Dispose of the UI components.
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Initializes the structure and columns of the DataGridView.
        /// </summary>
        private void InitializeGrid()
        {
            dgvItems.Columns.Clear();
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "IDX", Width = 40, ReadOnly = true, Frozen = true });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Internal Name", Width = 150, ReadOnly = true, Frozen = true });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Original Info", Width = 130, ReadOnly = true, Frozen = true });
            var pathCol = new DataGridViewTextBoxColumn { HeaderText = "Replacement File (Path)", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            dgvItems.Columns.Add(pathCol);
            var btnFind = new DataGridViewButtonColumn { HeaderText = "Edit", Text = "...", UseColumnTextForButtonValue = true, Width = 40 };
            dgvItems.Columns.Add(btnFind);
            var stateCol = new DataGridViewTextBoxColumn { HeaderText = "State", Width = 60, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            dgvItems.Columns.Add(stateCol);
            var btnReset = new DataGridViewButtonColumn { HeaderText = "Reset", Text = "X", UseColumnTextForButtonValue = true, Width = 40, DefaultCellStyle = { ForeColor = Color.Red } };
            dgvItems.Columns.Add(btnReset);
        }

        /// <summary>
        /// Populates the encoding format ComboBox with available FMOD sound types.
        /// </summary>
        private void PopulateFormatComboBox()
        {
            cmbFormat.Items.Add(SOUND_TYPE.VORBIS);
            cmbFormat.Items.Add(SOUND_TYPE.FADPCM);
            cmbFormat.Items.Add(SOUND_TYPE.USER); // Represents PCM.
            cmbFormat.SelectedIndex = 0;
            cmbFormat.DrawMode = DrawMode.OwnerDrawFixed;
            cmbFormat.DrawItem += CmbFormat_DrawItem;
            cmbFormat.SelectedIndexChanged += CmbFormat_SelectedIndexChanged;

            // Trigger the event once to set the initial info text.
            CmbFormat_SelectedIndexChanged(null, null);
        }

        #endregion

        #region 4. Core Logic (Data & State Management)

        /// <summary>
        /// Asynchronously populates the DataGridView with the initial audio item data.
        /// </summary>
        /// <param name="token">Cancellation token to stop the operation if the form closes.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task PopulateDataAsync(CancellationToken token)
        {
            SetWorkingState(true, "Loading items...");
            dgvItems.Rows.Clear();

            // Pre-allocate rows to avoid performance issues with frequent additions.
            dgvItems.RowCount = _sourceList.Count;

            // Iterate through the source list and populate each row.
            for (int i = 0; i < _sourceList.Count; i++)
            {
                // Check for a cancellation request.
                if (token.IsCancellationRequested) break;

                var audio = _sourceList[i];
                DataGridViewRow row = dgvItems.Rows[i];

                row.Cells[COL_IDX].Value = audio.Index;
                row.Cells[COL_NAME].Value = audio.Name;

                // Display PCM instead of USER for clarity.
                string fmt = audio.Type == SOUND_TYPE.USER ? "PCM" : audio.Type.ToString();
                row.Cells[COL_INFO].Value = $"{fmt} / {audio.LengthMs}ms";
                row.Tag = audio;

                // Set the initial state of the row to "Original".
                await SetRowStateAsync(row, null);
            }
            SetWorkingState(false, "Ready.");
        }

        /// <summary>
        /// Checks the duration of a new audio file against the original duration.
        /// </summary>
        /// <param name="path">The file path of the new audio file.</param>
        /// <param name="originalMs">The duration of the original audio in milliseconds.</param>
        /// <returns>1 if longer, -1 if shorter, 0 if identical, -2 on error.</returns>
        private int CheckDuration(string path, uint originalMs)
        {
            if (!File.Exists(path)) return -2;

            Sound sound = new Sound();
            RESULT result;

            // Lock FMOD calls to ensure thread safety when called from background tasks.
            lock (_fmodLock)
            {
                // Try to create the sound using OPENONLY for efficiency.
                result = _coreSystem.createSound(path, MODE.OPENONLY, out sound);
                if (result != RESULT.OK)
                {
                    return -2;
                }

                uint newMs = 0;
                result = sound.getLength(out newMs, TIMEUNIT.MS);

                // Release the sound handle immediately after use.
                sound.release();

                if (result != RESULT.OK)
                {
                    return -2;
                }

                if (newMs > originalMs) return 1;
                if (newMs < originalMs) return -1;
                return 0;
            }
        }

        /// <summary>
        /// Asynchronously updates the visual state of a DataGridView row based on a new file path.
        /// </summary>
        /// <param name="row">The DataGridViewRow to update.</param>
        /// <param name="newPath">The path to the new replacement audio file, or null to reset to original.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SetRowStateAsync(DataGridViewRow row, string newPath)
        {
            // Handle the case where the row is being reset to its original state.
            if (string.IsNullOrEmpty(newPath))
            {
                row.Cells[COL_PATH].Value = "(Keep Original)";
                row.Cells[COL_PATH].Style.ForeColor = SystemColors.GrayText;
                row.Cells[COL_STATE].Value = "[ORG]";
                row.Cells[COL_STATE].Style.ForeColor = SystemColors.WindowText;
                row.DefaultCellStyle.BackColor = Color.Empty;
                row.Cells[COL_STATE].ToolTipText = "";
            }
            else
            {
                row.Cells[COL_PATH].Value = newPath;
                row.Cells[COL_PATH].Style.ForeColor = SystemColors.WindowText;

                // Retrieve the original audio metadata stored in the row's Tag.
                var originalInfo = (FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo)row.Tag;

                // Run the potentially slow file I/O and FMOD analysis on a background thread.
                int durationCheck = await Task.Run(() => CheckDuration(newPath, originalInfo.LengthMs));

                // Initialize the StringBuilder for the tooltip.
                StringBuilder tooltipBuilder = new StringBuilder();
                bool hasLoop = (originalInfo.Mode & MODE.LOOP_NORMAL) != 0 || originalInfo.LoopEnd > 0;

                // Update the row's appearance and tooltip based on the duration check result.
                if (durationCheck == 1) // LONGER
                {
                    row.Cells[COL_STATE].Value = "[LONG]";
                    row.Cells[COL_STATE].Style.ForeColor = Color.Red;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 230);
                    tooltipBuilder.AppendLine(MSG_LONG_BODY);
                }
                else if (durationCheck == -1) // SHORTER
                {
                    row.Cells[COL_STATE].Value = "[SHORT]";
                    row.Cells[COL_STATE].Style.ForeColor = Color.Blue;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
                    tooltipBuilder.AppendLine(MSG_SHORT_BODY);
                }
                else if (durationCheck == -2) // ERROR
                {
                    row.Cells[COL_STATE].Value = "[ERR]";
                    row.Cells[COL_STATE].Style.ForeColor = Color.Red;
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                    tooltipBuilder.AppendLine("Error: File not found or invalid format.");
                }
                else // OK
                {
                    row.Cells[COL_STATE].Value = "[NEW]";
                    row.Cells[COL_STATE].Style.ForeColor = Color.Green;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(235, 255, 235);
                }

                // Append a loop warning if the duration has changed for a looping sound.
                if (hasLoop && durationCheck != 0)
                {
                    if (tooltipBuilder.Length > 0) tooltipBuilder.AppendLine();
                    tooltipBuilder.AppendLine(MSG_LOOP_BODY);
                }

                row.Cells[COL_STATE].ToolTipText = tooltipBuilder.ToString();
            }
        }

        #endregion

        #region 5. UI Event Handlers

        /// <summary>
        /// Handles the CellContentClick event of the dgvItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance containing the event data.</param>
        private async void dgvItems_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Handle the click on the 'Find File' button column.
            if (e.ColumnIndex == COL_BTN_FIND)
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Audio Files|*.wav;*.ogg;*.mp3;*.flac" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetWorkingState(true, "Analyzing file...");
                        await SetRowStateAsync(dgvItems.Rows[e.RowIndex], ofd.FileName);
                        SetWorkingState(false, "Ready.");
                        UpdateWarningPanels();
                    }
                }
            }
            // Handle the click on the 'Reset' button column.
            else if (e.ColumnIndex == COL_BTN_RESET)
            {
                string state = dgvItems.Rows[e.RowIndex].Cells[COL_STATE].Value.ToString();
                if (state != "[ORG]")
                {
                    await SetRowStateAsync(dgvItems.Rows[e.RowIndex], null);
                    UpdateWarningPanels();
                }
            }
        }

        /// <summary>
        /// Handles the CellFormatting event of the dgvItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellFormattingEventArgs"/> instance containing the event data.</param>
        private void dgvItems_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Set a tooltip for the file path column to show the full path on hover.
            if (e.ColumnIndex == COL_PATH && e.Value != null)
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = e.Value.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnAutoMatch control.
        /// Performs a two-step search (Exact Match & Smart Match) and asks the user for confirmation.
        /// Updates the window title with progress percentage during scanning and applying.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void btnAutoMatch_Click(object sender, EventArgs e)
        {
            // 1. Initial confirmation to prevent accidental overwrite.
            string confirmTitle = "Confirm Auto-Match";
            string confirmMessage = "This will automatically search for replacement files in a selected folder.\n\n" +
                                    "It supports two modes:\n" +
                                    "1. Exact Match (e.g., Sound_1 -> Sound_1.wav)\n" +
                                    "2. Smart Match (e.g., Sound_1 -> Sound.wav)\n\n" +
                                    "No changes will be applied until you confirm the results.\n" +
                                    "Do you want to start scanning?";

            if (MessageBox.Show(confirmMessage, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
            {
                return;
            }

            using (var fbd = new FolderBrowserDialog { Description = "Select folder containing replacement audio files." })
            {
                if (fbd.ShowDialog() != DialogResult.OK) return;

                // Store the original form title to restore it later.
                string originalTitle = this.Text;
                SetWorkingState(true, "Scanning for matches...");

                string folder = fbd.SelectedPath;
                string[] exts = { ".wav", ".ogg", ".mp3", ".flac", ".aif", ".aiff", ".m4a" };

                // Collections to store potential matches before applying them.
                var exactMatches = new List<(DataGridViewRow Row, string Path)>();
                var smartMatches = new List<(DataGridViewRow Row, string Path)>();

                // Regex to identify filenames with numeric suffixes (e.g., "_1", "_02").
                Regex suffixRegex = new Regex(@"_(\d+)$", RegexOptions.Compiled);

                int totalRows = dgvItems.Rows.Count;

                // 2. Scan Phase: Iterate through rows to find potential matches without applying them yet.
                for (int i = 0; i < totalRows; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    DataGridViewRow row = dgvItems.Rows[i];
                    string internalName = row.Cells[COL_NAME].Value.ToString();
                    bool exactFound = false;

                    // Update the window title with the current scanning progress.
                    int percent = (int)((float)(i + 1) / totalRows * 100);
                    this.Text = $"{originalTitle} - Scanning... ({percent}%)";

                    // Step 2-A: Check for Exact Match.
                    foreach (var ext in exts)
                    {
                        string candidate = Path.Combine(folder, internalName + ext);
                        if (File.Exists(candidate))
                        {
                            exactMatches.Add((row, candidate));
                            exactFound = true;
                            break;
                        }
                    }

                    // Step 2-B: Check for Smart Match (only if Exact Match failed).
                    // Logic: If 'Sound_1' is missing, try finding 'Sound' (base name) to use as a fallback.
                    if (!exactFound && suffixRegex.IsMatch(internalName))
                    {
                        string baseName = suffixRegex.Replace(internalName, ""); // Remove suffix ("_1" -> "")
                        foreach (var ext in exts)
                        {
                            string baseCandidate = Path.Combine(folder, baseName + ext);
                            if (File.Exists(baseCandidate))
                            {
                                smartMatches.Add((row, baseCandidate));
                                break;
                            }
                        }
                    }

                    // Keep the UI responsive during the loop.
                    if (i % 20 == 0) Application.DoEvents();
                }

                SetWorkingState(false, "Ready.");

                // Restore the original title before showing the message box.
                this.Text = originalTitle;

                // 3. Decision Phase: Analyze results and ask the user how to proceed.
                int totalExact = exactMatches.Count;
                int totalSmart = smartMatches.Count;

                if (totalExact == 0 && totalSmart == 0)
                {
                    MessageBox.Show("No matching files found in the selected folder.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                List<(DataGridViewRow Row, string Path)> finalApplyList = new List<(DataGridViewRow Row, string Path)>();
                DialogResult result;

                // Build the result summary message.
                StringBuilder msg = new StringBuilder();
                msg.AppendLine("Scan Complete.");
                msg.AppendLine();
                msg.AppendLine($"• Exact Matches Found: {totalExact}");
                msg.AppendLine($"• Smart Matches Found: {totalSmart} (Suffix Ignored)");
                msg.AppendLine();

                if (totalSmart > 0)
                {
                    msg.AppendLine("Smart Match allows using a single file (e.g., 'Sound.wav') for multiple variations (e.g., 'Sound_1', 'Sound_2').");
                    msg.AppendLine();
                    msg.AppendLine("Do you want to apply ALL matches (including Smart Matches)?");
                    msg.AppendLine("- Yes: Apply Exact + Smart Matches");
                    msg.AppendLine("- No: Apply Exact Matches Only");
                    msg.AppendLine("- Cancel: Do nothing");

                    result = MessageBox.Show(msg.ToString(), "Select Match Strategy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel) return;

                    finalApplyList.AddRange(exactMatches);
                    if (result == DialogResult.Yes)
                    {
                        finalApplyList.AddRange(smartMatches);
                    }
                }
                else
                {
                    msg.AppendLine("Do you want to apply these exact matches?");
                    result = MessageBox.Show(msg.ToString(), "Confirm Match", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.No) return;

                    finalApplyList.AddRange(exactMatches);
                }

                // 4. Application Phase: Apply the selected files to the grid.
                if (finalApplyList.Count > 0)
                {
                    SetWorkingState(true, $"Applying {finalApplyList.Count} files...");

                    for (int i = 0; i < finalApplyList.Count; i++)
                    {
                        if (_cts.Token.IsCancellationRequested) break;

                        var item = finalApplyList[i];

                        // Update the window title with the current application progress.
                        // Since SetRowStateAsync involves I/O and FMOD analysis, progress feedback is useful here.
                        int percent = (int)((float)(i + 1) / finalApplyList.Count * 100);
                        this.Text = $"{originalTitle} - Applying... ({percent}%)";

                        await SetRowStateAsync(item.Row, item.Path);
                    }

                    // Final cleanup and UI update.
                    this.Text = originalTitle;
                    SetWorkingState(false, "Ready.");
                    UpdateWarningPanels();

                    MessageBox.Show($"Successfully applied {finalApplyList.Count} replacements.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnClearAll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void btnClearAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all replacements?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SetWorkingState(true, "Resetting all items...");
                foreach (DataGridViewRow row in dgvItems.Rows)
                {
                    // Check for a cancellation request.
                    if (_cts.Token.IsCancellationRequested) break;

                    await SetRowStateAsync(row, null);
                }
                SetWorkingState(false, "Ready.");
                UpdateWarningPanels();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnStart control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            ResultBatchItems.Clear();
            ResultOptions.EncodingFormat = (SOUND_TYPE)cmbFormat.SelectedItem;
            ResultOptions.Quality = 100;

            int longWarningCount = 0;
            int errorCount = 0;

            // Iterate through all rows to collect replacement items and count warnings or errors.
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                string state = row.Cells[COL_STATE].Value.ToString();
                if (state != "[ORG]")
                {
                    if (state == "[LONG]") longWarningCount++;
                    if (state == "[ERR]")
                    {
                        errorCount++;
                    }

                    ResultBatchItems.Add(new FSB_BANK_Extractor_Rebuilder_CS_GUI.BatchItem
                    {
                        TargetIndex = ((FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo)row.Tag).Index,
                        NewFilePath = row.Cells[COL_PATH].Value.ToString()
                    });
                }
            }

            // Prevent the build if any files have errors.
            if (errorCount > 0)
            {
                MessageBox.Show($"There are {errorCount} file(s) with errors (invalid or not found).\nPlease fix them before starting the build.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Warn the user if any replacement files are longer than the original.
            if (longWarningCount > 0)
            {
                string msg = $"{longWarningCount} items exceed original duration.\n\n{MSG_LONG_BODY}\n\nDo you want to continue anyway?";
                if (MessageBox.Show(msg, "Length Exceeded Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // Confirm with the user if no replacements are selected, as this will re-encode all original files.
            if (ResultBatchItems.Count == 0)
            {
                if (MessageBox.Show("No items selected for replacement.\nThis will rebuild the FSB with original files using the new encoding format. Proceed?", "Confirm Rebuild", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // Display a final backup warning before starting the irreversible build process.
            string backupTitle = "Final Confirmation: Backup Warning";
            string backupMessage = "Rebuilding the FSB file is an irreversible action. It is strongly recommended to back up the original file before proceeding.\n\nHave you backed up your original files and wish to start the build?";
            if (MessageBox.Show(backupMessage, backupTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                // Cancel the OK action if the user selects No.
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cmbFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CmbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFormat.SelectedItem == null) return;
            SOUND_TYPE type = (SOUND_TYPE)cmbFormat.SelectedItem;

            // Display informational text based on the selected encoding format.
            if (type == SOUND_TYPE.VORBIS)
                lblFormatInfo.Text = "Quality will be automatically adjusted to fit within the original file size limits. (Recommended)";
            else if (type == SOUND_TYPE.FADPCM)
                lblFormatInfo.Text = "Uses fixed-quality compression (FADPCM). Faster encoding, but may have lower quality than Vorbis.";
            else
                lblFormatInfo.Text = "Uncompressed PCM data. WARNING: File size will be very large. Use only if necessary.";
        }

        /// <summary>
        /// Handles the DrawItem event of the cmbFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DrawItemEventArgs"/> instance containing the event data.</param>
        private void CmbFormat_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                // Custom draw the item to display 'PCM' instead of the enum name 'USER'.
                SOUND_TYPE type = (SOUND_TYPE)cmbFormat.Items[e.Index];
                string text = type == SOUND_TYPE.USER ? "PCM" : type.ToString();
                using (Brush brush = new SolidBrush(e.ForeColor))
                {
                    e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
                }
            }
            e.DrawFocusRectangle();
        }

        #endregion

        #region 6. UI Helper Methods

        /// <summary>
        /// Updates the warning panels based on the current state of all items in the grid.
        /// </summary>
        private void UpdateWarningPanels()
        {
            int longCount = 0;
            int shortCount = 0;
            int loopWarningCount = 0;

            // Tally the number of items with duration or loop warnings.
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                string state = row.Cells[COL_STATE].Value.ToString();
                if (state == "[ORG]") continue;

                var originalInfo = (FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo)row.Tag;
                bool hasLoop = (originalInfo.Mode & MODE.LOOP_NORMAL) != 0 || originalInfo.LoopEnd > 0;

                if (state == "[LONG]") { longCount++; if (hasLoop) loopWarningCount++; }
                else if (state == "[SHORT]") { shortCount++; if (hasLoop) loopWarningCount++; }
            }

            // Update the 'Long Duration' warning panel.
            if (longCount > 0)
            {
                grpWarningLong.Text = $"Duration Warning ({longCount} items are longer)";
                grpWarningLong.ForeColor = Color.Red;
                lblWarningLongText.Text = MSG_LONG_BODY;
            }
            else
            {
                grpWarningLong.Text = "Duration Warning";
                grpWarningLong.ForeColor = Color.Green;
                lblWarningLongText.Text = OK_MSG_LONG;
            }

            // Update the 'Short Duration' warning panel.
            if (shortCount > 0)
            {
                grpWarningShort.Text = $"Duration Note ({shortCount} items are shorter)";
                grpWarningShort.ForeColor = Color.Blue;
                lblWarningShortText.Text = MSG_SHORT_BODY;
            }
            else
            {
                grpWarningShort.Text = "Duration Note";
                grpWarningShort.ForeColor = Color.Green;
                lblWarningShortText.Text = OK_MSG_SHORT;
            }

            // Update the 'Looping' warning panel.
            if (loopWarningCount > 0)
            {
                grpWarningLoop.Text = $"Looping Warning ({loopWarningCount} items)";
                grpWarningLoop.ForeColor = Color.DarkGoldenrod;
                lblWarningLoopText.Text = MSG_LOOP_BODY;
            }
            else
            {
                grpWarningLoop.Text = "Looping Status";
                grpWarningLoop.ForeColor = Color.Green;
                lblWarningLoopText.Text = OK_MSG_LOOP;
            }
        }

        /// <summary>
        /// Sets the UI to a working or idle state to provide user feedback during async operations.
        /// </summary>
        /// <param name="isWorking">A boolean indicating whether a process is running.</param>
        /// <param name="status">A status message to display (currently unused in this form).</param>
        private void SetWorkingState(bool isWorking, string status)
        {
            // If the form is disposed or disposing, do not attempt to update UI.
            if (this.IsDisposed || this.Disposing) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetWorkingState(isWorking, status)));
                return;
            }

            this.Cursor = isWorking ? Cursors.WaitCursor : Cursors.Default;
            dgvItems.Enabled = !isWorking;
            pnlBottom.Enabled = !isWorking;
        }

        #endregion
    }

    /// <summary>
    /// A simple class to hold the selected rebuild options.
    /// </summary>
    public class RebuildOptions
    {
        public SOUND_TYPE EncodingFormat { get; set; }
        public int Quality { get; set; }
    }
}