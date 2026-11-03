using My_Life_Admin.Models;

namespace My_Life_Admin.DTOs.Reminders;

public class ReminderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? EventId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime ReminderDate { get; set; }
    public DateTime? SentAt { get; set; }
    public ReminderStatus Status { get; set; }
}