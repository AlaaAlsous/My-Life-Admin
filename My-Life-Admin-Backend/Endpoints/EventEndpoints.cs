using My_Life_Admin.DTOs.Events;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

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
        IEventService eventService)
    {
        IEnumerable<Event> events;

        if (from.HasValue && to.HasValue)
        {
            events = await eventService.GetBetweenAsync(
                from.Value,
                to.Value);
        }
        else
        {
            events = await eventService.GetAllAsync();
        }

        var result = events.Select(ToDto);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IEventService eventService)
    {
        var eventItem = await eventService.GetByIdAsync(id);

        if (eventItem is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToDto(eventItem));
    }

    private static async Task<IResult> CreateAsync(
        CreateEventDto dto,
        IEventService eventService)
    {
        var newEvent = new Event
        {
            UserId = dto.UserId,
            DocumentId = dto.DocumentId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            EventDate = dto.EventDate
        };

        var createdEvent = await eventService.CreateAsync(newEvent);

        return Results.Created(
            $"/api/events/{createdEvent.Id}",
            ToDto(createdEvent));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateEventDto dto,
        IEventService eventService)
    {
        var changes = new Event
        {
            DocumentId = dto.DocumentId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            EventDate = dto.EventDate
        };

    var updatedEvent = await eventService.UpdateAsync(id, changes);

        if (updatedEvent is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToDto(updatedEvent));
    }

    private static async Task<IResult> DeleteAsync(int id, IEventService eventService)
    {
        var deleted = await eventService.DeleteAsync(id);

        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
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