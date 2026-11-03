using System.ClientModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using My_Life_Admin.Options;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace My_Life_Admin.Ai;

public sealed class DocumentIntelligenceDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly DocumentIntelligenceClient _diClient;
    private readonly string _modelId;
    private readonly ResponsesClient? _responsesClient;
    private readonly string _deployment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly BinaryData AnalysisSchemaData = BinaryData.FromString(AnalysisJsonSchema);

    private const string AnalysisSystemPrompt = """
    - You are a meticulous document assistant.
    - You extract structured information from an uploaded document.
    - Always respond in the language of the document.
    """;

    private const string AnalysisJsonSchema = """
    {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "title": { "type": "string", "description": "Short document title" },
        "category": { "type": "string", "description": "Document category, e.g. ID, Insurance, Contract, Receipt" },
        "expiryDate": { "type": "string", "description": "ISO date (YYYY-MM-DD) or empty string" },
        "description": { "type": "string", "description": "Short summary of the key information in the document" }
      },
      "required": ["title", "category", "expiryDate", "description"]
    }
    """;

    public DocumentIntelligenceDocumentAnalyzer(
        IOptions<DocumentIntelligenceOptions> diOptions,
        IOptions<AiOptions> aiOptions)
    {
        var di = diOptions.Value;

        if (!di.IsConfigured || !Uri.TryCreate(di.Endpoint, UriKind.Absolute, out var diEndpoint))
        {
            throw new InvalidOperationException(
                "DocumentIntelligence:Endpoint (an absolute URL) and DocumentIntelligence:ApiKey are required.");
        }

        _diClient = new DocumentIntelligenceClient(diEndpoint, new AzureKeyCredential(di.ApiKey!));
        _modelId = string.IsNullOrWhiteSpace(di.ModelId) ? "prebuilt-read" : di.ModelId;

        var ai = aiOptions.Value;
        _deployment = ai.Deployment ?? string.Empty;

        if (ai.IsConfigured && Uri.TryCreate(ai.Endpoint, UriKind.Absolute, out var aiEndpoint))
        {
            var responsesOptions = new ResponsesClientOptions { Endpoint = aiEndpoint };
            _responsesClient = new ResponsesClient(new ApiKeyCredential(ai.ApiKey!), responsesOptions);
        }
    }

    public async Task<DocumentAnalysisResult> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        byte[] bytes;
        using (var buffer = new MemoryStream())
        {
            await fileStream.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }

        if (bytes.Length == 0)
        {
            return Empty();
        }

        var text = await ExtractTextAsync(bytes, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Empty();
        }

        if (_responsesClient != null)
        {
            var extracted = await TryExtractWithAiAsync(text, cancellationToken);
            if (extracted != null)
            {
                return extracted;
            }
        }

        return HeuristicExtraction(text, fileName);
    }

    private static DocumentAnalysisResult Empty() => new(null, null, null, null);

    private async Task<string?> ExtractTextAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _diClient.AnalyzeDocumentAsync(
                WaitUntil.Completed, _modelId, BinaryData.FromBytes(bytes), cancellationToken);

            var result = operation.Value;

            var text = result.Content;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }

            var lines = result.Pages.SelectMany(page => page.Lines).Select(line => line.Content);
            return string.Join("\n", lines).Trim();
        }
        catch (Exception ex) when (
            ex is RequestFailedException or HttpRequestException ||
            (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new AiServiceException("The document reading service could not be reached.", ex);
        }
    }

    private async Task<DocumentAnalysisResult?> TryExtractWithAiAsync(string text, CancellationToken cancellationToken)
    {
        var input = new List<ResponseItem>
        {
            ResponseItem.CreateUserMessageItem(BuildExtractionPrompt(text)),
        };

        var options = new CreateResponseOptions(_deployment, input)
        {
            Instructions = AnalysisSystemPrompt,
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "document_analysis",
                    jsonSchema: AnalysisSchemaData,
                    jsonSchemaFormatDescription: "Structured document fields extracted from the uploaded file",
                    jsonSchemaIsStrict: true),
            },
        };

        ResponseResult response;
        try
        {
            response = await _responsesClient!.CreateResponseAsync(options, cancellationToken);
        }
        catch (Exception ex) when (
            ex is ClientResultException or HttpRequestException ||
            (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return null;
        }

        var json = ExtractJson(response.GetOutputText());
        var dto = JsonSerializer.Deserialize<AnalysisDto>(json, JsonOptions);

        return dto == null ? null : MapDto(dto);
    }

    private static string BuildExtractionPrompt(string text)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Below is the raw text of an uploaded document.");
        builder.AppendLine("Extract the requested fields into the structured format.");
        builder.AppendLine();
        builder.AppendLine("--- DOCUMENT ---");
        builder.AppendLine(Truncate(text, 12_000));
        return builder.ToString();
    }

    private static DocumentAnalysisResult MapDto(AnalysisDto dto) =>
        new(
            NullIfEmpty(dto.Title),
            NullIfEmpty(dto.Category),
            ParseDate(dto.ExpiryDate),
            NullIfEmpty(dto.Description));

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return text.Trim();
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + "\n...[truncated]...";
    }

    private static DocumentAnalysisResult HeuristicExtraction(string text, string fileName)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var title = lines.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(fileName);
        }

        if (title.Length > 200)
        {
            title = title[..200];
        }

        var description = text.Length > 1000 ? text[..1000] : text;

        return new DocumentAnalysisResult(title, null, FindExpiryDate(text), description);
    }

    private static DateTime? FindExpiryDate(string text)
    {
        foreach (Match match in Regex.Matches(text, @"(20\d{2})[-/.](0?[1-9]|1[0-2])[-/.](0?[1-9]|[12]\d|3[01])"))
        {
            if (DateTime.TryParse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return null;
    }

    private sealed class AnalysisDto
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? ExpiryDate { get; set; }
        public string? Description { get; set; }
    }
}
