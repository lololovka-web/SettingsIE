using System.Text;
using System.Text.Json;
using SettingsIE.Models;

namespace SettingsIE.Services;

public class SettingsExporter : ISettingsExporter
{
    private readonly IWindowsSettingsRepository _repository;

    public SettingsExporter(IWindowsSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task ExportAsync(string outputPath, List<SettingsCategory> categories, IProgress<int>? progress = null)
    {
        await Task.Run(() =>
        {
            var exportData = new SettingsExportData
            {
                ExportDate = DateTime.Now,
                WindowsVersion = _repository.GetWindowsVersion(),
                Categories = new List<SettingsCategory>()
            };

            int totalItems = CountSelectedSubcategories(categories);
            int current = 0;

            foreach (var category in categories)
            {
                if (!category.IsSelected) continue;

                var exportedCat = new SettingsCategory
                {
                    Name = category.Name,
                    RegistryPaths = category.RegistryPaths,
                    SubCategories = new List<SettingsCategory>()
                };

                foreach (var sub in category.SubCategories)
                {
                    if (!sub.IsSelected) continue;

                    var exportedSub = new SettingsCategory
                    {
                        Name = sub.Name,
                        RegistryPaths = sub.RegistryPaths,
                        Values = new Dictionary<string, RegistryValueData>()
                    };

                    foreach (var regPath in sub.RegistryPaths)
                    {
                        var values = _repository.ReadRegistryValues(regPath);
                        foreach (var v in values)
                        {
                            exportedSub.Values[v.Key] = v.Value;
                        }
                    }

                    exportedCat.SubCategories.Add(exportedSub);
                    current++;
                    progress?.Report((int)((double)current / totalItems * 100));
                }

                exportData.Categories.Add(exportedCat);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(exportData, options);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        });
    }

    public async Task ExportToRegAsync(string outputPath, List<SettingsCategory> categories, IProgress<int>? progress = null)
    {
        await Task.Run(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Windows Registry Editor Version 5.00");
            sb.AppendLine();

            int totalItems = CountSelectedSubcategories(categories);
            int current = 0;

            foreach (var category in categories)
            {
                if (!category.IsSelected) continue;

                foreach (var sub in category.SubCategories)
                {
                    if (!sub.IsSelected) continue;

                    foreach (var regPath in sub.RegistryPaths)
                    {
                        var values = _repository.ReadRegistryValues(regPath);
                        if (values.Count == 0) continue;

                        sb.AppendLine($"[{regPath}]");
                        foreach (var kvp in values)
                        {
                            var name = kvp.Key == "" ? "@" : $"\"{kvp.Key}\"";
                            sb.AppendLine($"{name}={FormatRegValue(kvp.Value)}");
                        }
                        sb.AppendLine();
                    }

                    current++;
                    progress?.Report((int)((double)current / totalItems * 100));
                }
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.Unicode);
        });
    }

    private static string FormatRegValue(RegistryValueData val)
    {
        return val.Type switch
        {
            "String" => $"\"{val.Data}\"",
            "ExpandString" => $"\"{val.Data}\"",
            "DWord" => $"dword:{int.Parse(val.Data):x8}",
            "QWord" => $"hex(b):{FormatQWord(val.Data)}",
            "Binary" => $"hex:{FormatBinary(val.Data)}",
            "MultiString" => $"hex(7):{FormatMultiString(val.Data)}",
            _ => $"\"{val.Data}\""
        };
    }

    private static string FormatQWord(string data)
    {
        if (long.TryParse(data, out var val))
        {
            var bytes = BitConverter.GetBytes(val);
            return string.Join(",", bytes.Select(b => b.ToString("x2")));
        }
        return "00,00,00,00,00,00,00,00";
    }

    private static string FormatBinary(string hex)
    {
        if (hex.Length == 0) return "00";
        var parts = new List<string>();
        for (int i = 0; i < hex.Length; i += 2)
        {
            if (i + 1 < hex.Length)
                parts.Add(hex.Substring(i, 2));
        }
        return string.Join(",", parts);
    }

    private static string FormatMultiString(string data)
    {
        var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new List<byte>();
        foreach (var p in parts)
        {
            bytes.AddRange(Encoding.Unicode.GetBytes(p));
            bytes.AddRange(new byte[] { 0, 0 });
        }
        bytes.AddRange(new byte[] { 0, 0 });
        return string.Join(",", bytes.Select(b => b.ToString("x2")));
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
