using SettingsIE.Models;

namespace SettingsIE.Services;

public interface ISettingsImporter
{
    Task<SettingsExportData> LoadExportFileAsync(string inputPath);
    Task ImportAsync(SettingsExportData data, List<SettingsCategory> selectedCategories, IProgress<int>? progress = null);
}
