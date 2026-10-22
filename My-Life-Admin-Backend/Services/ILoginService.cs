namespace My_Life_Admin.Services;

public interface ILoginService
{
    Task<LoginResult> LoginAsync(string email, string password);
}
