using My_Life_Admin.Models;

namespace My_Life_Admin.Ai;

public interface IAiChatClient
{
    Task<string> GetReplyAsync(IReadOnlyList<ChatMessage> conversation, CancellationToken cancellationToken = default);
}
