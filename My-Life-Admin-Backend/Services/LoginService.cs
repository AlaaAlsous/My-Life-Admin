using Microsoft.AspNetCore.Identity;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class LoginService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher) : ILoginService
{
    public const string MissingCredentialsError = "Email and password are required.";
    public const string InvalidCredentialsError = "Invalid email or password.";

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return LoginResult.Failure(MissingCredentialsError);
        }

        var user = await userRepository.GetByEmailAsync(email.Trim());
        if (user is null)
        {
            return LoginResult.Failure(InvalidCredentialsError);
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed
            ? LoginResult.Failure(InvalidCredentialsError)
            : LoginResult.Success(user);
    }
}
