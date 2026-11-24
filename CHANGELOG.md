# Changelog

## [1.3.0] — 2026-11-24

### Added
- 3-way Uzbek text converter: Cyrillic ↔ Old Latin (1995) ↔ New Latin (`ş, ç, ö, ğ`) with Title Case / ALL CAPS preservation and Uzbek orthography rules (`ye/e`, `ts/s`, `yo/yu/ya`, `ъ/ʼ`).
- Global in-place selection converter (`Ctrl + Alt + L`, with `Auto` detection or manual direction in settings and tray menu; preserves clipboard).
- EN-layout typing mode (`WorkOnEnglish`) to type `ş, ç, ö, ğ` on English (US/UK) without adding a third Windows layout, plus a 1-click button in Settings to add `uz-Latn-UZ`.
- Direct `Right Alt` (`AltGr`) shortcuts: `Right Alt + S / C / O / G / '` → `ş / ç / ö / ğ / ʼ`.
- Optional Smart Tutuq (`ʼ`, U+02BC) replacement for words like `ma'no` → `maʼno`.
- Protected span masking in the converter: preserves URLs, emails, Windows paths, `@mentions`, `#hashtags`, and common brand/exception words (`Photoshop`, `Telegram`, `is'hoq`, `as'hob`, etc.).
- Single-instance protection (`Mutex`) that activates the running window instead of spawning duplicate keyboard hooks.
- Silent tray startup (`--tray` / `--minimized`) with lazy WebView2 initialization (zero Chromium RAM until the window is opened).
- Non-blocking GitHub release update check with a toolbar version badge.
- Bilingual (`UZ | RU`) installer with WebView2 Runtime registry check and bilingual uninstaller.
- Expanded xUnit test suite (74 tests covering all 6 conversion modes, protected spans, auto-detection, and settings persistence).

### Changed
- Default pause hotkey changed from `Ctrl + Alt + Pause` to laptop-friendly `Ctrl + Alt + U` (configurable presets in Settings: `Ctrl+Alt+U`, `Ctrl+Alt+P`, `Ctrl+Shift+U`, `Ctrl+Alt+Pause`).
- All user settings (language, enabled state, EN mode, AltGr, smart tutuq, theme, hotkeys, selection mode, stop list) are now persisted in `settings.json`.
- Auto-start now launches silently into the tray (`--tray`).
- Streamlined Apple-inspired UI: removed redundant descriptions on the main screen and grouped Settings into 4 compact Inset Grouped cards.

### Fixed
- Fixed `GlassSheet` scrolling transparency bug in Settings so scrolled controls stay crisp and readable in light and dark themes.
- Removed unused legacy string fields from `UiStrings.cs`.

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

[1.3.0]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.3.0
[1.2.5]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.5
[1.2.4]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.4
[1.2.0]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.2.0
[1.1.0]: https://github.com/StokeleyDev/yangi-lotin-klaviatura/releases/tag/v1.1.0
