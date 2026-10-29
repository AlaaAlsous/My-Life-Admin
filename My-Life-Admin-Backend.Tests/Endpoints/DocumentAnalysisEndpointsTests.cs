using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using My_Life_Admin.Ai;
using My_Life_Admin.Endpoints;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Endpoints;

public class DocumentAnalysisEndpointsTests
{
    private const int UserId = 1;

    private readonly Mock<IDocumentService> _serviceMock = new();
    private readonly Mock<IDocumentAnalyzer> _analyzerMock = new();
    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "Test"));

    [Fact]
    public async Task Analyze_ReturnsExtractedFields()
    {
        _serviceMock
            .Setup(s => s.OpenFileAsync(1, UserId))
            .ReturnsAsync(new DocumentFile(Passport(), new MemoryStream([1, 2, 3])));
        _analyzerMock
            .Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), "passport.pdf", "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentAnalysisResult("Passport", "ID", new DateTime(2030, 1, 1), "My passport"));

        var result = await DocumentEndpoints.AnalyzeAsync(
            1, _user, _serviceMock.Object, _analyzerMock.Object, CancellationToken.None);

        var ok = Assert.IsType<Ok<DocumentAnalysisResult>>(result);
        Assert.Equal("Passport", ok.Value!.Title);
        Assert.Equal("ID", ok.Value.Category);
        Assert.Equal(new DateTime(2030, 1, 1), ok.Value.ExpiryDate);
        Assert.Equal("My passport", ok.Value.Description);
    }

    [Fact]
    public async Task Analyze_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await DocumentEndpoints.AnalyzeAsync(
            1, anonymous, _serviceMock.Object, _analyzerMock.Object, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
        _serviceMock.VerifyNoOtherCalls();
        _analyzerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Analyze_ReturnsNotFound_WhenDocumentMissing()
    {
        _serviceMock.Setup(s => s.OpenFileAsync(1, UserId)).ReturnsAsync((DocumentFile?)null);

        var result = await DocumentEndpoints.AnalyzeAsync(
            1, _user, _serviceMock.Object, _analyzerMock.Object, CancellationToken.None);

        Assert.IsType<NotFound>(result);
        _analyzerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Analyze_ReturnsBadGateway_WhenAiFails()
    {
        _serviceMock
            .Setup(s => s.OpenFileAsync(1, UserId))
            .ReturnsAsync(new DocumentFile(Passport(), new MemoryStream([1, 2, 3])));
        _analyzerMock
            .Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), "passport.pdf", "application/pdf", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiServiceException("The AI service could not be reached."));

        var result = await DocumentEndpoints.AnalyzeAsync(
            1, _user, _serviceMock.Object, _analyzerMock.Object, CancellationToken.None);

        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, json.StatusCode);
    }

    private static Document Passport() => new()
    {
        Id = 1,
        UserId = UserId,
        Title = "Passport",
        FileName = "passport.pdf",
        FilePath = "stored-key.pdf"
    };
}
