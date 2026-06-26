using System.Linq;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class SettingsOptionsTests
    {
        [Fact]
        public void ColorChoicesContainFreshInstallButtonDefaults()
        {
            AppSettings defaults = AppSettings.CreateDefault();
            string[] values = SettingsOptions.ColorChoices.Select(choice => choice.Value).ToArray();

            Assert.Contains(defaults.ButtonColor, values);
            Assert.Contains(defaults.ButtonHoverColor, values);
            Assert.Contains(defaults.ButtonDisabledColor, values);
        }
    }
}
