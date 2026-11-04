using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IUserSettingsService
{
    Task<UserSettings?> GetByUserIdAsync(int userId);

    Task<UserSettings> GetOrDefaultAsync(int userId);

    Task UpdateAsync(UserSettings settings);

    Task<UserSettings> SaveAsync(int userId, UserSettings changes);
}
