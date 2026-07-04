namespace SettingsIE.Services;

public interface IRegistryBackupService
{
    Task<string> BackupRegistryAsync(string backupPath, IProgress<int>? progress = null);
    Task RestoreRegistryAsync(string backupFilePath, IProgress<int>? progress = null);
    Task<bool> CreateSystemRestorePointAsync(string description);
}
