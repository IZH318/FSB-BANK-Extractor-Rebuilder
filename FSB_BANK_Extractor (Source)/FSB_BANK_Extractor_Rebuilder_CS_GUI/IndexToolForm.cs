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
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-10
 */

using System;
using System.Windows.Forms;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class IndexToolForm : Form
    {
        // Public properties to pass data back to the main form.
        public string InputString { get; private set; }
        public bool IsJumpMode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the IndexToolForm class.
        /// </summary>
        public IndexToolForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Load event of the IndexToolForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IndexToolForm_Load(object sender, EventArgs e)
        {
            // Set initial focus to the input text box.
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

            // Disable the 'Jump to Index' option if the input suggests a range (contains ',' or '-').
            bool isMultiRange = text.Contains(",") || text.Contains("-");

            if (isMultiRange)
            {
                if (rdoJump.Enabled)
                {
                    rdoJump.Enabled = false;
                    rdoSelect.Checked = true;
                }
            }
            else
            {
                if (!rdoJump.Enabled)
                {
                    rdoJump.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            // Validate that the input text box is not empty.
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                MessageBox.Show("Please enter a value.", "Empty Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Store the user's input and selected mode in public properties.
            InputString = txtInput.Text;
            IsJumpMode = rdoJump.Checked;

            // Set the dialog result to OK, which indicates successful completion to the main form.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}