/**
 * @file FmodManager.cs
 * @brief Provides a high-level abstraction layer for managing FMOD Studio and Core API interactions.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This class acts as a facade for the FMOD engine, simplifying audio playback and system management.
 * It handles the initialization and cleanup of FMOD systems, manages the playback lifecycle of sounds
 * and events, and provides a thread-safe interface for UI components to interact with the audio engine.
 * The use of a PlaybackSession internal class helps to manage the state of the currently playing audio.
 *
 * Key Features:
 *  - FMOD System Initialization: Manages the setup and release of FMOD Studio and Core systems.
 *  - Unified Playback Control: Provides simple methods (Play, Stop, Pause) for both audio files and FMOD events.
 *  - Asynchronous Playback: Initiates audio playback on a background thread to keep the UI responsive.
 *  - State Management: Tracks the current playback state, including position, total length, and looping status.
 *  - Thread Safety: Uses a lock object to ensure that all FMOD API calls are synchronized.
 *  - Robust Fallback: Implements an in-memory decoding strategy for legacy or problematic audio formats.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Key Dependencies: FMOD Studio API, FMOD Core API
 *  - Last Update: 2025-12-24
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FMOD; // Core API
using FMOD.Studio; // Studio API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Manages all interactions with the FMOD audio engine.
    /// </summary>
    public class FmodManager : IDisposable
    {
        #region 1. Configuration & Constants

        /// <summary>
        /// The maximum number of virtual channels to be used by the FMOD Studio system.
        /// </summary>
        private const int MAX_VIRTUAL_CHANNELS = 1024;

        /// <summary>
        /// The initialization flags for the FMOD Studio system.
        /// </summary>
        private const FMOD.Studio.INITFLAGS STUDIO_INIT_FLAGS = FMOD.Studio.INITFLAGS.NORMAL;

        /// <summary>
        /// The initialization flags for the FMOD Core system.
        /// </summary>
        private const FMOD.INITFLAGS CORE_INIT_FLAGS = FMOD.INITFLAGS.NORMAL;

        #endregion

        #region 2. Fields & Properties

        /// <summary>
        /// The FMOD Studio System instance, used for high-level event and bank management.
        /// </summary>
        private FMOD.Studio.System _studioSystem;

        /// <summary>
        /// The FMOD Core System instance, used for low-level sound playback and processing.
        /// </summary>
        private FMOD.System _coreSystem;

        /// <summary>
        /// A lock object to ensure thread-safe access to FMOD API calls.
        /// </summary>
        public readonly object SyncLock = new object();

        /// <summary>
        /// Represents the currently active playback session.
        /// </summary>
        private PlaybackSession _currentSession;

        /// <summary>
        /// A flag indicating whether audio is currently playing.
        /// </summary>
        private bool _isPlaying = false;

        /// <summary>
        /// The total length in milliseconds of the currently loaded sound or event.
        /// </summary>
        private uint _currentTotalLengthMs = 0;

        /// <summary>
        /// A cancellation token source to manage the lifecycle of asynchronous playback tasks.
        /// </summary>
        private CancellationTokenSource _playCts;

        /// <summary>
        /// Gets the FMOD Core System instance.
        /// </summary>
        public FMOD.System CoreSystem => _coreSystem;

        /// <summary>
        /// Gets the FMOD Studio System instance.
        /// </summary>
        public FMOD.Studio.System StudioSystem => _studioSystem;

        /// <summary>
        /// Gets a value indicating whether audio is currently playing.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Gets the active FMOD Channel for the current playback session.
        /// </summary>
        /// <remarks>
        /// Returns a new, invalid Channel if no session is active.
        /// </remarks>
        public FMOD.Channel CurrentChannel => _currentSession?.Channel ?? new Channel();

        /// <summary>
        /// Gets the active FMOD Sound being played in the current session.
        /// </summary>
        /// <remarks>
        /// Returns a new, invalid Sound if no session is active.
        /// </remarks>
        public FMOD.Sound CurrentSound => _currentSession?.PlayableSound ?? new Sound();

        #endregion

        #region 3. Internal Playback Session Class

        /// <summary>
        /// Encapsulates all resources and state related to a single playback instance.
        /// </summary>
        private class PlaybackSession : IDisposable
        {
            /// <summary>
            /// The FMOD channel used for direct sound playback.
            /// </summary>
            public FMOD.Channel Channel;

            /// <summary>
            /// The FMOD event instance used for event playback.
            /// </summary>
            public FMOD.Studio.EventInstance EventInstance;

            /// <summary>
            /// The top-level FMOD Sound object loaded from a file or memory.
            /// </summary>
            public FMOD.Sound LoadedSound;

            /// <summary>
            /// The specific FMOD Sound object being played (could be a sub-sound).
            /// </summary>
            public FMOD.Sound PlayableSound;

            /// <summary>
            /// Gets a value indicating whether this session is for an FMOD Event.
            /// </summary>
            public bool IsEvent { get; }

            /// <summary>
            /// Gets or sets the total length of the sound or event in milliseconds.
            /// </summary>
            public uint TotalLengthMs { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="PlaybackSession"/> class.
            /// </summary>
            /// <param name="isEvent">Indicates if the session is for an event or a direct sound.</param>
            public PlaybackSession(bool isEvent = false)
            {
                IsEvent = isEvent;
            }

            /// <summary>
            /// Checks if the session's core FMOD object (Channel or EventInstance) is valid.
            /// </summary>
            /// <returns><c>true</c> if the session is valid and active; otherwise, <c>false</c>.</returns>
            public bool IsValid()
            {
                return (Channel.hasHandle() && !IsEvent) || (EventInstance.isValid() && IsEvent);
            }

            /// <summary>
            /// Releases all FMOD resources associated with this playback session.
            /// </summary>
            public void Dispose()
            {
                // Stop and release the channel if it's in use.
                if (Channel.hasHandle())
                {
                    Channel.stop();
                    Channel.clearHandle();
                }

                // Stop and release the event instance if it's in use.
                if (EventInstance.isValid())
                {
                    EventInstance.stop(STOP_MODE.IMMEDIATE);
                    EventInstance.release();
                    EventInstance.clearHandle();
                }

                // Safely release the primary loaded sound object.
                Sound tempLoadedSound = LoadedSound;
                Utilities.SafeRelease(ref tempLoadedSound);
                LoadedSound = tempLoadedSound;

                // Clear the handle for the playable sound if it's different from the loaded sound.
                // This prevents double-release issues if they share the same handle.
                if (PlayableSound.hasHandle() && PlayableSound.handle != LoadedSound.handle)
                {
                    PlayableSound.clearHandle();
                }
            }
        }

        #endregion

        #region 4. Initialization & Cleanup

        /// <summary>
        /// Initializes the FMOD Studio and Core systems.
        /// </summary>
        /// <exception cref="Exception">Thrown if FMOD initialization fails.</exception>
        public void Initialize()
        {
            try
            {
                // Create and initialize the FMOD Studio and Core System instances.
                Utilities.CheckFmodResult(FMOD.Studio.System.create(out _studioSystem));
                Utilities.CheckFmodResult(_studioSystem.getCoreSystem(out _coreSystem));
                Utilities.CheckFmodResult(_studioSystem.initialize(MAX_VIRTUAL_CHANNELS, STUDIO_INIT_FLAGS, CORE_INIT_FLAGS, IntPtr.Zero));
            }
            catch (Exception ex)
            {
                // Wrap FMOD-specific exceptions in a more informative general exception.
                throw new Exception($"FMOD Initialization Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes of the FmodManager and releases all FMOD resources.
        /// </summary>
        public void Dispose()
        {
            // Stop any active playback and cancel pending tasks.
            Stop();
            _playCts?.Cancel();
            _playCts?.Dispose();

            // Thread-safely release the FMOD systems.
            lock (SyncLock)
            {
                if (_studioSystem.isValid())
                {
                    _studioSystem.unloadAll();
                    _studioSystem.release();
                    _studioSystem.clearHandle();
                }

                if (_coreSystem.hasHandle())
                {
                    // Core system is owned by Studio system, so we only clear the handle.
                    _coreSystem.clearHandle();
                }
            }
        }

        #endregion

        #region 5. Core Engine Loop

        /// <summary>
        /// Updates the FMOD systems. This should be called regularly (e.g., in a timer tick).
        /// </summary>
        public void Update()
        {
            // Update both FMOD systems within a lock to ensure thread safety.
            lock (SyncLock)
            {
                if (_studioSystem.isValid())
                {
                    _studioSystem.update();
                }

                if (_coreSystem.hasHandle())
                {
                    _coreSystem.update();
                }
            }
        }

        #endregion

        #region 6. Playback Control

        /// <summary>
        /// Asynchronously starts playback for the selected audio or event node.
        /// </summary>
        /// <param name="selection">The <see cref="NodeData"/> to play.</param>
        /// <param name="volume">The initial volume for playback (0.0 to 1.0).</param>
        /// <param name="isLooping">Indicates whether the sound should loop.</param>
        /// <param name="onPlaybackStart">A callback action to execute when playback successfully starts.</param>
        /// <remarks>
        /// Playback workflow:
        ///  1) Cancel any existing playback tasks and stop current audio.
        ///  2) Identify the selection type (Direct Audio or FMOD Event).
        ///  3) [Audio] Determine loading strategy based on FSB version and format.
        ///  4) [Audio] Load sound data (prefer streaming, fallback to memory).
        ///  5) [Audio] Configure channel (Volume, Loop) and start playback.
        ///  6) [Event] Initialize and start the Event Instance.
        ///  7) Update the active session state and commit changes.
        /// </remarks>
        public async Task PlaySelectionAsync(NodeData selection, float volume, bool isLooping, Action<FMOD.System, FMOD.Channel, FMOD.Sound> onPlaybackStart)
        {
            if (selection == null)
            {
                return;
            }

            // Step 1: Cancel any existing playback tasks and stop current audio.
            // This ensures we have a clean slate before starting a new session.
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = new CancellationTokenSource();
            CancellationToken token = _playCts.Token;

            Stop();

            PlaybackSession newSession = null;

            try
            {
                // Step 2: Identify whether the selection is a direct Audio file or an FMOD Event.
                if (selection is AudioDataNode audioNode)
                {
                    newSession = new PlaybackSession();
                    AudioInfo info = audioNode.CachedAudio;
                    newSession.TotalLengthMs = info.LengthMs;

                    // Execute the sound loading and playback on a background thread.
                    await Task.Run(() =>
                    {
                        lock (SyncLock)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            // Step 3: Determine the best loading strategy based on FSB version and format.
                            // Legacy formats (e.g., MPEG, IMA ADPCM) often require an in-memory fallback.
                            char fsbVersion = Utilities.GetFsbVersion(info.SourcePath, info.FileOffset);
                            bool isLegacyContainer = (fsbVersion != '5' && fsbVersion != '0');

                            bool isLegacyFormat = info.Type == SOUND_TYPE.MPEG ||
                                                  ((uint)info.Mode & (uint)FsbModeFlags.ImaAdpcm) != 0 ||
                                                  info.Format == SOUND_FORMAT.BITSTREAM ||
                                                  info.Type == SOUND_TYPE.XMA;

                            bool useInMemoryFallback = isLegacyContainer || isLegacyFormat;
                            bool isStandaloneWav = false;

                            RESULT res = RESULT.ERR_FORMAT;

                            // Step 4: Load sound data securely.
                            // Attempt to create a stream directly from the file first for performance.
                            if (!useInMemoryFallback)
                            {
                                CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO
                                {
                                    cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                                    fileoffset = (uint)info.FileOffset
                                };
                                Utilities.SafeRelease(ref newSession.LoadedSound);

                                res = _coreSystem.createSound(info.SourcePath, MODE.CREATESTREAM | MODE.OPENONLY | MODE.IGNORETAGS, ref ex, out newSession.LoadedSound);
                            }

                            // If streaming fails or is not supported, use the in-memory fallback.
                            if (res != RESULT.OK || useInMemoryFallback)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return;
                                }

                                byte[] wavData = Utilities.GetDecodedWavBytes(_coreSystem, SyncLock, info);

                                if (token.IsCancellationRequested)
                                {
                                    return;
                                }

                                // Create an FMOD sound from the in-memory WAV data.
                                if (wavData != null && wavData.Length > 0)
                                {
                                    CREATESOUNDEXINFO memEx = new CREATESOUNDEXINFO
                                    {
                                        cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                                        length = (uint)wavData.Length
                                    };

                                    Utilities.SafeRelease(ref newSession.LoadedSound);

                                    res = _coreSystem.createSound(wavData, MODE.OPENMEMORY | MODE.CREATESAMPLE | MODE.IGNORETAGS, ref memEx, out newSession.LoadedSound);

                                    if (res == RESULT.OK)
                                    {
                                        newSession.LoadedSound.getLength(out uint newLen, TIMEUNIT.MS);
                                        newSession.TotalLengthMs = newLen;
                                        isStandaloneWav = true;
                                    }
                                }
                            }

                            // Step 5: Configure the channel and start playback.
                            // Ensure the sound was successfully loaded before attempting playback.
                            if (!token.IsCancellationRequested && res == RESULT.OK && newSession.LoadedSound.hasHandle())
                            {
                                Sound soundToPlay;
                                newSession.LoadedSound.getNumSubSounds(out int numSub);

                                if (!isStandaloneWav && numSub > 0 && info.Index < numSub)
                                {
                                    Utilities.CheckFmodResult(newSession.LoadedSound.getSubSound(info.Index, out soundToPlay));
                                }
                                else
                                {
                                    soundToPlay = newSession.LoadedSound;
                                }

                                newSession.PlayableSound = soundToPlay;

                                Utilities.CheckFmodResult(_coreSystem.playSound(newSession.PlayableSound, new ChannelGroup(IntPtr.Zero), false, out newSession.Channel));

                                if (newSession.Channel.hasHandle())
                                {
                                    newSession.Channel.setVolume(volume);
                                    newSession.Channel.setMode(isLooping ? MODE.LOOP_NORMAL : MODE.LOOP_OFF);
                                    onPlaybackStart?.Invoke(_coreSystem, newSession.Channel, newSession.PlayableSound);
                                }
                            }
                        }
                    }, token);
                }
                // Step 6: Initialize and start the Event Instance if the selection is an event.
                else if (selection is EventNode eventNode)
                {
                    newSession = new PlaybackSession(isEvent: true);
                    EventDescription evt = eventNode.EventObject;
                    if (evt.isValid())
                    {
                        evt.getLength(out int len);
                        newSession.TotalLengthMs = (uint)len;

                        evt.createInstance(out newSession.EventInstance);
                        newSession.EventInstance.setVolume(volume);
                        newSession.EventInstance.start();
                    }
                }

                token.ThrowIfCancellationRequested();

                // Step 7: Update the active session state and commit changes.
                if (newSession != null && newSession.IsValid())
                {
                    lock (SyncLock)
                    {
                        _currentSession = newSession;
                        _isPlaying = true;
                        _currentTotalLengthMs = newSession.TotalLengthMs;
                    }
                }
                else
                {
                    newSession?.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                newSession?.Dispose();
            }
            catch (Exception ex)
            {
                newSession?.Dispose();
                Stop();
                throw new Exception($"FMOD Playback Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Stops any currently playing audio and releases the active session.
        /// </summary>
        public void Stop()
        {
            lock (SyncLock)
            {
                _currentSession?.Dispose();
                _currentSession = null;
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Toggles the pause state of the currently playing audio.
        /// </summary>
        /// <returns><c>true</c> if a valid session was paused or unpaused; otherwise, <c>false</c>.</returns>
        public bool TogglePause()
        {
            lock (SyncLock)
            {
                if (_currentSession == null)
                {
                    return false;
                }

                bool playing = false, paused = false;

                // Get the current playing and paused state for either a channel or an event.
                if (!_currentSession.IsEvent && _currentSession.Channel.hasHandle())
                {
                    _currentSession.Channel.isPlaying(out playing);
                    _currentSession.Channel.getPaused(out paused);
                }
                else if (_currentSession.IsEvent && _currentSession.EventInstance.isValid())
                {
                    _currentSession.EventInstance.getPlaybackState(out PLAYBACK_STATE s);
                    playing = (s == PLAYBACK_STATE.PLAYING);
                    _currentSession.EventInstance.getPaused(out paused);
                }

                // Toggle the paused state based on the current state.
                if (playing && !paused)
                {
                    if (_currentSession.Channel.hasHandle())
                    {
                        _currentSession.Channel.setPaused(true);
                    }

                    if (_currentSession.EventInstance.isValid())
                    {
                        _currentSession.EventInstance.setPaused(true);
                    }
                    return true;
                }
                else if (paused)
                {
                    if (_currentSession.Channel.hasHandle())
                    {
                        _currentSession.Channel.setPaused(false);
                    }

                    if (_currentSession.EventInstance.isValid())
                    {
                        _currentSession.EventInstance.setPaused(false);
                    }
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region 7. Playback State & Configuration

        /// <summary>
        /// Gets the current playback status, including position and total length.
        /// </summary>
        /// <returns>A tuple containing the playing state, current position in ms, and total length in ms.</returns>
        public (bool IsPlaying, uint CurrentPosition, uint TotalLength) GetPlaybackStatus()
        {
            lock (SyncLock)
            {
                // If there's no active session, return the last known state.
                if (_currentSession == null || !_currentSession.IsValid())
                {
                    _isPlaying = false;
                    return (false, 0, _currentTotalLengthMs);
                }

                bool playing = false;
                uint currentPos = 0;

                // Retrieve position and state from either the channel or event instance.
                if (!_currentSession.IsEvent && _currentSession.Channel.hasHandle())
                {
                    _currentSession.Channel.isPlaying(out playing);
                    if (playing)
                    {
                        _currentSession.Channel.getPosition(out currentPos, TIMEUNIT.MS);

                        // Manually check if a non-looping sound has finished playback.
                        // FMOD sometimes reports 'isPlaying' as true for a short time after completion.
                        _currentSession.Channel.getMode(out MODE mode);

                        bool isOneShot = (mode & MODE.LOOP_NORMAL) == 0;
                        bool hasDuration = _currentTotalLengthMs > 0;
                        bool isFinished = currentPos >= _currentTotalLengthMs;

                        if (isOneShot && hasDuration && isFinished)
                        {
                            playing = false;
                        }
                    }
                }
                else if (_currentSession.IsEvent && _currentSession.EventInstance.isValid())
                {
                    _currentSession.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                    playing = (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING);
                    if (playing)
                    {
                        _currentSession.EventInstance.getTimelinePosition(out int pos);
                        currentPos = (uint)pos;
                    }
                }

                // If FMOD reports not playing but our state is playing, update and clean up.
                if (_isPlaying && !playing)
                {
                    Stop();
                }
                else
                {
                    _isPlaying = playing;
                }

                return (_isPlaying, currentPos, _currentTotalLengthMs);
            }
        }

        /// <summary>
        /// Sets the volume for the current playback session.
        /// </summary>
        /// <param name="volume">The new volume level (0.0 to 1.0).</param>
        public void SetVolume(float volume)
        {
            lock (SyncLock)
            {
                if (_currentSession == null)
                {
                    return;
                }

                if (_currentSession.Channel.hasHandle())
                {
                    _currentSession.Channel.setVolume(volume);
                }

                if (_currentSession.EventInstance.isValid())
                {
                    _currentSession.EventInstance.setVolume(volume);
                }
            }
        }

        /// <summary>
        /// Sets the looping mode for the current playback session.
        /// </summary>
        /// <param name="isLooping">A flag to enable or disable looping.</param>
        public void SetLooping(bool isLooping)
        {
            lock (SyncLock)
            {
                if (_currentSession != null && _currentSession.Channel.hasHandle())
                {
                    _currentSession.Channel.setMode(isLooping ? MODE.LOOP_NORMAL : MODE.LOOP_OFF);
                }
            }
        }

        /// <summary>
        /// Sets the playback position for the current session.
        /// </summary>
        /// <param name="positionMs">The new position in milliseconds.</param>
        public void SetPosition(uint positionMs)
        {
            lock (SyncLock)
            {
                if (_currentTotalLengthMs > 0 && _currentSession != null)
                {
                    if (_currentSession.Channel.hasHandle())
                    {
                        _currentSession.Channel.setPosition(positionMs, TIMEUNIT.MS);
                    }
                    else if (_currentSession.EventInstance.isValid())
                    {
                        _currentSession.EventInstance.setTimelinePosition((int)positionMs);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the total length for the currently loaded sound, used for UI display.
        /// </summary>
        /// <param name="length">The total length in milliseconds.</param>
        public void SetCurrentTotalLength(uint length)
        {
            _currentTotalLengthMs = length;
        }

        #endregion
    }
}