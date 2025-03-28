namespace PersonaEngine.Lib.LLM;

/// <summary>
///     Factory interface for creating chat history managers.
/// </summary>
public interface IChatHistoryManagerFactory
{
    /// <summary>
    ///     Creates a new chat history manager.
    /// </summary>
    /// <returns>A new chat history manager with system prompt already initialized.</returns>
    IChatHistoryManager Create();
}