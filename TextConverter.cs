using System.Text;
using System.Text.RegularExpressions;

namespace UzbekKeyboard;

public enum ConversionMode {
    OldLatinToNewLatin,
    NewLatinToOldLatin,
    CyrillicToNewLatin,
    CyrillicToOldLatin,
    NewLatinToCyrillic,
    OldLatinToCyrillic
}

internal static class TextConverter {
    internal const string Apostrophes = "'‘’ʻʼ`";

    private static readonly Regex Pairs = new(
        @"[sc]h|[og][" + Apostrophes + "]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NewPairs = new(
        @"[şŞçÇöÖğĞ]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ProtectedSpans = new(
        @"https?://[^\s<>()]+|www\.[^\s<>()]+|[A-Za-z]:\\[^\s]+|[\w.+-]+@[\w-]+\.[\w.-]+|\b[A-Za-z0-9-]+\.(?:uz|com|ru|org|net|io|dev|gov|edu|me|ai)(?:/[^\s]*)?|(?<!\w)[@#][A-Za-z0-9_.-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex WordTokens = new(
        @"\b[\p{L}'‘’ʻʼ`]+\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PlaceholderTokens = new(
        "\uE000(\\d+)\uE001",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> Exceptions = new(StringComparer.OrdinalIgnoreCase) {
        "is'hoq", "is‘hoq", "is’hoq", "isʼhoq", "ishoqi",
        "as'hob", "as‘hob", "as’hob", "asʼhob",
        "mus'haf", "mus‘haf", "mus’haf", "musʼhaf",
        "telegram", "whatsapp", "photoshop", "chrome", "cashback", "dashboard",
        "channel", "chat", "check", "shop", "push", "search", "share", "shell",
        "ssh", "http", "https", "html", "css", "json", "csharp", "iphone",
        "macbook", "windows", "excel", "word", "powerpoint", "github", "click",
        "payme", "uzum", "google", "youtube", "instagram", "facebook"
    };

    internal static bool IsApostrophe(char c) => c != '\0' && Apostrophes.Contains(c);

    internal static ConversionMode ParseMode(string? mode) => mode switch {
        "newToOld" => ConversionMode.NewLatinToOldLatin,
        "cyrToNew" => ConversionMode.CyrillicToNewLatin,
        "cyrToOld" => ConversionMode.CyrillicToOldLatin,
        "newToCyr" => ConversionMode.NewLatinToCyrillic,
        "oldToCyr" => ConversionMode.OldLatinToCyrillic,
        _ => ConversionMode.OldLatinToNewLatin
    };

    internal static ConversionMode DetectAutoMode(string text) {
        foreach (char c in text) {
            if (c is >= '\u0400' and <= '\u04FF')
                return ConversionMode.CyrillicToNewLatin;
        }
        foreach (char c in text) {
            if (c is 'ş' or 'Ş' or 'ç' or 'Ç' or 'ö' or 'Ö' or 'ğ' or 'Ğ')
                return ConversionMode.NewLatinToOldLatin;
        }
        return ConversionMode.OldLatinToNewLatin;
    }

    internal static string Convert(string text) => Convert(text, ConversionMode.OldLatinToNewLatin);

    internal static string Convert(string text, string? mode) {
        ConversionMode parsed = string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase)
            ? DetectAutoMode(text)
            : ParseMode(mode);
        return Convert(text, parsed);
    }

    internal static string Convert(string text, ConversionMode mode) {
        if (string.IsNullOrEmpty(text)) return text;

        var tokens = new List<string>();
        string masked = MaskProtected(text, tokens);

        string converted = mode switch {
            ConversionMode.OldLatinToNewLatin => ConvertOldToNewCore(masked),
            ConversionMode.NewLatinToOldLatin => ConvertNewToOldCore(masked),
            ConversionMode.CyrillicToNewLatin => ConvertCyrillicToLatinCore(masked, toNewLatin: true),
            ConversionMode.CyrillicToOldLatin => ConvertCyrillicToLatinCore(masked, toNewLatin: false),
            ConversionMode.NewLatinToCyrillic => ConvertNewLatinToCyrillicCore(masked),
            ConversionMode.OldLatinToCyrillic => ConvertNewLatinToCyrillicCore(ConvertOldToNewCore(masked)),
            _ => masked
        };

        return tokens.Count == 0 ? converted : RestoreProtected(converted, tokens);
    }

