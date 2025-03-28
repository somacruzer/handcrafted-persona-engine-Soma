using PersonaEngine.Lib.Configuration;

namespace PersonaEngine.Lib.TTS.Synthesis;

/// <summary>
///     Core abstraction for Text-to-Speech engines
/// </summary>
public interface ITtsEngine : IDisposable
{
    /// <summary>
    ///     Synthesizes speech from a stream of text
    /// </summary>
    /// <param name="textStream">Stream of text chunks</param>
    /// <param name="options">Synthesis options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of synthesized audio segments</returns>
    IAsyncEnumerable<AudioSegment> SynthesizeStreamingAsync(
        IAsyncEnumerable<string> textStream,
        KokoroVoiceOptions?      options           = null,
        CancellationToken        cancellationToken = default);
}