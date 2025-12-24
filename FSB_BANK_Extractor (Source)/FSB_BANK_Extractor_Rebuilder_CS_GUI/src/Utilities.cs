/**
 * @file Utilities.cs
 * @brief Provides a collection of static helper methods used throughout the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This static class centralizes common functionalities to avoid code duplication and improve maintainability.
 * It includes methods for FMOD API interaction, specialized file I/O operations, data parsing,
 * and string manipulation, ensuring consistent behavior across different parts of the application.
 *
 * Key Features:
 *  - FMOD Helpers: Simplifies common FMOD tasks such as result checking, safe resource release, and data extraction.
 *  - Legacy FSB Parsing: Contains logic to parse headers and sample information from older FSB file formats.
 *  - Audio Decoding: Provides methods for decoding various FMOD formats into standard WAV byte arrays.
 *  - Asynchronous File I/O: Implements async wrappers for file operations to maintain UI responsiveness.
 *  - Path Sanitization: Offers robust methods for cleaning file and path names to prevent filesystem errors.
 *  - Struct Marshaling: Provides a generic helper to read binary data into structs.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Key Dependencies: FMOD Core API
 *  - Last Update: 2025-12-24
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FMOD; // Core API

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Provides a collection of static utility methods for FMOD, file I/O, and string manipulation.
    /// </summary>
    public static class Utilities
    {
        #region Constants & Magic Numbers

        // Standard RIFF/WAVE header signatures.
        private const string RiffSignature = "RIFF";
        private const string WaveSignature = "WAVE";
        private const string FmtSignature = "fmt ";
        private const string DataSignature = "data";

        // WAV format codes and sizes.
        private const ushort WavFormatPcm = 0x0001;
        private const ushort WavFormatFloat = 0x0003;
        private const ushort WavFormatImaAdpcm = 0x0011;
        private const int PcmChunkSize = 16;
        private const int ImaAdpcmChunkSize = 20;

        // FMOD specific constants.
        private const int MaxNameLength = 256;

        #endregion

        #region Struct Marshaling Helper

        /// <summary>
        /// Reads bytes from the BinaryReader and marshals them into a struct.
        /// </summary>
        /// <typeparam name="T">The struct type to read.</typeparam>
        /// <param name="br">The BinaryReader stream.</param>
        /// <returns>The marshaled struct.</returns>
        public static T ReadStruct<T>(BinaryReader br) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] bytes = br.ReadBytes(size);

            // Check if we read enough bytes to populate the struct.
            if (bytes.Length < size)
            {
                throw new EndOfStreamException($"Could not read enough bytes for struct {typeof(T).Name}");
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        #endregion

        #region FMOD General Helpers

        /// <summary>
        /// Checks an FMOD RESULT code and throws a detailed exception if it indicates an error.
        /// </summary>
        /// <param name="result">The FMOD RESULT code to check.</param>
        /// <exception cref="Exception">Thrown when the result is not FMOD.RESULT.OK.</exception>
        public static void CheckFmodResult(RESULT result)
        {
            if (result != RESULT.OK)
            {
                throw new Exception($"FMOD Error [{result}]: {Error.String(result)}");
            }
        }

        /// <summary>
        /// Safely releases an FMOD Sound object if its handle is valid.
        /// </summary>
        /// <param name="sound">The Sound object to release. Passed by reference.</param>
        public static void SafeRelease(ref Sound sound)
        {
            if (sound.hasHandle())
            {
                sound.release();
                sound.clearHandle();
            }
        }

        /// <summary>
        /// Converts an FMOD GUID structure to its standard string representation.
        /// </summary>
        /// <param name="g">The FMOD GUID to convert.</param>
        /// <returns>A string in the format "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx".</returns>
        public static string GuidToString(FMOD.GUID g)
        {
            // FMOD's GUID.Data4 is a ulong, but standard GUIDs use two separate fields.
            // This logic manually reconstructs the standard format.
            byte[] rawBytes = BitConverter.GetBytes(g.Data4);
            byte[] data4Bytes = new byte[8];
            Array.Copy(rawBytes, data4Bytes, Math.Min(rawBytes.Length, 8));

            // Ensure correct byte order regardless of system architecture.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data4Bytes);
            }

            // Extract the two parts of the final GUID section.
            string part4 = BitConverter.ToString(data4Bytes, 0, 2).Replace("-", "");
            string part5 = BitConverter.ToString(data4Bytes, 2, 6).Replace("-", "");

            return $"{g.Data1:X8}-{g.Data2:X4}-{g.Data3:X4}-{part4}-{part5}";
        }

        /// <summary>
        /// Reads the FSB version character ('3', '4', '5', etc.) from a file at a specific offset.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <param name="offset">The starting offset of the FSB header within the file.</param>
        /// <returns>The FSB version character, or '0' if the header is invalid or an error occurs.</returns>
        public static char GetFsbVersion(string path, long offset)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Verify that the file is large enough to contain the FSB header signature.
                    if (fs.Length < offset + 4)
                    {
                        return '0';
                    }

                    fs.Seek(offset, SeekOrigin.Begin);
                    byte[] buffer = new byte[4];
                    if (fs.Read(buffer, 0, 4) != 4)
                    {
                        return '0';
                    }

                    // Validate the 'FSB' signature before returning the version character.
                    if (buffer[0] == 'F' && buffer[1] == 'S' && buffer[2] == 'B')
                    {
                        return (char)buffer[3];
                    }
                }
            }
            // Silently ignore I/O exceptions (e.g., file locked) and return a default value.
            // This prevents the application from crashing during scanning operations.
            catch (Exception)
            {
                return '0';
            }
            return '0';
        }

        #endregion

        #region Audio Info Extraction

        /// <summary>
        /// Extracts summary information for an entire FSB container Sound object.
        /// </summary>
        /// <param name="containerSound">The FMOD Sound object representing the FSB container.</param>
        /// <returns>An <see cref="FsbContainerInfo"/> struct populated with container-level details.</returns>
        public static FsbContainerInfo GetFsbContainerInfo(Sound containerSound)
        {
            var info = new FsbContainerInfo();

            if (!containerSound.hasHandle())
            {
                return info;
            }

            // Retrieve basic container properties including structure and format.
            containerSound.getNumSubSounds(out info.NumSubSounds);
            containerSound.getLength(out info.LengthMs, TIMEUNIT.MS);
            containerSound.getFormat(out info.Type, out info.Format, out info.Channels, out info.Bits);
            containerSound.getMode(out info.Mode);

            // Retrieve default playback settings.
            containerSound.getDefaults(out float frequency, out int priority);
            info.Frequency = (int)frequency;
            info.Priority = priority;

            // Retrieve and parse metadata tags attached to the container.
            info.Tags = new List<string>();
            containerSound.getNumTags(out int numTags, out _);
            for (int i = 0; i < numTags; i++)
            {
                if (containerSound.getTag(null, i, out TAG tag) == RESULT.OK)
                {
                    string tagName = tag.name;
                    string tagVal = $"[{tag.type}] {tagName} ({tag.datatype}, {tag.datalen} bytes)";
                    if ((tag.datatype == TAGDATATYPE.STRING || tag.datatype == TAGDATATYPE.STRING_UTF8) && tag.data != IntPtr.Zero)
                    {
                        string content = Marshal.PtrToStringAnsi(tag.data);
                        tagVal = $"[{tagName}]: {content}";
                    }
                    info.Tags.Add(tagVal);
                }
            }

            // Retrieve synchronization points defined in the container.
            info.SyncPoints = new List<string>();
            containerSound.getNumSyncPoints(out int numSyncPoints);
            for (int i = 0; i < numSyncPoints; i++)
            {
                if (containerSound.getSyncPoint(i, out IntPtr pointPtr) == RESULT.OK)
                {
                    if (containerSound.getSyncPointInfo(pointPtr, out string syncName, MaxNameLength, out uint offset, TIMEUNIT.MS) == RESULT.OK)
                    {
                        info.SyncPoints.Add($"[{i}] {syncName} @ {offset}ms");
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Extracts detailed audio information from an FMOD Sound object.
        /// </summary>
        /// <param name="sub">The FMOD Sound object representing a sub-sound.</param>
        /// <param name="index">The index of the sub-sound within its parent container.</param>
        /// <param name="path">The source file path of the parent container.</param>
        /// <param name="fsbChunkOffset">The offset of the FSB chunk within the source file.</param>
        /// <returns>An <see cref="AudioInfo"/> struct populated with details from the sound object.</returns>
        public static AudioInfo GetAudioInfo(Sound sub, int index, string path, long fsbChunkOffset)
        {
            var info = new AudioInfo { Index = index, SourcePath = path, FileOffset = fsbChunkOffset };

            // Retrieve basic audio properties.
            sub.getName(out info.Name, MaxNameLength);
            sub.getLength(out info.LengthMs, TIMEUNIT.MS);
            sub.getLength(out info.LengthPcm, TIMEUNIT.PCM);
            sub.getFormat(out info.Type, out info.Format, out info.Channels, out info.Bits);
            sub.getMode(out info.Mode);

            // Retrieve default playback settings.
            sub.getDefaults(out float frequency, out int priority);
            info.Frequency = (int)frequency;
            info.Priority = priority;

            // Retrieve loop points if defined.
            sub.getLoopPoints(out info.LoopStart, TIMEUNIT.MS, out info.LoopEnd, TIMEUNIT.MS);

            // Retrieve 3D spatialization settings.
            sub.get3DMinMaxDistance(out info.MinDistance3D, out info.MaxDistance3D);
            sub.get3DConeSettings(out info.InsideConeAngle, out info.OutsideConeAngle, out info.OutsideVolume);

            // Retrieve music-specific information for tracker/module formats.
            sub.getMusicNumChannels(out info.MusicChannelCount);
            if (info.MusicChannelCount > 0)
            {
                sub.getMusicSpeed(out info.MusicSpeed);
            }

            // Retrieve metadata tags.
            info.Tags = new List<string>();
            sub.getNumTags(out int numTags, out _);
            for (int i = 0; i < numTags; i++)
            {
                if (sub.getTag(null, i, out TAG tag) == RESULT.OK)
                {
                    string tagName = tag.name;
                    string tagVal = $"[{tag.type}] {tagName} ({tag.datatype}, {tag.datalen} bytes)";
                    if ((tag.datatype == TAGDATATYPE.STRING || tag.datatype == TAGDATATYPE.STRING_UTF8) && tag.data != IntPtr.Zero)
                    {
                        string content = Marshal.PtrToStringAnsi(tag.data);
                        tagVal = $"[{tagName}]: {content}";
                    }
                    info.Tags.Add(tagVal);
                }
            }

            // Retrieve synchronization points.
            info.SyncPoints = new List<string>();
            sub.getNumSyncPoints(out int numSyncPoints);
            for (int i = 0; i < numSyncPoints; i++)
            {
                if (sub.getSyncPoint(i, out IntPtr pointPtr) == RESULT.OK)
                {
                    if (sub.getSyncPointInfo(pointPtr, out string syncName, MaxNameLength, out uint offset, TIMEUNIT.MS) == RESULT.OK)
                    {
                        info.SyncPoints.Add($"[{i}] {syncName} @ {offset}ms");
                    }
                }
            }

            // Determine data layout (offset and length) within the physical file.
            // This requires manual parsing as FMOD API does not always expose raw file offsets.
            sub.getLength(out info.DataLength, TIMEUNIT.RAWBYTES);

            var (dataOffset, dataLength) = ParseFsbHeaderAndGetSampleInfo(path, (uint)fsbChunkOffset, index);
            info.DataOffset = dataOffset;
            if (dataLength > 0)
            {
                info.DataLength = dataLength;
            }

            return info;
        }

        #endregion

        #region FSB5 Header Parsing

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FSB5Header
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;
            public uint Version;
            public int NumSamples;
            public uint SampleHeadersSize;
            public uint NameTableSize;
            public uint DataSize;
            public uint Mode;
        }

        /// <summary>
        /// Manually parses an FSB5 header to get the precise data offset and length for a specific sample.
        /// </summary>
        /// <param name="filePath">The path to the file containing the FSB data.</param>
        /// <param name="fsbChunkOffset">The starting offset of the FSB5 chunk.</param>
        /// <param name="sampleIndex">The index of the sample to retrieve information for.</param>
        /// <returns>A tuple containing the data offset within the FSB chunk and the data length. Returns (0, 0) on failure.</returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Open the file and seek to the FSB chunk start.
        ///  2) Read and validate the FSB5 header signature.
        ///  3) Calculate the offset of the specific sample header based on version (0 or 1).
        ///  4) Read the sample's internal offset and length.
        ///  5) Compute absolute file offsets and validate boundaries.
        /// </remarks>
        private static (uint, uint) ParseFsbHeaderAndGetSampleInfo(string filePath, uint fsbChunkOffset, int sampleIndex)
        {
            try
            {
                // Step 1: Open the file and seek to the FSB chunk start.
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek(fsbChunkOffset, SeekOrigin.Begin);

                    // Step 2: Read and validate the FSB5 header signature.
                    FSB5Header header = ReadStruct<FSB5Header>(br);

                    if (Encoding.ASCII.GetString(header.Signature) != FsbSpecs.SignatureFSB5)
                    {
                        return (0, 0);
                    }

                    if (sampleIndex >= header.NumSamples)
                    {
                        return (0, 0);
                    }

                    // Step 3: Calculate the offset of the specific sample header based on version.
                    // Different FSB5 sub-versions use different header sizes.
                    uint sampleHeaderEntrySize = (header.Version == 0) ? (uint)FsbSpecs.SampleHeaderSize_Ver0 : (uint)FsbSpecs.SampleHeaderSize_Ver1;
                    uint sampleHeaderFieldsOffset = (header.Version == 0) ? (uint)FsbSpecs.SampleDataOffset_Ver0 : (uint)FsbSpecs.SampleDataOffset_Ver1;

                    long sampleHeaderTableStart = fsbChunkOffset + FsbSpecs.HeaderSize_FSB5;
                    long sampleHeaderOffset = sampleHeaderTableStart + (sampleHeaderEntrySize * sampleIndex);

                    if (sampleHeaderOffset >= fs.Length)
                    {
                        return (0, 0);
                    }

                    // Step 4: Read the sample's internal offset and length.
                    fs.Seek(sampleHeaderOffset + sampleHeaderFieldsOffset, SeekOrigin.Begin);
                    uint sampleDataOffset = br.ReadUInt32();
                    uint sampleDataLength = br.ReadUInt32();

                    // Step 5: Compute absolute file offsets and validate boundaries.
                    uint dataSectionStart = (uint)(sampleHeaderTableStart + header.SampleHeadersSize);
                    uint dataOffsetInFsb = (dataSectionStart - fsbChunkOffset) + sampleDataOffset;

                    if (fsbChunkOffset + dataOffsetInFsb + sampleDataLength > fs.Length)
                    {
                        return (0, 0);
                    }

                    return (dataOffsetInFsb, sampleDataLength);
                }
            }
            // Return a default value on any parsing or I/O error to avoid breaking the calling process.
            catch (Exception)
            {
                return (0, 0);
            }
        }

        #endregion

        #region Audio Decoding & WAV Generation

        /// <summary>
        /// Decodes a compressed or raw FMOD sound into a standard WAV format byte array.
        /// This method serves as a robust fallback for formats not easily handled by direct streaming.
        /// </summary>
        /// <param name="coreSystem">The FMOD Core System instance.</param>
        /// <param name="coreSystemLock">A lock object to ensure thread-safe access to the FMOD system.</param>
        /// <param name="info">The <see cref="AudioInfo"/> struct describing the audio to decode.</param>
        /// <returns>A byte array representing the complete WAV file, or <c>null</c> if decoding fails.</returns>
        /// <remarks>
        /// Processing steps:
        ///  1) Read the raw audio data chunk from the source file.
        ///  2) Prepend a temporary WAV/RIFF header if the format is IMA ADPCM (required for FMOD decoding).
        ///  3) Create an in-memory FMOD Sound from the raw data.
        ///  4) Decode the sound to PCM by reading it into a temporary buffer.
        ///  5) Construct the final WAV file (Header + PCM Data) and return it.
        /// </remarks>
        public static byte[] GetDecodedWavBytes(FMOD.System coreSystem, object coreSystemLock, AudioInfo info)
        {
            Sound s = new Sound();
            Sound sub = new Sound();
            byte[] resultBytes = null;
            byte[] rawAudioData = null;
            GCHandle pinnedArray = default(GCHandle);

            try
            {
                // Step 1: Read the raw audio data chunk from the source file.
                if (info.DataLength > 0)
                {
                    using (FileStream fs = new FileStream(info.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.Seek(info.FileOffset + info.DataOffset, SeekOrigin.Begin);
                        rawAudioData = new byte[info.DataLength];
                        fs.Read(rawAudioData, 0, (int)info.DataLength);
                    }
                }
                else
                {
                    return null;
                }

                // Step 2: Prepend a temporary WAV/RIFF header if the format is IMA ADPCM.
                // FMOD's createSound requires a header hint for certain raw compressed formats.
                bool isImaAdpcm = ((uint)info.Mode & (uint)FsbModeFlags.ImaAdpcm) != 0;
                if (isImaAdpcm)
                {
                    rawAudioData = AddImaAdpcmHeader(rawAudioData, info.Channels, info.Frequency);
                }

                // Step 3: Create an in-memory FMOD Sound from the raw data.
                // We pin the array to get a stable pointer for the native FMOD API.
                pinnedArray = GCHandle.Alloc(rawAudioData, GCHandleType.Pinned);
                IntPtr ptrRawData = pinnedArray.AddrOfPinnedObject();

                uint lenBytes = 0;

                // All FMOD operations must be synchronized to prevent race conditions.
                lock (coreSystemLock)
                {
                    CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO
                    {
                        cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                        length = (uint)rawAudioData.Length
                    };

                    // Provide format hints if it's raw PCM data.
                    if (!isImaAdpcm && info.Type == SOUND_TYPE.RAW)
                    {
                        ex.defaultfrequency = info.Frequency;
                        ex.numchannels = info.Channels;
                        ex.format = info.Format != SOUND_FORMAT.NONE ? info.Format : SOUND_FORMAT.PCM16;
                    }

                    // Attempt primary creation method.
                    RESULT res = coreSystem.createSound(ptrRawData, MODE.OPENMEMORY_POINT | MODE.CREATESTREAM | MODE.IGNORETAGS, ref ex, out s);

                    // Fallback strategy for MPEG formats which may require CREATESAMPLE.
                    if (res != RESULT.OK && info.Type == SOUND_TYPE.MPEG)
                    {
                        res = coreSystem.createSound(ptrRawData, MODE.OPENMEMORY_POINT | MODE.CREATECOMPRESSEDSAMPLE | MODE.OPENONLY | MODE.IGNORETAGS, ref ex, out s);
                    }

                    // Final fallback for raw data if standard methods fail.
                    if (res != RESULT.OK)
                    {
                        ex.defaultfrequency = info.Frequency;
                        ex.numchannels = info.Channels;
                        ex.format = SOUND_FORMAT.PCM16;
                        res = coreSystem.createSound(ptrRawData, MODE.OPENMEMORY_POINT | MODE.OPENRAW | MODE.CREATESAMPLE | MODE.IGNORETAGS, ref ex, out s);
                    }

                    if (res != RESULT.OK)
                    {
                        return null;
                    }

                    // Step 4: Decode the sound to PCM by reading it into a temporary buffer.
                    // We extract the sub-sound to ensure we are targeting the playable audio.
                    s.getSubSound(0, out sub);
                    if (!sub.hasHandle())
                    {
                        sub = s;
                    }

                    // Get final properties after decoding setup.
                    sub.getLength(out lenBytes, TIMEUNIT.PCMBYTES);
                    sub.getFormat(out _, out SOUND_FORMAT fmt, out int ch, out int bits);
                    sub.getDefaults(out float rate, out _);

                    int finalRate = (int)(rate < 100 ? info.Frequency : rate);

                    // Step 5: Construct the final WAV file (Header + PCM Data) and return it.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] header = CreateWavHeader((int)lenBytes, finalRate, ch, 16, false);
                        ms.Write(header, 0, header.Length);

                        // Read decoded data in chunks to handle large files efficiently.
                        byte[] buf = new byte[AppConstants.BufferSizeMedium];
                        uint totalRead = 0;
                        uint read;
                        while (totalRead < lenBytes)
                        {
                            sub.readData(buf, out read);
                            if (read == 0)
                            {
                                break;
                            }
                            ms.Write(buf, 0, (int)read);
                            totalRead += read;
                        }
                        resultBytes = ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDecodedWavBytes Error: {ex.Message}");
                return null;
            }
            finally
            {
                // Ensure native resources are released and the pinned memory is freed.
                lock (coreSystemLock)
                {
                    if (sub.hasHandle() && sub.handle != s.handle)
                    {
                        sub.release();
                    }
                    SafeRelease(ref s);
                }
                if (pinnedArray.IsAllocated)
                {
                    pinnedArray.Free();
                }
            }
            return resultBytes;
        }

        /// <summary>
        /// Constructs a temporary RIFF header for IMA ADPCM data, required for FMOD to decode it.
        /// </summary>
        /// <param name="rawData">The raw IMA ADPCM byte data.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <param name="frequency">The sample rate of the audio.</param>
        /// <returns>A new byte array containing the WAV header followed by the raw data.</returns>
        public static byte[] AddImaAdpcmHeader(byte[] rawData, int channels, int frequency)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes(RiffSignature));
                bw.Write(36 + rawData.Length);
                bw.Write(Encoding.ASCII.GetBytes(WaveSignature));
                bw.Write(Encoding.ASCII.GetBytes(FmtSignature));

                // Write the size of the fmt chunk.
                bw.Write(ImaAdpcmChunkSize);

                // Write the audio format code (IMA ADPCM).
                bw.Write(WavFormatImaAdpcm);

                bw.Write((ushort)channels);
                bw.Write(frequency);

                // Calculate Block Align and Byte Rate standard for IMA ADPCM.
                // BlockAlign = (SamplesPerBlock * NumChannels) e.g., typically 36 * channels.
                short blockAlign = (short)(36 * channels);
                int byteRate = (frequency * blockAlign) / 64;

                bw.Write(byteRate);
                bw.Write(blockAlign);

                // Extra format bytes.
                bw.Write((ushort)4);
                bw.Write((ushort)2);
                bw.Write((ushort)0x0040); // Samples per block hint

                bw.Write(Encoding.ASCII.GetBytes(DataSignature));
                bw.Write(rawData.Length);
                bw.Write(rawData);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Creates a standard 44-byte WAV header for PCM audio data.
        /// </summary>
        /// <param name="length">The length of the raw PCM data in bytes.</param>
        /// <param name="rate">The sample rate (e.g., 44100).</param>
        /// <param name="channels">The number of channels (1 for mono, 2 for stereo).</param>
        /// <param name="bits">The number of bits per sample (e.g., 16).</param>
        /// <param name="isFloat"><c>true</c> if the data is 32-bit floating point; otherwise, <c>false</c>.</param>
        /// <returns>A byte array containing the WAV header.</returns>
        public static byte[] CreateWavHeader(int length, int rate, int channels, int bits, bool isFloat)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Write RIFF chunk descriptor.
                bw.Write(Encoding.ASCII.GetBytes(RiffSignature));
                bw.Write(36 + length);
                bw.Write(Encoding.ASCII.GetBytes(WaveSignature));

                // Write "fmt " sub-chunk.
                bw.Write(Encoding.ASCII.GetBytes(FmtSignature));

                // Write sub-chunk size (16 for PCM).
                bw.Write(PcmChunkSize);

                // Write audio format (1 for PCM, 3 for Float).
                bw.Write((ushort)(isFloat ? WavFormatFloat : WavFormatPcm));

                bw.Write((short)channels);
                bw.Write(rate);

                // Calculate and write Byte Rate.
                bw.Write(rate * channels * bits / 8);

                // Calculate and write Block Align.
                bw.Write((short)(channels * bits / 8));

                bw.Write((short)bits);

                // Write "data" sub-chunk.
                bw.Write(Encoding.ASCII.GetBytes(DataSignature));
                bw.Write(length);
                return ms.ToArray();
            }
        }

        #endregion

        #region File I/O Helpers

        /// <summary>
        /// Asynchronously reads the entire contents of a file into a byte array.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the file's contents as a byte array.</returns>
        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            // Use FileOptions.Asynchronous for true non-blocking I/O in .NET Framework 4.8.
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, AppConstants.BufferSizeSmall, FileOptions.Asynchronous))
            using (MemoryStream ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously reads the entire contents of a file into a string using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the file's contents as a string.</returns>
        public static async Task<string> ReadAllTextAsync(string path)
        {
            // Use FileOptions.Asynchronous to maintain UI responsiveness during large file reads.
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, AppConstants.BufferSizeSmall, FileOptions.Asynchronous))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                return await sr.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Asynchronously writes a string to a file, overwriting the file if it already exists.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static Task WriteAllTextAsync(string path, string contents)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(contents);
            return WriteAllBytesAsync(path, encodedText);
        }

        /// <summary>
        /// Asynchronously writes a byte array to a file, overwriting the file if it already exists.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <param name="data">The byte array to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static async Task WriteAllBytesAsync(string path, byte[] data)
        {
            // Use FileOptions.Asynchronous to ensure the UI thread is not blocked during write operations.
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, AppConstants.BufferSizeSmall, FileOptions.Asynchronous))
            {
                await fs.WriteAsync(data, 0, data.Length);
            }
        }

        #endregion

        #region String & Path Helpers

        /// <summary>
        /// Sanitizes a string field for CSV output by enclosing it in quotes if it contains special characters.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <returns>The sanitized string, suitable for a CSV field.</returns>
        public static string SanitizeCsvField(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            // Check for commas, double quotes, or newlines which require the field to be quoted according to CSV standards.
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            {
                // Escape existing double quotes by replacing them with two double quotes ("").
                string escaped = s.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }

            return s;
        }

        /// <summary>
        /// Sanitizes a string to be used as a valid file name by replacing illegal characters.
        /// </summary>
        /// <param name="name">The proposed file name.</param>
        /// <returns>A sanitized string that is safe to use as a file name.</returns>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "_";
            }

            // Replace common but invalid characters with their full-width equivalents for readability.
            string sanitized = name
                .Replace(':', '：').Replace('*', '＊').Replace('?', '？')
                .Replace('"', '＂').Replace('<', '〈').Replace('>', '〉')
                .Replace('|', '｜').Replace('/', '／').Replace('\\', '＼');

            // Remove any remaining invalid characters as defined by the OS.
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(sanitized.Length);
            foreach (char c in sanitized)
            {
                sb.Append(Array.IndexOf(invalidChars, c) != -1 ? '_' : c);
            }
            sanitized = sb.ToString();

            // Check against reserved OS file names (e.g., CON, PRN, LPT1).
            // If matched, prepend an underscore to make it valid.
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            if (FileSystemDefs.ReservedFileNames.Contains(nameWithoutExtension))
            {
                sanitized = "_" + sanitized;
            }
            return sanitized;
        }

        /// <summary>
        /// Contains definitions related to filesystem constraints.
        /// </summary>
        public static class FileSystemDefs
        {
            /// <summary>
            /// A set of reserved file names on Windows that cannot be used.
            /// </summary>
            public static readonly HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };
        }

        #endregion
    }
}