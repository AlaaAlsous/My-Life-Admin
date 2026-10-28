namespace My_Life_Admin.DTOs.Reminders;

public class CreateReminderDto
{
    public int? EventId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime ReminderDate { get; set; }
}