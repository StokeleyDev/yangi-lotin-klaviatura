using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;

namespace UzbekKeyboard;

public partial class MainWindow : Window {
    private readonly KeyboardMapper mapper = new();
    private readonly System.Windows.Threading.DispatcherTimer languageStatusTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private AppLanguage currentLanguage = AppLanguage.Uzbek;
    private string? lastErrorMessage = null;
    private string? settingsError = null;

    public int BackdropHResult { get; private set; } = -1;
    public bool IsBackdropActive { get; private set; } = false;
    public string BackdropStatus { get; private set; } = "Not initialized";

    public MainWindow() {
        InitializeComponent();
        ApplyGlassAccessibility();
        ApplyLanguage(AppLanguage.Uzbek);
        SetAutomaticInput(true);
        languageStatusTimer.Tick += (_, _) => UpdateStatusDisplay();
        languageStatusTimer.Start();
    }

    private void ApplyGlassAccessibility() {
        // Solid system surfaces preserve legibility in Windows high contrast.
        if (!SystemParameters.HighContrast) return;
        Resources["CardBackground"] = SystemColors.WindowBrush;
        Resources["CardBorder"] = SystemColors.WindowTextBrush;
        Resources["TextPrimary"] = SystemColors.WindowTextBrush;
        Resources["TextSecondary"] = SystemColors.WindowTextBrush;
        Resources["GlassSurface"] = SystemColors.ControlBrush;
        Resources["GlassBlue"] = SystemColors.HighlightBrush;
    }

    private void ApplyLanguage(AppLanguage lang) {
        currentLanguage = lang;
        var s = UiStrings.Get(lang);

        Title = s.WindowTitle;
        if (WindowTitleText != null) WindowTitleText.Text = s.WindowTitle;
        if (HeaderTitleText != null) HeaderTitleText.Text = s.HeaderTitle;
        if (HeaderSubtitleText != null) HeaderSubtitleText.Text = s.HeaderSubtitle;
        if (LayoutBadgeText != null) LayoutBadgeText.Text = s.LayoutBadge;
        if (StatusCardTitleText != null) StatusCardTitleText.Text = s.StatusCardTitle;
        if (KeysCardTitleText != null) KeysCardTitleText.Text = s.KeysCardTitle;
        if (KeysCardSubtitleText != null) KeysCardSubtitleText.Text = s.KeysCardSubtitle;
        if (InputCardTitleText != null) InputCardTitleText.Text = s.InputCardTitle;
        if (Placeholder != null) Placeholder.Text = s.InputPlaceholder;
        if (HintTextBlock != null) HintTextBlock.Text = s.HintText;
        if (SettingsButton != null) {
            SettingsButton.Content = s.SettingsButtonText;
            SettingsButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.SettingsAutomationName);
        }

