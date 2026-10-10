namespace AutoElite.Domain.ValueObjects;

/// <summary>
/// A person's full name: last name, first name, and an optional middle name (patronymic).
/// </summary>
public sealed class FullName : ValueObject
{
    private const int MaxNamePartLength = 100;

    /// <summary>Gets the last name.</summary>
    public string LastName { get; }

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; }

    /// <summary>Gets the middle name, or <see langword="null"/> if the person has none.</summary>
    public string? MiddleName { get; }

    private FullName(string lastName, string firstName, string? middleName)
    {
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
    }

    /// <summary>Validates and creates a <see cref="FullName"/> from raw input.</summary>
    /// <param name="lastName">The last name. Required.</param>
    /// <param name="firstName">The first name. Required.</param>
    /// <param name="middleName">The middle name. Optional; empty values are treated as not provided.</param>
    /// <returns>The created name, or the first validation failure encountered.</returns>
    public static Result<FullName> Create(string? lastName, string? firstName, string? middleName = null)
    {
        var lastNameResult = Guard.RequiredText(lastName, MaxNamePartLength, DomainErrors.FullName.LastNameEmpty, DomainErrors.FullName.LastNameTooLong);
        if (lastNameResult.IsFailure)
            return Result.Failure<FullName>(lastNameResult.Error);

        var firstNameResult = Guard.RequiredText(firstName, MaxNamePartLength, DomainErrors.FullName.FirstNameEmpty, DomainErrors.FullName.FirstNameTooLong);
        if (firstNameResult.IsFailure)
            return Result.Failure<FullName>(firstNameResult.Error);

        var middleNameResult = Guard.OptionalText(middleName, MaxNamePartLength, DomainErrors.FullName.MiddleNameTooLong);
        if (middleNameResult.IsFailure)
            return Result.Failure<FullName>(middleNameResult.Error);

        return new FullName(lastNameResult.Value, firstNameResult.Value, middleNameResult.Value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LastName;
        yield return FirstName;
        yield return MiddleName;
    }

    /// <inheritdoc />
    public override string ToString() => MiddleName is null ? $"{LastName} {FirstName}" : $"{LastName} {FirstName} {MiddleName}";
}
