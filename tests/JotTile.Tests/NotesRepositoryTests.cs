using System.Collections.Generic;
using System.IO;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class NotesRepositoryTests
    {
        [Fact]
        public void SaveAndLoadPreservesWhitespaceUnicodeAndMultipleNotes()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                string legacyDir = workspace.CreateSubdirectory("legacy");
                NotesRepository repository = new NotesRepository(dataDir, legacyDir, workspace.CreateLogger());

                List<NoteRecord> notes = new List<NoteRecord>
                {
                    CreateNote("  hello\r\nworld  ", true),
                    CreateNote("äöü 😀", false)
                };

                repository.Save(notes);
                RepositoryLoadResult<List<NoteRecord>> result = repository.Load();

                Assert.Equal(2, result.Value.Count);
                Assert.Equal("  hello\r\nworld  ", result.Value[0].Text);
                Assert.Equal("äöü 😀", result.Value[1].Text);
            }
        }

        [Fact]
        public void SecondSaveKeepsBackupFile()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                NotesRepository repository = new NotesRepository(dataDir, workspace.CreateSubdirectory("legacy"), workspace.CreateLogger());

                repository.Save(new List<NoteRecord> { CreateNote("one", true) });
                repository.Save(new List<NoteRecord> { CreateNote("two", true) });

                Assert.True(File.Exists(Path.Combine(dataDir, AppIdentity.NotesBackupFileName)));
            }
        }

        [Fact]
        public void CorruptPrimaryFileFallsBackToBackup()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                NotesRepository repository = new NotesRepository(dataDir, workspace.CreateSubdirectory("legacy"), workspace.CreateLogger());
                repository.Save(new List<NoteRecord> { CreateNote("healthy", true) });
                repository.Save(new List<NoteRecord> { CreateNote("current", true) });

                File.WriteAllText(Path.Combine(dataDir, AppIdentity.NotesFileName), "{bad json");

                RepositoryLoadResult<List<NoteRecord>> result = repository.Load();

                Assert.Single(result.Value);
                Assert.Equal("healthy", result.Value[0].Text);
                Assert.Contains("restored notes from notes.bak", result.UserMessage);
            }
        }

        [Fact]
        public void CorruptPrimaryAndBackupYieldEmptyList()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                NotesRepository repository = new NotesRepository(dataDir, workspace.CreateSubdirectory("legacy"), workspace.CreateLogger());
                repository.Save(new List<NoteRecord> { CreateNote("healthy", true) });
                repository.Save(new List<NoteRecord> { CreateNote("current", true) });

                File.WriteAllText(Path.Combine(dataDir, AppIdentity.NotesFileName), "{bad json");
                File.WriteAllText(Path.Combine(dataDir, AppIdentity.NotesBackupFileName), "{bad backup");

                RepositoryLoadResult<List<NoteRecord>> result = repository.Load();

                Assert.Empty(result.Value);
                Assert.Contains("empty note list", result.UserMessage);
            }
        }

        [Fact]
        public void MigratesLegacyNotesWhenNewFileDoesNotExist()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                string legacyDir = workspace.CreateSubdirectory("legacy");
                NotesRepository legacyWriter = new NotesRepository(legacyDir, workspace.CreateSubdirectory("unused"), workspace.CreateLogger());
                legacyWriter.Save(new List<NoteRecord> { CreateNote("legacy", true) });

                NotesRepository repository = new NotesRepository(dataDir, legacyDir, workspace.CreateLogger());
                RepositoryLoadResult<List<NoteRecord>> result = repository.Load();

                Assert.Single(result.Value);
                Assert.Equal("legacy", result.Value[0].Text);
                Assert.True(File.Exists(Path.Combine(dataDir, AppIdentity.NotesFileName)));
                Assert.True(File.Exists(Path.Combine(legacyDir, AppIdentity.NotesFileName)));
            }
        }

        [Fact]
        public void ExistingNewFileWinsOverLegacyFile()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                string legacyDir = workspace.CreateSubdirectory("legacy");
                NotesRepository repository = new NotesRepository(dataDir, legacyDir, workspace.CreateLogger());
                repository.Save(new List<NoteRecord> { CreateNote("new", true) });

                NotesRepository legacyWriter = new NotesRepository(legacyDir, workspace.CreateSubdirectory("unused"), workspace.CreateLogger());
                legacyWriter.Save(new List<NoteRecord> { CreateNote("legacy", true) });

                RepositoryLoadResult<List<NoteRecord>> result = repository.Load();

                Assert.Single(result.Value);
                Assert.Equal("new", result.Value[0].Text);
            }
        }

        private static NoteRecord CreateNote(string text, bool isSaved)
        {
            return new NoteRecord
            {
                Id = System.Guid.NewGuid().ToString("N"),
                Text = text,
                X = 10,
                Y = 10,
                Width = 160,
                Height = 90,
                CreatedAt = "2026-01-01T00:00:00.0000000Z",
                UpdatedAt = "2026-01-01T00:00:00.0000000Z",
                IsSaved = isSaved
            };
        }
    }
}
