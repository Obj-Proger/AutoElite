using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Payments;
using AutoElite.Application.Payments.GetContractPayments;
using AutoElite.Application.Payments.GetMyPayments;
using AutoElite.Application.Payments.GetPayments;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Payments;

public class PaymentQueryHandlerTests
{
    private readonly FakePaymentQueries _queries = new();

    private static FakeCurrentUser Student(Guid userId) =>
        new() { UserId = userId, Roles = new HashSet<UserRole> { UserRole.Student } };

    private static PaymentItem Item(Guid studentId) => new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "AE-1", studentId, "Ivanov Ivan", 15000m, "RUB", PaymentMethod.Cash,
        "Tuition", null, PaymentStatus.Confirmed, TestData.Now, null, null, TestData.Now, null, null, null);

    private Guid SeedContractHistory(Guid studentId, Guid? branchId = null)
    {
        var contractId = Guid.CreateVersion7();
        _queries.Seed(contractId, new ContractPayments(studentId, branchId, [Item(studentId)]));

        return contractId;
    }

    private Task<Result<IReadOnlyList<PaymentItem>>> GetHistory(Guid contractId, FakeCurrentUser caller) =>
        new GetContractPaymentsQueryHandler(_queries, caller)
            .Handle(new GetContractPaymentsQuery(contractId), TestContext.Current.CancellationToken);

    private Task<Result<PaymentReport>> GetReport(GetPaymentsQuery query, FakeCurrentUser caller) =>
        new GetPaymentsQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetHistory_ManagerOfTheBranch_ReadsThePayments()
    {
        var branchId = Guid.CreateVersion7();
        var contractId = SeedContractHistory(Guid.CreateVersion7(), branchId);

        var result = await GetHistory(contractId, TestData.Manager(branchId));

        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetHistory_OwnContractOfTheStudent_ReadsThePayments()
    {
        var studentId = Guid.CreateVersion7();
        var contractId = SeedContractHistory(studentId);

        var result = await GetHistory(contractId, Student(studentId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetHistory_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var contractId = SeedContractHistory(Guid.CreateVersion7());

        var result = await GetHistory(contractId, Student(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task GetHistory_ManagerOfAnotherBranch_ReturnsContractNotFound()
    {
        var contractId = SeedContractHistory(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await GetHistory(contractId, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task GetMyPayments_Student_ReadsOnlyOwnPayments()
    {
        var studentId = Guid.CreateVersion7();
        var own = Item(studentId);
        _queries.StudentPayments.Add(own);
        _queries.StudentPayments.Add(Item(Guid.CreateVersion7()));

        var result = await new GetMyPaymentsQueryHandler(_queries, Student(studentId))
            .Handle(new GetMyPaymentsQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(own);
    }

    [Fact]
    public async Task GetReport_Filters_PassesThemWithTheScopeAndReturnsTheTotals()
    {
        var branchId = Guid.CreateVersion7();
        var managerId = Guid.CreateVersion7();
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);

        var result = await GetReport(
            new GetPaymentsQuery(from, to, PaymentMethod.BankCard, PaymentStatus.Confirmed, managerId, branchId, " AE ", 2, 50),
            TestData.Manager(branchId));

        _queries.LastReportCall.Should().Be((
            new PaymentReportFilter(from, to, PaymentMethod.BankCard, PaymentStatus.Confirmed, managerId, branchId, "AE"),
            DataScope.ForBranch(branchId),
            new PageRequest(2, 50)));
        result.Value.Totals.Should().Equal(new CurrencyTotal("RUB", 15000m, 1));
    }

    [Fact]
    public async Task GetReport_ManagerAsksForAnotherBranch_ReturnsAnEmptyReportWithoutReading()
    {
        var result = await GetReport(new GetPaymentsQuery(BranchId: Guid.CreateVersion7()), TestData.Manager(Guid.CreateVersion7()));

        result.Value.Page.Items.Should().BeEmpty();
        result.Value.Totals.Should().BeEmpty();
        _queries.LastReportCall.Should().BeNull();
    }

    [Fact]
    public void ReportValidator_PeriodEndsBeforeItStarts_ReportsPeriodInvalid()
    {
        var query = new GetPaymentsQuery(new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1));

        var result = new GetPaymentsQueryValidator().Validate(query);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Payment.PeriodInvalid.Code);
    }

    [Fact]
    public void ReportValidator_UndefinedMethodAndStatus_ReportsBoth()
    {
        var query = new GetPaymentsQuery(Method: (PaymentMethod)99, Status: (PaymentStatus)99);

        var codes = new GetPaymentsQueryValidator().Validate(query).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.Payment.MethodNotDefined.Code, ApplicationErrors.Payment.StatusNotDefined.Code);
    }
}
