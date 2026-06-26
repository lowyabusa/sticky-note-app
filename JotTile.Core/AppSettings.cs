using System.Runtime.Serialization;

namespace JotTile.Core
{
    [DataContract]
    internal sealed class AppSettings
    {
        [DataMember(Name = "backgroundColorStart", Order = 0)]
        public string BackgroundColorStart { get; set; } = "#FFF7AB";

        [DataMember(Name = "backgroundColorEnd", Order = 1)]
        public string BackgroundColorEnd { get; set; } = "#FFE06D";

        [DataMember(Name = "useGradient", Order = 2)]
        public bool UseGradient { get; set; } = true;

        [DataMember(Name = "gradientDirection", Order = 3)]
        public GradientDirection GradientDirection { get; set; } = GradientDirection.Vertical;

        [DataMember(Name = "textColor", Order = 4)]
        public string TextColor { get; set; } = "#443600";

        [DataMember(Name = "frameColor", Order = 5)]
        public string FrameColor { get; set; } = "#C9AB30";

        [DataMember(Name = "frameThickness", Order = 6)]
        public int FrameThickness { get; set; } = 2;

        [DataMember(Name = "innerStrokeColor", Order = 7)]
        public string InnerStrokeColor { get; set; } = "#FFF3C4";

        [DataMember(Name = "innerStrokeThickness", Order = 8)]
        public int InnerStrokeThickness { get; set; } = 1;

        [DataMember(Name = "outerStrokeColor", Order = 9)]
        public string OuterStrokeColor { get; set; } = "#9A7D19";

        [DataMember(Name = "outerStrokeThickness", Order = 10)]
        public int OuterStrokeThickness { get; set; } = 1;

        [DataMember(Name = "buttonColor", Order = 11)]
        public string ButtonColor { get; set; } = "#D9D9D9";

        [DataMember(Name = "buttonHoverColor", Order = 12)]
        public string ButtonHoverColor { get; set; } = "#ECECEC";

        [DataMember(Name = "buttonDisabledColor", Order = 13)]
        public string ButtonDisabledColor { get; set; } = "#686868";

        [DataMember(Name = "noteFontFamily", Order = 14)]
        public string NoteFontFamily { get; set; } = "Segoe UI";

        [DataMember(Name = "noteFontSize", Order = 15)]
        public float NoteFontSize { get; set; } = 11.5f;

        [DataMember(Name = "closeAction", Order = 16)]
        public NoteCloseAction CloseAction { get; set; } = NoteCloseAction.Delete;

        [DataMember(Name = "deleteRequiresConfirmation", Order = 17)]
        public bool DeleteRequiresConfirmation { get; set; } = false;

        [DataMember(Name = "exitRequiresConfirmation", Order = 18)]
        public bool ExitRequiresConfirmation { get; set; } = false;

        [DataMember(Name = "exitUnsavedAction", Order = 19)]
        public ExitUnsavedAction ExitUnsavedAction { get; set; } = ExitUnsavedAction.Discard;

        [DataMember(Name = "launchAtSignIn", Order = 20)]
        public bool LaunchAtSignIn { get; set; } = true;

        internal AppSettings Clone()
        {
            return new AppSettings
            {
                BackgroundColorStart = BackgroundColorStart,
                BackgroundColorEnd = BackgroundColorEnd,
                UseGradient = UseGradient,
                GradientDirection = GradientDirection,
                TextColor = TextColor,
                FrameColor = FrameColor,
                FrameThickness = FrameThickness,
                InnerStrokeColor = InnerStrokeColor,
                InnerStrokeThickness = InnerStrokeThickness,
                OuterStrokeColor = OuterStrokeColor,
                OuterStrokeThickness = OuterStrokeThickness,
                ButtonColor = ButtonColor,
                ButtonHoverColor = ButtonHoverColor,
                ButtonDisabledColor = ButtonDisabledColor,
                NoteFontFamily = NoteFontFamily,
                NoteFontSize = NoteFontSize,
                CloseAction = CloseAction,
                DeleteRequiresConfirmation = DeleteRequiresConfirmation,
                ExitRequiresConfirmation = ExitRequiresConfirmation,
                ExitUnsavedAction = ExitUnsavedAction,
                LaunchAtSignIn = LaunchAtSignIn
            };
        }

        internal void ApplyDefaults()
        {
            AppSettings defaults = CreateDefault();
            BackgroundColorStart = NormalizeText(BackgroundColorStart, defaults.BackgroundColorStart);
            BackgroundColorEnd = NormalizeText(BackgroundColorEnd, defaults.BackgroundColorEnd);
            TextColor = NormalizeText(TextColor, defaults.TextColor);
            FrameColor = NormalizeText(FrameColor, defaults.FrameColor);
            InnerStrokeColor = NormalizeText(InnerStrokeColor, defaults.InnerStrokeColor);
            OuterStrokeColor = NormalizeText(OuterStrokeColor, defaults.OuterStrokeColor);
            ButtonColor = NormalizeText(ButtonColor, defaults.ButtonColor);
            ButtonHoverColor = NormalizeText(ButtonHoverColor, defaults.ButtonHoverColor);
            ButtonDisabledColor = NormalizeText(ButtonDisabledColor, defaults.ButtonDisabledColor);
            NoteFontFamily = NormalizeText(NoteFontFamily, defaults.NoteFontFamily);
            NoteFontSize = NormalizeFloat(NoteFontSize, 8.0f, 24.0f, defaults.NoteFontSize);
            FrameThickness = NormalizeInt(FrameThickness, 1, 6, defaults.FrameThickness);
            InnerStrokeThickness = NormalizeInt(InnerStrokeThickness, 0, 6, defaults.InnerStrokeThickness);
            OuterStrokeThickness = NormalizeInt(OuterStrokeThickness, 0, 6, defaults.OuterStrokeThickness);
        }

        internal static AppSettings CreateDefault()
        {
            return new AppSettings();
        }

        internal ButtonRenderMode GetButtonRenderMode()
        {
            AppSettings defaults = CreateDefault();

            if (string.Equals(ButtonColor, defaults.ButtonColor, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ButtonHoverColor, defaults.ButtonHoverColor, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ButtonDisabledColor, defaults.ButtonDisabledColor, System.StringComparison.OrdinalIgnoreCase))
            {
                return ButtonRenderMode.SystemDefault;
            }

            return ButtonRenderMode.CustomOverride;
        }

        private static string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static int NormalizeInt(int value, int minValue, int maxValue, int fallback)
        {
            if (value < minValue || value > maxValue)
            {
                return fallback;
            }

            return value;
        }

        private static float NormalizeFloat(float value, float minValue, float maxValue, float fallback)
        {
            if (value < minValue || value > maxValue)
            {
                return fallback;
            }

            return value;
        }
    }
}
