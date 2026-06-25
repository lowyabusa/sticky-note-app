using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    internal sealed class StickyNoteForm : Form
    {
        private readonly Button _copyButton;
        private readonly Button _deleteButton;
        private readonly TextBox _inputBox;
        private readonly Label _displayLabel;
        private readonly Label _copyFeedbackLabel;
        private readonly Timer _saveTimer;
        private readonly Timer _copyFeedbackTimer;
        private bool _allowClose;
        private bool _initializing;

        public StickyNoteForm(NoteRecord note)
        {
            Note = note;
            _initializing = true;

            BackColor = Color.FromArgb(255, 247, 171);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(160, 90);
            TopMost = false;
            Font = new Font("Segoe UI", 10.0f, FontStyle.Regular, GraphicsUnit.Point);
            Padding = new Padding(10, 34, 10, 10);

            Bounds = new Rectangle(note.X, note.Y, note.Width, note.Height);

            _copyButton = new Button();
            _copyButton.Size = new Size(24, 24);
            _copyButton.Location = new Point(Width - 60, 8);
            _copyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _copyButton.FlatStyle = FlatStyle.Flat;
            _copyButton.FlatAppearance.BorderSize = 0;
            _copyButton.BackColor = Color.FromArgb(255, 228, 109);
            _copyButton.TabStop = false;
            _copyButton.UseVisualStyleBackColor = false;
            _copyButton.Click += HandleCopyClicked;
            _copyButton.Paint += HandleCopyButtonPaint;

            _deleteButton = new Button();
            _deleteButton.Text = "X";
            _deleteButton.Size = new Size(24, 24);
            _deleteButton.Location = new Point(Width - 32, 8);
            _deleteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _deleteButton.FlatStyle = FlatStyle.Flat;
            _deleteButton.FlatAppearance.BorderSize = 0;
            _deleteButton.BackColor = Color.FromArgb(255, 228, 109);
            _deleteButton.TabStop = false;
            _deleteButton.Click += HandleDeleteClicked;

            _inputBox = new TextBox();
            _inputBox.BorderStyle = BorderStyle.None;
            _inputBox.Dock = DockStyle.Fill;
            _inputBox.Multiline = false;
            _inputBox.Text = note.Text ?? string.Empty;
            _inputBox.KeyDown += HandleInputKeyDown;
            _inputBox.TextChanged += HandleContentChanged;

            _displayLabel = new Label();
            _displayLabel.Dock = DockStyle.Fill;
            _displayLabel.Text = note.Text ?? string.Empty;
            _displayLabel.AutoSize = false;
            _displayLabel.TextAlign = ContentAlignment.TopLeft;
            _displayLabel.BackColor = Color.Transparent;
            _displayLabel.Padding = new Padding(1);
            _displayLabel.UseMnemonic = false;
            _displayLabel.MouseDown += HandleDragMouseDown;

            _copyFeedbackLabel = new Label();
            _copyFeedbackLabel.AutoSize = true;
            _copyFeedbackLabel.Text = "Copied";
            _copyFeedbackLabel.Visible = false;
            _copyFeedbackLabel.BackColor = Color.FromArgb(255, 236, 150);
            _copyFeedbackLabel.ForeColor = Color.FromArgb(70, 56, 15);
            _copyFeedbackLabel.Padding = new Padding(6, 2, 6, 2);
            _copyFeedbackLabel.MouseDown += HandleDragMouseDown;

            _saveTimer = new Timer();
            _saveTimer.Interval = 250;
            _saveTimer.Tick += HandleSaveTimerTick;

            _copyFeedbackTimer = new Timer();
            _copyFeedbackTimer.Interval = 1400;
            _copyFeedbackTimer.Tick += HandleCopyFeedbackTimerTick;

            Controls.Add(_displayLabel);
            Controls.Add(_inputBox);
            Controls.Add(_copyFeedbackLabel);
            Controls.Add(_copyButton);
            Controls.Add(_deleteButton);

            MouseDown += HandleDragMouseDown;
            Move += HandleBoundsChanged;
            Resize += HandleBoundsChanged;
            Activated += HandleActivated;
            FormClosing += HandleFormClosing;

            UpdateMode();
            ApplyCurrentStateToNote();
            _initializing = false;
        }

        public NoteRecord Note { get; private set; }

        public event EventHandler PersistRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler Finalized;

        public void FocusDraftInput(bool requestForeground)
        {
            if (Note.IsFinalized || IsDisposed)
            {
                return;
            }

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

            QueueDraftInputFocus();
        }

        public void PrepareForApplicationExit()
        {
            _copyFeedbackTimer.Stop();
            FlushPendingSave();
            _allowClose = true;
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

        private void HandleDeleteClicked(object sender, EventArgs e)
        {
            _copyFeedbackTimer.Stop();
            _saveTimer.Stop();

            if (DeleteRequested != null)
            {
                DeleteRequested(this, EventArgs.Empty);
            }
        }

        private void HandleCopyClicked(object sender, EventArgs e)
        {
            if (!Note.IsFinalized)
            {
                return;
            }

            try
            {
                Clipboard.SetText(Note.Text ?? string.Empty);
                ShowCopyFeedback();
            }
            catch
            {
            }
        }

        private void HandleInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            e.Handled = true;

            Note.IsFinalized = true;
            _displayLabel.Text = _inputBox.Text;
            TouchNote();
            UpdateMode();
            FlushPendingSave();

            if (Finalized != null)
            {
                Finalized(this, EventArgs.Empty);
            }
        }

        private void HandleContentChanged(object sender, EventArgs e)
        {
            if (_initializing)
            {
                return;
            }

            _displayLabel.Text = _inputBox.Text;
            QueueSave();
        }

        private void HandleBoundsChanged(object sender, EventArgs e)
        {
            if (_initializing || WindowState != FormWindowState.Normal)
            {
                return;
            }

            if (_copyFeedbackLabel.Visible)
            {
                CenterCopyFeedbackLabel();
            }

            QueueSave();
        }

        private void HandleActivated(object sender, EventArgs e)
        {
            QueueDraftInputFocus();
        }

        private void HandleSaveTimerTick(object sender, EventArgs e)
        {
            _saveTimer.Stop();
            EmitPersistRequest();
        }

        private void HandleCopyFeedbackTimerTick(object sender, EventArgs e)
        {
            _copyFeedbackTimer.Stop();
            _copyFeedbackLabel.Visible = false;
        }

        private void HandleFormClosing(object sender, FormClosingEventArgs e)
        {
            _copyFeedbackTimer.Stop();
            FlushPendingSave();

            if (_allowClose)
            {
                return;
            }

            if (e.CloseReason == CloseReason.ApplicationExitCall ||
                e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing)
            {
                _allowClose = true;
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        private void HandleDragMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (!Note.IsFinalized && sender == _displayLabel)
            {
                return;
            }

            if (!_inputBox.Visible || sender != _inputBox)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WmNclButtonDown, (IntPtr)NativeMethods.HtCaption, IntPtr.Zero);
            }
        }

        private void QueueSave()
        {
            ApplyCurrentStateToNote();
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void QueueDraftInputFocus()
        {
            if (Note.IsFinalized || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke((MethodInvoker)delegate
            {
                if (IsDisposed || Note.IsFinalized)
                {
                    return;
                }

                _inputBox.Select();
                _inputBox.SelectionStart = _inputBox.TextLength;
                _inputBox.SelectionLength = 0;
            });
        }

        private void FlushPendingSave()
        {
            _saveTimer.Stop();
            EmitPersistRequest();
        }

        private void EmitPersistRequest()
        {
            ApplyCurrentStateToNote();

            if (PersistRequested != null)
            {
                PersistRequested(this, EventArgs.Empty);
            }
        }

        private void UpdateMode()
        {
            bool isFinalized = Note.IsFinalized;
            _inputBox.Visible = !isFinalized;
            _displayLabel.Visible = isFinalized;
            _copyButton.Enabled = isFinalized;

            if (isFinalized)
            {
                _displayLabel.Text = Note.Text ?? string.Empty;
            }
            else
            {
                _inputBox.Text = Note.Text ?? string.Empty;
                _copyFeedbackTimer.Stop();
                _copyFeedbackLabel.Visible = false;
            }

            if (_copyFeedbackLabel.Visible)
            {
                CenterCopyFeedbackLabel();
            }

            _copyButton.Invalidate();
        }

        private void ApplyCurrentStateToNote()
        {
            if (WindowState == FormWindowState.Normal)
            {
                Note.X = Left;
                Note.Y = Top;
                Note.Width = Width;
                Note.Height = Height;
            }

            if (!Note.IsFinalized)
            {
                Note.Text = _inputBox.Text ?? string.Empty;
            }

            TouchNote();
        }

        private void TouchNote()
        {
            Note.UpdatedAt = DateTime.UtcNow.ToString("o");
        }

        private void ShowCopyFeedback()
        {
            _copyFeedbackTimer.Stop();
            CenterCopyFeedbackLabel();
            _copyFeedbackLabel.Visible = true;
            _copyFeedbackLabel.BringToFront();
            _copyFeedbackTimer.Start();
        }

        private void CenterCopyFeedbackLabel()
        {
            Size preferredSize = _copyFeedbackLabel.GetPreferredSize(Size.Empty);
            _copyFeedbackLabel.Size = preferredSize;

            int x = Math.Max(0, (ClientSize.Width - preferredSize.Width) / 2);
            int y = Math.Max(0, (ClientSize.Height - preferredSize.Height) / 2);
            _copyFeedbackLabel.Location = new Point(x, y);
        }

        private void HandleCopyButtonPaint(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            Color lineColor = button.Enabled
                ? Color.FromArgb(70, 56, 15)
                : Color.FromArgb(150, 140, 110);

            Rectangle backSheet = new Rectangle(8, 7, 9, 10);
            Rectangle frontSheet = new Rectangle(6, 9, 9, 10);

            using (Pen pen = new Pen(lineColor))
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, backSheet);
                e.Graphics.DrawRectangle(pen, frontSheet);
            }
        }

        private int GetResizeHandle(Point cursor)
        {
            const int border = 8;
            bool left = cursor.X <= border;
            bool right = cursor.X >= ClientSize.Width - border;
            bool top = cursor.Y <= border;
            bool bottom = cursor.Y >= ClientSize.Height - border;

            if (left && top)
            {
                return NativeMethods.HtTopLeft;
            }

            if (right && top)
            {
                return NativeMethods.HtTopRight;
            }

            if (left && bottom)
            {
                return NativeMethods.HtBottomLeft;
            }

            if (right && bottom)
            {
                return NativeMethods.HtBottomRight;
            }

            if (left)
            {
                return NativeMethods.HtLeft;
            }

            if (right)
            {
                return NativeMethods.HtRight;
            }

            if (top)
            {
                return NativeMethods.HtTop;
            }

            if (bottom)
            {
                return NativeMethods.HtBottom;
            }

            return NativeMethods.HtClient;
        }
    }
}
