# StickyNote App

StickyNote App is a small Windows app for simple, persistent desktop notes. It opens as a real Windows window, saves notes locally, and restores active notes after a Windows restart.

## Quick Start

1. Double-click `build.cmd` in the project folder.
2. The setup builds the app.
3. After the build, setup optionally asks whether you want a desktop shortcut.
4. Start the app by double-clicking `app\StickyNoteApp.exe` or by using the desktop shortcut.

## What `build.ps1` Does

`build.ps1` is the main first-time setup entry point.

It handles all of this in one run:

* publishes a self-contained Windows EXE
* optionally creates a desktop shortcut
* places the uninstall entry in the output folder

Direct PowerShell command:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Usually the simpler option is:

```powershell
.\build.cmd
```

The build machine needs a .NET SDK that supports `net8.0-windows`.

## After the Build

The finished app is here:

```text
app\StickyNoteApp.exe
```

This published EXE is self-contained for `win-x64`, so the target machine does not need a separate .NET runtime installation.

On first start, a new note opens immediately. Press `Enter` to finalize it. After that, the note can only be moved, resized, or deleted with `X`.

## Where Notes Are Stored

Notes are saved locally here:

```text
%APPDATA%\SimpleStickyNotes\notes.json
```

There is no cloud, no account, and no network feature.

## Auto Start

When the real app starts for the first time, it automatically sets up user-level auto start so existing notes reappear after Windows sign-in.

It uses:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

No administrator rights are required.

## Uninstall

After the build, the output folder contains a clear uninstall entry:

```text
app\Uninstall StickyNote App.cmd
```

The uninstall process:

* asks whether it should close StickyNote App first if the app is currently running
* removes the auto start entry
* removes any desktop shortcut that points to this exact app EXE
* removes the app files in the output folder
* separately asks whether saved notes under `%APPDATA%\SimpleStickyNotes` should also be removed

If you want to keep your notes, answer that prompt with `No`.

## For Advanced Users

The app is implemented as a native WinForms application and is published with `dotnet publish` as a self-contained single-file Windows build. The build machine needs the .NET SDK, but the generated app is intended to run on matching `win-x64` machines without a separate .NET runtime install.
