/**
 * @file IndexToolForm.Designer.cs
 * @brief Auto-generated code for the Index Tool form layout and component initialization.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file contains the automated setup for Windows Forms controls (TextBox, RadioButton, etc.).
 * It is managed by the Visual Studio Form Designer.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-12
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class IndexToolForm
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
            this.lblGuide = new System.Windows.Forms.Label();
            this.txtInput = new System.Windows.Forms.TextBox();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.rdoSelect = new System.Windows.Forms.RadioButton();
            this.rdoJump = new System.Windows.Forms.RadioButton();
            this.btnOk = new System.Windows.Forms.Button();
            this.grpMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblGuide
            // 
            this.lblGuide.AutoSize = true;
            this.lblGuide.Location = new System.Drawing.Point(12, 15);
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Size = new System.Drawing.Size(221, 12);
            this.lblGuide.TabIndex = 0;
            this.lblGuide.Text = "Enter Indexes (e.g. 100, 200-300, 500):";
            // 
            // txtInput
            // 
            this.txtInput.Location = new System.Drawing.Point(12, 35);
            this.txtInput.Name = "txtInput";
            this.txtInput.Size = new System.Drawing.Size(310, 21);
            this.txtInput.TabIndex = 1;
            this.txtInput.TextChanged += new System.EventHandler(this.txtInput_TextChanged);
            // 
            // grpMode
            // 
            this.grpMode.Controls.Add(this.rdoSelect);
            this.grpMode.Controls.Add(this.rdoJump);
            this.grpMode.Location = new System.Drawing.Point(12, 70);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(310, 50);
            this.grpMode.TabIndex = 2;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "Action Mode";
            // 
            // rdoSelect
            // 
            this.rdoSelect.AutoSize = true;
            this.rdoSelect.Location = new System.Drawing.Point(150, 20);
            this.rdoSelect.Name = "rdoSelect";
            this.rdoSelect.Size = new System.Drawing.Size(98, 16);
            this.rdoSelect.TabIndex = 1;
            this.rdoSelect.Text = "Select Range";
            this.rdoSelect.UseVisualStyleBackColor = true;
            // 
            // rdoJump
            // 
            this.rdoJump.AutoSize = true;
            this.rdoJump.Checked = true;
            this.rdoJump.Location = new System.Drawing.Point(20, 20);
            this.rdoJump.Name = "rdoJump";
            this.rdoJump.Size = new System.Drawing.Size(103, 16);
            this.rdoJump.TabIndex = 0;
            this.rdoJump.TabStop = true;
            this.rdoJump.Text = "Jump to Index";
            this.rdoJump.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(117, 130);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 28);
            this.btnOk.TabIndex = 3;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // IndexToolForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 171);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.grpMode);
            this.Controls.Add(this.txtInput);
            this.Controls.Add(this.lblGuide);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IndexToolForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sub-Sound Index Tools";
            this.Load += new System.EventHandler(this.IndexToolForm_Load);
            this.grpMode.ResumeLayout(false);
            this.grpMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblGuide;
        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton rdoSelect;
        private System.Windows.Forms.RadioButton rdoJump;
        private System.Windows.Forms.Button btnOk;
    }
}