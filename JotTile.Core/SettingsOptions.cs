using System.Collections.Generic;

namespace JotTile.Core
{
    internal static class SettingsOptions
    {
        internal static readonly IReadOnlyList<NamedValue> ColorChoices = new[]
        {
            new NamedValue("Amber", "#FFD768"),
            new NamedValue("Sun", "#FFE06D"),
            new NamedValue("Honey", "#FFF7AB"),
            new NamedValue("Cream", "#FFF3C4"),
            new NamedValue("Moss", "#C8D66B"),
            new NamedValue("Sky", "#B9D6F2"),
            new NamedValue("Coral", "#F4A884"),
            new NamedValue("Ink", "#443600"),
            new NamedValue("Slate", "#5B6472"),
            new NamedValue("Coffee", "#7A5C3E")
        };

        internal static readonly IReadOnlyList<string> NoteFontFamilies = new[]
        {
            "Segoe UI",
            "Georgia",
            "Trebuchet MS",
            "Consolas",
            "Palatino Linotype"
        };
    }

    internal sealed class NamedValue
    {
        internal NamedValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        internal string Name { get; }

        internal string Value { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
