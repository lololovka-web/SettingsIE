P.S. aslo you can check my project in https://settingsie.netlify.app/

P.S.S. To donwload the program, donwload the source code and enter it into terminal: C:\Users\Пользователь\Desktop\csprowects\SettingsIE> publish\SettingsIE_setup.exe (it my example) 

SettingsIE
A Windows utility for exporting and importing Windows 10/11 system settings through the Registry. Provides a straightforward graphical interface to back up, transfer, or restore configuration profiles across machines or after reinstallation.

Features
Export any combination of system settings categories to a structured JSON file or a standard .reg file. The export includes display configuration, power scheme, mouse and keyboard settings, theme and personalization preferences, taskbar behavior, internet and proxy options, language and region settings, privacy and diagnostics settings, and more. Each export records the Windows version and timestamp.

Import settings from a previously created JSON file, selecting which categories to apply. The import wizard reads the file, displays its contents in a browsable tree, and writes only the chosen values back to the Registry. A direct .reg import is also supported.

Before making any changes to the system, a full registry backup can be created with a single click. The backup combines multiple relevant registry hives into a single .reg file stored on the desktop. Restoration from a backup is equally straightforward.

The built-in config library allows saving and naming export snapshots locally, complete with a description. Saved configs can be reloaded into the import view at any time, deleted, or exported to an external JSON file.

A live operation log at the bottom of the window tracks every action and surfaces any errors. All operations run asynchronously with a progress bar, keeping the interface responsive even when reading or writing large registry branches.

