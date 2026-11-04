using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.DTOs.Settings;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class UserSettingsEndpointsTests
{
    private const int UserId = 4;

    private readonly Mock<IUserSettingsService> _serviceMock = new();

    private readonly ClaimsPrincipal _user = new(
        new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test"));

    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    private static UserSettings TestSettings() => new()
    {
        Id = 2,
        UserId = UserId,
        DarkMode = true,
        EmailNotifications = false
    };

    [Fact]
    public async Task Get_ReturnsUsersSettingsAsDto()
    {
        _serviceMock
            .Setup(s => s.GetOrDefaultAsync(UserId))
            .ReturnsAsync(TestSettings());

        var result = await UserSettingsEndpoints.GetAsync(_user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<UserSettingsDto>>(result);
        Assert.Equal(2, ok.Value!.Id);
        Assert.Equal(UserId, ok.Value.UserId);
        Assert.True(ok.Value.DarkMode);
        Assert.False(ok.Value.EmailNotifications);
    }

    [Fact]
    public async Task Get_WithoutSavedSettings_ReturnsDefaults()
    {
        _serviceMock
            .Setup(s => s.GetOrDefaultAsync(UserId))
            .ReturnsAsync(new UserSettings { UserId = UserId });

        var result = await UserSettingsEndpoints.GetAsync(_user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<UserSettingsDto>>(result);
        Assert.Equal(UserId, ok.Value!.UserId);
        Assert.False(ok.Value.DarkMode);
        Assert.True(ok.Value.EmailNotifications);
    }

    [Fact]
    public async Task Get_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var result = await UserSettingsEndpoints.GetAsync(_anonymous, _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_SavesChangesForUserAndReturnsDto()
    {
        _serviceMock
            .Setup(s => s.SaveAsync(UserId, It.IsAny<UserSettings>()))
            .ReturnsAsync(TestSettings());

        var dto = new UpdateUserSettingsDto { DarkMode = true, EmailNotifications = false };

        var result = await UserSettingsEndpoints.UpdateAsync(dto, _user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<UserSettingsDto>>(result);
        Assert.True(ok.Value!.DarkMode);
        Assert.False(ok.Value.EmailNotifications);
        _serviceMock.Verify(
            s => s.SaveAsync(UserId, It.Is<UserSettings>(c => c.DarkMode && !c.EmailNotifications)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var dto = new UpdateUserSettingsDto { DarkMode = true };

        var result = await UserSettingsEndpoints.UpdateAsync(dto, _anonymous, _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }
}
