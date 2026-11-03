# Yangi lotin — Liquid Glass

The Windows keyboard engine remains C#/.NET 8. The interface is React, hosted locally inside Microsoft Edge WebView2. Buttons, switches, segmented controls and the settings sheet use @zakisheriff/liquid-glass 0.3.16 (MIT). In 1.2.0 the status panel, four letter cards, converter panel and editor are solid, opaque surfaces without refraction or backdrop blur.

## Run the portable build

Extract the entire portable ZIP into a new folder and launch "Yangi lotin klaviatura.exe". Keep the WebUI and runtimes folders next to it. Requires Windows 10/11, .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. The original app is not overwritten.

Runtime downloads if missing:
- https://dotnet.microsoft.com/download/dotnet/8.0 (Desktop Runtime)
- https://developer.microsoft.com/microsoft-edge/webview2/ (Evergreen Runtime)

The app serves bundled files using WebView2's local virtual host, not a web server. npm and Node.js are not needed by the portable build. Internet is not needed for the interface after the runtimes are installed. WebView2 profile data is stored in %LOCALAPPDATA%/YangiLotinKlaviatura/WebView2.

## Build from source

Requires Node.js 18+ and .NET SDK 8.

1. In WebUI: npm ci
2. In WebUI: npm run build
3. In the project root: dotnet publish -c Release -o publish

Ship the complete publish folder. To rebuild both the installer and portable distribution, run ./build-release.ps1 from PowerShell. It builds React, publishes the app and x64 uninstaller, refreshes installer/payload.zip, and creates dist/portable, dist/Yangi-Lotin-Portable.zip and dist/Yangi lotin klaviatura Setup.exe. Close any dev/preview servers before building.

## Behavior

- Uzbek is the default interface language, with Russian switching.
- Automatic replacement starts when the bundled interface connects successfully, just as the original app starts enabled.
- Replacement only operates with the Uzbek Latin Windows input language (0x0443).
- The native KeyboardMapper and TextConverter are unchanged.
- Closing the window stops the hooks. No tray mode or autostart has been added.
- The theme follows the system by default; the settings sheet also offers light/dark overrides for the current launch.
- Settings opens only the fixed Windows language settings URI.

## Verification

The production React UI and C# project were compiled. Headless Chromium checks cover the real component library, a simulated native bridge (toggle/conversion), language switching, settings sheet and theme changes, and horizontal overflow at 720 and 390 px.

The local-page loading regression was reproduced in real WebView2 156.0.4314.58 with an invisible parent window and isolated profile: navigating to the mapped folder root fails; explicitly navigating to /index.html succeeds. The corrected navigation policy is shared with the regression probe. The real React bundle emits its ready message and applies a native Russian-language/disabled-state message. The 1.2.0 interface has 7 glass surfaces in its default DOM, compared with 13 in 1.1.0; the six large panels are opaque and have no backdrop filter. The probe never shows a window or installs global keyboard hooks. Real global key interception and visible WPF window rendering are not covered by this test.

Refraction depends on the installed Chromium/WebView2 version and the background behind the controls. Using the same library does not guarantee identical pixels across different browsers, GPU configurations and Windows versions.
