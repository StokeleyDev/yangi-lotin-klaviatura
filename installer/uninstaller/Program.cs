using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace YangiUninstall;

public static class Program {
    [STAThread]
    public static int Main() {
        var app = new Application();
        bool ru = string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase);

        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YangiLotinKlaviatura");

        if (!string.Equals(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase)) {
            MessageBox.Show(
                ru ? "Запустите удаление из папки установленной программы." : "O‘chirishni o‘rnatilgan dastur papkasidan ishga tushiring.",
                "Yangi lotin klaviatura");
            return 1;
        }

        string exe = Path.Combine(root, "Yangi lotin klaviatura.exe");
        foreach (var process in Process.GetProcessesByName("Yangi lotin klaviatura")) {
            using (process) {
                string? path;
                try { path = process.MainModule?.FileName; } catch { continue; }
                if (string.Equals(path, exe, StringComparison.OrdinalIgnoreCase)) {
                    MessageBox.Show(
                        ru ? "Закройте Yangi lotin klaviatura перед удалением." : "O‘chirishdan oldin Yangi lotin klaviatura dasturini yoping.",
                        "Yangi lotin klaviatura",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return 1;
                }
            }
        }

        if (MessageBox.Show(
                ru ? "Удалить Yangi lotin klaviatura?" : "Yangi lotin klaviatura dasturini o‘chirish?",
                ru ? "Удаление" : "O‘chirish",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return 0;

        try {
            using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                string? val = run?.GetValue("YangiLotinKlaviatura") as string;
                if (string.Equals(val, "\"" + exe + "\"", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(val, "\"" + exe + "\" --tray", StringComparison.OrdinalIgnoreCase))
                    run?.DeleteValue("YangiLotinKlaviatura", false);
            }

            string shortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Yangi lotin klaviatura.lnk");
            if (File.Exists(shortcut)) {
                var t = Type.GetTypeFromProgID("WScript.Shell");
                if (t != null) {
                    dynamic shell = Activator.CreateInstance(t)!;
                    dynamic link = shell.CreateShortcut(shortcut);
                    string target = link.TargetPath;
                    if (string.Equals(target, exe, StringComparison.OrdinalIgnoreCase))
                        File.Delete(shortcut);
                }
            }

            string[] files = {
                "Yangi lotin klaviatura.exe", "Yangi lotin klaviatura.dll",
                "Yangi lotin klaviatura.deps.json", "Yangi lotin klaviatura.runtimeconfig.json",
                "Yangi lotin klaviatura.pdb", "Microsoft.Web.WebView2.Core.dll",
                "Microsoft.Web.WebView2.Wpf.dll", "Microsoft.Web.WebView2.WinForms.dll",
                "Microsoft.Web.WebView2.Core.xml", "Microsoft.Web.WebView2.Wpf.xml",
                "Microsoft.Web.WebView2.WinForms.xml", "README.md"
            };
            foreach (string name in files) {
                string path = Path.Combine(root, name);
                if (File.Exists(path)) File.Delete(path);
            }

            foreach (string name in new[] { "WebUI", "runtimes", "WebView2" }) {
                string path = Path.GetFullPath(Path.Combine(root, name));
                if (!path.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new IOException(ru ? "Недопустимый путь удаления." : "O‘chirish yo‘li noto‘g‘ri.");
                if (Directory.Exists(path))
                    Directory.Delete(path, (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0);
            }

            using (var uninstall = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true)) {
                using var own = uninstall?.OpenSubKey("YangiLotinKlaviatura");
                bool matches = string.Equals(own?.GetValue("InstallLocation") as string, root, StringComparison.OrdinalIgnoreCase);
                own?.Close();
                if (matches) uninstall?.DeleteSubKeyTree("YangiLotinKlaviatura", false);
            }

            string current = Environment.ProcessPath ?? throw new InvalidOperationException(ru ? "Не найден файл удаления." : "O‘chirish fayli topilmadi.");
            string quotedCurrent = current.Replace("'", "''");
            string quotedRoot = root.Replace("'", "''");
            int pid = Environment.ProcessId;
            string script = $"Wait-Process -Id {pid} -ErrorAction SilentlyContinue; Remove-Item -LiteralPath '{quotedCurrent}' -Force -ErrorAction SilentlyContinue; $expected=Join-Path $env:LOCALAPPDATA 'YangiLotinKlaviatura'; $target='{quotedRoot}'; if ([IO.Path]::GetFullPath($target).TrimEnd('\\') -ieq [IO.Path]::GetFullPath($expected).TrimEnd('\\') -and (Test-Path -LiteralPath $target)) {{ Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction SilentlyContinue }}";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"), "-NoProfile -NonInteractive -EncodedCommand " + encoded) {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            MessageBox.Show(ru ? "Программа удалена." : "Dastur o‘chirildi.", "Yangi lotin klaviatura");
            return 0;
        } catch (Exception ex) {
            MessageBox.Show((ru ? "Не удалось завершить удаление: " : "O‘chirish yakunlanmadi: ") + ex.Message, "Yangi lotin klaviatura", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }
}
