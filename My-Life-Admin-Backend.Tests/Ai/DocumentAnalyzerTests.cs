using Microsoft.Extensions.Options;
using My_Life_Admin.Ai;
using My_Life_Admin.Options;

namespace MyLife_Admin.Tests.Ai;

public class DocumentAnalyzerTests
{
    [Fact]
    public async Task Placeholder_ReturnsEmptyResult()
    {
        var analyzer = new PlaceholderDocumentAnalyzer();

        var result = await analyzer.AnalyzeAsync(new MemoryStream(), "doc.pdf", "application/pdf");

        Assert.NotNull(result);
        Assert.Null(result.Title);
        Assert.Null(result.Category);
        Assert.Null(result.ExpiryDate);
        Assert.Null(result.Description);
    }

    [Fact]
    public void DocumentIntelligenceDocumentAnalyzer_CanBeCreatedFromValidSettings()
    {
        var analyzer = new DocumentIntelligenceDocumentAnalyzer(
            Options.Create(ConfiguredDi()),
            Options.Create(ConfiguredAi()));

        Assert.NotNull(analyzer);
    }

    [Fact]
    public void DocumentIntelligenceDocumentAnalyzer_MissingSettings_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new DocumentIntelligenceDocumentAnalyzer(
                Options.Create(new DocumentIntelligenceOptions()),
                Options.Create(new AiOptions())));
    }

    [Fact]
    public void DocumentIntelligenceDocumentAnalyzer_InvalidEndpoint_Throws()
    {
        var di = ConfiguredDi();
        di.Endpoint = "not-a-url";

        Assert.Throws<InvalidOperationException>(
            () => new DocumentIntelligenceDocumentAnalyzer(
                Options.Create(di),
                Options.Create(ConfiguredAi())));
    }

    private static DocumentIntelligenceOptions ConfiguredDi() => new()
    {
        Endpoint = "https://example.cognitiveservices.azure.com/",
        ApiKey = "test-key"
    };

    private static AiOptions ConfiguredAi() => new()
    {
        Endpoint = "https://example.openai.azure.com/",
        ApiKey = "test-key",
        Deployment = "test-deployment"
    };
}
