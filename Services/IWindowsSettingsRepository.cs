using SettingsIE.Models;

namespace SettingsIE.Services;

public interface IWindowsSettingsRepository
{
    Dictionary<string, RegistryValueData> ReadRegistryValues(string registryPath);
    void WriteRegistryValues(string registryPath, Dictionary<string, RegistryValueData> values);
    bool KeyExists(string registryPath);
    void CreateKey(string registryPath);
    List<SettingsCategory> GetDefaultCategories();
    List<SettingsCategory> GetWindows11Categories();
    string GetWindowsVersion();
}
