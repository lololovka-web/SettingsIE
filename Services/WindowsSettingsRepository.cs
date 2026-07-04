using Microsoft.Win32;
using SettingsIE.Models;

namespace SettingsIE.Services;

public class WindowsSettingsRepository : IWindowsSettingsRepository
{
    public Dictionary<string, RegistryValueData> ReadRegistryValues(string registryPath)
    {
        var result = new Dictionary<string, RegistryValueData>();

        try
        {
            using var key = OpenRegistryKey(registryPath, writable: false);
            if (key == null) return result;

            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                if (value == null) continue;

                var valueKind = key.GetValueKind(valueName);
                result[valueName] = new RegistryValueData
                {
                    Type = valueKind.ToString(),
                    Data = SerializeValue(value, valueKind),
                    RegistryPath = registryPath
                };
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка чтения {registryPath}: {ex.Message}");
        }

        return result;
    }

    public void WriteRegistryValues(string registryPath, Dictionary<string, RegistryValueData> values)
    {
        if (values.Count == 0) return;

        try
        {
            if (!KeyExists(registryPath))
                CreateKey(registryPath);

            using var key = OpenRegistryKey(registryPath, writable: true);
            if (key == null) return;

            foreach (var kvp in values)
            {
                try
                {
                    var (value, kind) = DeserializeValue(kvp.Value.Type, kvp.Value.Data);
                    key.SetValue(kvp.Key, value, kind);
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка записи {registryPath}\\{kvp.Key}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка записи в {registryPath}: {ex.Message}");
        }
    }

    public bool KeyExists(string registryPath)
    {
        try
        {
            using var key = OpenRegistryKey(registryPath, writable: false);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    public void CreateKey(string registryPath)
    {
        try
        {
            var (hive, subPath) = ParseRegistryPath(registryPath);
            using var baseKey = GetHiveKey(hive);

            if (subPath != null)
            {
                if (baseKey != null)
                    baseKey.CreateSubKey(subPath, true);
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка создания ключа {registryPath}: {ex.Message}");
        }
    }

    public List<SettingsCategory> GetDefaultCategories()
    {
        return new List<SettingsCategory>
        {
            new()
            {
                Name = "Система",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Дисплей", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\Desktop" } },
                    new() { Name = "Уведомления", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications" } },
                    new() { Name = "Электропитание", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\PowerCfg" } },
                    new() { Name = "Хранилище", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense" } }
                }
            },
            new()
            {
                Name = "Устройства",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Мышь", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\Mouse" } },
                    new() { Name = "Клавиатура", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\Keyboard" } },
                    new() { Name = "Bluetooth", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Bluetooth" } },
                    new() { Name = "Автозапуск", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers" } }
                }
            },
            new()
            {
                Name = "Сеть и Интернет",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Прокси-сервер", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings" } },
                    new() { Name = "DNS", RegistryPaths = new() { @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" } }
                }
            },
            new()
            {
                Name = "Персонализация",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Темы оформления", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes" } },
                    new() { Name = "Панель задач", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" } },
                    new() { Name = "Фон рабочего стола", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\Desktop" } },
                    new() { Name = "Цвета", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent" } },
                    new() { Name = "Пуск", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartPage" } }
                }
            },
            new()
            {
                Name = "Приложения",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Приложения по умолчанию", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts" } },
                    new() { Name = "Автозагрузка", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" } }
                }
            },
            new()
            {
                Name = "Учетные записи",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Параметры входа", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Authentication" } },
                    new() { Name = "Синхронизация", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SettingSync" } }
                }
            },
            new()
            {
                Name = "Конфиденциальность",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Микрофон", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone" } },
                    new() { Name = "Камера", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam" } },
                    new() { Name = "Геолокация", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" } },
                    new() { Name = "Диагностика", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Diagnostics" } }
                }
            },
            new()
            {
                Name = "Обновления",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Активные часы", RegistryPaths = new() { @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings" } },
                    new() { Name = "Перезагрузка", RegistryPaths = new() { @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate" } }
                }
            },
            new()
            {
                Name = "Язык и регион",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Языковые настройки", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\International" } },
                    new() { Name = "Дата и время", RegistryPaths = new() { @"HKEY_CURRENT_USER\Control Panel\TimeDate" } }
                }
            },
            new()
            {
                Name = "Звук",
                SubCategories = new List<SettingsCategory>
                {
                    new() { Name = "Аудио", RegistryPaths = new() { @"HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio" } }
                }
            }
        };
    }

    public string GetWindowsVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                var productName = key.GetValue("ProductName")?.ToString();
                var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                var currentBuild = key.GetValue("CurrentBuild")?.ToString();
                return $"{productName} (Версия {displayVersion}, Сборка {currentBuild})";
            }
        }
        catch { }
        return "Windows 10/11";
    }

    private RegistryKey OpenRegistryKey(string registryPath, bool writable)
    {
        var (hive, subPath) = ParseRegistryPath(registryPath);
        var baseKey = GetHiveKey(hive);
        if (subPath == null) return baseKey;

        try
        {
            return baseKey?.OpenSubKey(subPath, writable);
        }
        catch
        {
            return null;
        }
    }

    private (RegistryHive? hive, string? subPath) ParseRegistryPath(string fullPath)
    {
        var parts = fullPath.Split('\\', 2);
        var hiveStr = parts[0];
        var subPath = parts.Length > 1 ? parts[1] : null;

        RegistryHive? hive = hiveStr switch
        {
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => null
        };

        return (hive, subPath);
    }

    private RegistryKey? GetHiveKey(RegistryHive? hive)
    {
        return hive switch
        {
            RegistryHive.CurrentUser => Registry.CurrentUser,
            RegistryHive.LocalMachine => Registry.LocalMachine,
            RegistryHive.ClassesRoot => Registry.ClassesRoot,
            RegistryHive.Users => Registry.Users,
            RegistryHive.CurrentConfig => Registry.CurrentConfig,
            _ => null!
        };
    }

    private string SerializeValue(object value, RegistryValueKind kind)
    {
        return kind switch
        {
            RegistryValueKind.Binary when value is byte[] bytes => Convert.ToHexString(bytes),
            RegistryValueKind.MultiString when value is string[] strs => string.Join("|", strs),
            RegistryValueKind.ExpandString or RegistryValueKind.String => value.ToString() ?? "",
            RegistryValueKind.DWord or RegistryValueKind.QWord => value.ToString() ?? "0",
            _ => value.ToString() ?? ""
        };
    }

    private (object value, RegistryValueKind kind) DeserializeValue(string typeStr, string data)
    {
        if (!Enum.TryParse<RegistryValueKind>(typeStr, out var kind))
            kind = RegistryValueKind.String;

        object value = kind switch
        {
            RegistryValueKind.String => data,
            RegistryValueKind.ExpandString => data,
            RegistryValueKind.DWord => int.TryParse(data, out var dw) ? dw : 0,
            RegistryValueKind.QWord => long.TryParse(data, out var qw) ? qw : 0L,
            RegistryValueKind.Binary => TryParseHex(data),
            RegistryValueKind.MultiString => data.Split('|', StringSplitOptions.RemoveEmptyEntries),
            _ => data
        };

        return (value, kind);
    }

    private static byte[] TryParseHex(string data)
    {
        try { return Convert.FromHexString(data); }
        catch { return Array.Empty<byte>(); }
    }

    private static void LogError(string message)
    {
        try
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "error.log");
            File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        catch { }
    }
}
