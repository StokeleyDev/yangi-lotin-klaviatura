using UzbekKeyboard;
using Xunit;

namespace UzbekKeyboard.Tests;

public class TextConverterTests {
    [Theory]
    [InlineData("shahar", "şahar")]
    [InlineData("choy", "çoy")]
    [InlineData("o'zbek", "özbek")]
    [InlineData("g'alaba", "ğalaba")]
    public void ConvertsPairs(string input, string expected) => Assert.Equal(expected, TextConverter.Convert(input));

    [Theory]
    [InlineData("Shahar", "Şahar")]
    [InlineData("SHAHAR", "ŞAHAR")]
    [InlineData("Choy", "Çoy")]
    [InlineData("O'zbek", "Özbek")]
    [InlineData("G'alaba", "Ğalaba")]
    [InlineData("sHo'G'", "şöĞ")]
    public void PreservesCaseOfFirstLetter(string input, string expected) => Assert.Equal(expected, TextConverter.Convert(input));

    [Theory]
    [InlineData("'")]
    [InlineData("‘")]
    [InlineData("’")]
    [InlineData("ʻ")]
    [InlineData("ʼ")]
    [InlineData("`")]
    public void AcceptsAllApostrophes(string a) {
        Assert.Equal("ö", TextConverter.Convert("o" + a));
        Assert.Equal("Ğ", TextConverter.Convert("G" + a));
    }

    [Theory]
    [InlineData("")]
    [InlineData("salom dunyo")]
    [InlineData("o g s c h")]
    [InlineData("o\"g-")]
    [InlineData("12345")]
    public void LeavesOtherTextUnchanged(string input) => Assert.Equal(input, TextConverter.Convert(input));

    [Theory]
    [InlineData("şahar", "shahar")]
    [InlineData("Şahar", "Shahar")]
    [InlineData("ŞAHAR", "SHAHAR")]
    [InlineData("çoy", "choy")]
    [InlineData("ÇOY", "CHOY")]
    [InlineData("özbek", "o‘zbek")]
    [InlineData("Özbek", "O‘zbek")]
    [InlineData("ğalaba", "g‘alaba")]
    [InlineData("ĞALABA", "G‘ALABA")]
    public void ConvertsNewToOldLatin(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.NewLatinToOldLatin));

    [Theory]
    [InlineData("Ўзбекистон", "Özbekiston")]
    [InlineData("шаҳар", "şahar")]
    [InlineData("Шаҳар", "Şahar")]
    [InlineData("ШАҲАР", "ŞAHAR")]
    [InlineData("чиройли", "çiroyli")]
    [InlineData("ғоя", "ğoya")]
    [InlineData("Ер", "Yer")]
    [InlineData("ЕР", "YER")]
    [InlineData("мева", "meva")]
    [InlineData("Ёз", "Yoz")]
    [InlineData("ЁЗ", "YOZ")]
    [InlineData("Цирк", "Sirk")]
    [InlineData("акция", "aksiya")]
    [InlineData("лицей", "litsey")]
    [InlineData("маъно", "maʼno")]
    public void ConvertsCyrillicToNewLatin(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.CyrillicToNewLatin));

    [Theory]
    [InlineData("Ўзбекистон", "O‘zbekiston")]
    [InlineData("ШАҲАР", "SHAHAR")]
    [InlineData("Шаҳар", "Shahar")]
    [InlineData("чиройли", "chiroyli")]
    [InlineData("ғоя", "g‘oya")]
    public void ConvertsCyrillicToOldLatin(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.CyrillicToOldLatin));

    [Theory]
    [InlineData("şahar", "шаҳар")]
    [InlineData("Şahar", "Шаҳар")]
    [InlineData("çiroyli", "чиройли")]
    [InlineData("özbek", "ўзбек")]
    [InlineData("ğoya", "ғоя")]
    [InlineData("yer", "ер")]
    [InlineData("er", "эр")]
    [InlineData("meva", "мева")]
    [InlineData("maʼno", "маъно")]
    public void ConvertsNewLatinToCyrillic(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.NewLatinToCyrillic));

    [Theory]
    [InlineData("shahar", "шаҳар")]
    [InlineData("o'zbek", "ўзбек")]
    [InlineData("g'alaba", "ғалаба")]
    public void ConvertsOldLatinToCyrillic(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.OldLatinToCyrillic));

    [Theory]
    [InlineData("Салом дунё", ConversionMode.CyrillicToNewLatin)]
    [InlineData("şahar", ConversionMode.NewLatinToOldLatin)]
    [InlineData("shahar", ConversionMode.OldLatinToNewLatin)]
    public void DetectsAutoMode(string input, ConversionMode expected) =>
        Assert.Equal(expected, TextConverter.DetectAutoMode(input));

    [Theory]
    [InlineData("https://my.gov.uz/share?q=choy", "https://my.gov.uz/share?q=choy")]
    [InlineData("shohruh@shop.uz", "shohruh@shop.uz")]
    [InlineData("@channel #cashback", "@channel #cashback")]
    [InlineData(@"C:\Users\sh\ch\file.txt", @"C:\Users\sh\ch\file.txt")]
    [InlineData("Photoshop va Telegram ishlayapti", "Photoshop va Telegram işlayapti")]
    [InlineData("Is'hoq va as'hob shahar", "Is'hoq va as'hob şahar")]
    public void ProtectsUrlsEmailsMentionsPathsAndExceptions(string input, string expected) =>
        Assert.Equal(expected, TextConverter.Convert(input, ConversionMode.OldLatinToNewLatin));
}
