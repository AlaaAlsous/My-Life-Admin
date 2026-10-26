namespace My_Life_Admin.Endpoints;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(int Id, string Name, string Email, string Token);

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record RegisterResponse(int Id, string Name, string Email);

public sealed record ErrorResponse(string Error);
