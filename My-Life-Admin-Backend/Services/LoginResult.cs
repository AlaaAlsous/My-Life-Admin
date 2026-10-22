using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public record LoginResult(bool Succeeded, User? User, string? Error)
{
    public static LoginResult Success(User user) => new(true, user, null);
    public static LoginResult Failure(string error) => new(false, null, error);
}
