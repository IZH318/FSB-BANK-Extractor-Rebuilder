/**
 * @file AudioAnalyzerForm.Designer.cs
 * @brief Auto-generated code for the Audio Analyzer form layout and component initialization.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file contains the automated setup for Windows Forms controls used in the Audio Analyzer.
 * It is managed by the Visual Studio Form Designer and should not be manually edited.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class AudioAnalyzerForm
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
            // Clean up visualization resources
            if (_staticWaveformBitmap != null) _staticWaveformBitmap.Dispose();
            _spectrogramBitmap?.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AudioAnalyzerForm));
            this.renderTimer = new System.Windows.Forms.Timer(this.components);
            this.panelSettings = new System.Windows.Forms.Panel();
            this.tableLayoutPanelTop = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanelTop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSmoothing = new System.Windows.Forms.Label();
            this.trackSmoothing = new System.Windows.Forms.TrackBar();
            this.lblPeakHold = new System.Windows.Forms.Label();
            this.trackPeakHold = new System.Windows.Forms.TrackBar();
            this.comboBoxStandards = new System.Windows.Forms.ComboBox();
            this.btnResetLoudness = new System.Windows.Forms.Button();
            this.cmbView1 = new System.Windows.Forms.ComboBox();
            this.cmbView2 = new System.Windows.Forms.ComboBox();
            this.trackViewSplit = new System.Windows.Forms.TrackBar();
            this.lblSplitLeft = new System.Windows.Forms.Label();
            this.lblSplitRight = new System.Windows.Forms.Label();
            this.panelDrawingSurface = new System.Windows.Forms.Panel();
            this.panelSettings.SuspendLayout();
            this.tableLayoutPanelTop.SuspendLayout();
            this.flowLayoutPanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackSmoothing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackPeakHold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackViewSplit)).BeginInit();
            this.SuspendLayout();
            // 
            // renderTimer
            // 
            this.renderTimer.Interval = 16;
            this.renderTimer.Tick += new System.EventHandler(this.renderTimer_Tick);
            // 
            // panelSettings
            // 
            this.panelSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.panelSettings.Controls.Add(this.tableLayoutPanelTop);
            this.panelSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSettings.Location = new System.Drawing.Point(0, 0);
            this.panelSettings.Name = "panelSettings";
            this.panelSettings.Size = new System.Drawing.Size(704, 40);
            this.panelSettings.TabIndex = 0;
            // 
            // tableLayoutPanelTop
            // 
            this.tableLayoutPanelTop.ColumnCount = 3;
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelTop.Controls.Add(this.flowLayoutPanelTop, 1, 0);
            this.tableLayoutPanelTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelTop.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelTop.Name = "tableLayoutPanelTop";
            this.tableLayoutPanelTop.RowCount = 1;
            this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelTop.Size = new System.Drawing.Size(704, 40);
            this.tableLayoutPanelTop.TabIndex = 5;
            // 
            // flowLayoutPanelTop
            // 
            this.flowLayoutPanelTop.AutoSize = true;
            this.flowLayoutPanelTop.Controls.Add(this.lblSmoothing);
            this.flowLayoutPanelTop.Controls.Add(this.trackSmoothing);
            this.flowLayoutPanelTop.Controls.Add(this.lblPeakHold);
            this.flowLayoutPanelTop.Controls.Add(this.trackPeakHold);
            this.flowLayoutPanelTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelTop.Location = new System.Drawing.Point(92, 0);
            this.flowLayoutPanelTop.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelTop.Name = "flowLayoutPanelTop";
            this.flowLayoutPanelTop.Size = new System.Drawing.Size(520, 40);
            this.flowLayoutPanelTop.TabIndex = 0;
            this.flowLayoutPanelTop.WrapContents = false;
            // 
            // lblSmoothing
            // 
            this.lblSmoothing.AutoSize = true;
            this.lblSmoothing.ForeColor = System.Drawing.Color.LightGray;
            this.lblSmoothing.Location = new System.Drawing.Point(3, 14);
            this.lblSmoothing.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            this.lblSmoothing.Name = "lblSmoothing";
            this.lblSmoothing.Size = new System.Drawing.Size(128, 12);
            this.lblSmoothing.TabIndex = 2;
            this.lblSmoothing.Text = "Meter Response: 70%";
            // 
            // trackSmoothing
            // 
            this.trackSmoothing.AutoSize = false;
            this.trackSmoothing.Location = new System.Drawing.Point(137, 8);
            this.trackSmoothing.Margin = new System.Windows.Forms.Padding(3, 8, 30, 3);
            this.trackSmoothing.Maximum = 99;
            this.trackSmoothing.Name = "trackSmoothing";
            this.trackSmoothing.Size = new System.Drawing.Size(120, 24);
            this.trackSmoothing.TabIndex = 1;
            this.trackSmoothing.TickFrequency = 10;
            this.trackSmoothing.Value = 70;
            this.trackSmoothing.Scroll += new System.EventHandler(this.trackSmoothing_Scroll);
            // 
            // lblPeakHold
            // 
            this.lblPeakHold.AutoSize = true;
            this.lblPeakHold.ForeColor = System.Drawing.Color.LightGray;
            this.lblPeakHold.Location = new System.Drawing.Point(290, 14);
            this.lblPeakHold.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            this.lblPeakHold.Name = "lblPeakHold";
            this.lblPeakHold.Size = new System.Drawing.Size(101, 12);
            this.lblPeakHold.TabIndex = 4;
            this.lblPeakHold.Text = "Peak Hold: 2000s";
            // 
            // trackPeakHold
            // 
            this.trackPeakHold.AutoSize = false;
            this.trackPeakHold.Location = new System.Drawing.Point(397, 8);
            this.trackPeakHold.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.trackPeakHold.Maximum = 3000;
            this.trackPeakHold.Name = "trackPeakHold";
            this.trackPeakHold.Size = new System.Drawing.Size(120, 24);
            this.trackPeakHold.TabIndex = 3;
            this.trackPeakHold.TickFrequency = 500;
            this.trackPeakHold.Value = 2000;
            this.trackPeakHold.Scroll += new System.EventHandler(this.trackPeakHold_Scroll);
            // 
            // comboBoxStandards
            // 
            this.comboBoxStandards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(35)))));
            this.comboBoxStandards.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStandards.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxStandards.ForeColor = System.Drawing.Color.White;
            this.comboBoxStandards.FormattingEnabled = true;
            this.comboBoxStandards.Location = new System.Drawing.Point(525, 300);
            this.comboBoxStandards.Name = "comboBoxStandards";
            this.comboBoxStandards.Size = new System.Drawing.Size(150, 20);
            this.comboBoxStandards.TabIndex = 4;
            this.comboBoxStandards.Visible = false;
            // 
            // btnResetLoudness
            // 
            this.btnResetLoudness.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.btnResetLoudness.FlatAppearance.BorderSize = 0;
            this.btnResetLoudness.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetLoudness.ForeColor = System.Drawing.Color.White;
            this.btnResetLoudness.Location = new System.Drawing.Point(525, 420);
            this.btnResetLoudness.Name = "btnResetLoudness";
            this.btnResetLoudness.Size = new System.Drawing.Size(150, 23);
            this.btnResetLoudness.TabIndex = 5;
            this.btnResetLoudness.Text = "Reset";
            this.btnResetLoudness.UseVisualStyleBackColor = false;
            this.btnResetLoudness.Visible = false;
            this.btnResetLoudness.Click += new System.EventHandler(this.btnResetLoudness_Click);
            // 
            // cmbView1
            // 
            this.cmbView1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(35)))));
            this.cmbView1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbView1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbView1.ForeColor = System.Drawing.Color.White;
            this.cmbView1.FormattingEnabled = true;
            this.cmbView1.Location = new System.Drawing.Point(228, 140);
            this.cmbView1.Name = "cmbView1";
            this.cmbView1.Size = new System.Drawing.Size(121, 20);
            this.cmbView1.TabIndex = 1;
            this.cmbView1.Visible = false;
            // 
            // cmbView2
            // 
            this.cmbView2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(35)))));
            this.cmbView2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbView2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbView2.ForeColor = System.Drawing.Color.White;
            this.cmbView2.FormattingEnabled = true;
            this.cmbView2.Location = new System.Drawing.Point(355, 140);
            this.cmbView2.Name = "cmbView2";
            this.cmbView2.Size = new System.Drawing.Size(121, 20);
            this.cmbView2.TabIndex = 2;
            this.cmbView2.Visible = false;
            // 
            // trackViewSplit
            // 
            this.trackViewSplit.AutoSize = false;
            this.trackViewSplit.Location = new System.Drawing.Point(228, 280);
            this.trackViewSplit.Maximum = 100;
            this.trackViewSplit.Name = "trackViewSplit";
            this.trackViewSplit.Size = new System.Drawing.Size(334, 24);
            this.trackViewSplit.TabIndex = 6;
            this.trackViewSplit.TickFrequency = 10;
            this.trackViewSplit.Value = 50;
            this.trackViewSplit.Visible = false;
            this.trackViewSplit.Scroll += new System.EventHandler(this.trackViewSplit_Scroll);
            // 
            // lblSplitLeft
            // 
            this.lblSplitLeft.AutoSize = true;
            this.lblSplitLeft.ForeColor = System.Drawing.Color.LightGray;
            this.lblSplitLeft.Location = new System.Drawing.Point(228, 244);
            this.lblSplitLeft.Name = "lblSplitLeft";
            this.lblSplitLeft.Size = new System.Drawing.Size(27, 12);
            this.lblSplitLeft.TabIndex = 7;
            this.lblSplitLeft.Text = "50%";
            this.lblSplitLeft.Visible = false;
            // 
            // lblSplitRight
            // 
            this.lblSplitRight.AutoSize = true;
            this.lblSplitRight.ForeColor = System.Drawing.Color.LightGray;
            this.lblSplitRight.Location = new System.Drawing.Point(533, 244);
            this.lblSplitRight.Name = "lblSplitRight";
            this.lblSplitRight.Size = new System.Drawing.Size(27, 12);
            this.lblSplitRight.TabIndex = 8;
            this.lblSplitRight.Text = "50%";
            this.lblSplitRight.Visible = false;
            // 
            // panelDrawingSurface
            // 
            this.panelDrawingSurface.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDrawingSurface.Location = new System.Drawing.Point(0, 40);
            this.panelDrawingSurface.Name = "panelDrawingSurface";
            this.panelDrawingSurface.Size = new System.Drawing.Size(704, 441);
            this.panelDrawingSurface.TabIndex = 9;
            this.panelDrawingSurface.Paint += new System.Windows.Forms.PaintEventHandler(this.panelDrawingSurface_Paint);
            // 
            // AudioAnalyzerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(704, 481);
            this.Controls.Add(this.lblSplitRight);
            this.Controls.Add(this.lblSplitLeft);
            this.Controls.Add(this.trackViewSplit);
            this.Controls.Add(this.cmbView2);
            this.Controls.Add(this.cmbView1);
            this.Controls.Add(this.btnResetLoudness);
            this.Controls.Add(this.comboBoxStandards);
            this.Controls.Add(this.panelDrawingSurface);
            this.Controls.Add(this.panelSettings);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(720, 520);
            this.Name = "AudioAnalyzerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Audio Analyzer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AudioAnalyzerForm_FormClosing);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.AudioAnalyzerForm_Paint);
            this.Resize += new System.EventHandler(this.AudioAnalyzerForm_Resize);
            this.panelSettings.ResumeLayout(false);
            this.tableLayoutPanelTop.ResumeLayout(false);
            this.tableLayoutPanelTop.PerformLayout();
            this.flowLayoutPanelTop.ResumeLayout(false);
            this.flowLayoutPanelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackSmoothing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackPeakHold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackViewSplit)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private System.Windows.Forms.Timer renderTimer;
        private System.Windows.Forms.Panel panelSettings;
        private System.Windows.Forms.Label lblSmoothing;
        private System.Windows.Forms.TrackBar trackSmoothing;
        private System.Windows.Forms.Label lblPeakHold;
        private System.Windows.Forms.TrackBar trackPeakHold;
        private System.Windows.Forms.ComboBox comboBoxStandards;
        private System.Windows.Forms.Button btnResetLoudness;
        private System.Windows.Forms.ComboBox cmbView1;
        private System.Windows.Forms.ComboBox cmbView2;
        private System.Windows.Forms.TrackBar trackViewSplit;
        private System.Windows.Forms.Label lblSplitLeft;
        private System.Windows.Forms.Label lblSplitRight;
        private System.Windows.Forms.Panel panelDrawingSurface;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTop;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelTop;
    }
}