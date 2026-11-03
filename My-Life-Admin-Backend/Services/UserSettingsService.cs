using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    public UserSettingsService(IUserSettingsRepository userSettingsRepository)
    {
        _userSettingsRepository = userSettingsRepository;
    }

    public async Task<UserSettings?> GetByUserIdAsync(int userId)
    {
        return await _userSettingsRepository.GetByUserIdAsync(userId);
    }

    public async Task<UserSettings> GetOrDefaultAsync(int userId)
    {
        return await _userSettingsRepository.GetByUserIdAsync(userId)
            ?? new UserSettings { UserId = userId };
    }

    public async Task UpdateAsync(UserSettings settings)
    {
        _userSettingsRepository.Update(settings);
        await _userSettingsRepository.SaveChangesAsync();
    }

    public async Task<UserSettings> SaveAsync(int userId, UserSettings changes)
    {
        var settings = await _userSettingsRepository.GetByUserIdAsync(userId);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            await _userSettingsRepository.AddAsync(settings);
        }

        settings.DarkMode = changes.DarkMode;
        settings.EmailNotifications = changes.EmailNotifications;

        await _userSettingsRepository.SaveChangesAsync();

        return settings;
    }
}