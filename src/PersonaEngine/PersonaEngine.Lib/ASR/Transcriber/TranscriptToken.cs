namespace PersonaEngine.Lib.ASR.Transcriber;

public class TranscriptToken
{
    /// <summary>
    ///     The logaritmic confidence of the token.
    /// </summary>
    public float? ConfidenceLog;

    /// <summary>
    ///     The text representation of the token.
    /// </summary>
    public string? Text;

    /// <summary>
    ///     The unique identifier for the token for transcriptors that uses IDs
    /// </summary>
    /// <remarks>
    ///     The type is object to allow for different types of IDs based on the transcriptor, this ID is not used inside
    ///     EchoSharp library.
    /// </remarks>
    public object? Id { get; set; }

    /// <summary>
    ///     The confidence of the token.
    /// </summary>
    public float? Confidence { get; set; }

    /// <summary>
    ///     The confidence of the timestamp of the token.
    /// </summary>
    public float TimestampConfidence { get; set; }

    /// <summary>
    ///     The timestamp confidence sum of the token.
    /// </summary>
    public float TimestampConfidenceSum { get; set; }

    /// <summary>
    ///     The start time of the token.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    ///     The duration of the token.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    ///     Dynamic Time Warping timestamp of the token.
    /// </summary>
    public long DtwTimestamp { get; set; }
}