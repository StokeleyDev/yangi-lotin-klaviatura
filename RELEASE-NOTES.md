# Yangi lotin klaviatura 1.1.0

- React interface inside WebView2 using the real @zakisheriff/liquid-glass library.
- Supplied Uzbek-themed light and dark backgrounds; system theme and manual selection.
- Uzbek and Russian interface, native automatic replacement and text conversion.
- Fixed local page navigation to index.html.
- Updated installer payload, portable ZIP and uninstaller cleanup for the new files.
- Reproducible release build with build-release.ps1.

Windows x64. Requires .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. Extract the complete portable ZIP before launching. Close the app before installing an update.

Verification: release builds succeeded; headless UI checks passed; real hidden WebView2 loaded the production interface and exchanged bridge messages. Installation on the user desktop and real global keyboard interception were not exercised.
