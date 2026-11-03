namespace My_Life_Admin.Models;

public class Event
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public User User { get; set; } = null!;
    public Document? Document { get; set; }
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}