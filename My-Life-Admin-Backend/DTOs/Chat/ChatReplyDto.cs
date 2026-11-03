namespace My_Life_Admin.DTOs.Chat;

public class ChatReplyDto
{
    public ChatMessageDto UserMessage { get; set; } = new();
    public ChatMessageDto Reply { get; set; } = new();
}
