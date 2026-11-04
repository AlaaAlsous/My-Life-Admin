namespace My_Life_Admin.DTOs.Chat;

public class SendChatMessageDto
{
    public string Message { get; set; } = string.Empty;

    public int? DocumentId { get; set; }
}
