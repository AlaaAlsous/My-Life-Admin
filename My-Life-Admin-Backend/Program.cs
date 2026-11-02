using Azure.Storage.Blobs;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using My_Life_Admin.Ai;
using My_Life_Admin.Data;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Options;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;
using My_Life_Admin.Storage;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:4200";
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("SqlServerConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'SqlServerConnection' is missing. " +
                "Set the ConnectionStrings__SqlServerConnection environment variable " +
                "(or the matching Azure App Service setting).");
        }

        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentEventSynchronizer, DocumentEventSynchronizer>();
builder.Services.AddScoped<DocumentEventBackfill>();
builder.Services.AddHostedService<ReminderBackgroundService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
}
else
{
    builder.Services.AddSingleton(sp =>
    {
        var options = sp
            .GetRequiredService<IOptions<FileStorageOptions>>()
            .Value;

        return new BlobServiceClient(options.ConnectionString);
    });

    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
}

builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

if (aiOptions.IsConfigured)
{
    builder.Services.AddSingleton<IAiChatClient, AzureOpenAiChatClient>();
}
else if (aiOptions.HasAnyConnectionSetting)
{
    throw new InvalidOperationException(
        "Ai:Endpoint, Ai:ApiKey and Ai:Deployment must all be set to enable the AI assistant " +
        "(leave all of them empty to use the placeholder reply).");
}
else
{
    builder.Services.AddSingleton<IAiChatClient, PlaceholderAiChatClient>();
}

builder.Services.Configure<DocumentIntelligenceOptions>(
    builder.Configuration.GetSection(DocumentIntelligenceOptions.SectionName));
var documentIntelligenceOptions =
    builder.Configuration.GetSection(DocumentIntelligenceOptions.SectionName).Get<DocumentIntelligenceOptions>()
    ?? new DocumentIntelligenceOptions();

if (documentIntelligenceOptions.IsConfigured)
{
    builder.Services.AddSingleton<IDocumentAnalyzer, DocumentIntelligenceDocumentAnalyzer>();
}
else
{
    builder.Services.AddSingleton<IDocumentAnalyzer, PlaceholderDocumentAnalyzer>();
}

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
    .Validate(o => o.ExpiresMinutes > 0, "Jwt:ExpiresMinutes must be greater than zero.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required.")
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Creates events for documents uploaded before events were generated from expiry dates.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<DocumentEventBackfill>().RunAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not create events for existing documents.");
    }
}

app.UseCors();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => new { status = "Healthy" }).AllowAnonymous();
app.MapAuthEndpoints();
app.MapEventEndpoints();
app.MapDashboardEndpoints();
app.MapDocumentEndpoints();
app.MapReminderEndpoints();
app.MapChatEndpoints();

app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();
