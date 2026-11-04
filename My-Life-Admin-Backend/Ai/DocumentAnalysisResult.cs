namespace My_Life_Admin.Ai;

public record DocumentAnalysisResult(
    string? Title,
    string? Category,
    DateTime? ExpiryDate,
    string? Description,
    string? ExtractedText);
