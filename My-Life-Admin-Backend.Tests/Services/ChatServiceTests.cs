using My_Life_Admin.Ai;
using My_Life_Admin.Models;
using My_Life_Admin.Options;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ChatServiceTests
{
    private readonly FakeChatMessageRepository _repository = new();
    private readonly FakeDocumentRepository _documentRepository = new();
    private readonly FakeAiChatClient _aiClient = new();
    private readonly AiOptions _aiOptions = new() { MaxHistoryMessages = 20 };
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _service = new ChatService(
            _repository,
            _aiClient,
            Microsoft.Extensions.Options.Options.Create(_aiOptions),
            _documentRepository);
    }

    [Fact]
    public async Task AddMessageAsync_ValidMessage_SavesAndReturnsIt()
    {
        var before = DateTime.UtcNow;

        var message = await _service.AddMessageAsync(1, ChatRole.User, "Hello");

        Assert.Equal(1, message.UserId);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("Hello", message.Content);
        Assert.True(message.CreatedAt >= before);
        Assert.Same(message, Assert.Single(_repository.Messages));
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task AddMessageAsync_TrimsContent()
    {
        var message = await _service.AddMessageAsync(1, ChatRole.Assistant, "  Hi there  ");

        Assert.Equal("Hi there", message.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddMessageAsync_EmptyContent_ThrowsWithoutSaving(string content)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddMessageAsync(1, ChatRole.User, content));

        Assert.Empty(_repository.Messages);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task AddMessageAsync_ContentOverMaxLength_ThrowsWithoutSaving()
    {
        var content = new string('a', ChatService.MaxContentLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddMessageAsync(1, ChatRole.User, content));

        Assert.Empty(_repository.Messages);
    }

    [Fact]
    public async Task AddMessageAsync_ContentAtMaxLength_Succeeds()
    {
        var content = new string('a', ChatService.MaxContentLength);

        var message = await _service.AddMessageAsync(1, ChatRole.User, content);

        Assert.Equal(content, message.Content);
    }

    [Fact]
    public async Task GetHistoryAsync_CountAboveMax_IsCapped()
    {
        await _service.GetHistoryAsync(1, 10_000, 7);

        Assert.Equal((1, ChatService.MaxHistoryCount, 7), _repository.LastHistoryRequest);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsDocumentMessages()
    {
        _repository.Messages.AddRange(
        [
            new ChatMessage { UserId = 1, Role = ChatRole.User, Content = "one", DocumentId = 7 },
            new ChatMessage { UserId = 1, Role = ChatRole.Assistant, Content = "two", DocumentId = 7 },
            new ChatMessage { UserId = 2, Role = ChatRole.User, Content = "other user", DocumentId = 7 },
            new ChatMessage { UserId = 1, Role = ChatRole.User, Content = "other document", DocumentId = 8 },
        ]);

        var history = await _service.GetHistoryAsync(1, 10, 7);

        Assert.Equal(["one", "two"], history.Select(m => m.Content));
    }

    [Fact]
    public async Task SendAsync_WithDocumentId_SavesUserMessageAndReply()
    {
        _aiClient.Reply = "Your passport expires in March.";

        var exchange = await _service.SendAsync(1, "When does my passport expire?", documentId: 7);

        Assert.Equal(ChatRole.User, exchange.UserMessage.Role);
        Assert.Equal("When does my passport expire?", exchange.UserMessage.Content);
        Assert.Equal(7, exchange.UserMessage.DocumentId);
        Assert.Equal(ChatRole.Assistant, exchange.Reply.Role);
        Assert.Equal("Your passport expires in March.", exchange.Reply.Content);
        Assert.Equal(7, exchange.Reply.DocumentId);
        Assert.Equal([exchange.UserMessage, exchange.Reply], _repository.Messages);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task SendAsync_WithoutDocumentId_DoesNotSave()
    {
        var exchange = await _service.SendAsync(1, "General question");

        Assert.Equal("General question", exchange.UserMessage.Content);
        Assert.Empty(_repository.Messages);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task SendAsync_SendsHistoryFollowedByNewMessageToAi()
    {
        _repository.Messages.AddRange(
        [
            new ChatMessage { UserId = 1, Role = ChatRole.User, Content = "Hi", DocumentId = 7 },
            new ChatMessage { UserId = 1, Role = ChatRole.Assistant, Content = "Hello!", DocumentId = 7 },
            new ChatMessage { UserId = 2, Role = ChatRole.User, Content = "other user", DocumentId = 7 },
            new ChatMessage { UserId = 1, Role = ChatRole.User, Content = "other document", DocumentId = 8 },
        ]);

        await _service.SendAsync(1, "  What can you do?  ", documentId: 7);

        Assert.NotNull(_aiClient.LastConversation);
        Assert.Equal(
            [(ChatRole.User, "Hi"), (ChatRole.Assistant, "Hello!"), (ChatRole.User, "What can you do?")],
            _aiClient.LastConversation.Select(m => (m.Role, m.Content)));
    }

    [Fact]
    public async Task SendAsync_LimitsHistoryToMaxHistoryMessages()
    {
        _aiOptions.MaxHistoryMessages = 5;

        await _service.SendAsync(1, "Hello", documentId: 7);

        Assert.Equal((1, 5, 7), _repository.LastHistoryRequest);
    }

    [Fact]
    public async Task SendAsync_AiFails_SavesNothingAndRethrows()
    {
        _aiClient.Error = new AiServiceException("down");

        await Assert.ThrowsAsync<AiServiceException>(() => _service.SendAsync(1, "Hello"));

        Assert.Empty(_repository.Messages);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_EmptyContent_ThrowsWithoutCallingAi(string content)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SendAsync(1, content));

        Assert.Null(_aiClient.LastConversation);
        Assert.Empty(_repository.Messages);
    }

    [Fact]
    public async Task SendAsync_WithDocumentId_PrependsSystemContext()
    {
        _documentRepository.Documents.Add(new Document
        {
            Id = 7,
            UserId = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            Category = "Identity",
            Description = "Travel document",
            ExpiryDate = new DateTime(2030, 1, 1)
        });

        await _service.SendAsync(1, "When does this expire?", documentId: 7);

        Assert.NotNull(_aiClient.LastConversation);
        var first = _aiClient.LastConversation[0];
        Assert.Equal(ChatRole.System, first.Role);
        Assert.Contains("Passport", first.Content);
        Assert.Contains("2030-01-01", first.Content);
        Assert.Equal("When does this expire?", _aiClient.LastConversation[^1].Content);
    }

    [Fact]
    public async Task SendAsync_WithDocumentId_IncludesExtractedTextInContext()
    {
        _documentRepository.Documents.Add(new Document
        {
            Id = 7,
            UserId = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            ExtractedText = "Name: Jane Doe\nPassport No: 12345"
        });

        await _service.SendAsync(1, "What is the passport number?", documentId: 7);

        Assert.NotNull(_aiClient.LastConversation);
        var first = _aiClient.LastConversation[0];
        Assert.Equal(ChatRole.System, first.Role);
        Assert.Contains("Jane Doe", first.Content);
        Assert.Contains("12345", first.Content);
    }

    [Fact]
    public async Task SendAsync_WithUnknownDocumentId_DoesNotAddSystemContext()
    {
        await _service.SendAsync(1, "Hello", documentId: 999);

        Assert.NotNull(_aiClient.LastConversation);
        Assert.DoesNotContain(_aiClient.LastConversation, m => m.Role == ChatRole.System);
    }

    [Fact]
    public async Task SendAsync_LongReply_IsTruncatedToMaxContentLength()
    {
        _aiClient.Reply = new string('a', ChatService.MaxContentLength + 100);

        var exchange = await _service.SendAsync(1, "Tell me everything");

        Assert.Equal(ChatService.MaxContentLength, exchange.Reply.Content.Length);
    }

    private sealed class FakeAiChatClient : IAiChatClient
    {
        public string Reply { get; set; } = "Reply";
        public Exception? Error { get; set; }
        public IReadOnlyList<ChatMessage>? LastConversation { get; private set; }

        public Task<string> GetReplyAsync(
            IReadOnlyList<ChatMessage> conversation, CancellationToken cancellationToken = default)
        {
            LastConversation = conversation;
            return Error is null ? Task.FromResult(Reply) : Task.FromException<string>(Error);
        }
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];

        public Task<Document?> GetByIdForUserAsync(int id, int userId) =>
            Task.FromResult(Documents.FirstOrDefault(d => d.Id == id && d.UserId == userId));

        public Task<IEnumerable<Document>> GetAllForUserAsync(int userId) =>
            Task.FromResult(Documents.Where(d => d.UserId == userId).AsEnumerable());

        public Task<IEnumerable<Document>> SearchAsync(int userId, string query) =>
            Task.FromResult(Documents.Where(d => d.UserId == userId).AsEnumerable());

        public Task<IEnumerable<Document>> GetExpiringWithinAsync(int userId, int days) =>
            Task.FromResult(Documents.Where(d => d.UserId == userId).AsEnumerable());

        public Task<Document?> GetByIdAsync(int id) =>
            Task.FromResult(Documents.FirstOrDefault(d => d.Id == id));

        public Task<IEnumerable<Document>> GetAllAsync() =>
            Task.FromResult(Documents.AsEnumerable());

        public Task AddAsync(Document entity)
        {
            Documents.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Document entity) { }

        public void Delete(Document entity) => Documents.Remove(entity);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        public List<ChatMessage> Messages { get; } = [];
        public int SaveCount { get; private set; }
        public (int UserId, int Count, int DocumentId)? LastHistoryRequest { get; private set; }

        public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count, int documentId)
        {
            LastHistoryRequest = (userId, count, documentId);
            IReadOnlyList<ChatMessage> result = Messages
                .Where(m => m.UserId == userId && m.DocumentId == documentId)
                .TakeLast(count)
                .ToList();
            return Task.FromResult(result);
        }

        public Task AddAsync(ChatMessage entity)
        {
            Messages.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<ChatMessage?> GetByIdAsync(int id) =>
            Task.FromResult(Messages.FirstOrDefault(m => m.Id == id));

        public Task<IEnumerable<ChatMessage>> GetAllAsync() =>
            Task.FromResult<IEnumerable<ChatMessage>>(Messages);

        public void Update(ChatMessage entity) { }

        public void Delete(ChatMessage entity) => Messages.Remove(entity);
    }
}
