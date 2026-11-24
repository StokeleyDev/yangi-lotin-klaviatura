using System.IO;
using System.Text.Json;

namespace UzbekKeyboard;

/// <summary>User settings stored in %LOCALAPPDATA%\YangiLotinKlaviatura\settings.json.</summary>
internal sealed class AppSettings {
    internal const int MaxExcludedApps = 64;
    internal static readonly string[] AllowedPauseHotkeys = { "Ctrl+Alt+U", "Ctrl+Alt+P", "Ctrl+Shift+U", "Ctrl+Alt+Pause" };
    internal static readonly string[] AllowedSelectionModes = { "auto", "oldToNew", "newToOld", "cyrToNew", "cyrToOld", "newToCyr", "oldToCyr" };

    private static readonly JsonSerializerOptions Json = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    internal static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YangiLotinKlaviatura",
        "settings.json");

    public List<string> ExcludedApps { get; set; } = new();
    public string Language { get; set; } = "uz";
    public bool Enabled { get; set; } = true;
    public bool WorkOnEnglish { get; set; } = false;
    public bool AltGrShortcuts { get; set; } = true;
    public bool SmartTutuq { get; set; } = false;
    public int Theme { get; set; } = 0;
    public string PauseHotkey { get; set; } = "Ctrl+Alt+U";
    public bool SelectionHotkeyEnabled { get; set; } = true;
    public string SelectionConvertMode { get; set; } = "auto";

    internal static AppSettings Load() => Load(FilePath);

    internal static AppSettings Load(string path) {
        try {
            if (File.Exists(path)) {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Json) ?? new AppSettings();
                loaded.Normalize();
                return loaded;
            }
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        return new AppSettings();
    }

    internal void Save() => Save(FilePath);

    internal void Save(string path) {
        Normalize();
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, Json));
        File.Move(temp, path, true);
    }

    internal void Normalize() {
        ExcludedApps = NormalizeApps(ExcludedApps ?? new());
        Language = string.Equals(Language, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "uz";
        Theme = Math.Clamp(Theme, 0, 2);
        PauseHotkey = NormalizePauseHotkey(PauseHotkey);
        SelectionConvertMode = NormalizeSelectionMode(SelectionConvertMode);
    }

    internal static string NormalizePauseHotkey(string? hotkey) {
        if (!string.IsNullOrWhiteSpace(hotkey)) {
            foreach (string allowed in AllowedPauseHotkeys) {
                if (string.Equals(allowed, hotkey.Trim(), StringComparison.OrdinalIgnoreCase))
                    return allowed;
            }
        }
        return "Ctrl+Alt+U";
    }

    internal static string NormalizeSelectionMode(string? mode) {
        if (!string.IsNullOrWhiteSpace(mode)) {
            foreach (string allowed in AllowedSelectionModes) {
                if (string.Equals(allowed, mode.Trim(), StringComparison.OrdinalIgnoreCase))
                    return allowed;
            }
        }
        return "auto";
    }

    /// <summary>Lower-case exe file names, ".exe" appended when no extension, invalid and duplicate names dropped.</summary>
    internal static List<string> NormalizeApps(IEnumerable<string?> names) {
        var result = new List<string>();
        foreach (var raw in names) {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string name = raw.Trim().Trim('"');
            int slash = name.LastIndexOfAny(new[] { '\\', '/' });
            if (slash >= 0) name = name[(slash + 1)..];
            if (name.Length == 0 || name.Length > 200 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
            if (!Path.HasExtension(name)) name += ".exe";
            name = name.ToLowerInvariant();
            if (!result.Contains(name)) result.Add(name);
            if (result.Count == MaxExcludedApps) break;
        }
        return result;
    }
}
