/**
 * @file DataModels.cs
 * @brief Contains all data structures, enumerations, and node classes used throughout the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file centralizes the definitions of core data models, including FMOD-related specifications,
 * file system structures, and manifest schemas for rebuilding. It uses an inheritance-based 
 * approach for TreeView nodes to store FMOD objects and metadata efficiently.
 *
 * Key Features:
 *  - FMOD Specifications: Defines constants and offsets for FSB5, FSB4, and FSB3 headers.
 *  - Node Hierarchy: Provides specialized classes for Banks, Events, Busses, and Audio Data.
 *  - Manifest System: Defines the JSON structure used for maintaining audio metadata during rebuilding.
 *  - Progress Tracking: Includes structures for reporting asynchronous operation status.
 *  - Thread-safe Logging: Implements a synchronized LogWriter for multi-threaded operations.
 *  - Search Data Model: Defines a pure data class for search results, decoupling logic from UI controls.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Key Dependencies: FMOD Core/Studio API, Newtonsoft.Json
 *  - Last Update: 2025-01-04
 */

using System;
using System.Collections.Generic;
using System.IO;
using FMOD; // Core API
using FMOD.Studio; // Studio API
using Newtonsoft.Json; // Required for manifest generation.

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    #region 1. Shared Specs & Enums

    /// <summary>
    /// Contains application-wide constants for buffer sizes, units, and algorithm weights.
    /// </summary>
    public static class AppConstants
    {
        // I/O Buffer Sizes.

        /// <summary>
        /// Defines the buffer size (4KB) used for default FileStream buffering and reading small binary headers.
        /// This size aligns with common filesystem block sizes (4096 bytes), ensuring efficient small-scale I/O operations.
        /// </summary>
        public const int BufferSizeSmall = 4096;

        /// <summary>
        /// Defines the buffer size (16KB) used as the chunk size when decoding FMOD Sound data into memory buffers.
        /// This value offers a balance between minimizing read operations and conserving memory for audio chunk processing.
        /// </summary>
        public const int BufferSizeMedium = 16384;

        /// <summary>
        /// Defines the buffer size (64KB) used for efficient sequential reading of large files (e.g., .bank, .fsb).
        /// This size is a multiple of common disk cluster sizes and helps maximize I/O throughput by reducing the number of system calls.
        /// </summary>
        public const int BufferSizeLarge = 65536;

        /// <summary>
        /// Defines the buffer size (80KB) used specifically in RebuildService for high-throughput bulk data transfers.
        /// This value is an exact multiple of BufferSizeMedium (16KB * 5), which improves cache efficiency.
        /// </summary>
        public const int BufferSizeXLarge = 81920;

        // Unit Conversions.

        /// <summary>
        /// Represents the conversion factor from bytes to megabytes (1048576.0).
        /// </summary>
        public const double BytesToMegabytes = 1048576.0;

        // Progress Calculation Weights (AssetLoader).

        /// <summary>
        /// Represents the percentage (10.0%) of the progress bar allocated to the initial file discovery phase.
        /// </summary>
        public const double ProgressWeightInit = 10.0;

        /// <summary>
        /// Represents the percentage (85.0%) of the progress bar allocated to the main parallel file analysis phase.
        /// </summary>
        public const double ProgressWeightAnalysis = 85.0;

        // External Tools.

        /// <summary>
        /// Specifies the filename ("fsbankcl.exe") of the external FMOD build tool executable.
        /// </summary>
        public const string FsBankExecutable = "fsbankcl.exe";
    }

    /// <summary>
    /// Contains constant values and offsets related to FMOD Sound Bank (FSB) file structures.
    /// </summary>
    public static class FsbSpecs
    {
        // Signatures for different FSB versions.

        /// <summary>
        /// Represents the signature string for FSB5 format.
        /// </summary>
        public const string SignatureFSB5 = "FSB5";

        /// <summary>
        /// Represents the signature string for FSB4 format.
        /// </summary>
        public const string SignatureFSB4 = "FSB4";

        /// <summary>
        /// Represents the signature string for FSB3 format.
        /// </summary>
        public const string SignatureFSB3 = "FSB3";

        /// <summary>
        /// Defines the length of the FSB signature string (4 bytes).
        /// </summary>
        public const uint SignatureLength = 4;

        // FSB5 Header Offsets.

        /// <summary>
        /// Defines the offset 0x08 in the FSB5 header.
        /// </summary>
        public const int Offset_0x08 = 0x08;

        /// <summary>
        /// Defines the offset 0x0C in the FSB5 header.
        /// </summary>
        public const int Offset_0x0C = 0x0C;

        /// <summary>
        /// Defines the offset 0x10 in the FSB5 header.
        /// </summary>
        public const int Offset_0x10 = 0x10;

        /// <summary>
        /// Defines the standard size of the FSB5 header (0x40 bytes).
        /// </summary>
        public const int HeaderSize_FSB5 = 0x40;

        // FSB5 Sample Header Versioning.

        /// <summary>
        /// Defines the sample header size for FSB5 version 0.
        /// </summary>
        public const int SampleHeaderSize_Ver0 = 64;

        /// <summary>
        /// Defines the sample data offset for FSB5 version 0.
        /// </summary>
        public const int SampleDataOffset_Ver0 = 52;

        /// <summary>
        /// Defines the sample header size for FSB5 version 1.
        /// </summary>
        public const int SampleHeaderSize_Ver1 = 80;

        /// <summary>
        /// Defines the sample data offset for FSB5 version 1.
        /// </summary>
        public const int SampleDataOffset_Ver1 = 68;

        // Legacy FSB4/FSB3 Header Offsets.

        /// <summary>
        /// Defines the standard size of the FSB4 header.
        /// </summary>
        public const int HeaderSize_FSB4 = 48;

        /// <summary>
        /// Defines the offset to the number of samples in FSB4.
        /// </summary>
        public const int Offset_FSB4_NumSamples = 0x04;

        /// <summary>
        /// Defines the offset to the sample header size in FSB4.
        /// </summary>
        public const int Offset_FSB4_SHdrSize = 0x08;

        /// <summary>
        /// Defines the offset to the data size in FSB4.
        /// </summary>
        public const int Offset_FSB4_DataSize = 0x0C;

        /// <summary>
        /// Defines the offset to the mode flags in FSB4.
        /// </summary>
        public const int Offset_FSB4_Mode = 0x14;

        /// <summary>
        /// Defines the size of the initial legacy header data to skip for FSB4.
        /// </summary>
        public const int LegacyHeaderSkip_FSB4 = 24;

        /// <summary>
        /// Defines the standard size of the FSB3 header.
        /// </summary>
        public const int HeaderSize_FSB3 = 24;

        /// <summary>
        /// Defines the offset to the number of samples in FSB3.
        /// </summary>
        public const int Offset_FSB3_NumSamples = 0x04;

        /// <summary>
        /// Defines the offset to the sample header size in FSB3.
        /// </summary>
        public const int Offset_FSB3_SHdrSize = 0x08;

        /// <summary>
        /// Defines the offset to the data size in FSB3.
        /// </summary>
        public const int Offset_FSB3_DataSize = 0x0C;

        /// <summary>
        /// Defines the offset to the mode flags in FSB3.
        /// </summary>
        public const int Offset_FSB3_Mode = 0x14;

        // Legacy Sample Header field sizes.

        /// <summary>
        /// Defines the size of the sample size field in legacy headers (2 bytes).
        /// </summary>
        public const int LegacySampleHeader_SizeField = 2;

        // Generic Legacy Constraints.

        /// <summary>
        /// Defines the minimum allowed size for a sample header.
        /// </summary>
        public const int MinSampleHeaderSize = 24;

        /// <summary>
        /// Defines the maximum allowed size for a sample header.
        /// </summary>
        public const int MaxSampleHeaderSize = 128;

        /// <summary>
        /// Defines the length of the name field in legacy headers.
        /// </summary>
        public const int NameFieldLength = 30;

        /// <summary>
        /// Defines the byte alignment requirement for legacy formats.
        /// </summary>
        public const int LegacyAlignment = 32;

        /// <summary>
        /// Defines the overlap size used during scanning.
        /// </summary>
        public const int ScanOverlapSize = 64;
    }

    /// <summary>
    /// Defines flags for FMOD Sound Bank playback and encoding modes.
    /// </summary>
    [Flags]
    public enum FsbModeFlags : uint
    {
        /// <summary>
        /// Represents no specific mode or flag.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Indicates that the sound loops normally.
        /// </summary>
        LoopNormal = 0x00000002,

        /// <summary>
        /// Indicates 8-bit audio data.
        /// </summary>
        Bits8 = 0x00000008,

        /// <summary>
        /// Indicates monaural audio (single channel).
        /// </summary>
        Mono = 0x00000020,

        /// <summary>
        /// Indicates stereo audio (two channels).
        /// </summary>
        Stereo = 0x00000040,

        /// <summary>
        /// Indicates MPEG (MP3) compression.
        /// </summary>
        Mpeg = 0x00020000,

        /// <summary>
        /// Indicates MPEG compression with padding.
        /// </summary>
        MpegPadded = 0x00200000,

        /// <summary>
        /// Indicates IMA ADPCM compression.
        /// </summary>
        ImaAdpcm = 0x00400000,

        /// <summary>
        /// Indicates VAG (PlayStation) compression.
        /// </summary>
        Vag = 0x00800000,

        /// <summary>
        /// Indicates XMA (Xbox) compression.
        /// </summary>
        Xma = 0x01000000,

        /// <summary>
        /// Indicates GameCube ADPCM compression.
        /// </summary>
        GcAdpcm = 0x02000000,

        /// <summary>
        /// Indicates that tags should be ignored.
        /// </summary>
        IgnoreTags = 0x02000000
    }

    /// <summary>
    /// Categorizes the types of nodes that can be displayed in the application's hierarchical views.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// Represents a Bank node.
        /// </summary>
        Bank,

        /// <summary>
        /// Represents an Event node.
        /// </summary>
        Event,

        /// <summary>
        /// Represents a Bus node.
        /// </summary>
        Bus,

        /// <summary>
        /// Represents a VCA node.
        /// </summary>
        VCA,

        /// <summary>
        /// Represents an FSB file container node.
        /// </summary>
        FsbFile,

        /// <summary>
        /// Represents a SubSound node.
        /// </summary>
        SubSound,

        /// <summary>
        /// Represents a raw AudioData node.
        /// </summary>
        AudioData
    }

    /// <summary>
    /// Represents a single audio replacement task in a batch rebuild operation.
    /// </summary>
    public struct BatchItem
    {
        /// <summary>
        /// Gets or sets the index of the sub-sound to be replaced.
        /// </summary>
        public int TargetIndex;

        /// <summary>
        /// Gets or sets the full path to the new audio file.
        /// </summary>
        public string NewFilePath;
    }

    /// <summary>
    /// Contains comprehensive metadata about a specific audio sub-sound.
    /// Expanded to include advanced FMOD attributes (Tags, 3D, SyncPoints, etc.).
    /// </summary>
    public struct AudioInfo
    {
        // Basic Info.
        public string Name;
        public uint LengthMs;
        public uint LengthPcm;
        public SOUND_TYPE Type;
        public SOUND_FORMAT Format;
        public int Channels;
        public int Bits;
        public int Frequency;
        public int Priority;

        // Looping.
        public uint LoopStart;
        public uint LoopEnd;
        public MODE Mode;

        // File Info.
        public int Index;
        public string SourcePath;
        public long FileOffset;
        public uint DataOffset;
        public uint DataLength;

        // Extended FMOD Info (For Detailed View).
        public float MinDistance3D;
        public float MaxDistance3D;
        public float InsideConeAngle;
        public float OutsideConeAngle;
        public float OutsideVolume;

        public int MusicChannelCount;
        public float MusicSpeed;

        public List<string> Tags;
        public List<string> SyncPoints;
    }

    /// <summary>
    /// Contains summary metadata for an entire FSB container.
    /// </summary>
    public struct FsbContainerInfo
    {
        public int NumSubSounds;
        public uint LengthMs;
        public SOUND_TYPE Type;
        public SOUND_FORMAT Format;
        public int Channels;
        public int Bits;
        public int Frequency;
        public int Priority;
        public MODE Mode;
        public List<string> Tags;
        public List<string> SyncPoints;
    }

    /// <summary>
    /// Defines the configuration settings for the FMOD Sound Bank rebuilding process.
    /// </summary>
    public class RebuildOptions
    {
        /// <summary>
        /// Gets or sets the target encoding format for the rebuilt bank.
        /// </summary>
        public SOUND_TYPE EncodingFormat { get; set; }

        /// <summary>
        /// Gets or sets the compression quality (primarily for Vorbis).
        /// </summary>
        public int Quality { get; set; }
    }

    #endregion

    #region 2. Node Data Models

    /// <summary>
    /// Serves as the base class for all data objects attached to TreeView nodes.
    /// </summary>
    public abstract class NodeData
    {
        /// <summary>
        /// Gets the category of the node.
        /// </summary>
        public abstract NodeType Type { get; }

        /// <summary>
        /// Gets or sets the associated native FMOD object (e.g., Bank, EventDescription).
        /// </summary>
        public object FmodObject { get; set; }

        /// <summary>
        /// Gets or sets additional contextual information, such as file paths.
        /// </summary>
        public string ExtraInfo { get; set; }

        /// <summary>
        /// Gets or sets the byte offset of the FSB chunk within a parent bank file.
        /// </summary>
        public long FsbChunkOffset { get; set; }

        /// <summary>
        /// Retrieves a formatted list of properties and their values for the details panel.
        /// </summary>
        /// <returns>A list of key-value pairs representing object metadata.</returns>
        public abstract List<KeyValuePair<string, string>> GetDetails();
    }

    /// <summary>
    /// Represents a leaf node containing specific audio data and its technical properties.
    /// </summary>
    public class AudioDataNode : NodeData
    {
        // Category constants for UI display.
        private const string CatBasicInfo = "Basic Information";
        private const string CatFormat = "Format";
        private const string Cat3D = "3D Settings";
        private const string CatLooping = "Looping";
        private const string CatMusic = "Music Info";
        private const string CatTags = "Metadata Tags";
        private const string CatSync = "Sync Points";
        private const string CatData = "Data Layout";

        /// <summary>
        /// Gets the category as AudioData.
        /// </summary>
        public override NodeType Type => NodeType.AudioData;

        /// <summary>
        /// Gets the underlying audio metadata structure.
        /// </summary>
        public AudioInfo CachedAudio { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDataNode"/> class.
        /// </summary>
        /// <param name="audioInfo">The metadata associated with the audio stream.</param>
        /// <param name="fsbOffset">The offset of the parent FSB container.</param>
        /// <param name="sourcePath">The path to the source file containing this audio.</param>
        public AudioDataNode(AudioInfo audioInfo, long fsbOffset, string sourcePath)
        {
            CachedAudio = audioInfo;
            FsbChunkOffset = fsbOffset;
            ExtraInfo = sourcePath;
        }

        /// <summary>
        /// Generates a detailed list of technical specifications for the audio.
        /// Now includes extended FMOD properties (3D, Tags, SyncPoints).
        /// </summary>
        /// <returns>A list of technical properties.</returns>
        public override List<KeyValuePair<string, string>> GetDetails()
        {
            var details = new List<KeyValuePair<string, string>>();
            var info = CachedAudio;

            // Add basic information.
            details.Add(new KeyValuePair<string, string>(CatBasicInfo, $"Name: {info.Name}"));
            details.Add(new KeyValuePair<string, string>(CatBasicInfo, $"Source File: {Path.GetFileName(info.SourcePath)}"));
            details.Add(new KeyValuePair<string, string>(CatBasicInfo, $"Index: {info.Index}"));
            details.Add(new KeyValuePair<string, string>(CatBasicInfo, $"Length: {info.LengthMs} ms ({info.LengthPcm} samples)"));

            // Determine the display string for the audio encoding format.
            string encodingDisplay = info.Type.ToString();
            string containerDisplay = info.Format.ToString();
            uint modeFlags = (uint)info.Mode;

            if (info.Type == SOUND_TYPE.VORBIS)
            {
                encodingDisplay = "Vorbis";
            }
            else if (info.Type == SOUND_TYPE.FADPCM)
            {
                encodingDisplay = "FADPCM";
            }
            else if (info.Type == SOUND_TYPE.AT9)
            {
                encodingDisplay = "ATRAC9";
            }
            else if (info.Type == SOUND_TYPE.XMA)
            {
                encodingDisplay = "XMA";
            }
            else if (info.Type == SOUND_TYPE.MPEG)
            {
                encodingDisplay = "MPEG (MP3)";
            }
            else
            {
                // Handle legacy and compressed formats.
                if ((modeFlags & (uint)FsbModeFlags.ImaAdpcm) != 0)
                {
                    encodingDisplay = "IMA ADPCM";
                    containerDisplay = "FSB (Compressed)";
                }
                else if ((modeFlags & (uint)FsbModeFlags.GcAdpcm) != 0 && info.Type == SOUND_TYPE.RAW)
                {
                    encodingDisplay = "GameCube ADPCM";
                    containerDisplay = "FSB (Compressed)";
                }
                else if ((modeFlags & (uint)FsbModeFlags.Xma) != 0)
                {
                    encodingDisplay = "XMA (Xbox)";
                    containerDisplay = "FSB (Compressed)";
                }
                else if ((modeFlags & (uint)FsbModeFlags.Vag) != 0)
                {
                    encodingDisplay = "VAG (PlayStation)";
                    containerDisplay = "FSB (Compressed)";
                }
                else if (info.Format == SOUND_FORMAT.PCM16)
                {
                    encodingDisplay = "PCM 16-bit";
                }
                else if (info.Format == SOUND_FORMAT.PCM8)
                {
                    encodingDisplay = "PCM 8-bit";
                }
                else if (info.Format == SOUND_FORMAT.PCMFLOAT)
                {
                    encodingDisplay = "PCM Float 32-bit";
                }
            }

            // Add format and technical properties.
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Encoding: {encodingDisplay}"));
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Container: {containerDisplay}"));
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Channels: {info.Channels}"));
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Frequency: {info.Frequency} Hz"));
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Bits: {info.Bits}-bit"));
            details.Add(new KeyValuePair<string, string>(CatFormat, $"Priority: {info.Priority}"));

            // Add 3D sound settings if applicable.
            if ((info.Mode & MODE._3D) != 0)
            {
                details.Add(new KeyValuePair<string, string>(Cat3D, $"Min/Max Distance: {info.MinDistance3D:F2} / {info.MaxDistance3D:F2}"));

                // Check if custom cone settings are applied (360 is the default full circle).
                if (info.InsideConeAngle < 360 || info.OutsideConeAngle < 360)
                {
                    details.Add(new KeyValuePair<string, string>(Cat3D, $"Cone (In/Out/Vol): {info.InsideConeAngle}/{info.OutsideConeAngle}/{info.OutsideVolume}"));
                }
            }

            // Add looping information.
            bool hasLoop = (info.Mode & MODE.LOOP_NORMAL) != 0 || (info.LoopStart != 0 || info.LoopEnd != 0);
            details.Add(new KeyValuePair<string, string>(CatLooping, $"Enabled: {hasLoop}"));

            if (hasLoop)
            {
                details.Add(new KeyValuePair<string, string>(CatLooping, $"Range (ms): {info.LoopStart} - {info.LoopEnd}"));
            }
            details.Add(new KeyValuePair<string, string>(CatLooping, $"Mode Flags: {info.Mode}"));

            // Add music module information.
            if (info.MusicChannelCount > 0)
            {
                details.Add(new KeyValuePair<string, string>(CatMusic, $"Channels: {info.MusicChannelCount}"));
                details.Add(new KeyValuePair<string, string>(CatMusic, $"Speed: {info.MusicSpeed}"));
            }

            // Add metadata tags.
            if (info.Tags != null && info.Tags.Count > 0)
            {
                foreach (var tag in info.Tags)
                {
                    details.Add(new KeyValuePair<string, string>(CatTags, tag));
                }
            }
            else
            {
                details.Add(new KeyValuePair<string, string>(CatTags, "None"));
            }

            // Add sync points.
            if (info.SyncPoints != null && info.SyncPoints.Count > 0)
            {
                foreach (var point in info.SyncPoints)
                {
                    details.Add(new KeyValuePair<string, string>(CatSync, point));
                }
            }
            else
            {
                details.Add(new KeyValuePair<string, string>(CatSync, "None"));
            }

            // Add raw data layout information.
            details.Add(new KeyValuePair<string, string>(CatData, $"Offset: 0x{info.DataOffset:X}"));
            details.Add(new KeyValuePair<string, string>(CatData, $"Size: {info.DataLength} bytes"));

            return details;
        }
    }

    /// <summary>
    /// Represents an FMOD Studio Bank file.
    /// </summary>
    public class BankNode : NodeData
    {
        /// <summary>
        /// Gets the category as Bank.
        /// </summary>
        public override NodeType Type => NodeType.Bank;

        /// <summary>
        /// Gets the native FMOD Studio Bank object.
        /// </summary>
        public Bank BankObject => (Bank)FmodObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="BankNode"/> class.
        /// </summary>
        /// <param name="path">The full path to the .bank file.</param>
        public BankNode(string path)
        {
            ExtraInfo = path;
        }

        /// <summary>
        /// Retrieves the bank's path and GUID.
        /// </summary>
        /// <returns>A list containing bank metadata.</returns>
        public override List<KeyValuePair<string, string>> GetDetails()
        {
            var details = new List<KeyValuePair<string, string>>();

            // Add basic file information.
            details.Add(new KeyValuePair<string, string>("Bank Information", $"File Path: {ExtraInfo}"));

            if (FmodObject is Bank bank && bank.isValid())
            {
                // Retrieve FMOD object identification.
                bank.getID(out GUID id);
                bank.getPath(out string fmodPath);
                details.Add(new KeyValuePair<string, string>("Bank Information", $"FMOD Path: {fmodPath}"));
                details.Add(new KeyValuePair<string, string>("Bank Information", $"GUID: {Utilities.GuidToString(id)}"));

                // Retrieve loading states.
                bank.getLoadingState(out LOADING_STATE loadingState);
                bank.getSampleLoadingState(out LOADING_STATE sampleLoadingState);
                details.Add(new KeyValuePair<string, string>("State", $"Bank Loading: {loadingState}"));
                details.Add(new KeyValuePair<string, string>("State", $"Sample Loading: {sampleLoadingState}"));

                // Retrieve content counts.
                bank.getEventCount(out int eventCount);
                bank.getBusCount(out int busCount);
                bank.getVCACount(out int vcaCount);
                bank.getStringCount(out int stringCount);
                details.Add(new KeyValuePair<string, string>("Contents", $"Event Count: {eventCount}"));
                details.Add(new KeyValuePair<string, string>("Contents", $"Bus Count: {busCount}"));
                details.Add(new KeyValuePair<string, string>("Contents", $"VCA Count: {vcaCount}"));
                details.Add(new KeyValuePair<string, string>("Contents", $"String Count: {stringCount}"));
            }
            else
            {
                details.Add(new KeyValuePair<string, string>("Bank Information", "Status: Not loaded or invalid FMOD object"));
            }
            return details;
        }
    }

    /// <summary>
    /// Represents an FMOD Studio Event.
    /// </summary>
    public class EventNode : NodeData
    {
        /// <summary>
        /// Gets the category as Event.
        /// </summary>
        public override NodeType Type => NodeType.Event;

        /// <summary>
        /// Gets the native FMOD Studio EventDescription object.
        /// </summary>
        public EventDescription EventObject => (EventDescription)FmodObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventNode"/> class.
        /// </summary>
        /// <param name="evt">The native FMOD event description.</param>
        public EventNode(EventDescription evt)
        {
            FmodObject = evt;
        }

        /// <summary>
        /// Retrieves the event's full path and GUID.
        /// </summary>
        /// <returns>A list containing event metadata.</returns>
        public override List<KeyValuePair<string, string>> GetDetails()
        {
            var details = new List<KeyValuePair<string, string>>();
            if (FmodObject is EventDescription evt && evt.isValid())
            {
                evt.getID(out GUID id);
                evt.getPath(out string path);
                details.Add(new KeyValuePair<string, string>("Event", $"Path: {path}"));
                details.Add(new KeyValuePair<string, string>("Event", $"GUID: {Utilities.GuidToString(id)}"));
            }
            return details;
        }
    }

    /// <summary>
    /// Represents an FMOD Studio Bus.
    /// </summary>
    public class BusNode : NodeData
    {
        /// <summary>
        /// Gets the category as Bus.
        /// </summary>
        public override NodeType Type => NodeType.Bus;

        /// <summary>
        /// Gets the native FMOD Studio Bus object.
        /// </summary>
        public Bus BusObject => (Bus)FmodObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusNode"/> class.
        /// </summary>
        /// <param name="bus">The native FMOD bus object.</param>
        public BusNode(Bus bus)
        {
            FmodObject = bus;
        }

        /// <summary>
        /// Retrieves the bus's path and GUID.
        /// </summary>
        /// <returns>A list containing bus metadata.</returns>
        public override List<KeyValuePair<string, string>> GetDetails()
        {
            var details = new List<KeyValuePair<string, string>>();
            if (FmodObject is Bus bus && bus.isValid())
            {
                bus.getID(out GUID id);
                bus.getPath(out string path);
                details.Add(new KeyValuePair<string, string>("Bus", $"Path: {path}"));
                details.Add(new KeyValuePair<string, string>("Bus", $"GUID: {Utilities.GuidToString(id)}"));
            }
            return details;
        }
    }

    /// <summary>
    /// Represents a standalone or embedded FMOD Sound Bank (FSB) container.
    /// </summary>
    public class FsbFileNode : NodeData
    {
        public override NodeType Type => NodeType.FsbFile;
        public FsbContainerInfo? ContainerInfo { get; set; }

        public FsbFileNode(string sourcePath, long fsbOffset)
        {
            ExtraInfo = sourcePath;
            FsbChunkOffset = fsbOffset;
        }

        public override List<KeyValuePair<string, string>> GetDetails()
        {
            var details = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("FSB Container", $"Source File: {Path.GetFileName(ExtraInfo)}"),
                new KeyValuePair<string, string>("FSB Container", $"File Offset: 0x{FsbChunkOffset:X}")
            };

            // If detailed container info is available, display it.
            if (ContainerInfo.HasValue)
            {
                var info = ContainerInfo.Value;

                // Add basic information.
                details.Add(new KeyValuePair<string, string>("Basic Information", $"Length: {info.LengthMs} ms"));
                details.Add(new KeyValuePair<string, string>("Basic Information", $"Sound Type: {info.Type}"));
                details.Add(new KeyValuePair<string, string>("Basic Information", $"Sound Format: {info.Format}"));
                details.Add(new KeyValuePair<string, string>("Basic Information", $"Channels: {info.Channels}"));
                details.Add(new KeyValuePair<string, string>("Basic Information", $"Bits Per Sample: {info.Bits}"));

                // Add FSB file structure info.
                details.Add(new KeyValuePair<string, string>("FSB Structure", $"Sub Sound Count: {info.NumSubSounds}"));

                // Add default settings and loop information.
                details.Add(new KeyValuePair<string, string>("Default Settings", $"Frequency: {info.Frequency}"));
                details.Add(new KeyValuePair<string, string>("Default Settings", $"Priority: {info.Priority}"));
                details.Add(new KeyValuePair<string, string>("Default Settings", $"Mode: {info.Mode}"));

                // Add metadata tags.
                if (info.Tags != null && info.Tags.Count > 0)
                {
                    foreach (var tag in info.Tags)
                    {
                        details.Add(new KeyValuePair<string, string>("Metadata Tags", tag));
                    }
                }
                else
                {
                    details.Add(new KeyValuePair<string, string>("Metadata Tags", "None"));
                }

                // Add sync points.
                if (info.SyncPoints != null && info.SyncPoints.Count > 0)
                {
                    foreach (var point in info.SyncPoints)
                    {
                        details.Add(new KeyValuePair<string, string>("Sync Points", point));
                    }
                }
                else
                {
                    details.Add(new KeyValuePair<string, string>("Sync Points", "None"));
                }
            }
            return details;
        }
    }

    #endregion

    #region 3. Manifest Models for Rebuilding

    /// <summary>
    /// Defines the root structure of the manifest file used for rebuilding an FSB container.
    /// </summary>
    public class FsbManifest
    {
        /// <summary>
        /// Gets or sets the target encoding format for the rebuild.
        /// </summary>
        [JsonProperty("build_format")]
        public SOUND_TYPE BuildFormat { get; set; } = SOUND_TYPE.VORBIS;

        /// <summary>
        /// Gets or sets the list of metadata for each sub-sound.
        /// </summary>
        [JsonProperty("sub_sounds")]
        public List<SubSoundManifestInfo> SubSounds { get; set; }
    }

    /// <summary>
    /// Contains metadata and timeline information for a single sub-sound within a manifest.
    /// </summary>
    public class SubSoundManifestInfo
    {
        /// <summary>
        /// Gets or sets the index of the sub-sound.
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the name of the sub-sound.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the original file name of the sub-sound.
        /// </summary>
        [JsonProperty("original_file_name")]
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sub-sound loops.
        /// </summary>
        [JsonProperty("looping")]
        public bool Looping { get; set; }

        /// <summary>
        /// Gets or sets the loop start point in milliseconds.
        /// </summary>
        [JsonProperty("loop_start_ms")]
        public uint LoopStart { get; set; }

        /// <summary>
        /// Gets or sets the loop end point in milliseconds.
        /// </summary>
        [JsonProperty("loop_end_ms")]
        public uint LoopEnd { get; set; }
    }

    #endregion

    #region 4. Rebuild Service Models

    /// <summary>
    /// Enumerates the possible outcomes of a rebuild operation.
    /// </summary>
    public enum RebuildStatus
    {
        /// <summary>
        /// Indicates that the rebuild operation completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Indicates that the rebuild operation failed due to an error.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicates that the operation was cancelled by the user.
        /// </summary>
        CancelledByUser,

        /// <summary>
        /// Indicates that the rebuilt file is larger than the original and requires user confirmation.
        /// </summary>
        OversizedConfirmationNeeded
    }

    /// <summary>
    /// Represents the final report of a rebuild operation.
    /// </summary>
    public class RebuildResult
    {
        /// <summary>
        /// Gets or sets the status of the operation.
        /// </summary>
        public RebuildStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a descriptive message regarding the result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the path to the temporary workspace used during rebuilding.
        /// </summary>
        public string WorkspacePath { get; set; }

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success => Status == RebuildStatus.Success;

        /// <summary>
        /// Gets or sets the path to the rebuilt FSB file in the temporary workspace.
        /// This is used to resume an oversized build without rebuilding.
        /// </summary>
        public string TemporaryFsbPath { get; set; }

        /// <summary>
        /// Gets or sets the size of the original FSB chunk in bytes.
        /// </summary>
        public long OriginalFsbSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the newly built FSB chunk in bytes.
        /// </summary>
        public long NewFsbSize { get; set; }
    }

    #endregion

    #region 5. Helper Classes & Structs

    /// <summary>
    /// Encapsulates a progress update containing a message and a percentage value.
    /// </summary>
    public struct ProgressReport
    {
        /// <summary>
        /// Gets the current status message.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the progress percentage (0-100).
        /// </summary>
        public int Percentage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressReport"/> struct.
        /// </summary>
        /// <param name="status">The message describing the current step.</param>
        /// <param name="percentage">The completion percentage.</param>
        public ProgressReport(string status, int percentage)
        {
            Status = status;
            Percentage = percentage;
        }
    }

    /// <summary>
    /// Provides thread-safe logging functionality to a text file.
    /// </summary>
    public class LogWriter : IDisposable
    {
        /// <summary>
        /// Defines the severity levels for log entries.
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// Represents an informational message.
            /// </summary>
            INFO,

            /// <summary>
            /// Represents a warning message.
            /// </summary>
            WARNING,

            /// <summary>
            /// Represents an error message.
            /// </summary>
            ERROR
        }

        private StreamWriter _writer;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter"/> class and opens the file stream.
        /// </summary>
        /// <param name="path">The full path to the log file.</param>
        public LogWriter(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL: Failed to initialize LogWriter for path '{path}'. Reason: {ex.Message}");
                _writer = null;
            }
        }

        /// <summary>
        /// Writes a timestamped message to the log.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public void WriteRaw(string message)
        {
            if (_writer == null)
            {
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                lock (_lock)
                {
                    _writer.WriteLine($"{timestamp} | {message}");
                }
            }
            catch
            {
                // Silently ignore logging failures.
            }
        }

        /// <summary>
        /// Writes a structured tab-separated entry to the log, primarily for extraction reports.
        /// </summary>
        /// <param name="level">The severity level.</param>
        /// <param name="values">The values to be recorded in columns.</param>
        public void LogTSV(LogLevel level, params string[] values)
        {
            if (_writer == null)
            {
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string line = $"{timestamp}\t{level}\t{string.Join("\t", values)}";

                lock (_lock)
                {
                    _writer.WriteLine(line);
                }
            }
            catch
            {
                // Silently ignore logging failures.
            }
        }

        /// <summary>
        /// Closes and disposes of the underlying stream writer.
        /// </summary>
        public void Dispose()
        {
            if (_writer != null)
            {
                try
                {
                    _writer.Close();
                    _writer.Dispose();
                }
                catch
                {
                    // Ignore errors during disposal.
                }

                _writer = null;
            }
        }
    }

    /// <summary>
    /// Represents a single search result item, decoupled from UI controls.
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// Gets or sets the display name of the item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type description of the item.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the full hierarchical path of the item.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is checked.
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// Gets or sets the underlying data object (e.g., TreeNode or NodeData).
        /// </summary>
        public object Tag { get; set; }
    }

    #endregion
}