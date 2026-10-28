namespace My_Life_Admin.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Deployment { get; set; }

    public string SystemPrompt { get; set; } =
        "You are the assistant in My Life Admin, an app that helps people keep track of important " +
        "documents, dates and reminders. Answer briefly and clearly, in the same language as the user.";

    public int MaxHistoryMessages { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 800;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Deployment);

    public bool HasAnyConnectionSetting =>
        !string.IsNullOrWhiteSpace(Endpoint) ||
        !string.IsNullOrWhiteSpace(ApiKey) ||
        !string.IsNullOrWhiteSpace(Deployment);
}
