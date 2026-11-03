using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IChatMessageRepository : IRepository<ChatMessage>
{
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count, int documentId);
}
