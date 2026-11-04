using AutoElite.Application.Leads;
using AutoElite.Application.Users;

namespace AutoElite.Application.Contracts.ConvertLeadToContract;

/// <summary>
/// Settles the student, draws up the contract in the lead's branch and marks the lead converted. A failure at any
/// step leaves nothing behind, because the unit of work saves only after success.
/// </summary>
internal sealed class ConvertLeadToContractCommandHandler(
    LeadAccess leadAccess,
    ContractStudents contractStudents,
    UserRegistration userRegistration,
    ContractFactory contractFactory,
    IRepository<Branch> branchRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ConvertLeadToContractCommand, LeadConversion>
{
    /// <inheritdoc />
    public async Task<Result<LeadConversion>> Handle(ConvertLeadToContractCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure<LeadConversion>(leadResult.Error);

        var lead = leadResult.Value;
        if (!lead.IsOpen)
            return Result.Failure<LeadConversion>(DomainErrors.Lead.NotOpen);

        var scope = DataScope.For(currentUser);
        var branchId = lead.BranchId ?? scope.BranchId;
        var branchResult = await BranchChecks.CheckAsync(branchRepository, branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<LeadConversion>(branchResult.Error);

        var studentResult = await SettleStudentAsync(command, lead, branchId, cancellationToken);
        if (studentResult.IsFailure)
            return Result.Failure<LeadConversion>(studentResult.Error);

        var student = studentResult.Value;
        var terms = new ContractTerms(
            command.CourseId, command.Transmission, command.StartDate, command.EndDate, command.PriceAmount, command.Number);
        var contractResult = await contractFactory.CreateAsync(student, terms, branchId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure<LeadConversion>(contractResult.Error);

        var contract = contractResult.Value;
        var convertResult = lead.MarkConverted(contract.Id, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);

        return convertResult.IsFailure
            ? Result.Failure<LeadConversion>(convertResult.Error)
            : new LeadConversion(student.Id, contract.Id);
    }

    private Task<Result<User>> SettleStudentAsync(
        ConvertLeadToContractCommand command, Lead lead, Guid? branchId, CancellationToken cancellationToken)
    {
        if (command.ExistingStudentId is { } studentId)
            return contractStudents.LoadAsync(studentId, cancellationToken);

        var details = new NewUserDetails(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            string.IsNullOrWhiteSpace(command.Email) ? lead.Email?.Value : command.Email,
            string.IsNullOrWhiteSpace(command.Phone) ? lead.Phone?.Value : command.Phone,
            branchId,
            command.PreferredLanguage);

        return userRegistration.RegisterAsync(details, [UserRole.Student], cancellationToken);
    }
}
