namespace AutoElite.Application.Contracts.ConvertLeadToContract;

/// <summary>
/// Turns an open lead into a training contract in one action (FR-2.6). The student is either an existing one or a
/// new one registered from the given details, who gets an invitation; the contract takes the lead's branch, and the
/// lead is marked converted. Everything is saved together or not at all.
/// </summary>
/// <param name="LeadId">The lead to convert.</param>
/// <param name="ExistingStudentId">An existing student; empty to register a new one from the details below.</param>
/// <param name="LastName">The last name of a new student.</param>
/// <param name="FirstName">The first name of a new student.</param>
/// <param name="MiddleName">The middle name of a new student, if any.</param>
/// <param name="Email">The email of a new student; empty for the lead's email.</param>
/// <param name="Phone">The phone of a new student; empty for the lead's phone.</param>
/// <param name="PreferredLanguage">The language of a new student.</param>
/// <param name="CourseId">The course.</param>
/// <param name="Transmission">The gearbox the student trains on.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training, later than the start.</param>
/// <param name="PriceAmount">The agreed price in the course currency; empty for the course price.</param>
/// <param name="Number">The number of the paper contract, unique; empty to give out the next number.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ConvertLeadToContractCommand(
    Guid LeadId,
    Guid? ExistingStudentId,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Email,
    string? Phone,
    Language PreferredLanguage,
    Guid CourseId,
    Transmission Transmission,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? PriceAmount = null,
    string? Number = null) : ICommand<LeadConversion>;
