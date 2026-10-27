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
    }
}