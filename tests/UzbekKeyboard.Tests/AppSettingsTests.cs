using System.IO;
using UzbekKeyboard;
using Xunit;

namespace UzbekKeyboard.Tests;

public class AppSettingsTests {
    [Fact]
    public void NormalizesExcludedApps() =>
        Assert.Equal(
            new[] { "keepass.exe", "code.exe" },
            AppSettings.NormalizeApps(new[] { " KeePass ", @"C:\Tools\Code.exe", "keepass.exe", "", null, "bad|name" }));

    [Fact]
    public void RoundTripsAndNormalizesAllSettings() {
        string tempDir = Path.Combine(Path.GetTempPath(), "YangiLotinTests_" + Guid.NewGuid().ToString("N"));
        string filePath = Path.Combine(tempDir, "settings.json");
        try {
            var settings = new AppSettings {
                ExcludedApps = new List<string> { "KeePass", "keepass.exe" },
                Language = "RU",
                Enabled = false,
                WorkOnEnglish = true,
                AltGrShortcuts = false,
                SmartTutuq = true,
                Theme = 99,
                PauseHotkey = "ctrl+alt+p",
                SelectionHotkeyEnabled = false,
                SelectionConvertMode = "cyrToNew"
            };
            settings.Save(filePath);

            var loaded = AppSettings.Load(filePath);
            Assert.Equal(new[] { "keepass.exe" }, loaded.ExcludedApps);
            Assert.Equal("ru", loaded.Language);
            Assert.False(loaded.Enabled);
            Assert.True(loaded.WorkOnEnglish);
            Assert.False(loaded.AltGrShortcuts);
            Assert.True(loaded.SmartTutuq);
            Assert.Equal(2, loaded.Theme);
            Assert.Equal("Ctrl+Alt+P", loaded.PauseHotkey);
            Assert.False(loaded.SelectionHotkeyEnabled);
            Assert.Equal("cyrToNew", loaded.SelectionConvertMode);
        } finally {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FallsBackToSafeDefaultsForInvalidValues() {
        Assert.Equal("Ctrl+Alt+U", AppSettings.NormalizePauseHotkey("Invalid+Key"));
        Assert.Equal("auto", AppSettings.NormalizeSelectionMode("unknown"));
    }
}
