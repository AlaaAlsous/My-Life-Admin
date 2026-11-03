using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IChatService
{
    Task<ChatMessage> AddMessageAsync(int userId, ChatRole role, string content);
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count = ChatService.DefaultHistoryCount);
    Task<ChatExchange> SendAsync(
        int userId,
        string content,
        int? documentId = null,
        CancellationToken cancellationToken = default);
}
