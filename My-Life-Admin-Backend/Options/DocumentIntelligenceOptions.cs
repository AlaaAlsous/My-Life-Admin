namespace My_Life_Admin.Options;

public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string ModelId { get; set; } = "prebuilt-read";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
