# SettingsIE

**Settings Import/Export** — A Windows utility for exporting and importing Windows 10/11 settings via registry.

## Features

- **Export** selected categories of Windows settings to JSON or .reg file
- **Import** settings from previously exported JSON files
- **Import** .reg files directly
- **Backup** registry before making changes
- **Restore** registry from a backup file
- **Diff-ready JSON** structure with categories, timestamps and Windows version info

## Supported Categories

| Category | Registry Paths |
|---|---|
| Display | `HKCU\Control Panel\Desktop` |
| Notifications | `HKCU\...\Notifications` |
| Power | `HKCU\Control Panel\PowerCfg` |
| Storage | `HKCU\...\StorageSense` |
| Mouse | `HKCU\Control Panel\Mouse` |
| Keyboard | `HKCU\Control Panel\Keyboard` |
| Bluetooth | `HKCU\...\Bluetooth` |
| Proxy / Internet | `HKCU\...\Internet Settings` |
| Themes | `HKCU\...\Themes` |
| Taskbar | `HKCU\...\Explorer\Advanced` |
| Desktop Background | `HKCU\Control Panel\Desktop` |
| Colors / Accent | `HKCU\...\Accent` |
| Start Menu | `HKCU\...\StartPage` |
| Default Apps | `HKCU\...\FileExts` |
| Startup | `HKCU\...\Run` |
| Sign-in | `HKCU\...\Authentication` |
| Sync | `HKCU\...\SettingSync` |
| Microphone / Camera / Location | `HKCU\...\CapabilityAccessManager\ConsentStore` |
| Diagnostics | `HKCU\...\Diagnostics` |
| Windows Update | `HKLM\...\WindowsUpdate` |
| Language & Region | `HKCU\Control Panel\International` |
| Date & Time | `HKCU\Control Panel\TimeDate` |
| Audio | `HKCU\...\Multimedia\Audio` |

## Requirements

- Windows 10 / 11
- [.NET 8+](https://dotnet.microsoft.com/en-us/download) (builds from source)
- Administrator rights recommended for full registry access

## Usage

### GUI

```bash
dotnet run
```

Select categories on the **Export** tab, choose a destination path and click **Export**.
On the **Import** tab, load a JSON file, select categories and click **Import**.

### Export format (JSON)

```json
{
  "exportDate": "2026-07-04T12:00:00",
  "windowsVersion": "Windows 10 Pro (Version 22H2, Build 19045)",
  "categories": [
    {
      "name": "Персонализация",
      "subCategories": [
        {
          "name": "Темы оформления",
          "values": {
            "ThemeFile": { "type": "String", "data": "...", "registryPath": "..." }
          }
        }
      ]
    }
  ]
}
```

## Build from source

```bash
git clone <repo>
cd SettingsIE
dotnet build
dotnet run
```
