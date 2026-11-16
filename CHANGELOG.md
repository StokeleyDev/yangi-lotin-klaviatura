# Changelog

## [1.2.5] — 2026-11-16

### Added
- Pause hotkey Ctrl+Alt+Pause, also in settings and the tray menu.
- Per-app stop list in settings (`%LOCALAPPDATA%\YangiLotinKlaviatura\settings.json`).
- Password fields are skipped where Windows reports them through UI Automation.
- Closing the window minimizes the app to the tray; replacement keeps working. Tray menu: open, automatic replacement, pause, exit.
- UI theme is remembered between launches.
- Unit tests for the text converter.
- `SHA256SUMS.txt` is published with every release.

### Changed
- Releases are built on GitHub Actions from source (`windows-latest`) when a `v*` tag is pushed. Built files are no longer stored in git.
- Version is kept in one place: `Directory.Build.props`.
- Installer renamed to `Yangi-Lotin-Setup.exe`.
- o'/g' work with every apostrophe variant: ' ‘ ’ ʻ ʼ `.
- Shift, CapsLock, Ctrl, Alt or Win pressed alone no longer break a pending pair.
- Windows run as administrator are skipped, and the status line explains why.
- Better input-layout detection in console windows.
- Pending pair is reset on caret moves, window or focus changes and navigation keys. The global mouse hook was removed.
- UI state is sent on events instead of every 400 ms.
- Background images converted to WebP; the installer is about half its previous size.
- README download links point to the latest release.

### Fixed
- Uninstaller refuses to run while the app is open, instead of stopping halfway.
- Uninstaller removes WebView2 data, so the install folder no longer stays behind.

## [1.2.4]
- Compact layout, bundled Inter font, refreshed light theme and app branding.

## [1.2.0]
- Solid content panels with Liquid Glass controls.

## [1.1.0]
- Liquid Glass UI, Uzbek themes, updated installer and portable builds.

[1.2.5]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.5
[1.2.4]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.4
[1.2.0]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.0
[1.1.0]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.1.0
