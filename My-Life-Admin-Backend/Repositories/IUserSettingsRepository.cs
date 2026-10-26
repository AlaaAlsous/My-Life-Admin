using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IUserSettingsRepository : IRepository<UserSettings>
{
    Task<UserSettings?> GetByUserIdAsync(int userId);
}