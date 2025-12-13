/**
 * @file RebuildManagerForm.Designer.cs
 * @brief Contains the auto-generated designer code for the RebuildManagerForm.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file is managed by the Visual Studio Form Designer and should not be edited manually.
 * It defines the layout and properties of all UI controls used in the Rebuild Manager.
 *
 * Key Features:
 *  - Auto-generated UI code: Defines controls, properties, and event wiring.
 *  - Resource Management Note: The standard Dispose method is moved to the main .cs file to handle custom resources.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-13
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class RebuildManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RebuildManagerForm));
            this.pnlTop = new System.Windows.Forms.Panel();
            this.tblWarningPanel = new System.Windows.Forms.TableLayoutPanel();
            this.grpWarningLong = new System.Windows.Forms.GroupBox();
            this.lblWarningLongText = new System.Windows.Forms.Label();
            this.grpWarningShort = new System.Windows.Forms.GroupBox();
            this.lblWarningShortText = new System.Windows.Forms.Label();
            this.grpWarningLoop = new System.Windows.Forms.GroupBox();
            this.lblWarningLoopText = new System.Windows.Forms.Label();
            this.lblTarget = new System.Windows.Forms.Label();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpEncoding = new System.Windows.Forms.GroupBox();
            this.lblFormatInfo = new System.Windows.Forms.Label();
            this.cmbFormat = new System.Windows.Forms.ComboBox();
            this.lblFmt = new System.Windows.Forms.Label();
            this.grpTools = new System.Windows.Forms.GroupBox();
            this.btnClearAll = new System.Windows.Forms.Button();
            this.btnAutoMatch = new System.Windows.Forms.Button();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.pnlTop.SuspendLayout();
            this.tblWarningPanel.SuspendLayout();
            this.grpWarningLong.SuspendLayout();
            this.grpWarningShort.SuspendLayout();
            this.grpWarningLoop.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.grpEncoding.SuspendLayout();
            this.grpTools.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.AutoSize = true;
            this.pnlTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlTop.Controls.Add(this.tblWarningPanel);
            this.pnlTop.Controls.Add(this.lblTarget);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Padding = new System.Windows.Forms.Padding(10, 10, 10, 5);
            this.pnlTop.Size = new System.Drawing.Size(704, 144);
            this.pnlTop.TabIndex = 0;
            // 
            // tblWarningPanel
            // 
            this.tblWarningPanel.AutoSize = true;
            this.tblWarningPanel.ColumnCount = 1;
            this.tblWarningPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblWarningPanel.Controls.Add(this.grpWarningLong, 0, 0);
            this.tblWarningPanel.Controls.Add(this.grpWarningShort, 0, 1);
            this.tblWarningPanel.Controls.Add(this.grpWarningLoop, 0, 2);
            this.tblWarningPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblWarningPanel.Location = new System.Drawing.Point(10, 22);
            this.tblWarningPanel.Name = "tblWarningPanel";
            this.tblWarningPanel.RowCount = 3;
            this.tblWarningPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblWarningPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblWarningPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblWarningPanel.Size = new System.Drawing.Size(684, 117);
            this.tblWarningPanel.TabIndex = 3;
            // 
            // grpWarningLong
            // 
            this.grpWarningLong.AutoSize = true;
            this.grpWarningLong.Controls.Add(this.lblWarningLongText);
            this.grpWarningLong.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWarningLong.ForeColor = System.Drawing.Color.Red;
            this.grpWarningLong.Location = new System.Drawing.Point(3, 3);
            this.grpWarningLong.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.grpWarningLong.Name = "grpWarningLong";
            this.grpWarningLong.Padding = new System.Windows.Forms.Padding(8, 3, 8, 5);
            this.grpWarningLong.Size = new System.Drawing.Size(678, 36);
            this.grpWarningLong.TabIndex = 0;
            this.grpWarningLong.TabStop = false;
            this.grpWarningLong.Text = "Duration Warning";
            // 
            // lblWarningLongText
            // 
            this.lblWarningLongText.AutoSize = true;
            this.lblWarningLongText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWarningLongText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblWarningLongText.Location = new System.Drawing.Point(8, 17);
            this.lblWarningLongText.MaximumSize = new System.Drawing.Size(660, 0);
            this.lblWarningLongText.Name = "lblWarningLongText";
            this.lblWarningLongText.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblWarningLongText.Size = new System.Drawing.Size(17, 14);
            this.lblWarningLongText.TabIndex = 1;
            this.lblWarningLongText.Text = "...";
            // 
            // grpWarningShort
            // 
            this.grpWarningShort.AutoSize = true;
            this.grpWarningShort.Controls.Add(this.lblWarningShortText);
            this.grpWarningShort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWarningShort.ForeColor = System.Drawing.Color.Blue;
            this.grpWarningShort.Location = new System.Drawing.Point(3, 42);
            this.grpWarningShort.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.grpWarningShort.Name = "grpWarningShort";
            this.grpWarningShort.Padding = new System.Windows.Forms.Padding(8, 3, 8, 5);
            this.grpWarningShort.Size = new System.Drawing.Size(678, 36);
            this.grpWarningShort.TabIndex = 1;
            this.grpWarningShort.TabStop = false;
            this.grpWarningShort.Text = "Duration Note";
            // 
            // lblWarningShortText
            // 
            this.lblWarningShortText.AutoSize = true;
            this.lblWarningShortText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWarningShortText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblWarningShortText.Location = new System.Drawing.Point(8, 17);
            this.lblWarningShortText.MaximumSize = new System.Drawing.Size(660, 0);
            this.lblWarningShortText.Name = "lblWarningShortText";
            this.lblWarningShortText.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblWarningShortText.Size = new System.Drawing.Size(17, 14);
            this.lblWarningShortText.TabIndex = 1;
            this.lblWarningShortText.Text = "...";
            // 
            // grpWarningLoop
            // 
            this.grpWarningLoop.AutoSize = true;
            this.grpWarningLoop.Controls.Add(this.lblWarningLoopText);
            this.grpWarningLoop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWarningLoop.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.grpWarningLoop.Location = new System.Drawing.Point(3, 81);
            this.grpWarningLoop.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.grpWarningLoop.Name = "grpWarningLoop";
            this.grpWarningLoop.Padding = new System.Windows.Forms.Padding(8, 3, 8, 5);
            this.grpWarningLoop.Size = new System.Drawing.Size(678, 36);
            this.grpWarningLoop.TabIndex = 2;
            this.grpWarningLoop.TabStop = false;
            this.grpWarningLoop.Text = "Looping Status";
            // 
            // lblWarningLoopText
            // 
            this.lblWarningLoopText.AutoSize = true;
            this.lblWarningLoopText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWarningLoopText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblWarningLoopText.Location = new System.Drawing.Point(8, 17);
            this.lblWarningLoopText.MaximumSize = new System.Drawing.Size(660, 0);
            this.lblWarningLoopText.Name = "lblWarningLoopText";
            this.lblWarningLoopText.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblWarningLoopText.Size = new System.Drawing.Size(17, 14);
            this.lblWarningLoopText.TabIndex = 1;
            this.lblWarningLoopText.Text = "...";
            // 
            // lblTarget
            // 
            this.lblTarget.AutoSize = true;
            this.lblTarget.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTarget.Location = new System.Drawing.Point(10, 10);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Size = new System.Drawing.Size(61, 12);
            this.lblTarget.TabIndex = 0;
            this.lblTarget.Text = "Target: ...";
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.btnStart);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.grpEncoding);
            this.pnlBottom.Controls.Add(this.grpTools);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 371);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(10);
            this.pnlBottom.Size = new System.Drawing.Size(704, 110);
            this.pnlBottom.TabIndex = 1;
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnStart.Location = new System.Drawing.Point(584, 20);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(110, 36);
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "START BUILD";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(584, 62);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 36);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // grpEncoding
            // 
            this.grpEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpEncoding.Controls.Add(this.lblFormatInfo);
            this.grpEncoding.Controls.Add(this.cmbFormat);
            this.grpEncoding.Controls.Add(this.lblFmt);
            this.grpEncoding.Location = new System.Drawing.Point(201, 13);
            this.grpEncoding.Name = "grpEncoding";
            this.grpEncoding.Size = new System.Drawing.Size(377, 85);
            this.grpEncoding.TabIndex = 1;
            this.grpEncoding.TabStop = false;
            this.grpEncoding.Text = "Encoding Options";
            // 
            // lblFormatInfo
            // 
            this.lblFormatInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblFormatInfo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblFormatInfo.Location = new System.Drawing.Point(3, 49);
            this.lblFormatInfo.Name = "lblFormatInfo";
            this.lblFormatInfo.Size = new System.Drawing.Size(371, 33);
            this.lblFormatInfo.TabIndex = 2;
            this.lblFormatInfo.Text = "...";
            // 
            // cmbFormat
            // 
            this.cmbFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFormat.FormattingEnabled = true;
            this.cmbFormat.Location = new System.Drawing.Point(64, 21);
            this.cmbFormat.Name = "cmbFormat";
            this.cmbFormat.Size = new System.Drawing.Size(121, 20);
            this.cmbFormat.TabIndex = 1;
            // 
            // lblFmt
            // 
            this.lblFmt.AutoSize = true;
            this.lblFmt.Location = new System.Drawing.Point(10, 24);
            this.lblFmt.Name = "lblFmt";
            this.lblFmt.Size = new System.Drawing.Size(48, 12);
            this.lblFmt.TabIndex = 0;
            this.lblFmt.Text = "Format:";
            // 
            // grpTools
            // 
            this.grpTools.Controls.Add(this.btnClearAll);
            this.grpTools.Controls.Add(this.btnAutoMatch);
            this.grpTools.Location = new System.Drawing.Point(13, 13);
            this.grpTools.Name = "grpTools";
            this.grpTools.Size = new System.Drawing.Size(182, 85);
            this.grpTools.TabIndex = 0;
            this.grpTools.TabStop = false;
            this.grpTools.Text = "Batch Tools";
            // 
            // btnClearAll
            // 
            this.btnClearAll.Location = new System.Drawing.Point(9, 49);
            this.btnClearAll.Name = "btnClearAll";
            this.btnClearAll.Size = new System.Drawing.Size(167, 28);
            this.btnClearAll.TabIndex = 1;
            this.btnClearAll.Text = "Clear All Replacements";
            this.btnClearAll.UseVisualStyleBackColor = true;
            this.btnClearAll.Click += new System.EventHandler(this.btnClearAll_Click);
            // 
            // btnAutoMatch
            // 
            this.btnAutoMatch.Location = new System.Drawing.Point(9, 18);
            this.btnAutoMatch.Name = "btnAutoMatch";
            this.btnAutoMatch.Size = new System.Drawing.Size(167, 28);
            this.btnAutoMatch.TabIndex = 0;
            this.btnAutoMatch.Text = "Auto-Match from Folder";
            this.btnAutoMatch.UseVisualStyleBackColor = true;
            this.btnAutoMatch.Click += new System.EventHandler(this.btnAutoMatch_Click);
            // 
            // dgvItems
            // 
            this.dgvItems.AllowUserToAddRows = false;
            this.dgvItems.AllowUserToDeleteRows = false;
            this.dgvItems.AllowUserToResizeRows = false;
            this.dgvItems.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvItems.ColumnHeadersHeight = 25;
            this.dgvItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvItems.Location = new System.Drawing.Point(0, 144);
            this.dgvItems.MultiSelect = false;
            this.dgvItems.Name = "dgvItems";
            this.dgvItems.RowHeadersVisible = false;
            this.dgvItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvItems.Size = new System.Drawing.Size(704, 227);
            this.dgvItems.TabIndex = 2;
            this.dgvItems.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvItems_CellContentClick);
            this.dgvItems.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvItems_CellFormatting);
            // 
            // RebuildManagerForm
            // 
            this.AcceptButton = this.btnStart;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(704, 481);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(720, 520);
            this.Name = "RebuildManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Rebuild Manager";
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.tblWarningPanel.ResumeLayout(false);
            this.tblWarningPanel.PerformLayout();
            this.grpWarningLong.ResumeLayout(false);
            this.grpWarningLong.PerformLayout();
            this.grpWarningShort.ResumeLayout(false);
            this.grpWarningShort.PerformLayout();
            this.grpWarningLoop.ResumeLayout(false);
            this.grpWarningLoop.PerformLayout();
            this.pnlBottom.ResumeLayout(false);
            this.grpEncoding.ResumeLayout(false);
            this.grpEncoding.PerformLayout();
            this.grpTools.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox grpEncoding;
        private System.Windows.Forms.Label lblFormatInfo;
        private System.Windows.Forms.ComboBox cmbFormat;
        private System.Windows.Forms.Label lblFmt;
        private System.Windows.Forms.GroupBox grpTools;
        private System.Windows.Forms.Button btnClearAll;
        private System.Windows.Forms.Button btnAutoMatch;
        private System.Windows.Forms.DataGridView dgvItems;
        private System.Windows.Forms.GroupBox grpWarningLong;
        private System.Windows.Forms.GroupBox grpWarningShort;
        private System.Windows.Forms.GroupBox grpWarningLoop;
        private System.Windows.Forms.Label lblWarningLongText;
        private System.Windows.Forms.Label lblWarningShortText;
        private System.Windows.Forms.Label lblWarningLoopText;
        private System.Windows.Forms.TableLayoutPanel tblWarningPanel;
    }
}