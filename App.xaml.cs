using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace UzbekKeyboard;

public partial class App : Application {
    private const string MutexName = @"Local\YangiLotinKlaviatura_SingleInstance";
    private const string ShowMessageName = "YangiLotinKlaviatura_Show";
    private static readonly IntPtr HWND_BROADCAST = new(0xffff);

    private Mutex? singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        uint showMessage = RegisterWindowMessage(ShowMessageName);
        singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew) {
            if (showMessage != 0)
                PostMessage(HWND_BROADCAST, showMessage, IntPtr.Zero, IntPtr.Zero);
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
            Shutdown(0);
            return;
        }

        bool startInTray = e.Args.Any(a =>
            a.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

        var window = new MainWindow(startInTray, showMessage);
        MainWindow = window;
        if (startInTray) {
            // Create the HWND so global hotkeys and single-instance messages work without showing the window.
            new WindowInteropHelper(window).EnsureHandle();
        } else {
            window.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e) {
        if (singleInstanceMutex != null) {
            try { singleInstanceMutex.ReleaseMutex(); } catch (ApplicationException) { }
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
        }
        base.OnExit(e);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
