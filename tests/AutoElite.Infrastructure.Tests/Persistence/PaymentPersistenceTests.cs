using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Payments;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class PaymentPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static readonly Currency _tenge = Currency.Create("KZT").Value;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static Payment Register(Contract contract, decimal amount, User? registrar, DateTime registeredAtUtc) =>
        Payment.Register(
                contract, Money.Create(amount, contract.Price.Currency).Value, PaymentMethod.BankCard, "Оплата обучения",
                null, registrar?.Id, registeredAtUtc)
            .Value;

    private static PaymentReportFilter Filter(
        User registrar, DateOnly? from = null, DateOnly? to = null, PaymentStatus? status = null, string? search = null) =>
        new(from, to, null, status, registrar.Id, null, search);

    private async Task ChangeAsync(Guid paymentId, Guid contractId, Action<Payment, Contract> change)
    {
        await using var context = database.CreateContext();
        var payment = await new PaymentRepository(context).GetByIdAsync(paymentId, CancellationToken);
        var contract = await new ContractRepository(context).GetByIdAsync(contractId, CancellationToken);
        change(payment!, contract!);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_PaymentConfirmedThenRefunded_StoresItsHistoryAndThePaidAmount()
    {
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var payment = Payment.Register(
                contract, Money.Create(15_000m, TrainingRecords.Rubles).Value, PaymentMethod.BankTransfer, "Первый взнос",
                "ПП-1024", null, _now)
            .Value;
        await SaveAsync(student, course, contract, payment);

        await ChangeAsync(payment.Id, contract.Id, (tracked, trackedContract) =>
            tracked.Confirm(trackedContract, _now.AddHours(1)).IsSuccess.Should().BeTrue());
        await ChangeAsync(payment.Id, contract.Id, (tracked, trackedContract) =>
            tracked.Refund(trackedContract, "Ошибочный платёж", _now.AddHours(2)).IsSuccess.Should().BeTrue());

        await using var reader = database.CreateContext();
        var loaded = await new PaymentRepository(reader).GetByIdAsync(payment.Id, CancellationToken);
        var loadedContract = await new ContractRepository(reader).GetByIdAsync(contract.Id, CancellationToken);
        loaded!.Amount.Should().Be(Money.Create(15_000m, TrainingRecords.Rubles).Value);
        loaded.Method.Should().Be(PaymentMethod.BankTransfer);
        loaded.ExternalReference.Should().Be("ПП-1024");
        loaded.Status.Should().Be(PaymentStatus.Refunded);
        loaded.ConfirmedAtUtc.Should().Be(_now.AddHours(1));
        loaded.RefundedAtUtc.Should().Be(_now.AddHours(2));
        loaded.Reason.Should().Be("Ошибочный платёж");
        loadedContract!.PaidAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public async Task Queries_ReadTheContractTheStudentAndTheReport()
    {
        var registrar = TrainingRecords.Person("Орлова", "Мария", null);
        var student = TrainingRecords.Person("Кузнецов");
        var rubleCourse = TrainingRecords.NewCourse();
        var tengeCourse = Course.Create(
                $"Категория B {Guid.NewGuid():N}", LicenseCategory.B, Money.Create(400_000m, _tenge).Value, 134, 56, null)
            .Value;
        var branch = TrainingRecords.NewBranch();
        var rubles = TrainingRecords.NewContract(TrainingRecords.Number(), student, rubleCourse, branch.Id);
        var tenge = TrainingRecords.NewContract(TrainingRecords.Number(), student, tengeCourse, branch.Id);
        var first = Register(rubles, 10_000m, registrar, _now);
        var second = Register(rubles, 5_000m, null, _now.AddDays(1));
        var third = Register(tenge, 100_000m, registrar, _now.AddDays(2));
        var fourth = Register(rubles, 2_500m, registrar, _now.AddDays(3));
        fourth.Cancel("Дубль", _now.AddDays(3).AddHours(1)).IsSuccess.Should().BeTrue();
        await SaveAsync(registrar, student, rubleCourse, tengeCourse, branch, rubles, tenge, first, second, third, fourth);
        await using var context = database.CreateContext();
        var queries = new PaymentQueries(context);
        var scope = DataScope.ForBranch(branch.Id);
        var firstDay = DateOnly.FromDateTime(_now);

        var ofContract = await queries.GetOfContractAsync(rubles.Id, CancellationToken);
        var ofStudent = await queries.GetOfStudentAsync(student.Id, CancellationToken);
        var report = await queries.GetReportAsync(Filter(registrar), scope, new PageRequest(1, 2), CancellationToken);
        var cancelled = await queries.GetReportAsync(
            Filter(registrar, status: PaymentStatus.Cancelled), DataScope.SchoolWide, new PageRequest(1, 20), CancellationToken);
        var days = await queries.GetReportAsync(
            Filter(registrar, firstDay.AddDays(1), firstDay.AddDays(2)), DataScope.SchoolWide, new PageRequest(1, 20),
            CancellationToken);
        var found = await queries.GetReportAsync(
            Filter(registrar, search: "t-" + rubles.Number[2..]), DataScope.SchoolWide, new PageRequest(1, 20), CancellationToken);
        var otherBranch = await queries.GetReportAsync(
            Filter(registrar), DataScope.ForBranch(Guid.CreateVersion7()), new PageRequest(1, 20), CancellationToken);

        ofContract!.StudentId.Should().Be(student.Id);
        ofContract.BranchId.Should().Be(branch.Id);
        ofContract.Payments.Select(payment => payment.Id).Should().Equal(first.Id, second.Id, fourth.Id);
        ofContract.Payments[0].RegisteredByName.Should().Be("Орлова Мария");
        ofContract.Payments[1].RegisteredByName.Should().BeNull();
        ofContract.Payments[0].ContractNumber.Should().Be(rubles.Number);
        ofContract.Payments[0].StudentName.Should().Be("Кузнецов Иван Петрович");
        ofStudent.Select(payment => payment.Id).Should().Equal(fourth.Id, third.Id, second.Id, first.Id);
        report.Page.TotalCount.Should().Be(3);
        report.Page.Items.Select(payment => payment.Id).Should().Equal(fourth.Id, third.Id);
        report.Totals.Should().Equal(new CurrencyTotal("KZT", 100_000m, 1), new CurrencyTotal("RUB", 12_500m, 2));
        cancelled.Page.Items.Should().ContainSingle().Which.Reason.Should().Be("Дубль");
        days.Page.Items.Select(payment => payment.Id).Should().Equal(third.Id);
        found.Page.Items.Select(payment => payment.Id).Should().Equal(fourth.Id, first.Id);
        otherBranch.Page.Items.Should().BeEmpty();
        otherBranch.Totals.Should().BeEmpty();
        (await queries.GetOfContractAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}
