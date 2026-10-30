using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public class DocumentEventBackfill(ApplicationDbContext db, ILogger<DocumentEventBackfill> logger)
{
    // Creates an event for every document that has an expiry date but no linked event.
    // Safe to run repeatedly.
    public async Task<int> RunAsync()
    {
        var documents = await db.Documents
            .Where(d => d.ExpiryDate != null && !d.Events.Any())
            .ToListAsync();

        foreach (var document in documents)
        {
            db.Events.Add(new Event
            {
                UserId = document.UserId,
                DocumentId = document.Id,
                Title = DocumentEventSynchronizer.TitleFor(document),
                Category = document.Category ?? string.Empty,
                EventDate = EventDates.FromExpiry(document.ExpiryDate!.Value)
            });
        }

        if (documents.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Created {Count} events for existing documents.", documents.Count);
        }

        return documents.Count;
    }
}
