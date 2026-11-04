namespace AutoElite.Application.Contracts;

/// <summary>Draws up a contract for a student on the given terms and adds it.</summary>
internal sealed class ContractFactory(
    IContractRepository contractRepository,
    IContractNumberGenerator numberGenerator,
    IRepository<Course> courseRepository,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Loads the course, settles the price and the number, creates the contract and adds it. The price is the course
    /// price unless another amount is agreed, always in the course currency.
    /// </summary>
    /// <param name="student">The student, already checked by the caller.</param>
    /// <param name="terms">The terms of the contract.</param>
    /// <param name="branchId">The branch, already checked by the caller.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Contract>> CreateAsync(
        User student, ContractTerms terms, Guid? branchId, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(terms.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure<Contract>(ApplicationErrors.NotFound<Course>());

        var priceResult = terms.PriceAmount is { } amount
            ? Money.Create(amount, course.Price.Currency)
            : Result.Success(course.Price);
        if (priceResult.IsFailure)
            return Result.Failure<Contract>(priceResult.Error);

        var numberResult = await SettleNumberAsync(terms, cancellationToken);
        if (numberResult.IsFailure)
            return Result.Failure<Contract>(numberResult.Error);

        var contractResult = Contract.Create(
            numberResult.Value,
            student,
            course,
            priceResult.Value,
            terms.Transmission,
            branchId,
            terms.StartDate,
            terms.EndDate,
            timeProvider.GetUtcNow().UtcDateTime);
        if (contractResult.IsFailure)
            return contractResult;

        contractRepository.Add(contractResult.Value);

        return contractResult;
    }

    private async Task<Result<string>> SettleNumberAsync(ContractTerms terms, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(terms.Number))
            return await numberGenerator.NextAsync(terms.StartDate, cancellationToken);

        var number = terms.Number.Trim();

        return await contractRepository.NumberExistsAsync(number, cancellationToken)
            ? Result.Failure<string>(ApplicationErrors.Contract.NumberTaken)
            : number;
    }
}
