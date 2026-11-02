using Microsoft.Extensions.Options;
using My_Life_Admin.Options;
using My_Life_Admin.Storage;
using Azure.Storage.Blobs;
using Moq;

namespace MyLife_Admin.Tests.Storage;

public class AzureBlobFileStorageTests
{
    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var options = Options.Create(
            new FileStorageOptions
            {
                ConnectionString = "",
                ContainerName = "documents"
            });

        var blobServiceClient = new Mock<BlobServiceClient>();

        Assert.Throws<InvalidOperationException>(
            () => new AzureBlobFileStorage(
                options,
                blobServiceClient.Object));
    }

    [Fact]
    public void Constructor_MissingContainerName_ThrowsInvalidOperationException()
    {
        var options = Options.Create(
            new FileStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = ""
            });

        var blobServiceClient = new Mock<BlobServiceClient>();

        Assert.Throws<InvalidOperationException>(
            () => new AzureBlobFileStorage(
                options,
                blobServiceClient.Object));
    }

    [Fact]
    public async Task SaveAsync_ReturnsKeyAndUploadsContent()
    {
        var blobClient = new Mock<BlobClient>();
        var containerClient = new Mock<BlobContainerClient>();
        var serviceClient = new Mock<BlobServiceClient>();

        containerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClient.Object);

        serviceClient
            .Setup(s => s.GetBlobContainerClient("documents"))
            .Returns(containerClient.Object);

        var options = Options.Create(
            new FileStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "documents"
            });

        var storage = new AzureBlobFileStorage(
            options,
            serviceClient.Object);

        using var content = new MemoryStream([1, 2, 3]);

        var key = await storage.SaveAsync(content, ".PDF");

        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, key);

        containerClient.Verify(
            c => c.CreateIfNotExistsAsync(
                It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<Azure.Storage.Blobs.Models.BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        blobClient.Verify(
            b => b.UploadAsync(
                content,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}