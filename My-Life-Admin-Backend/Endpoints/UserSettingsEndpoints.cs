using My_Life_Admin.DTOs.Settings;
using My_Life_Admin.Models;
using My_Life_Admin.Services;
using System.Security.Claims;

namespace My_Life_Admin.Endpoints;

public static class UserSettingsEndpoints
{
    public static IEndpointRouteBuilder MapUserSettingsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings");

        group.MapGet("/", GetAsync);
        group.MapPut("/", UpdateAsync);

        return app;
    }

    public static async Task<IResult> GetAsync(
        ClaimsPrincipal user,
        IUserSettingsService settingsService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var settings = await settingsService.GetOrDefaultAsync(userId);

        return TypedResults.Ok(ToDto(settings));
    }

    public static async Task<IResult> UpdateAsync(
        UpdateUserSettingsDto dto,
        ClaimsPrincipal user,
        IUserSettingsService settingsService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var changes = new UserSettings
        {
            DarkMode = dto.DarkMode,
            EmailNotifications = dto.EmailNotifications
        };

        var settings = await settingsService.SaveAsync(userId, changes);

        return TypedResults.Ok(ToDto(settings));
    }

    public static UserSettingsDto ToDto(UserSettings settings)
    {
        return new UserSettingsDto
        {
            Id = settings.Id,
            UserId = settings.UserId,
            DarkMode = settings.DarkMode,
            EmailNotifications = settings.EmailNotifications
        };
    }
}
