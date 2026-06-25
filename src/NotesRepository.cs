using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace StickyNoteApp
{
    internal sealed class NotesRepository
    {
        private readonly string _directoryPath;
        private readonly string _notesFilePath;

        public NotesRepository()
        {
            _directoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SimpleStickyNotes");
            _notesFilePath = Path.Combine(_directoryPath, "notes.json");
        }

        public string NotesFilePath
        {
            get { return _notesFilePath; }
        }

        public List<NoteRecord> Load()
        {
            if (!File.Exists(_notesFilePath))
            {
                return new List<NoteRecord>();
            }

            try
            {
                using (FileStream stream = File.OpenRead(_notesFilePath))
                {
                    DataContractJsonSerializer serializer = CreateSerializer();
                    List<NoteRecord> notes = serializer.ReadObject(stream) as List<NoteRecord>;
                    return notes ?? new List<NoteRecord>();
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SerializationException)
                {
                    TryQuarantineCorruptFile();
                    return new List<NoteRecord>();
                }

                throw;
            }
        }

        public void Save(IList<NoteRecord> notes)
        {
            Directory.CreateDirectory(_directoryPath);

            string tempFilePath = Path.Combine(_directoryPath, "notes.tmp");
            string backupFilePath = Path.Combine(_directoryPath, "notes.bak");

            using (FileStream stream = File.Create(tempFilePath))
            {
                DataContractJsonSerializer serializer = CreateSerializer();
                serializer.WriteObject(stream, notes);
                stream.Flush(true);
            }

            if (File.Exists(_notesFilePath))
            {
                File.Replace(tempFilePath, _notesFilePath, backupFilePath, true);

                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
            }
            else
            {
                File.Move(tempFilePath, _notesFilePath);
            }
        }

        private static DataContractJsonSerializer CreateSerializer()
        {
            return new DataContractJsonSerializer(typeof(List<NoteRecord>));
        }

        private void TryQuarantineCorruptFile()
        {
            try
            {
                string corruptFilePath = Path.Combine(
                    _directoryPath,
                    "notes.corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json");

                File.Move(_notesFilePath, corruptFilePath);
            }
            catch
            {
            }
        }
    }
}
