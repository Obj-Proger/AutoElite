namespace AutoElite.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ReturnsEmail()
    {
        var result = Email.Create("ivan@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ivan@example.com");
    }

    [Fact]
    public void Create_EmailWithSurroundingWhitespace_ReturnsTrimmedEmail()
    {
        var result = Email.Create("  ivan@example.com  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ivan@example.com");
    }

    [Fact]
    public void Create_UppercaseEmail_ReturnsLowercasedEmail()
    {
        var result = Email.Create("Ivan@Example.COM");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ivan@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespace_ReturnsEmptyError(string? rawValue)
    {
        var result = Email.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Email.Empty);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@example.com")]
    [InlineData("ivan@")]
    [InlineData("ivan@example")]
    [InlineData("ivan@example.")]
    [InlineData("iv an@example.com")]
    [InlineData("ivan@@example.com")]
    public void Create_InvalidFormat_ReturnsInvalidFormatError(string rawValue)
    {
        var result = Email.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Email.InvalidFormat);
    }

    [Fact]
    public void Create_LengthExactlyAtMaximum_ReturnsEmail()
    {
        var rawValue = new string('a', 314) + "@b.com";

        var result = Email.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_LengthExceedingMaximum_ReturnsTooLongError()
    {
        var rawValue = new string('a', 315) + "@b.com";

        var result = Email.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Email.TooLong);
    }

    [Fact]
    public void Equality_SameAddressInDifferentCase_AreEqual()
    {
        var first = Email.Create("Ivan@Example.com").Value;
        var second = Email.Create("ivan@example.com").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentAddresses_AreNotEqual()
    {
        var first = Email.Create("ivan@example.com").Value;
        var second = Email.Create("petr@example.com").Value;

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var email = Email.Create("ivan@example.com").Value;

        email.ToString().Should().Be("ivan@example.com");
    }
}