        // Update Accessible tooltips & automation names on traffic lights
        if (CloseButton != null) {
            CloseButton.ToolTip = s.CloseAutomationName;
            CloseButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.CloseAutomationName);
        }
        if (MinimizeButton != null) {
            MinimizeButton.ToolTip = s.MinimizeAutomationName;
            MinimizeButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.MinimizeAutomationName);
        }
        if (MaximizeButton != null) {
            MaximizeButton.ToolTip = s.MaximizeAutomationName;
            MaximizeButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.MaximizeAutomationName);
        }

        if (TestInput != null) {
            TestInput.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.InputAutomationName);
        }

        if (LangUz != null) {
            LangUz.IsChecked = lang == AppLanguage.Uzbek;
            LangUz.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.LangUzAutomationName);
        }
        if (LangRu != null) {
            LangRu.IsChecked = lang == AppLanguage.Russian;
            LangRu.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.LangRuAutomationName);
        }

        if (ConvertInfo != null) ConvertInfo.Text = lang == AppLanguage.Uzbek ? "sh → ş · ch → ç · o' → ö · g' → ğ   |   Bekor qilish: Backspace" : "sh → ş · ch → ç · o' → ö · g' → ğ   |   Отмена: Backspace";
        if (ConvertButton != null) ConvertButton.Content = lang == AppLanguage.Uzbek ? "Matnni o‘zgartirish" : "Преобразовать текст";
        if (KeysCardSubtitleText != null) KeysCardSubtitleText.Text = lang == AppLanguage.Uzbek ? "Odatiy yozing: sh → ş, ch → ç, o' → ö, g' → ğ" : "Пишите привычно: sh → ş, ch → ç, o' → ö, g' → ğ";
        if (StatusCardTitleText != null) StatusCardTitleText.Text = lang == AppLanguage.Uzbek ? "Avtomatik almashtirish" : "Автоматическая замена";
        if (InputCardTitleText != null) InputCardTitleText.Text = lang == AppLanguage.Uzbek ? "Tayyor matnni o‘zgartirish" : "Преобразование готового текста";
        UpdateStatusDisplay();
    }

    private void SetAutomaticInput(bool enable) {
        mapper.ResetPending();
        try { if (enable) mapper.Start(); else mapper.Stop(); lastErrorMessage = null; }
        catch (Exception ex) { mapper.Stop(); lastErrorMessage = ex.Message; }
        UpdateStatusDisplay();
    }

    private void Convert_Click(object sender, RoutedEventArgs e) {
        TestInput.Text = TextConverter.Convert(TestInput.Text);
        TestInput.CaretIndex = TestInput.Text.Length;
        TestInput.Focus();
    }

    private void UpdateStatusDisplay() {
        var s = UiStrings.Get(currentLanguage);
        if (ToggleButton == null || StatusText == null) return;

        if (settingsError != null) {
            StatusText.Text = s.SettingsErrorPrefix + settingsError;
        } else if (lastErrorMessage != null) {
            StatusText.Text = s.StatusErrorPrefix + lastErrorMessage;
        } else if (mapper.Enabled) {
            StatusText.Text = mapper.LastInputLanguage is ushort lang && lang != 0x0443
                ? (currentLanguage == AppLanguage.Uzbek ? "Win + Space: O‘zbek (lotin) tilini tanlang" : "Win + Пробел: выберите узбекский (латиница)")
                : s.StatusEnabled;
        } else {
            StatusText.Text = s.StatusDisabled;
        }
        ToggleButton.Content = mapper.Enabled ? s.ToggleActionDisable : s.ToggleActionEnable;
        ToggleButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, s.ToggleAutomationName);
    }

    private void LangUz_Click(object sender, RoutedEventArgs e) {
        ApplyLanguage(AppLanguage.Uzbek);
    }

    private void LangRu_Click(object sender, RoutedEventArgs e) {
        ApplyLanguage(AppLanguage.Russian);
    }

    private void Toggle_Click(object sender, RoutedEventArgs e) => SetAutomaticInput(!mapper.Enabled);

    private void Settings_Click(object sender, RoutedEventArgs e) {
        try {
            Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
            settingsError = null;
        } catch (Exception ex) {
            settingsError = ex.Message;
        }
        UpdateStatusDisplay();
    }

    private void TitleDrag(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left) {
            if (e.ClickCount == 2) {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            } else if (WindowState == WindowState.Normal) {
                try { DragMove(); } catch (System.InvalidOperationException) { }
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void TestInput_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (Placeholder != null && TestInput != null) {
            Placeholder.Visibility = string.IsNullOrEmpty(TestInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) { languageStatusTimer.Stop(); mapper.Stop(); }

    protected override void OnSourceInitialized(EventArgs e) {
        base.OnSourceInitialized(e);
        ApplySystemBackdrop();
    }

    private void ApplySystemBackdrop() {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        bool forceFallback = Environment.GetCommandLineArgs().Any(a => 
            a.Equals("--fallback", StringComparison.OrdinalIgnoreCase) || 
            a.Equals("--force-fallback", StringComparison.OrdinalIgnoreCase));

        // Windows 11 22H2 (Build 22621) is required for documented DWMWA_SYSTEMBACKDROP_TYPE (38)
        bool isWindows11_22H2_OrGreater = Environment.OSVersion.Platform == PlatformID.Win32NT &&
                                          Environment.OSVersion.Version.Major >= 10 &&
                                          Environment.OSVersion.Version.Build >= 22621;

        bool highContrast = SystemParameters.HighContrast;

        bool compositionEnabled = false;
        try {
            DwmIsCompositionEnabled(out compositionEnabled);
        } catch {
            compositionEnabled = false;
        }

        bool transparencyEnabled = false;
        try {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null) {
                object? val = key.GetValue("EnableTransparency");
                if (val is int intVal) {
                    transparencyEnabled = intVal != 0;
                }
            }
        } catch {
            // Treat registry read error safely by falling back to opaque
            transparencyEnabled = false;
        }

        if (forceFallback) {
            ApplyFallbackSurface("CLI --fallback flag requested");
            return;
        }

        if (!isWindows11_22H2_OrGreater) {
            ApplyFallbackSurface($"Unsupported OS version (Build {Environment.OSVersion.Version.Build} < 22621)");
            return;
        }

        if (highContrast) {
            ApplyFallbackSurface("System high contrast mode active");
            return;
        }

        if (!compositionEnabled) {
            ApplyFallbackSurface("DWM composition disabled");
            return;
        }

        if (!transparencyEnabled) {
            ApplyFallbackSurface("Windows transparency disabled in user settings");
            return;
        }

        try {
            var source = HwndSource.FromHwnd(hwnd);
            if (source?.CompositionTarget != null) {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }

            // Extend glass frame into client area and verify HRESULT
            var margins = new MARGINS(-1, -1, -1, -1);
            int hrExtend = DwmExtendFrameIntoClientArea(hwnd, ref margins);
            if (hrExtend != 0) {
                BackdropHResult = hrExtend;
                ApplyFallbackSurface($"DwmExtendFrameIntoClientArea failed with HRESULT 0x{hrExtend:X8}");
                return;
            }

            // Set window corner preference to rounded (12px)
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // Set light theme backdrop
            int darkMode = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Apply Desktop Acrylic backdrop (DWMSBT_TRANSIENTWINDOW = 3)
            int backdropType = DWMSBT_TRANSIENTWINDOW;
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            BackdropHResult = hr;

            if (hr == 0) {
                IsBackdropActive = true;
                BackdropStatus = "Desktop Acrylic (DWMSBT_TRANSIENTWINDOW=3) active";
                if (WindowRoot != null) {
                    WindowRoot.Background = new SolidColorBrush(Color.FromArgb(0xF0, 0xE9, 0xF0, 0xFC));
                }
            } else {
                ApplyFallbackSurface($"DwmSetWindowAttribute failed with HRESULT 0x{hr:X8}");
            }
        } catch (Exception ex) {
            ApplyFallbackSurface($"Exception applying backdrop: {ex.Message}");
        }
    }

    private void ApplyFallbackSurface(string reason) {
        IsBackdropActive = false;
        BackdropStatus = $"Fallback active: {reason}";
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero) {
            try {
                var source = HwndSource.FromHwnd(hwnd);
                if (source?.CompositionTarget != null) {
                    source.CompositionTarget.BackgroundColor = Color.FromRgb(0xE9, 0xF0, 0xFC);
                }
            } catch { }
        }
        if (WindowRoot != null) {
            WindowRoot.Background = new SolidColorBrush(Color.FromRgb(0xE9, 0xF0, 0xFC));
        }
    }

    // Win32 Interop for DWM Desktop Acrylic & System Backdrops
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll")]
    private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
        public MARGINS(int left, int right, int top, int bottom) {
            cxLeftWidth = left; cxRightWidth = right; cyTopHeight = top; cyBottomHeight = bottom;
        }
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;

    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Desktop Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4;
}





