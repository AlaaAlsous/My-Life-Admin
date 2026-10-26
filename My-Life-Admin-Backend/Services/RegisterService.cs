using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class RegisterService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher) : IRegisterService
{
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 256;
    public const int MinPasswordLength = 8;

    public const string NameRequiredError = "Name is required.";
    public const string NameTooLongError = "Name is too long.";
    public const string InvalidEmailError = "A valid email address is required.";
    public const string PasswordTooShortError = "Password must be at least 8 characters.";
    public const string EmailTakenError = "Email is already registered.";

    public async Task<RegisterResult> RegisterAsync(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid(NameRequiredError);
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Invalid(NameTooLongError);
        }

        if (!IsValidEmail(email))
        {
            return Invalid(InvalidEmailError);
        }

        if (password is null || password.Length < MinPasswordLength)
        {
            return Invalid(PasswordTooShortError);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(normalizedEmail))
        {
            return RegisterResult.Failure(RegisterError.EmailAlreadyExists, EmailTakenError);
        }

        var user = new User { Name = name.Trim(), Email = normalizedEmail };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        await userRepository.AddUserAsync(user);

        return RegisterResult.Success(user);
    }

    private static RegisterResult Invalid(string message) =>
        RegisterResult.Failure(RegisterError.InvalidInput, message);

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();

        return trimmed.Length <= MaxEmailLength
            && MailAddress.TryCreate(trimmed, out var address)
            && address.Address == trimmed;
    }
}
