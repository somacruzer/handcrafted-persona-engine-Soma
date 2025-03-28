using Microsoft.SemanticKernel.ChatCompletion;

namespace PersonaEngine.Lib.LLM;

public interface IChatHistoryManager
{
    ChatHistory ChatHistory { get; }

    IReadOnlyList<ChatHistoryItem> ChatHistoryItems { get; }

    Guid AddUserMessage(string message);

    Guid AddAssistantMessage(string message);

    void RemoveMessage(Guid id);

    public bool UpdateMessage(Guid id, string message);

    void Clear();

    event EventHandler<EventArgs> OnChatHistoryChanged;
}