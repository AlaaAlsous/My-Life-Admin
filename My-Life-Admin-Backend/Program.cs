var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseCors();

app.MapGet("/api/health", () => new { status = "Healthy" });

app.Run();
