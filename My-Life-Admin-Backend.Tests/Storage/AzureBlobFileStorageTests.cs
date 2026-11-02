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

    [Fact]
    public async Task OpenReadAsync_ExistingBlob_ReturnsStream()
    {
        var blobClient = new Mock<BlobClient>();
        var containerClient = new Mock<BlobContainerClient>();
        var serviceClient = new Mock<BlobServiceClient>();

        containerClient
            .Setup(c => c.GetBlobClient("test.pdf"))
            .Returns(blobClient.Object);

        serviceClient
            .Setup(s => s.GetBlobContainerClient("documents"))
            .Returns(containerClient.Object);

        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Azure.Response.FromValue(
                true,
                Mock.Of<Azure.Response>()));

        blobClient
            .Setup(b => b.DownloadToAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((stream, _) =>
            {
                stream.Write([1, 2, 3]);
            })
            .Returns(Task.FromResult(
                Mock.Of<Azure.Response>()));

        var options = Options.Create(
            new FileStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "documents"
            });

        var storage = new AzureBlobFileStorage(
            options,
            serviceClient.Object);

        await using var stream =
            await storage.OpenReadAsync("test.pdf");

        Assert.NotNull(stream);

        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);

        Assert.Equal(
            new byte[] { 1, 2, 3 },
            copy.ToArray());
    }

    [Fact]
    public async Task OpenReadAsync_UnknownKey_ReturnsNull()
    {
        var blobClient = new Mock<BlobClient>();
        var containerClient = new Mock<BlobContainerClient>();
        var serviceClient = new Mock<BlobServiceClient>();

        containerClient
            .Setup(c => c.GetBlobClient("missing.pdf"))
            .Returns(blobClient.Object);

        serviceClient
            .Setup(s => s.GetBlobContainerClient("documents"))
            .Returns(containerClient.Object);

        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Azure.Response.FromValue(
                false,
                Mock.Of<Azure.Response>()));

        var options = Options.Create(
            new FileStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "documents"
            });

        var storage = new AzureBlobFileStorage(
            options,
            serviceClient.Object);

        var stream = await storage.OpenReadAsync("missing.pdf");

        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBlob()
    {
        var blobClient = new Mock<BlobClient>();
        var containerClient = new Mock<BlobContainerClient>();
        var serviceClient = new Mock<BlobServiceClient>();

        containerClient
            .Setup(c => c.GetBlobClient("test.pdf"))
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

        await storage.DeleteAsync("test.pdf");

        blobClient.Verify(
            b => b.DeleteIfExistsAsync(
                It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
                It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_TwoFiles_GetDifferentKeys()
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

        var first = await storage.SaveAsync(
            new MemoryStream([1]),
            ".pdf");

        var second = await storage.SaveAsync(
            new MemoryStream([2]),
            ".pdf");

        Assert.NotEqual(first, second);
    }
}