    private static string MaskProtected(string text, List<string> tokens) {
        string step1 = ProtectedSpans.Replace(text, m => {
            int idx = tokens.Count;
            tokens.Add(m.Value);
            return $"\uE000{idx}\uE001";
        });
        return WordTokens.Replace(step1, m => {
            if (!Exceptions.Contains(m.Value)) return m.Value;
            int idx = tokens.Count;
            tokens.Add(m.Value);
            return $"\uE000{idx}\uE001";
        });
    }

    private static string RestoreProtected(string text, List<string> tokens) =>
        PlaceholderTokens.Replace(text, m => {
            int idx = int.Parse(m.Groups[1].Value);
            return idx >= 0 && idx < tokens.Count ? tokens[idx] : m.Value;
        });

    private static string ConvertOldToNewCore(string text) => Pairs.Replace(text, m => {
        var pair = m.Value;
        char result = char.ToLowerInvariant(pair[0]) switch {
            's' => 'ş',
            'c' => 'ç',
            'o' => 'ö',
            'g' => 'ğ',
            _ => pair[0]
        };
        return char.IsUpper(pair[0]) ? char.ToUpperInvariant(result).ToString() : result.ToString();
    });

    private static string ConvertNewToOldCore(string text) => NewPairs.Replace(text, m => {
        char ch = m.Value[0];
        int index = m.Index;
        bool isAllCaps = IsAllCapsAt(text, index);
        return ch switch {
            'ş' => "sh",
            'Ş' => isAllCaps ? "SH" : "Sh",
            'ç' => "ch",
            'Ç' => isAllCaps ? "CH" : "Ch",
            'ö' => "o‘",
            'Ö' => "O‘",
            'ğ' => "g‘",
            'Ğ' => "G‘",
            _ => m.Value
        };
    });

