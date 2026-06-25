param(
    [switch]$NoPrompt,
    [switch]$CreateDesktopShortcut,
    [string]$DesktopDirectoryOverride
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $scriptRoot "src"
$scriptsDir = Join-Path $scriptRoot "scripts"
$outputDir = Join-Path $scriptRoot "app"
$outputExe = Join-Path $outputDir "StickyNoteApp.exe"
$uninstallScriptSource = Join-Path $scriptsDir "Uninstall-StickyNoteApp.ps1"
$uninstallScriptTarget = Join-Path $outputDir "Uninstall-StickyNoteApp.ps1"
$uninstallCmdTarget = Join-Path $outputDir "Uninstall StickyNote App.cmd"
$shortcutFileName = "StickyNote App.lnk"
$setupTitle = "StickyNote App Setup"
$newLine = [Environment]::NewLine

function Initialize-Ui {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    [System.Windows.Forms.Application]::EnableVisualStyles()
}

function Get-CompilerPath {
    $compilerCandidates = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )

    $compiler = $compilerCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $compiler) {
        throw "csc.exe was not found. Expected under C:\Windows\Microsoft.NET\Framework(64)\v4.0.30319\csc.exe."
    }

    return $compiler
}

function Reset-OutputDirectory {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    foreach ($item in Get-ChildItem -Path $outputDir -Force) {
        $removed = $false

        foreach ($attempt in 1..5) {
            try {
                Remove-Item -LiteralPath $item.FullName -Force -Recurse
                $removed = $true
                break
            }
            catch {
                Start-Sleep -Milliseconds 250
            }
        }

        if (-not $removed) {
            throw "The app output is locked: $($item.FullName). Close any running StickyNoteApp.exe and run the build again."
        }
    }
}

function Get-SourceFiles {
    $sources = Get-ChildItem -Path $sourceDir -Filter *.cs | Sort-Object Name | ForEach-Object { $_.FullName }
    if (-not $sources) {
        throw "No C# source files were found under $sourceDir."
    }

    return $sources
}

function Invoke-AppBuild {
    $compiler = Get-CompilerPath
    $sources = Get-SourceFiles

    $arguments = @(
        "/nologo",
        "/target:winexe",
        "/out:$outputExe",
        "/r:System.dll",
        "/r:System.Core.dll",
        "/r:System.Drawing.dll",
        "/r:System.Windows.Forms.dll",
        "/r:System.Runtime.Serialization.dll"
    ) + $sources

    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

function Get-DesktopDirectory {
    if ($DesktopDirectoryOverride) {
        return $DesktopDirectoryOverride
    }

    return [Environment]::GetFolderPath("DesktopDirectory")
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

function Should-CreateDesktopShortcut {
    if ($NoPrompt) {
        return $CreateDesktopShortcut.IsPresent
    }

    $message = @(
        "Build completed successfully."
        ""
        "Create a desktop shortcut for StickyNote App?"
    ) -join $newLine

    return Show-YesNoDialog -Message $message -Title $setupTitle
}

function New-DesktopShortcut {
    param(
        [string]$TargetPath
    )

    $desktopDirectory = Get-DesktopDirectory
    New-Item -ItemType Directory -Path $desktopDirectory -Force | Out-Null

    $shortcutPath = Join-Path $desktopDirectory $shortcutFileName
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $null

    try {
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $TargetPath
        $shortcut.WorkingDirectory = Split-Path -Parent $TargetPath
        $shortcut.Description = "Launch StickyNote App"
        $shortcut.IconLocation = "$TargetPath,0"
        $shortcut.Save()
    }
    finally {
        if ($shortcut) {
            [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shortcut)
        }

        [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
    }

    return $shortcutPath
}

function New-UninstallLauncher {
    Copy-Item -LiteralPath $uninstallScriptSource -Destination $uninstallScriptTarget -Force

    $cmdContent = @(
        "@echo off",
        "powershell -ExecutionPolicy Bypass -File ""%~dp0Uninstall-StickyNoteApp.ps1"" %*"
    )

    Set-Content -LiteralPath $uninstallCmdTarget -Value $cmdContent -Encoding ASCII
}

function Write-ConsoleSummary {
    param(
        [bool]$ShortcutCreated,
        [string]$ShortcutPath
    )

    Write-Host ""
    Write-Host "StickyNote App is ready."
    Write-Host "App folder: $outputDir"
    Write-Host "EXE: $outputExe"

    if ($ShortcutCreated) {
        Write-Host "Desktop shortcut: $ShortcutPath"
    }
    else {
        Write-Host "Desktop shortcut: not created"
    }

    Write-Host "Uninstall: $uninstallCmdTarget"
}

function Show-CompletionDialog {
    param(
        [bool]$ShortcutCreated,
        [string]$ShortcutPath
    )

    if ($NoPrompt) {
        return
    }

    $shortcutLine = if ($ShortcutCreated) { $ShortcutPath } else { "Not created" }

    $message = @(
        "StickyNote App is ready."
        ""
        "App folder:"
        $outputDir
        ""
        "Start:"
        $outputExe
        ""
        "Desktop shortcut:"
        $shortcutLine
        ""
        "Uninstall:"
        $uninstallCmdTarget
    ) -join $newLine

    Show-InfoDialog -Message $message -Title $setupTitle
}

if (-not $NoPrompt) {
    Initialize-Ui
}

Write-Host "Building StickyNote App..."
Reset-OutputDirectory
Invoke-AppBuild
New-UninstallLauncher

$shortcutCreated = $false
$shortcutPath = $null

if (Should-CreateDesktopShortcut) {
    $shortcutPath = New-DesktopShortcut -TargetPath $outputExe
    $shortcutCreated = $true
}

Write-ConsoleSummary -ShortcutCreated $shortcutCreated -ShortcutPath $shortcutPath
Show-CompletionDialog -ShortcutCreated $shortcutCreated -ShortcutPath $shortcutPath
