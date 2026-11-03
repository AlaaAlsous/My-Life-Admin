using System.Globalization;
using System.Security.Claims;
using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", GetDashboardAsync);
        return app;
    }

    public static async Task<IResult> GetDashboardAsync(ClaimsPrincipal user, IDashboardService dashboardService)
    {
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var summary = await dashboardService.GetSummaryAsync(userId, DateTime.UtcNow);
        return TypedResults.Ok(summary);
    }
}
