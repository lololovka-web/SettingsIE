using System.Diagnostics;
using System.Text;

namespace SettingsIE.Services;

public class RegistryBackupService : IRegistryBackupService
{
    public async Task<string> BackupRegistryAsync(string backupPath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var exportedKeys = new List<string>
            {
                @"HKEY_CURRENT_USER\Control Panel",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications"
            };

            var tempDir = Path.Combine(Path.GetTempPath(), "SettingsIE_Backup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            int total = exportedKeys.Count;
            for (int i = 0; i < exportedKeys.Count; i++)
            {
                var regFilePath = Path.Combine(tempDir, $"backup_{SanitizeFileName(exportedKeys[i])}.reg");
                ExportRegistryKey(exportedKeys[i], regFilePath);
                progress?.Report((int)((double)(i + 1) / total * 100));
            }

            // Упаковываем в один .reg файл
            MergeRegFiles(tempDir, backupPath);

            // Очистка временных файлов
            try { Directory.Delete(tempDir, true); } catch { }

            return backupPath;
        });
    }

    public async Task RestoreRegistryAsync(string backupFilePath, IProgress<int>? progress = null)
    {
        await Task.Run(() =>
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Файл резервной копии не найден", backupFilePath);

            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"import \"{backupFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
                throw new InvalidOperationException("Ошибка при восстановлении реестра");

            progress?.Report(100);
        });
    }

    public async Task<bool> CreateSystemRestorePointAsync(string description)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", true);
                key?.SetValue("SystemRestorePointCreationFrequency", 0, Microsoft.Win32.RegistryValueKind.DWord);

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Checkpoint-Computer -Description '{description}' -RestorePointType MODIFY_SETTINGS\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(60000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });
    }

    private static void ExportRegistryKey(string registryPath, string outputFile)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"export \"{registryPath}\" \"{outputFile}\" /y",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            LogError($"Ошибка экспорта ключа {registryPath}: {ex.Message}");
        }
    }

    private static void MergeRegFiles(string sourceDir, string outputFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();

        foreach (var regFile in Directory.GetFiles(sourceDir, "*.reg"))
        {
            var content = File.ReadAllText(regFile);
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool skipHeader = true;

            foreach (var line in lines)
            {
                if (skipHeader && (line.StartsWith("Windows Registry") || string.IsNullOrWhiteSpace(line)))
                    continue;
                skipHeader = false;
                sb.AppendLine(line);
            }

            sb.AppendLine();
        }

        File.WriteAllText(outputFile, sb.ToString(), Encoding.Unicode);
    }

    private static string SanitizeFileName(string path)
    {
        return string.Join("_", path.Split(Path.GetInvalidFileNameChars()));
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
