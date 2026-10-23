using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IChatService
{
    Task<ChatMessage> AddMessageAsync(int userId, ChatRole role, string content);
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count = ChatService.DefaultHistoryCount);
}
