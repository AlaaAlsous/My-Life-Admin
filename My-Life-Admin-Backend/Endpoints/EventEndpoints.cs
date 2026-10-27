using My_Life_Admin.DTOs.Events;
using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        app.MapGet("/api/events", async (IEventService eventService) =>
        {
            var events = await eventService.GetAllAsync();

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