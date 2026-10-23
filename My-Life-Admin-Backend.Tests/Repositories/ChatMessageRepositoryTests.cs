using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace MyLife_Admin.Tests.Repositories;

public sealed class ChatMessageRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ApplicationDbContext _db;
    private readonly ChatMessageRepository _repository;

    public ChatMessageRepositoryTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        _db.Users.AddRange(
            new User { Id = 1, Name = "Anna", Email = "anna@example.com" },
            new User { Id = 2, Name = "Bo", Email = "bo@example.com" });
        _db.SaveChanges();

        _repository = new ChatMessageRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task SeedAsync(int userId, int count, ChatRole role = ChatRole.User)
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 1; i <= count; i++)
        {
            await _repository.AddAsync(new ChatMessage
            {
                UserId = userId,
                Role = role,
                Content = $"u{userId}-m{i}",
                CreatedAt = start.AddMinutes(i)
            });
        }
        await _repository.SaveChangesAsync();
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsLatestMessagesOldestFirst()
    {
        await SeedAsync(1, 5);

        var history = await _repository.GetHistoryAsync(1, 3);

        Assert.Equal(["u1-m3", "u1-m4", "u1-m5"], history.Select(m => m.Content));
    }

    [Fact]
    public async Task GetHistoryAsync_OnlyReturnsMessagesForRequestedUser()
    {
        await SeedAsync(1, 2);
        await SeedAsync(2, 3);

        var history = await _repository.GetHistoryAsync(2, 10);

        Assert.Equal(3, history.Count);
        Assert.All(history, m => Assert.Equal(2, m.UserId));
    }

    [Fact]
    public async Task GetHistoryAsync_CountLargerThanAvailable_ReturnsAll()
    {
        await SeedAsync(1, 2);

        var history = await _repository.GetHistoryAsync(1, 50);

        Assert.Equal(2, history.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetHistoryAsync_NonPositiveCount_ReturnsEmpty(int count)
    {
        await SeedAsync(1, 2);

        var history = await _repository.GetHistoryAsync(1, count);

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistoryAsync_UserWithoutMessages_ReturnsEmpty()
    {
        var history = await _repository.GetHistoryAsync(1, 10);

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistoryAsync_SameTimestamp_OrdersById()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        foreach (var content in new[] { "first", "second", "third" })
        {
            await _repository.AddAsync(new ChatMessage
            {
                UserId = 1,
                Content = content,
                CreatedAt = timestamp
            });
        }
        await _repository.SaveChangesAsync();

        var history = await _repository.GetHistoryAsync(1, 2);

        Assert.Equal(["second", "third"], history.Select(m => m.Content));
    }

    [Fact]
    public async Task AddAsync_PersistsRoleAndContent()
    {
        await SeedAsync(1, 1, ChatRole.Assistant);

        var saved = Assert.Single(await _repository.GetAllAsync());

        Assert.Equal(ChatRole.Assistant, saved.Role);
        Assert.Equal("u1-m1", saved.Content);
    }
}
