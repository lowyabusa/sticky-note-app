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

                repository.Save(settings);
                RepositoryLoadResult<AppSettings> result = repository.Load();

                Assert.False(result.Value.DeleteRequiresConfirmation);
                Assert.True(result.Value.ExitRequiresConfirmation);
                Assert.False(result.Value.LaunchAtSignIn);
                Assert.Equal(NoteCloseAction.Hide, result.Value.CloseAction);
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
    }
}
