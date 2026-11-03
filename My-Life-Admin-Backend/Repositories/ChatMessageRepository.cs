using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public class ChatMessageRepository : Repository<ChatMessage>, IChatMessageRepository
{
    private readonly ApplicationDbContext _db;

    public ChatMessageRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count, int documentId)
    {
        if (count <= 0)
        {
            return [];
        }

        var latest = await _db.ChatMessages
            .Where(m => m.UserId == userId && m.DocumentId == documentId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(count)
            .ToListAsync();

        latest.Reverse();
        return latest;
    }
}
