using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public enum RegisterError
{
    InvalidInput,
    EmailAlreadyExists,
}

public record RegisterResult(bool Succeeded, User? User, RegisterError? Error, string? Message)
{
    public static RegisterResult Success(User user) => new(true, user, null, null);
    public static RegisterResult Failure(RegisterError error, string message) => new(false, null, error, message);
}
