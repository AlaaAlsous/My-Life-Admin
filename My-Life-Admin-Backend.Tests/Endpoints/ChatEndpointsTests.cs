using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.Ai;
using My_Life_Admin.DTOs.Chat;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class ChatEndpointsTests
{
    private const int UserId = 1;

    private readonly Mock<IChatService> _serviceMock = new();

    private readonly ClaimsPrincipal _user = new(
        new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test"));

    [Fact]
    public async Task Send_ReturnsUserMessageAndReplyAsDtos()
    {
        var createdAt = new DateTime(2026, 10, 28, 12, 0, 0);
        _serviceMock
            .Setup(s => s.SendAsync(UserId, "Hello", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatExchange(
                Message(1, ChatRole.User, "Hello", createdAt),
                Message(2, ChatRole.Assistant, "Hi!", createdAt)));

        var result = await ChatEndpoints.SendAsync(
            new SendChatMessageDto { Message = "Hello" }, _user, _serviceMock.Object, CancellationToken.None);

        var ok = Assert.IsType<Ok<ChatReplyDto>>(result);
        Assert.Equal((1, "user", "Hello"), (ok.Value!.UserMessage.Id, ok.Value.UserMessage.Role, ok.Value.UserMessage.Content));
        Assert.Equal((2, "assistant", "Hi!"), (ok.Value.Reply.Id, ok.Value.Reply.Role, ok.Value.Reply.Content));
        Assert.Equal(createdAt, ok.Value.Reply.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_EmptyMessage_ReturnsBadRequest(string message)
    {
        var result = await ChatEndpoints.SendAsync(
            new SendChatMessageDto { Message = message }, _user, _serviceMock.Object, CancellationToken.None);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Send_TooLongMessage_ReturnsBadRequest()
    {
        var dto = new SendChatMessageDto { Message = new string('a', ChatService.MaxContentLength + 1) };

        var result = await ChatEndpoints.SendAsync(dto, _user, _serviceMock.Object, CancellationToken.None);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Send_AiUnavailable_Returns502()
    {
        _serviceMock
            .Setup(s => s.SendAsync(UserId, "Hello", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiServiceException("down"));

        var result = await ChatEndpoints.SendAsync(
            new SendChatMessageDto { Message = "Hello" }, _user, _serviceMock.Object, CancellationToken.None);

        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, json.StatusCode);
        Assert.Equal(ChatEndpoints.AiUnavailableError, json.Value?.Error);
    }

    [Fact]
    public async Task Send_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await ChatEndpoints.SendAsync(
            new SendChatMessageDto { Message = "Hello" }, anonymous, _serviceMock.Object, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetHistory_ReturnsMessagesAsDtosInOrder()
    {
        _serviceMock
            .Setup(s => s.GetHistoryAsync(UserId, 10))
            .ReturnsAsync([
                Message(1, ChatRole.User, "Hi", DateTime.UtcNow),
                Message(2, ChatRole.Assistant, "Hello!", DateTime.UtcNow)
            ]);

        var result = await ChatEndpoints.GetHistoryAsync(_user, _serviceMock.Object, count: 10);

        var ok = Assert.IsType<Ok<List<ChatMessageDto>>>(result);
        Assert.Equal([("user", "Hi"), ("assistant", "Hello!")], ok.Value!.Select(m => (m.Role, m.Content)));
    }

    [Fact]
    public async Task GetHistory_DefaultCount_UsesDefaultHistoryCount()
    {
        _serviceMock
            .Setup(s => s.GetHistoryAsync(UserId, ChatService.DefaultHistoryCount))
            .ReturnsAsync([]);

        var result = await ChatEndpoints.GetHistoryAsync(_user, _serviceMock.Object);

        Assert.IsType<Ok<List<ChatMessageDto>>>(result);
        _serviceMock.Verify(s => s.GetHistoryAsync(UserId, ChatService.DefaultHistoryCount), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetHistory_CountBelowOne_ReturnsBadRequest(int count)
    {
        var result = await ChatEndpoints.GetHistoryAsync(_user, _serviceMock.Object, count);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    private static ChatMessage Message(int id, ChatRole role, string content, DateTime createdAt) => new()
    {
        Id = id,
        UserId = UserId,
        Role = role,
        Content = content,
        CreatedAt = createdAt
    };
}
