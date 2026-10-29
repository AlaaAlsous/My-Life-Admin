namespace My_Life_Admin.Ai;

public interface IDocumentAnalyzer
{
    Task<DocumentAnalysisResult> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
