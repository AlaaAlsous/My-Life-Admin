using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.DTOs.Events;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class EventEndpointsTests
{
    private const int UserId = 1;

    private readonly Mock<IEventService> _serviceMock = new();

    private readonly ClaimsPrincipal _user = new(
        new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test"));

    private static Event TestEvent() => new()
    {
        Id = 1,
        UserId = UserId,
        Title = "Dentist",
        Description = "Annual checkup",
        Category = "Health",
        EventDate = new DateTime(2026, 11, 10, 14, 0, 0),
        CreatedAt = new DateTime(2026, 10, 27)
    };

    [Fact]
    public async Task GetAll_ReturnsUsersEventsAsDtos()
    {
        _serviceMock
            .Setup(s => s.GetAllAsync(UserId))
            .ReturnsAsync(new List<Event> { TestEvent() });

        var result = await EventEndpoints.GetAllAsync(
            null,
            null,
            _user,
            _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<EventDto>>>(result);

        var dto = Assert.Single(ok.Value!);

        Assert.Equal(1, dto.Id);
        Assert.Equal(UserId, dto.UserId);
        Assert.Equal("Dentist", dto.Title);
        Assert.Equal("Health", dto.Category);
    }

    [Fact]
    public async Task GetAll_WithRange_QueriesServiceWithUtcBounds()
    {
        var from = new DateTime(2026, 10, 25, 23, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 30, 22, 59, 59, DateTimeKind.Utc);

        _serviceMock
            .Setup(s => s.GetBetweenAsync(UserId, from, to))
            .ReturnsAsync(new List<Event> { TestEvent() });

        var result = await EventEndpoints.GetAllAsync(from, to, _user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<EventDto>>>(result);
        Assert.Single(ok.Value!);
        _serviceMock.Verify(s => s.GetAllAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WithLocalKindRange_ConvertsBoundsToUtc()
    {
        var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Local);
        var to = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Local);

        _serviceMock
            .Setup(s => s.GetBetweenAsync(UserId, from.ToUniversalTime(), to.ToUniversalTime()))
            .ReturnsAsync(new List<Event>());

        var result = await EventEndpoints.GetAllAsync(from, to, _user, _serviceMock.Object);

        Assert.IsType<Ok<List<EventDto>>>(result);
        _serviceMock.Verify(
            s => s.GetBetweenAsync(UserId, from.ToUniversalTime(), to.ToUniversalTime()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WithToBeforeFrom_ReturnsBadRequest()
    {
        var result = await EventEndpoints.GetAllAsync(
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            _user,
            _serviceMock.Object);

        Assert.IsType<BadRequest<string>>(result);
        _serviceMock.Verify(
            s => s.GetBetweenAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_ReturnsEventDatesAsUtc()
    {
        _serviceMock
            .Setup(s => s.GetAllAsync(UserId))
            .ReturnsAsync(new List<Event> { TestEvent() });

        var result = await EventEndpoints.GetAllAsync(null, null, _user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<EventDto>>>(result);
        Assert.Equal(DateTimeKind.Utc, ok.Value![0].EventDate.Kind);
    }
}