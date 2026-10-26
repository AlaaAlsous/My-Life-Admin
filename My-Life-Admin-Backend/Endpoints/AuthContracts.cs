namespace My_Life_Admin.Endpoints;

public record LoginRequest(string Email, string Password);

public record LoginResponse(int Id, string Name, string Email);

public record RegisterRequest(string Name, string Email, string Password);

public record RegisterResponse(int Id, string Name, string Email);

public record ErrorResponse(string Error);
