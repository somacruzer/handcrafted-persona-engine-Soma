namespace PersonaEngine.Lib.Audio.Player;

/// <summary>
///     VBAN protocol constants.
/// </summary>
public static class VBANConstants
{
    public const int HEADER_SIZE = 28;

    public const int MAX_STREAM_NAME_LENGTH = 16;

    public static readonly int[] SAMPLERATES = { 6000, 12000, 24000, 48000, 96000, 192000, 384000, 8000, 16000, 32000, 64000, 128000, 256000, 512000, 11025, 22050, 44100, 88200, 176400, 352800, 705600 };
}