using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class RegisterEndpointTests
{
    private readonly FakeRegisterService _registerService = new();

    [Fact]
    public async Task Register_Success_ReturnsCreatedWithUserInfoOnly()
    {
        var user = new User { Id = 7, Name = "Anna", Email = "anna@example.com", PasswordHash = "secret-hash" };
        _registerService.Result = RegisterResult.Success(user);

        var result = await AuthEndpoints.RegisterAsync(
            new RegisterRequest("Anna", "anna@example.com", "pw"), _registerService);

        var json = Assert.IsType<JsonHttpResult<RegisterResponse>>(result);
        Assert.Equal(StatusCodes.Status201Created, json.StatusCode);
        Assert.Equal(new RegisterResponse(7, "Anna", "anna@example.com"), json.Value);
    }

    [Fact]
    public async Task Register_PassesInputToService()
    {
        _registerService.Result = RegisterResult.Success(new User { Id = 1 });

        await AuthEndpoints.RegisterAsync(new RegisterRequest("Anna", "anna@example.com", "pw"), _registerService);

        Assert.Equal(("Anna", "anna@example.com", "pw"), _registerService.LastCall);
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsBadRequestWithMessage()
    {
        _registerService.Result = RegisterResult.Failure(RegisterError.InvalidInput, RegisterService.PasswordTooShortError);

        var result = await AuthEndpoints.RegisterAsync(new RegisterRequest("Anna", "anna@example.com", "x"), _registerService);

        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal(RegisterService.PasswordTooShortError, badRequest.Value?.Error);
    }

    [Fact]
    public async Task Register_EmailAlreadyExists_ReturnsConflictWithMessage()
    {
        _registerService.Result = RegisterResult.Failure(RegisterError.EmailAlreadyExists, RegisterService.EmailTakenError);

        var result = await AuthEndpoints.RegisterAsync(new RegisterRequest("Anna", "anna@example.com", "pw"), _registerService);

        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(StatusCodes.Status409Conflict, json.StatusCode);
        Assert.Equal(RegisterService.EmailTakenError, json.Value?.Error);
    }

    private sealed class FakeRegisterService : IRegisterService
    {
        public RegisterResult Result { get; set; } =
            RegisterResult.Failure(RegisterError.InvalidInput, RegisterService.InvalidEmailError);

        public (string Name, string Email, string Password)? LastCall { get; private set; }

        public Task<RegisterResult> RegisterAsync(string name, string email, string password)
        {
            LastCall = (name, email, password);
            return Task.FromResult(Result);
        }
    }
}
