using Microsoft.Extensions.Logging.Abstractions;
using My_Life_Admin.Ai;
using My_Life_Admin.Models;
using My_Life_Admin.Options;

namespace MyLife_Admin.Tests.Ai;

public class AiChatClientTests
{
    [Fact]
    public async Task Placeholder_ReturnsPlaceholderReply()
    {
        var client = new PlaceholderAiChatClient();

        var reply = await client.GetReplyAsync([new ChatMessage { Role = ChatRole.User, Content = "Hi" }]);

        Assert.Equal(PlaceholderAiChatClient.Reply, reply);
    }

    [Fact]
    public void AiOptions_AllConnectionSettings_IsConfigured()
    {
        var options = Configured();

        Assert.True(options.IsConfigured);
        Assert.True(options.HasAnyConnectionSetting);
    }

    [Fact]
    public void AiOptions_NoConnectionSettings_IsNotConfigured()
    {
        var options = new AiOptions { Endpoint = "", ApiKey = " ", Deployment = null };

        Assert.False(options.IsConfigured);
        Assert.False(options.HasAnyConnectionSetting);
    }

    [Theory]
    [InlineData("https://example.openai.azure.com/", "", "")]
    [InlineData("", "key", "")]
    [InlineData("", "", "deployment")]
    [InlineData("https://example.openai.azure.com/", "key", "")]
    public void AiOptions_SomeConnectionSettings_IsPartial(string endpoint, string apiKey, string deployment)
    {
        var options = new AiOptions { Endpoint = endpoint, ApiKey = apiKey, Deployment = deployment };

        Assert.False(options.IsConfigured);
        Assert.True(options.HasAnyConnectionSetting);
    }

    [Fact]
    public void AzureOpenAiChatClient_CanBeCreatedFromValidSettings()
    {
        var client = new AzureOpenAiChatClient(
            Microsoft.Extensions.Options.Options.Create(Configured()),
            NullLogger<AzureOpenAiChatClient>.Instance);

        Assert.NotNull(client);
    }

    [Fact]
    public void AzureOpenAiChatClient_InvalidEndpoint_Throws()
    {
        var options = Configured();
        options.Endpoint = "not-a-url";

        Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAiChatClient(
                Microsoft.Extensions.Options.Options.Create(options),
                NullLogger<AzureOpenAiChatClient>.Instance));
    }

    [Fact]
    public void AzureOpenAiChatClient_MissingSettings_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new AzureOpenAiChatClient(
                Microsoft.Extensions.Options.Options.Create(new AiOptions()),
                NullLogger<AzureOpenAiChatClient>.Instance));
    }

    private static AiOptions Configured() => new()
    {
        Endpoint = "https://example.openai.azure.com/",
        ApiKey = "test-key",
        Deployment = "test-deployment"
    };
}
