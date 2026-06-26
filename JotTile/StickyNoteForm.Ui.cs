using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal sealed partial class StickyNoteForm
    {
        private static readonly Color ActiveHeaderGlyphColor = Color.Black;
        private NoteIconButton _editSaveButton = null!;
        private NoteIconButton _copyButton = null!;
        private NoteIconButton _closeButton = null!;
        private TextBox _inputBox = null!;
        private Panel _savedTextHost = null!;
        private Label _savedTextLabel = null!;
        private Label _copyFeedbackLabel = null!;
        private Font? _noteFont;

        private void InitializeUi()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.FromArgb(255, 247, 171);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(160, 90);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            _editSaveButton = new NoteIconButton();
            _editSaveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _editSaveButton.CommandClick += HandleEditSaveClicked;

            _copyButton = new NoteIconButton();
            _copyButton.Glyph = NoteButtonGlyph.Copy;
            _copyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _copyButton.CommandClick += HandleCopyClicked;

            _closeButton = new NoteIconButton();
            _closeButton.Glyph = NoteButtonGlyph.Close;
            _closeButton.IsDangerButton = true;
            _closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _closeButton.CommandClick += HandleCloseClicked;

            _inputBox = new TextBox();
            _inputBox.BorderStyle = BorderStyle.None;
            _inputBox.Multiline = true;
            _inputBox.AcceptsReturn = true;
            _inputBox.AcceptsTab = false;
            _inputBox.WordWrap = true;
            _inputBox.ScrollBars = ScrollBars.Vertical;
            _inputBox.TextChanged += HandleEditorTextChanged;
            _inputBox.MouseDown += HandleDragMouseDown;

            _savedTextHost = new Panel();
            _savedTextHost.AutoScroll = true;
            _savedTextHost.TabStop = false;
            _savedTextHost.MouseDown += HandleDragMouseDown;

            _savedTextLabel = new Label();
            _savedTextLabel.AutoSize = false;
            _savedTextLabel.UseMnemonic = false;
            _savedTextLabel.TextAlign = ContentAlignment.TopLeft;
            _savedTextLabel.Padding = Padding.Empty;
            _savedTextLabel.Margin = Padding.Empty;
            _savedTextLabel.MouseDown += HandleDragMouseDown;
            _savedTextHost.Controls.Add(_savedTextLabel);

            _copyFeedbackLabel = new Label();
            _copyFeedbackLabel.AutoSize = true;
            _copyFeedbackLabel.Visible = false;
            _copyFeedbackLabel.Padding = new Padding(7, 3, 7, 3);
            _copyFeedbackLabel.BackColor = Color.FromArgb(255, 236, 150);
            _copyFeedbackLabel.ForeColor = Color.FromArgb(70, 56, 15);
            _copyFeedbackLabel.MouseDown += HandleDragMouseDown;

            Controls.Add(_savedTextHost);
            Controls.Add(_inputBox);
            Controls.Add(_copyFeedbackLabel);
            Controls.Add(_editSaveButton);
            Controls.Add(_copyButton);
            Controls.Add(_closeButton);

            Layout += HandleHeaderLayoutChanged;
            UpdateNoteSurfaceLayout();
        }

        private void ApplyPaletteToControls()
        {
            Color backgroundStart = ColorUtilities.Parse(_settings.BackgroundColorStart, Color.FromArgb(255, 247, 171));
            Color backgroundEnd = ColorUtilities.Parse(_settings.BackgroundColorEnd, Color.FromArgb(255, 224, 109));
            Color textColor = ColorUtilities.Parse(_settings.TextColor, Color.FromArgb(68, 54, 0));
            Color buttonColor = ColorUtilities.Parse(_settings.ButtonColor, Color.FromArgb(217, 217, 217));
            Color buttonHoverColor = ColorUtilities.Parse(_settings.ButtonHoverColor, Color.FromArgb(236, 236, 236));
            Color buttonDisabledColor = ColorUtilities.Parse(_settings.ButtonDisabledColor, Color.FromArgb(104, 104, 104));
            ButtonRenderMode renderMode = _settings.GetButtonRenderMode();

            BackColor = backgroundStart;
            ForeColor = textColor;

            ApplyNoteFont();
            _inputBox.ForeColor = textColor;
            _savedTextHost.ForeColor = textColor;
            _savedTextLabel.ForeColor = textColor;
            _inputBox.BackColor = Blend(backgroundStart, backgroundEnd);
            _savedTextHost.BackColor = Blend(backgroundStart, backgroundEnd);
            _savedTextLabel.BackColor = Blend(backgroundStart, backgroundEnd);

            ApplyButtonPalette(_editSaveButton, buttonColor, buttonHoverColor, buttonDisabledColor, renderMode);
            ApplyButtonPalette(_copyButton, buttonColor, buttonHoverColor, buttonDisabledColor, renderMode);
            ApplyButtonPalette(_closeButton, buttonColor, buttonHoverColor, buttonDisabledColor, renderMode);

            _copyFeedbackLabel.ForeColor = textColor;
            _copyFeedbackLabel.BackColor = Blend(backgroundStart, buttonHoverColor);
            UpdateNoteSurfaceLayout();
            Invalidate();
        }

        private static void ApplyButtonPalette(NoteIconButton button, Color buttonColor, Color hoverColor, Color disabledColor, ButtonRenderMode renderMode)
        {
            button.ButtonColor = buttonColor;
            button.HoverColor = hoverColor;
            button.DisabledColor = disabledColor;
            button.GlyphColor = ActiveHeaderGlyphColor;
            button.DisabledGlyphColor = ControlPaint.Dark(disabledColor);
            button.RenderMode = renderMode;
            button.Invalidate();
        }

        private Font CreateNoteFont()
        {
            return new Font(_settings.NoteFontFamily, _settings.NoteFontSize, FontStyle.Regular, GraphicsUnit.Point);
        }

        private void ApplyNoteFont()
        {
            Font nextFont = CreateNoteFont();
            Font? previousFont = _noteFont;
            _noteFont = nextFont;
            _inputBox.Font = nextFont;
            _savedTextLabel.Font = nextFont;
            if (previousFont != null)
            {
                previousFont.Dispose();
            }
        }

        private void HandleHeaderLayoutChanged(object? sender, LayoutEventArgs e)
        {
            UpdateNoteSurfaceLayout();
        }

        private void UpdateNoteSurfaceLayout()
        {
            NoteSurfaceLayoutMetrics metrics = CreateSurfaceLayoutMetrics();
            NoteSurfaceLayout layout = NoteSurfaceLayoutCalculator.Calculate(ClientRectangle, _settings, metrics);

            Rectangle closeBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(ClientRectangle, _settings, metrics, 0);
            Rectangle copyBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(ClientRectangle, _settings, metrics, 1);
            Rectangle editBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(ClientRectangle, _settings, metrics, 2);

            _closeButton.Bounds = closeBounds;
            _copyButton.Bounds = copyBounds;
            _editSaveButton.Bounds = editBounds;
            _inputBox.Bounds = layout.TextBounds;
            _savedTextHost.Bounds = layout.TextBounds;
            UpdateSavedTextScrollState();
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

            DrawStroke(graphics, ClientRectangle, ColorUtilities.Parse(_settings.OuterStrokeColor, Color.FromArgb(154, 125, 25)), _settings.OuterStrokeThickness, 0);
            DrawStroke(graphics, ClientRectangle, ColorUtilities.Parse(_settings.FrameColor, Color.FromArgb(201, 171, 48)), _settings.FrameThickness, _settings.OuterStrokeThickness);
            DrawStroke(graphics, ClientRectangle, ColorUtilities.Parse(_settings.InnerStrokeColor, Color.FromArgb(255, 243, 196)), _settings.InnerStrokeThickness, _settings.OuterStrokeThickness + _settings.FrameThickness);
        }

        private static void DrawStroke(Graphics graphics, Rectangle bounds, Color color, int thickness, int inset)
        {
            if (thickness <= 0)
            {
                return;
            }

            Rectangle rect = new Rectangle(
                bounds.Left + inset,
                bounds.Top + inset,
                bounds.Width - (2 * inset) - 1,
                bounds.Height - (2 * inset) - 1);

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

        private static NoteSurfaceLayoutMetrics CreateSurfaceLayoutMetrics()
        {
            return new NoteSurfaceLayoutMetrics
            {
                HeaderTop = 5,
                HeaderButtonSize = 17,
                HeaderButtonSpacing = 3,
                HeaderRightMargin = 5,
                HeaderBottomGap = 8,
                ContentSidePadding = 10,
                ContentBottomPadding = 8,
                PreviewOuterMargin = 12
            };
        }
    }
}
