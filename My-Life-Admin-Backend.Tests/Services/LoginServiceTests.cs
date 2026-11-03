using Microsoft.AspNetCore.Identity;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class LoginServiceTests
{
    private const string Email = "anna@example.com";
    private const string Password = "CorrectHorse1!";

    private readonly PasswordHasher<User> _hasher = new();
    private readonly FakeUserRepository _repository = new();
    private readonly LoginService _service;

    public LoginServiceTests()
    {
        var user = new User { Id = 1, Name = "Anna", Email = Email };
        user.PasswordHash = _hasher.HashPassword(user, Password);
        _repository.Users.Add(user);

        _service = new LoginService(_repository, _hasher);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_Succeeds()
    {
        var result = await _service.LoginAsync(Email, Password);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.User?.Id);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task LoginAsync_EmailWithSurroundingWhitespace_Succeeds()
    {
        var result = await _service.LoginAsync($"  {Email}  ", Password);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_EmailInDifferentCase_Succeeds()
    {
        var result = await _service.LoginAsync(Email.ToUpperInvariant(), Password);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Fails()
    {
        var result = await _service.LoginAsync(Email, "wrong-password");

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal(LoginService.InvalidCredentialsError, result.Error);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_FailsWithSameErrorAsWrongPassword()
    {
        var result = await _service.LoginAsync("nobody@example.com", Password);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginService.InvalidCredentialsError, result.Error);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("   ", Password)]
    [InlineData(Email, "")]
    public async Task LoginAsync_MissingEmailOrPassword_FailsWithoutQueryingRepository(string email, string password)
    {
        var result = await _service.LoginAsync(email, password);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginService.MissingCredentialsError, result.Error);
        Assert.Equal(0, _repository.LookupCount);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public int LookupCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email)
        {
            LookupCount++;
            return Task.FromResult(Users.FirstOrDefault(u => u.Email == email));
        }

        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(Users.Any(u => u.Email == email));

        public Task AddUserAsync(User user)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }
    }
}
