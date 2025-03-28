namespace PersonaEngine.Lib.Utils;

public class WavUtils
{
    private const int RIFF_CHUNK_ID = 0x46464952; // "RIFF" in ASCII

    private const int WAVE_FORMAT = 0x45564157; // "WAVE" in ASCII

    private const int FMT_CHUNK_ID = 0x20746D66; // "fmt " in ASCII

    private const int DATA_CHUNK_ID = 0x61746164; // "data" in ASCII

    private const int PCM_FORMAT = 1; // PCM audio format

    public static void SaveToWav(Memory<float> samples, string filePath, int sampleRate = 44100, int channels = 1)
    {
        using (var writer = new BinaryWriter(File.OpenWrite(filePath)))
        {
            WriteWavFile(writer, samples, sampleRate, channels);
        }
    }

    public static void AppendToWav(Memory<float> samples, string filePath)
    {
        // First, read the existing WAV file header
        WavHeader header;
        long      dataPosition;
        using (var reader = new BinaryReader(File.OpenRead(filePath)))
        {
            header       = ReadWavHeader(reader);
            dataPosition = reader.BaseStream.Position;
        }

        // Open file for writing and seek to end of data
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
        using (var writer = new BinaryWriter(stream))
        {
            stream.Seek(0, SeekOrigin.End);

            // Write new samples
            var span = samples.Span;
            for ( var i = 0; i < span.Length; i++ )
            {
                var pcm = (short)(span[i] * short.MaxValue);
                writer.Write(pcm);
            }

            // Update file size in header
            var newDataSize = header.DataSize + samples.Length * sizeof(short);
            var newFileSize = newDataSize + 44 - 8; // 44 is header size, subtract 8 for RIFF header

            stream.Seek(4, SeekOrigin.Begin);
            writer.Write(newFileSize);

            // Update data chunk size
            stream.Seek(dataPosition - 4, SeekOrigin.Begin);
            writer.Write(newDataSize);
        }
    }

    private static void WriteWavFile(BinaryWriter writer, Memory<float> samples, int sampleRate, int channels)
    {
        var bytesPerSample = sizeof(short); // We'll convert float to 16-bit PCM
        var dataSize       = samples.Length * bytesPerSample;
        var headerSize     = 44;                        // Standard WAV header size
        var fileSize       = headerSize + dataSize - 8; // Total file size - 8 bytes

        // Write WAV header
        writer.Write(RIFF_CHUNK_ID); // "RIFF" chunk
        writer.Write(fileSize);      // File size - 8
        writer.Write(WAVE_FORMAT);   // "WAVE" format

        // Write format chunk
        writer.Write(FMT_CHUNK_ID);                           // "fmt " chunk
        writer.Write(16);                                     // Format chunk size (16 for PCM)
        writer.Write((short)PCM_FORMAT);                      // Audio format (1 for PCM)
        writer.Write((short)channels);                        // Number of channels
        writer.Write(sampleRate);                             // Sample rate
        writer.Write(sampleRate * channels * bytesPerSample); // Byte rate
        writer.Write((short)(channels * bytesPerSample));     // Block align
        writer.Write((short)(bytesPerSample * 8));            // Bits per sample

        // Write data chunk
        writer.Write(DATA_CHUNK_ID); // "data" chunk
        writer.Write(dataSize);      // Data size

        // Write audio samples
        var span = samples.Span;
        for ( var i = 0; i < span.Length; i++ )
        {
            var pcm = (short)(span[i] * short.MaxValue);
            writer.Write(pcm);
        }
    }

    private static WavHeader ReadWavHeader(BinaryReader reader)
    {
        // Verify RIFF header
        if ( reader.ReadInt32() != RIFF_CHUNK_ID )
        {
            throw new InvalidDataException("Not a valid RIFF file");
        }

        var header = new WavHeader { FileSize = reader.ReadInt32() };

        // Verify WAVE format
        if ( reader.ReadInt32() != WAVE_FORMAT )
        {
            throw new InvalidDataException("Not a valid WAVE file");
        }

        // Read format chunk
        if ( reader.ReadInt32() != FMT_CHUNK_ID )
        {
            throw new InvalidDataException("Missing format chunk");
        }

        var fmtSize = reader.ReadInt32();
        if ( reader.ReadInt16() != PCM_FORMAT )
        {
            throw new InvalidDataException("Unsupported audio format (must be PCM)");
        }

        header.Channels      = reader.ReadInt16();
        header.SampleRate    = reader.ReadInt32();
        header.ByteRate      = reader.ReadInt32();
        header.BlockAlign    = reader.ReadInt16();
        header.BitsPerSample = reader.ReadInt16();

        // Skip any extra format bytes
        if ( fmtSize > 16 )
        {
            reader.BaseStream.Seek(fmtSize - 16, SeekOrigin.Current);
        }

        // Find data chunk
        while ( true )
        {
            var chunkId   = reader.ReadInt32();
            var chunkSize = reader.ReadInt32();

            if ( chunkId == DATA_CHUNK_ID )
            {
                header.DataSize = chunkSize;

                break;
            }

            reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);

            if ( reader.BaseStream.Position >= reader.BaseStream.Length )
            {
                throw new InvalidDataException("Missing data chunk");
            }
        }

        return header;
    }

    public static bool ValidateWavFile(string filePath)
    {
        try
        {
            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                ReadWavHeader(reader);

                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private struct WavHeader
    {
        public int FileSize;

        public int Channels;

        public int SampleRate;

        public int ByteRate;

        public short BlockAlign;

        public short BitsPerSample;

        public int DataSize;
    }
}