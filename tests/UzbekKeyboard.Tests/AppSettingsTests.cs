using UzbekKeyboard;
using Xunit;
namespace UzbekKeyboard.Tests;
public class AppSettingsTests {
 [Fact]
 public void NormalizesExcludedApps() =>
  Assert.Equal(new[]{"keepass.exe","code.exe"},
   AppSettings.NormalizeApps(new[]{" KeePass ",@"C:\Tools\Code.exe","keepass.exe","", null, "bad|name"}));
}
