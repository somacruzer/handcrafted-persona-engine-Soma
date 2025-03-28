namespace PersonaEngine.Lib.TTS.Synthesis;

/// <summary>
///     Voice data for synthesis
/// </summary>
public class VoiceData
{
    private const int StyleDim = 256;

    private readonly ReadOnlyMemory<float> _rawEmbedding;

    public VoiceData(string id, ReadOnlyMemory<float> rawEmbedding)
    {
        Id            = id;
        _rawEmbedding = rawEmbedding;
    }

    /// <summary>
    ///     Voice ID
    /// </summary>
    public string Id { get; }

    public ReadOnlyMemory<float> GetEmbedding(ReadOnlySpan<int> inputDimensions)
    {
        var numTokens = GetNumTokens(inputDimensions);
        var offset    = numTokens * StyleDim;

        // Check bounds to prevent access violations
        return offset + StyleDim > _rawEmbedding.Length
                   ?
                   // Return empty vector if out of bounds
                   new float[StyleDim]
                   :
                   // Return the slice of memory at the specified offset
                   _rawEmbedding.Slice(offset, StyleDim);
    }

    private int GetNumTokens(ReadOnlySpan<int> inputDimensions)
    {
        var lastDim = inputDimensions[^1];

        return Math.Min(Math.Max(lastDim - 2, 0), 509);
    }
}