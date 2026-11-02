using Microsoft.Extensions.Options;
using My_Life_Admin.Options;
using My_Life_Admin.Storage;

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

        Assert.Throws<InvalidOperationException>(
            () => new AzureBlobFileStorage(options));
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

        Assert.Throws<InvalidOperationException>(
            () => new AzureBlobFileStorage(options));
    }
}