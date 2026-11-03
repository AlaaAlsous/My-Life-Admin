using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IChatMessageRepository : IRepository<ChatMessage>
{
    /// Returns the user's latest <paramref name="count"/> messages, oldest first.
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count);
}
