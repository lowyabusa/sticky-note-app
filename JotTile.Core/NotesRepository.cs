using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace JotTile.Core
{
    internal sealed class NotesRepository
    {
        private readonly string _directoryPath;
        private readonly string _legacyDirectoryPath;
        private readonly string _notesFilePath;
        private readonly string _backupFilePath;
        private readonly AppLogger _logger;

        internal NotesRepository()
            : this(AppIdentity.GetDataDirectoryPath(), AppIdentity.GetLegacyDataDirectoryPath(), new AppLogger())
        {
        }

        internal NotesRepository(string dataDirectoryPath, string legacyDirectoryPath, AppLogger? logger = null)
        {
            _directoryPath = dataDirectoryPath;
            _legacyDirectoryPath = legacyDirectoryPath;
            _notesFilePath = Path.Combine(_directoryPath, AppIdentity.NotesFileName);
            _backupFilePath = Path.Combine(_directoryPath, AppIdentity.NotesBackupFileName);
            _logger = logger ?? new AppLogger();
        }

        internal string NotesFilePath
        {
            get { return _notesFilePath; }
        }

        internal RepositoryLoadResult<List<NoteRecord>> Load()
        {
            string? migrationMessage = TryMigrateLegacyNotes();

            if (!File.Exists(_notesFilePath))
            {
                return new RepositoryLoadResult<List<NoteRecord>>(new List<NoteRecord>(), migrationMessage);
            }

            try
            {
                List<NoteRecord> notes = JsonFileRepositoryHelpers.Read<List<NoteRecord>>(_notesFilePath, CreateSerializer);
                return new RepositoryLoadResult<List<NoteRecord>>(notes, migrationMessage);
            }
            catch (Exception ex) when (IsRecoverableReadException(ex))
            {
                _logger.Warning("notes-load-main", "Primary notes file is unreadable. Attempting backup recovery.", ex);
                return RecoverFromBackup(migrationMessage, ex);
            }
        }

        internal void Save(IList<NoteRecord> notes)
        {
            JsonFileRepositoryHelpers.AtomicSave(
                _directoryPath,
                _notesFilePath,
                _backupFilePath,
                "notes.tmp",
                notes,
                CreateSerializer);
        }

        private RepositoryLoadResult<List<NoteRecord>> RecoverFromBackup(string? migrationMessage, Exception mainException)
        {
            string mainQuarantinePath = TryQuarantine(_notesFilePath, "notes");

            if (File.Exists(_backupFilePath))
            {
                try
                {
                    List<NoteRecord> notes = JsonFileRepositoryHelpers.Read<List<NoteRecord>>(_backupFilePath, CreateSerializer);
                    File.Copy(_backupFilePath, _notesFilePath, true);
                    _logger.Warning("notes-load-backup", "Recovered notes from backup copy.");
                    return new RepositoryLoadResult<List<NoteRecord>>(
                        notes,
                        ComposeRecoveryMessage(migrationMessage, mainQuarantinePath));
                }
                catch (Exception backupException) when (IsRecoverableReadException(backupException))
                {
                    _logger.Error("notes-load-backup", "Backup notes file is unreadable.", backupException);
                    string backupQuarantinePath = TryQuarantine(_backupFilePath, "notes-backup");
                    return new RepositoryLoadResult<List<NoteRecord>>(
                        new List<NoteRecord>(),
                        ComposeDataLossMessage(migrationMessage, mainQuarantinePath, backupQuarantinePath));
                }
            }

            return new RepositoryLoadResult<List<NoteRecord>>(
                new List<NoteRecord>(),
                ComposeDataLossMessage(migrationMessage, mainQuarantinePath, null));
        }

        private string? TryMigrateLegacyNotes()
        {
            if (File.Exists(_notesFilePath))
            {
                if (File.Exists(Path.Combine(_legacyDirectoryPath, AppIdentity.NotesFileName)))
                {
                    _logger.Info("notes-migration", "Legacy notes file left untouched because a JotTile notes file already exists.");
                }

                return null;
            }

            string legacyFilePath = Path.Combine(_legacyDirectoryPath, AppIdentity.NotesFileName);
            if (!File.Exists(legacyFilePath))
            {
                return null;
            }

            try
            {
                Directory.CreateDirectory(_directoryPath);
                File.Copy(legacyFilePath, _notesFilePath, false);
                JsonFileRepositoryHelpers.Read<List<NoteRecord>>(_notesFilePath, CreateSerializer);
                _logger.Info("notes-migration", "Legacy notes were copied to the JotTile data directory.");
                return "Legacy notes were copied into the new JotTile data directory.";
            }
            catch (Exception ex) when (IsRecoverableReadException(ex) || ex is UnauthorizedAccessException)
            {
                _logger.Error("notes-migration", "Legacy note migration failed.", ex);
                JsonFileRepositoryHelpers.TryDelete(_notesFilePath);
                return "Legacy notes could not be migrated automatically. JotTile started with a new data store.";
            }
        }

        private string TryQuarantine(string filePath, string prefix)
        {
            try
            {
                return JsonFileRepositoryHelpers.Quarantine(_directoryPath, filePath, prefix);
            }
            catch (Exception ex)
            {
                _logger.Warning("notes-quarantine", "Failed to quarantine a corrupt notes file.", ex);
                return filePath;
            }
        }

        private static string ComposeRecoveryMessage(string? migrationMessage, string mainQuarantinePath)
        {
            string baseMessage = "The main notes file was corrupt. JotTile restored notes from notes.bak.";
            return migrationMessage == null
                ? baseMessage + " Quarantined file: " + mainQuarantinePath
                : migrationMessage + Environment.NewLine + baseMessage + " Quarantined file: " + mainQuarantinePath;
        }

        private static string ComposeDataLossMessage(string? migrationMessage, string mainQuarantinePath, string? backupQuarantinePath)
        {
            string baseMessage = "Both the main notes file and backup were unreadable. JotTile started with an empty note list.";
            string detail = " Quarantined files: " + mainQuarantinePath;
            if (!string.IsNullOrWhiteSpace(backupQuarantinePath))
            {
                detail += ", " + backupQuarantinePath;
            }

            return migrationMessage == null
                ? baseMessage + detail
                : migrationMessage + Environment.NewLine + baseMessage + detail;
        }

        private static bool IsRecoverableReadException(Exception exception)
        {
            return exception is IOException || exception is SerializationException;
        }

        private static DataContractJsonSerializer CreateSerializer()
        {
            return new DataContractJsonSerializer(typeof(List<NoteRecord>));
        }
    }
}
