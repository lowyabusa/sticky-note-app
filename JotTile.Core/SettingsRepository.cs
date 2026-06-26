using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace JotTile.Core
{
    internal sealed class SettingsRepository
    {
        private readonly string _directoryPath;
        private readonly string _settingsFilePath;
        private readonly string _backupFilePath;
        private readonly AppLogger _logger;

        internal SettingsRepository()
            : this(AppIdentity.GetDataDirectoryPath(), new AppLogger())
        {
        }

        internal SettingsRepository(string dataDirectoryPath, AppLogger? logger = null)
        {
            _directoryPath = dataDirectoryPath;
            _settingsFilePath = Path.Combine(_directoryPath, AppIdentity.SettingsFileName);
            _backupFilePath = Path.Combine(_directoryPath, AppIdentity.SettingsBackupFileName);
            _logger = logger ?? new AppLogger();
        }

        internal string SettingsFilePath
        {
            get { return _settingsFilePath; }
        }

        internal RepositoryLoadResult<AppSettings> Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new RepositoryLoadResult<AppSettings>(AppSettings.CreateDefault());
            }

            try
            {
                AppSettings settings = JsonFileRepositoryHelpers.Read<AppSettings>(_settingsFilePath, CreateSerializer);
                settings.ApplyDefaults();
                return new RepositoryLoadResult<AppSettings>(settings);
            }
            catch (Exception ex) when (IsRecoverableReadException(ex))
            {
                _logger.Warning("settings-load-main", "Primary settings file is unreadable. Attempting backup recovery.", ex);
                return RecoverFromBackup();
            }
        }

        internal void Save(AppSettings settings)
        {
            settings.ApplyDefaults();
            JsonFileRepositoryHelpers.AtomicSave(
                _directoryPath,
                _settingsFilePath,
                _backupFilePath,
                "settings.tmp",
                settings,
                CreateSerializer);
        }

        private RepositoryLoadResult<AppSettings> RecoverFromBackup()
        {
            string mainQuarantinePath = TryQuarantine(_settingsFilePath, "settings");

            if (File.Exists(_backupFilePath))
            {
                try
                {
                    AppSettings settings = JsonFileRepositoryHelpers.Read<AppSettings>(_backupFilePath, CreateSerializer);
                    settings.ApplyDefaults();
                    File.Copy(_backupFilePath, _settingsFilePath, true);
                    return new RepositoryLoadResult<AppSettings>(
                        settings,
                        "The main settings file was corrupt. JotTile restored settings from settings.bak. Quarantined file: " + mainQuarantinePath);
                }
                catch (Exception backupException) when (IsRecoverableReadException(backupException))
                {
                    _logger.Error("settings-load-backup", "Backup settings file is unreadable.", backupException);
                    string backupQuarantinePath = TryQuarantine(_backupFilePath, "settings-backup");
                    return new RepositoryLoadResult<AppSettings>(
                        AppSettings.CreateDefault(),
                        "Both settings files were unreadable. Default settings were restored. Quarantined files: " + mainQuarantinePath + ", " + backupQuarantinePath);
                }
            }

            return new RepositoryLoadResult<AppSettings>(
                AppSettings.CreateDefault(),
                "The settings file was unreadable and no backup was available. Default settings were restored. Quarantined file: " + mainQuarantinePath);
        }

        private string TryQuarantine(string filePath, string prefix)
        {
            try
            {
                return JsonFileRepositoryHelpers.Quarantine(_directoryPath, filePath, prefix);
            }
            catch (Exception ex)
            {
                _logger.Warning("settings-quarantine", "Failed to quarantine a corrupt settings file.", ex);
                return filePath;
            }
        }

        private static bool IsRecoverableReadException(Exception exception)
        {
            return exception is IOException || exception is SerializationException;
        }

        private static DataContractJsonSerializer CreateSerializer()
        {
            return new DataContractJsonSerializer(typeof(AppSettings));
        }
    }
}
