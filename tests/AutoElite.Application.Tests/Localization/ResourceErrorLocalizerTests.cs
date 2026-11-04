using System.Globalization;
using AutoElite.Application.Common.Localization;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Localization;

public class ResourceErrorLocalizerTests
{
    private static readonly CultureInfo _russian = CultureInfo.GetCultureInfo("ru");
    private static readonly CultureInfo _english = CultureInfo.GetCultureInfo("en");

    private readonly ResourceErrorLocalizer _localizer = new();

    [Fact]
    public void Localize_DomainErrorInRussian_TranslatesMessageAndKeepsCodeAndType()
    {
        var error = DomainErrors.Email.InvalidFormat;

        var localized = _localizer.Localize(error, _russian);

        localized.Message.Should().Be("Неверный формат адреса электронной почты.");
        localized.Code.Should().Be(error.Code);
        localized.Type.Should().Be(error.Type);
    }

    [Fact]
    public void Localize_RegionalCulture_FallsBackToItsLanguage()
    {
        var localized = _localizer.Localize(ApplicationErrors.Auth.Forbidden, CultureInfo.GetCultureInfo("ru-RU"));

        localized.Message.Should().Be("У вас нет прав на это действие.");
    }

    [Fact]
    public void Localize_DomainErrorInEnglish_KeepsTheMessageInCode()
    {
        var localized = _localizer.Localize(DomainErrors.Email.InvalidFormat, _english);

        localized.Should().Be(DomainErrors.Email.InvalidFormat);
    }

    [Fact]
    public void Localize_NotFoundInEnglish_UsesTheReadableEntityName()
    {
        var localized = _localizer.Localize(ApplicationErrors.NotFound<DrivingSlot>(), _english);

        localized.Message.Should().Be("Driving slot was not found.");
    }

    [Fact]
    public void Localize_UnknownCode_KeepsTheMessage()
    {
        var error = Error.Conflict("Test.Unknown", "Something went wrong.");

        _localizer.Localize(error, _russian).Should().Be(error);
    }

    [Fact]
    public void Localize_ValidationError_TranslatesErrorAndKnownFieldErrors()
    {
        var error = ApplicationErrors.ValidationFailed(
        [
            new FieldError("Email", DomainErrors.Email.Empty.Code, DomainErrors.Email.Empty.Message),
            new FieldError("Name", "NotEmptyValidator", "'Name' must not be empty.")
        ]);

        var localized = _localizer.Localize(error, _russian);

        localized.Message.Should().Be("Одна или несколько ошибок проверки данных.");
        localized.FieldErrors.Should().Equal(
            new FieldError("Email", DomainErrors.Email.Empty.Code, "Адрес электронной почты не может быть пустым."),
            new FieldError("Name", "NotEmptyValidator", "'Name' must not be empty."));
    }

    [Fact]
    public void Localize_ErrorWithoutFieldErrors_KeepsThemAbsent()
    {
        _localizer.Localize(DomainErrors.Email.Empty, _russian).FieldErrors.Should().BeNull();
    }
}
