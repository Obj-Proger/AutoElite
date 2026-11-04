using System.Globalization;
using AutoElite.Application.Common.Localization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Localization;

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData(Language.Russian, "ru")]
    [InlineData(Language.English, "en")]
    public void ToCulture_ReturnsTheLanguageCulture(Language language, string cultureName)
    {
        language.ToCulture().Name.Should().Be(cultureName);
    }

    [Fact]
    public void ToCulture_UndefinedLanguage_Throws()
    {
        var act = () => ((Language)99).ToCulture();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("ru", Language.Russian)]
    [InlineData("ru-RU", Language.Russian)]
    [InlineData("en-GB", Language.English)]
    public void FromCulture_SupportedCulture_ReturnsTheLanguage(string cultureName, Language expected)
    {
        SupportedLanguages.FromCulture(CultureInfo.GetCultureInfo(cultureName)).Should().Be(expected);
    }

    [Fact]
    public void FromCulture_UnsupportedCulture_ReturnsNull()
    {
        SupportedLanguages.FromCulture(CultureInfo.GetCultureInfo("de")).Should().BeNull();
    }

    [Fact]
    public void Cultures_EveryLanguageHasOne_DefaultFirst()
    {
        SupportedLanguages.Cultures.Should().Equal(Enum.GetValues<Language>().Select(language => language.ToCulture()));
        SupportedLanguages.Cultures[0].Should().Be(SupportedLanguages.Default.ToCulture());
    }
}
