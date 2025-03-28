using PersonaEngine.Lib.Utils;

namespace PersonaEngine.Lib.Audio.Player;

/// <summary>
///     Implementation of IVBANPacketBuilder.
/// </summary>
public class VBANPacketBuilder : IVBANPacketBuilder
{
    private readonly AtomicCounter _frameCounter;

    private readonly string _streamName;

    public VBANPacketBuilder(string streamName)
    {
        _streamName = streamName.Length > VBANConstants.MAX_STREAM_NAME_LENGTH
                          ? streamName[..VBANConstants.MAX_STREAM_NAME_LENGTH]
                          : streamName;

        _frameCounter = new AtomicCounter();
    }

    public byte[] BuildPacket(ReadOnlyMemory<float> audioData, int sampleRate, int samplesPerChannel, int channels)
    {
        // Create the VBAN packet header (28 bytes) + audio data
        var packet = new byte[VBANConstants.HEADER_SIZE + audioData.Length * sizeof(short)]; // 16-bit PCM format

        // VBAN header magic bytes
        packet[0] = (byte)'V';
        packet[1] = (byte)'B';
        packet[2] = (byte)'A';
        packet[3] = (byte)'N';

        // Sub protocol (audio) and sample rate index
        var sampleRateIndex = Array.IndexOf(VBANConstants.SAMPLERATES, sampleRate);
        if ( sampleRateIndex < 0 )
        {
            sampleRateIndex = 0; // Default if not found
        }

        packet[4] = (byte)(((int)VBANProtocol.VBAN_PROTOCOL_AUDIO << 5) | sampleRateIndex);
        packet[5] = (byte)(samplesPerChannel - 1);                                                                // Samples per frame (0-255 for 1-256 samples)
        packet[6] = (byte)(channels - 1);                                                                         // Channels per frame (0 for 1 channel)
        packet[7] = ((int)VBANCodec.VBAN_CODEC_PCM << 5) | (0 << 4) | (byte)VBANBitResolution.VBAN_BITFMT_16_INT; // Format

        // Stream name (16 bytes)
        for ( var i = 0; i < Math.Min(_streamName.Length, 16); i++ )
        {
            packet[i + 8] = (byte)_streamName[i];
        }

        // Frame counter (4 bytes)
        var counterValue = _frameCounter.Increment();
        var counterBytes = BitConverter.GetBytes(counterValue);
        Array.Copy(counterBytes, 0, packet, 24, 4);

        // Convert samples to 16-bit PCM and add to packet
        var dataOffset = VBANConstants.HEADER_SIZE;
        for ( var i = 0; i < audioData.Length; i++ )
        {
            // Convert float (-1.0 to 1.0) to short (-32768 to 32767)
            var value = audioData.Span[i] * 32767f;
            value = Math.Clamp(value, -32768f, 32767f);
            var sampleValue = (short)value;

            // Add sample bytes to packet (little endian)
            var bytes = BitConverter.GetBytes(sampleValue);
            packet[dataOffset++] = bytes[0];
            packet[dataOffset++] = bytes[1];
        }

        return packet;
    }
}