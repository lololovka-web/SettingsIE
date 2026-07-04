using System.Text.Json.Serialization;

namespace SettingsIE.Models;

public class SettingsCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("registryPaths")]
    public List<string> RegistryPaths { get; set; } = new();

    [JsonPropertyName("values")]
    public Dictionary<string, RegistryValueData> Values { get; set; } = new();

    [JsonPropertyName("subCategories")]
    public List<SettingsCategory> SubCategories { get; set; } = new();

    [JsonIgnore]
    public bool IsSelected { get; set; } = true;
}

public class RegistryValueData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("registryPath")]
    public string RegistryPath { get; set; } = string.Empty;
}

public class SettingsExportData
{
    [JsonPropertyName("exportDate")]
    public DateTime ExportDate { get; set; } = DateTime.Now;

    [JsonPropertyName("windowsVersion")]
    public string WindowsVersion { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<SettingsCategory> Categories { get; set; } = new();
}
