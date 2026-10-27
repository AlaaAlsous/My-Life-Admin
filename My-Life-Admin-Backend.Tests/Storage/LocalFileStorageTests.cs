using Microsoft.Extensions.Hosting;
using Moq;
using My_Life_Admin.Options;
using My_Life_Admin.Storage;

namespace MyLife_Admin.Tests.Storage;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"mylife-storage-{Guid.NewGuid():N}");
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.ContentRootPath).Returns(_contentRoot);

        _storage = new LocalFileStorage(
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions { LocalRootPath = "uploads" }),
            environment.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenOpenReadAsync_ReturnsSameContent()
    {
        var key = await _storage.SaveAsync(new MemoryStream([1, 2, 3]), ".pdf");

        await using var stream = await _storage.OpenReadAsync(key);

        Assert.NotNull(stream);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal(new byte[] { 1, 2, 3 }, copy.ToArray());
    }

    [Fact]
    public async Task SaveAsync_StoresFileUnderRootWithGeneratedNameAndLowercaseExtension()
    {
        var key = await _storage.SaveAsync(new MemoryStream([1]), ".PDF");

        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, key);
        Assert.True(File.Exists(Path.Combine(_contentRoot, "uploads", key)));
    }

    [Fact]
    public async Task SaveAsync_TwoFiles_GetDifferentKeys()
    {
        var first = await _storage.SaveAsync(new MemoryStream([1]), ".pdf");
        var second = await _storage.SaveAsync(new MemoryStream([2]), ".pdf");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task OpenReadAsync_UnknownKey_ReturnsNull()
    {
        var stream = await _storage.OpenReadAsync("missing.pdf");

        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        var key = await _storage.SaveAsync(new MemoryStream([1]), ".pdf");

        await _storage.DeleteAsync(key);

        Assert.Null(await _storage.OpenReadAsync(key));
    }

    [Fact]
    public async Task DeleteAsync_UnknownKey_DoesNotThrow()
    {
        await _storage.DeleteAsync("missing.pdf");
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("..")]
    [InlineData("sub/file.pdf")]
    [InlineData("")]
    public async Task OpenReadAsync_KeyOutsideRoot_Throws(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenReadAsync(key));
    }
}
