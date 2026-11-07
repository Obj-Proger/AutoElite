using AutoElite.Application.Analytics;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;

namespace AutoElite.Infrastructure.Tests.Persistence;

/// <summary>
/// Every test reports on a branch of its own, so that the records of the other tests of the class do not count. The
/// period is March and April 2026 in Yekaterinburg (UTC+5).
/// </summary>
public sealed class AnalyticsQueriesTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly TimeZoneInfo _yekaterinburg = TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg");

    /// <summary>The start of March 1, local time.</summary>
    private static readonly DateTime _start = new(2026, 2, 28, 19, 0, 0, DateTimeKind.Utc);

    /// <summary>The end of April 30, local time.</summary>
    private static readonly DateTime _end = new(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ReportScope Scope(Guid branchId) =>
        new(branchId, TimeInterval.Create(_start, _end).Value, _yekaterinburg);

    /// <summary>Saves records as created at the given time, which the audit stamps on them.</summary>
    private async Task SaveAsync(DateTime createdAtUtc, params object[] records)
    {
        await using var context = database.CreateContext(TestCurrentUser.System, new FixedClock(createdAtUtc));
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static AnalyticsQueries Queries(AutoEliteDbContext context, DateTime? utcNow = null) =>
        new(context, utcNow is { } at ? new FixedClock(at) : FixedClock.Default);

    private static User Staff(Branch branch, string lastName) =>
        User.Create(
                FullName.Create(lastName, "Иван", null).Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value, null,
                branch.Id, _now)
            .Value;

    private static Lead NewLead(Branch branch, LeadSource source) =>
        Lead.Create(
                "Пётр Сидоров", null, Email.Create($"{Guid.NewGuid():N}@example.com").Value, source, LicenseCategory.B, null,
                branch.Id, null, _now)
            .Value;

    [Fact]
    public async Task GetLeadStatistics_CountsTheLeadsOfThePeriodAndBranch()
    {
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        var converted = NewLead(branch, LeadSource.Website);
        converted.MarkConverted(contract.Id, null, _now).IsSuccess.Should().BeTrue();
        var open = NewLead(branch, LeadSource.Website);
        var lost = NewLead(branch, LeadSource.PhoneCall);
        lost.MarkLost("Дорого", null, _now).IsSuccess.Should().BeTrue();
        await SaveAsync(_now, branch, otherBranch, student, course, contract, converted, open, lost,
            NewLead(otherBranch, LeadSource.Website));
        await SaveAsync(_start.AddDays(-1), NewLead(branch, LeadSource.Website));
        await using var context = database.CreateContext();

        var statistics = await Queries(context).GetLeadStatisticsAsync(Scope(branch.Id), CancellationToken);

        statistics.Total.Should().Be(3);
        statistics.Converted.Should().Be(1);
        statistics.BySource.Select(source => source.Source).Should().Equal(Enum.GetValues<LeadSource>());
        statistics.BySource.Single(source => source.Source == LeadSource.Website)
            .Should().Be(new LeadSourceStatistics(LeadSource.Website, 2, 1));
        statistics.BySource.Single(source => source.Source == LeadSource.PhoneCall)
            .Should().Be(new LeadSourceStatistics(LeadSource.PhoneCall, 1, 0));
        statistics.BySource.Single(source => source.Source == LeadSource.Referral).Total.Should().Be(0);
        statistics.ByStatus.Should().Equal(
            new LeadStatusCount(LeadStatus.New, 1),
            new LeadStatusCount(LeadStatus.InProgress, 0),
            new LeadStatusCount(LeadStatus.Converted, 1),
            new LeadStatusCount(LeadStatus.Lost, 1));
    }

    [Fact]
    public async Task GetRevenue_SumsConfirmedAndRefundedMoneyByLocalMonth()
    {
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        var otherContract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, otherBranch.Id);

        Payment Paid(Contract paidContract, decimal amount, DateTime confirmedAtUtc)
        {
            var payment = Payment.Register(
                    paidContract, Money.Create(amount, TrainingRecords.Rubles).Value, PaymentMethod.Cash, "Оплата обучения",
                    null, null, _now)
                .Value;
            payment.Confirm(paidContract, confirmedAtUtc).IsSuccess.Should().BeTrue();

            return payment;
        }

        var march = Paid(contract, 10_000m, new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc));

        // 20:00 UTC on March 31 is already April 1 in Yekaterinburg.
        var aprilByLocalTime = Paid(contract, 5_000m, new DateTime(2026, 3, 31, 20, 0, 0, DateTimeKind.Utc));
        var refunded = Paid(contract, 3_000m, new DateTime(2026, 3, 5, 6, 0, 0, DateTimeKind.Utc));
        refunded.Refund(contract, "Отказ от обучения", new DateTime(2026, 4, 10, 6, 0, 0, DateTimeKind.Utc))
            .IsSuccess.Should().BeTrue();
        var otherBranchPayment = Paid(otherContract, 7_000m, new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc));
        var pending = Payment.Register(
                contract, Money.Create(1_000m, TrainingRecords.Rubles).Value, PaymentMethod.Cash, "Оплата обучения", null,
                null, _now)
            .Value;
        await SaveAsync(
            _now, branch, otherBranch, student, course, contract, otherContract, march, aprilByLocalTime, refunded,
            otherBranchPayment, pending);
        await using var context = database.CreateContext();

        var revenue = await Queries(context).GetRevenueAsync(Scope(branch.Id), CancellationToken);

        revenue.Should().Equal(
            new MonthlyRevenue(2026, 3, "RUB", 13_000m, 0m),
            new MonthlyRevenue(2026, 4, "RUB", 5_000m, 3_000m));
    }

    [Fact]
    public async Task GetContractSummary_CountsTheContractsOfThePeriodByStatus()
    {
        var branch = TrainingRecords.NewBranch();
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var otherCourse = TrainingRecords.NewCourse();
        var active = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        var terminated = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        terminated.Terminate("Переезд", new DateOnly(2026, 3, 20), _now).IsSuccess.Should().BeTrue();
        var ofOtherCourse = TrainingRecords.NewContract(TrainingRecords.Number(), student, otherCourse, branch.Id);
        await SaveAsync(_now, branch, student, course, otherCourse, active, terminated, ofOtherCourse);
        await SaveAsync(_start.AddDays(-1), TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id));
        await using var context = database.CreateContext();
        var queries = Queries(context);

        var all = await queries.GetContractSummaryAsync(Scope(branch.Id), null, CancellationToken);
        var ofCourse = await queries.GetContractSummaryAsync(Scope(branch.Id), course.Id, CancellationToken);

        all.Should().Be(new ContractSummary(2, 0, 1, null));
        all.CompletionRate.Should().Be(0m);
        ofCourse.Should().Be(new ContractSummary(1, 0, 1, null));
    }

    [Fact]
    public async Task GetInstructorWorkload_SumsTheLessonsOfTheBranchInstructors()
    {
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var instructor = Staff(branch, "Орлов");
        var idle = Staff(branch, "Бездельников");
        var outsider = Staff(otherBranch, "Чужов");
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course, branch.Id);
        var group = TrainingRecords.NewGroup(course, instructor, branch.Id);
        group.Enroll(contract, _now).IsSuccess.Should().BeTrue();
        var vehicle = Vehicle.Register(
                LicensePlate.Create("ВС" + string.Concat(Guid.NewGuid().ToString("N").Where(char.IsDigit).Take(8))).Value,
                "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, branch.Id, _now)
            .Value;

        static TimeInterval Hours(int day, int fromHour, int toHour) =>
            TimeInterval.Create(
                    new DateTime(2026, 3, day, fromHour, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, day, toHour, 0, 0, DateTimeKind.Utc))
                .Value;

        DrivingSlot Slot(User slotInstructor, int day) =>
            DrivingSlot.Offer(slotInstructor.Id, vehicle, null, null, Hours(day, 9, 11), _now).Value;

        var completed = Slot(instructor, 10);
        completed.Book(contract, _now).IsSuccess.Should().BeTrue();
        completed.Complete(contract, 1.5m, [], null, 10, new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc))
            .IsSuccess.Should().BeTrue();
        completed.Rate(5, null, 2, new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc)).IsSuccess.Should().BeTrue();
        var cancelled = Slot(instructor, 11);
        cancelled.Book(contract, _now).IsSuccess.Should().BeTrue();
        cancelled.CancelBooking(instructor.Id, "Ремонт автомобиля", TimeSpan.Zero, _now).IsSuccess.Should().BeTrue();
        var noShow = Slot(instructor, 12);
        noShow.Book(contract, _now).IsSuccess.Should().BeTrue();
        noShow.MarkNoShow(contract, null, new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc)).IsSuccess.Should().BeTrue();
        var outsiderSlot = Slot(outsider, 13);
        outsiderSlot.Book(contract, _now).IsSuccess.Should().BeTrue();
        outsiderSlot.Complete(contract, 2m, [], null, 10, new DateTime(2026, 3, 13, 11, 0, 0, DateTimeKind.Utc))
            .IsSuccess.Should().BeTrue();
        var held = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(14, 10, 12), _now).Value;
        held.MarkAttendance(group, contract, true, null, instructor.Id, new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc))
            .IsSuccess.Should().BeTrue();
        var notHeld = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(15, 10, 12), _now).Value;
        await SaveAsync(
            _now, branch, otherBranch, instructor, idle, outsider, student, course, contract, group, vehicle, completed, cancelled,
            noShow, outsiderSlot, held, notHeld);
        await using var context = database.CreateContext();

        var workload = await Queries(context).GetInstructorWorkloadAsync(Scope(branch.Id), CancellationToken);

        workload.Should().Equal(new InstructorWorkloadItem(instructor.Id, "Орлов Иван", 1, 1.5m, 1, 1, 5m, 1, 2m));
    }

    [Fact]
    public async Task GetStaffActivity_CountsAssignedCompletedAndOverdueTasks()
    {
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var manager = Staff(branch, "Белова");
        var outsider = Staff(otherBranch, "Чужова");
        var reportedAt = new DateTime(2026, 4, 15, 6, 0, 0, DateTimeKind.Utc);

        StaffTask NewTask(User assignee, DateTime dueAtUtc) =>
            StaffTask.Create("Перезвонить", null, assignee.Id, dueAtUtc, null, TaskRecurrence.None, null, _now).Value;

        var completedLate = NewTask(manager, new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc));
        completedLate.Complete(manager.Id, new DateTime(2026, 3, 11, 6, 0, 0, DateTimeKind.Utc)).IsSuccess.Should().BeTrue();
        var completedInTime = NewTask(manager, new DateTime(2026, 3, 20, 6, 0, 0, DateTimeKind.Utc));
        completedInTime.Complete(manager.Id, new DateTime(2026, 3, 19, 6, 0, 0, DateTimeKind.Utc)).IsSuccess.Should().BeTrue();
        var openOverdue = NewTask(manager, new DateTime(2026, 4, 1, 6, 0, 0, DateTimeKind.Utc));
        var openNotDue = NewTask(manager, new DateTime(2026, 4, 25, 6, 0, 0, DateTimeKind.Utc));
        await SaveAsync(_now, branch, otherBranch, manager, outsider);
        await SaveAsync(_now, completedLate, completedInTime, openOverdue, openNotDue, NewTask(outsider, reportedAt));
        await using var context = database.CreateContext();

        var activity = await Queries(context, reportedAt).GetStaffActivityAsync(Scope(branch.Id), CancellationToken);

        activity.Should().Equal(new StaffActivityItem(manager.Id, "Белова Иван", 4, 2, 2));
    }
}
