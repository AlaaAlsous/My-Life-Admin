using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
}
