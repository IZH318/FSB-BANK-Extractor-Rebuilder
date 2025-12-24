/**
 * @file RebuildManagerForm.cs
 * @brief Provides a modal dialog for managing single or batch audio file replacements before an FSB rebuild.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form serves as the primary user interface for configuring audio replacements. It allows users to
 * specify new audio files for one or more sub-sounds within an FSB container. The form validates the
 * duration of replacement files against the originals, provides clear warnings about potential issues
 * (e.g., mismatched lengths, looping problems), and offers an auto-match feature to streamline the
 * replacement process. All file I/O and analysis operations are performed asynchronously to maintain
 * UI responsiveness.
 *
 * Key Features:
 *  - Batch & Single Item Replacement: Manages one or more audio file replacements in a DataGridView.
 *  - Asynchronous Validation: Checks the duration of replacement audio against the original without blocking the UI.
 *  - Auto-Match Functionality: Scans a user-selected folder to automatically find and suggest replacement files.
 *  - User-Friendly Warnings: Provides clear, color-coded feedback about potential duration and looping issues.
 *  - Format Configuration: Allows the user to select the target encoding format for the rebuilt FSB.
 *
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
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
using FMOD; // Core API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class RebuildManagerForm : Form
    {
        #region 1. Public Properties

        /// <summary>
        /// Gets the list of items configured for replacement by the user.
        /// </summary>
        public List<BatchItem> ResultBatchItems { get; private set; }

        /// <summary>
        /// Gets the rebuild options (e.g., encoding format) selected by the user.
        /// </summary>
        public RebuildOptions ResultOptions { get; private set; }

        #endregion

        #region 2. Private Fields & Constants

        // Column Names (Constants to prevent magic strings)
        private const string COL_IDX = "colIdx";
        private const string COL_INTERNAL_NAME = "colInternalName";
        private const string COL_ORIGINAL_INFO = "colOriginalInfo";
        private const string COL_REPLACEMENT_PATH = "colReplacementPath";
        private const string COL_BTN_FIND = "colBtnFind";
        private const string COL_STATE = "colState";
        private const string COL_BTN_RESET = "colBtnReset";

        // UI Colors & Visual States
        private static readonly Color ColorBgLong = Color.FromArgb(255, 240, 230);
        private static readonly Color ColorBgShort = Color.FromArgb(240, 245, 255);
        private static readonly Color ColorBgError = Color.MistyRose;
        private static readonly Color ColorBgNew = Color.FromArgb(235, 255, 235);

        private static readonly Color ColorTextLong = Color.Red;
        private static readonly Color ColorTextShort = Color.Blue;
        private static readonly Color ColorTextNew = Color.Green;
        private static readonly Color ColorTextError = Color.Red;
        private static readonly Color ColorTextWarningLoop = Color.DarkGoldenrod;

        // Messages & Extensions
        private const string MSG_LONG_BODY = "Proceeding may cause unexpected behavior with game event timelines or looping, as these often rely on the original audio's duration. Stability is not guaranteed.";
        private const string MSG_SHORT_BODY = "NOTE: The new audio is SHORTER. The remaining time might be filled with silence, which could affect looping behavior.";
        private const string MSG_LOOP_BODY = "This sound has loop points based on the original timeline.\nThe loop may behave unexpectedly if the new audio's length is different.";

        private const string OK_MSG_LONG = "OK: No items exceed their original duration.";
        private const string OK_MSG_SHORT = "OK: No items are shorter than their original.";
        private const string OK_MSG_LOOP = "OK: No potential looping issues detected based on duration changes.";

        private const string MSG_AUTO_MATCH_CONFIRM =
            "This will automatically search for replacement files in a selected folder.\n\n" +
            "It supports two modes:\n" +
            "1. Exact Match (e.g., Sound_1 -> Sound_1.wav)\n" +
            "2. Smart Match (e.g., Sound_1 -> Sound.wav)\n\n" +
            "No changes will be applied until you confirm the results.\n" +
            "Do you want to start scanning?";

        private const string MSG_BACKUP_WARNING =
            "Rebuilding the FSB file is an irreversible action. It is strongly recommended to back up the original file before proceeding.\n\n" +
            "Have you backed up your original files and wish to start the build?";

        // Supported audio extensions for auto-matching.
        private static readonly string[] SupportedExtensions = { ".wav", ".ogg", ".mp3", ".flac", ".aif", ".aiff", ".m4a" };

        // Regex to identify numbered suffixes (e.g., "_01") for smart matching.
        private const string REGEX_SUFFIX_PATTERN = @"_(\d+)$";

        // Core Logic Fields
        /// <summary>
        /// A list of the original audio information for the items being managed.
        /// </summary>
        private readonly List<AudioInfo> _sourceList;

        /// <summary>
        /// The name of the parent container (e.g., .bank or .fsb file).
        /// </summary>
        private readonly string _containerName;

        /// <summary>
        /// A reference to the shared FMOD Core System instance.
        /// </summary>
        private FMOD.System _coreSystem;

        /// <summary>
        /// A lock object to synchronize access to the shared FMOD Core System.
        /// </summary>
        private readonly object _fmodLock;

        /// <summary>
        /// A token source to signal cancellation for asynchronous operations.
        /// </summary>
        private CancellationTokenSource _cts;

        #endregion

        #region 3. Initialization & Cleanup

        /// <summary>
        /// Initializes a new instance of the <see cref="RebuildManagerForm"/> class.
        /// </summary>
        /// <param name="containerName">The display name of the container being rebuilt.</param>
        /// <param name="audioItems">A list of <see cref="AudioInfo"/> objects representing the original sub-sounds.</param>
        /// <param name="sharedCoreSystem">The shared FMOD Core System instance for audio analysis.</param>
        /// <param name="syncLock">The lock object for synchronizing FMOD API calls.</param>
        public RebuildManagerForm(string containerName, List<AudioInfo> audioItems, FMOD.System sharedCoreSystem, object syncLock)
        {
            _containerName = containerName;
            _sourceList = audioItems;
            ResultOptions = new RebuildOptions();
            ResultBatchItems = new List<BatchItem>();

            _cts = new CancellationTokenSource();
            _coreSystem = sharedCoreSystem;
            _fmodLock = syncLock;

            InitializeComponent();

            this.Load += RebuildManagerForm_Load;

            this.Text = "Rebuild Manager";
            lblTarget.Text = $"Target: {_containerName}";

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
                // Asynchronously populate the grid with audio data when the form loads.
                await PopulateDataAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // This exception is expected if the form is closed while loading, so it can be safely ignored.
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
                // Signal cancellation to any running asynchronous tasks.
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                // Explicitly clear DataGridView resources to prevent memory leaks.
                // The DataGridView maintains strong references to its rows, preventing Garbage Collection
                // even after the form is closed. Clearing them manually resolves the RAM accumulation issue.
                if (dgvItems != null)
                {
                    dgvItems.DataSource = null;
                    dgvItems.Rows.Clear();
                    dgvItems.Columns.Clear();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Configures the structure and properties of the DataGridView control.
        /// </summary>
        private void InitializeGrid()
        {
            dgvItems.Columns.Clear();

            dgvItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_IDX,
                HeaderText = "IDX",
                Width = 40,
                ReadOnly = true,
                Frozen = true
            });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_INTERNAL_NAME,
                HeaderText = "Internal Name",
                Width = 150,
                ReadOnly = true,
                Frozen = true
            });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_ORIGINAL_INFO,
                HeaderText = "Original Info",
                Width = 130,
                ReadOnly = true,
                Frozen = true
            });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_REPLACEMENT_PATH,
                HeaderText = "Replacement File (Path)",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvItems.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_BTN_FIND,
                HeaderText = "Edit",
                Text = "...",
                UseColumnTextForButtonValue = true,
                Width = 40
            });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_STATE,
                HeaderText = "State",
                Width = 60,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvItems.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_BTN_RESET,
                HeaderText = "Reset",
                Text = "X",
                UseColumnTextForButtonValue = true,
                Width = 40,
                DefaultCellStyle = { ForeColor = Color.Red }
            });
        }

        /// <summary>
        /// Populates the format selection ComboBox with supported FMOD sound types.
        /// </summary>
        private void PopulateFormatComboBox()
        {
            cmbFormat.Items.Add(SOUND_TYPE.VORBIS);
            cmbFormat.Items.Add(SOUND_TYPE.FADPCM);
            cmbFormat.Items.Add(SOUND_TYPE.USER); // Represents uncompressed PCM.
            cmbFormat.SelectedIndex = 0;

            // Enable custom drawing to display "PCM" instead of "USER".
            cmbFormat.DrawMode = DrawMode.OwnerDrawFixed;
            cmbFormat.DrawItem += CmbFormat_DrawItem;
            cmbFormat.SelectedIndexChanged += CmbFormat_SelectedIndexChanged;

            // Trigger the event handler to set the initial info label text.
            CmbFormat_SelectedIndexChanged(null, null);
        }

        #endregion

        #region 4. Core Logic & State Management

        /// <summary>
        /// Asynchronously populates the DataGridView with the initial list of audio items.
        /// </summary>
        /// <param name="token">A cancellation token to observe while performing the operation.</param>
        private async Task PopulateDataAsync(CancellationToken token)
        {
            SetWorkingState(true, "[LOADING] Loading items...");
            dgvItems.Rows.Clear();
            if (_sourceList.Count > 0)
            {
                dgvItems.RowCount = _sourceList.Count;
            }

            // Build the grid rows based on the source audio list.
            // The initial state is set to 'Original' until the user supplies a replacement.
            for (int i = 0; i < _sourceList.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var audio = _sourceList[i];
                DataGridViewRow row = dgvItems.Rows[i];
                row.Cells[COL_IDX].Value = audio.Index;
                row.Cells[COL_INTERNAL_NAME].Value = audio.Name;

                // Display "PCM" for USER type for better readability.
                string fmt = audio.Type == SOUND_TYPE.USER ? "PCM" : audio.Type.ToString();
                row.Cells[COL_ORIGINAL_INFO].Value = $"{fmt} / {audio.LengthMs}ms";
                row.Tag = audio;

                // Set the initial state of the row to "Original".
                await SetRowStateAsync(row, null);
            }

            SetWorkingState(false, "[READY] Waiting for user input.");
        }

        /// <summary>
        /// Checks the duration of a specified audio file against an original duration.
        /// </summary>
        /// <param name="path">The path to the audio file to check.</param>
        /// <param name="originalMs">The original duration in milliseconds to compare against.</param>
        /// <returns>
        /// Returns 1 if the new audio is longer, -1 if shorter, 0 if the same length, and -2 on error (file not found or invalid format).
        /// </returns>
        private int CheckDuration(string path, uint originalMs)
        {
            if (!File.Exists(path))
            {
                return -2;
            }

            Sound sound = new Sound();
            RESULT result;

            // Lock the FMOD system to ensure thread-safe API calls.
            lock (_fmodLock)
            {
                // Create a sound object just to read its metadata without decoding the entire file.
                // Using MODE.OPENONLY is crucial for performance when only querying info.
                result = _coreSystem.createSound(path, MODE.OPENONLY, out sound);
                if (result != RESULT.OK)
                {
                    return -2;
                }

                uint newMs = 0;
                result = sound.getLength(out newMs, TIMEUNIT.MS);

                // Always release the FMOD handle after use to prevent memory leaks.
                sound.release();

                if (result != RESULT.OK)
                {
                    return -2;
                }

                if (newMs > originalMs)
                {
                    return 1;
                }

                if (newMs < originalMs)
                {
                    return -1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Asynchronously updates the state and visual appearance of a DataGridView row based on a replacement file.
        /// </summary>
        /// <param name="row">The <see cref="DataGridViewRow"/> to update.</param>
        /// <param name="newPath">The path to the new replacement file, or null to reset to original.</param>
        private async Task SetRowStateAsync(DataGridViewRow row, string newPath)
        {
            // Handle the case where the replacement is reset to the original.
            if (string.IsNullOrEmpty(newPath))
            {
                row.Cells[COL_REPLACEMENT_PATH].Value = "(Keep Original)";
                row.Cells[COL_REPLACEMENT_PATH].Style.ForeColor = SystemColors.GrayText;
                row.Cells[COL_STATE].Value = "[ORG]";
                row.Cells[COL_STATE].Style.ForeColor = SystemColors.WindowText;
                row.DefaultCellStyle.BackColor = Color.Empty;
                row.Cells[COL_STATE].ToolTipText = "";
            }
            // Handle the case where a new replacement file is specified.
            else
            {
                row.Cells[COL_REPLACEMENT_PATH].Value = newPath;
                row.Cells[COL_REPLACEMENT_PATH].Style.ForeColor = SystemColors.WindowText;

                var originalInfo = (AudioInfo)row.Tag;

                // Asynchronously check the duration of the new file against the original.
                int durationCheck = await Task.Run(() => CheckDuration(newPath, originalInfo.LengthMs));

                StringBuilder tooltipBuilder = new StringBuilder();
                bool hasLoop = (originalInfo.Mode & MODE.LOOP_NORMAL) != 0 || originalInfo.LoopEnd > 0;

                // Update the row's appearance and tooltip based on the duration check result.
                if (durationCheck == 1)
                {
                    row.Cells[COL_STATE].Value = "[LONG]";
                    row.Cells[COL_STATE].Style.ForeColor = ColorTextLong;
                    row.DefaultCellStyle.BackColor = ColorBgLong;
                    tooltipBuilder.AppendLine(MSG_LONG_BODY);
                }
                else if (durationCheck == -1)
                {
                    row.Cells[COL_STATE].Value = "[SHORT]";
                    row.Cells[COL_STATE].Style.ForeColor = ColorTextShort;
                    row.DefaultCellStyle.BackColor = ColorBgShort;
                    tooltipBuilder.AppendLine(MSG_SHORT_BODY);
                }
                else if (durationCheck == -2)
                {
                    row.Cells[COL_STATE].Value = "[ERR]";
                    row.Cells[COL_STATE].Style.ForeColor = ColorTextError;
                    row.DefaultCellStyle.BackColor = ColorBgError;
                    tooltipBuilder.AppendLine("Error: File not found or invalid audio format.");
                }
                else
                {
                    row.Cells[COL_STATE].Value = "[NEW]";
                    row.Cells[COL_STATE].Style.ForeColor = ColorTextNew;
                    row.DefaultCellStyle.BackColor = ColorBgNew;
                }

                // Append a specific warning if the audio has loop points and the duration has changed.
                if (hasLoop && durationCheck != 0)
                {
                    if (tooltipBuilder.Length > 0)
                    {
                        tooltipBuilder.AppendLine();
                    }
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
            // Ensure the click is on a valid row.
            if (e.RowIndex < 0)
            {
                return;
            }

            string colName = dgvItems.Columns[e.ColumnIndex].Name;

            // Handle the click on the "Find" button to select a replacement file.
            if (colName == COL_BTN_FIND)
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Audio Files|*.wav;*.ogg;*.mp3;*.flac" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetWorkingState(true, "[ANALYZING] Processing selected file...");
                        await SetRowStateAsync(dgvItems.Rows[e.RowIndex], ofd.FileName);
                        SetWorkingState(false, "[READY] Waiting for user input.");
                        UpdateWarningPanels();
                    }
                }
            }
            // Handle the click on the "Reset" button to clear a replacement.
            else if (colName == COL_BTN_RESET)
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
            // Set a tooltip for the replacement path column to show the full path on hover.
            if (dgvItems.Columns[e.ColumnIndex].Name == COL_REPLACEMENT_PATH && e.Value != null)
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = e.Value.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnAutoMatch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Execution Flow:
        /// 1) Confirm the scan intent with the user.
        /// 2) Select the source directory via a FolderBrowserDialog.
        /// 3) Scan the folder asynchronously for exact and smart matches.
        /// 4) Present the results and determine the application strategy.
        /// 5) Apply the selected matches to the grid items.
        /// </remarks>
        private async void btnAutoMatch_Click(object sender, EventArgs e)
        {
            // Step 1: Confirm the scan intent with the user.
            // This prevents accidental starts of the potentially long-running scan process.
            if (MessageBox.Show(MSG_AUTO_MATCH_CONFIRM, "Confirm Auto-Match", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
            {
                return;
            }

            // Step 2: Select the source directory via a FolderBrowserDialog.
            using (var fbd = new FolderBrowserDialog { Description = "Select folder containing replacement audio files." })
            {
                if (fbd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string originalTitle = this.Text;
                SetWorkingState(true, "[SCANNING] Searching for matching files...");

                try
                {
                    // Step 3: Scan the folder asynchronously for exact and smart matches.
                    // Progress is reported to the window title to keep the UI responsive.
                    var progress = new Progress<int>(percent =>
                    {
                        this.Text = $"{originalTitle} - [SCANNING] {percent}%";
                    });

                    var (exactMatches, smartMatches) = await ScanAndMatchFilesAsync(fbd.SelectedPath, progress, _cts.Token);

                    // Step 4: Present the results and determine the application strategy.
                    // If no matches are found, inform the user and exit early.
                    if (exactMatches.Count == 0 && smartMatches.Count == 0)
                    {
                        MessageBox.Show("No matching files found in the selected folder.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var finalApplyList = new List<(DataGridViewRow Row, string Path)>();
                    StringBuilder msg = new StringBuilder("Scan Complete.\n\n");
                    msg.AppendLine($"• Exact Matches Found: {exactMatches.Count}");
                    msg.AppendLine($"• Smart Matches Found: {smartMatches.Count} (Suffix Ignored)\n");

                    DialogResult result;

                    // Differentiate between simple exact matches and mixed matches requiring user decision.
                    if (smartMatches.Count > 0)
                    {
                        msg.AppendLine("Smart Match allows using a single file (e.g., 'Sound.wav') for multiple variations (e.g., 'Sound_1', 'Sound_2').\n");
                        msg.AppendLine("Do you want to apply ALL matches (including Smart Matches)?");
                        msg.AppendLine("- Yes: Apply Exact + Smart Matches");
                        msg.AppendLine("- No: Apply Exact Matches Only");
                        msg.AppendLine("- Cancel: Do nothing");
                        result = MessageBox.Show(msg.ToString(), "Select Match Strategy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        if (result == DialogResult.Cancel)
                        {
                            return;
                        }

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

                        if (result == DialogResult.No)
                        {
                            return;
                        }
                        finalApplyList.AddRange(exactMatches);
                    }

                    // Step 5: Apply the selected matches to the grid items.
                    if (finalApplyList.Count > 0)
                    {
                        SetWorkingState(true, $"[APPLYING] Updating {finalApplyList.Count} items...");

                        for (int i = 0; i < finalApplyList.Count; i++)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            var item = finalApplyList[i];
                            this.Text = $"{originalTitle} - [APPLYING] {(int)((float)(i + 1) / finalApplyList.Count * 100)}%";
                            await SetRowStateAsync(item.Row, item.Path);
                        }

                        UpdateWarningPanels();
                        MessageBox.Show($"Successfully applied {finalApplyList.Count} replacements.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (OperationCanceledException)
                {
                    // This exception is expected if the form is closed during the operation. Silently exit.
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during auto-match: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetWorkingState(false, "[READY] Waiting for user input.");
                    this.Text = originalTitle;
                }
            }
        }

        /// <summary>
        /// Asynchronously scans a folder for audio files that match the names of items in the grid.
        /// </summary>
        /// <param name="folder">The directory path to scan.</param>
        /// <param name="progress">An object to report progress back to the UI.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A tuple containing lists of exact and smart matches found.</returns>
        private Task<(List<(DataGridViewRow Row, string Path)> exactMatches, List<(DataGridViewRow Row, string Path)> smartMatches)> ScanAndMatchFilesAsync(string folder, IProgress<int> progress, CancellationToken token)
        {
            // Run the file-intensive scan on a background thread to avoid blocking the UI.
            return Task.Run(() =>
            {
                var exactMatches = new List<(DataGridViewRow Row, string Path)>();
                var smartMatches = new List<(DataGridViewRow Row, string Path)>();
                Regex suffixRegex = new Regex(REGEX_SUFFIX_PATTERN, RegexOptions.Compiled);

                for (int i = 0; i < dgvItems.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    DataGridViewRow row = dgvItems.Rows[i];
                    string internalName = row.Cells[COL_INTERNAL_NAME].Value.ToString();
                    bool exactFound = false;

                    // Report progress periodically to update the UI.
                    if (i % 20 == 0 || i == dgvItems.Rows.Count - 1)
                    {
                        int percent = (int)(((float)i + 1) / dgvItems.Rows.Count * 100);
                        progress.Report(percent);
                    }

                    // Check for an exact name match (e.g., "Sound_01.wav").
                    foreach (var ext in SupportedExtensions)
                    {
                        string candidate = Path.Combine(folder, internalName + ext);
                        if (File.Exists(candidate))
                        {
                            exactMatches.Add((row, candidate));
                            exactFound = true;
                            break;
                        }
                    }

                    // If no exact match, check for a smart match (e.g., "Sound_01" matches "Sound.wav").
                    if (!exactFound && suffixRegex.IsMatch(internalName))
                    {
                        string baseName = suffixRegex.Replace(internalName, "");
                        foreach (var ext in SupportedExtensions)
                        {
                            string baseCandidate = Path.Combine(folder, baseName + ext);
                            if (File.Exists(baseCandidate))
                            {
                                smartMatches.Add((row, baseCandidate));
                                break;
                            }
                        }
                    }
                }

                return (exactMatches, smartMatches);
            }, token);
        }

        /// <summary>
        /// Handles the Click event of the btnClearAll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void btnClearAll_Click(object sender, EventArgs e)
        {
            // Confirm the reset action with the user.
            if (MessageBox.Show("Are you sure you want to reset all replacements?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SetWorkingState(true, "[RESETTING] Clearing all replacements...");
                foreach (DataGridViewRow row in dgvItems.Rows)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                    await SetRowStateAsync(row, null);
                }
                SetWorkingState(false, "[READY] Waiting for user input.");
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
            ResultOptions.Quality = 100; // Quality is determined by the binary search later.
            int longWarningCount = 0;
            int errorCount = 0;

            // Collect all modified items from the grid and perform validation.
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                string state = row.Cells[COL_STATE].Value.ToString();
                if (state != "[ORG]")
                {
                    if (state == "[LONG]")
                    {
                        longWarningCount++;
                    }

                    if (state == "[ERR]")
                    {
                        errorCount++;
                    }
                    // Add the item to the batch list if it has been modified.
                    ResultBatchItems.Add(new BatchItem
                    {
                        TargetIndex = ((AudioInfo)row.Tag).Index,
                        NewFilePath = row.Cells[COL_REPLACEMENT_PATH].Value.ToString()
                    });
                }
            }

            // Prevent the build from starting if there are validation errors.
            if (errorCount > 0)
            {
                MessageBox.Show($"There are {errorCount} file(s) with errors (invalid or not found).\nPlease fix them before starting the build.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Show a final warning if any replacement files are longer than the originals.
            if (longWarningCount > 0)
            {
                string msg = $"{longWarningCount} items exceed original duration.\n\n{MSG_LONG_BODY}\n\nDo you want to continue anyway?";
                if (MessageBox.Show(msg, "Length Exceeded Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // Confirm with the user if no replacements are selected, as this will only re-encode the original files.
            if (ResultBatchItems.Count == 0)
            {
                if (MessageBox.Show("No items selected for replacement.\n\nThis will rebuild the FSB with original files using the new encoding format. Proceed?", "Confirm Rebuild", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // Provide a final backup reminder to the user before proceeding with the irreversible build process.
            if (MessageBox.Show(MSG_BACKUP_WARNING, "Final Confirmation: Backup Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
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
            if (cmbFormat.SelectedItem == null)
            {
                return;
            }

            SOUND_TYPE type = (SOUND_TYPE)cmbFormat.SelectedItem;

            if (type == SOUND_TYPE.VORBIS)
            {
                lblFormatInfo.Text = "Quality will be automatically adjusted to fit within the original file size limits. (Recommended)";
            }
            else if (type == SOUND_TYPE.FADPCM)
            {
                lblFormatInfo.Text = "Uses fixed-quality compression (FADPCM). Faster encoding, but may have lower quality than Vorbis.";
            }
            else
            {
                lblFormatInfo.Text = "Uncompressed PCM data. WARNING: File size will be very large. Use only if necessary.";
            }
        }

        /// <summary>
        /// Handles the DrawItem event of the cmbFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DrawItemEventArgs"/> instance containing the event data.</param>
        private void CmbFormat_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Custom drawing is required to display "PCM" instead of the internal FMOD enum name "USER".
            e.DrawBackground();
            if (e.Index >= 0)
            {
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
        /// Updates the warning panels at the bottom of the form based on the current state of the grid.
        /// </summary>
        private void UpdateWarningPanels()
        {
            int longCount = 0, shortCount = 0, loopWarningCount = 0;
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                if (row.Cells[COL_STATE].Value == null)
                {
                    continue;
                }

                string state = row.Cells[COL_STATE].Value.ToString();
                if (state == "[ORG]")
                {
                    continue;
                }

                var originalInfo = (AudioInfo)row.Tag;
                bool hasLoop = (originalInfo.Mode & MODE.LOOP_NORMAL) != 0 || originalInfo.LoopEnd > 0;

                if (state == "[LONG]")
                {
                    longCount++;
                    if (hasLoop)
                    {
                        loopWarningCount++;
                    }
                }
                else if (state == "[SHORT]")
                {
                    shortCount++;
                    if (hasLoop)
                    {
                        loopWarningCount++;
                    }
                }
            }

            // Update the "Long Duration" warning panel.
            grpWarningLong.Text = longCount > 0 ? $"Duration Warning ({longCount} items are longer)" : "Duration Warning";
            grpWarningLong.ForeColor = longCount > 0 ? ColorTextLong : ColorTextNew;
            lblWarningLongText.Text = longCount > 0 ? MSG_LONG_BODY : OK_MSG_LONG;

            // Update the "Short Duration" note panel.
            grpWarningShort.Text = shortCount > 0 ? $"Duration Note ({shortCount} items are shorter)" : "Duration Note";
            grpWarningShort.ForeColor = shortCount > 0 ? ColorTextShort : ColorTextNew;
            lblWarningShortText.Text = shortCount > 0 ? MSG_SHORT_BODY : OK_MSG_SHORT;

            // Update the "Looping" status panel.
            grpWarningLoop.Text = loopWarningCount > 0 ? $"Looping Warning ({loopWarningCount} items)" : "Looping Status";
            grpWarningLoop.ForeColor = loopWarningCount > 0 ? ColorTextWarningLoop : ColorTextNew;
            lblWarningLoopText.Text = loopWarningCount > 0 ? MSG_LOOP_BODY : OK_MSG_LOOP;
        }

        /// <summary>
        /// Sets the UI to a working or idle state to provide feedback and prevent user interaction during long operations.
        /// </summary>
        /// <param name="isWorking">A value indicating whether to enter the working state.</param>
        /// <param name="status">The status message to display.</param>
        private void SetWorkingState(bool isWorking, string status)
        {
            // Ensure UI updates are performed on the UI thread.
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetWorkingState(isWorking, status)));
                return;
            }

            this.Cursor = isWorking ? Cursors.WaitCursor : Cursors.Default;
            dgvItems.Enabled = !isWorking;
            pnlBottom.Enabled = !isWorking;
            pnlTop.Enabled = !isWorking; // Also disable top panel during operations.
        }

        #endregion
    }
}