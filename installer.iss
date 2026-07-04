; Inno Setup Script for SettingsIE
; ℹ Requires Inno Setup 6+ (https://jrsoftware.org/isdl.php)

#define MyAppName "SettingsIE"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SettingsIE"
#define MyAppURL "https://github.com/lololovka-web/SettingsIE"
#define MyAppExeName "SettingsIE.exe"

[Setup]
AppId={{B8A3C2E1-4F5D-4E6A-9B7C-8D9E0F1A2B3C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\publish
OutputBaseFilename=SettingsIE_setup
Compression=lzma2/max
SolidCompression=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительно:"
Name: "quicklaunchicon"; Description: "Добавить в панель быстрого запуска"; GroupDescription: "Дополнительно:"; Flags: unchecked

[Files]
Source: ".\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c ""rmdir /s /q ""{app}\logs"" 2>nul"""; Flags: runhidden

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveStringToFile(ExpandConstant('{app}\install_path.txt'), ExpandConstant('{app}'), False);
  end;
end;
