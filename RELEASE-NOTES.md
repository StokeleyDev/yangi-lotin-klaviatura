# Yangi lotin klaviatura 1.2.0

- Solid, opaque status panel, four letter cards, converter panel and text field.
- Liquid Glass retained on buttons, switches, language controls and the settings sheet.
- Six large refracting card surfaces removed.
- Supplied Uzbek-themed light and dark backgrounds; system theme and manual selection.
- Uzbek and Russian interface, native automatic replacement and text conversion.
- Fixed local page navigation to index.html.
- Updated installer payload, portable ZIP and uninstaller cleanup for the new files.
- Reproducible release build with build-release.ps1.

Windows x64. Requires .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. Extract the complete portable ZIP before launching. Close the app before installing an update.

Verification: release builds succeeded; headless UI checks passed; real hidden WebView2 loaded the production interface and exchanged bridge messages. Installation on the user desktop and real global keyboard interception were not exercised.
