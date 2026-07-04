using System.Text.Json;
using SettingsIE.Models;

namespace SettingsIE.Services;

public class ConfigLibraryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public int CategoryCount { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class ConfigLibraryService
{
    private readonly string _libraryDir;

    public ConfigLibraryService()
    {
        _libraryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SettingsIE", "ConfigLibrary");
        Directory.CreateDirectory(_libraryDir);
    }

    public List<ConfigLibraryEntry> GetEntries()
    {
        var entries = new List<ConfigLibraryEntry>();
        var metaFile = Path.Combine(_libraryDir, "library.json");

        if (File.Exists(metaFile))
        {
            try
            {
                var json = File.ReadAllText(metaFile);
                entries = JsonSerializer.Deserialize<List<ConfigLibraryEntry>>(json) ?? new();
            }
            catch { }
        }

        return entries;
    }

    public void Save(string name, string description, SettingsExportData data)
    {
        var entries = GetEntries();

        var id = Guid.NewGuid().ToString("N");
        var fileName = $"{id}.json";

        var entry = new ConfigLibraryEntry
        {
            Id = id,
            Name = name,
            Description = description,
            Created = DateTime.Now,
            CategoryCount = data.Categories.Count,
            FileName = fileName
        };

        var configPath = Path.Combine(_libraryDir, fileName);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(configPath, JsonSerializer.Serialize(data, options));

        entries.Add(entry);
        SaveEntries(entries);
    }

    public SettingsExportData? Load(string id)
    {
        var entries = GetEntries();
        var entry = entries.FirstOrDefault(e => e.Id == id);
        if (entry == null) return null;

        var configPath = Path.Combine(_libraryDir, entry.FileName);
        if (!File.Exists(configPath)) return null;

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<SettingsExportData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    public void Delete(string id)
    {
        var entries = GetEntries();
        var entry = entries.FirstOrDefault(e => e.Id == id);
        if (entry == null) return;

        var configPath = Path.Combine(_libraryDir, entry.FileName);
        try { File.Delete(configPath); } catch { }

        entries.Remove(entry);
        SaveEntries(entries);
    }

    public string ExportToFile(string id, string outputPath)
    {
        var data = Load(id);
        if (data == null) throw new InvalidOperationException("Конфиг не найден");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(outputPath, json);
        return outputPath;
    }

    private void SaveEntries(List<ConfigLibraryEntry> entries)
    {
        var metaFile = Path.Combine(_libraryDir, "library.json");
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaFile, json);
    }
}
