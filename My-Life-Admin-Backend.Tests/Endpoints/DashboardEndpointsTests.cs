using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class DashboardEndpointsTests
{
    private const int UserId = 3;

    private readonly Mock<IDashboardService> _serviceMock = new();

    private readonly ClaimsPrincipal _user = new(
        new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test"));

    private static DashboardSummary TestSummary() => new(
        [new DashboardDocument(1, "Passport", "ID", new DateTime(2026, 10, 1), new DateTime(2030, 1, 1))],
        [new DashboardEvent(2, "Dentist", "Health", new DateTime(2026, 11, 5, 9, 0, 0))],
        [new DashboardReminder(3, new DateTime(2026, 10, 28), "Dentist")]);

    [Fact]
    public async Task GetDashboard_ReturnsSummaryForUser()
    {
        var summary = TestSummary();
        _serviceMock
            .Setup(s => s.GetSummaryAsync(UserId, It.IsAny<DateTime>()))
            .ReturnsAsync(summary);

        var result = await DashboardEndpoints.GetDashboardAsync(_user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<DashboardSummary>>(result);
        Assert.Same(summary, ok.Value);
        _serviceMock.Verify(
            s => s.GetSummaryAsync(UserId, It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboard_SummaryContainsAllSections()
    {
        _serviceMock
            .Setup(s => s.GetSummaryAsync(UserId, It.IsAny<DateTime>()))
            .ReturnsAsync(TestSummary());

        var result = await DashboardEndpoints.GetDashboardAsync(_user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<DashboardSummary>>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value, JsonSerializerOptions.Web));
        var root = json.RootElement;

        Assert.Equal("Passport", Assert.Single(root.GetProperty("recentDocuments").EnumerateArray()).GetProperty("title").GetString());
        Assert.Equal("Dentist", Assert.Single(root.GetProperty("upcomingEvents").EnumerateArray()).GetProperty("title").GetString());
        Assert.Equal(3, Assert.Single(root.GetProperty("latestReminders").EnumerateArray()).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetDashboard_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await DashboardEndpoints.GetDashboardAsync(anonymous, _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboard_NonNumericUserIdClaim_ReturnsUnauthorized()
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "abc")], "Test"));

        var result = await DashboardEndpoints.GetDashboardAsync(user, _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }
}
