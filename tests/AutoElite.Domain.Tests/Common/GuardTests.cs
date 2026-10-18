namespace AutoElite.Domain.Tests.Common;

public class GuardTests
{
    private const int MaxLength = 5;

    private static readonly Error _emptyError = Error.Validation("Test.Empty", "Value is empty.");
    private static readonly Error _tooLongError = Error.Validation("Test.TooLong", "Value is too long.");
    private static readonly Error _error = Error.Validation("Test.Invalid", "Value is invalid.");
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private enum SampleEnum
    {
        First = 1,
        Second = 2
    }

    [Fact]
    public void RequiredText_ValidValue_ReturnsValue()
    {
        var result = Guard.RequiredText("abc", MaxLength, _emptyError, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc");
    }

    [Fact]
    public void RequiredText_ValueWithSurroundingWhitespace_ReturnsTrimmedValue()
    {
        var result = Guard.RequiredText("  abc  ", MaxLength, _emptyError, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredText_NullEmptyOrWhitespace_ReturnsEmptyError(string? value)
    {
        var result = Guard.RequiredText(value, MaxLength, _emptyError, _tooLongError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_emptyError);
    }

    [Fact]
    public void RequiredText_ValueExactlyAtMaximumLength_ReturnsValue()
    {
        var result = Guard.RequiredText("abcde", MaxLength, _emptyError, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abcde");
    }

    [Fact]
    public void RequiredText_ValueExceedingMaximumLength_ReturnsTooLongError()
    {
        var result = Guard.RequiredText("abcdef", MaxLength, _emptyError, _tooLongError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_tooLongError);
    }

    [Fact]
    public void RequiredText_LengthIsMeasuredAfterTrimming_ReturnsValue()
    {
        var result = Guard.RequiredText("  abcde  ", MaxLength, _emptyError, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abcde");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OptionalText_NullEmptyOrWhitespace_ReturnsSuccessWithNull(string? value)
    {
        var result = Guard.OptionalText(value, MaxLength, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void OptionalText_ValidValue_ReturnsTrimmedValue()
    {
        var result = Guard.OptionalText("  abc  ", MaxLength, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abc");
    }

    [Fact]
    public void OptionalText_ValueExactlyAtMaximumLength_ReturnsValue()
    {
        var result = Guard.OptionalText("abcde", MaxLength, _tooLongError);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("abcde");
    }

    [Fact]
    public void OptionalText_ValueExceedingMaximumLength_ReturnsTooLongError()
    {
        var result = Guard.OptionalText("abcdef", MaxLength, _tooLongError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_tooLongError);
    }

    [Fact]
    public void NotEmpty_EmptyGuid_ReturnsGivenError()
    {
        var result = Guard.NotEmpty(Guid.Empty, _error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }

    [Fact]
    public void NotEmpty_NonEmptyGuid_ReturnsSameValue()
    {
        var value = new Guid("11111111-1111-1111-1111-111111111111");

        var result = Guard.NotEmpty(value, _error);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void NotPast_PastValue_ReturnsGivenError()
    {
        var result = Guard.NotPast(_now.AddMinutes(-1), _now, _error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }

    [Fact]
    public void NotPast_ValueEqualToNow_ReturnsGivenError()
    {
        var result = Guard.NotPast(_now, _now, _error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }

    [Fact]
    public void NotPast_ValueJustAfterNow_ReturnsValue()
    {
        var value = _now.AddTicks(1);

        var result = Guard.NotPast(value, _now, _error);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void DefinedEnum_DeclaredMember_ReturnsSameValue()
    {
        var result = Guard.DefinedEnum(SampleEnum.Second, _error);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SampleEnum.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void DefinedEnum_UndeclaredValue_ReturnsGivenError(int raw)
    {
        var result = Guard.DefinedEnum((SampleEnum)raw, _error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }
}
