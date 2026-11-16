using System.Text.RegularExpressions;
namespace UzbekKeyboard;
internal static class TextConverter {
 internal const string Apostrophes="'‘’ʻʼ`";
 private static readonly Regex Pairs=new(@"[sc]h|[og]["+Apostrophes+"]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
 internal static bool IsApostrophe(char c)=>c!='\0' && Apostrophes.Contains(c);
 internal static string Convert(string text) => Pairs.Replace(text, m => {
  var pair=m.Value;
  char result=char.ToLowerInvariant(pair[0]) switch { 's'=>'ş', 'c'=>'ç', 'o'=>'ö', 'g'=>'ğ', _=>pair[0] };
  return char.IsUpper(pair[0]) ? char.ToUpperInvariant(result).ToString() : result.ToString();
 });
}
