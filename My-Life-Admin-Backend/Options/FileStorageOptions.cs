namespace My_Life_Admin.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string LocalRootPath { get; set; } = "uploads";
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}
