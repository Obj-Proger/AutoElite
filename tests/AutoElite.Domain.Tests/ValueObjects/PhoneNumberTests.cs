namespace AutoElite.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Fact]
    public void Create_ValidE164Number_ReturnsPhoneNumber()
    {
        var result = PhoneNumber.Create("+79991234567");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+79991234567");
    }

    [Fact]
    public void Create_NumberWithFormatting_ReturnsNormalizedNumber()
    {
        var result = PhoneNumber.Create("+7 (999) 123-45-67");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+79991234567");
    }

    [Fact]
    public void Create_NumberWithSurroundingWhitespace_ReturnsTrimmedNumber()
    {
        var result = PhoneNumber.Create("  +79991234567  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+79991234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespace_ReturnsEmptyError(string? rawValue)
    {
        var result = PhoneNumber.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.PhoneNumber.Empty);
    }

    [Theory]
    [InlineData("79991234567")]
    [InlineData("89991234567")]
    [InlineData("+09991234567")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+7999abc4567")]
    [InlineData("+")]
    [InlineData("++79991234567")]
    [InlineData("---")]
    [InlineData("() -")]
    public void Create_InvalidFormat_ReturnsInvalidFormatError(string rawValue)
    {
        var result = PhoneNumber.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.PhoneNumber.InvalidFormat);
    }

    [Fact]
    public void Create_ShortestValidNumber_ReturnsPhoneNumber()
    {
        var result = PhoneNumber.Create("+12345678");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+12345678");
    }

    [Fact]
    public void Create_LongestValidNumber_ReturnsPhoneNumber()
    {
        var result = PhoneNumber.Create("+123456789012345");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+123456789012345");
    }

    [Fact]
    public void Equality_SameNumberWithDifferentFormatting_AreEqual()
    {
        var first = PhoneNumber.Create("+7 999 123-45-67").Value;
        var second = PhoneNumber.Create("+79991234567").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentNumbers_AreNotEqual()
    {
        var first = PhoneNumber.Create("+79991234567").Value;
        var second = PhoneNumber.Create("+79991234568").Value;

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var phoneNumber = PhoneNumber.Create("+79991234567").Value;

        phoneNumber.ToString().Should().Be("+79991234567");
    }
}
