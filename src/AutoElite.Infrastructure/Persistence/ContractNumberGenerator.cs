using System.Globalization;
using AutoElite.Application.Contracts;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// Gives out contract numbers such as <c>2026-000123</c> from a counter per year. The counter is advanced by a single
/// atomic statement outside the request's changes, so concurrent requests never get the same number; a number given
/// to a request that then fails is skipped, as with a database sequence.
/// </summary>
/// <remarks>
/// Staff may also type numbers by hand. A generated number that happens to be taken already is skipped, so the
/// numbering stays unique without the two kinds of numbers having to differ.
/// </remarks>
internal sealed class ContractNumberGenerator(AutoEliteDbContext dbContext) : IContractNumberGenerator
{
    /// <summary>The most taken numbers in a row that are skipped before giving up.</summary>
    private const int MaxSkippedNumbers = 100;

    /// <inheritdoc />
    public async Task<string> NextAsync(DateOnly startDate, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxSkippedNumbers; attempt++)
        {
            var number = Format(startDate.Year, await AdvanceAsync(startDate.Year, cancellationToken));
            if (!await dbContext.Set<Contract>().AnyAsync(contract => contract.Number == number, cancellationToken))
                return number;
        }

        throw new InvalidOperationException(
            $"Could not find a free contract number for {startDate.Year} after {MaxSkippedNumbers} attempts.");
    }

    /// <summary>Formats a number: the year and the value padded to six digits.</summary>
    /// <param name="year">The year.</param>
    /// <param name="value">The value of the counter.</param>
    internal static string Format(int year, long value) =>
        string.Create(CultureInfo.InvariantCulture, $"{year}-{value:D6}");

    /// <remarks>Run through the connection directly, since EF Core composes raw queries it reads rows from.</remarks>
    private async Task<long> AdvanceAsync(int year, CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = """
                INSERT INTO contract_number_counters AS counter (year, last_value)
                VALUES (@year, 1)
                ON CONFLICT (year) DO UPDATE SET last_value = counter.last_value + 1
                RETURNING counter.last_value
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "year";
            parameter.Value = year;
            command.Parameters.Add(parameter);

            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
