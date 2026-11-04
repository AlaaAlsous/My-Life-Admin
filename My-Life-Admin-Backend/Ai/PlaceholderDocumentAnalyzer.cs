namespace My_Life_Admin.Ai;

public class PlaceholderDocumentAnalyzer : IDocumentAnalyzer
{
    public Task<DocumentAnalysisResult> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DocumentAnalysisResult(null, null, null, null, null));
    }
}
