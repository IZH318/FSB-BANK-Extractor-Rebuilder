/**
 * @file AudioAnalyzerForm.cs
 * @brief Provides a real-time audio visualization and analysis interface using FMOD.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form renders a static waveform, frequency spectrum, spectrogram, vectorscope, oscilloscope,
 * and detailed loudness statistics (LUFS, True Peak) for a playing audio stream. It uses FMOD's
 * Digital Signal Processing (DSP) units for FFT and Loudness Metering to capture data in real-time.
 * The loudness analysis strictly follows standards like EBU R 128 and ATSC A/85.
 *
 * Key Features:
 *  - Dynamically configurable analysis panel with a split-view mode.
 *  - Real-time FFT spectrum analysis, spectrogram, oscilloscope, and vectorscope.
 *  - Comprehensive loudness metering (Momentary, Short-Term, Integrated, True Peak).
 *  - Compliance checks against various broadcasting standards.
 *  - Detailed per-channel statistics including RMS, Peak, and Clip detection.
 *
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-13
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FMOD; // Core API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class AudioAnalyzerForm : Form
    {
        #region 1. Constants & Configuration

        /// <summary>
        /// Contains constants related to audio analysis parameters.
        /// </summary>
        private static class AnalysisSettings
        {
            public const int FFT_WINDOW_SIZE = 4096;
            public const float SILENCE_THRESHOLD_DB = -90.0f;
            public const float METER_MIN_DB = -60.0f;
            public const int SPECTRUM_HISTORY_COUNT = 512; // Width of the spectrogram in pixels.
            public const float SPECTRUM_MIN_DB = -72.0f;
            public const float SPECTRUM_MAX_DB = 6.0f;
        }

        /// <summary>
        /// Contains constants related to the visual layout of the form.
        /// </summary>
        private static class LayoutConstants
        {
            public const float RATIO_WAVEFORM = 0.20f;
            public const float RATIO_ANALYSIS = 0.35f;
            public const float RATIO_STATS = 0.45f;
            public const int PADDING = 8;
            public const int LOUDNESS_PANEL_WIDTH = 200;
        }

        /// <summary>
        /// Defines the color palette and fonts used throughout the application theme.
        /// </summary>
        private static class AppTheme
        {
            // Define panel and background colors.
            public static readonly Color BG = Color.FromArgb(20, 20, 20);
            public static readonly Color PANEL_BG = Color.FromArgb(32, 32, 35);
            public static readonly Color SETTINGS_PANEL_BG = Color.FromArgb(45, 45, 48);
            public static readonly Color GRID = Color.FromArgb(60, 60, 60);
            public static readonly Color GRAPH_BG = Color.Black;

            // Define text colors.
            public static readonly Color LABEL = Color.Gray;
            public static readonly Color AXIS_TEXT = Color.FromArgb(180, 180, 180);
            public static readonly Color VAL_NORMAL = Color.White;
            public static readonly Color VAL_WARNING = Color.Yellow;
            public static readonly Color VAL_DANGER = Color.Red;
            public static readonly Color VAL_OK = Color.FromArgb(0, 200, 0);

            // Define graph colors.
            public static readonly Color WAVEFORM_STATIC = Color.FromArgb(100, 149, 237);
            public static readonly Color PLAYHEAD = Color.Red;
            public static readonly Color OSCILLOSCOPE = Color.Lime;
            public static readonly Color VECTORSCOPE = Color.FromArgb(0, 255, 128);
            public static readonly Color FFT_TOP = Color.FromArgb(0, 255, 128);
            public static readonly Color FFT_BOTTOM = Color.FromArgb(0, 100, 200);

            // Define meter colors.
            public static readonly Color METER_BG = Color.FromArgb(10, 10, 10);
            public static readonly Color METER_LOW = Color.FromArgb(0, 180, 0);
            public static readonly Color METER_MID = Color.FromArgb(200, 200, 0);
            public static readonly Color METER_HIGH = Color.Red;

            // Define clip indicator colors.
            public static readonly Color CLIP_ON = Color.Red;
            public static readonly Color CLIP_OFF = Color.FromArgb(60, 0, 0);
        }

        #endregion

        #region 2. Enums & Structs

        /// <summary>
        /// Defines the available analysis tools for the dynamic view panels.
        /// </summary>
        private enum AnalysisTool { Oscilloscope, Spectrum, Spectrogram }

        /// <summary>
        /// Defines the properties of a loudness standard.
        /// </summary>
        private struct LoudnessStandard
        {
            public string Name { get; set; }
            public float? TargetIntegratedLoudness { get; set; }
            public float? MaxTruePeak { get; set; }
            public override string ToString() => Name;
        }

        #endregion

        #region 3. Fields & State

        // Declare font objects.
        private Font _fontLabel;
        private Font _fontValue;
        private Font _fontTitle;
        private Font _fontAxis;

        // Declare dynamic view state variables.
        private AnalysisTool _panel1Tool = AnalysisTool.Spectrum;
        private AnalysisTool _panel2Tool = AnalysisTool.Spectrogram;
        private float _splitRatio = 0.5f; // 0.0 to 1.0

        // Declare FMOD system objects.
        private FMOD.System _coreSystem;
        private FMOD.Channel _activeChannel;
        private FMOD.Sound _activeSound;

        // Declare FMOD DSP units.
        private FMOD.DSP _fftDsp;
        private FMOD.DSP _meteringDsp;
        private FMOD.DSP _loudnessDsp;

        // Declare analysis data structures.
        private FMOD.DSP_PARAMETER_FFT _fftData;
        private FMOD.DSP_METERING_INFO _meteringOutput;
        private FMOD.DSP_LOUDNESS_METER_INFO_TYPE _loudnessInfo;

        // Declare visualization state variables.
        private Bitmap _staticWaveformBitmap;
        private Bitmap _spectrogramBitmap;
        private List<float[]> _spectrumHistory;
        private bool _isInitialized = false;
        private uint _totalLengthMs = 0;
        private float _currentSampleRate = 48000;
        private int _lastChannelCount = 0;
        private string _audioInfoString = "";

        // Declare smoothing and hold state variables.
        private float[] _smoothedRMS;
        private float[] _smoothedPeak;
        private long[] _clipResetTime;
        private float _smoothingFactor = 0.7f;
        private int _peakHoldTimeMs = 2000;

        // Declare statistics history accumulators.
        private float[] _statMaxPeak;
        private float[] _statMaxRMS;
        private float[] _statMinRMS;
        private int[] _statClipCount;
        private float _statMaxMomentaryLUFS = -100.0f;
        private float _statMaxShortTermLUFS = -100.0f;
        private float _statMaxTruePeak = -100.0f;
        private float _statDcOffset = 0.0f;

        // Declare loudness standard variables.
        private List<LoudnessStandard> _loudnessStandards;
        private LoudnessStandard _selectedStandard;

        // Track volume for auto-reset functionality.
        private float _lastKnownVolume = -1.0f;

        #endregion

        #region 4. Initialization & Cleanup

        /// <summary>
        /// Initializes a new instance of the AudioAnalyzerForm class.
        /// </summary>
        public AudioAnalyzerForm()
        {
            InitializeComponent();
            InitializeCustomResources();
            InitializeDynamicViewControls();
            InitializeLoudnessStandardsAndComboBox();
            UpdateSettingsLabels();
            UpdateSplitLabels();

            // Override designer properties with constants for consistency.
            this.BackColor = AppTheme.BG;
            panelSettings.BackColor = AppTheme.SETTINGS_PANEL_BG;
            lblPeakHold.ForeColor = AppTheme.AXIS_TEXT;
            lblSmoothing.ForeColor = AppTheme.AXIS_TEXT;
            lblSplitLeft.ForeColor = AppTheme.AXIS_TEXT;
            lblSplitRight.ForeColor = AppTheme.AXIS_TEXT;
            comboBoxStandards.BackColor = AppTheme.PANEL_BG;
            comboBoxStandards.ForeColor = AppTheme.VAL_NORMAL;
            cmbView1.BackColor = AppTheme.PANEL_BG;
            cmbView1.ForeColor = AppTheme.VAL_NORMAL;
            cmbView2.BackColor = AppTheme.PANEL_BG;
            cmbView2.ForeColor = AppTheme.VAL_NORMAL;
            btnResetLoudness.BackColor = AppTheme.GRID;
            btnResetLoudness.ForeColor = AppTheme.VAL_NORMAL;

            // Enable double buffering on the custom drawing panel to reduce flicker.
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, this.panelDrawingSurface, new object[] { true });

            // Perform initial layout update.
            UpdateControlLayout();
        }

        /// <summary>
        /// Sets up GDI+ resources and drawing styles.
        /// </summary>
        private void InitializeCustomResources()
        {
            _fontTitle = new Font("Segoe UI", 9f, FontStyle.Bold);
            _fontLabel = new Font("Segoe UI", 8f, FontStyle.Regular);
            _fontValue = new Font("Consolas", 9f, FontStyle.Bold);
            _fontAxis = new Font("Segoe UI", 7f, FontStyle.Regular);

            _spectrumHistory = new List<float[]>();

            // Enable double buffering to reduce flicker during high-speed rendering.
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        /// <summary>
        /// Initializes controls for the dynamic view system.
        /// </summary>
        private void InitializeDynamicViewControls()
        {
            cmbView1.DataSource = Enum.GetValues(typeof(AnalysisTool));
            cmbView2.DataSource = Enum.GetValues(typeof(AnalysisTool));

            cmbView1.SelectedItem = _panel1Tool;
            cmbView2.SelectedItem = _panel2Tool;

            cmbView1.SelectedIndexChanged += CmbView_SelectedIndexChanged;
            cmbView2.SelectedIndexChanged += CmbView_SelectedIndexChanged;

            UpdateComboBoxes();
        }

        /// <summary>
        /// Initializes the list of loudness standards and configures the ComboBox.
        /// </summary>
        private void InitializeLoudnessStandardsAndComboBox()
        {
            _loudnessStandards = new List<LoudnessStandard>
            {
                new LoudnessStandard { Name = "EBU R 128", TargetIntegratedLoudness = -23.0f, MaxTruePeak = -1.0f },
                new LoudnessStandard { Name = "ATSC A/85", TargetIntegratedLoudness = -24.0f, MaxTruePeak = -2.0f },
                new LoudnessStandard { Name = "ARIB TR-B32", TargetIntegratedLoudness = -24.0f, MaxTruePeak = -1.0f },
                new LoudnessStandard { Name = "OP-59", TargetIntegratedLoudness = -24.0f, MaxTruePeak = -2.0f },
                new LoudnessStandard { Name = "ITU-R BS.1770", TargetIntegratedLoudness = null, MaxTruePeak = null }
            };

            // Configure the data source for the combo box.
            comboBoxStandards.DataSource = _loudnessStandards;
            comboBoxStandards.SelectedIndex = 0;
            _selectedStandard = (LoudnessStandard)comboBoxStandards.SelectedItem;
            comboBoxStandards.SelectedIndexChanged += ComboBoxStandards_SelectedIndexChanged;
        }

        /// <summary>
        /// Attaches the analyzer to the currently playing audio stream.
        /// </summary>
        /// <param name="coreSystem">The FMOD Core system.</param>
        /// <param name="channel">The active playback channel.</param>
        /// <param name="sound">The sound being played.</param>
        public void AttachToAudio(FMOD.System coreSystem, FMOD.Channel channel, FMOD.Sound sound)
        {
            ResetAnalysis();

            _coreSystem = coreSystem;
            _activeChannel = channel;
            _activeSound = sound;

            if (!_activeChannel.hasHandle()) return;

            // Capture initial volume to prevent an immediate reset loop.
            _activeChannel.getVolume(out _lastKnownVolume);

            sound.getDefaults(out _currentSampleRate, out _);

            _coreSystem.createDSPByType(DSP_TYPE.LOUDNESS_METER, out _meteringDsp);
            if (_meteringDsp.hasHandle())
            {
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _meteringDsp);
                _meteringDsp.setActive(true);
                _meteringDsp.setMeteringEnabled(true, true);
            }

            _coreSystem.createDSPByType(DSP_TYPE.FFT, out _fftDsp);
            if (_fftDsp.hasHandle())
            {
                _fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, AnalysisSettings.FFT_WINDOW_SIZE);
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _fftDsp);
                _fftDsp.setActive(true);
            }

            _coreSystem.createDSPByType(DSP_TYPE.LOUDNESS_METER, out _loudnessDsp);
            if (_loudnessDsp.hasHandle())
            {
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _loudnessDsp);
                _loudnessDsp.setActive(true);
            }

            _activeSound.getFormat(out _, out SOUND_FORMAT format, out int numChannels, out int bits);
            _lastChannelCount = numChannels;

            string formatType = (format == SOUND_FORMAT.PCMFLOAT) ? " Float" : "";
            string channelStr = numChannels switch
            {
                1 => "Mono",
                2 => "Stereo",
                4 => "Quad",
                6 => "5.1",
                8 => "7.1",
                _ => $"{numChannels} Ch"
            };
            _audioInfoString = $"{_currentSampleRate / 1000:F1} kHz, {bits}-bit{formatType}, {channelStr}";

            int safeChannels = Math.Max(numChannels, 8);
            _statMaxPeak = new float[safeChannels];
            _statMaxRMS = new float[safeChannels];
            _statMinRMS = new float[safeChannels];
            _statClipCount = new int[safeChannels];
            _smoothedRMS = new float[safeChannels];
            _smoothedPeak = new float[safeChannels];
            _clipResetTime = new long[safeChannels];
            Array.Clear(_statMinRMS, 0, safeChannels);
            Array.Clear(_statClipCount, 0, safeChannels);

            GenerateStaticWaveform();

            _activeSound.getLength(out _totalLengthMs, TIMEUNIT.MS);
            _isInitialized = true;

            cmbView1.Visible = true;
            cmbView2.Visible = true;
            trackViewSplit.Visible = true;
            lblSplitLeft.Visible = true;
            lblSplitRight.Visible = true;
            comboBoxStandards.Visible = true;
            btnResetLoudness.Visible = true;
            UpdateControlLayout();

            renderTimer.Start();
        }

        /// <summary>
        /// Resets the analyzer state and releases FMOD DSP resources.
        /// </summary>
        private void ResetAnalysis()
        {
            renderTimer.Stop();
            _isInitialized = false;
            _audioInfoString = "";

            cmbView1.Visible = false;
            cmbView2.Visible = false;
            trackViewSplit.Visible = false;
            lblSplitLeft.Visible = false;
            lblSplitRight.Visible = false;
            comboBoxStandards.Visible = false;
            btnResetLoudness.Visible = false;

            if (_fftDsp.hasHandle())
            {
                if (_activeChannel.hasHandle()) _activeChannel.removeDSP(_fftDsp);
                _fftDsp.release(); _fftDsp.clearHandle();
            }
            if (_meteringDsp.hasHandle())
            {
                if (_activeChannel.hasHandle()) _activeChannel.removeDSP(_meteringDsp);
                _meteringDsp.release(); _meteringDsp.clearHandle();
            }
            if (_loudnessDsp.hasHandle())
            {
                if (_activeChannel.hasHandle()) _activeChannel.removeDSP(_loudnessDsp);
                _loudnessDsp.release(); _loudnessDsp.clearHandle();
            }

            _staticWaveformBitmap?.Dispose(); _staticWaveformBitmap = null;
            _spectrogramBitmap?.Dispose(); _spectrogramBitmap = null;
            _spectrumHistory?.Clear();
        }

        /// <summary>
        /// Resets all loudness-related DSPs and tracked statistics to start a new measurement session.
        /// </summary>
        private void ResetLoudnessAnalysis()
        {
            if (_loudnessDsp.hasHandle())
            {
                _loudnessDsp.reset();
            }

            _statMaxMomentaryLUFS = -100.0f;
            _statMaxShortTermLUFS = -100.0f;
            _statMaxTruePeak = -100.0f;

            panelDrawingSurface.Invalidate();
        }

        #endregion

        #region 5. UI Events & Layout

        /// <summary>
        /// Handles the FormClosing event of the AudioAnalyzerForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        private void AudioAnalyzerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ResetAnalysis();
            _fontLabel?.Dispose();
            _fontValue?.Dispose();
            _fontTitle?.Dispose();
            _fontAxis?.Dispose();
        }

        /// <summary>
        /// Handles the Resize event of the AudioAnalyzerForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void AudioAnalyzerForm_Resize(object sender, EventArgs e)
        {
            UpdateControlLayout();
            panelDrawingSurface.Invalidate();
        }

        /// <summary>
        /// Handles the Scroll event of the trackSmoothing control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackSmoothing_Scroll(object sender, EventArgs e)
        {
            float val = trackSmoothing.Value;
            _smoothingFactor = val / 100.0f;
            UpdateSettingsLabels();
        }

        /// <summary>
        /// Handles the Scroll event of the trackPeakHold control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackPeakHold_Scroll(object sender, EventArgs e)
        {
            _peakHoldTimeMs = trackPeakHold.Value;
            UpdateSettingsLabels();
        }

        /// <summary>
        /// Handles the Scroll event of the trackViewSplit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackViewSplit_Scroll(object sender, EventArgs e)
        {
            _splitRatio = trackViewSplit.Value / 100.0f;
            UpdateSplitLabels();
            panelDrawingSurface.Invalidate();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event for the view selection ComboBoxes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CmbView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender == cmbView1) _panel1Tool = (AnalysisTool)cmbView1.SelectedItem;
            if (sender == cmbView2) _panel2Tool = (AnalysisTool)cmbView2.SelectedItem;
            UpdateComboBoxes();
            panelDrawingSurface.Invalidate();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the comboBoxStandards control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ComboBoxStandards_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedStandard = (LoudnessStandard)comboBoxStandards.SelectedItem;
            ResetLoudnessAnalysis();
        }

        /// <summary>
        /// Handles the Click event of the btnResetLoudness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnResetLoudness_Click(object sender, EventArgs e) => ResetLoudnessAnalysis();

        /// <summary>
        /// Recalculates the positions of all controls dynamically based on form size.
        /// </summary>
        private void UpdateControlLayout()
        {
            this.SuspendLayout();

            int topOffset = panelSettings.Height;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height - topOffset;

            int availH = h - (LayoutConstants.PADDING * 4);
            int hWave = (int)(availH * LayoutConstants.RATIO_WAVEFORM);
            int hAnalysis = (int)(availH * LayoutConstants.RATIO_ANALYSIS);
            int hStats = availH - hWave - hAnalysis;

            // These are the drawing areas *inside* the panelDrawingSurface.
            // Control positioning is relative to the form's ClientSize, not the panel's.
            Rectangle drawingArea = new Rectangle(0, 0, w, h);

            Rectangle rectAnalysis = new Rectangle(LayoutConstants.PADDING, LayoutConstants.PADDING + hWave + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hAnalysis);
            Rectangle rectStats = new Rectangle(LayoutConstants.PADDING, rectAnalysis.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hStats);

            // Position dynamic controls inside the analysis panel.
            int controlBarHeight = 30;
            Rectangle rectAnalysisControls = new Rectangle(rectAnalysis.X, panelDrawingSurface.Top + rectAnalysis.Y, rectAnalysis.Width, controlBarHeight);

            cmbView1.Location = new Point(rectAnalysisControls.X + 150, rectAnalysisControls.Y + 5);
            cmbView2.Location = new Point(rectAnalysisControls.Right - cmbView2.Width - 5, rectAnalysisControls.Y + 5);

            int labelPadding = 8;
            lblSplitLeft.Location = new Point(cmbView1.Right + labelPadding, rectAnalysisControls.Y + 7);

            int sliderX = lblSplitLeft.Right + labelPadding;

            lblSplitRight.Location = new Point(cmbView2.Left - lblSplitRight.Width - labelPadding, rectAnalysisControls.Y + 7);

            int sliderWidth = lblSplitRight.Left - sliderX - labelPadding;

            trackViewSplit.Location = new Point(sliderX, rectAnalysisControls.Y + 3);
            trackViewSplit.Width = sliderWidth > 0 ? sliderWidth : 1;

            // Position loudness controls inside the stats panel.
            int metersPanelWidth = CalculateMetersPanelWidth(_lastChannelCount);
            Rectangle rRight = new Rectangle(rectStats.Right - LayoutConstants.LOUDNESS_PANEL_WIDTH, panelDrawingSurface.Top + rectStats.Y, LayoutConstants.LOUDNESS_PANEL_WIDTH, rectStats.Height);

            if (rRight.Width > 20 && rRight.Height > 60)
            {
                comboBoxStandards.Location = new Point(rRight.X + 10, rRight.Y + 25);
                comboBoxStandards.Width = rRight.Width - 20;
                btnResetLoudness.Location = new Point(rRight.X + 10, rRight.Bottom - 30);
                btnResetLoudness.Width = rRight.Width - 20;
            }

            this.ResumeLayout(true);
        }

        /// <summary>
        /// Updates the text labels for the setting sliders.
        /// </summary>
        private void UpdateSettingsLabels()
        {
            lblSmoothing.Text = $"Smoothing: {Math.Round(_smoothingFactor * 100)}%";
            lblPeakHold.Text = $"Peak Hold: {_peakHoldTimeMs}ms";
        }

        /// <summary>
        /// Updates the percentage labels for the view split slider.
        /// </summary>
        private void UpdateSplitLabels()
        {
            lblSplitLeft.Text = $"{trackViewSplit.Value}%";
            lblSplitRight.Text = $"{100 - trackViewSplit.Value}%";
        }

        /// <summary>
        /// Ensures that the same tool cannot be selected in both ComboBoxes.
        /// </summary>
        private void UpdateComboBoxes()
        {
            cmbView1.SelectedIndexChanged -= CmbView_SelectedIndexChanged;
            cmbView2.SelectedIndexChanged -= CmbView_SelectedIndexChanged;

            var allTools = (AnalysisTool[])Enum.GetValues(typeof(AnalysisTool));

            var availableForView2 = new List<AnalysisTool>();
            foreach (var tool in allTools) { if (tool != _panel1Tool) availableForView2.Add(tool); }
            cmbView2.DataSource = availableForView2;
            if (cmbView2.Items.Contains(_panel2Tool)) cmbView2.SelectedItem = _panel2Tool;
            else { _panel2Tool = availableForView2.Count > 0 ? availableForView2[0] : _panel1Tool; cmbView2.SelectedItem = _panel2Tool; }

            var availableForView1 = new List<AnalysisTool>();
            foreach (var tool in allTools) { if (tool != _panel2Tool) availableForView1.Add(tool); }
            cmbView1.DataSource = availableForView1;
            if (cmbView1.Items.Contains(_panel1Tool)) cmbView1.SelectedItem = _panel1Tool;
            else { _panel1Tool = availableForView1.Count > 0 ? availableForView1[0] : _panel2Tool; cmbView1.SelectedItem = _panel1Tool; }

            cmbView1.SelectedIndexChanged += CmbView_SelectedIndexChanged;
            cmbView2.SelectedIndexChanged += CmbView_SelectedIndexChanged;
        }

        #endregion

        #region 6. Core Data Processing

        /// <summary>
        /// Handles the Tick event of the renderTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void renderTimer_Tick(object sender, EventArgs e)
        {
            if (_isInitialized && !this.IsDisposed)
            {
                UpdateAnalysisData();
                panelDrawingSurface.Invalidate();
            }
        }

        /// <summary>
        /// Fetches the latest metering, FFT, and loudness data from FMOD.
        /// </summary>
        private void UpdateAnalysisData()
        {
            if (!_activeChannel.hasHandle()) return;

            // Check for volume changes and reset loudness analysis if necessary.
            float currentVolume;
            if (_activeChannel.getVolume(out currentVolume) == RESULT.OK)
            {
                if (Math.Abs(currentVolume - _lastKnownVolume) > 0.001f)
                {
                    _lastKnownVolume = currentVolume;
                    ResetLoudnessAnalysis();
                }
            }

            if (_meteringDsp.hasHandle())
            {
                _meteringDsp.getMeteringInfo(IntPtr.Zero, out _meteringOutput);
                if (_meteringOutput.numchannels > 0) _lastChannelCount = _meteringOutput.numchannels;

                int channels = Math.Min(_lastChannelCount, 32);
                for (int i = 0; i < channels; i++)
                {
                    if (i >= _meteringOutput.peaklevel.Length) break;
                    float rawPeak = _meteringOutput.peaklevel[i];
                    float rawRms = _meteringOutput.rmslevel[i];

                    if (rawPeak >= _smoothedPeak[i]) _smoothedPeak[i] = rawPeak;
                    else _smoothedPeak[i] = _smoothedPeak[i] * _smoothingFactor + rawPeak * (1.0f - _smoothingFactor);
                    if (rawRms >= _smoothedRMS[i]) _smoothedRMS[i] = rawRms;
                    else _smoothedRMS[i] = _smoothedRMS[i] * _smoothingFactor + rawRms * (1.0f - _smoothingFactor);

                    if (rawPeak > _statMaxPeak[i]) _statMaxPeak[i] = rawPeak;
                    if (rawRms > _statMaxRMS[i]) _statMaxRMS[i] = rawRms;

                    if (20.0f * (float)Math.Log10(rawRms + 1e-5) > AnalysisSettings.SILENCE_THRESHOLD_DB)
                    {
                        if (_statMinRMS[i] == 0.0f || rawRms < _statMinRMS[i]) _statMinRMS[i] = rawRms;
                    }

                    if (rawPeak >= 1.0f)
                    {
                        _statClipCount[i]++;
                        _clipResetTime[i] = DateTime.Now.Ticks + (_peakHoldTimeMs * 10000);
                    }
                }
            }

            if (_fftDsp.hasHandle())
            {
                IntPtr dataPtr; uint length;
                _fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out dataPtr, out length);
                if (dataPtr != IntPtr.Zero && length > 0)
                {
                    _fftData = (DSP_PARAMETER_FFT)Marshal.PtrToStructure(dataPtr, typeof(DSP_PARAMETER_FFT));
                    if (_fftData.numchannels > 0)
                    {
                        float[] currentSpectrum = new float[_fftData.length];
                        _fftData.getSpectrum(0, ref currentSpectrum);

                        _spectrumHistory.Add(currentSpectrum);
                        if (_spectrumHistory.Count > AnalysisSettings.SPECTRUM_HISTORY_COUNT) _spectrumHistory.RemoveAt(0);

                        _statDcOffset = currentSpectrum[0];
                    }
                }
            }

            if (_loudnessDsp.hasHandle())
            {
                IntPtr dataPtr; uint length;
                _loudnessDsp.getParameterData((int)DSP_LOUDNESS_METER.INFO, out dataPtr, out length);
                if (dataPtr != IntPtr.Zero)
                {
                    _loudnessInfo = (DSP_LOUDNESS_METER_INFO_TYPE)Marshal.PtrToStructure(dataPtr, typeof(DSP_LOUDNESS_METER_INFO_TYPE));

                    if (_loudnessInfo.momentaryloudness > _statMaxMomentaryLUFS) _statMaxMomentaryLUFS = _loudnessInfo.momentaryloudness;
                    if (_loudnessInfo.shorttermloudness > _statMaxShortTermLUFS) _statMaxShortTermLUFS = _loudnessInfo.shorttermloudness;
                    if (_loudnessInfo.maxtruepeak > _statMaxTruePeak) _statMaxTruePeak = _loudnessInfo.maxtruepeak;
                }
            }
        }

        /// <summary>
        /// Reads the entire audio buffer to generate a static waveform image.
        /// </summary>
        private void GenerateStaticWaveform()
        {
            if (!_activeSound.hasHandle()) return;
            IntPtr ptr1, ptr2; uint len1, len2;
            _activeSound.getLength(out uint lengthBytes, TIMEUNIT.PCMBYTES);
            _activeSound.getFormat(out _, out SOUND_FORMAT format, out int channels, out int bits);

            RESULT res = _activeSound.@lock(0, lengthBytes, out ptr1, out ptr2, out len1, out len2);
            if (res != RESULT.OK) return;

            int width = 2048, height = 200;
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = new Pen(AppTheme.WAVEFORM_STATIC, 1))
            {
                g.Clear(Color.Transparent);
                int bytesPerSample = bits / 8;
                if (bytesPerSample == 0) bytesPerSample = 2;

                long totalSamples = len1 / (uint)(bytesPerSample * channels);
                int samplesPerPixel = (int)Math.Max(1, totalSamples / width);

                float yCenter = height / 2f;
                float yScale = height / 2f;
                float[] fBuf = new float[1];

                for (int x = 0; x < width; x++)
                {
                    long offsetBytes = (long)x * samplesPerPixel * bytesPerSample * channels;
                    if (offsetBytes >= len1) break;

                    float sampleVal = 0.0f;
                    IntPtr readPtr = new IntPtr(ptr1.ToInt64() + offsetBytes);

                    try
                    {
                        switch (format)
                        {
                            case SOUND_FORMAT.PCM8: sampleVal = (Marshal.ReadByte(readPtr) - 128) / 128f; break;
                            case SOUND_FORMAT.PCM16: sampleVal = Marshal.ReadInt16(readPtr) / 32768f; break;
                            case SOUND_FORMAT.PCM24:
                                byte b0 = Marshal.ReadByte(readPtr), b1 = Marshal.ReadByte(readPtr, 1), b2 = Marshal.ReadByte(readPtr, 2);
                                int val24 = (b0 | (b1 << 8) | (b2 << 16));
                                if ((val24 & 0x800000) != 0) val24 |= unchecked((int)0xFF000000);
                                sampleVal = val24 / 8388608f;
                                break;
                            case SOUND_FORMAT.PCM32: sampleVal = Marshal.ReadInt32(readPtr) / 2147483648f; break;
                            case SOUND_FORMAT.PCMFLOAT: Marshal.Copy(readPtr, fBuf, 0, 1); sampleVal = fBuf[0]; break;
                        }
                    }
                    catch { break; }

                    g.DrawLine(pen, x, yCenter, x, yCenter + (sampleVal * yScale));
                }
            }
            _activeSound.unlock(ptr1, ptr2, len1, len2);
            _staticWaveformBitmap = bmp;
        }

        #endregion

        #region 7. Main Rendering Logic

        /// <summary>
        /// Handles the Paint event of the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void AudioAnalyzerForm_Paint(object sender, PaintEventArgs e)
        {
            // All drawing logic has been moved to panelDrawingSurface_Paint to prevent flickering.
        }

        /// <summary>
        /// Handles the Paint event of the main drawing panel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void panelDrawingSurface_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(AppTheme.BG);

            Panel panel = sender as Panel;
            int w = panel.ClientSize.Width;
            int h = panel.ClientSize.Height;

            if (!_isInitialized || this.IsDisposed)
            {
                DrawCenterText(g, "Audio Data Not Available", 0, h);
                return;
            }

            int availH = h - (LayoutConstants.PADDING * 4);
            int hWave = (int)(availH * LayoutConstants.RATIO_WAVEFORM);
            int hAnalysis = (int)(availH * LayoutConstants.RATIO_ANALYSIS);
            int hStats = availH - hWave - hAnalysis;

            Rectangle rectWave = new Rectangle(LayoutConstants.PADDING, LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hWave);
            Rectangle rectAnalysis = new Rectangle(LayoutConstants.PADDING, rectWave.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hAnalysis);
            Rectangle rectStats = new Rectangle(LayoutConstants.PADDING, rectAnalysis.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hStats);

            DrawTimelineAndVectorscope(g, rectWave);
            DrawAnalysisPanel(g, rectAnalysis);
            DrawDetailedStats(g, rectStats);
        }

        /// <summary>
        /// Draws the top section, which contains the Timeline and Vectorscope panels.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the section will be drawn.</param>
        private void DrawTimelineAndVectorscope(Graphics g, Rectangle bounds)
        {
            // Calculate panel rectangles.
            int vectorscopePanelSize = bounds.Height;
            Rectangle rectVectorscopePanel = new Rectangle(
                bounds.Right - vectorscopePanelSize,
                bounds.Y,
                vectorscopePanelSize,
                bounds.Height
            );
            Rectangle rectTimelinePanel = new Rectangle(
                bounds.X,
                bounds.Y,
                bounds.Width - vectorscopePanelSize - LayoutConstants.PADDING,
                bounds.Height
            );

            // Draw the Timeline Panel.
            using (Brush panelBrush = new SolidBrush(AppTheme.PANEL_BG))
            using (Pen borderPen = new Pen(AppTheme.GRID))
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.FillRectangle(panelBrush, rectTimelinePanel);
                g.DrawRectangle(borderPen, rectTimelinePanel);
                Region prev = g.Clip;
                g.SetClip(rectTimelinePanel);

                Rectangle waveArea = new Rectangle(rectTimelinePanel.X + LayoutConstants.PADDING, rectTimelinePanel.Y + 20, rectTimelinePanel.Width - LayoutConstants.PADDING * 2, rectTimelinePanel.Height - 20 - LayoutConstants.PADDING);
                if (_staticWaveformBitmap != null) g.DrawImage(_staticWaveformBitmap, waveArea);

                if (_activeChannel.hasHandle() && _totalLengthMs > 0)
                {
                    _activeChannel.getPosition(out uint positionMs, TIMEUNIT.MS);
                    float progress = _totalLengthMs > 0 ? (float)positionMs / _totalLengthMs : 0;
                    int xPos = waveArea.X + (int)(waveArea.Width * progress);

                    using (Pen playheadPen = new Pen(AppTheme.PLAYHEAD, 1))
                    {
                        g.DrawLine(playheadPen, xPos, rectTimelinePanel.Top, xPos, rectTimelinePanel.Bottom);
                    }
                    g.DrawString($"TIMELINE: {TimeSpan.FromMilliseconds(positionMs):mm\\:ss\\.fff} / {TimeSpan.FromMilliseconds(_totalLengthMs):mm\\:ss\\.fff}",
                        _fontTitle, titleBrush, rectTimelinePanel.X + 5, rectTimelinePanel.Y + 2);

                    if (!string.IsNullOrEmpty(_audioInfoString))
                    {
                        using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far })
                        {
                            g.DrawString(_audioInfoString, _fontTitle, titleBrush, rectTimelinePanel.Right - 5, rectTimelinePanel.Y + 2, sfRight);
                        }
                    }
                }
                g.Clip = prev;
            }

            // Draw the Vectorscope panel.
            DrawVectorscope(g, rectVectorscopePanel);
        }

        /// <summary>
        /// Draws the middle panel containing dynamic analysis tools.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the panel will be drawn.</param>
        private void DrawAnalysisPanel(Graphics g, Rectangle bounds)
        {
            using (Brush panelBrush = new SolidBrush(AppTheme.PANEL_BG))
            using (Pen borderPen = new Pen(AppTheme.GRID))
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.FillRectangle(panelBrush, bounds);
                g.DrawRectangle(borderPen, bounds);
                Region prev = g.Clip; g.SetClip(bounds);

                int controlBarHeight = 30;
                Rectangle rectRender = new Rectangle(bounds.X, bounds.Y + controlBarHeight, bounds.Width, bounds.Height - controlBarHeight);

                g.DrawString("ANALYSIS TOOLS", _fontTitle, titleBrush, bounds.X + 5, bounds.Y + 7);

                int splitPoint = rectRender.X + (int)(rectRender.Width * _splitRatio);

                // Define padded rectangles for each tool to create a visual border.
                int internalPadding = 5;
                Rectangle rect1 = new Rectangle(
                    rectRender.X + internalPadding,
                    rectRender.Y + internalPadding,
                    (splitPoint - rectRender.X) - internalPadding - (_splitRatio < 0.99f ? 2 : internalPadding),
                    rectRender.Height - (internalPadding * 2)
                    );

                Rectangle rect2 = new Rectangle(
                    splitPoint + (_splitRatio > 0.01f ? 2 : internalPadding),
                    rectRender.Y + internalPadding,
                    (rectRender.Right - splitPoint) - internalPadding - (_splitRatio > 0.01f ? 2 : internalPadding),
                    rectRender.Height - (internalPadding * 2)
                    );

                if (rect1.Width > 1) DrawTool(g, _panel1Tool, rect1);
                if (rect2.Width > 1) DrawTool(g, _panel2Tool, rect2);

                // Draw a separator line between the two tool views.
                if (_splitRatio > 0.01f && _splitRatio < 0.99f)
                {
                    using (Pen separatorPen = new Pen(AppTheme.GRID))
                    {
                        g.DrawLine(separatorPen, splitPoint, rectRender.Top, splitPoint, rectRender.Bottom);
                    }
                }

                g.Clip = prev;
            }
        }

        /// <summary>
        /// Draws a specific analysis tool into the given rectangle.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="tool">The analysis tool to draw.</param>
        /// <param name="bounds">The rectangular area where the tool will be drawn.</param>
        private void DrawTool(Graphics g, AnalysisTool tool, Rectangle bounds)
        {
            switch (tool)
            {
                case AnalysisTool.Oscilloscope: DrawRealtimeOscilloscope(g, bounds); break;
                case AnalysisTool.Spectrum: DrawSpectrum(g, bounds); break;
                case AnalysisTool.Spectrogram: DrawSpectrogram(g, bounds, _currentSampleRate / 2.0f); break;
            }
        }

        /// <summary>
        /// Draws the detailed statistics panel including meters, numeric stats, and loudness info.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the statistics will be drawn.</param>
        private void DrawDetailedStats(Graphics g, Rectangle bounds)
        {
            using (Brush panelBrush = new SolidBrush(AppTheme.PANEL_BG))
            using (Pen borderPen = new Pen(AppTheme.GRID))
            using (Pen separatorPen = new Pen(AppTheme.GRID))
            {
                g.FillRectangle(panelBrush, bounds);
                g.DrawRectangle(borderPen, bounds);
                Region prev = g.Clip; g.SetClip(bounds);

                int metersPanelWidth = CalculateMetersPanelWidth(_lastChannelCount);

                Rectangle rMeters = new Rectangle(bounds.X, bounds.Y, metersPanelWidth, bounds.Height);
                Rectangle rRight = new Rectangle(bounds.Right - LayoutConstants.LOUDNESS_PANEL_WIDTH, bounds.Y, LayoutConstants.LOUDNESS_PANEL_WIDTH, bounds.Height);
                Rectangle rStats = new Rectangle(rMeters.Right, bounds.Y, rRight.Left - rMeters.Right, bounds.Height);

                g.DrawLine(separatorPen, rMeters.Right, bounds.Top + 10, rMeters.Right, bounds.Bottom - 10);
                g.DrawLine(separatorPen, rStats.Right, bounds.Top + 10, rStats.Right, bounds.Bottom - 10);

                DrawVerticalMeters(g, rMeters, _lastChannelCount);
                DrawStatsTable(g, rStats);
                DrawLoudnessPanel(g, rRight);

                g.Clip = prev;
            }
        }

        #endregion

        #region 8. Sub-Renderers

        /// <summary>
        /// Draws the real-time frequency spectrum analyzer.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the spectrum will be drawn.</param>
        private void DrawSpectrum(Graphics g, Rectangle bounds)
        {
            Region prev = g.Clip; g.SetClip(bounds);

            int yAxisWidth = 35;
            int xAxisHeight = 20;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - xAxisHeight - topPadding);

            if (graphRect.Width <= 0 || graphRect.Height <= 0) { g.Clip = prev; return; }

            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            using (Pen gridPen = new Pen(AppTheme.GRID) { DashStyle = DashStyle.Dot })
            using (Brush axisBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center })
            {
                // Draw Y-Axis (dB) with standard audio level markers.
                float dbRange = AnalysisSettings.SPECTRUM_MAX_DB - AnalysisSettings.SPECTRUM_MIN_DB;
                float[] dbMarkers = { 0, -6, -12, -24, -36, -48, -60 };

                foreach (float db in dbMarkers)
                {
                    if (db > AnalysisSettings.SPECTRUM_MAX_DB || db < AnalysisSettings.SPECTRUM_MIN_DB) continue;
                    int y = graphRect.Top + (int)((AnalysisSettings.SPECTRUM_MAX_DB - db) / dbRange * graphRect.Height);
                    g.DrawLine(gridPen, graphRect.Left, y, graphRect.Right, y);
                    g.DrawString($"{db:F0}dB", _fontAxis, axisBrush, graphRect.Left - 4, y, sfRight);
                }

                // Draw X-Axis (Frequency) on a logarithmic scale.
                float nyquist = _currentSampleRate / 2.0f;
                float[] freqMarkers = { 100, 1000, 5000, 10000, 20000 };
                foreach (float freq in freqMarkers)
                {
                    if (freq > nyquist || freq < 20.0f) continue;
                    float xRatio = (float)(Math.Log10(freq / 20.0) / Math.Log10(nyquist / 20.0));
                    int xPos = graphRect.Left + (int)(graphRect.Width * xRatio);
                    g.DrawLine(gridPen, xPos, graphRect.Top, xPos, graphRect.Bottom);
                    string label = (freq >= 1000) ? $"{freq / 1000}k" : $"{freq}";
                    g.DrawString(label, _fontAxis, axisBrush, xPos, graphRect.Bottom + 2, sfCenter);
                }
            }

            // Draw the spectrum bars.
            if (_fftData.length > 0)
            {
                int numBins = _fftData.length / 2;
                float[] spectrum = new float[_fftData.length];
                if (_fftData.numchannels > 0) _fftData.getSpectrum(0, ref spectrum);

                for (int x = 0; x < graphRect.Width; x++)
                {
                    float xRatio = (float)x / graphRect.Width;
                    float freq = 20.0f * (float)Math.Pow(_currentSampleRate / 2.0 / 20.0, xRatio);
                    int bin = (int)(freq * _fftData.length / _currentSampleRate);

                    if (bin >= numBins) continue;

                    float db = 20 * (float)Math.Log10(spectrum[bin] + 1e-9);
                    float dbRange = AnalysisSettings.SPECTRUM_MAX_DB - AnalysisSettings.SPECTRUM_MIN_DB;
                    float yRatio = (db - AnalysisSettings.SPECTRUM_MIN_DB) / dbRange;
                    if (yRatio < 0) yRatio = 0;

                    float h = yRatio * graphRect.Height;

                    if (h > 1)
                    {
                        float barX = graphRect.Left + x;
                        using (Brush b = new LinearGradientBrush(new PointF(barX, graphRect.Bottom), new PointF(barX, graphRect.Bottom - h), AppTheme.FFT_BOTTOM, AppTheme.FFT_TOP))
                        {
                            g.FillRectangle(b, barX, graphRect.Bottom - h, 1, h);
                        }
                    }
                }
            }
            g.Clip = prev;
        }

        /// <summary>
        /// Draws the real-time spectrogram.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the spectrogram will be drawn.</param>
        /// <param name="nyquist">The Nyquist frequency of the audio.</param>
        private void DrawSpectrogram(Graphics g, Rectangle bounds, float nyquist)
        {
            Region prev = g.Clip; g.SetClip(bounds);

            int yAxisWidth = 35;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - topPadding * 2);

            if (graphRect.Width <= 0 || graphRect.Height <= 0) { g.Clip = prev; return; }

            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            if (_spectrogramBitmap == null || _spectrogramBitmap.Width != graphRect.Width || _spectrogramBitmap.Height != graphRect.Height)
            {
                _spectrogramBitmap?.Dispose();
                _spectrogramBitmap = new Bitmap(graphRect.Width, graphRect.Height);
            }

            // Shift the existing spectrogram image one pixel to the left.
            using (Graphics bmpG = Graphics.FromImage(_spectrogramBitmap))
            {
                bmpG.DrawImage(_spectrogramBitmap, new Rectangle(-1, 0, _spectrogramBitmap.Width, _spectrogramBitmap.Height));
            }

            // Draw the newest spectrum data on the far right column.
            if (_spectrumHistory.Count > 0)
            {
                float[] latestSpectrum = _spectrumHistory[_spectrumHistory.Count - 1];
                int numBins = latestSpectrum.Length / 2;

                for (int y = 0; y < graphRect.Height; y++)
                {
                    double logY = 1.0 - (double)y / graphRect.Height;
                    double freq = 20 * Math.Pow(nyquist / 20, logY);
                    int binIndex = (int)(freq * _fftData.length / _currentSampleRate);

                    if (binIndex >= 0 && binIndex < numBins)
                    {
                        float magnitude = latestSpectrum[binIndex];
                        _spectrogramBitmap.SetPixel(graphRect.Width - 1, y, GetColorForMagnitude(magnitude));
                    }
                }
            }
            g.DrawImage(_spectrogramBitmap, graphRect);

            // Draw the Y-Axis (Frequency) grid and labels.
            using (Pen gridPen = new Pen(AppTheme.GRID) { DashStyle = DashStyle.Dot })
            using (Brush axisBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                float[] freqMarkers = { 100, 500, 1000, 5000, 10000, 20000 };
                foreach (float freq in freqMarkers)
                {
                    if (freq > nyquist || freq < 20.0f) continue;
                    double logRatio = Math.Log(freq / 20.0) / Math.Log(nyquist / 20.0);
                    int yPos = graphRect.Top + (int)((1.0 - logRatio) * graphRect.Height);
                    g.DrawLine(gridPen, graphRect.Left, yPos, graphRect.Right, yPos);
                    string label = (freq >= 1000) ? $"{freq / 1000}kHz" : $"{freq}Hz";
                    g.DrawString(label, _fontAxis, axisBrush, graphRect.Left - 4, yPos, sfRight);
                }
            }
            g.Clip = prev;
        }

        /// <summary>
        /// Draws the real-time oscilloscope.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the oscilloscope will be drawn.</param>
        private void DrawRealtimeOscilloscope(Graphics g, Rectangle bounds)
        {
            Region prev = g.Clip; g.SetClip(bounds);

            int yAxisWidth = 35;
            int xAxisHeight = 20;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - xAxisHeight - topPadding);

            if (graphRect.Width <= 0 || graphRect.Height <= 0) { g.Clip = prev; return; }

            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Draw the axis grid and labels.
            using (Pen gridPen = new Pen(AppTheme.GRID) { DashStyle = DashStyle.Dot })
            using (Brush axisBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                // Draw Y-Axis (Amplitude).
                float[] ampLevels = { 1.0f, 0.5f, 0, -0.5f, -1.0f };
                foreach (float amp in ampLevels)
                {
                    int y = graphRect.Top + (int)((-amp + 1.0f) / 2.0f * graphRect.Height);
                    g.DrawLine(gridPen, graphRect.Left, y, graphRect.Right, y);
                    g.DrawString(amp.ToString("F1"), _fontAxis, axisBrush, graphRect.Left - 4, y, sfRight);
                }
                // Draw X-Axis Label.
                g.DrawString("Samples", _fontAxis, axisBrush, graphRect.Left + graphRect.Width / 2, graphRect.Bottom + 2, new StringFormat { Alignment = StringAlignment.Center });
            }

            // Draw the waveform.
            if (_spectrumHistory.Count > 0)
            {
                float[] spectrum = _spectrumHistory[_spectrumHistory.Count - 1];
                int pointsToDraw = Math.Min(graphRect.Width, spectrum.Length);
                PointF[] points = new PointF[pointsToDraw];
                float yCenter = graphRect.Top + graphRect.Height / 2.0f;
                float yScale = graphRect.Height / 2.0f;

                for (int i = 0; i < pointsToDraw; i++)
                {
                    float x = graphRect.Left + (float)i / (pointsToDraw - 1) * graphRect.Width;
                    float y = yCenter - (spectrum[i] * yScale);
                    points[i] = new PointF(x, y);
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen wavePen = new Pen(AppTheme.OSCILLOSCOPE, 1.5f))
                {
                    g.DrawLines(wavePen, points);
                }
                g.SmoothingMode = SmoothingMode.Default;
            }
            g.Clip = prev;
        }

        /// <summary>
        /// Draws the real-time vectorscope.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the vectorscope will be drawn.</param>
        private void DrawVectorscope(Graphics g, Rectangle bounds)
        {
            // Draw the panel's main background and border.
            using (Brush panelBrush = new SolidBrush(AppTheme.PANEL_BG))
            using (Pen borderPen = new Pen(AppTheme.GRID))
            {
                g.FillRectangle(panelBrush, bounds);
                g.DrawRectangle(borderPen, bounds);
            }

            Region prev = g.Clip;
            g.SetClip(bounds);

            // Define the inner black rectangle for the graph content.
            Rectangle graphRect = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2);
            if (graphRect.Width <= 0 || graphRect.Height <= 0) { g.Clip = prev; return; }

            // Fill the graph area with a black background.
            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Calculate geometry based on the inner graph rectangle.
            PointF center = new PointF(graphRect.X + graphRect.Width / 2.0f, graphRect.Top + graphRect.Height / 2.0f);
            float radius = Math.Min(graphRect.Width, graphRect.Height) / 2.0f;

            // Draw the grid lines first for proper layering.
            using (Pen gridPen = new Pen(AppTheme.GRID))
            {
                g.DrawEllipse(gridPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                g.DrawLine(gridPen, center.X, graphRect.Top, center.X, graphRect.Bottom);
                g.DrawLine(gridPen, graphRect.Left, center.Y, graphRect.Right, center.Y);
                g.DrawLine(gridPen, graphRect.Left, graphRect.Top, graphRect.Right, graphRect.Bottom);
                g.DrawLine(gridPen, graphRect.Left, graphRect.Bottom, graphRect.Right, graphRect.Top);
            }

            // Draw the title string on top of the grid.
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("VECTORSCOPE", _fontTitle, titleBrush, bounds.X + 5, bounds.Y);
            }

            // Draw the real-time vectorscope data.
            if (_fftData.numchannels >= 2 && _spectrumHistory.Count > 0)
            {
                float[] spectrumL = new float[_fftData.length];
                float[] spectrumR = new float[_fftData.length];
                _fftData.getSpectrum(0, ref spectrumL);
                _fftData.getSpectrum(1, ref spectrumR);

                int pointsToDraw = 256;
                PointF[] points = new PointF[pointsToDraw];
                float scale = radius * 4.0f;

                for (int i = 0; i < pointsToDraw; i++)
                {
                    float l = spectrumL[i];
                    float r = spectrumR[i];
                    float x = (l - r) * scale;
                    float y = (l + r) * scale;
                    points[i] = new PointF(center.X + x, center.Y - y);
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen scopePen = new Pen(AppTheme.VECTORSCOPE, 1.0f))
                {
                    g.DrawLines(scopePen, points);
                }
                g.SmoothingMode = SmoothingMode.Default;
            }
            else if (_lastChannelCount < 2)
            {
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Brush infoBrush = new SolidBrush(AppTheme.LABEL))
                {
                    g.DrawString("Stereo Required", _fontAxis, infoBrush, graphRect, sf);
                }
            }
            g.Clip = prev;
        }

        #endregion

        #region 9. Rendering Helpers & Utilities

        /// <summary>
        /// Draws centered text on the canvas.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="msg">The message string to display.</param>
        /// <param name="offset">The vertical offset to start drawing from.</param>
        /// <param name="height">The height of the drawing area.</param>
        private void DrawCenterText(Graphics g, string msg, int offset, int height)
        {
            using (Font font = new Font("Segoe UI", 10f))
            using (Brush textBrush = new SolidBrush(AppTheme.LABEL))
            {
                SizeF size = g.MeasureString(msg, font);
                g.DrawString(msg, font, textBrush, (this.panelDrawingSurface.Width - size.Width) / 2, offset + (height - size.Height) / 2);
            }
        }

        /// <summary>
        /// Calculates the required width for the Meters panel based on channel count.
        /// </summary>
        /// <param name="channels">The number of audio channels.</param>
        /// <returns>The calculated width in pixels.</returns>
        private int CalculateMetersPanelWidth(int channels)
        {
            int channelsToCalc = Math.Max(2, channels);
            const int RULER_AREA_WIDTH = 35, METER_WIDTH = 20, METER_SPACING = 15, PANEL_HORIZONTAL_PADDING = 25;
            int totalMetersWidth = (channelsToCalc * METER_WIDTH) + Math.Max(0, channelsToCalc - 1) * METER_SPACING;
            return RULER_AREA_WIDTH + totalMetersWidth + PANEL_HORIZONTAL_PADDING;
        }

        /// <summary>
        /// Draws vertical volume meters for each audio channel.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the meters will be drawn.</param>
        /// <param name="channels">The number of channels to draw.</param>
        private void DrawVerticalMeters(Graphics g, Rectangle bounds, int channels)
        {
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("METERS", _fontTitle, titleBrush, bounds.X + 5, bounds.Y + 5);
            }
            if (channels == 0) return;

            int meterAreaH = bounds.Height - 50;
            int meterAreaY = bounds.Y + 35;
            int meterW = 20, meterSpacing = 15;
            int totalMeterW = (channels * meterW) + ((channels - 1) * meterSpacing);
            int startX = bounds.X + (bounds.Width - totalMeterW - 35) / 2 + 25;

            float[] dbTicks = { 0, -6, -12, -24, -36, -48, -60 };
            int rulerX = startX - 25;
            using (Pen gridPen = new Pen(AppTheme.GRID))
            using (Brush textBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                foreach (float db in dbTicks)
                {
                    float ratio = Math.Max(0, Math.Min(1, (db - AnalysisSettings.METER_MIN_DB) / (0 - AnalysisSettings.METER_MIN_DB)));
                    int yPos = meterAreaY + meterAreaH - (int)(ratio * meterAreaH);
                    g.DrawString($"{db}", _fontAxis, textBrush, rulerX + 20, yPos, sfRight);
                    g.DrawLine(gridPen, rulerX + 22, yPos, startX + totalMeterW, yPos);
                }
            }

            for (int i = 0; i < channels; i++)
            {
                int xPos = startX + (i * (meterW + meterSpacing));
                Rectangle meterRect = new Rectangle(xPos, meterAreaY, meterW, meterAreaH);
                using (Brush meterBgBrush = new SolidBrush(AppTheme.METER_BG))
                {
                    g.FillRectangle(meterBgBrush, meterRect);
                }

                float rms = 0, peak = 0;
                if (_smoothedRMS != null && i < _smoothedRMS.Length) rms = _smoothedRMS[i];
                if (_smoothedPeak != null && i < _smoothedPeak.Length) peak = _smoothedPeak[i];

                float rmsDb = 20.0f * (float)Math.Log10(rms + 1e-5);
                float peakDb = 20.0f * (float)Math.Log10(peak + 1e-5);
                float rmsRatio = Math.Max(0, Math.Min(1, (rmsDb - AnalysisSettings.METER_MIN_DB) / (0 - AnalysisSettings.METER_MIN_DB)));
                float peakRatio = Math.Max(0, Math.Min(1, (peakDb - AnalysisSettings.METER_MIN_DB) / (0 - AnalysisSettings.METER_MIN_DB)));
                int rmsH = (int)(rmsRatio * meterAreaH);
                int rmsY = meterAreaY + meterAreaH - rmsH;

                Color barColor = AppTheme.METER_LOW;
                if (rmsDb > -6) barColor = AppTheme.METER_MID;
                if (rmsDb > 0) barColor = AppTheme.METER_HIGH;

                using (Brush barBrush = new SolidBrush(barColor))
                {
                    if (rmsH > 0) g.FillRectangle(barBrush, xPos, rmsY, meterW, rmsH);
                }

                int peakY = meterAreaY + meterAreaH - (int)(peakRatio * meterAreaH);
                using (Pen peakPen = new Pen(AppTheme.VAL_NORMAL))
                {
                    g.DrawLine(peakPen, xPos, peakY, xPos + meterW, peakY);
                }

                int clipH = 4;
                Rectangle clipBox = new Rectangle(xPos, meterAreaY - clipH - 2, meterW, clipH);
                bool showClip = (_clipResetTime != null && _clipResetTime.Length > i && DateTime.Now.Ticks < _clipResetTime[i]);
                using (Brush clipBrush = new SolidBrush(showClip ? AppTheme.CLIP_ON : AppTheme.CLIP_OFF))
                {
                    g.FillRectangle(clipBrush, clipBox);
                }

                using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
                {
                    g.DrawString($"Ch{i + 1}", _fontAxis, labelBrush, xPos, meterAreaY + meterAreaH + 2);
                }
            }
        }

        /// <summary>
        /// Draws the table of channel statistics.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the table will be drawn.</param>
        private void DrawStatsTable(Graphics g, Rectangle bounds)
        {
            if (bounds.Width < 150) return;
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("CHANNEL STATISTICS", _fontTitle, titleBrush, bounds.X + 10, bounds.Y + 5);
            }

            int startY = bounds.Y + 45;
            int rowH = 18;
            int startX = bounds.X + 10;
            int totalW = bounds.Width - 20;

            int numChannels = _lastChannelCount;
            int channelsToDisplay = Math.Min(numChannels, 8);
            if (channelsToDisplay == 0) return;

            DrawStatHeader(g, startX, startY - rowH, totalW, channelsToDisplay);

            string[] peakVals = new string[channelsToDisplay];
            string[] maxRmsVals = new string[channelsToDisplay];
            string[] minRmsVals = new string[channelsToDisplay];
            string[] currentRmsVals = new string[channelsToDisplay];
            string[] clipVals = new string[channelsToDisplay];

            for (int i = 0; i < channelsToDisplay; i++)
            {
                peakVals[i] = FormatDb((_statMaxPeak != null && _statMaxPeak.Length > i) ? _statMaxPeak[i] : 0);
                maxRmsVals[i] = FormatDb((_statMaxRMS != null && _statMaxRMS.Length > i) ? _statMaxRMS[i] : 0);
                minRmsVals[i] = FormatDb((_statMinRMS != null && _statMinRMS.Length > i) ? _statMinRMS[i] : 0);
                currentRmsVals[i] = FormatDb((_smoothedRMS != null && _smoothedRMS.Length > i) ? _smoothedRMS[i] : 0);
                clipVals[i] = (_statClipCount != null && _statClipCount.Length > i) ? _statClipCount[i].ToString() : "0";
            }

            DrawStatDataRow(g, startX, startY + (rowH * 0), totalW, "Sample Peak Max", peakVals, checkPeak: true);
            DrawStatDataRow(g, startX, startY + (rowH * 1), totalW, "Max RMS", maxRmsVals);
            DrawStatDataRow(g, startX, startY + (rowH * 2), totalW, "Min RMS", minRmsVals);
            DrawStatDataRow(g, startX, startY + (rowH * 3), totalW, "Current RMS", currentRmsVals);
            DrawStatDataRow(g, startX, startY + (rowH * 4), totalW, "Clipped Samples", clipVals, checkClip: true);
            DrawStatRowDynamic(g, y: startY + (rowH * 5),
                cols: new[] { startX, startX + (int)(totalW * 0.45f) },
                vals: new[] { "DC Offset", $"{_statDcOffset * 100:F4}%" });
        }

        /// <summary>
        /// Draws the header row for the statistics table.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="x">The starting X coordinate.</param>
        /// <param name="y">The starting Y coordinate.</param>
        /// <param name="width">The total width of the table.</param>
        /// <param name="numChannels">The number of channels to display.</param>
        private void DrawStatHeader(Graphics g, int x, int y, int width, int numChannels)
        {
            int metricWidth = (int)(width * 0.45f);
            if (numChannels > 4) metricWidth = (int)(width * 0.35f);
            int valueWidth = (width - metricWidth) / numChannels;

            using (Brush headerBrush = new SolidBrush(AppTheme.VAL_NORMAL))
            {
                g.DrawString("METRIC", _fontTitle, headerBrush, x, y);

                for (int i = 0; i < numChannels; i++)
                {
                    int colX = x + metricWidth + (i * valueWidth);
                    string chLabel = GetChannelLabel(i, numChannels);
                    g.DrawString(chLabel, _fontTitle, headerBrush, colX, y);
                }
            }
        }

        /// <summary>
        /// Draws a single data row for the statistics table.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="x">The starting X coordinate.</param>
        /// <param name="y">The starting Y coordinate.</param>
        /// <param name="width">The total width of the table.</param>
        /// <param name="metric">The name of the metric being displayed.</param>
        /// <param name="values">The array of string values for each channel.</param>
        /// <param name="checkPeak">A flag to indicate if peak values should be color-coded.</param>
        /// <param name="checkClip">A flag to indicate if clip counts should be color-coded.</param>
        private void DrawStatDataRow(Graphics g, int x, int y, int width, string metric, string[] values, bool checkPeak = false, bool checkClip = false)
        {
            int numChannels = values.Length;
            int metricWidth = (int)(width * 0.45f);
            if (numChannels > 4) metricWidth = (int)(width * 0.35f);
            int valueWidth = (width - metricWidth) / numChannels;

            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(metric, _fontLabel, labelBrush, x, y);
            }

            for (int i = 0; i < numChannels; i++)
            {
                Color valColor = AppTheme.VAL_NORMAL;
                if (checkPeak && ParseDb(values[i]) >= 0) valColor = AppTheme.VAL_DANGER;
                if (checkClip && int.Parse(values[i]) > 0) valColor = AppTheme.VAL_WARNING;

                using (Brush bVal = new SolidBrush(valColor))
                {
                    int colX = x + metricWidth + (i * valueWidth);
                    g.DrawString(values[i], _fontValue, bVal, colX, y);
                }
            }
        }

        /// <summary>
        /// Draws the loudness information panel.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the panel will be drawn.</param>
        private void DrawLoudnessPanel(Graphics g, Rectangle bounds)
        {
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("LOUDNESS", _fontTitle, titleBrush, bounds.X + 10, bounds.Y + 5);
            }

            int startY = bounds.Y + 55;
            int rowH = 18;
            int startX = bounds.X + 10;
            int valX = startX + (int)(bounds.Width * 0.5f);

            var colorIntegrated = AppTheme.VAL_NORMAL;
            var colorTruePeak = AppTheme.VAL_NORMAL;
            string feedbackText = " ";
            Color feedbackColor = AppTheme.LABEL;

            if (_selectedStandard.TargetIntegratedLoudness.HasValue)
            {
                float targetLoudness = _selectedStandard.TargetIntegratedLoudness.Value;
                float measuredLoudness = _loudnessInfo.integratedloudness;
                float diff = measuredLoudness - targetLoudness;

                if (Math.Abs(diff) <= 0.5f)
                {
                    colorIntegrated = AppTheme.VAL_OK;
                    feedbackText = "Loudness: OK";
                    feedbackColor = AppTheme.VAL_OK;
                }
                else
                {
                    colorIntegrated = AppTheme.VAL_WARNING;
                    feedbackText = $"Loudness: {(diff > 0 ? "+" : "")}{diff:F1} LU";
                    feedbackColor = AppTheme.VAL_WARNING;
                }
            }
            else { feedbackText = "Absolute Scale"; }

            if (_selectedStandard.MaxTruePeak.HasValue && _statMaxTruePeak > _selectedStandard.MaxTruePeak.Value)
            {
                colorTruePeak = AppTheme.VAL_DANGER;
            }

            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 0), "Integrated", $"{_loudnessInfo.integratedloudness:F1} LUFS", colorIntegrated);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 1), "Short-Term Max", $"{_statMaxShortTermLUFS:F1} LUFS", AppTheme.VAL_NORMAL);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 2), "Momentary Max", $"{_statMaxMomentaryLUFS:F1} LUFS", AppTheme.VAL_NORMAL);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 3), "True Peak Max", $"{_statMaxTruePeak:F2} dBTP", colorTruePeak);

            using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center })
            using (Brush feedbackBrush = new SolidBrush(feedbackColor))
            {
                g.DrawString(feedbackText, _fontTitle, feedbackBrush, bounds.X + (bounds.Width / 2), bounds.Bottom - 55, sfCenter);
            }
        }

        /// <summary>
        /// Draws a single row of dynamic statistics with label and value columns.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="y">The Y coordinate of the row.</param>
        /// <param name="cols">An array of X coordinates for each column.</param>
        /// <param name="vals">An array of string values for each column.</param>
        private void DrawStatRowDynamic(Graphics g, int y, int[] cols, string[] vals)
        {
            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(vals[0], _fontLabel, labelBrush, cols[0], y);
            }
            if (vals.Length > 1)
            {
                using (Brush valueBrush = new SolidBrush(AppTheme.VAL_NORMAL))
                {
                    g.DrawString(vals[1], _fontValue, valueBrush, cols[1], y);
                }
            }
        }

        /// <summary>
        /// Draws a row of loudness data with specific coloring for the value.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="labelX">The X coordinate for the label.</param>
        /// <param name="valX">The X coordinate for the value.</param>
        /// <param name="y">The Y coordinate for the row.</param>
        /// <param name="label">The label text.</param>
        /// <param name="val">The value text.</param>
        /// <param name="valColor">The color for the value text.</param>
        private void DrawLoudnessRowDynamic(Graphics g, int labelX, int valX, int y, string label, string val, Color valColor)
        {
            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(label, _fontLabel, labelBrush, labelX, y);
            }
            using (Brush valBrush = new SolidBrush(valColor))
            {
                g.DrawString(val, _fontValue, valBrush, valX, y);
            }
        }

        /// <summary>
        /// Gets a standard label for a given channel index.
        /// </summary>
        /// <param name="channelIndex">The index of the channel.</param>
        /// <param name="totalChannels">The total number of channels.</param>
        /// <returns>A string label for the channel.</returns>
        private string GetChannelLabel(int channelIndex, int totalChannels)
        {
            if (totalChannels == 1) return "Mono";
            if (totalChannels == 2) return (channelIndex == 0) ? "L" : "R";
            if (totalChannels == 6)
            {
                string[] labels = { "L", "R", "C", "LFE", "SL", "SR" };
                return (channelIndex < labels.Length) ? labels[channelIndex] : $"Ch{channelIndex + 1}";
            }
            if (totalChannels == 8)
            {
                string[] labels = { "L", "R", "C", "LFE", "BL", "BR", "SL", "SR" };
                return (channelIndex < labels.Length) ? labels[channelIndex] : $"Ch{channelIndex + 1}";
            }
            return $"Ch{channelIndex + 1}";
        }

        /// <summary>
        /// Formats a linear amplitude value to a dB string.
        /// </summary>
        /// <param name="lin">The linear amplitude value (0.0 to 1.0).</param>
        /// <returns>The value formatted as a decibel string.</returns>
        private string FormatDb(float lin)
        {
            if (lin <= 0) return "-inf dB";
            float db = 20.0f * (float)Math.Log10(lin);
            return $"{db:F2} dB";
        }

        /// <summary>
        /// Parses a dB string back to a float value.
        /// </summary>
        /// <param name="dbStr">The decibel string to parse.</param>
        /// <returns>The parsed float value.</returns>
        private float ParseDb(string dbStr)
        {
            if (dbStr.StartsWith("-inf")) return -999;
            string num = dbStr.Replace(" dB", "");
            float.TryParse(num, out float result);
            return result;
        }

        /// <summary>
        /// Gets a color for the spectrogram based on signal magnitude.
        /// </summary>
        /// <param name="magnitude">The signal magnitude from the FFT.</param>
        /// <returns>A color representing the magnitude.</returns>
        private Color GetColorForMagnitude(float magnitude)
        {
            float db = 20 * (float)Math.Log10(magnitude + 1e-9);
            float normalized = Math.Max(0, Math.Min(1, (db + 80) / 80));

            if (normalized < 0.25f) return Color.FromArgb(0, 0, (int)(normalized * 4 * 255));
            if (normalized < 0.5f) return Color.FromArgb(0, (int)((normalized - 0.25f) * 4 * 255), 255);
            if (normalized < 0.75f) return Color.FromArgb((int)((normalized - 0.5f) * 4 * 255), 255, (int)(255 - (normalized - 0.5f) * 4 * 255));
            return Color.FromArgb(255, (int)(255 - (normalized - 0.75f) * 4 * 255), 0);
        }

        #endregion
    }
}