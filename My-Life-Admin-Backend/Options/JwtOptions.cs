namespace My_Life_Admin.Options;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int ExpiresMinutes { get; init; }
    public required string SigningKey { get; init; }
}
