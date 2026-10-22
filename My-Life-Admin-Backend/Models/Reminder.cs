namespace My_Life_Admin.Models;

public class Reminder
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime ReminderTime { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}