namespace My_Life_Admin.DTOs.Settings;

public class UpdateUserSettingsDto
{
    public bool DarkMode { get; set; }
    public bool EmailNotifications { get; set; } = true;
}
