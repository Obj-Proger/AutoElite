namespace AutoElite.Application.Contracts;

/// <summary>The terms of a new contract, as given in a command.</summary>
/// <param name="CourseId">The course.</param>
/// <param name="Transmission">The gearbox the student trains on.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training.</param>
/// <param name="PriceAmount">The agreed price in the course currency; <see langword="null"/> for the course price.</param>
/// <param name="Number">The number of the paper contract; empty to give out the next number.</param>
internal sealed record ContractTerms(
    Guid CourseId,
    Transmission Transmission,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? PriceAmount,
    string? Number);
