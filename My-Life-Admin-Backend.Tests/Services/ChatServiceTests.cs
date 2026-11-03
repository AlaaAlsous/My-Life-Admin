using My_Life_Admin.Ai;
using My_Life_Admin.Models;
using My_Life_Admin.Options;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ChatServiceTests
{
    private readonly FakeChatMessageRepository _repository = new();
    private readonly FakeAiChatClient _aiClient = new();
    private readonly AiOptions _aiOptions = new() { MaxHistoryMessages = 20 };
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _service = new ChatService(
            _repository,
            _aiClient,
            Microsoft.Extensions.Options.Options.Create(_aiOptions));
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
    public async Task GetHistoryAsync_DefaultCount_UsesDefaultHistoryCount()
    {
        await _service.GetHistoryAsync(1);

        Assert.Equal((1, ChatService.DefaultHistoryCount), _repository.LastHistoryRequest);
    }

    [Fact]
    public async Task GetHistoryAsync_CountAboveMax_IsCapped()
    {
        await _service.GetHistoryAsync(1, 10_000);

        Assert.Equal((1, ChatService.MaxHistoryCount), _repository.LastHistoryRequest);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsRepositoryResult()
    {
        await _service.AddMessageAsync(1, ChatRole.User, "one");
        await _service.AddMessageAsync(1, ChatRole.Assistant, "two");
        await _service.AddMessageAsync(2, ChatRole.User, "other user");

        var history = await _service.GetHistoryAsync(1, 10);

        Assert.Equal(["one", "two"], history.Select(m => m.Content));
    }

    [Fact]
    public async Task SendAsync_SavesUserMessageAndReplyInOneSave()
    {
        _aiClient.Reply = "Your passport expires in March.";

        var exchange = await _service.SendAsync(1, "When does my passport expire?");

        Assert.Equal(ChatRole.User, exchange.UserMessage.Role);
        Assert.Equal("When does my passport expire?", exchange.UserMessage.Content);
        Assert.Equal(ChatRole.Assistant, exchange.Reply.Role);
        Assert.Equal("Your passport expires in March.", exchange.Reply.Content);
        Assert.Equal([exchange.UserMessage, exchange.Reply], _repository.Messages);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task SendAsync_SendsHistoryFollowedByNewMessageToAi()
    {
        await _service.AddMessageAsync(1, ChatRole.User, "Hi");
        await _service.AddMessageAsync(1, ChatRole.Assistant, "Hello!");
        await _service.AddMessageAsync(2, ChatRole.User, "other user");

        await _service.SendAsync(1, "  What can you do?  ");

        Assert.NotNull(_aiClient.LastConversation);
        Assert.Equal(
            [(ChatRole.User, "Hi"), (ChatRole.Assistant, "Hello!"), (ChatRole.User, "What can you do?")],
            _aiClient.LastConversation.Select(m => (m.Role, m.Content)));
    }

    [Fact]
    public async Task SendAsync_LimitsHistoryToMaxHistoryMessages()
    {
        _aiOptions.MaxHistoryMessages = 5;

        await _service.SendAsync(1, "Hello");

        Assert.Equal((1, 5), _repository.LastHistoryRequest);
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

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        public List<ChatMessage> Messages { get; } = [];
        public int SaveCount { get; private set; }
        public (int UserId, int Count)? LastHistoryRequest { get; private set; }

        public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(int userId, int count)
        {
            LastHistoryRequest = (userId, count);
            IReadOnlyList<ChatMessage> result = Messages
                .Where(m => m.UserId == userId)
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
