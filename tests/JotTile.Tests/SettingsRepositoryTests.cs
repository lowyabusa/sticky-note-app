using System.IO;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class SettingsRepositoryTests
    {
        [Fact]
        public void SaveAndLoadRoundTripsBehaviorFlags()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                SettingsRepository repository = new SettingsRepository(workspace.CreateSubdirectory("data"), workspace.CreateLogger());
                AppSettings settings = AppSettings.CreateDefault();
                settings.DeleteRequiresConfirmation = false;
                settings.ExitRequiresConfirmation = true;
                settings.LaunchAtSignIn = false;
                settings.CloseAction = NoteCloseAction.Hide;
                settings.NoteFontSize = 14.5f;

                repository.Save(settings);
                RepositoryLoadResult<AppSettings> result = repository.Load();

                Assert.False(result.Value.DeleteRequiresConfirmation);
                Assert.True(result.Value.ExitRequiresConfirmation);
                Assert.False(result.Value.LaunchAtSignIn);
                Assert.Equal(NoteCloseAction.Hide, result.Value.CloseAction);
                Assert.Equal(14.5f, result.Value.NoteFontSize);
            }
        }

        [Fact]
        public void CorruptSettingsFallBackToDefaults()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                SettingsRepository repository = new SettingsRepository(dataDir, workspace.CreateLogger());
                repository.Save(AppSettings.CreateDefault());
                File.WriteAllText(Path.Combine(dataDir, AppIdentity.SettingsFileName), "{broken");

                RepositoryLoadResult<AppSettings> result = repository.Load();

                Assert.Equal(AppSettings.CreateDefault().NoteFontFamily, result.Value.NoteFontFamily);
                Assert.Contains("Default settings were restored", result.UserMessage);
            }
        }

        [Fact]
        public void MissingNoteFontSizeFallsBackToDefault()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                string dataDir = workspace.CreateSubdirectory("data");
                SettingsRepository repository = new SettingsRepository(dataDir, workspace.CreateLogger());
                string settingsPath = Path.Combine(dataDir, AppIdentity.SettingsFileName);
                string legacyJson = "{\"backgroundColorStart\":\"#FFF7AB\",\"backgroundColorEnd\":\"#FFE06D\",\"useGradient\":true,\"gradientDirection\":0,\"textColor\":\"#443600\",\"frameColor\":\"#C9AB30\",\"frameThickness\":2,\"innerStrokeColor\":\"#FFF3C4\",\"innerStrokeThickness\":1,\"outerStrokeColor\":\"#9A7D19\",\"outerStrokeThickness\":1,\"buttonColor\":\"#D9D9D9\",\"buttonHoverColor\":\"#ECECEC\",\"buttonDisabledColor\":\"#686868\",\"noteFontFamily\":\"Segoe UI\",\"closeAction\":0,\"deleteRequiresConfirmation\":true,\"exitRequiresConfirmation\":false,\"exitUnsavedAction\":0,\"launchAtSignIn\":true}";
                File.WriteAllText(settingsPath, legacyJson);

                RepositoryLoadResult<AppSettings> result = repository.Load();

                Assert.Equal(AppSettings.CreateDefault().NoteFontSize, result.Value.NoteFontSize);
            }
        }
    }
}
