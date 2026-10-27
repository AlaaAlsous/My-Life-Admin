using My_Life_Admin.DTOs.Events;
using My_Life_Admin.Services;
using My_Life_Admin.Models;

namespace My_Life_Admin.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        app.MapGet("/api/events", async (
            DateTime? from,
            DateTime? to,
            IEventService eventService) =>
        {
            IEnumerable<Event> events;

            if (from.HasValue && to.HasValue)
            {
                events = await eventService.GetBetweenAsync(from.Value, to.Value);
            }
            else
            {
                events = await eventService.GetAllAsync();
            }

            var result = events.Select(e => new EventDto
            {
                Id = e.Id,
                UserId = e.UserId,
                DocumentId = e.DocumentId,
                Title = e.Title,
                Description = e.Description,
                Category = e.Category,
                EventDate = e.EventDate,
                CreatedAt = e.CreatedAt
            });

            return Results.Ok(result);
        });

        app.MapPost("/api/events", async (
            CreateEventDto dto,
            IEventService eventService) =>
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

            var result = new EventDto
            {
                Id = createdEvent.Id,
                UserId = createdEvent.UserId,
                DocumentId = createdEvent.DocumentId,
                Title = createdEvent.Title,
                Description = createdEvent.Description,
                Category = createdEvent.Category,
                EventDate = createdEvent.EventDate,
                CreatedAt = createdEvent.CreatedAt
            };

            return Results.Created($"/api/events/{createdEvent.Id}", result);
        });

        app.MapPut("/api/events/{id:int}", async (
    int id,
    UpdateEventDto dto,
    IEventService eventService) =>
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

            var result = new EventDto
            {
                Id = updatedEvent.Id,
                UserId = updatedEvent.UserId,
                DocumentId = updatedEvent.DocumentId,
                Title = updatedEvent.Title,
                Description = updatedEvent.Description,
                Category = updatedEvent.Category,
                EventDate = updatedEvent.EventDate,
                CreatedAt = updatedEvent.CreatedAt
            };

            return Results.Ok(result);
        });
    }
}