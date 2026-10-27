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
}