namespace PersonaEngine.Lib.TTS.Synthesis;

/// <summary>
///     Interface for model loading and management
/// </summary>
public interface IModelProvider : IAsyncDisposable
{
    /// <summary>
    ///     Gets a model by type
    /// </summary>
    /// <param name="modelType">Type of model</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model resource</returns>
    Task<ModelResource> GetModelAsync(
        ModelType         modelType,
        CancellationToken cancellationToken = default);
}