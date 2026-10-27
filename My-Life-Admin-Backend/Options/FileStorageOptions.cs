namespace My_Life_Admin.Options;

// TODO(Azure Blob): add the Blob Storage settings here (e.g. ConnectionString and ContainerName).
// Keep the connection string out of appsettings.json: use user secrets locally and App Service configuration in Azure.
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    // Only used by LocalFileStorage. Relative to the backend's content root.
    public string LocalRootPath { get; set; } = "uploads";
}
