/**
 * @file RebuildOptionsForm.cs
 * @brief Provides a modal dialog for configuring FMOD sound rebuild options.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form is presented to the user during the sound rebuilding process.
 * Its primary role is to gather the desired encoding format for the new audio data.
 * It separates the UI logic from the data by using the 'RebuildOptions' class.
 *
 * Key Features:
 *  - Displays original and replacement sound names for context.
 *  - Allows selection of the target FMOD encoding format (Vorbis, FADPCM, PCM).
 *  - Provides a user-friendly display for technical enum values (e.g., SOUND_TYPE.USER is shown as "PCM").
 *  - Passes the selected options back to the main form via a dedicated data class.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-09
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FMOD;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class RebuildOptionsForm : Form
    {
        public RebuildOptions Options { get; private set; }
        private readonly FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo _originalAudio;
        private readonly string _replacementAudioPath;

        /// <summary>
        /// Initializes a new instance of the RebuildOptionsForm class.
        /// </summary>
        /// <param name="originalAudio">The AudioInfo of the sound being replaced.</param>
        /// <param name="replacementAudioPath">The file path of the new replacement audio.</param>
        public RebuildOptionsForm(FSB_BANK_Extractor_Rebuilder_CS_GUI.AudioInfo originalAudio, string replacementAudioPath)
        {
            _originalAudio = originalAudio;
            _replacementAudioPath = replacementAudioPath;
            Options = new RebuildOptions();

            // This method initializes all controls on the form.
            InitializeComponent();

            PopulateControls();
        }

        /// <summary>
        /// Populates the form's controls with relevant data.
        /// </summary>
        private void PopulateControls()
        {
            // Display file names to provide context to the user.
            lblOriginalSoundName.Text = $"Original Sound: {_originalAudio.Name}";
            lblReplacementSoundName.Text = $"Replacement Sound: {Path.GetFileName(_replacementAudioPath)}";

            // Add the most relevant encoding format options to the ComboBox.
            comboFormat.Items.Add(SOUND_TYPE.VORBIS); // Most common and flexible choice.
            comboFormat.Items.Add(SOUND_TYPE.FADPCM); // A common, low-cost ADPCM format.
            comboFormat.Items.Add(SOUND_TYPE.USER);   // This enum value represents uncompressed PCM data.

            // Set up custom drawing for the ComboBox to improve user experience.
            // This allows displaying "PCM" instead of the less intuitive "USER".
            comboFormat.DrawMode = DrawMode.OwnerDrawFixed;
            comboFormat.DrawItem += (sender, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    SOUND_TYPE type = (SOUND_TYPE)comboFormat.Items[e.Index];
                    string text = type.ToString();
                    if (type == SOUND_TYPE.USER)
                    {
                        text = "PCM"; // Display SOUND_TYPE.USER as "PCM".
                    }
                    e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), e.Bounds);
                }
                e.DrawFocusRectangle();
            };

            // Set the default selection to VORBIS for user convenience.
            comboFormat.SelectedItem = SOUND_TYPE.VORBIS;
        }

        /// <summary>
        /// Handles the Click event of the btnRebuild control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnRebuild_Click(object sender, EventArgs e)
        {
            // Save the user's selection into the public Options object.
            Options.EncodingFormat = (SOUND_TYPE)comboFormat.SelectedItem;

            // Set a safe default quality value.
            // The actual quality for lossy formats is determined later by a binary search algorithm.
            Options.Quality = 100;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    /// <summary>
    /// Represents a data class to hold the user's selected rebuild options.
    /// </summary>
    public class RebuildOptions
    {
        public SOUND_TYPE EncodingFormat { get; set; }
        public int Quality { get; set; } // Represents quality from 1-100.
    }
}