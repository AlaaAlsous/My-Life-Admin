using Microsoft.AspNetCore.Identity;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class RegisterServiceTests
{
    private const string Name = "Anna";
    private const string Email = "anna@example.com";
    private const string Password = "CorrectHorse1!";

    private readonly PasswordHasher<User> _hasher = new();
    private readonly FakeUserRepository _repository = new();
    private readonly RegisterService _service;

    public RegisterServiceTests()
    {
        _service = new RegisterService(_repository, _hasher);
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_CreatesUser()
    {
        var result = await _service.RegisterAsync(Name, Email, Password);

        Assert.True(result.Succeeded);
        var stored = Assert.Single(_repository.Users);
        Assert.Same(stored, result.User);
        Assert.Equal(Name, stored.Name);
        Assert.Equal(Email, stored.Email);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashedPasswordNotPlainText()
    {
        await _service.RegisterAsync(Name, Email, Password);

        var stored = Assert.Single(_repository.Users);
        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(stored, stored.PasswordHash, Password));
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(stored, stored.PasswordHash, "wrong-password"));
    }

    [Fact]
    public async Task RegisterAsync_TrimsNameAndNormalizesEmail()
    {
        await _service.RegisterAsync("  Anna  ", "  Anna@Example.COM ", Password);

        var stored = Assert.Single(_repository.Users);
        Assert.Equal("Anna", stored.Name);
        Assert.Equal("anna@example.com", stored.Email);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_FailsWithConflict()
    {
        _repository.Users.Add(new User { Id = 1, Name = "Existing", Email = Email });

        var result = await _service.RegisterAsync(Name, Email, Password);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterError.EmailAlreadyExists, result.Error);
        Assert.Equal(RegisterService.EmailTakenError, result.Message);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegisteredInDifferentCase_Fails()
    {
        _repository.Users.Add(new User { Id = 1, Name = "Existing", Email = Email });

        var result = await _service.RegisterAsync(Name, "ANNA@EXAMPLE.COM", Password);

        Assert.Equal(RegisterError.EmailAlreadyExists, result.Error);
    }

    [Theory]
    [InlineData("", Email, Password, RegisterService.NameRequiredError)]
    [InlineData("   ", Email, Password, RegisterService.NameRequiredError)]
    [InlineData(Name, "", Password, RegisterService.InvalidEmailError)]
    [InlineData(Name, "not-an-email", Password, RegisterService.InvalidEmailError)]
    [InlineData(Name, "Anna <anna@example.com>", Password, RegisterService.InvalidEmailError)]
    [InlineData(Name, Email, "short", RegisterService.PasswordTooShortError)]
    [InlineData(Name, Email, "", RegisterService.PasswordTooShortError)]
    public async Task RegisterAsync_InvalidInput_FailsWithoutTouchingRepository(
        string name, string email, string password, string expectedMessage)
    {
        var result = await _service.RegisterAsync(name, email, password);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterError.InvalidInput, result.Error);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Equal(0, _repository.LookupCount);
        Assert.Empty(_repository.Users);
    }

    [Fact]
    public async Task RegisterAsync_NameTooLong_Fails()
    {
        var result = await _service.RegisterAsync(new string('a', RegisterService.MaxNameLength + 1), Email, Password);

        Assert.Equal(RegisterService.NameTooLongError, result.Message);
    }

    [Fact]
    public async Task RegisterAsync_EmailTooLong_Fails()
    {
        var email = new string('a', RegisterService.MaxEmailLength) + "@example.com";

        var result = await _service.RegisterAsync(Name, email, Password);

        Assert.Equal(RegisterService.InvalidEmailError, result.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThenLogin_WorksWithSamePassword()
    {
        await _service.RegisterAsync(Name, "Anna@Example.com", Password);
        var loginService = new LoginService(_repository, _hasher);

        var result = await loginService.LoginAsync("anna@example.com", Password);

        Assert.True(result.Succeeded);
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

        public Task<bool> ExistsByEmailAsync(string email)
        {
            LookupCount++;
            return Task.FromResult(Users.Any(u => u.Email == email));
        }

        public Task AddUserAsync(User user)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }
    }
}
