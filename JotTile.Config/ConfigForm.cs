using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile.Config
{
    internal sealed class ConfigForm : Form
    {
        private const int AppearancePreviewWidthDivisor = 3;
        private const int AppearancePreviewMinimumWidth = 240;
        private const int AppearancePreviewFallbackMinimumWidth = 120;
        private const int AppearanceEditorMinimumWidth = 260;
        private readonly SettingsRepository _settingsRepository;
        private readonly AppLogger _logger;
        private readonly PreviewNoteControl _previewControl;
        private readonly ComboBox _backgroundStartCombo;
        private readonly ComboBox _backgroundEndCombo;
        private readonly ComboBox _textColorCombo;
        private readonly ComboBox _frameColorCombo;
        private readonly NumericUpDown _frameThickness;
        private readonly ComboBox _innerStrokeColorCombo;
        private readonly NumericUpDown _innerStrokeThickness;
        private readonly ComboBox _outerStrokeColorCombo;
        private readonly NumericUpDown _outerStrokeThickness;
        private readonly ComboBox _buttonColorCombo;
        private readonly ComboBox _buttonHoverColorCombo;
        private readonly ComboBox _buttonDisabledColorCombo;
        private readonly CheckBox _useGradientCheckBox;
        private readonly ComboBox _gradientDirectionCombo;
        private readonly ComboBox _fontFamilyCombo;
        private readonly NumericUpDown _fontSizeEditor;
        private readonly Button _restoreAppearanceDefaultsButton;
        private readonly ComboBox _closeActionCombo;
        private readonly CheckBox _deleteConfirmationCheckBox;
        private readonly CheckBox _exitConfirmationCheckBox;
        private readonly ComboBox _exitUnsavedActionCombo;
        private readonly CheckBox _launchAtSignInCheckBox;
        private readonly Label _statusLabel;
        private AppSettings _settings;

        internal ConfigForm()
        {
            _settingsRepository = new SettingsRepository();
            _logger = new AppLogger();
            _settings = _settingsRepository.Load().Value;

            Text = "JotTile Settings";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(820, 620);
            Size = new Size(860, 660);
            Font = SystemFonts.MessageBoxFont;

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            _backgroundStartCombo = CreateColorCombo();
            _backgroundEndCombo = CreateColorCombo();
            _textColorCombo = CreateColorCombo();
            _frameColorCombo = CreateColorCombo();
            _frameThickness = CreateThicknessEditor();
            _innerStrokeColorCombo = CreateColorCombo();
            _innerStrokeThickness = CreateThicknessEditor();
            _outerStrokeColorCombo = CreateColorCombo();
            _outerStrokeThickness = CreateThicknessEditor();
            _buttonColorCombo = CreateColorCombo();
            _buttonHoverColorCombo = CreateColorCombo();
            _buttonDisabledColorCombo = CreateColorCombo();
            _useGradientCheckBox = new CheckBox();
            _useGradientCheckBox.Text = "Use gradient background";
            _gradientDirectionCombo = new ComboBox();
            _gradientDirectionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _gradientDirectionCombo.Items.AddRange(Enum.GetNames(typeof(GradientDirection)));
            _fontFamilyCombo = new ComboBox();
            _fontFamilyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _fontFamilyCombo.Items.AddRange(SettingsOptions.NoteFontFamilies.Cast<object>().ToArray());
            _fontSizeEditor = CreateFontSizeEditor();
            _restoreAppearanceDefaultsButton = new Button();
            _restoreAppearanceDefaultsButton.Text = "Restore defaults";
            _restoreAppearanceDefaultsButton.AutoSize = true;
            _restoreAppearanceDefaultsButton.Anchor = AnchorStyles.Left;
            _restoreAppearanceDefaultsButton.Click += HandleRestoreAppearanceDefaultsClicked;
            _closeActionCombo = new ComboBox();
            _closeActionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _closeActionCombo.Items.AddRange(Enum.GetNames(typeof(NoteCloseAction)));
            _deleteConfirmationCheckBox = new CheckBox();
            _deleteConfirmationCheckBox.Text = "Require confirmation before deleting a note";
            _exitConfirmationCheckBox = new CheckBox();
            _exitConfirmationCheckBox.Text = "Require confirmation before exiting";
            _exitUnsavedActionCombo = new ComboBox();
            _exitUnsavedActionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _exitUnsavedActionCombo.Items.AddRange(Enum.GetNames(typeof(ExitUnsavedAction)));
            _launchAtSignInCheckBox = new CheckBox();
            _launchAtSignInCheckBox.Text = "Launch JotTile at Windows sign-in";
            _previewControl = new PreviewNoteControl();
            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            tabs.TabPages.Add(CreateAppearanceTab());
            tabs.TabPages.Add(CreateBehaviorTab());

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Padding = new Padding(0);

            Button saveButton = new Button();
            saveButton.Text = "Save and Close";
            saveButton.AutoSize = true;
            saveButton.Click += HandleSaveAndCloseClicked;

            Button applyButton = new Button();
            applyButton.Text = "Apply";
            applyButton.AutoSize = true;
            applyButton.Click += HandleApplyClicked;

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.AutoSize = true;
            cancelButton.Click += HandleCancelClicked;

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(tabs, 0, 0);
            root.Controls.Add(_statusLabel, 0, 1);
            root.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(root);

            WirePreviewRefresh();
            BindSettingsToUi();
            UpdatePreview();
        }

        private TabPage CreateAppearanceTab()
        {
            TabPage page = new TabPage("Appearance");
            TableLayoutPanel layout = CreateEditorGrid();
            AddEditorRow(layout, 0, "Background start", _backgroundStartCombo);
            AddEditorRow(layout, 1, "Background end", _backgroundEndCombo);
            AddEditorRow(layout, 2, "Text color", _textColorCombo);
            AddEditorRow(layout, 3, "Frame color", _frameColorCombo);
            AddEditorRow(layout, 4, "Frame thickness", _frameThickness);
            AddEditorRow(layout, 5, "Inner stroke color", _innerStrokeColorCombo);
            AddEditorRow(layout, 6, "Inner stroke thickness", _innerStrokeThickness);
            AddEditorRow(layout, 7, "Outer stroke color", _outerStrokeColorCombo);
            AddEditorRow(layout, 8, "Outer stroke thickness", _outerStrokeThickness);
            AddEditorRow(layout, 9, "Button color", _buttonColorCombo);
            AddEditorRow(layout, 10, "Button hover", _buttonHoverColorCombo);
            AddEditorRow(layout, 11, "Button disabled", _buttonDisabledColorCombo);
            AddEditorRow(layout, 12, "Gradient", _useGradientCheckBox);
            AddEditorRow(layout, 13, "Gradient direction", _gradientDirectionCombo);
            AddEditorRow(layout, 14, "Note font", _fontFamilyCombo);
            AddEditorRow(layout, 15, "Note text size", _fontSizeEditor);
            AddEditorRow(layout, 16, "Defaults", _restoreAppearanceDefaultsButton);

            TableLayoutPanel appearanceLayout = new TableLayoutPanel();
            appearanceLayout.Dock = DockStyle.Fill;
            appearanceLayout.Padding = new Padding(12);
            appearanceLayout.RowCount = 1;
            appearanceLayout.ColumnCount = 2;
            appearanceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            appearanceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            appearanceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppearancePreviewMinimumWidth));

            Panel editorHost = new Panel();
            editorHost.Dock = DockStyle.Fill;
            editorHost.Controls.Add(layout);

            Panel previewHost = new Panel();
            previewHost.Dock = DockStyle.Fill;
            _previewControl.Dock = DockStyle.Fill;
            previewHost.Controls.Add(_previewControl);

            appearanceLayout.Controls.Add(editorHost, 0, 0);
            appearanceLayout.Controls.Add(previewHost, 1, 0);
            appearanceLayout.Resize += HandleAppearanceLayoutResize;
            ApplyAppearanceLayout(appearanceLayout);

            page.Controls.Add(appearanceLayout);
            return page;
        }

        private TabPage CreateBehaviorTab()
        {
            TabPage page = new TabPage("Behavior");
            TableLayoutPanel layout = CreateEditorGrid();
            AddEditorRow(layout, 0, "X button action", _closeActionCombo);
            AddEditorRow(layout, 1, "Delete confirmation", _deleteConfirmationCheckBox);
            AddEditorRow(layout, 2, "Exit confirmation", _exitConfirmationCheckBox);
            AddEditorRow(layout, 3, "Unsaved exit action", _exitUnsavedActionCombo);
            AddEditorRow(layout, 4, "Startup", _launchAtSignInCheckBox);
            page.Controls.Add(layout);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            return page;
        }

        private void WirePreviewRefresh()
        {
            EventHandler handler = delegate
            {
                UpdatePreview();
            };

            Control[] controls =
            {
                _backgroundStartCombo,
                _backgroundEndCombo,
                _textColorCombo,
                _frameColorCombo,
                _frameThickness,
                _innerStrokeColorCombo,
                _innerStrokeThickness,
                _outerStrokeColorCombo,
                _outerStrokeThickness,
                _buttonColorCombo,
                _buttonHoverColorCombo,
                _buttonDisabledColorCombo,
                _useGradientCheckBox,
                _gradientDirectionCombo,
                _fontFamilyCombo,
                _fontSizeEditor,
                _closeActionCombo,
                _deleteConfirmationCheckBox,
                _exitConfirmationCheckBox,
                _exitUnsavedActionCombo,
                _launchAtSignInCheckBox
            };

            for (int index = 0; index < controls.Length; index++)
            {
                controls[index].TextChanged += handler;
                controls[index].Click += handler;
                controls[index].Enter += handler;
            }

            ComboBox[] comboBoxes =
            {
                _backgroundStartCombo,
                _backgroundEndCombo,
                _textColorCombo,
                _frameColorCombo,
                _innerStrokeColorCombo,
                _outerStrokeColorCombo,
                _buttonColorCombo,
                _buttonHoverColorCombo,
                _buttonDisabledColorCombo,
                _gradientDirectionCombo,
                _fontFamilyCombo,
                _closeActionCombo,
                _exitUnsavedActionCombo
            };

            for (int index = 0; index < comboBoxes.Length; index++)
            {
                comboBoxes[index].SelectedIndexChanged += handler;
            }

            CheckBox[] checkBoxes =
            {
                _useGradientCheckBox,
                _deleteConfirmationCheckBox,
                _exitConfirmationCheckBox,
                _launchAtSignInCheckBox
            };

            for (int index = 0; index < checkBoxes.Length; index++)
            {
                checkBoxes[index].CheckedChanged += handler;
            }

            NumericUpDown[] numericEditors =
            {
                _frameThickness,
                _innerStrokeThickness,
                _outerStrokeThickness,
                _fontSizeEditor
            };

            for (int index = 0; index < numericEditors.Length; index++)
            {
                numericEditors[index].ValueChanged += handler;
            }
        }

        private void HandleApplyClicked(object? sender, EventArgs e)
        {
            SaveSettings(closeAfterSave: false);
        }

        private void HandleSaveAndCloseClicked(object? sender, EventArgs e)
        {
            if (SaveSettings(closeAfterSave: true))
            {
                Close();
            }
        }

        private void HandleCancelClicked(object? sender, EventArgs e)
        {
            Close();
        }

        private void HandleRestoreAppearanceDefaultsClicked(object? sender, EventArgs e)
        {
            BindAppearanceSettingsToUi(AppSettings.CreateDefault());
            UpdatePreview();
            _statusLabel.Text = "Appearance defaults restored.";
        }

        private bool SaveSettings(bool closeAfterSave)
        {
            try
            {
                _settings = ReadSettingsFromUi();
                _settingsRepository.Save(_settings);
                AppSignals.Raise(AppIdentity.SettingsChangedEventName);
                _statusLabel.Text = closeAfterSave ? "Settings saved." : "Settings applied.";
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("config-save", "Saving settings failed.", ex);
                _statusLabel.Text = "Saving settings failed.";
                MessageBox.Show(
                    this,
                    "JotTile could not save the settings. See app.log for technical details.",
                    "JotTile Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void BindSettingsToUi()
        {
            BindAppearanceSettingsToUi(_settings);
            _closeActionCombo.SelectedItem = _settings.CloseAction.ToString();
            _deleteConfirmationCheckBox.Checked = _settings.DeleteRequiresConfirmation;
            _exitConfirmationCheckBox.Checked = _settings.ExitRequiresConfirmation;
            _exitUnsavedActionCombo.SelectedItem = _settings.ExitUnsavedAction.ToString();
            _launchAtSignInCheckBox.Checked = _settings.LaunchAtSignIn;
        }

        private AppSettings ReadSettingsFromUi()
        {
            AppSettings settings = _settings.Clone();
            settings.BackgroundColorStart = GetSelectedColorValue(_backgroundStartCombo);
            settings.BackgroundColorEnd = GetSelectedColorValue(_backgroundEndCombo);
            settings.TextColor = GetSelectedColorValue(_textColorCombo);
            settings.FrameColor = GetSelectedColorValue(_frameColorCombo);
            settings.InnerStrokeColor = GetSelectedColorValue(_innerStrokeColorCombo);
            settings.OuterStrokeColor = GetSelectedColorValue(_outerStrokeColorCombo);
            settings.ButtonColor = GetSelectedColorValue(_buttonColorCombo);
            settings.ButtonHoverColor = GetSelectedColorValue(_buttonHoverColorCombo);
            settings.ButtonDisabledColor = GetSelectedColorValue(_buttonDisabledColorCombo);
            settings.FrameThickness = (int)_frameThickness.Value;
            settings.InnerStrokeThickness = (int)_innerStrokeThickness.Value;
            settings.OuterStrokeThickness = (int)_outerStrokeThickness.Value;
            settings.UseGradient = _useGradientCheckBox.Checked;
            settings.GradientDirection = ParseEnum<GradientDirection>(_gradientDirectionCombo.SelectedItem, GradientDirection.Vertical);
            settings.NoteFontFamily = _fontFamilyCombo.SelectedItem as string ?? AppSettings.CreateDefault().NoteFontFamily;
            settings.NoteFontSize = (float)_fontSizeEditor.Value;
            settings.CloseAction = ParseEnum<NoteCloseAction>(_closeActionCombo.SelectedItem, NoteCloseAction.Delete);
            settings.DeleteRequiresConfirmation = _deleteConfirmationCheckBox.Checked;
            settings.ExitRequiresConfirmation = _exitConfirmationCheckBox.Checked;
            settings.ExitUnsavedAction = ParseEnum<ExitUnsavedAction>(_exitUnsavedActionCombo.SelectedItem, ExitUnsavedAction.Discard);
            settings.LaunchAtSignIn = _launchAtSignInCheckBox.Checked;
            settings.ApplyDefaults();
            return settings;
        }

        private void UpdatePreview()
        {
            _previewControl.ApplySettings(ReadSettingsFromUi());
        }

        private void BindAppearanceSettingsToUi(AppSettings settings)
        {
            SelectColor(_backgroundStartCombo, settings.BackgroundColorStart);
            SelectColor(_backgroundEndCombo, settings.BackgroundColorEnd);
            SelectColor(_textColorCombo, settings.TextColor);
            SelectColor(_frameColorCombo, settings.FrameColor);
            SelectColor(_innerStrokeColorCombo, settings.InnerStrokeColor);
            SelectColor(_outerStrokeColorCombo, settings.OuterStrokeColor);
            SelectColor(_buttonColorCombo, settings.ButtonColor);
            SelectColor(_buttonHoverColorCombo, settings.ButtonHoverColor);
            SelectColor(_buttonDisabledColorCombo, settings.ButtonDisabledColor);
            _frameThickness.Value = settings.FrameThickness;
            _innerStrokeThickness.Value = settings.InnerStrokeThickness;
            _outerStrokeThickness.Value = settings.OuterStrokeThickness;
            _useGradientCheckBox.Checked = settings.UseGradient;
            _gradientDirectionCombo.SelectedItem = settings.GradientDirection.ToString();
            _fontFamilyCombo.SelectedItem = settings.NoteFontFamily;
            SetNumericValue(_fontSizeEditor, (decimal)settings.NoteFontSize);
        }

        private void HandleAppearanceLayoutResize(object? sender, EventArgs e)
        {
            TableLayoutPanel? layout = sender as TableLayoutPanel;
            if (layout == null)
            {
                return;
            }

            ApplyAppearanceLayout(layout);
        }

        private static void ApplyAppearanceLayout(TableLayoutPanel layout)
        {
            int availableWidth = Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal);
            int previewWidth = availableWidth / AppearancePreviewWidthDivisor;
            if (previewWidth < AppearancePreviewMinimumWidth)
            {
                previewWidth = AppearancePreviewMinimumWidth;
            }

            int maxPreviewWidth = availableWidth - AppearanceEditorMinimumWidth;
            if (maxPreviewWidth < previewWidth)
            {
                previewWidth = maxPreviewWidth;
            }

            if (previewWidth < AppearancePreviewFallbackMinimumWidth)
            {
                previewWidth = AppearancePreviewFallbackMinimumWidth;
            }

            if (previewWidth >= availableWidth)
            {
                previewWidth = Math.Max(1, availableWidth - 1);
            }

            layout.ColumnStyles[1].SizeType = SizeType.Absolute;
            layout.ColumnStyles[1].Width = previewWidth;
        }

        private static TableLayoutPanel CreateEditorGrid()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 2;
            layout.RowCount = 18;
            layout.AutoScroll = true;
            layout.Dock = DockStyle.Fill;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            return layout;
        }

        private static void AddEditorRow(TableLayoutPanel layout, int rowIndex, string labelText, Control editor)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label label = new Label();
            label.Text = labelText;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            label.AutoSize = true;

            editor.Dock = DockStyle.Top;

            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
        }

        private static ComboBox CreateColorCombo()
        {
            ComboBox combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.AddRange(SettingsOptions.ColorChoices.Cast<object>().ToArray());
            return combo;
        }

        private static NumericUpDown CreateThicknessEditor()
        {
            NumericUpDown editor = new NumericUpDown();
            editor.Minimum = 0;
            editor.Maximum = 6;
            return editor;
        }

        private static NumericUpDown CreateFontSizeEditor()
        {
            NumericUpDown editor = new NumericUpDown();
            editor.Minimum = 8;
            editor.Maximum = 24;
            editor.DecimalPlaces = 1;
            editor.Increment = 0.5m;
            return editor;
        }

        private static void SelectColor(ComboBox combo, string hexValue)
        {
            for (int index = 0; index < combo.Items.Count; index++)
            {
                if (combo.Items[index] is NamedValue option &&
                    string.Equals(option.Value, hexValue, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = index;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private static string GetSelectedColorValue(ComboBox combo)
        {
            NamedValue? selected = combo.SelectedItem as NamedValue;
            if (selected != null)
            {
                return selected.Value;
            }

            if (combo.Items.Count > 0 && combo.Items[0] is NamedValue first)
            {
                return first.Value;
            }

            return AppSettings.CreateDefault().BackgroundColorStart;
        }

        private static void SetNumericValue(NumericUpDown editor, decimal value)
        {
            decimal clampedValue = Math.Min(editor.Maximum, Math.Max(editor.Minimum, value));
            editor.Value = clampedValue;
        }

        private static TEnum ParseEnum<TEnum>(object? selectedItem, TEnum fallback)
            where TEnum : struct
        {
            if (selectedItem is string text && Enum.TryParse(text, out TEnum parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
