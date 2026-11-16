using UzbekKeyboard;
using Xunit;
namespace UzbekKeyboard.Tests;
public class TextConverterTests {
 [Theory]
 [InlineData("shahar","şahar")]
 [InlineData("choy","çoy")]
 [InlineData("o'zbek","özbek")]
 [InlineData("g'alaba","ğalaba")]
 public void ConvertsPairs(string input,string expected)=>Assert.Equal(expected,TextConverter.Convert(input));
 [Theory]
 [InlineData("Shahar","Şahar")]
 [InlineData("SHAHAR","ŞAHAR")]
 [InlineData("Choy","Çoy")]
 [InlineData("O'zbek","Özbek")]
 [InlineData("G'alaba","Ğalaba")]
 [InlineData("sHo'G'","şöĞ")]
 public void PreservesCaseOfFirstLetter(string input,string expected)=>Assert.Equal(expected,TextConverter.Convert(input));
 [Theory]
 [InlineData("'")]
 [InlineData("‘")]
 [InlineData("’")]
 [InlineData("ʻ")]
 [InlineData("ʼ")]
 [InlineData("`")]
 public void AcceptsAllApostrophes(string a) {
  Assert.Equal("ö",TextConverter.Convert("o"+a));
  Assert.Equal("Ğ",TextConverter.Convert("G"+a));
 }
 [Theory]
 [InlineData("")]
 [InlineData("salom dunyo")]
 [InlineData("o g s c h")]
 [InlineData("o\"g-")]
 [InlineData("12345")]
 public void LeavesOtherTextUnchanged(string input)=>Assert.Equal(input,TextConverter.Convert(input));
}
