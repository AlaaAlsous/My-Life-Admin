using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using My_Life_Admin.Ai;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace My_Life_Admin.Endpoints;

public static class DocumentEndpoints
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxExpiringDays = 3650;

    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 1000;
    public const int MaxCategoryLength = 100;
    public const int MaxFileNameLength = 255;

    public static readonly IReadOnlySet<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".heic", ".bmp", ".tif", ".tiff"
        };

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/search", SearchAsync);
        group.MapGet("/expiring", GetExpiringAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapGet("/{id:int}/file", DownloadFileAsync);

        group.MapPost("/", CreateAsync).DisableAntiforgery();

        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
        group.MapPost("/{id:int}/analyze", AnalyzeAsync);

        return app;
    }

    public static async Task<IResult> GetAllAsync(ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var documents = await documentService.GetAllAsync(userId);

        return TypedResults.Ok(documents.Select(ToResponse).ToList());
    }

    public static async Task<IResult> GetByIdAsync(int id, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var document = await documentService.GetByIdAsync(id, userId);

        return document == null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(document));
    }

    public static async Task<IResult> SearchAsync(string? q, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return TypedResults.BadRequest(new ErrorResponse("Search query is required."));
        }

        var documents = await documentService.SearchAsync(userId, q.Trim());

        return TypedResults.Ok(documents.Select(ToResponse).ToList());
    }

    public static async Task<IResult> GetExpiringAsync(ClaimsPrincipal user, IDocumentService documentService, int days = 30)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (days < 0 || days > MaxExpiringDays)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Days must be between 0 and {MaxExpiringDays}."));
        }

        var documents = await documentService.GetExpiringWithinAsync(userId, days);

        return TypedResults.Ok(documents.Select(ToResponse).ToList());
    }

    public static async Task<IResult> CreateAsync(
        [FromForm] CreateDocumentForm form, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var fileName = GetFileName(form.File);
        var error = ValidateMetadata(form.Title, form.Description, form.Category) ?? ValidateFile(form.File, fileName);

        if (error != null)
        {
            return TypedResults.BadRequest(new ErrorResponse(error));
        }

        var document = new Document
        {
            UserId = userId,
            Title = form.Title.Trim(),
            Description = TrimOrNull(form.Description),
            Category = TrimOrNull(form.Category),
            ExpiryDate = form.ExpiryDate,
            FileName = fileName,
        };

        await using var content = form.File!.OpenReadStream();
        var created = await documentService.CreateAsync(document, content);

        return TypedResults.Created($"/api/documents/{created.Id}", ToResponse(created));
    }

    public static async Task<IResult> UpdateAsync(
        int id, UpdateDocumentRequest request, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var error = ValidateMetadata(request.Title, request.Description, request.Category);

        if (error != null)
        {
            return TypedResults.BadRequest(new ErrorResponse(error));
        }

        var changes = new Document
        {
            Title = request.Title.Trim(),
            Description = TrimOrNull(request.Description),
            Category = TrimOrNull(request.Category),
            ExpiryDate = request.ExpiryDate,
        };

        var updated = await documentService.UpdateAsync(id, userId, changes);

        return updated == null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(updated));
    }

    public static async Task<IResult> DeleteAsync(int id, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await documentService.DeleteAsync(id, userId);

        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<IResult> DownloadFileAsync(int id, ClaimsPrincipal user, IDocumentService documentService)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var file = await documentService.OpenFileAsync(id, userId);

        if (file == null)
        {
            return TypedResults.NotFound();
        }

        if (!ContentTypes.TryGetContentType(file.Document.FileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return TypedResults.File(file.Content, contentType, file.Document.FileName);
    }

    public static async Task<IResult> AnalyzeAsync(
        int id,
        ClaimsPrincipal user,
        IDocumentService documentService,
        IDocumentAnalyzer documentAnalyzer,
        CancellationToken cancellationToken)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var file = await documentService.OpenFileAsync(id, userId);

        if (file == null)
        {
            return TypedResults.NotFound();
        }

        if (!ContentTypes.TryGetContentType(file.Document.FileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        try
        {
            await using var content = file.Content;
            var result = await documentAnalyzer.AnalyzeAsync(
                content, file.Document.FileName, contentType, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.ExtractedText))
            {
                await documentService.SaveExtractedTextAsync(id, userId, result.ExtractedText);
            }

            return TypedResults.Ok(result);
        }
        catch (AiServiceException)
        {
            return TypedResults.Json(
                new ErrorResponse("The AI service is unavailable right now. Please try again later."),
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static DocumentResponse ToResponse(Document document) =>
        new(
            document.Id,
            document.Title,
            document.FileName,
            document.Description,
            document.Category,
            document.ExpiryDate,
            document.UploadedAt);

    private static string GetFileName(IFormFile? file) =>
        file == null ? string.Empty : file.FileName.Split('/', '\\').Last().Trim();

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ValidateMetadata(string? title, string? description, string? category)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            return "Title is too long.";
        }

        if (description?.Trim().Length > MaxDescriptionLength)
        {
            return "Description is too long.";
        }

        if (category?.Trim().Length > MaxCategoryLength)
        {
            return "Category is too long.";
        }

        return null;
    }

    private static string? ValidateFile(IFormFile? file, string fileName)
    {
        if (file == null || file.Length == 0)
        {
            return "A file is required.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "File must be at most 10 MB.";
        }

        if (fileName.Length == 0 || fileName.Length > MaxFileNameLength)
        {
            return "Invalid file name.";
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(fileName)))
        {
            return "Only PDF and image files are allowed.";
        }

        return null;
    }
}
