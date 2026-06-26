using System;
using System.Drawing;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal sealed partial class StickyNoteForm : Form
    {
        private readonly NoteInteractionController _interaction;
        private readonly NoteLayoutCalculator _layoutCalculator;
        private readonly AppLogger _logger;
        private readonly Func<NoteRecord, NoteCommitRequest, NoteCommitResult> _commitRequested;
        private readonly Func<NoteRecord, Rectangle, bool> _boundsPersistRequested;
        private readonly Action<StickyNoteForm> _closeRequested;
        private readonly Timer _boundsPersistTimer;
        private readonly Timer _copyFeedbackTimer;
        private readonly ToolTip _toolTip;
        private AppSettings _settings;
        private bool _allowClose;
        private bool _isInitializing;
        private bool _isApplyingSavedBounds;

        internal StickyNoteForm(
            NoteRecord note,
            AppSettings settings,
            NoteLayoutCalculator layoutCalculator,
            AppLogger logger,
            Func<NoteRecord, NoteCommitRequest, NoteCommitResult> commitRequested,
            Func<NoteRecord, Rectangle, bool> boundsPersistRequested,
            Action<StickyNoteForm> closeRequested)
        {
            Note = note;
            _settings = settings.Clone();
            _layoutCalculator = layoutCalculator;
            _logger = logger;
            _commitRequested = commitRequested;
            _boundsPersistRequested = boundsPersistRequested;
            _closeRequested = closeRequested;
            _interaction = new NoteInteractionController(note.Text, note.IsSaved);
            _boundsPersistTimer = new Timer();
            _copyFeedbackTimer = new Timer();
            _toolTip = new ToolTip();
            _isInitializing = true;

            InitializeUi();

            Bounds = new Rectangle(note.X, note.Y, note.Width, note.Height);
            ApplySettings(_settings, false);
            InitializeStateFromNote();

            _boundsPersistTimer.Interval = 250;
            _boundsPersistTimer.Tick += HandleBoundsPersistTimerTick;
            _copyFeedbackTimer.Interval = 1400;
            _copyFeedbackTimer.Tick += HandleCopyFeedbackTimerTick;

            Move += HandleBoundsChanged;
            Resize += HandleBoundsChanged;
            Activated += HandleActivated;
            FormClosing += HandleFormClosing;
            MouseDown += HandleDragMouseDown;

            _isInitializing = false;
        }

        internal NoteRecord Note { get; }

        internal bool HasUnsavedChanges
        {
            get { return _interaction.Mode == NoteInteractionMode.Editing && _interaction.IsDirty; }
        }

        internal bool HasSavedContent
        {
            get { return !string.IsNullOrEmpty(_interaction.CommittedText); }
        }

        internal bool IsTrulyEmptyDraft
        {
            get
            {
                return _interaction.Mode == NoteInteractionMode.Editing &&
                    string.IsNullOrEmpty(_interaction.CommittedText) &&
                    string.IsNullOrEmpty(_interaction.EditorText);
            }
        }

        internal string SavedText
        {
            get { return _interaction.CommittedText; }
        }

        internal RichTextBox SavedTextView
        {
            get { return _savedTextView; }
        }

        internal void FocusEditingSurface(bool requestForeground)
        {
            if (!Visible)
            {
                Show();
            }

            if (requestForeground)
            {
                NativeMethods.ShowWindow(Handle, NativeMethods.SwRestore);
                NativeMethods.SetForegroundWindow(Handle);
            }
            else
            {
                Activate();
            }

            if (_interaction.Mode == NoteInteractionMode.Editing)
            {
                QueueEditorFocus();
            }
        }

        internal void EnsureVisibleFromTray()
        {
            if (!Visible)
            {
                Show();
            }

            EnsureVisibleOnCurrentScreens();
            NativeMethods.ShowWindow(Handle, NativeMethods.SwRestore);
            Activate();
            NativeMethods.SetForegroundWindow(Handle);

            if (_interaction.Mode == NoteInteractionMode.Editing)
            {
                QueueEditorFocus();
            }
        }

        internal void HideToTray()
        {
            _copyFeedbackTimer.Stop();
            _copyFeedbackLabel.Visible = false;
            Hide();
        }

        internal void PrepareForApplicationExit()
        {
            _allowClose = true;
            _boundsPersistTimer.Stop();
            _copyFeedbackTimer.Stop();
        }

        internal void EnsureVisibleOnCurrentScreens()
        {
            Rectangle correctedBounds = NoteWindowPlacement.ClampToVisibleArea(
                Bounds,
                NoteWindowPlacement.GetPrimaryWorkingArea(),
                NoteWindowPlacement.GetWorkingAreas());

            if (correctedBounds == Bounds)
            {
                return;
            }

            ApplySavedBounds(correctedBounds);
            PersistBounds();
        }

        internal void ApplySettings(AppSettings settings, bool relayoutSavedNote)
        {
            _settings = settings.Clone();
            ApplyPaletteToControls();

            if (relayoutSavedNote && _interaction.Mode == NoteInteractionMode.Saved)
            {
                ApplySavedBounds(CalculateTargetBounds(_interaction.CommittedText));
                PersistBounds();
            }
            else
            {
                Invalidate();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle = BuildShadowNoteExtendedStyle(createParams.ExStyle);
                return createParams;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_interaction.Mode == NoteInteractionMode.Editing && keyData == (Keys.Control | Keys.S))
            {
                ExecuteSaveCommand();
                return true;
            }

            if (_interaction.Mode == NoteInteractionMode.Saved && keyData == Keys.F2)
            {
                ExecuteEditCommand();
                return true;
            }

            if (_interaction.Mode == NoteInteractionMode.Saved && keyData == (Keys.Control | Keys.C))
            {
                ExecuteCopyCommand();
                return true;
            }

            if (keyData == (Keys.Control | Keys.W))
            {
                ExecuteCloseCommand();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        internal static int BuildShadowNoteExtendedStyle(int exStyle)
        {
            return (exStyle | NativeMethods.WsExToolWindow) & ~NativeMethods.WsExAppWindow;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeMethods.WmNcHitTest)
            {
                base.WndProc(ref message);

                if ((int)message.Result == NativeMethods.HtClient)
                {
                    Point cursor = PointToClient(Cursor.Position);
                    message.Result = (IntPtr)GetResizeHandle(cursor);
                }

                return;
            }

            base.WndProc(ref message);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            PaintNoteBackground(e.Graphics);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_noteFont != null)
                {
                    _noteFont.Dispose();
                    _noteFont = null;
                }
            }

            base.Dispose(disposing);
        }

        private void InitializeStateFromNote()
        {
            _inputBox.Text = _interaction.EditorText;
            UpdateSavedTextPresentation();
            ApplyInteractionUi();

            if (_interaction.Mode == NoteInteractionMode.Editing)
            {
                QueueEditorFocus();
            }
        }

        private void HandleActivated(object? sender, EventArgs e)
        {
            if (_interaction.Mode == NoteInteractionMode.Editing)
            {
                QueueEditorFocus();
            }
        }

        private void HandleFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_allowClose)
            {
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                ExecuteCloseCommand();
                return;
            }

            if (e.CloseReason == CloseReason.ApplicationExitCall ||
                e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing)
            {
                _allowClose = true;
            }
        }

        private void HandleBoundsChanged(object? sender, EventArgs e)
        {
            if (_isInitializing || _isApplyingSavedBounds || WindowState != FormWindowState.Normal)
            {
                return;
            }

            if (_copyFeedbackLabel.Visible)
            {
                CenterCopyFeedbackLabel();
            }

            QueueBoundsPersist();
        }

        private void HandleBoundsPersistTimerTick(object? sender, EventArgs e)
        {
            _boundsPersistTimer.Stop();
            PersistBounds();
        }

        private void HandleCopyFeedbackTimerTick(object? sender, EventArgs e)
        {
            _copyFeedbackTimer.Stop();
            _copyFeedbackLabel.Visible = false;
        }

        private void HandleEditorTextChanged(object? sender, EventArgs e)
        {
            if (_isInitializing)
            {
                return;
            }

            _interaction.UpdateEditorText(_inputBox.Text);
        }

        private void HandleEditSaveClicked(object? sender, EventArgs e)
        {
            if (_interaction.Mode == NoteInteractionMode.Editing)
            {
                ExecuteSaveCommand();
            }
            else
            {
                ExecuteEditCommand();
            }
        }

        private void HandleCopyClicked(object? sender, EventArgs e)
        {
            ExecuteCopyCommand();
        }

        private void HandleCloseClicked(object? sender, EventArgs e)
        {
            ExecuteCloseCommand();
        }

        private void ExecuteSaveCommand()
        {
            if (_interaction.Mode != NoteInteractionMode.Editing)
            {
                return;
            }

            Rectangle targetBounds = CalculateTargetBounds(_inputBox.Text);
            NoteCommitResult result = _interaction.Save(
                delegate(NoteCommitRequest request)
                {
                    return _commitRequested(Note, request);
                },
                targetBounds);

            if (!result.Succeeded)
            {
                string message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "JotTile could not save this note."
                    : result.ErrorMessage;
                MessageBox.Show(this, message, AppIdentity.DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateSavedTextPresentation();
            ApplySavedBounds(targetBounds);
            ApplyInteractionUi();
        }

        private void ExecuteEditCommand()
        {
            if (_interaction.Mode != NoteInteractionMode.Saved)
            {
                return;
            }

            _interaction.BeginEdit();
            _inputBox.Text = _interaction.EditorText;
            ApplyInteractionUi();
            QueueEditorFocus();
        }

        private void ExecuteCopyCommand()
        {
            if (_interaction.Mode != NoteInteractionMode.Saved)
            {
                return;
            }

            try
            {
                Clipboard.SetText(_interaction.CommittedText);
                ShowCopyFeedback("Copied");
            }
            catch (Exception ex)
            {
                _logger.Warning("copy-note", "Copy to clipboard failed.", ex);
                ShowCopyFeedback("Copy failed");
            }
        }

        private void ExecuteCloseCommand()
        {
            _closeRequested(this);
        }

        private void ApplySavedBounds(Rectangle targetBounds)
        {
            _isApplyingSavedBounds = true;

            try
            {
                Bounds = targetBounds;
            }
            finally
            {
                _isApplyingSavedBounds = false;
            }
        }

        private Rectangle CalculateTargetBounds(string text)
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            using (Font noteFont = CreateNoteFont())
            {
                return _layoutCalculator.CalculateBounds(text, Bounds, noteFont, workingArea, CreateLayoutMetrics());
            }
        }

        private NoteLayoutMetrics CreateLayoutMetrics()
        {
            NoteSurfaceLayoutMetrics surfaceMetrics = CreateSurfaceLayoutMetrics();
            return new NoteLayoutMetrics
            {
                MinimumWindowSize = MinimumSize,
                MaximumWindowSize = new Size(640, 480),
                ScreenMargin = 16,
                HorizontalChrome = surfaceMetrics.GetHorizontalChrome(_settings),
                VerticalChrome = surfaceMetrics.GetVerticalChrome(_settings)
            };
        }

        private void QueueBoundsPersist()
        {
            _boundsPersistTimer.Stop();
            _boundsPersistTimer.Start();
        }

        private void PersistBounds()
        {
            if (WindowState != FormWindowState.Normal)
            {
                return;
            }

            if (!_boundsPersistRequested(Note, Bounds))
            {
                ApplySavedBounds(new Rectangle(Note.X, Note.Y, Note.Width, Note.Height));
            }
        }

        private void ApplyInteractionUi()
        {
            bool isEditing = _interaction.Mode == NoteInteractionMode.Editing;

            _inputBox.Visible = isEditing;
            _savedTextView.Visible = !isEditing;
            UpdateSavedTextPresentation();
            _copyButton.CommandEnabled = !isEditing;
            _editSaveButton.Glyph = isEditing ? NoteButtonGlyph.Save : NoteButtonGlyph.Edit;

            _editSaveButton.AccessibleName = isEditing ? "Save note" : "Edit note";
            _editSaveButton.AccessibleDescription = isEditing ? "Save note, Ctrl+S" : "Edit note, F2";
            _copyButton.AccessibleName = "Copy note";
            _copyButton.AccessibleDescription = "Copy note, Ctrl+C";
            _closeButton.AccessibleName = "Close note";
            _closeButton.AccessibleDescription = "Close note, Ctrl+W";

            _toolTip.SetToolTip(_editSaveButton, isEditing ? "Save (Ctrl+S)" : "Edit (F2)");
            _toolTip.SetToolTip(_copyButton, "Copy (Ctrl+C)");
            _toolTip.SetToolTip(_closeButton, "Close (Ctrl+W)");

            _editSaveButton.Invalidate();
            _copyButton.Invalidate();
            _closeButton.Invalidate();
        }

        private void UpdateSavedTextPresentation()
        {
            string displayText = PrepareSavedDisplayText(_interaction.CommittedText);
            if (!string.Equals(_savedTextView.Text, displayText, StringComparison.Ordinal))
            {
                _savedTextView.Text = displayText;
            }

            UpdateSavedTextScrollState();
        }

        private void UpdateSavedTextScrollState()
        {
            if (_noteFont == null || _savedTextView.IsDisposed)
            {
                return;
            }

            int viewportWidth = Math.Max(1, _savedTextView.ClientSize.Width);
            int viewportHeight = Math.Max(1, _savedTextView.ClientSize.Height);
            int textHeight = _layoutCalculator.MeasureDisplayTextHeight(_savedTextView.Text, _noteFont, viewportWidth);
            RichTextBoxScrollBars targetScrollBars = textHeight > viewportHeight
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;

            if (_savedTextView.ScrollBars != targetScrollBars)
            {
                _savedTextView.ScrollBars = targetScrollBars;
            }
        }

        private void QueueEditorFocus()
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke((MethodInvoker)delegate
            {
                if (IsDisposed)
                {
                    return;
                }

                _inputBox.Focus();
                _inputBox.SelectionStart = _inputBox.TextLength;
                _inputBox.SelectionLength = 0;
            });
        }

        private void ShowCopyFeedback(string text)
        {
            _copyFeedbackLabel.Text = text;
            CenterCopyFeedbackLabel();
            _copyFeedbackLabel.Visible = true;
            _copyFeedbackLabel.BringToFront();
            _copyFeedbackTimer.Stop();
            _copyFeedbackTimer.Start();
        }

        private void CenterCopyFeedbackLabel()
        {
            Size preferredSize = _copyFeedbackLabel.GetPreferredSize(Size.Empty);
            _copyFeedbackLabel.Size = preferredSize;
            _copyFeedbackLabel.Location = new Point(
                Math.Max(0, (ClientSize.Width - preferredSize.Width) / 2),
                Math.Max(0, (ClientSize.Height - preferredSize.Height) / 2));
        }

        private static string PrepareSavedDisplayText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.EndsWith("\r\n", StringComparison.Ordinal) ||
                text.EndsWith("\n", StringComparison.Ordinal) ||
                text.EndsWith("\r", StringComparison.Ordinal))
            {
                return text + " ";
            }

            return text;
        }
    }
}
