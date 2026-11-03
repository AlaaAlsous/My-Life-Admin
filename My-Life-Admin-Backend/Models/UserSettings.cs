namespace My_Life_Admin.Models;

public class UserSettings
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public bool DarkMode { get; set; }

    public bool EmailNotifications { get; set; } = true;

    public User User { get; set; } = null!;
}
