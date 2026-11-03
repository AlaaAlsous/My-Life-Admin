using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class AuthEndpointsTests
{
    private readonly FakeLoginService _loginService = new();
    private readonly FakeTokenService _tokenService = new();

    [Fact]
    public async Task Login_Success_ReturnsOkWithUserInfoAndToken()
    {
        var user = new User { Id = 7, Name = "Anna", Email = "anna@example.com", PasswordHash = "secret-hash" };
        _loginService.Result = LoginResult.Success(user);
        _tokenService.TokenToReturn = "signed-jwt-token";

        var result = await AuthEndpoints.LoginAsync(new LoginRequest("anna@example.com", "pw"), _loginService, _tokenService);

        var ok = Assert.IsType<Ok<LoginResponse>>(result);
        Assert.Equal(new LoginResponse(7, "Anna", "anna@example.com", "signed-jwt-token"), ok.Value);
    }

    [Fact]
    public async Task Login_Success_GeneratesTokenFromUser()
    {
        var user = new User { Id = 7, Name = "Anna", Email = "anna@example.com", PasswordHash = "secret-hash" };
        _loginService.Result = LoginResult.Success(user);

        await AuthEndpoints.LoginAsync(new LoginRequest("anna@example.com", "pw"), _loginService, _tokenService);

        Assert.Same(user, _tokenService.LastUser);
    }

    [Fact]
    public async Task Login_Success_PassesCredentialsToService()
    {
        _loginService.Result = LoginResult.Success(new User { Id = 1 });

        await AuthEndpoints.LoginAsync(new LoginRequest("anna@example.com", "pw"), _loginService, _tokenService);

        Assert.Equal(("anna@example.com", "pw"), _loginService.LastCall);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorizedWithError()
    {
        _loginService.Result = LoginResult.Failure(LoginService.InvalidCredentialsError);

        var result = await AuthEndpoints.LoginAsync(new LoginRequest("anna@example.com", "wrong"), _loginService, _tokenService);

        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, json.StatusCode);
        Assert.Equal(LoginService.InvalidCredentialsError, json.Value?.Error);
    }

    [Fact]
    public async Task Login_MissingCredentials_ReturnsBadRequestWithError()
    {
        _loginService.Result = LoginResult.Failure(LoginService.MissingCredentialsError);

        var result = await AuthEndpoints.LoginAsync(new LoginRequest("", ""), _loginService, _tokenService);

        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal(LoginService.MissingCredentialsError, badRequest.Value?.Error);
    }

    private sealed class FakeLoginService : ILoginService
    {
        public LoginResult Result { get; set; } = LoginResult.Failure(LoginService.InvalidCredentialsError);
        public (string Email, string Password)? LastCall { get; private set; }

        public Task<LoginResult> LoginAsync(string email, string password)
        {
            LastCall = (email, password);
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string TokenToReturn { get; set; } = "default-token";
        public User? LastUser { get; private set; }

        public string CreateToken(User user)
        {
            LastUser = user;
            return TokenToReturn;
        }
    }
}
