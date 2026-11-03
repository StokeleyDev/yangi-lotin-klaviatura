namespace UzbekKeyboard;

internal static class LocalUiNavigation
{
    internal const string Host = "yangi.local";
    internal const string DocumentUrl = "https://yangi.local/index.html";

    internal static bool IsTrustedDocument(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.GetLeftPart(UriPartial.Path) == DocumentUrl;
}
