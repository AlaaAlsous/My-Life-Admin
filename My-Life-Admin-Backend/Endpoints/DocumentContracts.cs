namespace My_Life_Admin.Endpoints;

public record DocumentResponse(
    int Id,
    string Title,
    string FileName,
    string? Description,
    string? Category,
    DateTime? ExpiryDate,
    DateTime UploadedAt);

public class CreateDocumentForm
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public IFormFile? File { get; set; }
}

public record UpdateDocumentRequest(string Title, string? Description, string? Category, DateTime? ExpiryDate);

public record DocumentAnalysisResponse(
    string? Title,
    string? Category,
    DateTime? ExpiryDate,
    string? Description);
