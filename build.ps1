param(
    [switch]$NoPrompt,
    [switch]$CreateDesktopShortcut,
    [string]$DesktopDirectoryOverride
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $scriptRoot "JotTile.sln"
$appProjectPath = Join-Path $scriptRoot "JotTile\JotTile.csproj"
$configProjectPath = Join-Path $scriptRoot "JotTile.Config\JotTile.Config.csproj"
$scriptsDir = Join-Path $scriptRoot "scripts"
$outputDir = Join-Path $scriptRoot "app"
$outputExe = Join-Path $outputDir "JotTile.exe"
$configExe = Join-Path $outputDir "config.exe"
$uninstallScriptSource = Join-Path $scriptsDir "Uninstall-JotTile.ps1"
$uninstallScriptTarget = Join-Path $outputDir "Uninstall-JotTile.ps1"
$uninstallCmdTarget = Join-Path $outputDir "Uninstall JotTile.cmd"
$shortcutFileName = "JotTile.lnk"
$setupTitle = "JotTile Setup"
$newLine = [Environment]::NewLine
$runtimeIdentifier = "win-x64"
$buildConfiguration = "Release"

function Initialize-Ui {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    [System.Windows.Forms.Application]::EnableVisualStyles()
}

function Get-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $dotnetCandidates = @(
        "C:\Program Files\dotnet\dotnet.exe",
        "C:\Program Files (x86)\dotnet\dotnet.exe"
    )

    $dotnetPath = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $dotnetPath) {
        throw ".NET SDK was not found. Install a .NET SDK that supports net8.0-windows and re-run build.cmd."
    }

    return $dotnetPath
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
            throw "The app output is locked: $($item.FullName). Close any running JotTile.exe or config.exe and run the build again."
        }
    }
}

function Assert-ProjectsExist {
    if (-not (Test-Path $solutionPath)) {
        throw "Solution file was not found: $solutionPath"
    }

    if (-not (Test-Path $appProjectPath)) {
        throw "App project file was not found: $appProjectPath"
    }

    if (-not (Test-Path $configProjectPath)) {
        throw "Config project file was not found: $configProjectPath"
    }
}

function Invoke-AppBuild {
    $dotnet = Get-DotNetPath
    Assert-ProjectsExist

    $restoreArguments = @(
        "restore",
        $solutionPath
    )

    & $dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed."
    }

    $publishArguments = @(
        "publish",
        $appProjectPath,
        "-c", $buildConfiguration,
        "-r", $runtimeIdentifier,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $outputDir
    )

    & $dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "JotTile publish failed."
    }

    $configPublishArguments = @(
        "publish",
        $configProjectPath,
        "-c", $buildConfiguration,
        "-r", $runtimeIdentifier,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $outputDir
    )

    & $dotnet @configPublishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "config.exe publish failed."
    }
}

function Get-DesktopDirectory {
    if ($DesktopDirectoryOverride) {
        return $DesktopDirectoryOverride
    }

    return [Environment]::GetFolderPath("DesktopDirectory")
}

function Show-SetupDialog {
    param(
        [string]$Message,
        [string]$Title,
        [System.Windows.Forms.MessageBoxButtons]$Buttons,
        [System.Windows.Forms.MessageBoxIcon]$Icon,
        [switch]$UseInstallSound
    )

    if ($UseInstallSound) {
        [System.Media.SystemSounds]::Asterisk.Play()
    }

    return [System.Windows.Forms.MessageBox]::Show(
        $Message,
        $Title,
        $Buttons,
        $Icon
    )
}

function Show-YesNoDialog {
    param(
        [string]$Message,
        [string]$Title
    )

    $result = Show-SetupDialog `
        -Message $Message `
        -Title $Title `
        -Buttons ([System.Windows.Forms.MessageBoxButtons]::YesNo) `
        -Icon ([System.Windows.Forms.MessageBoxIcon]::Question) `
        -UseInstallSound

    return $result -eq [System.Windows.Forms.DialogResult]::Yes
}

function Show-InfoDialog {
    param(
        [string]$Message,
        [string]$Title
    )

    Show-SetupDialog `
        -Message $Message `
        -Title $Title `
        -Buttons ([System.Windows.Forms.MessageBoxButtons]::OK) `
        -Icon ([System.Windows.Forms.MessageBoxIcon]::Information) `
        -UseInstallSound | Out-Null
}

function Should-CreateDesktopShortcut {
    if ($NoPrompt) {
        return $CreateDesktopShortcut.IsPresent
    }

    $message = @(
        "Build completed successfully."
        ""
        "Create a desktop shortcut for JotTile?"
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
        $shortcut.Description = "Launch JotTile"
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
        "powershell -ExecutionPolicy Bypass -File ""%~dp0Uninstall-JotTile.ps1"" %*"
    )

    Set-Content -LiteralPath $uninstallCmdTarget -Value $cmdContent -Encoding ASCII
}

function Write-ConsoleSummary {
    param(
        [bool]$ShortcutCreated,
        [string]$ShortcutPath
    )

    Write-Host ""
    Write-Host "JotTile is ready."
    Write-Host "App folder: $outputDir"
    Write-Host "Main EXE: $outputExe"
    Write-Host "Config EXE: $configExe"
    Write-Host "Publish mode: self-contained ($runtimeIdentifier)"

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
        "JotTile is ready."
        ""
        "App folder:"
        $outputDir
        ""
        "Main app:"
        $outputExe
        ""
        "Config app:"
        $configExe
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

Write-Host "Building JotTile..."
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
