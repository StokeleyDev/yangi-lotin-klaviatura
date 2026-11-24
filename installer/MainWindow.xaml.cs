using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace YangiInstaller;

public partial class MainWindow : Window {
    private static readonly string InstallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YangiLotinKlaviatura");

    private static readonly string AppPath = Path.Combine(InstallDir, "Yangi lotin klaviatura.exe");
    private const string WebView2ClientGuid = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
    private const string WebView2DownloadUrl = "https://developer.microsoft.com/en-us/microsoft-edge/webview2/";

    private bool completed;
    private bool isRussian;

    public MainWindow() {
        InitializeComponent();
        ApplyLanguage();
        CheckWebView2();
    }

    private void LangUz_Click(object sender, RoutedEventArgs e) {
        isRussian = false;
        ApplyLanguage();
    }

    private void LangRu_Click(object sender, RoutedEventArgs e) {
        isRussian = true;
        ApplyLanguage();
    }

    private void ApplyLanguage() {
        var activeBg = new SolidColorBrush(Color.FromRgb(0x08, 0x75, 0xEE));
        var activeFg = Brushes.White;
        var idleBg = new SolidColorBrush(Color.FromRgb(0xDC, 0xE5, 0xF5));
        var idleFg = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));

        LangUzButton.Background = isRussian ? idleBg : activeBg;
        LangUzButton.Foreground = isRussian ? idleFg : activeFg;
        LangRuButton.Background = isRussian ? activeBg : idleBg;
        LangRuButton.Foreground = isRussian ? activeFg : idleFg;

        Title = isRussian ? "Yangi lotin klaviatura — установка" : "Yangi lotin klaviatura — o‘rnatish";
        SubtitleText.Text = isRussian ? "Установка для Windows" : "Windows uchun o‘rnatish";

        if (completed) {
            CardTitle.Text = isRussian ? "Установка завершена" : "O‘rnatish tugallandi";
            CardDescription.Text = isRussian
                ? "Программа установлена, ярлык добавлен на рабочий стол."
                : "Dastur o‘rnatildi, ish stoliga yorliq qo‘shildi.";
            InstallButton.Content = isRussian ? "Завершить" : "Yakunlash";
        } else {
            CardTitle.Text = isRussian ? "Всё готово к установке" : "O‘rnatishga tayyor";
            CardDescription.Text = isRussian
                ? "Приложение будет установлено для текущего пользователя. Для работы нужны .NET Desktop Runtime 8 и Microsoft Edge WebView2 Runtime."
                : "Dastur joriy foydalanuvchi uchun o‘rnatiladi. Ishlash uchun .NET Desktop Runtime 8 va Microsoft Edge WebView2 Runtime talab qilinadi.";
            AutoStart.Content = isRussian ? "Запускать в трее вместе с Windows" : "Windows bilan birga trayda ishga tushirish";
            OptionDescription.Text = isRussian ? "Этот параметр можно выбрать при установке." : "Bu parametrni o‘rnatish vaqtida tanlash mumkin.";
            InstallButton.Content = isRussian ? "Установить" : "O‘rnatish";
        }

        WebViewWarningText.Text = isRussian
            ? "Microsoft Edge WebView2 Runtime не найден. Без него окно настроек не откроется."
            : "Microsoft Edge WebView2 Runtime topilmadi. Usiz sozlamalar oynasi ochilmaydi.";
        WebViewDownloadButton.Content = isRussian ? "Скачать WebView2 Runtime" : "WebView2 Runtime yuklab olish";
    }

    private void CheckWebView2() {
        bool found = HasWebView2(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\" + WebView2ClientGuid) ||
                     HasWebView2(Registry.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate\Clients\" + WebView2ClientGuid) ||
                     HasWebView2(Registry.CurrentUser, @"Software\Microsoft\EdgeUpdate\Clients\" + WebView2ClientGuid);
        WebViewWarning.Visibility = found ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool HasWebView2(RegistryKey root, string subkey) {
        try {
            using var key = root.OpenSubKey(subkey);
            return key?.GetValue("pv") is string pv && !string.IsNullOrWhiteSpace(pv);
        } catch {
            return false;
        }
    }

    private void DownloadWebView_Click(object sender, RoutedEventArgs e) {
        try { Process.Start(new ProcessStartInfo(WebView2DownloadUrl) { UseShellExecute = true }); }
        catch (Exception ex) { Status.Text = ex.Message; }
    }

    private void Install_Click(object sender, RoutedEventArgs e) {
        if (completed) { Close(); return; }
        try {
            foreach (var process in Process.GetProcessesByName("Yangi lotin klaviatura")) {
                using (process) {
                    string? path;
                    try { path = process.MainModule?.FileName; } catch { continue; }
                    if (string.Equals(path, AppPath, StringComparison.OrdinalIgnoreCase)) {
                        throw new IOException(isRussian
                            ? "Закройте Yangi lotin klaviatura перед обновлением."
                            : "Yangilashdan oldin Yangi lotin klaviatura dasturini yoping.");
                    }
                }
            }

            Directory.CreateDirectory(InstallDir);
            using Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip")
                ?? throw new InvalidOperationException(isRussian ? "Файлы приложения не найдены." : "Dastur fayllari topilmadi.");
            using var zip = new ZipArchive(source, ZipArchiveMode.Read);
            foreach (var item in zip.Entries) {
                if (string.IsNullOrEmpty(item.Name)) continue;
                string dest = Path.GetFullPath(Path.Combine(InstallDir, item.FullName));
                if (!dest.StartsWith(InstallDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(isRussian ? "Недопустимый путь в архиве." : "Arxivda noto‘g‘ri yo‘l.");
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                item.ExtractToFile(dest, true);
            }

            using var run = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")
                ?? throw new IOException(isRussian ? "Не удалось открыть настройки автозапуска." : "Avtoyuklash sozlamalarini ochib bo‘lmadi.");
            const string name = "YangiLotinKlaviatura";
            string runPlain = "\"" + AppPath + "\"";
            string runTray = "\"" + AppPath + "\" --tray";
            if (AutoStart.IsChecked == true) {
                run.SetValue(name, runTray);
            } else {
                string? existing = run.GetValue(name) as string;
                if (string.Equals(existing, runPlain, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(existing, runTray, StringComparison.OrdinalIgnoreCase))
                    run.DeleteValue(name, false);
            }

            string shortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Yangi lotin klaviatura.lnk");
            Type shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException(isRussian ? "Не удалось создать ярлык рабочего стола." : "Ish stoli yorlig‘ini yaratib bo‘lmadi.");
            dynamic shell = Activator.CreateInstance(shellType)!;
            if (File.Exists(shortcut)) {
                dynamic old = shell.CreateShortcut(shortcut);
                string target = old.TargetPath;
                if (!string.Equals(target, AppPath, StringComparison.OrdinalIgnoreCase)) {
                    throw new IOException(isRussian
                        ? "На рабочем столе уже есть другой ярлык Yangi lotin klaviatura. Переименуйте его и повторите установку."
                        : "Ish stolida boshqa Yangi lotin klaviatura yorlig‘i bor. Uni qayta nomlab, qayta urinib ko‘ring.");
                }
            }
            dynamic link = shell.CreateShortcut(shortcut);
            link.TargetPath = AppPath;
            link.WorkingDirectory = InstallDir;
            link.Description = "Yangi lotin klaviatura";
            link.IconLocation = AppPath + ",0";
            link.Save();

            using (var uninstall = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\YangiLotinKlaviatura")) {
                uninstall?.SetValue("DisplayName", "Yangi lotin klaviatura");
                uninstall?.SetValue("DisplayVersion", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "");
                uninstall?.SetValue("InstallLocation", InstallDir);
                uninstall?.SetValue("DisplayIcon", AppPath + ",0");
                uninstall?.SetValue("UninstallString", "\"" + Path.Combine(InstallDir, "Uninstall.exe") + "\"");
                uninstall?.SetValue("NoModify", 1, RegistryValueKind.DWord);
                uninstall?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }

            completed = true;
            OptionsCard.Height = OptionsCard.ActualHeight;
            AutoStart.Visibility = Visibility.Hidden;
            OptionDescription.Visibility = Visibility.Hidden;
            WebViewWarning.Visibility = Visibility.Collapsed;
            Status.Text = string.Empty;
            ApplyLanguage();
        } catch (Exception ex) {
            Status.Text = (isRussian ? "Установка не завершена: " : "O‘rnatish yakunlanmadi: ") + ex.Message;
        }
    }
}
