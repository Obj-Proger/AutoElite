namespace AutoElite.Domain.Tests.ValueObjects;

public class FullNameTests
{
    [Fact]
    public void Create_AllParts_ReturnsFullName()
    {
        var result = FullName.Create("Ivanov", "Ivan", "Ivanovich");

        result.IsSuccess.Should().BeTrue();
        result.Value.LastName.Should().Be("Ivanov");
        result.Value.FirstName.Should().Be("Ivan");
        result.Value.MiddleName.Should().Be("Ivanovich");
    }

    [Fact]
    public void Create_WithoutMiddleName_ReturnsFullNameWithNullMiddleName()
    {
        var result = FullName.Create("Ivanov", "Ivan");

        result.IsSuccess.Should().BeTrue();
        result.Value.MiddleName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespaceMiddleName_ReturnsFullNameWithNullMiddleName(string? middleName)
    {
        var result = FullName.Create("Ivanov", "Ivan", middleName);

        result.IsSuccess.Should().BeTrue();
        result.Value.MiddleName.Should().BeNull();
    }

    [Fact]
    public void Create_PartsWithSurroundingWhitespace_ReturnsTrimmedParts()
    {
        var result = FullName.Create("  Ivanov  ", "  Ivan  ", "  Ivanovich  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.LastName.Should().Be("Ivanov");
        result.Value.FirstName.Should().Be("Ivan");
        result.Value.MiddleName.Should().Be("Ivanovich");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespaceLastName_ReturnsLastNameEmptyError(string? lastName)
    {
        var result = FullName.Create(lastName, "Ivan");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.LastNameEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespaceFirstName_ReturnsFirstNameEmptyError(string? firstName)
    {
        var result = FullName.Create("Ivanov", firstName);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.FirstNameEmpty);
    }

    [Fact]
    public void Create_LastNameAndFirstNameBothEmpty_ReportsLastNameFirst()
    {
        var result = FullName.Create(null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.LastNameEmpty);
    }

    [Fact]
    public void Create_PartsExactlyAtMaximumLength_ReturnsFullName()
    {
        var longName = new string('a', 100);

        var result = FullName.Create(longName, longName, longName);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_LastNameExceedingMaximumLength_ReturnsLastNameTooLongError()
    {
        var result = FullName.Create(new string('a', 101), "Ivan");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.LastNameTooLong);
    }

    [Fact]
    public void Create_FirstNameExceedingMaximumLength_ReturnsFirstNameTooLongError()
    {
        var result = FullName.Create("Ivanov", new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.FirstNameTooLong);
    }

    [Fact]
    public void Create_MiddleNameExceedingMaximumLength_ReturnsMiddleNameTooLongError()
    {
        var result = FullName.Create("Ivanov", "Ivan", new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.FullName.MiddleNameTooLong);
    }

    [Fact]
    public void ToString_WithMiddleName_ReturnsAllPartsInOrder()
    {
        var fullName = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;

        fullName.ToString().Should().Be("Ivanov Ivan Ivanovich");
    }

    [Fact]
    public void ToString_WithoutMiddleName_ReturnsLastAndFirstName()
    {
        var fullName = FullName.Create("Ivanov", "Ivan").Value;

        fullName.ToString().Should().Be("Ivanov Ivan");
    }

    [Fact]
    public void Equality_SameParts_AreEqual()
    {
        var first = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;
        var second = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentMiddleNames_AreNotEqual()
    {
        var first = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;
        var second = FullName.Create("Ivanov", "Ivan", "Petrovich").Value;

        first.Should().NotBe(second);
    }

    [Fact]
    public void Equality_MiddleNamePresentOnOneSide_AreNotEqual()
    {
        var first = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;
        var second = FullName.Create("Ivanov", "Ivan").Value;

        first.Should().NotBe(second);
    }
}
