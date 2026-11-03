namespace My_Life_Admin.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public UserSettings? Settings { get; set; }
}