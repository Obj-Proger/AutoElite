namespace AutoElite.Mobile.Pages;

/// <summary>A student expected at a theory lesson, whom the instructor marks present or absent.</summary>
public sealed class AttendanceEntry : ViewModel
{
    /// <summary>Gets the contract of the student, which the mark is for.</summary>
    public required Guid ContractId { get; init; }

    /// <summary>Gets the full name of the student.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the number of the contract, which tells apart students with the same name.</summary>
    public required string Contract { get; init; }

    /// <summary>Gets or sets a value indicating whether the student attended.</summary>
    public bool IsPresent
    {
        get;
        set => SetProperty(ref field, value);
    }
}
