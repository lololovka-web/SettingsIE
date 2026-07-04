# SettingsIE

**Settings Import/Export** — утилита для экспорта и импорта настроек Windows 10/11 через реестр.

## Возможности

- **Экспорт** выбранных категорий настроек Windows в JSON или .reg файл
- **Импорт** настроек из ранее экспортированных JSON-файлов
- **Импорт** .reg файлов напрямую
- **Резервное копирование** реестра перед изменениями
- **Восстановление** реестра из резервной копии
- **JSON-структура** с категориями, временем экспорта и версией Windows

## Поддерживаемые категории

| Категория | Путь в реестре |
|---|---|
| Дисплей | `HKCU\Control Panel\Desktop` |
| Уведомления | `HKCU\...\Notifications` |
| Электропитание | `HKCU\Control Panel\PowerCfg` |
| Хранилище | `HKCU\...\StorageSense` |
| Мышь | `HKCU\Control Panel\Mouse` |
| Клавиатура | `HKCU\Control Panel\Keyboard` |
| Bluetooth | `HKCU\...\Bluetooth` |
| Прокси / Интернет | `HKCU\...\Internet Settings` |
| Темы | `HKCU\...\Themes` |
| Панель задач | `HKCU\...\Explorer\Advanced` |
| Фон рабочего стола | `HKCU\Control Panel\Desktop` |
| Цвета / Акцент | `HKCU\...\Accent` |
| Пуск | `HKCU\...\StartPage` |
| Приложения по умолчанию | `HKCU\...\FileExts` |
| Автозагрузка | `HKCU\...\Run` |
| Параметры входа | `HKCU\...\Authentication` |
| Синхронизация | `HKCU\...\SettingSync` |
| Микрофон / Камера / Геолокация | `HKCU\...\CapabilityAccessManager\ConsentStore` |
| Диагностика | `HKCU\...\Diagnostics` |
| Обновления | `HKLM\...\WindowsUpdate` |
| Язык и регион | `HKCU\Control Panel\International` |
| Дата и время | `HKCU\Control Panel\TimeDate` |
| Звук | `HKCU\...\Multimedia\Audio` |

## Требования

- Windows 10 / 11
- [.NET 8+](https://dotnet.microsoft.com/en-us/download) (для сборки из исходников)
- Права администратора (рекомендуется для полного доступа к реестру)

## Использование

### GUI

```bash
dotnet run
```

На вкладке **Экспорт** выберите категории, укажите путь и нажмите **Экспортировать**.
На вкладке **Импорт** загрузите JSON-файл, выберите категории и нажмите **Импортировать**.

### Формат экспорта (JSON)

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

## Сборка из исходников

```bash
git clone <repo>
cd SettingsIE
dotnet build
dotnet run
```
