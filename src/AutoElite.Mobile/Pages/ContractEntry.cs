namespace AutoElite.Mobile.Pages;

/// <summary>A contract of the student with the progress of the training and its payments.</summary>
public sealed class ContractEntry
{
    /// <summary>Gets the course of the contract.</summary>
    public required string Course { get; init; }

    /// <summary>Gets the number and the state of the contract.</summary>
    public required string Number { get; init; }

    /// <summary>Gets the driving hours completed out of those of the course.</summary>
    public required string Practice { get; init; }

    /// <summary>Gets the share of the driving hours completed, from 0 to 1.</summary>
    public double PracticeProgress { get; init; }

    /// <summary>Gets the theory lessons attended.</summary>
    public required string Theory { get; init; }

    /// <summary>Gets the quality score of the training, if any lesson was graded.</summary>
    public string? Quality { get; init; }

    /// <summary>Gets a value indicating whether the training has a quality score.</summary>
    public bool HasQuality => Quality is not null;

    /// <summary>Gets what was paid out of the price, and what is still owed.</summary>
    public required string Money { get; init; }

    /// <summary>Gets the payments of the contract, the latest first.</summary>
    public IReadOnlyList<string> Payments { get; init; } = [];

    /// <summary>Gets a value indicating whether the contract has payments.</summary>
    public bool HasPayments => Payments.Count > 0;
}
