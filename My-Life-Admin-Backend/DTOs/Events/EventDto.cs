namespace My_Life_Admin.DTOs.Events;

public class EventDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime CreatedAt { get; set; }
}