    private static string ConvertCyrillicToLatinCore(string text, bool toNewLatin) {
        var sb = new StringBuilder(text.Length + 16);
        for (int i = 0; i < text.Length; i++) {
            char ch = text[i];
            char lower = char.ToLowerInvariant(ch);
            bool upper = char.IsUpper(ch);
            bool allCaps = upper && IsAllCapsAt(text, i);

            switch (lower) {
                case 'а': sb.Append(upper ? 'A' : 'a'); break;
                case 'б': sb.Append(upper ? 'B' : 'b'); break;
                case 'в': sb.Append(upper ? 'V' : 'v'); break;
                case 'г': sb.Append(upper ? 'G' : 'g'); break;
                case 'д': sb.Append(upper ? 'D' : 'd'); break;
                case 'е': {
                    bool prevBoundary = i == 0 || !IsWordChar(text[i - 1]) || IsCyrillicVowel(text[i - 1]) || text[i - 1] is 'ъ' or 'Ъ' or 'ь' or 'Ь';
                    if (prevBoundary) sb.Append(upper ? (allCaps ? "YE" : "Ye") : "ye");
                    else sb.Append(upper ? 'E' : 'e');
                    break;
                }
                case 'ё': sb.Append(upper ? (allCaps ? "YO" : "Yo") : "yo"); break;
                case 'ж': sb.Append(upper ? 'J' : 'j'); break;
                case 'з': sb.Append(upper ? 'Z' : 'z'); break;
                case 'и': sb.Append(upper ? 'I' : 'i'); break;
                case 'й': sb.Append(upper ? 'Y' : 'y'); break;
                case 'к': sb.Append(upper ? 'K' : 'k'); break;
                case 'л': sb.Append(upper ? 'L' : 'l'); break;
                case 'м': sb.Append(upper ? 'M' : 'm'); break;
                case 'н': sb.Append(upper ? 'N' : 'n'); break;
                case 'о': sb.Append(upper ? 'O' : 'o'); break;
                case 'п': sb.Append(upper ? 'P' : 'p'); break;
                case 'р': sb.Append(upper ? 'R' : 'r'); break;
                case 'с': sb.Append(upper ? 'S' : 's'); break;
                case 'т': sb.Append(upper ? 'T' : 't'); break;
                case 'у': sb.Append(upper ? 'U' : 'u'); break;
                case 'ф': sb.Append(upper ? 'F' : 'f'); break;
                case 'х': sb.Append(upper ? 'X' : 'x'); break;
                case 'ц': {
                    bool afterVowel = i > 0 && IsCyrillicVowel(text[i - 1]);
                    if (afterVowel) sb.Append(upper ? (allCaps ? "TS" : "Ts") : "ts");
                    else sb.Append(upper ? 'S' : 's');
                    break;
                }
                case 'ч':
                    if (toNewLatin) sb.Append(upper ? 'Ç' : 'ç');
                    else sb.Append(upper ? (allCaps ? "CH" : "Ch") : "ch");
                    break;
                case 'ш':
                    if (toNewLatin) sb.Append(upper ? 'Ş' : 'ş');
                    else sb.Append(upper ? (allCaps ? "SH" : "Sh") : "sh");
                    break;
                case 'щ':
                    if (toNewLatin) sb.Append(upper ? (allCaps ? "ŞÇ" : "Şç") : "şç");
                    else sb.Append(upper ? (allCaps ? "SHCH" : "Shch") : "shch");
                    break;
                case 'ъ': sb.Append('ʼ'); break;
                case 'ы': sb.Append(upper ? 'I' : 'i'); break;
                case 'ь': break;
                case 'э': sb.Append(upper ? 'E' : 'e'); break;
                case 'ю': sb.Append(upper ? (allCaps ? "YU" : "Yu") : "yu"); break;
                case 'я': sb.Append(upper ? (allCaps ? "YA" : "Ya") : "ya"); break;
                case 'ў':
                    if (toNewLatin) sb.Append(upper ? 'Ö' : 'ö');
                    else sb.Append(upper ? "O‘" : "o‘");
                    break;
                case 'қ': sb.Append(upper ? 'Q' : 'q'); break;
                case 'ғ':
                    if (toNewLatin) sb.Append(upper ? 'Ğ' : 'ğ');
                    else sb.Append(upper ? "G‘" : "g‘");
                    break;
                case 'ҳ': sb.Append(upper ? 'H' : 'h'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    private static string ConvertNewLatinToCyrillicCore(string text) {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++) {
            char ch = text[i];
            char lower = char.ToLowerInvariant(ch);
            bool upper = char.IsUpper(ch);

            if (i + 1 < text.Length) {
                char nextLower = char.ToLowerInvariant(text[i + 1]);
                if (lower == 'y' && nextLower is 'o' or 'u' or 'a' or 'e') {
                    bool isWordBoundaryOrAfterVowel = i == 0 || !IsWordChar(text[i - 1]) || IsLatinVowel(text[i - 1]) || IsApostrophe(text[i - 1]);
                    if (nextLower == 'o') { sb.Append(upper ? 'Ё' : 'ё'); i++; continue; }
                    if (nextLower == 'u') { sb.Append(upper ? 'Ю' : 'ю'); i++; continue; }
                    if (nextLower == 'a') { sb.Append(upper ? 'Я' : 'я'); i++; continue; }
                    if (nextLower == 'e' && isWordBoundaryOrAfterVowel) { sb.Append(upper ? 'Е' : 'е'); i++; continue; }
                }
                if (lower == 't' && nextLower == 's' && i > 0 && IsLatinVowel(text[i - 1])) {
                    sb.Append(upper ? 'Ц' : 'ц');
                    i++;
                    continue;
                }
            }

            if (IsApostrophe(ch)) {
                bool betweenLetters = i > 0 && i + 1 < text.Length && char.IsLetter(text[i - 1]) && char.IsLetter(text[i + 1]);
                sb.Append(betweenLetters ? 'ъ' : ch);
                continue;
            }

            switch (lower) {
                case 'a': sb.Append(upper ? 'А' : 'а'); break;
                case 'b': sb.Append(upper ? 'Б' : 'б'); break;
                case 'c': sb.Append(upper ? 'С' : 'с'); break;
                case 'd': sb.Append(upper ? 'Д' : 'д'); break;
                case 'e': {
                    bool atStartOrAfterVowel = i == 0 || !IsWordChar(text[i - 1]) || IsLatinVowel(text[i - 1]);
                    if (atStartOrAfterVowel) sb.Append(upper ? 'Э' : 'э');
                    else sb.Append(upper ? 'Е' : 'е');
                    break;
                }
                case 'f': sb.Append(upper ? 'Ф' : 'ф'); break;
                case 'g': sb.Append(upper ? 'Г' : 'г'); break;
                case 'h': sb.Append(upper ? 'Ҳ' : 'ҳ'); break;
                case 'i': sb.Append(upper ? 'И' : 'и'); break;
                case 'j': sb.Append(upper ? 'Ж' : 'ж'); break;
                case 'k': sb.Append(upper ? 'К' : 'к'); break;
                case 'l': sb.Append(upper ? 'Л' : 'л'); break;
                case 'm': sb.Append(upper ? 'М' : 'м'); break;
                case 'n': sb.Append(upper ? 'Н' : 'н'); break;
                case 'o': sb.Append(upper ? 'О' : 'о'); break;
                case 'p': sb.Append(upper ? 'П' : 'п'); break;
                case 'q': sb.Append(upper ? 'Қ' : 'қ'); break;
                case 'r': sb.Append(upper ? 'Р' : 'р'); break;
                case 's': sb.Append(upper ? 'С' : 'с'); break;
                case 't': sb.Append(upper ? 'Т' : 'т'); break;
                case 'u': sb.Append(upper ? 'У' : 'у'); break;
                case 'v': sb.Append(upper ? 'В' : 'в'); break;
                case 'w': sb.Append(upper ? 'В' : 'в'); break;
                case 'x': sb.Append(upper ? 'Х' : 'х'); break;
                case 'y': sb.Append(upper ? 'Й' : 'й'); break;
                case 'z': sb.Append(upper ? 'З' : 'з'); break;
                case 'ş': sb.Append(upper ? 'Ш' : 'ш'); break;
                case 'ç': sb.Append(upper ? 'Ч' : 'ч'); break;
                case 'ö': sb.Append(upper ? 'Ў' : 'ў'); break;
                case 'ğ': sb.Append(upper ? 'Ғ' : 'ғ'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    private static bool IsAllCapsAt(string text, int index) {
        if (!char.IsUpper(text[index])) return false;
        bool nextUpper = index + 1 < text.Length && char.IsLetter(text[index + 1]) && char.IsUpper(text[index + 1]);
        bool prevUpperAtEnd = index > 0 && char.IsLetter(text[index - 1]) && char.IsUpper(text[index - 1]) &&
                              (index + 1 >= text.Length || !char.IsLetter(text[index + 1]));
        return nextUpper || prevUpperAtEnd;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || IsApostrophe(c);
    private static bool IsCyrillicVowel(char c) => "аеёиоуўэюяАЕЁИОУЎЭЮЯ".Contains(c);
    private static bool IsLatinVowel(char c) => "aeiouöAEIOUÖ".Contains(c);
}
