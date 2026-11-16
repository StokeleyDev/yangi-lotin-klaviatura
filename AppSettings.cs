using System.IO;
using System.Text.Json;
namespace UzbekKeyboard;
/// <summary>User settings stored in %LOCALAPPDATA%\YangiLotinKlaviatura\settings.json.</summary>
internal sealed class AppSettings {
 internal const int MaxExcludedApps=64;
 private static readonly JsonSerializerOptions Json=new() { PropertyNamingPolicy=JsonNamingPolicy.CamelCase, WriteIndented=true };
 internal static string FilePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YangiLotinKlaviatura","settings.json");
 public List<string> ExcludedApps { get; set; }=new();

 internal static AppSettings Load() {
  try {
   if (File.Exists(FilePath)) {
    var loaded=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath),Json) ?? new AppSettings();
    loaded.ExcludedApps=NormalizeApps(loaded.ExcludedApps ?? new());
    return loaded;
   }
  } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
  return new AppSettings();
 }

 internal void Save() {
  Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
  string temp=FilePath+".tmp";
  File.WriteAllText(temp,JsonSerializer.Serialize(this,Json));
  File.Move(temp,FilePath,true);
 }

 /// <summary>Lower-case exe file names, ".exe" appended when no extension, invalid and duplicate names dropped.</summary>
 internal static List<string> NormalizeApps(IEnumerable<string?> names) {
  var result=new List<string>();
  foreach (var raw in names) {
   if (string.IsNullOrWhiteSpace(raw)) continue;
   string name=raw.Trim().Trim('"');
   int slash=name.LastIndexOfAny(new[]{'\\','/'}); if (slash>=0) name=name[(slash+1)..];
   if (name.Length==0 || name.Length>200 || name.IndexOfAny(Path.GetInvalidFileNameChars())>=0) continue;
   if (!Path.HasExtension(name)) name+=".exe";
   name=name.ToLowerInvariant();
   if (!result.Contains(name)) result.Add(name);
   if (result.Count==MaxExcludedApps) break;
  }
  return result;
 }
}
