namespace AutoElite.Infrastructure.Persistence;

/// <summary>The last contract number given out in a year; numbering starts again every year.</summary>
internal sealed class ContractNumberCounter
{
    private ContractNumberCounter()
    {
    }

    /// <summary>Gets the year the numbers belong to.</summary>
    public int Year { get; private set; }

    /// <summary>Gets the last number given out in the year.</summary>
    public long LastValue { get; private set; }
}
