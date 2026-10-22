namespace My_Life_Admin.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = new List<Document>();

    public ICollection<Event> Events { get; set; } = new List<Event>();

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}