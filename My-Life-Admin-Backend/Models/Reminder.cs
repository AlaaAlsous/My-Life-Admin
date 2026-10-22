namespace My_Life_Admin.Models;

public class Reminder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? EventId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime ReminderDate { get; set; }
    public DateTime? SentAt { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public User User { get; set; } = null!;
    public Event? Event { get; set; }
    public Document? Document { get; set; }
}