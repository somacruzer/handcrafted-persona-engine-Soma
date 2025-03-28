using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PersonaEngine.Lib.LLM;

public record ChatHistoryItem(Guid Id, AuthorRole Role, string Content)
{
    public string Content { get; set; } = Content;
}

public class BasicChatHistoryManager : IChatHistoryManager
{
    private readonly Dictionary<Guid, ChatHistoryItem> _history = new();

    private readonly Lock _lock = new();

    private readonly ChatEngineOptions _options;

    public BasicChatHistoryManager(IOptions<ChatEngineOptions> options)
    {
        _options = options.Value;

        if ( !string.IsNullOrWhiteSpace(options.Value.SystemPrompt) )
        {
            AddSystemMessage(options.Value.SystemPrompt);
        }
    }

    public ChatHistory ChatHistory
    {
        get
        {
            lock (_lock)
            {
                var chatHistory = new ChatHistory();
                foreach ( var historyItem in _history.Values )
                {
                    chatHistory.AddMessage(historyItem.Role, historyItem.Content);
                }

                return chatHistory;
            }
        }
    }

    public IReadOnlyList<ChatHistoryItem> ChatHistoryItems
    {
        get
        {
            lock (_lock)
            {
                return _history.Values.ToList().AsReadOnly();
            }
        }
    }

    public Guid AddUserMessage(string message)
    {
        var item = new ChatHistoryItem(Guid.NewGuid(), AuthorRole.User, message);
        lock (_lock)
        {
            _history.Add(item.Id, item);
        }

        ChatHistoryChanged();

        return item.Id;
    }

    public Guid AddAssistantMessage(string message)
    {
        var item = new ChatHistoryItem(Guid.NewGuid(), AuthorRole.Assistant, message);
        lock (_lock)
        {
            _history.Add(item.Id, item);
        }

        ChatHistoryChanged();

        return item.Id;
    }

    public void RemoveMessage(Guid id)
    {
        lock (_lock)
        {
            _history.Remove(id);
        }

        ChatHistoryChanged();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
        }

        if ( !string.IsNullOrWhiteSpace(_options.SystemPrompt) )
        {
            AddSystemMessage(_options.SystemPrompt);
        }

        ChatHistoryChanged();
    }

    public event EventHandler<EventArgs>? OnChatHistoryChanged;

    public bool UpdateMessage(Guid id, string message)
    {
        var updated = false;
        lock (_lock)
        {
            if ( _history.TryGetValue(id, out var chatMessage) )
            {
                chatMessage.Content = message;
                updated             = true;
            }
        }

        ChatHistoryChanged();

        return updated;
    }

    private void AddSystemMessage(string message)
    {
        var item = new ChatHistoryItem(Guid.NewGuid(), AuthorRole.System, message);
        lock (_lock)
        {
            _history.Add(item.Id, item);
        }
    }

    private void ChatHistoryChanged() { OnChatHistoryChanged?.Invoke(this, EventArgs.Empty); }
}