using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class ContractPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly PageRequest _firstPage = new(1, PageRequest.MaxPageSize);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task<Contract> LoadAsync(Guid contractId)
    {
        await using var context = database.CreateContext();

        return (await new ContractRepository(context).GetByIdAsync(contractId, CancellationToken))!;
    }

    [Fact]
    public async Task Save_ContractWithPaymentsAndGrades_StoresTheProgress()
    {
        var student = TrainingRecords.Person("Смирнов");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        await SaveAsync(student, course, contract);

        await using (var context = database.CreateContext())
        {
            var tracked = await new ContractRepository(context).GetByIdAsync(contract.Id, CancellationToken);
            tracked!.ApplyPayment(Money.Create(15_000.25m, TrainingRecords.Rubles).Value).IsSuccess.Should().BeTrue();
            tracked.RecordPracticeLesson(1.5m, 4m, 10).IsSuccess.Should().BeTrue();
            tracked.RecordPracticeLesson(2m, 5m, 10).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        var loaded = await LoadAsync(contract.Id);
        loaded.Price.Should().Be(course.Price);
        loaded.PaidAmount.Should().Be(Money.Create(15_000.25m, TrainingRecords.Rubles).Value);
        loaded.Debt.Amount.Should().Be(44_999.75m);
        loaded.CompletedPracticeHours.Should().Be(3.5m);
        loaded.RecentGrades.Should().Equal(4m, 5m);
        loaded.QualityScore.Should().Be(4.5m);
        loaded.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public async Task Save_NumberTaken_ThrowsConcurrencyConflict()
    {
        var student = TrainingRecords.Person("Козлов");
        var course = TrainingRecords.NewCourse();
        var number = TrainingRecords.Number();
        await SaveAsync(student, course, TrainingRecords.NewContract(number, student, course));
        await using (var context = database.CreateContext())
            (await new ContractRepository(context).NumberExistsAsync(number, CancellationToken)).Should().BeTrue();

        var save = () => SaveAsync(TrainingRecords.NewContract(number, student, course));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task NextNumber_CountsPerYearAndSkipsTakenNumbers()
    {
        var student = TrainingRecords.Person("Новиков");
        var course = TrainingRecords.NewCourse();
        await SaveAsync(student, course, TrainingRecords.NewContract(ContractNumberGenerator.Format(2102, 2), student, course));
        await using var context = database.CreateContext();
        var numbers = new ContractNumberGenerator(context);

        var first = await numbers.NextAsync(new DateOnly(2101, 5, 1), CancellationToken);
        var second = await numbers.NextAsync(new DateOnly(2101, 9, 1), CancellationToken);
        var otherYear = await numbers.NextAsync(new DateOnly(2102, 1, 1), CancellationToken);
        var afterTaken = await numbers.NextAsync(new DateOnly(2102, 1, 1), CancellationToken);

        first.Should().Be("2101-000001");
        second.Should().Be("2101-000002");
        otherYear.Should().Be("2102-000001");
        afterTaken.Should().Be("2102-000003");
    }

    [Fact]
    public async Task NextNumber_ConcurrentRequests_GetDifferentNumbers()
    {
        var numbers = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = database.CreateContext();

            return await new ContractNumberGenerator(context).NextAsync(new DateOnly(2103, 1, 1), CancellationToken);
        }));

        numbers.Should().OnlyHaveUniqueItems().And.HaveCount(10);
    }

    [Fact]
    public async Task GetList_SearchesNumberAndStudentAndFiltersByDebtAndScope()
    {
        var marker = Guid.NewGuid().ToString("N")[..10];
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var student = TrainingRecords.Person("Orlov" + marker);
        var otherStudent = TrainingRecords.Person("Павлов");
        var course = TrainingRecords.NewCourse();
        var earlier = TrainingRecords.NewContract($"A-{marker}-1", student, course, branch.Id, new DateOnly(2026, 1, 10));
        var later = TrainingRecords.NewContract($"A-{marker}-2", student, course, null, new DateOnly(2026, 3, 10));
        var paid = TrainingRecords.NewContract($"B-{marker}-3", otherStudent, course, otherBranch.Id);
        paid.ApplyPayment(course.Price).IsSuccess.Should().BeTrue();
        await SaveAsync(branch, otherBranch, student, otherStudent, course, earlier, later, paid);
        await using var context = database.CreateContext();
        var queries = new ContractQueries(context);

        async Task<IEnumerable<Guid>> IdsAsync(ContractListFilter filter, DataScope? scope = null) =>
            (await queries.GetListAsync(filter, scope ?? DataScope.SchoolWide, _firstPage, CancellationToken))
                .Items.Select(item => item.Id);

        (await IdsAsync(Filter("orlov" + marker))).Should().Equal(later.Id, earlier.Id);
        (await IdsAsync(Filter($"b-{marker}"))).Should().Equal(paid.Id);
        (await IdsAsync(Filter(marker) with { HasDebt = false })).Should().Equal(paid.Id);
        (await IdsAsync(Filter(marker) with { HasDebt = true })).Should().Equal(later.Id, earlier.Id);
        (await IdsAsync(Filter(marker), DataScope.ForBranch(branch.Id))).Should().Equal(later.Id, earlier.Id);
    }

    [Fact]
    public async Task GetDetails_ReadsTheContractWithTheNamesAndAmounts()
    {
        var branch = TrainingRecords.NewBranch();
        var student = TrainingRecords.Person("Лебедев", "Олег", null);
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        contract.ApplyPayment(Money.Create(10_000m, TrainingRecords.Rubles).Value).IsSuccess.Should().BeTrue();
        await SaveAsync(branch, student, course, contract);
        await using var context = database.CreateContext();
        var queries = new ContractQueries(context);

        var details = await queries.GetDetailsAsync(contract.Id, CancellationToken);
        var ofStudent = await queries.GetOfStudentAsync(student.Id, CancellationToken);

        details!.StudentName.Should().Be("Лебедев Олег");
        details.CourseName.Should().Be(course.Name);
        details.BranchName.Should().Be(branch.Name);
        details.Currency.Should().Be("RUB");
        details.PriceAmount.Should().Be(60_000m);
        details.PaidAmount.Should().Be(10_000m);
        details.DebtAmount.Should().Be(50_000m);
        details.CoursePracticeHours.Should().Be(56);
        ofStudent.Should().ContainSingle().Which.Id.Should().Be(contract.Id);
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    private static ContractListFilter Filter(string search) => new(search, null, null, null, null, null);
}
