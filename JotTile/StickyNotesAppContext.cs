using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using JotTile.Core;
using Microsoft.Win32;

namespace JotTile
{
    internal sealed class StickyNotesAppContext : ApplicationContext
    {
        private readonly NotesRepository _notesRepository;
        private readonly SettingsRepository _settingsRepository;
        private readonly NoteLayoutCalculator _layoutCalculator;
        private readonly AppLogger _logger;
        private readonly List<NoteRecord> _notes;
        private readonly Dictionary<string, StickyNoteForm> _formsById;
        private readonly bool _restoreOnlyLaunch;
        private readonly Control _uiInvoker;
        private readonly NotifyIcon _notifyIcon;
        private readonly EventWaitHandle _createNoteEvent;
        private readonly EventWaitHandle _settingsChangedEvent;
        private Thread? _launchWatchThread;
        private Thread? _settingsWatchThread;
        private bool _isShuttingDown;
        private AppSettings _settings;

        internal StickyNotesAppContext(bool restoreOnlyLaunch)
        {
            _restoreOnlyLaunch = restoreOnlyLaunch;
            _logger = new AppLogger();
            _layoutCalculator = new NoteLayoutCalculator();
            _notesRepository = new NotesRepository();
            _settingsRepository = new SettingsRepository();
            _formsById = new Dictionary<string, StickyNoteForm>();
            _uiInvoker = new Control();
            _uiInvoker.CreateControl();
            _createNoteEvent = new EventWaitHandle(false, EventResetMode.AutoReset, AppIdentity.CreateNoteEventName);
            _settingsChangedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, AppIdentity.SettingsChangedEventName);

            RepositoryLoadResult<AppSettings> settingsLoad = _settingsRepository.Load();
            _settings = settingsLoad.Value;
            ShowLoadMessageIfNeeded(settingsLoad.UserMessage);

            RepositoryLoadResult<List<NoteRecord>> notesLoad = _notesRepository.Load();
            _notes = notesLoad.Value;
            ShowLoadMessageIfNeeded(notesLoad.UserMessage);

            _notifyIcon = CreateNotifyIcon();

            EnsureAutostart();
            OpenExistingNotes();
            EnsureDraftNoteIfNeeded();
            StartWatchers();
        }

        protected override void ExitThreadCore()
        {
            if (_isShuttingDown)
            {
                base.ExitThreadCore();
                return;
            }

            if (!CanExitApplication())
            {
                return;
            }

            _isShuttingDown = true;

            try
            {
                _notifyIcon.Visible = false;
                _createNoteEvent.Set();
                _settingsChangedEvent.Set();

                foreach (StickyNoteForm form in _formsById.Values.ToList())
                {
                    form.PrepareForApplicationExit();
                    form.Close();
                    form.Dispose();
                }

                _formsById.Clear();
            }
            finally
            {
                JoinThread(_launchWatchThread);
                JoinThread(_settingsWatchThread);
                _createNoteEvent.Dispose();
                _settingsChangedEvent.Dispose();
                _notifyIcon.Dispose();
                _uiInvoker.Dispose();
            }

            base.ExitThreadCore();
        }

