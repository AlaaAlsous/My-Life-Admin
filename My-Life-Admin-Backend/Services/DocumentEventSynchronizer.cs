using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public interface IDocumentEventSynchronizer
{
    // Stages event changes only; the caller saves them together with the document.
    Task SyncAsync(Document document);

    Task RemoveAsync(Document document);
}

public class DocumentEventSynchronizer(IEventRepository eventRepository) : IDocumentEventSynchronizer
{
    public async Task SyncAsync(Document document)
    {
        // A document that is not saved yet cannot have any events.
        var linked = document.Id == 0
            ? []
            : await eventRepository.GetByDocumentIdAsync(document.Id, document.UserId);

        // The first linked event is the one generated from the expiry date.
        var existing = linked.FirstOrDefault();

        if (document.ExpiryDate is not { } expiry)
        {
            if (existing is not null)
            {
                eventRepository.Delete(existing);
            }

            return;
        }

        if (existing is null)
        {
            await eventRepository.AddAsync(new Event
            {
                UserId = document.UserId,
                Document = document,
                Title = TitleFor(document),
                Category = document.Category ?? string.Empty,
                EventDate = EventDates.FromExpiry(expiry)
            });

            return;
        }

        existing.Title = TitleFor(document);
        existing.Category = document.Category ?? string.Empty;
        existing.EventDate = EventDates.FromExpiry(expiry);
        eventRepository.Update(existing);
    }

    public async Task RemoveAsync(Document document)
    {
        var linked = await eventRepository.GetByDocumentIdAsync(document.Id, document.UserId);

        foreach (var eventItem in linked)
        {
            eventRepository.Delete(eventItem);
        }
    }

    private static string TitleFor(Document document)
    {
        var name = string.IsNullOrWhiteSpace(document.Title) ? document.FileName : document.Title;
        return $"{name} expires";
    }
}
