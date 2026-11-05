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
    IDocumentRepository documentRepository,
    IEventRepository eventRepository,
    IReminderRepository reminderRepository) : IChatService
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

    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count, int documentId)
    {
        return repository.GetHistoryAsync(userId, Math.Min(count, MaxHistoryCount), documentId);
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
            DocumentId = documentId,
            CreatedAt = DateTime.UtcNow
        };

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

            var history = await repository.GetHistoryAsync(
                userId, Math.Max(0, aiOptions.Value.MaxHistoryMessages), docId);
            conversation.AddRange(history);
        }
        else
        {
            var documents = await documentRepository.GetAllForUserAsync(userId);
            var events = await eventRepository.GetByUserIdAsync(userId);
            var reminders = await reminderRepository.GetByUserIdAsync(userId);

            conversation.Add(new ChatMessage
            {
                UserId = userId,
                Role = ChatRole.System,
                Content = BuildAccountContext(documents, events, reminders),
                CreatedAt = DateTime.UtcNow
            });
        }

        conversation.Add(userMessage);

        var replyText = await aiClient.GetReplyAsync(conversation, cancellationToken);

        var reply = new ChatMessage
        {
            UserId = userId,
            Role = ChatRole.Assistant,
            Content = Truncate(replyText.Trim(), MaxContentLength),
            DocumentId = documentId,
            CreatedAt = DateTime.UtcNow
        };

        if (documentId is not null)
        {
            await repository.AddAsync(userMessage);
            await repository.AddAsync(reply);
            await repository.SaveChangesAsync();
        }

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

        if (!string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            parts.Add($"Document content:\n{Truncate(document.ExtractedText.Trim(), 12_000)}");
        }

        parts.Add("Use the details above when relevant, and say so when the document does not contain the answer.");

        return string.Join('\n', parts);
    }

    private static string BuildAccountContext(
        IEnumerable<Document> documents,
        IEnumerable<Event> events,
        IEnumerable<Reminder> reminders)
    {
        var parts = new List<string>
        {
            "You are answering questions about the user's account. Here is a summary of their data."
        };

        var documentLines = documents
            .OrderBy(d => d.ExpiryDate ?? DateTime.MaxValue)
            .Select(d => d.ExpiryDate is { } e ? $"- {d.Title} (expires {e:yyyy-MM-dd})" : $"- {d.Title}")
            .ToList();

        if (documentLines.Count > 0)
        {
            parts.Add($"Documents:\n{string.Join('\n', documentLines)}");
        }

        var eventLines = events
            .OrderBy(e => e.EventDate)
            .Select(e => $"- {e.Title} ({e.EventDate:yyyy-MM-dd})")
            .ToList();

        if (eventLines.Count > 0)
        {
            parts.Add($"Events:\n{string.Join('\n', eventLines)}");
        }

        var reminderLines = reminders
            .OrderBy(r => r.ReminderDate)
            .Select(r => $"- {r.ReminderDate:yyyy-MM-dd} ({r.Status})")
            .ToList();

        if (reminderLines.Count > 0)
        {
            parts.Add($"Reminders:\n{string.Join('\n', reminderLines)}");
        }

        parts.Add(
            "Answer questions about the data above. If the user asks about something not listed, say you do not have that information.");

        return Truncate(string.Join("\n\n", parts), 12_000);
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
