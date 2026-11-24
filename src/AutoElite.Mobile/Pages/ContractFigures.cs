using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>The progress of a contract as figures for tiles: the driving hours, the theory lessons and the money.</summary>
public sealed class ContractFigures
{
    /// <summary>Gets the hours driven, such as 10,5.</summary>
    public required string DrivingHours { get; init; }

    /// <summary>Gets the hours of the course, such as of 56 h.</summary>
    public required string CourseHours { get; init; }

    /// <summary>Gets the share of the hours of the course driven, from 0 to 1.</summary>
    public double DrivingProgress { get; init; }

    /// <summary>Gets the number of theory lessons attended.</summary>
    public required string TheoryAttended { get; init; }

    /// <summary>Gets the amount owed, or else paid, without the currency.</summary>
    public required string Money { get; init; }

    /// <summary>Gets what the amount is, with its currency: owed or paid.</summary>
    public required string MoneyUnit { get; init; }

    /// <summary>Gets the share of the price paid, from 0 to 1.</summary>
    public double PaidProgress { get; init; }

    /// <summary>Gets what the money tells: a warning while something is owed, else success.</summary>
    public StatusTone MoneyTone { get; init; }

    /// <summary>Makes the figures of a contract.</summary>
    /// <param name="contract">The contract.</param>
    /// <returns>The figures.</returns>
    public static ContractFigures Of(ContractDetails contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var culture = CultureInfo.CurrentCulture;
        var driven = contract.CompletedPracticeHours ?? 0;
        var course = contract.CoursePracticeHours ?? 0;
        var price = contract.PriceAmount ?? 0;
        var paid = contract.PaidAmount ?? 0;
        var debt = contract.DebtAmount ?? 0;

        return new ContractFigures
        {
            DrivingHours = driven.ToString("0.#", culture),
            CourseHours = AppTexts.Format("Today.OfHours", course),
            DrivingProgress = course > 0 ? Math.Clamp(driven / course, 0, 1) : 0,
            TheoryAttended = (contract.AttendedTheoryLessons ?? 0).ToString(culture),
            Money = (debt > 0 ? debt : paid).ToString("N0", culture),
            MoneyUnit = debt > 0
                ? AppTexts.Format("Study.Due", contract.Currency)
                : AppTexts.Format("Study.Paid", contract.Currency),
            PaidProgress = price > 0 ? Math.Clamp(paid / price, 0, 1) : 1,
            MoneyTone = debt > 0 ? StatusTone.Warning : StatusTone.Success
        };
    }
}
