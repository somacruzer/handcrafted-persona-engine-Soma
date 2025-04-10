using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.LLM;

/// <summary>
///     Interface for text filters that process text before TTS synthesis
/// </summary>
public interface ITextFilter
{
    /// <summary>
    ///     Priority of the filter (higher values run first)
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Processes text and extracts/transforms it before TTS processing
    /// </summary>
    /// <param name="text">The input text to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed text and any extracted metadata</returns>
    Task<TextFilterResult> ProcessAsync(string text, CancellationToken cancellationToken = default);

    Task PostProcessAsync(TextFilterResult textFilterResult, AudioSegment segment, CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of text filter processing
/// </summary>
public record TextFilterResult
{
    /// <summary>
    ///     The processed text with filters applied
    /// </summary>
    public string ProcessedText { get; set; } = string.Empty;

    /// <summary>
    ///     Additional metadata extracted during processing
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}