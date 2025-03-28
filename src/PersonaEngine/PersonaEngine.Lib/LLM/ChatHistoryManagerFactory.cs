using Microsoft.Extensions.Options;

namespace PersonaEngine.Lib.LLM;

/// <summary>
///     Factory for creating chat history managers with consistent initialization.
/// </summary>
public class ChatHistoryManagerFactory : IChatHistoryManagerFactory
{
    private readonly IOptions<ChatEngineOptions> _options;

    public ChatHistoryManagerFactory(IOptions<ChatEngineOptions> options) { _options = options ?? throw new ArgumentNullException(nameof(options)); }

    /// <summary>
    ///     Creates a new chat history manager with system prompt already configured.
    /// </summary>
    public IChatHistoryManager Create() { return new BasicChatHistoryManager(_options); }
}