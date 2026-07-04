using SettingsIE.Models;

namespace SettingsIE.Services;

public interface ISettingsExporter
{
    Task ExportAsync(string outputPath, List<SettingsCategory> categories, IProgress<int>? progress = null);
    Task ExportToRegAsync(string outputPath, List<SettingsCategory> categories, IProgress<int>? progress = null);
}
