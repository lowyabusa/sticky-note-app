using System;
using System.IO;

namespace JotTile.Core
{
    internal static class AppIdentity
    {
        internal const string ProductName = "JotTile";
        internal const string LegacyDataDirectoryName = "SimpleStickyNotes";
        internal const string DataDirectoryName = "JotTile";
        internal const string LogDirectoryName = "JotTile";
        internal const string NotesFileName = "notes.json";
        internal const string NotesBackupFileName = "notes.bak";
        internal const string SettingsFileName = "settings.json";
        internal const string SettingsBackupFileName = "settings.bak";
        internal const string SettingsChangedEventName = @"Local\JotTile.SettingsChanged";
        internal const string CreateNoteEventName = @"Local\JotTile.CreateNote";
        internal const string AppMutexName = @"Local\JotTile.App";
        internal const string ConfigMutexName = @"Local\JotTile.Config";
        internal const string ConfigActivateEventName = @"Local\JotTile.ConfigActivate";
        internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        internal const string RunValueName = "JotTile";
        internal const string LegacyRunValueName = "SimpleStickyNotes";
        internal const string ShortcutName = "JotTile.lnk";
        internal const string ShortcutDescription = "Launch JotTile";
        internal const string DialogTitle = "JotTile";
        internal const string ConfigExecutableName = "config.exe";
        internal const string MainExecutableName = "JotTile.exe";

        internal static string GetDataDirectoryPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                DataDirectoryName);
        }

        internal static string GetLegacyDataDirectoryPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                LegacyDataDirectoryName);
        }

        internal static string GetLocalLogDirectoryPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                LogDirectoryName,
                "logs");
        }
    }
}
