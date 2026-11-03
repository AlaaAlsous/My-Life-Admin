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

    public async Task UpdateAsync(UserSettings settings)
    {
        _userSettingsRepository.Update(settings);
        await _userSettingsRepository.SaveChangesAsync();
    }
}