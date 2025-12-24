/**
 * @file IndexToolForm.cs
 * @brief Provides a dialog for users to jump to or select audio files by their index.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form allows users to input a single index number to jump to a specific sub-sound
 * or a range/list of indices (e.g., "10-20, 35") to select multiple items at once.
 * The form's logic dynamically adjusts UI options based on the input format.
 *
 * Key Features:
 *  - Single Index Input: Supports numeric input for "Jump to Index" functionality.
 *  - Range and List Input: Supports range ("5-10") and list ("15, 20") formats for "Select Indices" functionality.
 *  - Dynamic UI Logic: Automatically disables the "Jump" option when a multi-selection format is detected.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
 */

using System;
using System.Windows.Forms;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Represents a dialog form that allows users to select audio files by index.
    /// </summary>
    public partial class IndexToolForm : Form
    {
        #region 1. Constants

        // Validation messages displayed to the user.
        private const string MSG_EMPTY_INPUT_CAPTION = "Empty Input";
        private const string MSG_EMPTY_INPUT_TEXT = "Please enter a value.";

        // Input format separators used to detect ranges or lists.
        private const string SEPARATOR_COMMA = ",";
        private const string SEPARATOR_RANGE = "-";

        #endregion

        #region 2. Public Properties

        /// <summary>
        /// Gets the user-provided input string from the text box.
        /// </summary>
        /// <value>
        /// The string containing the index or range of indices to be processed.
        /// </value>
        public string InputString { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user selected the "Jump to Index" mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if the jump mode is selected; otherwise, <c>false</c> if select mode is chosen.
        /// </value>
        public bool IsJumpMode { get; private set; }

        #endregion

        #region 3. Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexToolForm"/> class.
        /// </summary>
        public IndexToolForm()
        {
            InitializeComponent();
        }

        #endregion

        #region 4. UI Event Handlers

        /// <summary>
        /// Handles the Load event of the IndexToolForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IndexToolForm_Load(object sender, EventArgs e)
        {
            // Set the initial focus to the input text box to allow immediate typing.
            txtInput.Focus();
        }

        /// <summary>
        /// Handles the TextChanged event of the txtInput control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void txtInput_TextChanged(object sender, EventArgs e)
        {
            string text = txtInput.Text;

            // Determine if the input represents a multi-selection or range (e.g., contains "," or "-").
            bool isMultiRange = text.Contains(SEPARATOR_COMMA) || text.Contains(SEPARATOR_RANGE);

            // Disable the 'Jump to Index' option if the input format suggests a range or list,
            // as jumping is strictly for single numeric indices.
            rdoJump.Enabled = !isMultiRange;

            // Automatically switch to the 'Select' mode if the input requires it,
            // ensuring the UI state matches the user's intent.
            if (isMultiRange)
            {
                rdoSelect.Checked = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            // Validate the user input to ensure a value is provided before processing.
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                MessageBox.Show(MSG_EMPTY_INPUT_TEXT, MSG_EMPTY_INPUT_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Capture the valid input and the selected operation mode for the parent form.
            InputString = txtInput.Text;
            IsJumpMode = rdoJump.Checked;

            // Signal successful completion to the caller and close the dialog.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion
    }
}