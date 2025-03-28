using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace PersonaEngine.Lib.TTS.Synthesis;

/// <summary>
///     High-performance sentence segmentation using both rule-based and ML approaches with
///     advanced handling of edge cases and text structures
/// </summary>
public class SentenceSegmenter : ISentenceSegmenter
{
    // Minimum number of sentences we want to generate
    private const int MinimumSentences = 2;

    private readonly ILogger<SentenceSegmenter> _logger;

    private readonly IMlSentenceDetector _mlDetector;

    public SentenceSegmenter(IMlSentenceDetector mlDetector, ILogger<SentenceSegmenter> logger)
    {
        _mlDetector = mlDetector ?? throw new ArgumentNullException(nameof(mlDetector));
        _logger     = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Segments text into sentences with optimized handling and pre-processing for boundary detection
    /// </summary>
    public IReadOnlyList<string> Segment(string text)
    {
        if ( string.IsNullOrEmpty(text) )
        {
            return Array.Empty<string>();
        }

        try
        {
            // First attempt with original text
            var sentences = _mlDetector.Detect(text);

            // If we have fewer than minimum required sentences, try pre-processing
            if ( sentences.Count < MinimumSentences )
            {
                _logger.LogTrace("Initial segmentation yielded only {count} sentence(s), attempting pre-processing", sentences.Count);

                // Apply pre-processing to create potential sentence boundaries
                var preprocessedText = AddPotentialSentenceBoundaries(text);

                // Re-detect sentences with the pre-processed text
                sentences = _mlDetector.Detect(preprocessedText);

                _logger.LogTrace("After pre-processing, segmentation yielded {count} sentence(s)", sentences.Count);
            }

            return sentences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sentence segmentation.");

            return Array.Empty<string>();
        }
    }

    /// <summary>
    ///     Adds potential sentence boundaries by replacing certain punctuation with periods
    ///     to help generate at least 2 sentences when possible
    /// </summary>
    private string AddPotentialSentenceBoundaries(string text)
    {
        if ( string.IsNullOrEmpty(text) )
        {
            return text;
        }

        try
        {
            // Replace semicolons with periods
            text = text.Replace(";", ".");

            // Replace colons with periods
            text = text.Replace(":", ".");

            // Replace dashes with periods (when surrounded by spaces)
            text = text.Replace(" - ", ". ");
            text = text.Replace(" – ", ". ");
            text = text.Replace(" — ", ". ");

            // Replace commas followed by conjunctions that often indicate new clauses
            // text = Regex.Replace(text, @",\s+(and|but|or|nor|yet|so)\s+", ". ");

            // Ensure proper spacing after periods for the ML detector
            text = Regex.Replace(text, @"\.([A-Za-z0-9])", ". $1");

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during preprocessing for sentence boundaries. Using original text.");

            return text;
        }
    }
}