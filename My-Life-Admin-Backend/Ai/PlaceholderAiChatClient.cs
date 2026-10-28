using My_Life_Admin.Models;

namespace My_Life_Admin.Ai;

public sealed class PlaceholderAiChatClient : IAiChatClient
{
    public const string Reply = "The AI assistant isn't configured yet, so I can't answer right now.";

    public Task<string> GetReplyAsync(IReadOnlyList<ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Reply);
    }
}
