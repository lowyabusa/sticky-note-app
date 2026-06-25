param(
    [switch]$NoPrompt,
    [switch]$RemoveData,
    [switch]$KeepData,
    [switch]$CloseRunningApp,
    [switch]$KeepRunningApp,
    [string]$DataDirectory,
    [string]$DesktopDirectoryOverride,
    [string]$RunKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run",
    [string]$RunValueName = "SimpleStickyNotes"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$scriptPath = $MyInvocation.MyCommand.Path
$installDirectory = Split-Path -Parent $scriptPath
$appExePath = Join-Path $installDirectory "StickyNoteApp.exe"
$uninstallCmdPath = Join-Path $installDirectory "Uninstall StickyNote App.cmd"
$uninstallPs1Path = Join-Path $installDirectory "Uninstall-StickyNoteApp.ps1"
$dialogTitle = "StickyNote App Uninstall"
$newLine = [Environment]::NewLine

function Initialize-Ui {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    [System.Windows.Forms.Application]::EnableVisualStyles()
}

function Show-YesNoDialog {
    param(
        [string]$Message,
        [string]$Title
    )

    $result = [System.Windows.Forms.MessageBox]::Show(
        $Message,
        $Title,
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )

    return $result -eq [System.Windows.Forms.DialogResult]::Yes
}

function Show-InfoDialog {
    param(
        [string]$Message,
        [string]$Title
    )

    [System.Windows.Forms.MessageBox]::Show(
        $Message,
        $Title,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
}

function Show-ErrorDialog {
    param(
        [string]$Message,
        [string]$Title
    )

    [System.Windows.Forms.MessageBox]::Show(
        $Message,
        $Title,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
}

function Get-DesktopDirectory {
    if ($DesktopDirectoryOverride) {
        return $DesktopDirectoryOverride
    }

    return [Environment]::GetFolderPath("DesktopDirectory")
}

function Get-DataDirectory {
    if ($DataDirectory) {
        return $DataDirectory
    }

    return Join-Path $env:APPDATA "SimpleStickyNotes"
}

function Get-NormalizedPathOrNull {
    param(
        [string]$PathValue
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return $null
    }

    try {
        return [System.IO.Path]::GetFullPath($PathValue)
    }
    catch {
        return $null
    }
}

function Test-AppRunning {
    return @((Get-RunningAppProcesses)).Count -gt 0
}

function Get-RunningAppProcesses {
    $matchingProcesses = New-Object System.Collections.ArrayList
    $normalizedAppExePath = Get-NormalizedPathOrNull -PathValue $appExePath

    foreach ($process in Get-Process StickyNoteApp -ErrorAction SilentlyContinue) {
        try {
            $processPath = Get-NormalizedPathOrNull -PathValue $process.Path
            if ($processPath -and [string]::Equals($processPath, $normalizedAppExePath, [StringComparison]::OrdinalIgnoreCase)) {
                [void]$matchingProcesses.Add($process)
            }
        }
        catch {
        }
    }

    return @($matchingProcesses.ToArray())
}

function Get-ShouldCloseRunningApp {
    if ($CloseRunningApp -and $KeepRunningApp) {
        throw "CloseRunningApp and KeepRunningApp cannot be used together."
    }

    if ($CloseRunningApp) {
        return $true
    }

    if ($KeepRunningApp) {
        return $false
    }

    if ($NoPrompt) {
        throw "StickyNote App is currently running. Re-run uninstall with -CloseRunningApp or run it interactively to confirm closing the app."
    }

    $message = @(
        "StickyNote App is currently running."
        ""
        "Close the running app and continue uninstall?"
    ) -join $newLine

    return Show-YesNoDialog -Message $message -Title $dialogTitle
}

function Stop-RunningAppProcesses {
    $matchingProcesses = Get-RunningAppProcesses

    foreach ($process in $matchingProcesses) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
        }
        catch {
        }
    }

    foreach ($attempt in 1..20) {
        if (@((Get-RunningAppProcesses)).Count -eq 0) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "StickyNote App could not be closed. Please close it manually and run uninstall again."
}

function Get-ShouldRemoveData {
    if ($RemoveData -and $KeepData) {
        throw "RemoveData and KeepData cannot be used together."
    }

    if ($RemoveData) {
        return $true
    }

    if ($KeepData -or $NoPrompt) {
        return $false
    }

    $message = @(
        "Do you also want to remove the saved notes?"
        ""
        "This will affect:"
        (Get-DataDirectory)
    ) -join $newLine

    return Show-YesNoDialog -Message $message -Title $dialogTitle
}

function Remove-AutostartEntry {
    if (-not (Test-Path $RunKeyPath)) {
        return
    }

    $property = Get-ItemProperty -Path $RunKeyPath -Name $RunValueName -ErrorAction SilentlyContinue
    if ($null -ne $property) {
        Remove-ItemProperty -Path $RunKeyPath -Name $RunValueName -ErrorAction Stop
    }
}

function Remove-DesktopShortcut {
    $desktopDirectory = Get-DesktopDirectory
    if (-not (Test-Path $desktopDirectory)) {
        return
    }

    $targetPath = Get-NormalizedPathOrNull -PathValue $appExePath
    if (-not $targetPath) {
        return
    }

    $shell = New-Object -ComObject WScript.Shell

    try {
        foreach ($shortcutFile in Get-ChildItem -LiteralPath $desktopDirectory -Filter *.lnk -File -ErrorAction SilentlyContinue) {
            $shortcut = $null

            try {
                $shortcut = $shell.CreateShortcut($shortcutFile.FullName)
                $resolvedTarget = Get-NormalizedPathOrNull -PathValue $shortcut.TargetPath

                if ($resolvedTarget -and [string]::Equals($resolvedTarget, $targetPath, [StringComparison]::OrdinalIgnoreCase)) {
                    Remove-Item -LiteralPath $shortcutFile.FullName -Force -ErrorAction SilentlyContinue
                }
            }
            catch {
            }
            finally {
                if ($shortcut) {
                    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shortcut)
                }
            }
        }
    }
    finally {
        [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
    }
}

function Remove-DataDirectory {
    param(
        [bool]$ShouldRemoveData
    )

    if (-not $ShouldRemoveData) {
        return
    }

    $targetDirectory = Get-DataDirectory
    if (Test-Path $targetDirectory) {
        Remove-Item -LiteralPath $targetDirectory -Force -Recurse
    }
}

function Start-InstallCleanup {
    $cleanupScriptPath = Join-Path $env:TEMP ("StickyNoteApp-Uninstall-" + [Guid]::NewGuid().ToString("N") + ".cmd")
    $escapedInstallDirectory = $installDirectory.Replace("'", "''")

    $cleanupLines = @(
        "@echo off",
        "powershell -NoProfile -ExecutionPolicy Bypass -Command ""Start-Sleep -Seconds 2; if (Test-Path -LiteralPath '$escapedInstallDirectory') { Remove-Item -LiteralPath '$escapedInstallDirectory' -Recurse -Force -ErrorAction SilentlyContinue }""",
        "del /f /q ""%~f0"""
    )

    Set-Content -LiteralPath $cleanupScriptPath -Value $cleanupLines -Encoding ASCII
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "`"$cleanupScriptPath`"" -WindowStyle Hidden
}

function Write-ConsoleSummary {
    param(
        [bool]$RemovedData
    )

    Write-Host "StickyNote App was removed."
    Write-Host "App folder removed: $installDirectory"

    if ($RemovedData) {
        Write-Host "Saved notes were removed."
    }
    else {
        Write-Host "Saved notes were kept."
    }
}

function Show-CompletionDialog {
    param(
        [bool]$RemovedData
    )

    if ($NoPrompt) {
        return
    }

    $dataText = if ($RemovedData) { "Saved notes were removed." } else { "Saved notes were kept." }
    $message = @(
        "StickyNote App was removed."
        ""
        "App folder removed:"
        $installDirectory
        ""
        $dataText
    ) -join $newLine

    Show-InfoDialog -Message $message -Title $dialogTitle
}

if (-not $NoPrompt) {
    Initialize-Ui
}

if (Test-AppRunning) {
    $shouldCloseRunningApp = Get-ShouldCloseRunningApp

    if (-not $shouldCloseRunningApp) {
        if (-not $NoPrompt) {
            Show-InfoDialog -Message "Uninstall was canceled. StickyNote App is still running." -Title $dialogTitle
        }

        exit 0
    }

    Stop-RunningAppProcesses
}

$shouldRemoveData = Get-ShouldRemoveData
Remove-AutostartEntry
Remove-DesktopShortcut
Remove-DataDirectory -ShouldRemoveData $shouldRemoveData
Write-ConsoleSummary -RemovedData $shouldRemoveData
Show-CompletionDialog -RemovedData $shouldRemoveData
Start-InstallCleanup
