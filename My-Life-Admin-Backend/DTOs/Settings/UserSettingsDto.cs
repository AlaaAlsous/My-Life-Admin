namespace My_Life_Admin.DTOs.Settings;

public class UserSettingsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool DarkMode { get; set; }
    public bool EmailNotifications { get; set; }
}
