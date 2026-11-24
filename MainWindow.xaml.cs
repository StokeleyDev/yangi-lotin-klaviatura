using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace UzbekKeyboard;

public partial class MainWindow : Window {
    private const int WM_HOTKEY = 0x312;
    private const int HotkeyPause = 1, HotkeyPauseCancel = 2, HotkeyConvertSelection = 3;
    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;
    private const string ReleasesBaseUrl = "https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases";
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/StokeleyDev/yangi-lotin-klaviatura/releases/latest";

    private readonly KeyboardMapper mapper = new();
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly uint showMessageId;
    private readonly bool startInTray;

    private AppSettings settings = AppSettings.Load();
    private AppLanguage language = AppLanguage.Uzbek;
    private string? error;
    private string? lastState;
    private bool ready;
    private bool closing;
    private bool initializing;
    private bool browserStarted;
    private bool exiting;
    private bool hotkeyRegistered;
    private bool selectionHotkeyRegistered;
    private bool updateChecked;
    private string? updateVersion;
    private string? updateUrl;

    private readonly System.Windows.Forms.NotifyIcon tray = new();
    private readonly System.Windows.Forms.ToolStripMenuItem trayOpen = new(),
        trayEnabled = new(),
        trayWorkOnEnglish = new(),
        trayConvertSelection = new(),
        trayPause = new(),
        trayExit = new();

    public MainWindow() : this(false, 0) { }

