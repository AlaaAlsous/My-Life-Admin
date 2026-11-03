namespace My_Life_Admin.DTOs.Reminders;

public class UpdateReminderDto
{
    public int? EventId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime ReminderDate { get; set; }
}