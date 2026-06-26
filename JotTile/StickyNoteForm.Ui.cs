using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal sealed partial class StickyNoteForm
    {
        private NoteIconButton _editSaveButton = null!;
        private NoteIconButton _copyButton = null!;
        private NoteIconButton _closeButton = null!;
        private TextBox _inputBox = null!;
        private Label _displayLabel = null!;
        private Label _copyFeedbackLabel = null!;

        private void InitializeUi()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.FromArgb(255, 247, 171);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(160, 90);
            Padding = new Padding(12, 42, 12, 12);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            _editSaveButton = new NoteIconButton();
            _editSaveButton.Location = new Point(Width - 88, 10);
            _editSaveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _editSaveButton.CommandClick += HandleEditSaveClicked;

            _copyButton = new NoteIconButton();
            _copyButton.Glyph = NoteButtonGlyph.Copy;
            _copyButton.Location = new Point(Width - 58, 10);
            _copyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _copyButton.CommandClick += HandleCopyClicked;

            _closeButton = new NoteIconButton();
            _closeButton.Glyph = NoteButtonGlyph.Close;
            _closeButton.Location = new Point(Width - 28, 10);
            _closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _closeButton.CommandClick += HandleCloseClicked;

            _inputBox = new TextBox();
            _inputBox.BorderStyle = BorderStyle.None;
            _inputBox.Multiline = true;
            _inputBox.AcceptsReturn = true;
            _inputBox.AcceptsTab = false;
            _inputBox.WordWrap = true;
            _inputBox.ScrollBars = ScrollBars.Vertical;
            _inputBox.Dock = DockStyle.Fill;
            _inputBox.TextChanged += HandleEditorTextChanged;
            _inputBox.MouseDown += HandleDragMouseDown;

            _displayLabel = new Label();
            _displayLabel.Dock = DockStyle.Fill;
            _displayLabel.AutoSize = false;
            _displayLabel.UseMnemonic = false;
            _displayLabel.TextAlign = ContentAlignment.TopLeft;
            _displayLabel.BackColor = Color.Transparent;
            _displayLabel.Padding = new Padding(2);
            _displayLabel.MouseDown += HandleDragMouseDown;

            _copyFeedbackLabel = new Label();
            _copyFeedbackLabel.AutoSize = true;
            _copyFeedbackLabel.Visible = false;
            _copyFeedbackLabel.Padding = new Padding(7, 3, 7, 3);
            _copyFeedbackLabel.BackColor = Color.FromArgb(255, 236, 150);
            _copyFeedbackLabel.ForeColor = Color.FromArgb(70, 56, 15);
            _copyFeedbackLabel.MouseDown += HandleDragMouseDown;

            Controls.Add(_displayLabel);
            Controls.Add(_inputBox);
            Controls.Add(_copyFeedbackLabel);
            Controls.Add(_editSaveButton);
            Controls.Add(_copyButton);
            Controls.Add(_closeButton);
        }

        private void ApplyPaletteToControls()
        {
            Color backgroundStart = ColorUtilities.Parse(_settings.BackgroundColorStart, Color.FromArgb(255, 247, 171));
            Color backgroundEnd = ColorUtilities.Parse(_settings.BackgroundColorEnd, Color.FromArgb(255, 224, 109));
            Color textColor = ColorUtilities.Parse(_settings.TextColor, Color.FromArgb(68, 54, 0));
            Color buttonColor = ColorUtilities.Parse(_settings.ButtonColor, Color.FromArgb(255, 215, 104));
            Color buttonHoverColor = ColorUtilities.Parse(_settings.ButtonHoverColor, Color.FromArgb(255, 228, 140));
            Color buttonDisabledColor = ColorUtilities.Parse(_settings.ButtonDisabledColor, Color.FromArgb(244, 217, 146));

            BackColor = backgroundStart;
            ForeColor = textColor;

            Font noteFont = CreateNoteFont();
            _inputBox.Font = noteFont;
            _displayLabel.Font = noteFont;
            _inputBox.ForeColor = textColor;
            _displayLabel.ForeColor = textColor;
            _inputBox.BackColor = Blend(backgroundStart, backgroundEnd);
            _displayLabel.BackColor = Color.Transparent;

            ApplyButtonPalette(_editSaveButton, buttonColor, buttonHoverColor, buttonDisabledColor, textColor);
            ApplyButtonPalette(_copyButton, buttonColor, buttonHoverColor, buttonDisabledColor, textColor);
            ApplyButtonPalette(_closeButton, buttonColor, buttonHoverColor, buttonDisabledColor, textColor);

            _copyFeedbackLabel.ForeColor = textColor;
            _copyFeedbackLabel.BackColor = Blend(backgroundStart, buttonHoverColor);
            Invalidate();
        }

        private static void ApplyButtonPalette(NoteIconButton button, Color buttonColor, Color hoverColor, Color disabledColor, Color textColor)
        {
            button.ButtonColor = buttonColor;
            button.HoverColor = hoverColor;
            button.DisabledColor = disabledColor;
            button.GlyphColor = textColor;
            button.DisabledGlyphColor = ControlPaint.Dark(disabledColor);
            button.Invalidate();
        }

        private Font CreateNoteFont()
        {
            return new Font(_settings.NoteFontFamily, 10.0f, FontStyle.Regular, GraphicsUnit.Point);
        }

        private void PaintNoteBackground(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            Color backgroundStart = ColorUtilities.Parse(_settings.BackgroundColorStart, Color.FromArgb(255, 247, 171));
            Color backgroundEnd = ColorUtilities.Parse(_settings.BackgroundColorEnd, Color.FromArgb(255, 224, 109));

            if (_settings.UseGradient)
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, backgroundStart, backgroundEnd, ToLinearGradientMode(_settings.GradientDirection)))
                {
                    graphics.FillRectangle(brush, rect);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(backgroundStart))
                {
                    graphics.FillRectangle(brush, rect);
                }
            }

            DrawStroke(graphics, ColorUtilities.Parse(_settings.OuterStrokeColor, Color.FromArgb(154, 125, 25)), _settings.OuterStrokeThickness, 0);
            DrawStroke(graphics, ColorUtilities.Parse(_settings.FrameColor, Color.FromArgb(201, 171, 48)), _settings.FrameThickness, _settings.OuterStrokeThickness);
            DrawStroke(graphics, ColorUtilities.Parse(_settings.InnerStrokeColor, Color.FromArgb(255, 243, 196)), _settings.InnerStrokeThickness, _settings.OuterStrokeThickness + _settings.FrameThickness);
        }

        private static void DrawStroke(Graphics graphics, Color color, int thickness, int inset)
        {
            if (thickness <= 0)
            {
                return;
            }

            Rectangle rect = new Rectangle(inset, inset, graphics.VisibleClipBounds.Width >= 1 ? (int)graphics.VisibleClipBounds.Width - (2 * inset) - 1 : 0, graphics.VisibleClipBounds.Height >= 1 ? (int)graphics.VisibleClipBounds.Height - (2 * inset) - 1 : 0);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using (Pen pen = new Pen(color, thickness))
            {
                graphics.DrawRectangle(pen, rect);
            }
        }

        private static LinearGradientMode ToLinearGradientMode(GradientDirection direction)
        {
            switch (direction)
            {
                case GradientDirection.Horizontal:
                    return LinearGradientMode.Horizontal;
                case GradientDirection.ForwardDiagonal:
                    return LinearGradientMode.ForwardDiagonal;
                case GradientDirection.BackwardDiagonal:
                    return LinearGradientMode.BackwardDiagonal;
                default:
                    return LinearGradientMode.Vertical;
            }
        }

        private static Color Blend(Color a, Color b)
        {
            return Color.FromArgb(
                (a.R + b.R) / 2,
                (a.G + b.G) / 2,
                (a.B + b.B) / 2);
        }
    }
}
