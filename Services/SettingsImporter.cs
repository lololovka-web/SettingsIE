using System.Text.Json;
using SettingsIE.Models;

namespace SettingsIE.Services;

public class SettingsImporter : ISettingsImporter
{
    private readonly IWindowsSettingsRepository _repository;

    public SettingsImporter(IWindowsSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SettingsExportData> LoadExportFileAsync(string inputPath)
    {
        return await Task.Run(() =>
        {
            var json = File.ReadAllText(inputPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<SettingsExportData>(json, options)
                ?? throw new InvalidOperationException("Не удалось загрузить файл экспорта");
        });
    }

    public async Task ImportAsync(SettingsExportData data, List<SettingsCategory> selectedCategories, IProgress<int>? progress = null)
    {
        await Task.Run(() =>
        {
            int totalItems = CountSelectedSubcategories(selectedCategories);
            int current = 0;

            foreach (var category in selectedCategories)
            {
                if (!category.IsSelected) continue;

                // Найти соответствующую категорию в данных экспорта
                var exportCat = data.Categories.FirstOrDefault(c =>
                    c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase));
                if (exportCat == null) continue;

                foreach (var sub in category.SubCategories)
                {
                    if (!sub.IsSelected) continue;

                    var exportSub = exportCat.SubCategories.FirstOrDefault(s =>
                        s.Name.Equals(sub.Name, StringComparison.OrdinalIgnoreCase));
                    if (exportSub == null) continue;

                    foreach (var regPath in sub.RegistryPaths)
                    {
                        var valuesForPath = exportSub.Values
                            .Where(v => v.Value.RegistryPath?.Equals(regPath, StringComparison.OrdinalIgnoreCase) == true)
                            .ToDictionary(v => v.Key, v => v.Value);

                        if (valuesForPath.Count > 0)
                        {
                            _repository.WriteRegistryValues(regPath, valuesForPath);
                        }
                    }

                    current++;
                    progress?.Report((int)((double)current / totalItems * 100));
                }
            }
        });
    }

    private static int CountSelectedSubcategories(List<SettingsCategory> categories)
    {
        int count = 0;
        foreach (var cat in categories)
        {
            if (!cat.IsSelected) continue;
            count += cat.SubCategories.Count(s => s.IsSelected);
        }
        return count;
    }
}