    public MainWindow(bool startInTray, uint showMessageId) {
        this.startInTray = startInTray;
        this.showMessageId = showMessageId;

        InitializeComponent();

        // Fit the usable desktop in WPF's DPI-independent units.
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, Math.Max(360, workArea.Width - 24));
        Height = Math.Min(Height, Math.Max(360, workArea.Height - 24));
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);

        language = settings.Language == "ru" ? AppLanguage.Russian : AppLanguage.Uzbek;
        mapper.SetExcludedApps(settings.ExcludedApps);
        mapper.WorkOnEnglish = settings.WorkOnEnglish;
        mapper.AltGrShortcuts = settings.AltGrShortcuts;
        mapper.SmartTutuq = settings.SmartTutuq;

        CreateTray();
        SetEnabled(settings.Enabled, save: false);

        if (Application.Current != null)
            Application.Current.SessionEnding += (_, _) => exiting = true;

        SourceInitialized += (_, _) => {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            RegisterHotkeys();
        };

        if (!startInTray)
            Loaded += async (_, _) => await EnsureBrowserStartedAsync();

        // State is pushed on events only: toggle, pause, language, errors, and layout/foreground changes from the mapper.
        mapper.StateChanged += () => Dispatcher.BeginInvoke(() => SendState());
    }

    private async Task EnsureBrowserStartedAsync() {
        if (browserStarted || initializing || closing) return;
        browserStarted = true;
        StartUpdateCheck();
        await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync() {
        if (initializing || closing) return;
        initializing = true;
        Retry.Visibility = Visibility.Collapsed;
        Loading.Visibility = Visibility.Visible;
        try {
            string ui = Path.Combine(AppContext.BaseDirectory, "WebUI");
            if (!File.Exists(Path.Combine(ui, "index.html")))
                throw new FileNotFoundException("WebUI/index.html topilmadi / Не найден. Extract the entire ZIP before starting.");
            string userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YangiLotinKlaviatura",
                "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, userData);
            if (closing) return;
            await Browser.EnsureCoreWebView2Async(environment);
            if (closing) return;
            var core = Browser.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.SetVirtualHostNameToFolderMapping(LocalUiNavigation.Host, ui, CoreWebView2HostResourceAccessKind.DenyCors);
            core.WebMessageReceived -= OnWebMessage;
            core.WebMessageReceived += OnWebMessage;
            core.NavigationStarting -= OnNavigationStarting;
            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.ProcessFailed += (_, _) => {
                ready = false;
                mapper.Stop();
                ShowFailure("Interfeys to‘xtadi / Интерфейс остановлен. Restart the app.");
            };
            core.NavigationCompleted += (_, e) => {
                if (!e.IsSuccess) ShowFailure("Interfeys ochilmadi / Не удалось загрузить интерфейс: " + e.WebErrorStatus);
            };
            Browser.Source = new Uri(LocalUiNavigation.DocumentUrl);
        } catch (Exception ex) {
            ShowFailure("Microsoft Edge WebView2 Runtime kerak / Требуется Microsoft Edge WebView2 Runtime.\n" + ex.Message);
        } finally {
            initializing = false;
        }
    }

    private void ShowFailure(string message) {
        if (closing) return;
        Loading.Visibility = Visibility.Visible;
        LoadingText.Text = message;
        Retry.Visibility = Visibility.Visible;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e) {
        // Only our bundled page may host the bridge. No remote pages or arbitrary native commands.
        if (!LocalUiNavigation.IsTrustedDocument(e.Uri))
            e.Cancel = true;
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
        if (closing || !LocalUiNavigation.IsTrustedDocument(e.Source)) return;
        try {
            if (e.WebMessageAsJson.Length > 1_500_000) return;
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var data = document.RootElement;
            if (!data.TryGetProperty("action", out var action) || action.ValueKind != JsonValueKind.String) return;
            switch (action.GetString()) {
                case "ready":
                    ready = true;
                    Loading.Visibility = Visibility.Collapsed;
                    SendState(force: true);
                    break;
                case "setEnabled" when ready:
                    if (data.TryGetProperty("enabled", out var enabled) &&
                        enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        SetEnabled(enabled.GetBoolean(), save: true);
                    break;
                case "setPaused" when ready:
                    if (data.TryGetProperty("paused", out var paused) &&
                        paused.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        SetPaused(paused.GetBoolean());
                    break;
                case "setWorkOnEnglish" when ready:
                    if (data.TryGetProperty("enabled", out var enMode) &&
                        enMode.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        SetWorkOnEnglish(enMode.GetBoolean());
                    break;
                case "setAltGrShortcuts" when ready:
                    if (data.TryGetProperty("enabled", out var altGr) &&
                        altGr.ValueKind is JsonValueKind.True or JsonValueKind.False) {
                        settings.AltGrShortcuts = altGr.GetBoolean();
                        mapper.AltGrShortcuts = settings.AltGrShortcuts;
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "setSmartTutuq" when ready:
                    if (data.TryGetProperty("enabled", out var tutuq) &&
                        tutuq.ValueKind is JsonValueKind.True or JsonValueKind.False) {
                        settings.SmartTutuq = tutuq.GetBoolean();
                        mapper.SmartTutuq = settings.SmartTutuq;
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "setTheme" when ready:
                    if (data.TryGetProperty("theme", out var themeProp) && themeProp.TryGetInt32(out int themeVal)) {
                        settings.Theme = Math.Clamp(themeVal, 0, 2);
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "setPauseHotkey" when ready:
                    if (data.TryGetProperty("hotkey", out var hk) && hk.ValueKind == JsonValueKind.String) {
                        settings.PauseHotkey = AppSettings.NormalizePauseHotkey(hk.GetString());
                        SaveSettings();
                        RegisterHotkeys();
                        SendState(force: true);
                    }
                    break;
                case "setSelectionHotkey" when ready:
                    if (data.TryGetProperty("enabled", out var selHk) &&
                        selHk.ValueKind is JsonValueKind.True or JsonValueKind.False) {
                        settings.SelectionHotkeyEnabled = selHk.GetBoolean();
                        SaveSettings();
                        RegisterHotkeys();
                        SendState(force: true);
                    }
                    break;
                case "setSelectionConvertMode" when ready:
                    if (data.TryGetProperty("mode", out var selMode) && selMode.ValueKind == JsonValueKind.String) {
                        settings.SelectionConvertMode = AppSettings.NormalizeSelectionMode(selMode.GetString());
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "setExcludedApps" when ready:
                    if (data.TryGetProperty("apps", out var apps) && apps.ValueKind == JsonValueKind.Array) {
                        var list = AppSettings.NormalizeApps(apps.EnumerateArray()
                            .Where(a => a.ValueKind == JsonValueKind.String).Select(a => a.GetString()));
                        settings.ExcludedApps = list;
                        mapper.SetExcludedApps(list);
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "setLanguage" when ready:
                    if (data.TryGetProperty("language", out var lang) && lang.ValueKind == JsonValueKind.String) {
                        language = lang.GetString() == "ru" ? AppLanguage.Russian : AppLanguage.Uzbek;
                        settings.Language = language == AppLanguage.Russian ? "ru" : "uz";
                        SaveSettings();
                        SendState(force: true);
                    }
                    break;
                case "settings" when ready:
                    try {
                        Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
                        error = null;
                    } catch (Exception ex) { error = ex.Message; }
                    SendState(force: true);
                    break;
                case "addUzbekLayout" when ready:
                    _ = AddUzbekLayoutAsync();
                    break;
                case "openUpdateUrl" when ready:
                    if (data.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String) {
                        string? candidate = urlProp.GetString();
                        if (!string.IsNullOrEmpty(candidate) && candidate.StartsWith(ReleasesBaseUrl, StringComparison.OrdinalIgnoreCase)) {
                            try { Process.Start(new ProcessStartInfo(candidate) { UseShellExecute = true }); }
                            catch (Exception ex) { error = ex.Message; SendState(force: true); }
                        }
                    }
                    break;
                case "convert" when ready:
                    if (!data.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String ||
                        !data.TryGetProperty("id", out var id) || !id.TryGetInt32(out int requestId)) break;
                    string input = text.GetString() ?? "";
                    string mode = data.TryGetProperty("mode", out var modeProp) && modeProp.ValueKind == JsonValueKind.String
                        ? modeProp.GetString() ?? "oldToNew"
                        : "oldToNew";
                    Post(new {
                        type = "converted",
                        id = requestId,
                        text = input.Length <= 200_000 ? TextConverter.Convert(input, mode) : input,
                        error = input.Length > 200_000 ? "200 000 belgigacha / До 200 000 символов" : null
                    });
                    break;
            }
        } catch (JsonException) { /* Malformed messages do not reach native operations. */ }
    }

    private void SaveSettings() {
        try {
            settings.Save();
            error = null;
        } catch (Exception ex) {
            error = ex.Message;
        }
    }

    private void SetEnabled(bool enabled, bool save) {
        mapper.ResetPending();
        try {
            if (enabled) mapper.Start(); else mapper.Stop();
            error = null;
        } catch (Exception ex) {
            mapper.Stop();
            error = ex.Message;
        }
        if (save) {
            settings.Enabled = mapper.Enabled;
            SaveSettings();
        }
        SendState(force: true);
    }

    private void SetWorkOnEnglish(bool enabled) {
        settings.WorkOnEnglish = enabled;
        mapper.WorkOnEnglish = enabled;
        SaveSettings();
        SendState(force: true);
    }

    private async Task AddUzbekLayoutAsync() {
        try {
            const string script = "$list = Get-WinUserLanguageList; if (-not ($list | Where-Object { $_.LanguageTag -like 'uz-Latn*' })) { $list.Add('uz-Latn-UZ'); Set-WinUserLanguageList $list -Force }";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            string ps = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
            using var proc = Process.Start(new ProcessStartInfo(ps, "-NoProfile -NonInteractive -EncodedCommand " + encoded) {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (proc != null) await proc.WaitForExitAsync();
            bool ok = proc?.ExitCode == 0;
            if (ready) {
                Post(new {
                    type = "layoutResult",
                    ok,
                    message = ok
                        ? (language == AppLanguage.Uzbek ? "O‘zbek (lotin) qo‘shildi" : "Узбекский (латиница) добавлен")
                        : (language == AppLanguage.Uzbek ? "Qo‘shib bo‘lmadi — Windows sozlamasini oching" : "Не удалось добавить — откройте настройки Windows")
                });
            }
        } catch (Exception ex) {
            if (ready) Post(new { type = "layoutResult", ok = false, message = ex.Message });
        }
    }

    private void StartUpdateCheck() {
        if (updateChecked) return;
        updateChecked = true;
        _ = Task.Run(async () => {
            try {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("YangiLotinKlaviatura/1.3");
                string body = await http.GetStringAsync(LatestReleaseApiUrl);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                string? tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
                string? htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(htmlUrl)) return;
                if (!htmlUrl.StartsWith(ReleasesBaseUrl, StringComparison.OrdinalIgnoreCase)) return;

                string cleanTag = tag.Trim().TrimStart('v', 'V');
                Version? current = Assembly.GetExecutingAssembly().GetName().Version;
                if (Version.TryParse(cleanTag, out Version? latest) && current != null) {
                    var normalizedCurrent = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
                    var normalizedLatest = new Version(latest.Major, latest.Minor, Math.Max(0, latest.Build));
                    if (normalizedLatest > normalizedCurrent) {
                        updateVersion = cleanTag;
                        updateUrl = htmlUrl;
                        _ = Dispatcher.BeginInvoke(() => SendState(force: true));
                    }
                }
            } catch { /* Silent failure for background update check. */ }
        });
    }

    private void CreateTray() {
        tray.Icon = Environment.ProcessPath is string exe ? System.Drawing.Icon.ExtractAssociatedIcon(exe) : null;
        tray.Text = "Yangi lotin klaviatura";
        trayOpen.Click += (_, _) => ShowFromTray();
        trayOpen.Font = new System.Drawing.Font(trayOpen.Font, System.Drawing.FontStyle.Bold);
        trayEnabled.Click += (_, _) => SetEnabled(!mapper.Enabled, save: true);
        trayWorkOnEnglish.Click += (_, _) => SetWorkOnEnglish(!settings.WorkOnEnglish);
        trayConvertSelection.Click += (_, _) => _ = ConvertSelectionFromHotkeyAsync();
        trayPause.Click += (_, _) => SetPaused(!mapper.Paused);
        trayExit.Click += (_, _) => { exiting = true; Close(); };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            trayOpen,
            trayEnabled,
            trayWorkOnEnglish,
            trayConvertSelection,
            trayPause,
            new System.Windows.Forms.ToolStripSeparator(),
            trayExit
        });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => ShowFromTray();
        UpdateTray();
        tray.Visible = true;
    }

    private void UpdateTray() {
        var strings = UiStrings.Get(language);
        trayOpen.Text = strings.TrayOpen;
        trayEnabled.Text = strings.TrayEnabled;
        trayEnabled.Checked = mapper.Enabled;
        trayWorkOnEnglish.Text = strings.TrayWorkOnEnglish;
        trayWorkOnEnglish.Checked = settings.WorkOnEnglish;
        trayConvertSelection.Text = strings.TrayConvertSelection;
        trayPause.Text = string.Format(strings.TrayPauseFormat, settings.PauseHotkey);
        trayPause.Checked = mapper.Paused;
        trayExit.Text = strings.TrayExit;
        tray.Text = !mapper.Enabled ? strings.TrayTooltipDisabled :
            mapper.Paused ? strings.TrayTooltipPaused :
            strings.TrayTooltipActive;
    }

    private void ShowFromTray() {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        _ = EnsureBrowserStartedAsync();
    }

    private void RegisterHotkeys() {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        UnregisterHotKey(handle, HotkeyPause);
        UnregisterHotKey(handle, HotkeyPauseCancel);
        UnregisterHotKey(handle, HotkeyConvertSelection);

        string preset = AppSettings.NormalizePauseHotkey(settings.PauseHotkey);
        if (preset == "Ctrl+Alt+Pause") {
            uint mods = MOD_ALT | MOD_CONTROL | MOD_NOREPEAT;
            bool pause = RegisterHotKey(handle, HotkeyPause, mods, 0x13);
            bool cancel = RegisterHotKey(handle, HotkeyPauseCancel, mods, 0x03);
            hotkeyRegistered = pause || cancel;
        } else {
            (uint mods, uint vk) = preset switch {
                "Ctrl+Alt+P" => (MOD_ALT | MOD_CONTROL | MOD_NOREPEAT, 0x50u),
                "Ctrl+Shift+U" => (MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, 0x55u),
                _ => (MOD_ALT | MOD_CONTROL | MOD_NOREPEAT, 0x55u) // Ctrl+Alt+U
            };
            hotkeyRegistered = RegisterHotKey(handle, HotkeyPause, mods, vk);
        }

        selectionHotkeyRegistered = settings.SelectionHotkeyEnabled &&
            RegisterHotKey(handle, HotkeyConvertSelection, MOD_ALT | MOD_CONTROL | MOD_NOREPEAT, 0x4C); // Ctrl+Alt+L
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (showMessageId != 0 && msg == showMessageId) {
            ShowFromTray();
            handled = true;
            return IntPtr.Zero;
        }
        if (msg == WM_HOTKEY) {
            int id = wParam.ToInt32();
            if (id is HotkeyPause or HotkeyPauseCancel) {
                SetPaused(!mapper.Paused);
                handled = true;
            } else if (id == HotkeyConvertSelection) {
                _ = ConvertSelectionFromHotkeyAsync();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private async Task ConvertSelectionFromHotkeyAsync() {
        try {
            await SelectionConverter.ConvertSelectionAsync(mapper, settings.SelectionConvertMode, Dispatcher);
        } catch { /* Do not crash background hook host on transient clipboard errors. */ }
    }

    private void SetPaused(bool paused) {
        mapper.Paused = paused;
        SendState(force: true);
    }

    private void SendState(bool force = false) {
        if (closing) return;
        UpdateTray();
        if (!ready) return;
        var strings = UiStrings.Get(language);
        Title = strings.WindowTitle;
        string status = error != null ? strings.StatusErrorPrefix + error :
            !mapper.Enabled ? strings.StatusDisabled :
            mapper.Paused ? string.Format(strings.StatusPausedFormat, settings.PauseHotkey) :
            mapper.ElevatedApp is string elevatedApp ? string.Format(strings.StatusElevated, elevatedApp) :
            mapper.LastInputLanguage is ushort lang && !mapper.IsLayoutSupported(lang) ? strings.StatusNeedLayout :
            mapper.ExcludedApp is string excludedApp ? string.Format(strings.StatusExcluded, excludedApp) :
            mapper.LastInputLanguage is ushort activeLang && (activeLang & 0x03FF) == 0x0009 && mapper.WorkOnEnglish ? strings.StatusEnabledEn :
            strings.StatusEnabled;

        string json = JsonSerializer.Serialize(new {
            type = "state",
            language = language == AppLanguage.Russian ? "ru" : "uz",
            enabled = mapper.Enabled,
            paused = mapper.Paused,
            workOnEnglish = settings.WorkOnEnglish,
            altGrShortcuts = settings.AltGrShortcuts,
            smartTutuq = settings.SmartTutuq,
            theme = settings.Theme,
            pauseHotkey = settings.PauseHotkey,
            selectionHotkeyEnabled = settings.SelectionHotkeyEnabled,
            selectionConvertMode = settings.SelectionConvertMode,
            selectionHotkeyRegistered,
            excludedApps = settings.ExcludedApps,
            hotkey = hotkeyRegistered ? null : string.Format(strings.HotkeyUnavailableFormat, settings.PauseHotkey),
            updateVersion,
            updateUrl,
            status,
            error,
            strings
        }, jsonOptions);

        if (force || json != lastState) {
            lastState = json;
            Browser.CoreWebView2.PostWebMessageAsJson(json);
        }
    }

    private void Post(object message) =>
        Browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, jsonOptions));

    private async void Retry_Click(object sender, RoutedEventArgs e) => await InitializeBrowserAsync();

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
        if (!exiting) {
            // Closing the window keeps the app (and replacement) running in the tray; exit is in the tray menu.
            e.Cancel = true;
            Hide();
            return;
        }
        tray.Visible = false;
        tray.Dispose();
        closing = true;
        ready = false;
        mapper.Stop();
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero) {
            UnregisterHotKey(handle, HotkeyPause);
            UnregisterHotKey(handle, HotkeyPauseCancel);
            UnregisterHotKey(handle, HotkeyConvertSelection);
        }
        Browser.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
