using AutoElite.Application.Settings;

namespace AutoElite.Application.Courses;

/// <summary>Creates the price of a course from command values.</summary>
internal sealed class CoursePrice(SchoolSettingsProvider settingsProvider)
{
    /// <summary>Creates the price in the given currency, or in the school's default currency if none is given.</summary>
    /// <param name="amount">The amount.</param>
    /// <param name="currencyCode">The ISO 4217 code, or <see langword="null"/> for the default currency.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Money>> CreateAsync(decimal amount, string? currencyCode, CancellationToken cancellationToken)
    {
        Currency currency;
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            currency = (await settingsProvider.GetAsync(cancellationToken)).DefaultCurrency;
        }
        else
        {
            var currencyResult = Currency.Create(currencyCode);
            if (currencyResult.IsFailure)
                return Result.Failure<Money>(currencyResult.Error);

            currency = currencyResult.Value;
        }

        return Money.Create(amount, currency);
    }
}
