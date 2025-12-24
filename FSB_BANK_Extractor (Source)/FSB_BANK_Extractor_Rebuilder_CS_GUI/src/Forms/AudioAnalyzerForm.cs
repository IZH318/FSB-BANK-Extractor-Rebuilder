/**
 * @file AudioAnalyzerForm.cs
 * @brief Provides a real-time audio visualization and analysis interface using the FMOD audio engine.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form renders a static waveform, frequency spectrum, spectrogram, vectorscope, and oscilloscope
 * for a playing audio stream in real-time. It also provides detailed loudness statistics (LUFS, True Peak)
 * by leveraging FMOD's Digital Signal Processing (DSP) units for FFT and Loudness Metering. The loudness
 * analysis strictly follows standards like EBU R 128 and ATSC A/85.
 *
 * Key Features:
 *  - Dynamic Analysis Panel: Features a split-view for displaying multiple analysis tools simultaneously.
 *  - Real-time Visualizers: Includes FFT Spectrum, Spectrogram, Oscilloscope, and Vectorscope.
 *  - Comprehensive Loudness Metering: Measures Momentary, Short-Term, and Integrated loudness, along with True Peak levels.
 *  - Configurable Metering Ballistics: Allows user adjustment of RMS smoothing and Peak hold time for tailored visual feedback.
 *  - Measurement Integrity: Automatically resets loudness analysis when the source volume changes to prevent data corruption and ensure accurate readings.
 *  - Compliance Checks: Validates loudness against various broadcasting standards.
 *  - Per-Channel Statistics: Provides detailed stats including RMS, Peak, and Clip detection for each channel.
 *
 * Technical Environment:
 *  - FMOD Engine Version: v2.03.11 (Studio API minor release, build 158528)
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
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
    /// <summary>
    /// Represents the main window for visualizing audio data and analyzing loudness statistics.
    /// </summary>
    public partial class AudioAnalyzerForm : Form
    {
        #region 1. Constants & Configuration

        /// <summary>
        /// Contains constants related to audio analysis parameters and visual thresholds.
        /// </summary>
        private static class AnalysisSettings
        {
            /// <summary>
            /// The size of the FFT window used for spectrum analysis.
            /// Higher values provide better frequency resolution but lower time resolution.
            /// This is also the size of the buffer for the oscilloscope wave data.
            /// </summary>
            public const int FFT_WINDOW_SIZE = 4096;

            /// <summary>
            /// The threshold in decibels below which the signal is considered silent.
            /// </summary>
            public const float SILENCE_THRESHOLD_DB = -90.0f;

            /// <summary>
            /// The minimum decibel value displayed on volume meters.
            /// </summary>
            public const float METER_MIN_DB = -60.0f;

            /// <summary>
            /// The number of historical spectrum snapshots to keep for rendering the spectrogram.
            /// </summary>
            public const int SPECTRUM_HISTORY_COUNT = 512;

            /// <summary>
            /// The minimum decibel value for the spectrum analyzer's Y-axis.
            /// </summary>
            public const float SPECTRUM_MIN_DB = -72.0f;

            /// <summary>
            /// The maximum decibel value for the spectrum analyzer's Y-axis.
            /// </summary>
            public const float SPECTRUM_MAX_DB = 6.0f;
        }

        /// <summary>
        /// Contains constants related to the visual layout of the form.
        /// </summary>
        private static class LayoutConstants
        {
            /// <summary>
            /// The proportional height of the waveform panel.
            /// </summary>
            public const float RATIO_WAVEFORM = 0.20f;

            /// <summary>
            /// The proportional height of the analysis tools panel.
            /// </summary>
            public const float RATIO_ANALYSIS = 0.35f;

            /// <summary>
            /// The proportional height of the statistics panel.
            /// </summary>
            public const float RATIO_STATS = 0.45f;

            /// <summary>
            /// The padding used between UI elements.
            /// </summary>
            public const int PADDING = 8;

            /// <summary>
            /// The fixed width of the loudness information panel.
            /// </summary>
            public const int LOUDNESS_PANEL_WIDTH = 200;
        }

        /// <summary>
        /// Defines the color palette and fonts used throughout the application theme.
        /// </summary>
        private static class AppTheme
        {
            // Panel and background colors.
            public static readonly Color BG = Color.FromArgb(20, 20, 20);
            public static readonly Color PANEL_BG = Color.FromArgb(32, 32, 35);
            public static readonly Color SETTINGS_PANEL_BG = Color.FromArgb(45, 45, 48);
            public static readonly Color GRID = Color.FromArgb(60, 60, 60);
            public static readonly Color GRAPH_BG = Color.Black;

            // Text colors.
            public static readonly Color LABEL = Color.Gray;
            public static readonly Color AXIS_TEXT = Color.FromArgb(180, 180, 180);
            public static readonly Color VAL_NORMAL = Color.White;
            public static readonly Color VAL_WARNING = Color.Yellow;
            public static readonly Color VAL_DANGER = Color.Red;
            public static readonly Color VAL_OK = Color.FromArgb(0, 200, 0);

            // Graph colors.
            public static readonly Color WAVEFORM_STATIC = Color.FromArgb(100, 149, 237);
            public static readonly Color PLAYHEAD = Color.Red;
            public static readonly Color OSCILLOSCOPE = Color.Lime;
            public static readonly Color VECTORSCOPE = Color.FromArgb(0, 255, 128);
            public static readonly Color FFT_TOP = Color.FromArgb(0, 255, 128);
            public static readonly Color FFT_BOTTOM = Color.FromArgb(0, 100, 200);

            // Meter colors.
            public static readonly Color METER_BG = Color.FromArgb(10, 10, 10);
            public static readonly Color METER_LOW = Color.FromArgb(0, 180, 0);
            public static readonly Color METER_MID = Color.FromArgb(200, 200, 0);
            public static readonly Color METER_HIGH = Color.Red;

            // Clip indicator colors.
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
        /// Defines the properties of a broadcasting loudness standard used for compliance checks.
        /// </summary>
        private struct LoudnessStandard
        {
            /// <summary>
            /// Gets or sets the display name of the standard (e.g., "EBU R 128").
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the target integrated loudness in LUFS. A null value indicates no specific target.
            /// </summary>
            public float? TargetIntegratedLoudness { get; set; }

            /// <summary>
            /// Gets or sets the maximum allowable true peak in dBTP. A null value indicates no specific limit.
            /// </summary>
            public float? MaxTruePeak { get; set; }

            /// <summary>
            /// Returns the name of the standard for display purposes.
            /// </summary>
            /// <returns>A string representing the standard's name.</returns>
            public override string ToString() => Name;
        }

        #endregion

        #region 3. Fields & State

        // GDI+ font objects for rendering text.
        private Font _fontLabel;
        private Font _fontValue;
        private Font _fontTitle;
        private Font _fontAxis;

        // State variables for the dynamic analysis view.
        private AnalysisTool _panel1Tool = AnalysisTool.Spectrum;
        private AnalysisTool _panel2Tool = AnalysisTool.Spectrogram;

        // Range: 0.0 to 1.0.
        private float _splitRatio = 0.5f;

        // FMOD system objects.
        private FMOD.System _coreSystem;
        private FMOD.Channel _activeChannel;
        private FMOD.Sound _activeSound;

        // FMOD Digital Signal Processing (DSP) units.
        private FMOD.DSP _fftDsp;
        private FMOD.DSP _meteringDsp;
        private FMOD.DSP _loudnessDsp;

        // Data structures for storing analysis results from DSPs.
        private FMOD.DSP_PARAMETER_FFT _fftData;
        private FMOD.DSP_METERING_INFO _meteringOutput;
        private FMOD.DSP_LOUDNESS_METER_INFO_TYPE _loudnessInfo;

        // Buffer for time-domain wave data from the FFT DSP.
        private float[] _waveData;

        // State variables for visualization.
        private Bitmap _staticWaveformBitmap;
        private Bitmap _spectrogramBitmap;
        private List<float[]> _spectrumHistory;
        private bool _isInitialized = false;
        private uint _totalLengthMs = 0;
        private float _currentSampleRate = 48000;
        private int _lastChannelCount = 0;
        private string _audioInfoString = "";

        // State variables for metering ballistics (smoothing and peak hold).
        private float[] _smoothedRMS;
        private float[] _smoothedPeak;
        private long[] _clipResetTime;
        private float _smoothingFactor = 0.7f;
        private int _peakHoldTimeMs = 2000;

        // Accumulators for tracking session statistics.
        private float[] _statMaxPeak;
        private float[] _statMaxRMS;
        private float[] _statMinRMS;
        private int[] _statClipCount;
        private float _statMaxMomentaryLUFS = -100.0f;
        private float _statMaxShortTermLUFS = -100.0f;
        private float _statMaxTruePeak = -100.0f;
        private float _statDcOffset = 0.0f;

        // Variables for managing loudness standards.
        private List<LoudnessStandard> _loudnessStandards;
        private LoudnessStandard _selectedStandard;

        // Tracks the last known volume to detect changes for auto-resetting loudness analysis.
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

            // Override designer properties with theme constants for consistency.
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
            // This uses reflection as the 'DoubleBuffered' property is protected.
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, this.panelDrawingSurface, new object[] { true });

            // Perform an initial layout update.
            UpdateControlLayout();
        }

        /// <summary>
        /// Sets up GDI+ resources and drawing styles.
        /// </summary>
        private void InitializeCustomResources()
        {
            // Initialize font objects.
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
            // Populate combo boxes with analysis tool options.
            cmbView1.DataSource = Enum.GetValues(typeof(AnalysisTool));
            cmbView2.DataSource = Enum.GetValues(typeof(AnalysisTool));

            // Set default selections.
            cmbView1.SelectedItem = _panel1Tool;
            cmbView2.SelectedItem = _panel2Tool;

            // Attach event handlers.
            cmbView1.SelectedIndexChanged += CmbView_SelectedIndexChanged;
            cmbView2.SelectedIndexChanged += CmbView_SelectedIndexChanged;

            // Ensure combo box states are consistent.
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

            // Attach event handler.
            comboBoxStandards.SelectedIndexChanged += ComboBoxStandards_SelectedIndexChanged;
        }

        /// <summary>
        /// Attaches the analyzer to the currently playing audio stream and initializes DSP units.
        /// </summary>
        /// <param name="coreSystem">The FMOD Core system instance responsible for low-level audio processing.</param>
        /// <param name="channel">The active playback channel to analyze.</param>
        /// <param name="sound">The sound object currently being played on the channel.</param>
        public void AttachToAudio(FMOD.System coreSystem, FMOD.Channel channel, FMOD.Sound sound)
        {
            // Clean up any previous analysis sessions and reset state variables.
            ResetAnalysis();

            _coreSystem = coreSystem;
            _activeChannel = channel;
            _activeSound = sound;

            // Exit if the channel is not valid.
            if (!_activeChannel.hasHandle())
            {
                return;
            }

            // Capture initial volume to prevent an immediate reset loop when volume monitoring starts.
            _activeChannel.getVolume(out _lastKnownVolume);

            // Retrieve sample rate to configure analysis parameters.
            sound.getDefaults(out _currentSampleRate, out _);

            // Create and attach the Metering DSP to the channel head for RMS and Peak analysis.
            _coreSystem.createDSPByType(DSP_TYPE.LOUDNESS_METER, out _meteringDsp);
            if (_meteringDsp.hasHandle())
            {
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _meteringDsp);
                _meteringDsp.setActive(true);
                _meteringDsp.setMeteringEnabled(true, true);
            }

            // Create and attach the FFT DSP for frequency spectrum analysis.
            _coreSystem.createDSPByType(DSP_TYPE.FFT, out _fftDsp);
            if (_fftDsp.hasHandle())
            {
                _fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, AnalysisSettings.FFT_WINDOW_SIZE);
                _fftDsp.setParameterInt((int)DSP_FFT.WINDOW, (int)DSP_FFT_WINDOW_TYPE.RECT);
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _fftDsp);
                _fftDsp.setActive(true);
            }

            // Create and attach the Loudness Meter DSP for EBU R 128/BS.1770 compliance checks.
            _coreSystem.createDSPByType(DSP_TYPE.LOUDNESS_METER, out _loudnessDsp);
            if (_loudnessDsp.hasHandle())
            {
                _activeChannel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, _loudnessDsp);
                _loudnessDsp.setActive(true);
            }

            // Retrieve sound format details to display channel and bit depth information.
            _activeSound.getFormat(out _, out SOUND_FORMAT format, out int numChannels, out int bits);
            _lastChannelCount = numChannels;

            // Format the audio information string for display.
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

            // Initialize statistics arrays based on the channel count.
            // Use a safe maximum of 8 channels to prevent out-of-bounds errors with high channel counts.
            int safeChannels = Math.Max(numChannels, 8);
            _statMaxPeak = new float[safeChannels];
            _statMaxRMS = new float[safeChannels];
            _statMinRMS = new float[safeChannels];
            _statClipCount = new int[safeChannels];
            _smoothedRMS = new float[safeChannels];
            _smoothedPeak = new float[safeChannels];
            _clipResetTime = new long[safeChannels];
            _waveData = new float[AnalysisSettings.FFT_WINDOW_SIZE * Math.Max(1, numChannels)];

            // Clear arrays to ensure a clean state.
            Array.Clear(_statMinRMS, 0, safeChannels);
            Array.Clear(_statClipCount, 0, safeChannels);

            // Pre-render the static waveform for the timeline view.
            GenerateStaticWaveform();

            _activeSound.getLength(out _totalLengthMs, TIMEUNIT.MS);
            _isInitialized = true;

            // Enable UI controls associated with active analysis.
            cmbView1.Visible = true;
            cmbView2.Visible = true;
            trackViewSplit.Visible = true;
            lblSplitLeft.Visible = true;
            lblSplitRight.Visible = true;
            comboBoxStandards.Visible = true;
            btnResetLoudness.Visible = true;
            UpdateControlLayout();

            // Start the rendering timer to begin real-time visualization.
            renderTimer.Start();
        }

        /// <summary>
        /// Resets the analyzer state and releases FMOD DSP resources.
        /// </summary>
        private void ResetAnalysis()
        {
            // Stop updates and reset state flags.
            renderTimer.Stop();
            _isInitialized = false;
            _audioInfoString = "";

            // Hide UI controls related to analysis.
            cmbView1.Visible = false;
            cmbView2.Visible = false;
            trackViewSplit.Visible = false;
            lblSplitLeft.Visible = false;
            lblSplitRight.Visible = false;
            comboBoxStandards.Visible = false;
            btnResetLoudness.Visible = false;

            // Safely remove and release each DSP unit.
            if (_fftDsp.hasHandle())
            {
                if (_activeChannel.hasHandle())
                {
                    _activeChannel.removeDSP(_fftDsp);
                }
                _fftDsp.release();
                _fftDsp.clearHandle();
            }

            if (_meteringDsp.hasHandle())
            {
                if (_activeChannel.hasHandle())
                {
                    _activeChannel.removeDSP(_meteringDsp);
                }
                _meteringDsp.release();
                _meteringDsp.clearHandle();
            }

            if (_loudnessDsp.hasHandle())
            {
                if (_activeChannel.hasHandle())
                {
                    _activeChannel.removeDSP(_loudnessDsp);
                }
                _loudnessDsp.release();
                _loudnessDsp.clearHandle();
            }

            // Dispose of GDI+ resources and clear data buffers.
            _staticWaveformBitmap?.Dispose();
            _staticWaveformBitmap = null;

            _spectrogramBitmap?.Dispose();
            _spectrogramBitmap = null;

            _spectrumHistory?.Clear();
            _waveData = null;
        }

        /// <summary>
        /// Resets all loudness-related DSPs and tracked statistics to start a new measurement session.
        /// </summary>
        private void ResetLoudnessAnalysis()
        {
            // Reset the FMOD loudness meter DSP if it exists.
            if (_loudnessDsp.hasHandle())
            {
                _loudnessDsp.reset();
            }

            // Reset tracked maximum loudness statistics.
            _statMaxMomentaryLUFS = -100.0f;
            _statMaxShortTermLUFS = -100.0f;
            _statMaxTruePeak = -100.0f;

            // Force a repaint to reflect the reset values.
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
            // Ensure all resources are cleaned up when the form closes.
            ResetAnalysis();

            // Dispose of GDI+ resources.
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
            // Update the layout and repaint the surface when the form is resized.
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
            // Update the smoothing factor based on the slider's value.
            _smoothingFactor = trackSmoothing.Value / 100.0f;
            UpdateSettingsLabels();
        }

        /// <summary>
        /// Handles the Scroll event of the trackPeakHold control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void trackPeakHold_Scroll(object sender, EventArgs e)
        {
            // Update the peak hold time from the slider.
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
            // Update the split ratio for the dynamic analysis panel.
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
            // Update the tool selection based on which combo box was changed.
            if (sender == cmbView1)
            {
                _panel1Tool = (AnalysisTool)cmbView1.SelectedItem;
            }
            if (sender == cmbView2)
            {
                _panel2Tool = (AnalysisTool)cmbView2.SelectedItem;
            }

            // Ensure the selections are valid and consistent.
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

            // Reset loudness analysis when the standard changes to start a fresh measurement.
            ResetLoudnessAnalysis();
        }

        /// <summary>
        /// Handles the Click event of the btnResetLoudness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnResetLoudness_Click(object sender, EventArgs e)
        {
            ResetLoudnessAnalysis();
        }

        /// <summary>
        /// Recalculates the positions of all controls dynamically based on the form size.
        /// </summary>
        private void UpdateControlLayout()
        {
            this.SuspendLayout();

            // Calculate available drawing area dimensions.
            int topOffset = panelSettings.Height;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height - topOffset;

            int availH = h - (LayoutConstants.PADDING * 4);
            int hWave = (int)(availH * LayoutConstants.RATIO_WAVEFORM);
            int hAnalysis = (int)(availH * LayoutConstants.RATIO_ANALYSIS);
            int hStats = availH - hWave - hAnalysis;

            // Define the rectangles for the main drawing areas.
            Rectangle rectAnalysis = new Rectangle(LayoutConstants.PADDING, LayoutConstants.PADDING + hWave + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hAnalysis);
            Rectangle rectStats = new Rectangle(LayoutConstants.PADDING, rectAnalysis.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hStats);

            // Position dynamic controls inside the analysis panel's control bar area.
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
            Rectangle rRight = new Rectangle(rectStats.Right - LayoutConstants.LOUDNESS_PANEL_WIDTH, panelDrawingSurface.Top + rectStats.Y, LayoutConstants.LOUDNESS_PANEL_WIDTH, rectStats.Height);

            // Ensure the panel is large enough to contain the controls before positioning them.
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
        /// Updates the text labels for the meter ballistics setting sliders.
        /// </summary>
        private void UpdateSettingsLabels()
        {
            lblSmoothing.Text = $"Meter Response: {Math.Round(_smoothingFactor * 100)}%";
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
        /// Ensures that the same tool cannot be selected in both ComboBoxes, preventing logical conflicts.
        /// </summary>
        private void UpdateComboBoxes()
        {
            // Temporarily detach event handlers to prevent circular updates.
            cmbView1.SelectedIndexChanged -= CmbView_SelectedIndexChanged;
            cmbView2.SelectedIndexChanged -= CmbView_SelectedIndexChanged;

            var allTools = (AnalysisTool[])Enum.GetValues(typeof(AnalysisTool));

            // Update the available items for the second view based on the first view's selection.
            var availableForView2 = new List<AnalysisTool>();
            foreach (var tool in allTools)
            {
                if (tool != _panel1Tool)
                {
                    availableForView2.Add(tool);
                }
            }
            cmbView2.DataSource = availableForView2;

            // If the current selection for view 2 is no longer valid, select the first available item.
            if (cmbView2.Items.Contains(_panel2Tool))
            {
                cmbView2.SelectedItem = _panel2Tool;
            }
            else
            {
                _panel2Tool = availableForView2.Count > 0 ? availableForView2[0] : _panel1Tool;
                cmbView2.SelectedItem = _panel2Tool;
            }

            // Update the available items for the first view based on the second view's selection.
            var availableForView1 = new List<AnalysisTool>();
            foreach (var tool in allTools)
            {
                if (tool != _panel2Tool)
                {
                    availableForView1.Add(tool);
                }
            }
            cmbView1.DataSource = availableForView1;

            // If the current selection for view 1 is no longer valid, select the first available item.
            if (cmbView1.Items.Contains(_panel1Tool))
            {
                cmbView1.SelectedItem = _panel1Tool;
            }
            else
            {
                _panel1Tool = availableForView1.Count > 0 ? availableForView1[0] : _panel2Tool;
                cmbView1.SelectedItem = _panel1Tool;
            }

            // Re-attach event handlers.
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
            // Only perform updates if the analyzer is initialized and the form is not disposed.
            if (_isInitialized && !this.IsDisposed)
            {
                UpdateAnalysisData();
                panelDrawingSurface.Invalidate(); // Trigger a repaint.
            }
        }

        /// <summary>
        /// Fetches the latest metering, FFT, and loudness data from FMOD DSPs.
        /// </summary>
        private void UpdateAnalysisData()
        {
            try
            {
                // Ensure the active channel is still valid before attempting to read data.
                if (!_activeChannel.hasHandle())
                {
                    return;
                }

                // Monitor volume changes to ensure loudness measurements remain valid for the current gain level.
                if (_activeChannel.getVolume(out float currentVolume) == RESULT.OK)
                {
                    // If the volume has changed significantly, reset the loudness analysis.
                    if (Math.Abs(currentVolume - _lastKnownVolume) > 0.001f)
                    {
                        _lastKnownVolume = currentVolume;
                        ResetLoudnessAnalysis();
                    }
                }

                // Retrieve RMS and Peak levels from the Metering DSP and apply smoothing.
                if (_meteringDsp.hasHandle())
                {
                    _meteringDsp.getMeteringInfo(IntPtr.Zero, out _meteringOutput);
                    if (_meteringOutput.numchannels > 0)
                    {
                        _lastChannelCount = _meteringOutput.numchannels;
                    }

                    int channels = Math.Min(_lastChannelCount, 32);
                    for (int i = 0; i < channels; i++)
                    {
                        if (i >= _meteringOutput.peaklevel.Length)
                        {
                            break;
                        }
                        float rawPeak = _meteringOutput.peaklevel[i];
                        float rawRms = _meteringOutput.rmslevel[i];

                        // Apply attack/decay smoothing logic for visual stability.
                        if (rawPeak >= _smoothedPeak[i])
                        {
                            _smoothedPeak[i] = rawPeak;
                        }
                        else
                        {
                            _smoothedPeak[i] = _smoothedPeak[i] * _smoothingFactor + rawPeak * (1.0f - _smoothingFactor);
                        }
                        if (rawRms >= _smoothedRMS[i])
                        {
                            _smoothedRMS[i] = rawRms;
                        }
                        else
                        {
                            _smoothedRMS[i] = _smoothedRMS[i] * _smoothingFactor + rawRms * (1.0f - _smoothingFactor);
                        }

                        // Track maximum peak and RMS values for session statistics.
                        if (rawPeak > _statMaxPeak[i])
                        {
                            _statMaxPeak[i] = rawPeak;
                        }
                        if (rawRms > _statMaxRMS[i])
                        {
                            _statMaxRMS[i] = rawRms;
                        }

                        // Update minimum RMS only if the signal is above the silence threshold.
                        if (20.0f * (float)Math.Log10(rawRms + 1e-5) > AnalysisSettings.SILENCE_THRESHOLD_DB)
                        {
                            if (_statMinRMS[i] == 0.0f || rawRms < _statMinRMS[i])
                            {
                                _statMinRMS[i] = rawRms;
                            }
                        }

                        // Detect digital clipping (0dBFS) and set the clip hold timer.
                        if (rawPeak >= 1.0f)
                        {
                            _statClipCount[i]++;
                            _clipResetTime[i] = DateTime.Now.Ticks + (_peakHoldTimeMs * 10000);
                        }
                    }
                }

                // Retrieve FFT spectrum data and update the history buffer for the spectrogram.
                if (_fftDsp.hasHandle())
                {
                    _fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr dataPtr, out uint length);
                    if (dataPtr != IntPtr.Zero && length > 0)
                    {
                        _fftData = (DSP_PARAMETER_FFT)Marshal.PtrToStructure(dataPtr, typeof(DSP_PARAMETER_FFT));
                        if (_fftData.numchannels > 0 && _fftData.length > 0)
                        {
                            // --- Frequency Data for Spectrum & Spectrogram ---
                            float[] currentSpectrum = new float[_fftData.length];
                            _fftData.getSpectrum(0, ref currentSpectrum);

                            // Add the latest spectrum data to the history for the spectrogram.
                            _spectrumHistory.Add(currentSpectrum);
                            if (_spectrumHistory.Count > AnalysisSettings.SPECTRUM_HISTORY_COUNT)
                            {
                                _spectrumHistory.RemoveAt(0);
                            }

                            // The first bin (index 0) of the spectrum represents the DC offset.
                            _statDcOffset = currentSpectrum[0];

                            // --- Time-Domain Data for Oscilloscope & Vectorscope ---
                            // The raw 'spectrum' field in the FFT structure contains the time-domain data
                            // before it is processed by the getSpectrum() function.
                            if (_waveData != null)
                            {
                                // Get a pointer to the start of the native spectrum data array.
                                IntPtr spectrumArrayPtr = Marshal.OffsetOf(typeof(DSP_PARAMETER_FFT), "spectrum_internal");
                                IntPtr firstChannelPtr = Marshal.ReadIntPtr(new IntPtr(dataPtr.ToInt64() + spectrumArrayPtr.ToInt64()));

                                // Copy the time-domain data for all channels directly into our wave buffer.
                                int samplesToCopy = Math.Min(_waveData.Length, _fftData.length * _fftData.numchannels);
                                Marshal.Copy(firstChannelPtr, _waveData, 0, samplesToCopy);
                            }
                        }
                    }
                }

                // Retrieve and track integrated loudness and true peak data.
                if (_loudnessDsp.hasHandle())
                {
                    _loudnessDsp.getParameterData((int)DSP_LOUDNESS_METER.INFO, out IntPtr dataPtr, out uint length);
                    if (dataPtr != IntPtr.Zero)
                    {
                        _loudnessInfo = (DSP_LOUDNESS_METER_INFO_TYPE)Marshal.PtrToStructure(dataPtr, typeof(DSP_LOUDNESS_METER_INFO_TYPE));

                        // Update session maximums for loudness metrics.
                        if (_loudnessInfo.momentaryloudness > _statMaxMomentaryLUFS)
                        {
                            _statMaxMomentaryLUFS = _loudnessInfo.momentaryloudness;
                        }
                        if (_loudnessInfo.shorttermloudness > _statMaxShortTermLUFS)
                        {
                            _statMaxShortTermLUFS = _loudnessInfo.shorttermloudness;
                        }
                        if (_loudnessInfo.maxtruepeak > _statMaxTruePeak)
                        {
                            _statMaxTruePeak = _loudnessInfo.maxtruepeak;
                        }
                    }
                }
            }
            catch
            {
                // Stop the render timer if an error occurs (e.g., channel released by main thread).
                // This prevents further exceptions and ensures application stability.
                renderTimer.Stop();
            }
        }

        /// <summary>
        /// Reads the entire audio buffer to generate a static waveform image for the timeline view.
        /// </summary>
        private void GenerateStaticWaveform()
        {
            if (!_activeSound.hasHandle())
            {
                return;
            }

            // Lock the sound's sample data to get direct memory access.
            _activeSound.getLength(out uint lengthBytes, TIMEUNIT.PCMBYTES);
            _activeSound.getFormat(out _, out SOUND_FORMAT format, out int channels, out int bits);
            RESULT res = _activeSound.@lock(0, lengthBytes, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            if (res != RESULT.OK)
            {
                return;
            }

            int width = 2048;
            int height = 200;
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = new Pen(AppTheme.WAVEFORM_STATIC, 1))
            {
                g.Clear(Color.Transparent);
                int bytesPerSample = bits / 8;

                // Default to 16-bit resolution if the bit depth is reported as zero.
                if (bytesPerSample == 0)
                {
                    bytesPerSample = 2;
                }

                long totalSamples = len1 / (uint)(bytesPerSample * channels);
                int samplesPerPixel = (int)Math.Max(1, totalSamples / width);

                float yCenter = height / 2f;
                float yScale = height / 2f;

                // Buffer for floating point samples.
                float[] fBuf = new float[1];

                // Iterate through each horizontal pixel of the bitmap.
                for (int x = 0; x < width; x++)
                {
                    long offsetBytes = (long)x * samplesPerPixel * bytesPerSample * channels;
                    if (offsetBytes >= len1)
                    {
                        break;
                    }

                    // Read a single sample value from the locked memory buffer.
                    float sampleVal = 0.0f;
                    IntPtr readPtr = new IntPtr(ptr1.ToInt64() + offsetBytes);

                    // Decode the sample based on its format.
                    try
                    {
                        switch (format)
                        {
                            case SOUND_FORMAT.PCM8:
                                sampleVal = (Marshal.ReadByte(readPtr) - 128) / 128f;
                                break;
                            case SOUND_FORMAT.PCM16:
                                sampleVal = Marshal.ReadInt16(readPtr) / 32768f;
                                break;
                            case SOUND_FORMAT.PCM24:
                                byte b0 = Marshal.ReadByte(readPtr);
                                byte b1 = Marshal.ReadByte(readPtr, 1);
                                byte b2 = Marshal.ReadByte(readPtr, 2);
                                int val24 = (b0 | (b1 << 8) | (b2 << 16));

                                // Sign extend the 24-bit value to a 32-bit integer.
                                if ((val24 & 0x800000) != 0)
                                {
                                    val24 |= unchecked((int)0xFF000000);
                                }
                                sampleVal = val24 / 8388608f;
                                break;
                            case SOUND_FORMAT.PCM32:
                                sampleVal = Marshal.ReadInt32(readPtr) / 2147483648f;
                                break;
                            case SOUND_FORMAT.PCMFLOAT:
                                Marshal.Copy(readPtr, fBuf, 0, 1);
                                sampleVal = fBuf[0];
                                break;
                        }
                    }
                    catch
                    {
                        // Break on any marshalling error (e.g., end of buffer).
                        break;
                    }

                    // Draw a vertical line representing the sample's amplitude.
                    g.DrawLine(pen, x, yCenter, x, yCenter + (sampleVal * yScale));
                }
            }

            // Unlock the sound data and store the generated bitmap.
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
            // All drawing logic is handled by panelDrawingSurface_Paint to leverage
            // double buffering and prevent rendering on the main form surface.
        }

        /// <summary>
        /// Handles the Paint event of the main drawing panel, orchestrating all rendering.
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

            // If not initialized, display a message and exit.
            if (!_isInitialized || this.IsDisposed)
            {
                DrawCenterText(g, "Audio Data Not Available", 0, h);
                return;
            }

            // Calculate the rectangles for each major UI section.
            int availH = h - (LayoutConstants.PADDING * 4);
            int hWave = (int)(availH * LayoutConstants.RATIO_WAVEFORM);
            int hAnalysis = (int)(availH * LayoutConstants.RATIO_ANALYSIS);
            int hStats = availH - hWave - hAnalysis;

            Rectangle rectWave = new Rectangle(LayoutConstants.PADDING, LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hWave);
            Rectangle rectAnalysis = new Rectangle(LayoutConstants.PADDING, rectWave.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hAnalysis);
            Rectangle rectStats = new Rectangle(LayoutConstants.PADDING, rectAnalysis.Bottom + LayoutConstants.PADDING, w - (LayoutConstants.PADDING * 2), hStats);

            // Call the dedicated rendering methods for each section.
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
            // Calculate panel rectangles for timeline and vectorscope.
            int vectorscopePanelSize = bounds.Height;
            Rectangle rectVectorscopePanel = new Rectangle(bounds.Right - vectorscopePanelSize, bounds.Y, vectorscopePanelSize, bounds.Height);
            Rectangle rectTimelinePanel = new Rectangle(bounds.X, bounds.Y, bounds.Width - vectorscopePanelSize - LayoutConstants.PADDING, bounds.Height);

            // Draw the Timeline Panel.
            using (Brush panelBrush = new SolidBrush(AppTheme.PANEL_BG))
            using (Pen borderPen = new Pen(AppTheme.GRID))
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.FillRectangle(panelBrush, rectTimelinePanel);
                g.DrawRectangle(borderPen, rectTimelinePanel);
                Region prev = g.Clip;
                g.SetClip(rectTimelinePanel);

                // Draw the pre-rendered static waveform.
                Rectangle waveArea = new Rectangle(rectTimelinePanel.X + LayoutConstants.PADDING, rectTimelinePanel.Y + 20, rectTimelinePanel.Width - LayoutConstants.PADDING * 2, rectTimelinePanel.Height - 20 - LayoutConstants.PADDING);
                if (_staticWaveformBitmap != null)
                {
                    g.DrawImage(_staticWaveformBitmap, waveArea);
                }

                // Draw the playhead and time information if audio is playing.
                if (_activeChannel.hasHandle() && _totalLengthMs > 0)
                {
                    _activeChannel.getPosition(out uint positionMs, TIMEUNIT.MS);
                    float progress = (float)positionMs / _totalLengthMs;
                    int xPos = waveArea.X + (int)(waveArea.Width * progress);

                    // Draw the vertical red playhead line.
                    using (Pen playheadPen = new Pen(AppTheme.PLAYHEAD, 1))
                    {
                        g.DrawLine(playheadPen, xPos, rectTimelinePanel.Top, xPos, rectTimelinePanel.Bottom);
                    }

                    // Draw time and format information.
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
                Region prev = g.Clip;
                g.SetClip(bounds);

                // Define the area below the control bar for rendering the tools.
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

                // Draw the selected tools in their respective rectangles.
                if (rect1.Width > 1)
                {
                    DrawTool(g, _panel1Tool, rect1);
                }
                if (rect2.Width > 1)
                {
                    DrawTool(g, _panel2Tool, rect2);
                }

                // Draw a separator line between the two tool views if split is active.
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
        /// Dispatches the drawing call to the appropriate rendering method based on the selected tool.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="tool">The analysis tool to draw.</param>
        /// <param name="bounds">The rectangular area where the tool will be drawn.</param>
        private void DrawTool(Graphics g, AnalysisTool tool, Rectangle bounds)
        {
            switch (tool)
            {
                case AnalysisTool.Oscilloscope:
                    DrawRealtimeOscilloscope(g, bounds);
                    break;
                case AnalysisTool.Spectrum:
                    DrawSpectrum(g, bounds);
                    break;
                case AnalysisTool.Spectrogram:
                    DrawSpectrogram(g, bounds, _currentSampleRate / 2.0f);
                    break;
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
                Region prev = g.Clip;
                g.SetClip(bounds);

                // Calculate the rectangles for the three sub-panels: meters, stats, and loudness.
                int metersPanelWidth = CalculateMetersPanelWidth(_lastChannelCount);
                Rectangle rMeters = new Rectangle(bounds.X, bounds.Y, metersPanelWidth, bounds.Height);
                Rectangle rRight = new Rectangle(bounds.Right - LayoutConstants.LOUDNESS_PANEL_WIDTH, bounds.Y, LayoutConstants.LOUDNESS_PANEL_WIDTH, bounds.Height);
                Rectangle rStats = new Rectangle(rMeters.Right, bounds.Y, rRight.Left - rMeters.Right, bounds.Height);

                // Draw vertical separators between the sub-panels.
                g.DrawLine(separatorPen, rMeters.Right, bounds.Top + 10, rMeters.Right, bounds.Bottom - 10);
                g.DrawLine(separatorPen, rStats.Right, bounds.Top + 10, rStats.Right, bounds.Bottom - 10);

                // Call the dedicated rendering methods for each sub-panel.
                DrawVerticalMeters(g, rMeters, _lastChannelCount);
                DrawStatsTable(g, rStats);
                DrawLoudnessPanel(g, rRight);

                g.Clip = prev;
            }
        }

        #endregion

        #region 8. Sub-Renderers (Visualizers)

        /// <summary>
        /// Draws the real-time frequency spectrum analyzer.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The rectangular area where the spectrum will be drawn.</param>
        private void DrawSpectrum(Graphics g, Rectangle bounds)
        {
            Region prev = g.Clip;
            g.SetClip(bounds);

            // Define graph dimensions, accounting for axes and padding.
            int yAxisWidth = 35;
            int xAxisHeight = 20;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - xAxisHeight - topPadding);

            if (graphRect.Width <= 0 || graphRect.Height <= 0)
            {
                g.Clip = prev;
                return;
            }

            // Draw the graph background.
            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Draw the axis grid and labels.
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
                    if (db > AnalysisSettings.SPECTRUM_MAX_DB || db < AnalysisSettings.SPECTRUM_MIN_DB)
                    {
                        continue;
                    }

                    int y = graphRect.Top + (int)((AnalysisSettings.SPECTRUM_MAX_DB - db) / dbRange * graphRect.Height);
                    g.DrawLine(gridPen, graphRect.Left, y, graphRect.Right, y);
                    g.DrawString($"{db:F0}dB", _fontAxis, axisBrush, graphRect.Left - 4, y, sfRight);
                }

                // Draw X-Axis (Frequency) on a logarithmic scale.
                float nyquist = _currentSampleRate / 2.0f;
                float[] freqMarkers = { 100, 1000, 5000, 10000, 20000 };
                foreach (float freq in freqMarkers)
                {
                    if (freq > nyquist || freq < 20.0f)
                    {
                        continue;
                    }

                    float xRatio = (float)(Math.Log10(freq / 20.0) / Math.Log10(nyquist / 20.0));
                    int xPos = graphRect.Left + (int)(graphRect.Width * xRatio);
                    g.DrawLine(gridPen, xPos, graphRect.Top, xPos, graphRect.Bottom);
                    string label = (freq >= 1000) ? $"{freq / 1000}k" : $"{freq}";
                    g.DrawString(label, _fontAxis, axisBrush, xPos, graphRect.Bottom + 2, sfCenter);
                }
            }

            // Draw the spectrum bars using the latest FFT data.
            if (_spectrumHistory.Count > 0)
            {
                float[] spectrum = _spectrumHistory[_spectrumHistory.Count - 1];
                int numBins = spectrum.Length / 2;

                // Iterate through each horizontal pixel of the graph area.
                for (int x = 0; x < graphRect.Width; x++)
                {
                    // Map the pixel's x-coordinate to a logarithmic frequency.
                    float xRatio = (float)x / graphRect.Width;
                    float freq = 20.0f * (float)Math.Pow(_currentSampleRate / 2.0 / 20.0, xRatio);
                    int bin = (int)(freq * spectrum.Length / _currentSampleRate);

                    if (bin >= numBins)
                    {
                        continue;
                    }

                    // Convert the magnitude from the FFT bin to decibels.
                    float db = 20 * (float)Math.Log10(spectrum[bin] + 1e-9);
                    float dbRange = AnalysisSettings.SPECTRUM_MAX_DB - AnalysisSettings.SPECTRUM_MIN_DB;
                    float yRatio = (db - AnalysisSettings.SPECTRUM_MIN_DB) / dbRange;
                    if (yRatio < 0)
                    {
                        yRatio = 0;
                    }

                    float h = yRatio * graphRect.Height;

                    // Draw the vertical bar with a gradient.
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
            Region prev = g.Clip;
            g.SetClip(bounds);

            // Define graph dimensions.
            int yAxisWidth = 35;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - topPadding * 2);

            if (graphRect.Width <= 0 || graphRect.Height <= 0)
            {
                g.Clip = prev;
                return;
            }

            // Draw the graph background.
            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Recreate the bitmap if the size has changed.
            if (_spectrogramBitmap == null || _spectrogramBitmap.Width != graphRect.Width || _spectrogramBitmap.Height != graphRect.Height)
            {
                _spectrogramBitmap?.Dispose();
                _spectrogramBitmap = new Bitmap(graphRect.Width, graphRect.Height);
            }

            // Shift the existing spectrogram image one pixel to the left to make room for the new data.
            using (Graphics bmpG = Graphics.FromImage(_spectrogramBitmap))
            {
                bmpG.DrawImage(_spectrogramBitmap, new Rectangle(-1, 0, _spectrogramBitmap.Width, _spectrogramBitmap.Height));
            }

            // Draw the newest spectrum data on the far right column.
            if (_spectrumHistory.Count > 0)
            {
                float[] latestSpectrum = _spectrumHistory[_spectrumHistory.Count - 1];
                int numBins = latestSpectrum.Length / 2;

                // Iterate through each vertical pixel of the new column.
                for (int y = 0; y < graphRect.Height; y++)
                {
                    // Map the y-coordinate to a logarithmic frequency.
                    double logY = 1.0 - (double)y / graphRect.Height;
                    double freq = 20 * Math.Pow(nyquist / 20, logY);
                    int binIndex = (int)(freq * _fftData.length / _currentSampleRate);

                    // Set the pixel color based on the magnitude at that frequency.
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
                    if (freq > nyquist || freq < 20.0f)
                    {
                        continue;
                    }

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
            Region prev = g.Clip;
            g.SetClip(bounds);

            // Define graph dimensions.
            int yAxisWidth = 35;
            int xAxisHeight = 20;
            int topPadding = 10;
            int hPadding = 5;
            Rectangle graphRect = new Rectangle(bounds.X + yAxisWidth, bounds.Y + topPadding, bounds.Width - yAxisWidth - hPadding, bounds.Height - xAxisHeight - topPadding);

            if (graphRect.Width <= 0 || graphRect.Height <= 0)
            {
                g.Clip = prev;
                return;
            }

            // Draw the graph background.
            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Draw the axis grid and labels.
            using (Pen gridPen = new Pen(AppTheme.GRID) { DashStyle = DashStyle.Dot })
            using (Brush axisBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                // Draw Y-Axis (Amplitude) from -1.0 to 1.0.
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

            // Draw the waveform using the time-domain data from the _waveData buffer.
            if (_waveData != null && _fftData.length > 0)
            {
                int samplesPerChannel = _fftData.length;
                int pointsToDraw = Math.Min(graphRect.Width, samplesPerChannel);
                if (pointsToDraw < 2)
                {
                    g.Clip = prev;
                    return;
                }

                PointF[] points = new PointF[pointsToDraw];
                float yCenter = graphRect.Top + graphRect.Height / 2.0f;
                float yScale = graphRect.Height / 2.0f;

                // Create an array of points to draw the waveform lines.
                for (int i = 0; i < pointsToDraw; i++)
                {
                    // Map the sample index to the horizontal pixel coordinate.
                    float x = graphRect.Left + ((float)i / (pointsToDraw - 1)) * (graphRect.Width - 1);

                    // Read the sample value from the interleaved wave data buffer (L, R, L, R...).
                    // We only visualize the first channel for simplicity.
                    int sampleIndex = i * _fftData.numchannels;
                    float sampleValue = (sampleIndex < _waveData.Length) ? _waveData[sampleIndex] : 0.0f;

                    // Invert Y-axis for screen coordinates (0,0 is top-left).
                    float y = yCenter - (sampleValue * yScale);
                    points[i] = new PointF(x, y);
                }

                // Draw the lines connecting the points.
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
        /// Draws the real-time vectorscope (Goniometer) to visualize stereo width and phase correlation.
        /// </summary>
        /// <param name="g">The Graphics object used for drawing.</param>
        /// <param name="bounds">The Rectangle defining the area where the vectorscope should be rendered.</param>
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
            if (graphRect.Width <= 0 || graphRect.Height <= 0)
            {
                g.Clip = prev;
                return;
            }

            // Fill the graph area with a black background.
            using (Brush bgBrush = new SolidBrush(AppTheme.GRAPH_BG))
            {
                g.FillRectangle(bgBrush, graphRect);
            }

            // Calculate geometry based on the inner graph rectangle.
            PointF center = new PointF(graphRect.X + graphRect.Width / 2.0f, graphRect.Top + graphRect.Height / 2.0f);
            float radius = Math.Min(graphRect.Width, graphRect.Height) / 2.0f;

            // Draw the grid lines (circle and diagonal lines) first for proper layering.
            using (Pen gridPen = new Pen(AppTheme.GRID))
            {
                g.DrawEllipse(gridPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

                // Vertical (Mid)
                g.DrawLine(gridPen, center.X, graphRect.Top, center.X, graphRect.Bottom);

                // Horizontal (Side)
                g.DrawLine(gridPen, graphRect.Left, center.Y, graphRect.Right, center.Y);

                // Left Channel Diagonal
                g.DrawLine(gridPen, graphRect.Left, graphRect.Top, graphRect.Right, graphRect.Bottom);

                // Right Channel Diagonal
                g.DrawLine(gridPen, graphRect.Left, graphRect.Bottom, graphRect.Right, graphRect.Top);
            }

            // Draw the title string on top of the grid.
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("VECTORSCOPE", _fontTitle, titleBrush, bounds.X + 5, bounds.Y);
            }

            // Draw the Lissajous figure if stereo data is available.
            if (_fftData.numchannels >= 2 && _waveData != null)
            {
                int samplesPerChannel = _fftData.length;
                int pointsToDraw = Math.Min(512, samplesPerChannel); // Limit points for performance.
                if (pointsToDraw < 1)
                {
                    g.Clip = prev;
                    return;
                }

                PointF[] points = new PointF[pointsToDraw];

                // Use a large, fixed multiplier to act as a "gain" knob.
                // This amplifies the signal to ensure it's visible even at low volumes.
                float scale = radius * 2.0f;

                // Create points for the vectorscope plot.
                for (int i = 0; i < pointsToDraw; i++)
                {
                    // Get left and right samples from the interleaved buffer.
                    int leftIndex = i * _fftData.numchannels;
                    int rightIndex = leftIndex + 1;

                    if (rightIndex >= _waveData.Length)
                    {
                        break;
                    }

                    float l = _waveData[leftIndex];
                    float r = _waveData[rightIndex];

                    // Standard goniometer calculation to map L/R to X/Y.
                    // Side channel (L-R)
                    float x_raw = (l - r) * 0.7071f;

                    // Mid channel (L+R)
                    float y_raw = (l + r) * 0.7071f;

                    // Apply scale.
                    float x_scaled = x_raw * scale;
                    float y_scaled = y_raw * scale;

                    // Clamp the values to the radius to prevent drawing outside the circle.
                    float magnitude = (float)Math.Sqrt(x_scaled * x_scaled + y_scaled * y_scaled);
                    if (magnitude > radius)
                    {
                        x_scaled = (x_scaled / magnitude) * radius;
                        y_scaled = (y_scaled / magnitude) * radius;
                    }

                    // Y is inverted for screen coordinates.
                    points[i] = new PointF(center.X + x_scaled, center.Y - y_scaled);
                }

                // Draw the resulting shape.
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen scopePen = new Pen(AppTheme.VECTORSCOPE, 1.0f))
                {
                    g.DrawLines(scopePen, points);
                }
                g.SmoothingMode = SmoothingMode.Default;
            }
            else if (_lastChannelCount < 2)
            {
                // Display a message if the audio is not stereo.
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
            const int RULER_AREA_WIDTH = 35;
            const int METER_WIDTH = 20;
            const int METER_SPACING = 15;
            const int PANEL_HORIZONTAL_PADDING = 25;
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
            // Draw panel title.
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("METERS", _fontTitle, titleBrush, bounds.X + 5, bounds.Y + 5);
            }

            if (channels == 0)
            {
                return;
            }

            // Define meter geometry.
            int meterAreaH = bounds.Height - 50;
            int meterAreaY = bounds.Y + 35;
            int meterW = 20;
            int meterSpacing = 15;
            int totalMeterW = (channels * meterW) + ((channels - 1) * meterSpacing);
            int startX = bounds.X + (bounds.Width - totalMeterW - 35) / 2 + 25;

            // Draw the decibel ruler and grid lines.
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

            // Draw each channel's meter.
            for (int i = 0; i < channels; i++)
            {
                int xPos = startX + (i * (meterW + meterSpacing));
                Rectangle meterRect = new Rectangle(xPos, meterAreaY, meterW, meterAreaH);

                // Draw meter background.
                using (Brush meterBgBrush = new SolidBrush(AppTheme.METER_BG))
                {
                    g.FillRectangle(meterBgBrush, meterRect);
                }

                // Get smoothed RMS and Peak values.
                float rms = (_smoothedRMS != null && i < _smoothedRMS.Length) ? _smoothedRMS[i] : 0;
                float peak = (_smoothedPeak != null && i < _smoothedPeak.Length) ? _smoothedPeak[i] : 0;

                // Convert linear values to decibels and then to a height ratio.
                float rmsDb = 20.0f * (float)Math.Log10(rms + 1e-5);
                float peakDb = 20.0f * (float)Math.Log10(peak + 1e-5);
                float rmsRatio = Math.Max(0, Math.Min(1, (rmsDb - AnalysisSettings.METER_MIN_DB) / (0 - AnalysisSettings.METER_MIN_DB)));
                float peakRatio = Math.Max(0, Math.Min(1, (peakDb - AnalysisSettings.METER_MIN_DB) / (0 - AnalysisSettings.METER_MIN_DB)));
                int rmsH = (int)(rmsRatio * meterAreaH);
                int rmsY = meterAreaY + meterAreaH - rmsH;

                // Determine the color of the RMS bar based on its level.
                Color barColor = AppTheme.METER_LOW;
                if (rmsDb > -6)
                {
                    barColor = AppTheme.METER_MID;
                }
                if (rmsDb > 0)
                {
                    barColor = AppTheme.METER_HIGH;
                }

                // Draw the RMS bar.
                using (Brush barBrush = new SolidBrush(barColor))
                {
                    if (rmsH > 0)
                    {
                        g.FillRectangle(barBrush, xPos, rmsY, meterW, rmsH);
                    }
                }

                // Draw the peak level indicator line.
                int peakY = meterAreaY + meterAreaH - (int)(peakRatio * meterAreaH);
                using (Pen peakPen = new Pen(AppTheme.VAL_NORMAL))
                {
                    g.DrawLine(peakPen, xPos, peakY, xPos + meterW, peakY);
                }

                // Draw the clip indicator box.
                int clipH = 4;
                Rectangle clipBox = new Rectangle(xPos, meterAreaY - clipH - 2, meterW, clipH);
                bool showClip = (_clipResetTime != null && i < _clipResetTime.Length && DateTime.Now.Ticks < _clipResetTime[i]);
                using (Brush clipBrush = new SolidBrush(showClip ? AppTheme.CLIP_ON : AppTheme.CLIP_OFF))
                {
                    g.FillRectangle(clipBrush, clipBox);
                }

                // Draw the channel label below the meter.
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
            if (bounds.Width < 150)
            {
                return;
            }

            // Draw panel title.
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("CHANNEL STATISTICS", _fontTitle, titleBrush, bounds.X + 10, bounds.Y + 5);
            }

            int startY = bounds.Y + 45;
            int rowH = 18;
            int startX = bounds.X + 10;
            int totalW = bounds.Width - 20;

            int channelsToDisplay = Math.Min(_lastChannelCount, 8);
            if (channelsToDisplay == 0)
            {
                return;
            }

            // Draw the header row.
            DrawStatHeader(g, startX, startY - rowH, totalW, channelsToDisplay);

            // Prepare data arrays for each row.
            string[] peakVals = new string[channelsToDisplay];
            string[] maxRmsVals = new string[channelsToDisplay];
            string[] minRmsVals = new string[channelsToDisplay];
            string[] currentRmsVals = new string[channelsToDisplay];
            string[] clipVals = new string[channelsToDisplay];

            for (int i = 0; i < channelsToDisplay; i++)
            {
                peakVals[i] = FormatDb((_statMaxPeak != null && i < _statMaxPeak.Length) ? _statMaxPeak[i] : 0);
                maxRmsVals[i] = FormatDb((_statMaxRMS != null && i < _statMaxRMS.Length) ? _statMaxRMS[i] : 0);
                minRmsVals[i] = FormatDb((_statMinRMS != null && i < _statMinRMS.Length) ? _statMinRMS[i] : 0);
                currentRmsVals[i] = FormatDb((_smoothedRMS != null && i < _smoothedRMS.Length) ? _smoothedRMS[i] : 0);
                clipVals[i] = (_statClipCount != null && i < _statClipCount.Length) ? _statClipCount[i].ToString() : "0";
            }

            // Draw each data row.
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
            if (numChannels > 4)
            {
                metricWidth = (int)(width * 0.35f);
            }
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
        /// <param name="checkPeak">A flag to indicate if peak values should be color-coded for clipping.</param>
        /// <param name="checkClip">A flag to indicate if clip counts should be color-coded.</param>
        private void DrawStatDataRow(Graphics g, int x, int y, int width, string metric, string[] values, bool checkPeak = false, bool checkClip = false)
        {
            int numChannels = values.Length;
            int metricWidth = (int)(width * 0.45f);
            if (numChannels > 4)
            {
                metricWidth = (int)(width * 0.35f);
            }
            int valueWidth = (width - metricWidth) / numChannels;

            // Draw the metric label.
            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(metric, _fontLabel, labelBrush, x, y);
            }

            // Draw the value for each channel.
            for (int i = 0; i < numChannels; i++)
            {
                // Determine the color based on value thresholds.
                Color valColor = AppTheme.VAL_NORMAL;
                if (checkPeak && ParseDb(values[i]) >= 0)
                {
                    valColor = AppTheme.VAL_DANGER;
                }
                if (checkClip && int.Parse(values[i]) > 0)
                {
                    valColor = AppTheme.VAL_WARNING;
                }

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
            // Draw panel title.
            using (Brush titleBrush = new SolidBrush(AppTheme.AXIS_TEXT))
            {
                g.DrawString("LOUDNESS", _fontTitle, titleBrush, bounds.X + 10, bounds.Y + 5);
            }

            int startY = bounds.Y + 55;
            int rowH = 18;
            int startX = bounds.X + 10;
            int valX = startX + (int)(bounds.Width * 0.5f);

            // Determine colors and feedback text based on compliance with the selected standard.
            var colorIntegrated = AppTheme.VAL_NORMAL;
            var colorTruePeak = AppTheme.VAL_NORMAL;
            string feedbackText = " ";
            Color feedbackColor = AppTheme.LABEL;

            // Check integrated loudness compliance.
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
            else
            {
                feedbackText = "Absolute Scale";
            }

            // Check true peak compliance.
            if (_selectedStandard.MaxTruePeak.HasValue && _statMaxTruePeak > _selectedStandard.MaxTruePeak.Value)
            {
                colorTruePeak = AppTheme.VAL_DANGER;
            }

            // Draw each loudness metric row.
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 0), "Integrated", $"{_loudnessInfo.integratedloudness:F1} LUFS", colorIntegrated);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 1), "Short-Term Max", $"{_statMaxShortTermLUFS:F1} LUFS", AppTheme.VAL_NORMAL);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 2), "Momentary Max", $"{_statMaxMomentaryLUFS:F1} LUFS", AppTheme.VAL_NORMAL);
            DrawLoudnessRowDynamic(g, startX, valX, startY + (rowH * 3), "True Peak Max", $"{_statMaxTruePeak:F2} dBTP", colorTruePeak);

            // Draw the feedback text at the bottom.
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
            // Draw the label for the row.
            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(vals[0], _fontLabel, labelBrush, cols[0], y);
            }

            // Draw the value for the row.
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
            // Draw the label for the row.
            using (Brush labelBrush = new SolidBrush(AppTheme.LABEL))
            {
                g.DrawString(label, _fontLabel, labelBrush, labelX, y);
            }

            // Draw the value with the specified color.
            using (Brush valBrush = new SolidBrush(valColor))
            {
                g.DrawString(val, _fontValue, valBrush, valX, y);
            }
        }

        /// <summary>
        /// Gets a standard label for a given channel index (e.g., "L", "R", "C").
        /// </summary>
        /// <param name="channelIndex">The index of the channel.</param>
        /// <param name="totalChannels">The total number of channels.</param>
        /// <returns>A string label for the channel.</returns>
        private string GetChannelLabel(int channelIndex, int totalChannels)
        {
            if (totalChannels == 1)
            {
                return "Mono";
            }
            if (totalChannels == 2)
            {
                return (channelIndex == 0) ? "L" : "R";
            }

            // 5.1 Surround.
            if (totalChannels == 6)
            {
                string[] labels = { "L", "R", "C", "LFE", "SL", "SR" };
                return (channelIndex < labels.Length) ? labels[channelIndex] : $"Ch{channelIndex + 1}";
            }

            // 7.1 Surround.
            if (totalChannels == 8)
            {
                string[] labels = { "L", "R", "C", "LFE", "BL", "BR", "SL", "SR" };
                return (channelIndex < labels.Length) ? labels[channelIndex] : $"Ch{channelIndex + 1}";
            }
            return $"Ch{channelIndex + 1}";
        }

        /// <summary>
        /// Formats a linear amplitude value (0.0 to 1.0) to a decibel string.
        /// </summary>
        /// <param name="lin">The linear amplitude value.</param>
        /// <returns>The value formatted as a decibel string (e.g., "-6.02 dB").</returns>
        private string FormatDb(float lin)
        {
            if (lin <= 0)
            {
                return "-inf dB";
            }
            float db = 20.0f * (float)Math.Log10(lin);
            return $"{db:F2} dB";
        }

        /// <summary>
        /// Parses a decibel string back to a float value.
        /// </summary>
        /// <param name="dbStr">The decibel string to parse.</param>
        /// <returns>The parsed float value in decibels.</returns>
        private float ParseDb(string dbStr)
        {
            if (dbStr.StartsWith("-inf"))
            {
                return -999;
            }
            string num = dbStr.Replace(" dB", "");
            float.TryParse(num, out float result);
            return result;
        }

        /// <summary>
        /// Gets a color for the spectrogram based on signal magnitude.
        /// </summary>
        /// <param name="magnitude">The signal magnitude from the FFT.</param>
        /// <returns>A color representing the magnitude, creating a heat map effect.</returns>
        private Color GetColorForMagnitude(float magnitude)
        {
            // Convert linear magnitude to decibels and normalize it to a 0-1 range.
            // Map -80dB to 0dB range.
            float db = 20 * (float)Math.Log10(magnitude + 1e-9);
            float normalized = Math.Max(0, Math.Min(1, (db + 80) / 80));

            // Use a gradient (Blue -> Cyan -> Green -> Yellow -> Red) for the heatmap.
            if (normalized < 0.25f)
            {
                // Blue.
                return Color.FromArgb(0, 0, (int)(normalized * 4 * 255));
            }
            if (normalized < 0.5f)
            {
                // Blue to Cyan.
                return Color.FromArgb(0, (int)((normalized - 0.25f) * 4 * 255), 255);
            }
            if (normalized < 0.75f)
            {
                // Cyan to Green to Yellow.
                return Color.FromArgb((int)((normalized - 0.5f) * 4 * 255), 255, (int)(255 - (normalized - 0.5f) * 4 * 255));
            }

            // Yellow to Red.
            return Color.FromArgb(255, (int)(255 - (normalized - 0.75f) * 4 * 255), 0);
        }

        #endregion
    }
}