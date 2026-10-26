using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace MyLife_Admin.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        _repository = new UserRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AddUserAsync_PersistsUserWithoutExtraSave()
    {
        await _repository.AddUserAsync(NewUser("anna@example.com"));

        await using var other = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        var stored = await other.Users.SingleAsync();
        Assert.Equal("anna@example.com", stored.Email);
        Assert.True(stored.Id > 0);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ReturnsTrueOnlyForRegisteredEmail()
    {
        await _repository.AddUserAsync(NewUser("anna@example.com"));

        Assert.True(await _repository.ExistsByEmailAsync("anna@example.com"));
        Assert.False(await _repository.ExistsByEmailAsync("other@example.com"));
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsAddedUser()
    {
        await _repository.AddUserAsync(NewUser("anna@example.com"));

        var user = await _repository.GetByEmailAsync("anna@example.com");

        Assert.NotNull(user);
        Assert.Equal("Anna", user.Name);
    }

    [Fact]
    public async Task AddUserAsync_DuplicateEmail_ThrowsBecauseOfUniqueIndex()
    {
        await _repository.AddUserAsync(NewUser("anna@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _repository.AddUserAsync(NewUser("anna@example.com")));
    }

    private static User NewUser(string email) =>
        new() { Name = "Anna", Email = email, PasswordHash = "hash" };
}
