using Microsoft.Extensions.Options;
using My_Life_Admin.Ai;
using My_Life_Admin.Models;
using My_Life_Admin.Options;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class ChatService(
    IChatMessageRepository repository,
    IAiChatClient aiClient,
    IOptions<AiOptions> aiOptions,
    IDocumentRepository documentRepository) : IChatService
{
    public const int DefaultHistoryCount = 50;
    public const int MaxHistoryCount = 200;
    public const int MaxContentLength = 4000;

    public async Task<ChatMessage> AddMessageAsync(int userId, ChatRole role, string content)
    {
        var message = new ChatMessage
        {
            UserId = userId,
            Role = role,
            Content = ValidateContent(content),
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

    public async Task<ChatExchange> SendAsync(
        int userId,
        string content,
        int? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var userMessage = new ChatMessage
        {
            UserId = userId,
            Role = ChatRole.User,
            Content = ValidateContent(content),
            CreatedAt = DateTime.UtcNow
        };

        var history = await repository.GetHistoryAsync(userId, Math.Max(0, aiOptions.Value.MaxHistoryMessages));

        var conversation = new List<ChatMessage>();

        if (documentId is { } docId)
        {
            var document = await documentRepository.GetByIdForUserAsync(docId, userId);

            if (document is not null)
            {
                conversation.Add(new ChatMessage
                {
                    UserId = userId,
                    Role = ChatRole.System,
                    Content = BuildDocumentContext(document),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        conversation.AddRange(history);
        conversation.Add(userMessage);

        var replyText = await aiClient.GetReplyAsync(conversation, cancellationToken);

        var reply = new ChatMessage
        {
            UserId = userId,
            Role = ChatRole.Assistant,
            Content = Truncate(replyText.Trim(), MaxContentLength),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(userMessage);
        await repository.AddAsync(reply);
        await repository.SaveChangesAsync();

        return new ChatExchange(userMessage, reply);
    }

    private static string BuildDocumentContext(Document document)
    {
        var parts = new List<string>
        {
            "You are answering questions about a document the user has open."
        };

        if (!string.IsNullOrWhiteSpace(document.Title))
        {
            parts.Add($"Title: {document.Title.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(document.FileName))
        {
            parts.Add($"File name: {document.FileName.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(document.Category))
        {
            parts.Add($"Category: {document.Category.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(document.Description))
        {
            parts.Add($"Description: {document.Description.Trim()}");
        }

        if (document.ExpiryDate is { } expiry)
        {
            parts.Add($"Expiry date: {expiry:yyyy-MM-dd}");
        }

        parts.Add("Use the details above when relevant, and say so when the document does not contain the answer.");

        return string.Join('\n', parts);
    }

    private static string ValidateContent(string content)
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

        return trimmed;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
