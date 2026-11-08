using System.Collections;
using System.Globalization;
using System.Reflection;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Tests.Localization;

public class ErrorMessagesCatalogTests
{
    private static readonly IReadOnlyDictionary<string, string> _russian = ReadResources(CultureInfo.GetCultureInfo("ru"));
    private static readonly IReadOnlyDictionary<string, string> _neutral = ReadResources(CultureInfo.InvariantCulture);

    private static readonly IReadOnlyList<string> _domainCodes = ErrorCodesOf(typeof(DomainErrors));

    private static readonly IReadOnlyList<string> _applicationCodes =
        [.. ErrorCodesOf(typeof(ApplicationErrors)), ApplicationErrors.ValidationFailedCode];

    private static readonly IReadOnlyList<string> _notFoundCodes =
        [.. typeof(BaseEntity).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(typeof(BaseEntity)))
            .Select(type => $"{type.Name}.NotFound")];

    private static Dictionary<string, string> ReadResources(CultureInfo culture) =>
        ResourceErrorLocalizer.Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    private static IReadOnlyList<string> ErrorCodesOf(Type catalog) =>
        [.. catalog.GetNestedTypes()
            .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.FieldType == typeof(Error))
            .Select(field => ((Error)field.GetValue(null)!).Code)];

    [Fact]
    public void Catalogs_AreFound()
    {
        _domainCodes.Should().HaveCountGreaterThan(300);
        _applicationCodes.Should().Contain(ApplicationErrors.Auth.Forbidden.Code);
        _notFoundCodes.Should().Contain("User.NotFound");
    }

    [Fact]
    public void Russian_HasEveryDomainError()
    {
        _domainCodes.Where(code => !_russian.ContainsKey(code)).Should().BeEmpty("Russian users read every error message in Russian");
    }

    [Fact]
    public void Russian_HasEveryApplicationError()
    {
        _applicationCodes.Where(code => !_russian.ContainsKey(code)).Should().BeEmpty();
    }

    [Fact]
    public void RussianAndEnglish_HaveNotFoundForEveryEntity()
    {
        _notFoundCodes.Where(code => !_russian.ContainsKey(code)).Should().BeEmpty();
        _notFoundCodes.Where(code => !_neutral.ContainsKey(code)).Should().BeEmpty(
            "the generated English message uses the type name, for example DrivingSlot");
    }

    [Fact]
    public void Resources_HaveNoUnknownCodes()
    {
        var known = _domainCodes.Concat(_applicationCodes).Concat(_notFoundCodes).ToHashSet();

        _russian.Keys.Concat(_neutral.Keys).Where(code => !known.Contains(code))
            .Should().BeEmpty("a message for a removed or renamed error is never shown");
    }

    [Fact]
    public void Russian_MessagesAreCyrillicSentences()
    {
        var violating = _russian
            .Where(entry => !entry.Value.EndsWith('.') || !entry.Value.Any(letter => letter is >= 'а' and <= 'я'))
            .Select(entry => entry.Key);

        violating.Should().BeEmpty();
    }

    [Fact]
    public void English_MessagesAreSentences()
    {
        _neutral.Where(entry => !entry.Value.EndsWith('.')).Select(entry => entry.Key).Should().BeEmpty();
    }
}
