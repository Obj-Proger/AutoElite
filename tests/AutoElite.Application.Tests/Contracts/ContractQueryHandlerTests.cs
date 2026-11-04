using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts;
using AutoElite.Application.Contracts.GetContract;
using AutoElite.Application.Contracts.GetContracts;
using AutoElite.Application.Contracts.GetMyContracts;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Contracts;

public class ContractQueryHandlerTests
{
    private readonly FakeContractQueries _queries = new();

    private static readonly DateOnly _start = new(2026, 2, 1);

    private static FakeCurrentUser Student(Guid userId) =>
        new() { UserId = userId, Roles = new HashSet<UserRole> { UserRole.Student } };

    private ContractDetails SeedDetails(Guid studentId, Guid? branchId = null)
    {
        var details = new ContractDetails(
            Guid.CreateVersion7(), "AE-1", studentId, "Ivanov Ivan", Guid.CreateVersion7(), "Category B", LicenseCategory.B,
            Transmission.Manual, branchId, null, ContractStatus.Active, _start, _start.AddMonths(4), null, null, "RUB",
            45000m, 10000m, 35000m, 12.5m, 56, 8, 4.2m, TestData.Now);
        _queries.Seed(details);

        return details;
    }

    private Task<Result<ContractDetails>> GetContract(Guid contractId, FakeCurrentUser caller) =>
        new GetContractQueryHandler(_queries, caller).Handle(new GetContractQuery(contractId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetContracts_Filters_PassesThemWithTrimmedSearchAndTheScope()
    {
        var branchId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        await new GetContractsQueryHandler(_queries, TestData.Manager(branchId)).Handle(
            new GetContractsQuery(" AE-1 ", ContractStatus.Active, courseId, null, null, true, 2, 25),
            TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be((
            new ContractListFilter("AE-1", ContractStatus.Active, courseId, null, null, true),
            DataScope.ForBranch(branchId),
            new PageRequest(2, 25)));
    }

    [Fact]
    public async Task GetContracts_ManagerFiltersByAnotherBranch_ReturnsAnEmptyPageWithoutReading()
    {
        var result = await new GetContractsQueryHandler(_queries, TestData.Manager(Guid.CreateVersion7())).Handle(
            new GetContractsQuery(BranchId: Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Value.Items.Should().BeEmpty();
        _queries.LastListCall.Should().BeNull();
    }

    [Fact]
    public async Task GetContract_ManagerOfTheBranch_ReadsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = SeedDetails(Guid.CreateVersion7(), branchId);

        var result = await GetContract(details.Id, TestData.Manager(branchId));

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetContract_ManagerOfAnotherBranch_ReturnsContractNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await GetContract(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task GetContract_OwnContractOfTheStudent_ReadsTheCard()
    {
        var studentId = Guid.CreateVersion7();
        var details = SeedDetails(studentId);

        var result = await GetContract(details.Id, Student(studentId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetContract_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await GetContract(details.Id, Student(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task GetMyContracts_Student_ReadsOnlyOwnContracts()
    {
        var studentId = Guid.CreateVersion7();
        var own = new ContractListItem(
            Guid.CreateVersion7(), "AE-1", studentId, "Ivanov Ivan", Guid.CreateVersion7(), "Category B", LicenseCategory.B,
            Transmission.Manual, null, null, ContractStatus.Active, _start, _start.AddMonths(4), "RUB", 45000m, 0m, 45000m);
        _queries.Items.Add(own);
        _queries.Items.Add(own with { Id = Guid.CreateVersion7(), StudentId = Guid.CreateVersion7() });

        var result = await new GetMyContractsQueryHandler(_queries, Student(studentId))
            .Handle(new GetMyContractsQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(own);
    }

    [Fact]
    public void GetContractsValidator_UndefinedStatus_ReportsStatusNotDefined()
    {
        var result = new GetContractsQueryValidator().Validate(new GetContractsQuery(Status: (ContractStatus)99));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Contract.StatusNotDefined.Code);
    }
}
