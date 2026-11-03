using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LoginAsync).AllowAnonymous();
        app.MapPost("/api/auth/register", RegisterAsync).AllowAnonymous();
        return app;
    }

    public static async Task<IResult> RegisterAsync(RegisterRequest request, IRegisterService registerService)
    {
        var result = await registerService.RegisterAsync(request.Name, request.Email, request.Password);

        if (result.Succeeded && result.User is { } user)
        {
            return TypedResults.Json(
                new RegisterResponse(user.Id, user.Name, user.Email),
                statusCode: StatusCodes.Status201Created);
        }

        var error = new ErrorResponse(result.Message ?? "Registration failed.");

        return result.Error == RegisterError.EmailAlreadyExists
            ? TypedResults.Json(error, statusCode: StatusCodes.Status409Conflict)
            : TypedResults.BadRequest(error);
    }

    public static async Task<IResult> LoginAsync(LoginRequest request, ILoginService loginService, ITokenService tokenService)
    {
        var result = await loginService.LoginAsync(request.Email, request.Password);

        if (result.Succeeded && result.User is { } user)
        {
            var token = tokenService.CreateToken(user);
            return TypedResults.Ok(new LoginResponse(user.Id, user.Name, user.Email, token));
        }

        var error = new ErrorResponse(result.Error ?? LoginService.InvalidCredentialsError);

        return result.Error == LoginService.MissingCredentialsError
            ? TypedResults.BadRequest(error)
            : TypedResults.Json(error, statusCode: StatusCodes.Status401Unauthorized);
    }
}
