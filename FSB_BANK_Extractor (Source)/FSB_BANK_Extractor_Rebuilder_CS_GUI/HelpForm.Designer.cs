/**
 * @file HelpForm.Designer.cs
 * @brief Auto-generated code for the Help GUI form layout and component initialization.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file contains the automated setup for Windows Forms controls (TabControl, RichTextBox, etc.).
 * It is managed by the Visual Studio Form Designer.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-09
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class HelpForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpForm));
            this.tabControlHelp = new System.Windows.Forms.TabControl();
            this.tabPageKorean = new System.Windows.Forms.TabPage();
            this.richTextBoxKorean = new System.Windows.Forms.RichTextBox();
            this.tabPageEnglish = new System.Windows.Forms.TabPage();
            this.richTextBoxEnglish = new System.Windows.Forms.RichTextBox();
            this.tabControlHelp.SuspendLayout();
            this.tabPageKorean.SuspendLayout();
            this.tabPageEnglish.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlHelp
            // 
            this.tabControlHelp.Controls.Add(this.tabPageKorean);
            this.tabControlHelp.Controls.Add(this.tabPageEnglish);
            this.tabControlHelp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlHelp.Location = new System.Drawing.Point(0, 0);
            this.tabControlHelp.Name = "tabControlHelp";
            this.tabControlHelp.SelectedIndex = 0;
            this.tabControlHelp.Size = new System.Drawing.Size(838, 441);
            this.tabControlHelp.TabIndex = 0;
            // 
            // tabPageKorean
            // 
            this.tabPageKorean.Controls.Add(this.richTextBoxKorean);
            this.tabPageKorean.Location = new System.Drawing.Point(4, 22);
            this.tabPageKorean.Name = "tabPageKorean";
            this.tabPageKorean.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageKorean.Size = new System.Drawing.Size(830, 415);
            this.tabPageKorean.TabIndex = 0;
            this.tabPageKorean.Text = "한국어 (KR)";
            this.tabPageKorean.UseVisualStyleBackColor = true;
            // 
            // richTextBoxKorean
            // 
            this.richTextBoxKorean.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxKorean.Location = new System.Drawing.Point(3, 3);
            this.richTextBoxKorean.Name = "richTextBoxKorean";
            this.richTextBoxKorean.ReadOnly = true;
            this.richTextBoxKorean.Size = new System.Drawing.Size(824, 409);
            this.richTextBoxKorean.TabIndex = 0;
            this.richTextBoxKorean.Text = "";
            // 
            // tabPageEnglish
            // 
            this.tabPageEnglish.Controls.Add(this.richTextBoxEnglish);
            this.tabPageEnglish.Location = new System.Drawing.Point(4, 22);
            this.tabPageEnglish.Name = "tabPageEnglish";
            this.tabPageEnglish.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEnglish.Size = new System.Drawing.Size(830, 415);
            this.tabPageEnglish.TabIndex = 1;
            this.tabPageEnglish.Text = "English (EN)";
            this.tabPageEnglish.UseVisualStyleBackColor = true;
            // 
            // richTextBoxEnglish
            // 
            this.richTextBoxEnglish.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxEnglish.Location = new System.Drawing.Point(3, 3);
            this.richTextBoxEnglish.Name = "richTextBoxEnglish";
            this.richTextBoxEnglish.ReadOnly = true;
            this.richTextBoxEnglish.Size = new System.Drawing.Size(824, 409);
            this.richTextBoxEnglish.TabIndex = 0;
            this.richTextBoxEnglish.Text = "";
            // 
            // HelpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(838, 441);
            this.Controls.Add(this.tabControlHelp);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HelpForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "도움말(Help)";
            this.tabControlHelp.ResumeLayout(false);
            this.tabPageKorean.ResumeLayout(false);
            this.tabPageEnglish.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlHelp;
        private System.Windows.Forms.TabPage tabPageKorean;
        private System.Windows.Forms.RichTextBox richTextBoxKorean;
        private System.Windows.Forms.TabPage tabPageEnglish;
        private System.Windows.Forms.RichTextBox richTextBoxEnglish;
    }
}