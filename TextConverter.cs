using System.Text.RegularExpressions;
namespace UzbekKeyboard;
internal static class TextConverter {
 internal static string Convert(string text) => Regex.Replace(text, @"[sc]h|[og]['‘’ʻʼ` ]", m => {
  var pair=m.Value;
  if (pair[1]==' ') return pair;
  char result=char.ToLowerInvariant(pair[0]) switch { 's'=>'ş', 'c'=>'ç', 'o'=>'ö', 'g'=>'ğ', _=>pair[0] };
  return char.IsUpper(pair[0]) ? char.ToUpperInvariant(result).ToString() : result.ToString();
 }, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}

