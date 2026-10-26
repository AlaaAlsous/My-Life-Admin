namespace My_Life_Admin.Services;

public interface IRegisterService
{
    Task<RegisterResult> RegisterAsync(string name, string email, string password);
}
