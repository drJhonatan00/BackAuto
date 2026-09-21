<div align="center">

# BackAuto

> A focused Windows desktop utility for reliable, incremental file backups.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms-0078D4?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/winforms/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-0078D4?logo=windows&logoColor=white)](https://www.microsoft.com/windows/)
[![Language](https://img.shields.io/badge/language-C%23-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)

BackAuto is a lightweight Windows Forms application built with C# and .NET 8. It lets you select important files, choose a backup destination, and run incremental backups on a schedule. The application can remain active in the Windows notification area while scheduled backups continue in the background.

It is designed to be understandable, transparent, and easy to own. There is no cloud account, no external service, and no proprietary backup format. BackAuto copies your files directly to a folder that you control.


> **Current scope:** BackAuto is a local file-copy utility. It is not a full disk image tool, cloud synchronisation client, or ransomware-protection system.

</div>

## Why BackAuto?

Many backup tools are built around complex workflows, subscriptions, or services that hide where your data is stored. BackAuto takes a smaller and more direct approach:

- Select the files that matter.
- Choose where their copies should live.
- Decide how often the backup should run.
- Keep the application open, either normally or in the notification area.

The backup engine compares each source file with its destination copy. It copies a file when the destination does not exist, when the source has changed, or when the file sizes differ. Files that are already up to date are skipped.

## Features

### Incremental backups

BackAuto copies only new or changed files. This reduces unnecessary disk activity and makes repeated scheduled backups faster than copying every file on every run.

### Flexible file selection

Select multiple individual files through the standard Windows file picker. The selected paths are stored locally so that the backup set is available the next time BackAuto opens.

### Configurable destination

Backups are stored in `C:\Backup` by default. You can choose another folder at any time, including an external drive or a network location that is available to Windows.

### Automatic scheduling

Set an interval in minutes and enable **Run automatically**. BackAuto checks the schedule in the background and starts a backup when the configured interval has elapsed since the last completed run.

### Windows notification area mode

Click **Run in background** to hide the main window while keeping BackAuto active. Scheduled backups continue to run. Double-click the notification-area icon to restore the window. The icon menu also provides commands to open BackAuto, run a backup immediately, or exit the application.

### Light and dark themes

Switch between light and dark themes from the application sidebar. The selected theme is retained in the local settings file.

### Activity log

The activity panel records backup progress, skipped files, copied files, and errors. This makes it possible to understand what happened without guessing.

### Local configuration

BackAuto stores its settings in the current user's local application data directory. No account or internet connection is required.

## Screenshots

<img width="1174" height="729" alt="1000037526" src="https://github.com/user-attachments/assets/4564d507-3421-40bc-9a15-c0223251cff0" />


## Requirements

- Windows 10 version 1809 or later.
- Visual Studio 2022 with the **.NET desktop development** workload.
- .NET 8 SDK or a Visual Studio installation that includes the .NET 8 targeting pack.

The project targets `net8.0-windows10.0.19041.0` and uses Windows Forms.

## Getting started

### Open the solution in Visual Studio

1. Clone the repository:

   ```powershell
   git clone https://github.com/YOUR_USERNAME/BackAuto.git
   cd BackAuto
   ```

2. Open `BackAuto.sln` in Visual Studio 2022.
3. Select the `Debug` or `Release` configuration.
4. Select `Any CPU` as the platform.
5. Press `F5` to build and run the application.

### Build from the command line

On a Windows machine with the .NET 8 SDK installed:

```powershell
dotnet restore BackAuto.sln
dotnet build BackAuto.sln --configuration Release
```

The compiled output is written to:

```text
BackAuto/bin/Release/net8.0-windows10.0.19041.0/
```

### Publish a self-contained Windows executable

To publish a standalone 64-bit Windows build that includes the .NET runtime:

```powershell
dotnet publish BackAuto/BackAuto.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output publish/win-x64
```

The published application will be available in `publish/win-x64`.

## How to use BackAuto

### 1. Add files

Click **Add files** and select one or more files. Each selected file appears in the backup list and is checked by default.

### 2. Choose a destination

Use **Browse** to choose a destination folder. If you do not change the setting, BackAuto uses:

```text
C:\Backup
```

The destination folder is created automatically when the first backup runs.

### 3. Configure the schedule

Enable **Run automatically**, enter an interval in minutes, and click **Save settings**. For example, an interval of `60` runs the backup approximately once per hour while BackAuto remains open.

### 4. Run a backup manually

Click **Run backup now** whenever you want to start a backup immediately. The activity panel displays the progress and final result.

### 5. Keep BackAuto out of the way

Click **Run in background** to move BackAuto to the Windows notification area. The application remains active without occupying the taskbar. To return to the main window, double-click the BackAuto icon or choose **Open BackAuto** from its context menu.

### 6. Remove the backup set

Click **Clear all** to remove every selected file from the list and permanently clear the saved backup paths. This does **not** delete any files already present in the destination folder.

## How scheduling works

The scheduler runs inside the BackAuto desktop process. A timer checks the configured interval every 30 seconds. When the interval has elapsed, BackAuto starts the backup if another backup is not already running.

The application must remain open for scheduled backups to run. It may be hidden in the notification area, but closing BackAuto stops the scheduler. The current version does not register itself to start automatically with Windows.

The backup process is asynchronous and uses cancellation support internally. A source file that cannot be read, or a destination file that cannot be created, is reported in the activity log while the remaining files continue to be processed.

## Data and privacy

BackAuto does not upload files or use a remote API. File contents are copied directly from the source path to the selected destination path.

The application stores configuration data as JSON at:

```text
%LOCALAPPDATA%\BackAuto\settings.json
```

This file contains the selected source paths, the destination folder, the interval, the theme preference, the schedule state, and the last backup timestamp. It does not contain the contents of the backed-up files.

## Project structure

```text
BackAuto/
├── BackAuto.sln
├── README.md
└── BackAuto/
    ├── BackAuto.csproj
    ├── MainForm.cs
    ├── Program.cs
    ├── app.manifest
    ├── Models/
    │   └── AppSettings.cs
    └── Services/
        └── BackupService.cs
```

### Main components

| Component | Responsibility |
|---|---|
| `MainForm.cs` | Builds the user interface, handles user actions, manages themes, schedules backups, and controls the notification-area icon. |
| `BackupService.cs` | Performs asynchronous incremental file copies and reports progress. |
| `AppSettings.cs` | Loads and saves local JSON configuration. |
| `Program.cs` | Configures and starts the Windows Forms application. |
| `app.manifest` | Declares Windows application compatibility metadata. |

## Design principles

BackAuto follows a few deliberately simple principles:

1. **The user owns the destination.** Backups are ordinary files in a folder chosen by the user.
2. **Do not copy unchanged data.** Existing up-to-date files are skipped.
3. **Show what happened.** The activity log exposes progress and errors.
4. **Keep the application local.** No account, server, or network dependency is required.
5. **Make the interface calm.** The UI uses clear hierarchy, restrained colour, light and dark themes, and a notification-area mode.

## Limitations and operational notes

BackAuto currently has the following limitations:

- Scheduled backups run only while BackAuto is open.
- The application does not yet start automatically with Windows.
- Backups are file copies, not versioned snapshots.
- If a source file is deleted, its old copy in the destination is not automatically deleted.
- Files with the same name from different source folders are copied into the same destination folder and may conflict.
- There is no built-in encryption, compression, cloud upload, or restore wizard.
- The application does not guarantee protection against disk failure, theft, malware, or accidental deletion of the destination folder.

For important data, use more than one backup destination and periodically verify that copied files can be opened.

## Roadmap

Potential future improvements include:

- Start BackAuto automatically when Windows starts.
- Support source folders with preserved directory structure.
- Add backup profiles for different file groups and destinations.
- Add versioned backups and retention policies.
- Add a restore browser with file previews.
- Improve conflict handling for files with identical names.
- Add optional compression and encryption.
- Add automated tests for the backup engine.
- Add a GitHub Actions build and release pipeline.
- Add Windows toast notifications for completed and failed backups.

## Contributing

Contributions are welcome. Before opening a pull request:

1. Create a focused feature branch.
2. Keep user-facing text in English.
3. Preserve the existing light and dark theme behaviour.
4. Avoid introducing external services for local backup operations.
5. Build the solution in Release mode and check the activity flow manually.
6. Explain the user-facing change and any behaviour that may affect existing settings.

For larger changes, open an issue first so the design can be discussed before implementation.

## Issue reports

When reporting a problem, include:

- Windows version.
- Visual Studio or .NET SDK version.
- BackAuto version or commit hash.
- The source and destination types, such as local disk, USB drive, or network location.
- The exact steps that reproduce the problem.
- Relevant activity-log messages.

Do not attach private files or sensitive paths unless they have been anonymised.

## Acknowledgements

BackAuto is built on the .NET platform and Windows Forms. The project uses standard .NET libraries for file access, JSON configuration, asynchronous operations, and the Windows notification area.
<!-- Watashi wa Watashi sore dake -->
