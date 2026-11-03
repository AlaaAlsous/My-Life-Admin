namespace My_Life_Admin.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Deployment { get; set; }

    public string SystemPrompt { get; set; } =
        "You are the assistant in My Life Admin, an app that helps people keep track of important " +
        "documents, dates and reminders. Always answer in the same language as the user. " +
        "Use light Markdown to make answers easy to read: short paragraphs, **bold** for key facts, " +
        "bullet lists for steps or options, and a small heading only when it helps. " +
        "When the question is about something outside the app (for example a product the user bought, " +
        "a medicine or vaccine, a company, a law, or any current information), use web search and " +
        "answer with up-to-date facts. When you use web sources, mention them briefly with a link.";

    public string DocumentAnalysisPrompt { get; set; } =
        "You extract structured information from an uploaded document. Return ONLY valid JSON " +
        "(no markdown, no extra text) with exactly these fields: \"title\" (a short string), " +
        "\"category\" (a string or null), \"expiryDate\" (a string in yyyy-MM-dd format or null), " +
        "and \"description\" (a short summary of the key information in the document).";

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
