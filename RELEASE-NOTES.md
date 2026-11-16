# Yangi lotin klaviatura 1.2.5

Changes since 1.2.4:

- Release is now built on GitHub Actions from source; SHA256SUMS.txt is published with every release.
- Installer renamed to Yangi-Lotin-Setup.exe.
- Pause hotkey Ctrl+Alt+Pause and a per-app stop list in settings.
- Password fields are skipped where Windows reports them (UI Automation).
- Windows run as administrator are skipped and the status line explains why.
- Better layout detection in console windows.
- Closing the window minimizes to the tray; replacement keeps working. Exit from the tray menu.
- o'/g' work with all apostrophe variants (' ‘ ’ ʻ ʼ `); Shift/CapsLock alone no longer break a pair.
- Pending pair resets on caret moves and navigation keys; global mouse hook removed.
- Uninstaller refuses while the app is running and removes all app data.
- UI theme is remembered; background images are much smaller (WebP).
- Unit tests for the text converter.

Windows x64. Requires .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. Close the old app before updating.

Validation: builds and unit tests in CI. Real global keyboard interception, tray and hotkey were not tested automatically on a desktop.
