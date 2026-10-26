using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public UserSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserSettings?> GetByIdAsync(int id)
    {
        return await _context.UserSettings.FindAsync(id);
    }

    public async Task<IEnumerable<UserSettings>> GetAllAsync()
    {
        return await _context.UserSettings.ToListAsync();
    }

    public async Task<UserSettings?> GetByUserIdAsync(int userId)
    {
        return await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task AddAsync(UserSettings settings)
    {
        await _context.UserSettings.AddAsync(settings);
    }

    public void Update(UserSettings settings)
    {
        _context.UserSettings.Update(settings);
    }

    public void Delete(UserSettings settings)
    {
        _context.UserSettings.Remove(settings);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}