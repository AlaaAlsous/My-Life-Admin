using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using My_Life_Admin.Options;
using OpenAI.Chat;

namespace My_Life_Admin.Ai;

public sealed class AzureOpenAiChatClient : IAiChatClient
{
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;
    private readonly int _maxOutputTokens;

    public AzureOpenAiChatClient(IOptions<AiOptions> options)
    {
        var settings = options.Value;

        if (!settings.IsConfigured || !Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                "Ai:Endpoint (an absolute URL), Ai:ApiKey and Ai:Deployment are required for Azure OpenAI.");
        }

        var client = new AzureOpenAIClient(endpoint, new AzureKeyCredential(settings.ApiKey!));
        _chatClient = client.GetChatClient(settings.Deployment);
        _systemPrompt = settings.SystemPrompt;
        _maxOutputTokens = settings.MaxOutputTokens;
    }

    public async Task<string> GetReplyAsync(
        IReadOnlyList<Models.ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(_systemPrompt) };
        messages.AddRange(conversation.Select(ToOpenAiMessage));

        var options = new ChatCompletionOptions { MaxOutputTokenCount = _maxOutputTokens };

        ChatCompletion completion;
        try
        {
            completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        }
        catch (Exception ex) when (
            ex is ClientResultException or HttpRequestException ||
            (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new AiServiceException("The AI service could not be reached.", ex);
        }

        var reply = string.Concat(completion.Content.Select(part => part.Text)).Trim();

        if (reply.Length == 0)
        {
            throw new AiServiceException("The AI service returned an empty reply.");
        }

        return reply;
    }

    private static ChatMessage ToOpenAiMessage(Models.ChatMessage message) =>
        message.Role == Models.ChatRole.Assistant
            ? new AssistantChatMessage(message.Content)
            : new UserChatMessage(message.Content);
}
