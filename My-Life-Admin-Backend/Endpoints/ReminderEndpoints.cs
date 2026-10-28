using System.Security.Claims;
using My_Life_Admin.DTOs.Reminders;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class ReminderEndpoints
{
    public static IEndpointRouteBuilder MapReminderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reminders");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/pending", GetPendingAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    public static async Task<IResult> GetAllAsync(
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var reminders = await reminderService.GetAllAsync(userId);

        return TypedResults.Ok(
            reminders.Select(ToDto).ToList());
    }

    public static async Task<IResult> GetPendingAsync(
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var reminders = await reminderService.GetPendingAsync(userId);

        return TypedResults.Ok(
            reminders.Select(ToDto).ToList());
    }

    public static async Task<IResult> GetByIdAsync(
        int id,
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var reminder = await reminderService.GetByIdAsync(id, userId);

        return reminder is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(reminder));
    }

    public static async Task<IResult> CreateAsync(
        CreateReminderDto dto,
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var reminder = new Reminder
        {
            UserId = userId,
            EventId = dto.EventId,
            DocumentId = dto.DocumentId,
            ReminderDate = dto.ReminderDate
        };

        var created = await reminderService.AddAsync(reminder);

        return TypedResults.Created(
            $"/api/reminders/{created.Id}",
            ToDto(created));
    }

    public static async Task<IResult> UpdateAsync(
        int id,
        UpdateReminderDto dto,
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var changes = new Reminder
        {
            EventId = dto.EventId,
            DocumentId = dto.DocumentId,
            ReminderDate = dto.ReminderDate
        };

        var updated = await reminderService.UpdateAsync(
            id,
            userId,
            changes);

        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(updated));
    }

    public static async Task<IResult> DeleteAsync(
        int id,
        ClaimsPrincipal user,
        IReminderService reminderService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await reminderService.DeleteAsync(id, userId);

        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static ReminderDto ToDto(Reminder reminder)
    {
        return new ReminderDto
        {
            Id = reminder.Id,
            UserId = reminder.UserId,
            EventId = reminder.EventId,
            DocumentId = reminder.DocumentId,
            ReminderDate = reminder.ReminderDate,
            SentAt = reminder.SentAt,
            Status = reminder.Status
        };
    }
}