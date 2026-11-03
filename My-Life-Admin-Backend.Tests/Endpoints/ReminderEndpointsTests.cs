using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.DTOs.Reminders;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class ReminderEndpointsTests
{
    private const int UserId = 5;

    private readonly Mock<IReminderService> _serviceMock = new();

    private readonly ClaimsPrincipal _user = new(
        new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test"));

    private static Reminder TestReminder() => new()
    {
        Id = 1,
        UserId = UserId,
        EventId = 2,
        ReminderDate = new DateTime(2026, 11, 10, 14, 0, 0),
        Status = ReminderStatus.Pending
    };

    [Fact]
    public async Task GetAll_ReturnsUsersRemindersAsDtos()
    {
        _serviceMock
            .Setup(s => s.GetAllAsync(UserId))
            .ReturnsAsync(new List<Reminder> { TestReminder() });

        var result = await ReminderEndpoints.GetAllAsync(
            _user,
            _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<ReminderDto>>>(result);

        var dto = Assert.Single(ok.Value!);

        Assert.Equal(1, dto.Id);
        Assert.Equal(UserId, dto.UserId);
        Assert.Equal(2, dto.EventId);
        Assert.Equal(ReminderStatus.Pending, dto.Status);
    }

    [Fact]
    public async Task GetPending_ReturnsPendingReminders()
    {
        _serviceMock
            .Setup(s => s.GetPendingAsync(UserId))
            .ReturnsAsync(new List<Reminder> { TestReminder() });

        var result = await ReminderEndpoints.GetPendingAsync(
            _user,
            _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<ReminderDto>>>(result);

        var dto = Assert.Single(ok.Value!);

        Assert.Equal(ReminderStatus.Pending, dto.Status);

        _serviceMock.Verify(
            s => s.GetPendingAsync(UserId),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingReminder_ReturnsOk()
    {
        _serviceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(TestReminder());

        var result = await ReminderEndpoints.GetByIdAsync(
            1,
            _user,
            _serviceMock.Object);

        var ok = Assert.IsType<Ok<ReminderDto>>(result);

        Assert.Equal(1, ok.Value!.Id);
        Assert.Equal(UserId, ok.Value.UserId);
    }

    [Fact]
    public async Task GetById_UnknownReminder_ReturnsNotFound()
    {
        _serviceMock
            .Setup(s => s.GetByIdAsync(42, UserId))
            .ReturnsAsync((Reminder?)null);

        var result = await ReminderEndpoints.GetByIdAsync(
            42,
            _user,
            _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task Create_UsesLoggedInUserId()
    {
        Reminder? capturedReminder = null;

        _serviceMock
            .Setup(s => s.AddAsync(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => capturedReminder = r)
            .ReturnsAsync((Reminder r) =>
            {
                r.Id = 1;
                return r;
            });

        var dto = new CreateReminderDto
        {
            EventId = 2,
            ReminderDate = new DateTime(2026, 11, 10, 14, 0, 0)
        };

        var result = await ReminderEndpoints.CreateAsync(
            dto,
            _user,
            _serviceMock.Object);

        var created = Assert.IsType<Created<ReminderDto>>(result);

        Assert.NotNull(capturedReminder);
        Assert.Equal(UserId, capturedReminder.UserId);
        Assert.Equal(2, capturedReminder.EventId);
        Assert.Equal(1, created.Value!.Id);
    }

    [Fact]
    public async Task Update_ExistingReminder_ReturnsOk()
    {
        var updatedReminder = TestReminder();
        updatedReminder.DocumentId = 3;

        _serviceMock
            .Setup(s => s.UpdateAsync(
                1,
                UserId,
                It.IsAny<Reminder>()))
            .ReturnsAsync(updatedReminder);

        var dto = new UpdateReminderDto
        {
            DocumentId = 3,
            ReminderDate = new DateTime(2026, 12, 1)
        };

        var result = await ReminderEndpoints.UpdateAsync(
            1,
            dto,
            _user,
            _serviceMock.Object);

        var ok = Assert.IsType<Ok<ReminderDto>>(result);

        Assert.Equal(3, ok.Value!.DocumentId);

        _serviceMock.Verify(
            s => s.UpdateAsync(
                1,
                UserId,
                It.IsAny<Reminder>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_UnknownReminder_ReturnsNotFound()
    {
        _serviceMock
            .Setup(s => s.UpdateAsync(
                42,
                UserId,
                It.IsAny<Reminder>()))
            .ReturnsAsync((Reminder?)null);

        var dto = new UpdateReminderDto
        {
            ReminderDate = new DateTime(2026, 12, 1)
        };

        var result = await ReminderEndpoints.UpdateAsync(
            42,
            dto,
            _user,
            _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task Delete_ExistingReminder_ReturnsNoContent()
    {
        _serviceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(true);

        var result = await ReminderEndpoints.DeleteAsync(
            1,
            _user,
            _serviceMock.Object);

        Assert.IsType<NoContent>(result);
    }

    [Fact]
    public async Task Delete_UnknownReminder_ReturnsNotFound()
    {
        _serviceMock
            .Setup(s => s.DeleteAsync(42, UserId))
            .ReturnsAsync(false);

        var result = await ReminderEndpoints.DeleteAsync(
            42,
            _user,
            _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetAll_WithoutUserId_ReturnsUnauthorized()
    {
        var unauthenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity());

        var result = await ReminderEndpoints.GetAllAsync(
            unauthenticatedUser,
            _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }
}