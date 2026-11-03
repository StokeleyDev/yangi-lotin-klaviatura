# Yangi lotin klaviatura 1.2.4

Changes since the previous published release (1.2.0):

- Bundled Inter Variable with larger, readable interface text; no font CDN or internet needed.
- Compact 960 x 680 default window, DPI-aware sizing and a two-column layout for short desktop windows.
- Removed the large introductory title and description in Uzbek and Russian.
- Updated light wallpaper with dark foreground text; dark wallpaper retained.
- Formal letter-mapping headings and descriptions in Uzbek and Russian.
- Real application icon beside the toolbar brand.
- Solid content panels and Liquid Glass controls retained.
- Updated portable ZIP, installer payload and screenshots.

Windows x64. Requires .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. Extract the entire portable ZIP. Close the old app before updating.

Validation: production React and native release builds, font/logo loading checks, language/theme/interaction checks, no-scroll checks across desktop sizes down to 540 px high, and hidden real WebView2 loading/bridge checks. Desktop installation and real global keyboard interception were not automated on the user desktop.
