using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StickyNoteApp
{
    internal sealed class StickyNotesAppContext : ApplicationContext
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "SimpleStickyNotes";
        private readonly NotesRepository _repository;
        private readonly List<NoteRecord> _notes;
        private readonly Dictionary<string, StickyNoteForm> _formsById;
        private readonly bool _restoreOnlyLaunch;
        private readonly Control _uiInvoker;
        private readonly EventWaitHandle _createNoteEvent;
        private Thread _watchThread;
        private bool _isShuttingDown;

        public StickyNotesAppContext(bool restoreOnlyLaunch)
        {
            _restoreOnlyLaunch = restoreOnlyLaunch;
            _repository = new NotesRepository();
            _notes = _repository.Load();
            _formsById = new Dictionary<string, StickyNoteForm>();
            _uiInvoker = new Control();
            _uiInvoker.CreateControl();
            _createNoteEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.CreateNoteEventName);

            EnsureAutostart();
            OpenExistingNotes();
            EnsureDraftNoteIfNeeded();
            StartExternalLaunchWatcher();

            if (_formsById.Count == 0)
            {
                Application.Idle += HandleInitialIdleExit;
            }
        }

        protected override void ExitThreadCore()
        {
            if (_isShuttingDown)
            {
                base.ExitThreadCore();
                return;
            }

            PersistAllNotes();
            _isShuttingDown = true;

            foreach (StickyNoteForm form in _formsById.Values.ToList())
            {
                form.PrepareForApplicationExit();
                form.Close();
            }

            if (_watchThread != null)
            {
                try
                {
                    _createNoteEvent.Set();
                    _watchThread.Join(1000);
                }
                catch
                {
                }
            }

            _createNoteEvent.Dispose();
            _uiInvoker.Dispose();

            base.ExitThreadCore();
        }

        private void OpenExistingNotes()
        {
            foreach (NoteRecord note in _notes.ToList())
            {
                NormalizeNote(note);
                OpenNote(note);
            }
        }

        private void EnsureDraftNoteIfNeeded()
        {
            if (_restoreOnlyLaunch)
            {
                return;
            }

            if (_notes.Any(note => !note.IsFinalized))
            {
                return;
            }

            NoteRecord noteRecord = CreateNewDraftNote();
            _notes.Add(noteRecord);
            OpenNote(noteRecord);
            PersistAllNotes();
        }

        private NoteRecord CreateNewDraftNote()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int offset = _notes.Count * 24;
            int width = 260;
            int height = 140;
            int x = Math.Max(workingArea.Left + 20, workingArea.Left + 80 + offset);
            int y = Math.Max(workingArea.Top + 20, workingArea.Top + 80 + offset);
            string timestamp = DateTime.UtcNow.ToString("o");

            return new NoteRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = string.Empty,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
                IsFinalized = false
            };
        }

        private void OpenNote(NoteRecord note)
        {
            StickyNoteForm form = new StickyNoteForm(note);
            form.PersistRequested += HandlePersistRequested;
            form.DeleteRequested += HandleDeleteRequested;
            form.Finalized += HandlePersistRequested;
            form.FormClosed += HandleFormClosed;

            _formsById[note.Id] = form;
            form.Show();

            if (!note.IsFinalized)
            {
                form.FocusDraftInput();
            }
        }

        private void HandlePersistRequested(object sender, EventArgs e)
        {
            PersistAllNotes();
        }

        private void HandleDeleteRequested(object sender, EventArgs e)
        {
            StickyNoteForm form = sender as StickyNoteForm;
            if (form == null)
            {
                return;
            }

            form.PrepareForApplicationExit();
            _formsById.Remove(form.Note.Id);
            _notes.RemoveAll(note => note.Id == form.Note.Id);
            PersistAllNotes();
            form.Close();

            if (_formsById.Count == 0)
            {
                ExitThread();
            }
        }

        private void HandleFormClosed(object sender, FormClosedEventArgs e)
        {
            StickyNoteForm form = sender as StickyNoteForm;
            if (form == null)
            {
                return;
            }

            form.PersistRequested -= HandlePersistRequested;
            form.DeleteRequested -= HandleDeleteRequested;
            form.Finalized -= HandlePersistRequested;
            form.FormClosed -= HandleFormClosed;
            form.Dispose();
        }

        private void PersistAllNotes()
        {
            if (_isShuttingDown)
            {
                return;
            }

            try
            {
                _repository.Save(_notes);
            }
            catch
            {
            }
        }

        private void NormalizeNote(NoteRecord note)
        {
            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.Id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(note.CreatedAt))
            {
                note.CreatedAt = DateTime.UtcNow.ToString("o");
            }

            if (string.IsNullOrWhiteSpace(note.UpdatedAt))
            {
                note.UpdatedAt = note.CreatedAt;
            }

            if (note.Width < 160)
            {
                note.Width = 160;
            }

            if (note.Height < 90)
            {
                note.Height = 90;
            }

            if (note.X < -2000 || note.X > 20000)
            {
                note.X = 80;
            }

            if (note.Y < -2000 || note.Y > 20000)
            {
                note.Y = 80;
            }

            if (note.Text == null)
            {
                note.Text = string.Empty;
            }
        }

        private void EnsureAutostart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    string expectedValue = "\"" + Application.ExecutablePath + "\" --restore-only";
                    object currentValue = key.GetValue(RunValueName);
                    string currentText = currentValue as string;

                    if (!string.Equals(currentText, expectedValue, StringComparison.Ordinal))
                    {
                        key.SetValue(RunValueName, expectedValue);
                    }
                }
            }
            catch
            {
            }
        }

        private void StartExternalLaunchWatcher()
        {
            _watchThread = new Thread(WatchExternalLaunches);
            _watchThread.IsBackground = true;
            _watchThread.Start();
        }

        private void HandleInitialIdleExit(object sender, EventArgs e)
        {
            Application.Idle -= HandleInitialIdleExit;
            ExitThread();
        }

        private void WatchExternalLaunches()
        {
            while (!_isShuttingDown)
            {
                _createNoteEvent.WaitOne();

                if (_isShuttingDown)
                {
                    return;
                }

                try
                {
                    _uiInvoker.BeginInvoke((MethodInvoker)HandleExternalLaunch);
                }
                catch
                {
                    return;
                }
            }
        }

        private void HandleExternalLaunch()
        {
            StickyNoteForm draft = _formsById.Values.FirstOrDefault(form => !form.Note.IsFinalized);

            if (draft != null)
            {
                draft.FocusDraftInput();
                return;
            }

            NoteRecord noteRecord = CreateNewDraftNote();
            _notes.Add(noteRecord);
            OpenNote(noteRecord);
            PersistAllNotes();
        }
    }
}
