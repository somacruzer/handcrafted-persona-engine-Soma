namespace PersonaEngine.Lib.Audio.Player;

public static class VBANConsts
{
    public static readonly int[] SAMPLERATES = { 6000, 12000, 24000, 48000, 96000, 192000, 384000, 8000, 16000, 32000, 64000, 128000, 256000, 512000, 11025, 22050, 44100, 88200, 176400, 352800, 705600 };
}

/// <summary>
///     VBAN bit resolution formats.
/// </summary>
public enum VBANBitResolution : byte
{
    VBAN_BITFMT_8_INT = 0x00,

    VBAN_BITFMT_16_INT = 0x01,

    VBAN_BITFMT_24_INT = 0x02,

    VBAN_BITFMT_32_INT = 0x03,

    VBAN_BITFMT_32_FLOAT = 0x04,

    VBAN_BITFMT_64_FLOAT = 0x05
}

/// <summary>
///     VBAN codec types.
/// </summary>
public enum VBANCodec : byte
{
    VBAN_CODEC_PCM = 0x00,

    VBAN_CODEC_VBCA = 0x10,

    VBAN_CODEC_VBCV = 0x20
}

/// <summary>
///     VBAN protocol enumerations.
/// </summary>
public enum VBANProtocol : byte
{
    VBAN_PROTOCOL_AUDIO = 0x00,

    VBAN_PROTOCOL_SERIAL = 0x20,

    VBAN_PROTOCOL_TXT = 0x40,

    VBAN_PROTOCOL_SERVICE = 0x60
}

public enum VBanQuality
{
    VBAN_QUALITY_OPTIMAL = 512,

    VBAN_QUALITY_FAST = 1024,

    VBAN_QUALITY_MEDIUM = 2048,

    VBAN_QUALITY_SLOW = 4096,

    VBAN_QUALITY_VERYSLOW = 8192
}