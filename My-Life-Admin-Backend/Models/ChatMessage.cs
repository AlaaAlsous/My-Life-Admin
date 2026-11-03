namespace My_Life_Admin.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
