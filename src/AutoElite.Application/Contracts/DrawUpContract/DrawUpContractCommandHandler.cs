namespace AutoElite.Application.Contracts.DrawUpContract;

/// <summary>Checks the student and the branch, then draws up the contract.</summary>
internal sealed class DrawUpContractCommandHandler(
    ContractStudents contractStudents,
    ContractFactory contractFactory,
    IRepository<Branch> branchRepository,
    ICurrentUser currentUser) : ICommandHandler<DrawUpContractCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(DrawUpContractCommand command, CancellationToken cancellationToken)
    {
        var studentResult = await contractStudents.LoadAsync(command.StudentId, cancellationToken);
        if (studentResult.IsFailure)
            return Result.Failure<Guid>(studentResult.Error);

        var student = studentResult.Value;
        var scope = DataScope.For(currentUser);
        var branchId = command.BranchId ?? student.BranchId ?? scope.BranchId;
        var branchResult = await BranchChecks.CheckAsync(branchRepository, branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var terms = new ContractTerms(
            command.CourseId, command.Transmission, command.StartDate, command.EndDate, command.PriceAmount, command.Number);
        var contractResult = await contractFactory.CreateAsync(student, terms, branchId, cancellationToken);

        return contractResult.IsFailure ? Result.Failure<Guid>(contractResult.Error) : contractResult.Value.Id;
    }
}