        private NotifyIcon CreateNotifyIcon()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("New note", null, HandleNewNoteMenuClick);
            menu.Items.Add("Show notes", null, HandleShowNotesMenuClick);
            menu.Items.Add("Minimize notes", null, HandleMinimizeNotesMenuClick);
            menu.Items.Add("Settings", null, HandleSettingsMenuClick);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, HandleExitMenuClick);

            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = LoadApplicationIcon();
            notifyIcon.Text = AppIdentity.ProductName;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.DoubleClick += HandleNewNoteMenuClick;
            return notifyIcon;
        }

        private static Icon LoadApplicationIcon()
        {
            try
            {
                Icon? extractedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (extractedIcon != null)
                {
                    return extractedIcon;
                }
            }
            catch
            {
            }

            return SystemIcons.Information;
        }

        private void HandleNewNoteMenuClick(object? sender, EventArgs e)
        {
            HandleExternalLaunch();
        }

        private void HandleShowNotesMenuClick(object? sender, EventArgs e)
        {
            ShowNotes();
        }

        private void HandleExitMenuClick(object? sender, EventArgs e)
        {
            ExitThread();
        }

        private void HandleMinimizeNotesMenuClick(object? sender, EventArgs e)
        {
            MinimizeAllNotes();
        }

        private void HandleSettingsMenuClick(object? sender, EventArgs e)
        {
            OpenSettings();
        }

        private void OpenExistingNotes()
        {
            foreach (NoteRecord note in _notes.ToList())
            {
                NormalizeNote(note);
                OpenNote(note, false);
            }
        }

        private void EnsureDraftNoteIfNeeded()
        {
            if (_restoreOnlyLaunch)
            {
                return;
            }

            if (_notes.Any(note => !note.IsSaved))
            {
                return;
            }

            NoteRecord note = CreateNewDraftNote();
            _notes.Add(note);
            OpenNote(note, false);
            TryPersistNotesList("create-draft");
        }

        private void StartWatchers()
        {
            _launchWatchThread = new Thread(WatchExternalLaunches);
            _launchWatchThread.IsBackground = true;
            _launchWatchThread.Start();

            _settingsWatchThread = new Thread(WatchSettingsChanges);
            _settingsWatchThread.IsBackground = true;
            _settingsWatchThread.Start();
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

        private void WatchSettingsChanges()
        {
            while (!_isShuttingDown)
            {
                _settingsChangedEvent.WaitOne();
                if (_isShuttingDown)
                {
                    return;
                }

                try
                {
                    _uiInvoker.BeginInvoke((MethodInvoker)ReloadSettings);
                }
                catch
                {
                    return;
                }
            }
        }

        private void ReloadSettings()
        {
            RepositoryLoadResult<AppSettings> result = _settingsRepository.Load();
            _settings = result.Value;
            EnsureAutostart();
            ShowLoadMessageIfNeeded(result.UserMessage);

            foreach (StickyNoteForm form in _formsById.Values.ToList())
            {
                form.ApplySettings(_settings, true);
            }
        }

        private void HandleExternalLaunch()
        {
            StickyNoteForm? draft = _formsById.Values.FirstOrDefault(form => form.Note.IsSaved == false);
            if (draft != null)
            {
                draft.FocusEditingSurface(true);
                return;
            }

            NoteRecord note = CreateNewDraftNote();
            _notes.Add(note);
            OpenNote(note, true);
            TryPersistNotesList("external-create-note");
        }

        private void ShowNotes()
        {
            if (_formsById.Count == 0 && _notes.Count == 0)
            {
                HandleExternalLaunch();
                return;
            }

            foreach (NoteRecord note in _notes.ToList())
            {
                if (_formsById.TryGetValue(note.Id, out StickyNoteForm? form))
                {
                    form.EnsureVisibleFromTray();
                }
                else
                {
                    OpenNote(note, false);
                }
            }

            StickyNoteForm? focusTarget = _formsById.Values.FirstOrDefault();
            if (focusTarget != null)
            {
                focusTarget.EnsureVisibleFromTray();
            }
        }

        private void MinimizeAllNotes()
        {
            foreach (StickyNoteForm form in _formsById.Values.ToList())
            {
                form.HideToTray();
            }
        }

        private void OpenSettings()
        {
            string appDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty;
            string configPath = Path.Combine(appDirectory, AppIdentity.ConfigExecutableName);

            if (!File.Exists(configPath))
            {
                _logger.Warning("open-settings", "Settings executable was not found.", null);
                MessageBox.Show(
                    "JotTile could not open Settings because config.exe was not found next to JotTile.exe.",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(configPath);
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = appDirectory;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("open-settings", "Launching config.exe failed.", ex);
                MessageBox.Show(
                    "JotTile could not open Settings.",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OpenNote(NoteRecord note, bool requestForeground)
        {
            StickyNoteForm form = new StickyNoteForm(
                note,
                _settings,
                _layoutCalculator,
                _logger,
                CommitNote,
                PersistNoteBounds,
                HandleNoteCloseRequested);

            form.FormClosed += HandleFormClosed;
            _formsById[note.Id] = form;
            form.Show();

            if (requestForeground || !note.IsSaved)
            {
                form.FocusEditingSurface(requestForeground);
            }
        }

        private void HandleFormClosed(object? sender, FormClosedEventArgs e)
        {
            StickyNoteForm? form = sender as StickyNoteForm;
            if (form == null)
            {
                return;
            }

            form.FormClosed -= HandleFormClosed;
        }

        private NoteCommitResult CommitNote(NoteRecord note, NoteCommitRequest request)
        {
            NoteRecord snapshot = note.Clone();

            try
            {
                note.Text = request.Text;
                note.IsSaved = true;
                note.X = request.Bounds.X;
                note.Y = request.Bounds.Y;
                note.Width = request.Bounds.Width;
                note.Height = request.Bounds.Height;
                note.UpdatedAt = DateTime.UtcNow.ToString("o");

                _notesRepository.Save(_notes);
                return NoteCommitResult.Success();
            }
            catch (Exception ex)
            {
                RestoreNote(note, snapshot);
                _logger.Error("commit-note", "A note save operation failed.", ex);
                return NoteCommitResult.Failure("JotTile could not save this note. The editor stayed open and no text was committed.");
            }
        }

        private bool PersistNoteBounds(NoteRecord note, Rectangle bounds)
        {
            NoteRecord snapshot = note.Clone();

            try
            {
                note.X = bounds.X;
                note.Y = bounds.Y;
                note.Width = bounds.Width;
                note.Height = bounds.Height;
                note.UpdatedAt = DateTime.UtcNow.ToString("o");
                _notesRepository.Save(_notes);
                return true;
            }
            catch (Exception ex)
            {
                RestoreNote(note, snapshot);
                _logger.Warning("persist-bounds", "Persisting note bounds failed.", ex);
                return false;
            }
        }

        private void HandleNoteCloseRequested(StickyNoteForm form)
        {
            if (_settings.CloseAction == NoteCloseAction.Hide)
            {
                form.HideToTray();
                return;
            }

            bool requiresConfirmation = _settings.DeleteRequiresConfirmation && (form.HasSavedContent || form.HasUnsavedChanges);
            if (!form.IsTrulyEmptyDraft && requiresConfirmation)
            {
                DialogResult result = MessageBox.Show(
                    "Delete this note from the saved notes list?",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            int noteIndex = _notes.FindIndex(candidate => string.Equals(candidate.Id, form.Note.Id, StringComparison.Ordinal));
            if (noteIndex < 0)
            {
                return;
            }

            NoteRecord removedNote = _notes[noteIndex];
            _notes.RemoveAt(noteIndex);

            try
            {
                _notesRepository.Save(_notes);
            }
            catch (Exception ex)
            {
                _notes.Insert(noteIndex, removedNote);
                _logger.Error("delete-note", "Deleting a note failed during persistence.", ex);
                MessageBox.Show(
                    "JotTile could not delete this note because the note list could not be saved.",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _formsById.Remove(form.Note.Id);
            form.PrepareForApplicationExit();
            form.Close();
            form.Dispose();
        }

        private bool CanExitApplication()
        {
            bool hasUnsaved = _formsById.Values.Any(form => form.HasUnsavedChanges);

            if (hasUnsaved && (_settings.ExitRequiresConfirmation || _settings.ExitUnsavedAction == ExitUnsavedAction.ConfirmDiscard))
            {
                DialogResult result = MessageBox.Show(
                    "Exit JotTile and discard unsaved changes?",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                return result == DialogResult.Yes;
            }

            if (!hasUnsaved && _settings.ExitRequiresConfirmation)
            {
                DialogResult result = MessageBox.Show(
                    "Exit JotTile?",
                    AppIdentity.DialogTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                return result == DialogResult.Yes;
            }

            return true;
        }

        private void EnsureAutostart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(AppIdentity.RunKeyPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    key.DeleteValue(AppIdentity.LegacyRunValueName, false);

                    if (_settings.LaunchAtSignIn)
                    {
                        string expectedValue = "\"" + Application.ExecutablePath + "\" --restore-only";
                        object? currentValue = key.GetValue(AppIdentity.RunValueName);
                        if (!string.Equals(currentValue as string, expectedValue, StringComparison.Ordinal))
                        {
                            key.SetValue(AppIdentity.RunValueName, expectedValue);
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppIdentity.RunValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("autostart", "Autostart registration could not be updated.", ex);
            }
        }

        private bool TryPersistNotesList(string operation)
        {
            try
            {
                _notesRepository.Save(_notes);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(operation, "Persisting the note list failed.", ex);
                return false;
            }
        }

        private NoteRecord CreateNewDraftNote()
        {
            Rectangle workingArea = Screen.PrimaryScreen != null
                ? Screen.PrimaryScreen.WorkingArea
                : Screen.AllScreens[0].WorkingArea;
            int offset = _notes.Count * 24;
            int width = 260;
            int height = 160;
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
                IsSaved = false
            };
        }

        private static void RestoreNote(NoteRecord target, NoteRecord snapshot)
        {
            target.Text = snapshot.Text;
            target.IsSaved = snapshot.IsSaved;
            target.X = snapshot.X;
            target.Y = snapshot.Y;
            target.Width = snapshot.Width;
            target.Height = snapshot.Height;
            target.CreatedAt = snapshot.CreatedAt;
            target.UpdatedAt = snapshot.UpdatedAt;
        }

        private static void NormalizeNote(NoteRecord note)
        {
            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.Id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(note.Text))
            {
                note.Text = string.Empty;
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
        }

        private static void JoinThread(Thread? thread)
        {
            if (thread == null)
            {
                return;
            }

            try
            {
                thread.Join(1000);
            }
            catch
            {
            }
        }

        private static void ShowLoadMessageIfNeeded(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageBox.Show(message, AppIdentity.DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
