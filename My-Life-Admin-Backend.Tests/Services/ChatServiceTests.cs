using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ChatServiceTests
{
    private readonly FakeChatMessageRepository _repository = new();
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _service = new ChatService(_repository);
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
