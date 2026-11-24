# Yangi lotin klaviatura 1.3.0

Changes since 1.2.5:

- **3-way Uzbek converter:** Cyrillic ↔ Old Latin (1995) ↔ New Latin (`ş, ç, ö, ğ`), with URL, email, `@mention`, `#hashtag`, Windows path, and brand/exception word protection.
- **In-place selection converter (`Ctrl + Alt + L`):** convert highlighted text directly inside any app (auto-detect or manual direction; preserves clipboard).
- **EN-layout mode & `Right Alt` (`AltGr`) shortcuts:** type `ş, ç, ö, ğ` on English (US/UK) without adding a 3rd Windows layout, or insert `ş, ç, ö, ğ, ʼ` anytime via `Right Alt + S / C / O / G / '`.
- **Optional Smart Tutuq (`ʼ`):** automatically replaces `'` with U+02BC in words like `maʼno`, `sanʼat`.
- **Single-instance & silent tray autostart (`--tray`):** prevents duplicate hooks and delays WebView2 initialization until the window is opened.
- **Full settings persistence & laptop-friendly pause hotkey:** saves all settings in `settings.json`; default pause hotkey changed to `Ctrl + Alt + U` with presets in Settings.
- **Bilingual (`UZ | RU`) installer & uninstaller** with WebView2 Runtime detection and automatic GitHub release update check.
- **Cleaner Apple-inspired UI:** decluttered main screen, Inset Grouped settings cards, and fixed `GlassSheet` scroll backdrop.
- **74 unit tests** covering all 6 conversion modes, protected spans, auto-detection, and settings persistence.

Windows x64. Requires .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. Close the old app via the tray menu before updating.

Validation: builds and 74 unit tests in CI. Real global keyboard interception, tray and hotkeys were not tested automatically on a desktop.
