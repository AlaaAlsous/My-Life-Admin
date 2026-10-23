namespace My_Life_Admin.Endpoints;

public record LoginRequest(string Email, string Password);

public record LoginResponse(int Id, string Name, string Email);

public record ErrorResponse(string Error);
