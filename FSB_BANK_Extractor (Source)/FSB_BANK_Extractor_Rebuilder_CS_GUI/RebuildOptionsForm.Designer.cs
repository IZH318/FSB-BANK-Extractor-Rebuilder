/**
 * @file RebuildOptionsForm.Designer.cs
 * @brief Auto-generated code for the Rebuild Options form layout and component initialization.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file contains the automated setup for Windows Forms controls (ComboBox, GroupBox, etc.).
 * It is managed by the Visual Studio Form Designer.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-10
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class RebuildOptionsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RebuildOptionsForm));
            this.lblOriginalSoundName = new System.Windows.Forms.Label();
            this.lblReplacementSoundName = new System.Windows.Forms.Label();
            this.grpEncodingOptions = new System.Windows.Forms.GroupBox();
            this.comboFormat = new System.Windows.Forms.ComboBox();
            this.lblFormat = new System.Windows.Forms.Label();
            this.btnRebuild = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.grpEncodingOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblOriginalSoundName
            // 
            this.lblOriginalSoundName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOriginalSoundName.AutoEllipsis = true;
            this.lblOriginalSoundName.Location = new System.Drawing.Point(12, 13);
            this.lblOriginalSoundName.Name = "lblOriginalSoundName";
            this.lblOriginalSoundName.Size = new System.Drawing.Size(360, 18);
            this.lblOriginalSoundName.TabIndex = 0;
            this.lblOriginalSoundName.Text = "Original Sound: ...";
            // 
            // lblReplacementSoundName
            // 
            this.lblReplacementSoundName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblReplacementSoundName.AutoEllipsis = true;
            this.lblReplacementSoundName.Location = new System.Drawing.Point(12, 35);
            this.lblReplacementSoundName.Name = "lblReplacementSoundName";
            this.lblReplacementSoundName.Size = new System.Drawing.Size(360, 18);
            this.lblReplacementSoundName.TabIndex = 1;
            this.lblReplacementSoundName.Text = "Replacement Sound: ...";
            // 
            // grpEncodingOptions
            // 
            this.grpEncodingOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpEncodingOptions.Controls.Add(this.comboFormat);
            this.grpEncodingOptions.Controls.Add(this.lblFormat);
            this.grpEncodingOptions.Location = new System.Drawing.Point(14, 64);
            this.grpEncodingOptions.Name = "grpEncodingOptions";
            this.grpEncodingOptions.Size = new System.Drawing.Size(358, 59);
            this.grpEncodingOptions.TabIndex = 2;
            this.grpEncodingOptions.TabStop = false;
            this.grpEncodingOptions.Text = "Encoding Options";
            // 
            // comboFormat
            // 
            this.comboFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboFormat.FormattingEnabled = true;
            this.comboFormat.Location = new System.Drawing.Point(92, 23);
            this.comboFormat.Name = "comboFormat";
            this.comboFormat.Size = new System.Drawing.Size(121, 20);
            this.comboFormat.TabIndex = 2;
            // 
            // lblFormat
            // 
            this.lblFormat.AutoSize = true;
            this.lblFormat.Location = new System.Drawing.Point(17, 26);
            this.lblFormat.Name = "lblFormat";
            this.lblFormat.Size = new System.Drawing.Size(48, 12);
            this.lblFormat.TabIndex = 0;
            this.lblFormat.Text = "Format:";
            // 
            // btnRebuild
            // 
            this.btnRebuild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRebuild.Location = new System.Drawing.Point(176, 179);
            this.btnRebuild.Name = "btnRebuild";
            this.btnRebuild.Size = new System.Drawing.Size(95, 30);
            this.btnRebuild.TabIndex = 3;
            this.btnRebuild.Text = "Rebuild";
            this.btnRebuild.UseVisualStyleBackColor = true;
            this.btnRebuild.Click += new System.EventHandler(this.btnRebuild_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(277, 179);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(95, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInfo.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblInfo.Location = new System.Drawing.Point(12, 136);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(360, 40);
            this.lblInfo.TabIndex = 5;
            this.lblInfo.Text = "The tool will automatically find the best possible quality that fits within the o" +
    "riginal sound\'s file size.";
            // 
            // RebuildOptionsForm
            // 
            this.AcceptButton = this.btnRebuild;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 221);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRebuild);
            this.Controls.Add(this.grpEncodingOptions);
            this.Controls.Add(this.lblReplacementSoundName);
            this.Controls.Add(this.lblOriginalSoundName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 260);
            this.Name = "RebuildOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Rebuild Sound Options";
            this.grpEncodingOptions.ResumeLayout(false);
            this.grpEncodingOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblOriginalSoundName;
        private System.Windows.Forms.Label lblReplacementSoundName;
        private System.Windows.Forms.GroupBox grpEncodingOptions;
        private System.Windows.Forms.ComboBox comboFormat;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.Button btnRebuild;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblInfo;
    }
}