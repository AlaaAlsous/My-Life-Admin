using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LoginAsync);
        return app;
    }

    public static async Task<IResult> LoginAsync(LoginRequest request, ILoginService loginService)
    {
        var result = await loginService.LoginAsync(request.Email, request.Password);

        if (result.Succeeded && result.User is { } user)
        {
            return TypedResults.Ok(new LoginResponse(user.Id, user.Name, user.Email));
        }

        var error = new ErrorResponse(result.Error ?? LoginService.InvalidCredentialsError);

        return result.Error == LoginService.MissingCredentialsError
            ? TypedResults.BadRequest(error)
            : TypedResults.Json(error, statusCode: StatusCodes.Status401Unauthorized);
    }
}
