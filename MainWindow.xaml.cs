using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace UzbekKeyboard;

public partial class MainWindow : Window
{
    private readonly KeyboardMapper mapper = new();
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private AppLanguage language = AppLanguage.Uzbek;
    private string? error;
    private string? lastState;
    private bool ready;
    private bool closing;
    private bool initializing;
    private AppSettings settings = AppSettings.Load();
    private bool hotkeyRegistered;
    private const int WM_HOTKEY = 0x312, HotkeyPause = 1, HotkeyPauseCancel = 2;

    public MainWindow()
    {
        InitializeComponent();
        // Fit the usable desktop in WPF's DPI-independent units.
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, Math.Max(360, workArea.Width - 24));
        Height = Math.Min(Height, Math.Max(360, workArea.Height - 24));
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);
        mapper.SetExcludedApps(settings.ExcludedApps);
        SourceInitialized += (_, _) => RegisterPauseHotkey();
        Loaded += async (_, _) => await InitializeBrowserAsync();
        // State is pushed on events only: toggle, pause, language, errors, and layout/foreground changes from the mapper.
        mapper.StateChanged += () => Dispatcher.BeginInvoke(() => SendState());
    }

    private async Task InitializeBrowserAsync()
    {
        if (initializing || closing) return;
        initializing = true;
        Retry.Visibility = Visibility.Collapsed;
        Loading.Visibility = Visibility.Visible;
        try
        {
            string ui = Path.Combine(AppContext.BaseDirectory, "WebUI");
            if (!File.Exists(Path.Combine(ui, "index.html")))
                throw new FileNotFoundException("WebUI/index.html topilmadi / Не найден. Extract the entire ZIP before starting.");
            string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YangiLotinKlaviatura", "WebView2");
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
            core.ProcessFailed += (_, _) =>
            {
                ready = false;
                mapper.Stop();
                ShowFailure("Interfeys to‘xtadi / Интерфейс остановлен. Restart the app.");
            };
            core.NavigationCompleted += (_, e) =>
            {
                if (!e.IsSuccess) ShowFailure("Interfeys ochilmadi / Не удалось загрузить интерфейс: " + e.WebErrorStatus);
            };
            Browser.Source = new Uri(LocalUiNavigation.DocumentUrl);
        }
        catch (Exception ex)
        {
            ShowFailure("Microsoft Edge WebView2 Runtime kerak / Требуется Microsoft Edge WebView2 Runtime.\n" + ex.Message);
        }
        finally { initializing = false; }
    }

    private void ShowFailure(string message)
    {
        if (closing) return;
        Loading.Visibility = Visibility.Visible;
        LoadingText.Text = message;
        Retry.Visibility = Visibility.Visible;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Only our bundled page may host the bridge. No remote pages or arbitrary native commands.
        if (!LocalUiNavigation.IsTrustedDocument(e.Uri))
            e.Cancel = true;
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (closing || !LocalUiNavigation.IsTrustedDocument(e.Source)) return;
        try
        {
            if (e.WebMessageAsJson.Length > 1_500_000) return;
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var data = document.RootElement;
            if (!data.TryGetProperty("action", out var action) || action.ValueKind != JsonValueKind.String) return;
            switch (action.GetString())
            {
                case "ready":
                    if (!ready)
                    {
                        ready = true;
                        SetEnabled(true);
                    }
                    Loading.Visibility = Visibility.Collapsed;
                    SendState(force: true);
                    break;
                case "setEnabled" when ready:
                    if (data.TryGetProperty("enabled", out var enabled) &&
                        enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        SetEnabled(enabled.GetBoolean());
                    break;
                case "setPaused" when ready:
                    if (data.TryGetProperty("paused", out var paused) &&
                        paused.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        SetPaused(paused.GetBoolean());
                    break;
                case "setExcludedApps" when ready:
                    if (data.TryGetProperty("apps", out var apps) && apps.ValueKind == JsonValueKind.Array)
                    {
                        var list = AppSettings.NormalizeApps(apps.EnumerateArray()
                            .Where(a => a.ValueKind == JsonValueKind.String).Select(a => a.GetString()));
                        settings.ExcludedApps = list;
                        mapper.SetExcludedApps(list);
                        try { settings.Save(); error = null; } catch (Exception ex) { error = ex.Message; }
                        SendState(force: true);
                    }
                    break;
                case "setLanguage" when ready:
                    if (data.TryGetProperty("language", out var lang) && lang.ValueKind == JsonValueKind.String)
                    {
                        language = lang.GetString() == "ru" ? AppLanguage.Russian : AppLanguage.Uzbek;
                        SendState(force: true);
                    }
                    break;
                case "settings" when ready:
                    try
                    {
                        Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
                        error = null;
                    }
                    catch (Exception ex) { error = ex.Message; }
                    SendState(force: true);
                    break;
                case "convert" when ready:
                    if (!data.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String ||
                        !data.TryGetProperty("id", out var id) || !id.TryGetInt32(out int requestId)) break;
                    string input = text.GetString() ?? "";
                    Post(new { type = "converted", id = requestId,
                        text = input.Length <= 200_000 ? TextConverter.Convert(input) : input,
                        error = input.Length > 200_000 ? "200 000 belgigacha / До 200 000 символов" : null });
                    break;
            }
        }
        catch (JsonException) { /* Malformed messages do not reach native operations. */ }
    }

    private void SetEnabled(bool enabled)
    {
        mapper.ResetPending();
        try
        {
            if (enabled) mapper.Start(); else mapper.Stop();
            error = null;
        }
        catch (Exception ex) { mapper.Stop(); error = ex.Message; }
        SendState(force: true);
    }

    private void RegisterPauseHotkey()
    {
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WndProc);
        const uint modifiers = 0x1 | 0x2 | 0x4000; // MOD_ALT | MOD_CONTROL | MOD_NOREPEAT
        // With Ctrl held, Pause is often reported as VK_CANCEL, so both are registered.
        bool pause = RegisterHotKey(handle, HotkeyPause, modifiers, 0x13);
        bool cancel = RegisterHotKey(handle, HotkeyPauseCancel, modifiers, 0x03);
        hotkeyRegistered = pause || cancel;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() is HotkeyPause or HotkeyPauseCancel)
        {
            SetPaused(!mapper.Paused);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void SetPaused(bool paused)
    {
        mapper.Paused = paused;
        SendState(force: true);
    }

    private void SendState(bool force = false)
    {
        if (!ready || closing) return;
        var strings = UiStrings.Get(language);
        Title = strings.WindowTitle;
        string status = error != null ? strings.StatusErrorPrefix + error :
            !mapper.Enabled ? strings.StatusDisabled :
            mapper.Paused ? strings.StatusPaused :
            mapper.ElevatedApp is string elevatedApp ? string.Format(strings.StatusElevated, elevatedApp) :
            mapper.LastInputLanguage is ushort lang && lang != 0x0443 ?
                (language == AppLanguage.Uzbek ? "Win + Space: O‘zbek (lotin) tilini tanlang" : "Win + Пробел: выберите узбекский (латиница)") :
                mapper.ExcludedApp is string excludedApp ? string.Format(strings.StatusExcluded, excludedApp) :
                strings.StatusEnabled;
        string json = JsonSerializer.Serialize(new
        {
            type = "state", language = language == AppLanguage.Russian ? "ru" : "uz",
            enabled = mapper.Enabled, paused = mapper.Paused, excludedApps = settings.ExcludedApps,
            hotkey = hotkeyRegistered ? null : strings.HotkeyUnavailable, status, error, strings
        }, jsonOptions);
        if (force || json != lastState)
        {
            lastState = json;
            Browser.CoreWebView2.PostWebMessageAsJson(json);
        }
    }

    private void Post(object message) => Browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, jsonOptions));
    private async void Retry_Click(object sender, RoutedEventArgs e) => await InitializeBrowserAsync();
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        closing = true;
        ready = false;
        mapper.Stop();
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(handle, HotkeyPause);
        UnregisterHotKey(handle, HotkeyPauseCancel);
        Browser.Dispose();
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
