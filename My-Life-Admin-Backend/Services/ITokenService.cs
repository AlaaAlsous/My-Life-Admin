using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface ITokenService
{
    string CreateToken(User user);
}
