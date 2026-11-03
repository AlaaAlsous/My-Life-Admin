using System.Security.Claims;
using My_Life_Admin.Ai;
using My_Life_Admin.DTOs.Chat;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class ChatEndpoints
{
    public const string AiUnavailableError = "The AI assistant is unavailable right now. Please try again later.";

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat");

        group.MapPost("/", SendAsync);
        group.MapGet("/history", GetHistoryAsync);

        return app;
    }

    public static async Task<IResult> SendAsync(
        SendChatMessageDto dto,
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            return TypedResults.BadRequest(new ErrorResponse("Message is required."));
        }

        if (dto.Message.Trim().Length > ChatService.MaxContentLength)
        {
            return TypedResults.BadRequest(
                new ErrorResponse($"Message cannot exceed {ChatService.MaxContentLength} characters."));
        }

        try
        {
            var exchange = await chatService.SendAsync(userId, dto.Message, dto.DocumentId, cancellationToken);

            return TypedResults.Ok(new ChatReplyDto
            {
                UserMessage = ToDto(exchange.UserMessage),
                Reply = ToDto(exchange.Reply)
            });
        }
        catch (AiServiceException)
        {
            return TypedResults.Json(
                new ErrorResponse(AiUnavailableError),
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    public static async Task<IResult> GetHistoryAsync(
        ClaimsPrincipal user,
        IChatService chatService,
        int count = ChatService.DefaultHistoryCount)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (count < 1)
        {
            return TypedResults.BadRequest(new ErrorResponse("Count must be at least 1."));
        }

        var history = await chatService.GetHistoryAsync(userId, count);

        return TypedResults.Ok(history.Select(ToDto).ToList());
    }

    public static ChatMessageDto ToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            Role = message.Role.ToString().ToLowerInvariant(),
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }
}
