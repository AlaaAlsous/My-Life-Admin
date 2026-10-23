using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class ChatService(IChatMessageRepository repository) : IChatService
{
    public const int DefaultHistoryCount = 50;
    public const int MaxHistoryCount = 200;
    public const int MaxContentLength = 4000;

    public async Task<ChatMessage> AddMessageAsync(int userId, ChatRole role, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        var trimmed = content.Trim();
        if (trimmed.Length > MaxContentLength)
        {
            throw new ArgumentException(
                $"Message content cannot exceed {MaxContentLength} characters.", nameof(content));
        }

        var message = new ChatMessage
        {
            UserId = userId,
            Role = role,
            Content = trimmed,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(message);
        await repository.SaveChangesAsync();
        return message;
    }

    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count = DefaultHistoryCount)
    {
        return repository.GetHistoryAsync(userId, Math.Min(count, MaxHistoryCount));
    }
}
