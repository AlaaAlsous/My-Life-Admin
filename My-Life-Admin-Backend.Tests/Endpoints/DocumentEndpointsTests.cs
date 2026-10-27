using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class DocumentEndpointsTests
{
    private const int UserId = 1;

    private readonly Mock<IDocumentService> _serviceMock = new();
    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "Test"));

    [Fact]
    public async Task GetAll_ReturnsUsersDocumentsAsDtos()
    {
        _serviceMock.Setup(s => s.GetAllAsync(UserId)).ReturnsAsync(new List<Document> { Passport() });

        var result = await DocumentEndpoints.GetAllAsync(_user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<DocumentResponse>>>(result);
        var dto = Assert.Single(ok.Value!);
        Assert.Equal(
            new DocumentResponse(1, "Passport", "passport.pdf", "My passport", "ID", new DateTime(2030, 1, 1), new DateTime(2026, 1, 1)),
            dto);
    }

    [Fact]
    public async Task GetAll_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await DocumentEndpoints.GetAllAsync(anonymous, _serviceMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_ReturnsDocument_WhenFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(1, UserId)).ReturnsAsync(Passport());

        var result = await DocumentEndpoints.GetByIdAsync(1, _user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<DocumentResponse>>(result);
        Assert.Equal("Passport", ok.Value!.Title);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(1, UserId)).ReturnsAsync((Document?)null);

        var result = await DocumentEndpoints.GetByIdAsync(1, _user, _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task Search_PassesTrimmedQueryAndUser()
    {
        _serviceMock.Setup(s => s.SearchAsync(UserId, "pass")).ReturnsAsync(new List<Document> { Passport() });

        var result = await DocumentEndpoints.SearchAsync("  pass ", _user, _serviceMock.Object);

        var ok = Assert.IsType<Ok<List<DocumentResponse>>>(result);
        Assert.Single(ok.Value!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQuery_ReturnsBadRequest(string? query)
    {
        var result = await DocumentEndpoints.SearchAsync(query, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetExpiring_PassesDaysAndUser()
    {
        _serviceMock.Setup(s => s.GetExpiringWithinAsync(UserId, 60)).ReturnsAsync(new List<Document> { Passport() });

        var result = await DocumentEndpoints.GetExpiringAsync(_user, _serviceMock.Object, days: 60);

        var ok = Assert.IsType<Ok<List<DocumentResponse>>>(result);
        Assert.Single(ok.Value!);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(DocumentEndpoints.MaxExpiringDays + 1)]
    public async Task GetExpiring_DaysOutOfRange_ReturnsBadRequest(int days)
    {
        var result = await DocumentEndpoints.GetExpiringAsync(_user, _serviceMock.Object, days);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ValidForm_ReturnsCreatedAndSavesDocumentForUser()
    {
        Document? saved = null;
        _serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<Document>(), It.IsAny<Stream>()))
            .Callback<Document, Stream>((document, _) =>
            {
                saved = document;
                document.Id = 5;
            })
            .ReturnsAsync((Document document, Stream _) => document);

        var form = new CreateDocumentForm
        {
            Title = "  Passport ",
            Description = " ",
            Category = "ID",
            ExpiryDate = new DateTime(2030, 1, 1),
            File = File("passport.pdf"),
        };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        var created = Assert.IsType<Created<DocumentResponse>>(result);
        Assert.Equal("/api/documents/5", created.Location);
        Assert.NotNull(saved);
        Assert.Equal(UserId, saved.UserId);
        Assert.Equal("Passport", saved.Title);
        Assert.Null(saved.Description);
        Assert.Equal("ID", saved.Category);
        Assert.Equal("passport.pdf", saved.FileName);
    }

    [Fact]
    public async Task Create_FileNameWithPath_KeepsOnlyFileName()
    {
        Document? saved = null;
        _serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<Document>(), It.IsAny<Stream>()))
            .Callback<Document, Stream>((document, _) => saved = document)
            .ReturnsAsync((Document document, Stream _) => document);

        var form = new CreateDocumentForm { Title = "Passport", File = File(@"C:\fakepath\passport.pdf") };

        await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.Equal("passport.pdf", saved?.FileName);
    }

    [Fact]
    public async Task Create_MissingFile_ReturnsBadRequest()
    {
        var form = new CreateDocumentForm { Title = "Passport", File = null };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_EmptyFile_ReturnsBadRequest()
    {
        var form = new CreateDocumentForm { Title = "Passport", File = File("passport.pdf", length: 0) };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_TooLargeFile_ReturnsBadRequest()
    {
        var form = new CreateDocumentForm
        {
            Title = "Passport",
            File = File("passport.pdf", length: DocumentEndpoints.MaxFileSizeBytes + 1),
        };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("notes.txt")]
    [InlineData("no-extension")]
    public async Task Create_DisallowedFileType_ReturnsBadRequest(string fileName)
    {
        var form = new CreateDocumentForm { Title = "Passport", File = File(fileName) };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_UppercaseExtension_IsAllowed()
    {
        _serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<Document>(), It.IsAny<Stream>()))
            .ReturnsAsync((Document document, Stream _) => document);

        var form = new CreateDocumentForm { Title = "Passport", File = File("PASSPORT.PDF") };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<Created<DocumentResponse>>(result);
    }

    [Fact]
    public async Task Create_BlankTitle_ReturnsBadRequest()
    {
        var form = new CreateDocumentForm { Title = " ", File = File("passport.pdf") };

        var result = await DocumentEndpoints.CreateAsync(form, _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ReturnsUpdatedDocument_WhenFound()
    {
        Document? changes = null;
        _serviceMock
            .Setup(s => s.UpdateAsync(1, UserId, It.IsAny<Document>()))
            .Callback<int, int, Document>((_, _, c) => changes = c)
            .ReturnsAsync(Passport());

        var request = new UpdateDocumentRequest(" New title ", null, "Travel", new DateTime(2031, 1, 1));

        var result = await DocumentEndpoints.UpdateAsync(1, request, _user, _serviceMock.Object);

        Assert.IsType<Ok<DocumentResponse>>(result);
        Assert.Equal("New title", changes?.Title);
        Assert.Equal("Travel", changes?.Category);
        Assert.Equal(new DateTime(2031, 1, 1), changes?.ExpiryDate);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _serviceMock
            .Setup(s => s.UpdateAsync(1, UserId, It.IsAny<Document>()))
            .ReturnsAsync((Document?)null);

        var result = await DocumentEndpoints.UpdateAsync(
            1, new UpdateDocumentRequest("Title", null, null, null), _user, _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task Update_BlankTitle_ReturnsBadRequest()
    {
        var result = await DocumentEndpoints.UpdateAsync(
            1, new UpdateDocumentRequest("", null, null, null), _user, _serviceMock.Object);

        Assert.IsType<BadRequest<ErrorResponse>>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenDeleted()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, UserId)).ReturnsAsync(true);

        var result = await DocumentEndpoints.DeleteAsync(1, _user, _serviceMock.Object);

        Assert.IsType<NoContent>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNotDeleted()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, UserId)).ReturnsAsync(false);

        var result = await DocumentEndpoints.DeleteAsync(1, _user, _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DownloadFile_ReturnsFileWithOriginalNameAndContentType()
    {
        var content = new MemoryStream([1, 2, 3]);
        _serviceMock.Setup(s => s.OpenFileAsync(1, UserId)).ReturnsAsync(new DocumentFile(Passport(), content));

        var result = await DocumentEndpoints.DownloadFileAsync(1, _user, _serviceMock.Object);

        var file = Assert.IsType<FileStreamHttpResult>(result);
        Assert.Same(content, file.FileStream);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("passport.pdf", file.FileDownloadName);
    }

    [Fact]
    public async Task DownloadFile_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _serviceMock.Setup(s => s.OpenFileAsync(1, UserId)).ReturnsAsync((DocumentFile?)null);

        var result = await DocumentEndpoints.DownloadFileAsync(1, _user, _serviceMock.Object);

        Assert.IsType<NotFound>(result);
    }

    private static Document Passport() => new()
    {
        Id = 1,
        UserId = UserId,
        Title = "Passport",
        FileName = "passport.pdf",
        FilePath = "stored-key.pdf",
        Description = "My passport",
        Category = "ID",
        ExpiryDate = new DateTime(2030, 1, 1),
        UploadedAt = new DateTime(2026, 1, 1),
    };

    private static FormFile File(string fileName, long length = 3) =>
        new(new MemoryStream(new byte[Math.Min(length, 3)]), 0, length, "File", fileName);
}
