using My_Life_Admin.DTOs.Events;
using My_Life_Admin.Models;
using My_Life_Admin.Services;
using System.Security.Claims;

namespace My_Life_Admin.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);

        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);

        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> GetAllAsync(
    DateTime? from,
    DateTime? to,
    ClaimsPrincipal user,
    IEventService eventService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        IEnumerable<Event> events;

        if (from.HasValue && to.HasValue)
        {
            events = await eventService.GetBetweenAsync(
                userId,
                from.Value,
                to.Value);
        }
        else
        {
            events = await eventService.GetAllAsync(userId);
        }

        return TypedResults.Ok(events.Select(ToDto).ToList());
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        ClaimsPrincipal user,
        IEventService eventService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var eventItem = await eventService.GetByIdAsync(id, userId);

        return eventItem is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(eventItem));
    }

    private static async Task<IResult> CreateAsync(
        CreateEventDto dto,
        ClaimsPrincipal user,
        IEventService eventService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var newEvent = new Event
        {
            UserId = userId,
            DocumentId = dto.DocumentId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            EventDate = dto.EventDate
        };

        var createdEvent = await eventService.CreateAsync(newEvent);

        return TypedResults.Created(
            $"/api/events/{createdEvent.Id}",
            ToDto(createdEvent));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateEventDto dto,
        ClaimsPrincipal user,
        IEventService eventService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var changes = new Event
        {
            DocumentId = dto.DocumentId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            EventDate = dto.EventDate
        };

        var updatedEvent = await eventService.UpdateAsync(
            id,
            userId,
            changes);

        return updatedEvent is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(updatedEvent));
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        ClaimsPrincipal user,
        IEventService eventService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await eventService.DeleteAsync(id, userId);

        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static EventDto ToDto(Event eventItem)
    {
        return new EventDto
        {
            Id = eventItem.Id,
            UserId = eventItem.UserId,
            DocumentId = eventItem.DocumentId,
            Title = eventItem.Title,
            Description = eventItem.Description,
            Category = eventItem.Category,
            EventDate = eventItem.EventDate,
            CreatedAt = eventItem.CreatedAt
        };
    }
}