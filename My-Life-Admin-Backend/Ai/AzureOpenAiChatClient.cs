using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My_Life_Admin.Options;

namespace My_Life_Admin.Ai;

public sealed class AzureOpenAiChatClient : IAiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = new();
    private readonly string _responsesUrl;
    private readonly string _apiKey;
    private readonly string _deployment;
    private readonly string _systemPrompt;
    private readonly int _maxOutputTokens;
    private readonly ILogger<AzureOpenAiChatClient> _logger;

    public AzureOpenAiChatClient(IOptions<AiOptions> options, ILogger<AzureOpenAiChatClient> logger)
    {
        var settings = options.Value;

        if (!settings.IsConfigured || !Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                "Ai:Endpoint (an absolute URL), Ai:ApiKey and Ai:Deployment are required.");
        }

        _responsesUrl = endpoint.ToString().TrimEnd('/') + "/responses";
        _apiKey = settings.ApiKey!;
        _deployment = settings.Deployment!;
        _systemPrompt = settings.SystemPrompt;
        _maxOutputTokens = settings.MaxOutputTokens;
        _logger = logger;
    }

    public async Task<string> GetReplyAsync(
        IReadOnlyList<Models.ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        var instructions = _systemPrompt;
        var input = new List<InputMessage>();

        foreach (var message in conversation)
        {
            switch (message.Role)
            {
                case Models.ChatRole.System:
                    instructions = $"{instructions}\n\n{message.Content}";
                    break;
                case Models.ChatRole.Assistant:
                    input.Add(new InputMessage("assistant", message.Content));
                    break;
                default:
                    input.Add(new InputMessage("user", message.Content));
                    break;
            }
        }

        var payload = new
        {
            model = _deployment,
            instructions,
            input,
            tools = new[] { new { type = "web_search" } },
            max_output_tokens = _maxOutputTokens,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _responsesUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Azure OpenAI responses request failed.");
            throw new AiServiceException("The AI service could not be reached.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Azure OpenAI responses request returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    errorBody);
                throw new AiServiceException($"The AI service returned {(int)response.StatusCode}.", null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var reply = ExtractReply(body);

            if (string.IsNullOrWhiteSpace(reply))
            {
                throw new AiServiceException("The AI service returned an empty reply.");
            }

            return reply.Trim();
        }
    }

    private static string ExtractReply(string body)
    {
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var text = new StringBuilder();

        foreach (var item in output.EnumerateArray())
        {
            var type = item.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;

            if (type != "message" ||
                !item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProperty))
                {
                    text.Append(textProperty.GetString());
                }
            }
        }

        return text.ToString();
    }

    private sealed record InputMessage(string Role, string Content);
}
