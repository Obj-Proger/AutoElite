using AutoElite.Application.Analytics;
using AutoElite.Application.Analytics.GetContractSummary;
using AutoElite.Application.Analytics.GetLeadStatistics;
using AutoElite.Application.Analytics.GetRevenue;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Analytics;

public class AnalyticsHandlerTests
{
    private static readonly DateOnly _from = new(2026, 3, 1);
    private static readonly DateOnly _to = new(2026, 3, 31);

    private readonly FakeBranchRepository _branches = new();
    private readonly FakeSchoolSettingsRepository _settings = new();
    private readonly FakeAnalyticsQueries _queries = new();

    /// <summary>A branch in Asia/Yekaterinburg, five hours ahead of UTC.</summary>
    private readonly Branch _branch = TestData.NewBranch();

    public AnalyticsHandlerTests()
    {
        _branches.Seed(_branch);
    }

    private ReportScopes Scopes(FakeCurrentUser caller) => new(_branches, new LocalTimes(_branches, _settings), caller);

    private Task<Result<LeadStatistics>> Leads(FakeCurrentUser caller, Guid? branchId = null) =>
        new GetLeadStatisticsQueryHandler(Scopes(caller), _queries)
            .Handle(new GetLeadStatisticsQuery(_from, _to, branchId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Report_ManagerOfABranch_CoversTheirBranchInItsLocalDays()
    {
        var result = await Leads(TestData.Manager(_branch.Id));

        result.IsSuccess.Should().BeTrue();
        var scope = _queries.LastScope!;
        scope.BranchId.Should().Be(_branch.Id);
        scope.TimeZone.Id.Should().Be("Asia/Yekaterinburg");
        scope.Period.StartUtc.Should().Be(new DateTime(2026, 2, 28, 19, 0, 0, DateTimeKind.Utc));
        scope.Period.EndUtc.Should().Be(new DateTime(2026, 3, 31, 19, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Report_ManagerAskingForAnotherBranch_ReturnsForbidden()
    {
        var result = await Leads(TestData.Manager(_branch.Id), Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
    }

    [Fact]
    public async Task Report_AdministratorAskingForAnUnknownBranch_ReturnsBranchNotFound()
    {
        var result = await Leads(TestData.Administrator(), Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public async Task Report_AdministratorWithoutBranch_CoversTheSchoolInItsTimeZone()
    {
        await Leads(TestData.Administrator());

        var scope = _queries.LastScope!;
        scope.BranchId.Should().BeNull();
        scope.Period.StartUtc.Should().Be(new DateTime(2026, 2, 28, 21, 0, 0, DateTimeKind.Utc), "Moscow is three hours ahead");
    }

    [Fact]
    public async Task ContractSummary_PassesTheCourse()
    {
        var courseId = Guid.CreateVersion7();

        await new GetContractSummaryQueryHandler(Scopes(TestData.Administrator()), _queries)
            .Handle(new GetContractSummaryQuery(_from, _to, CourseId: courseId), TestContext.Current.CancellationToken);

        _queries.LastCourseId.Should().Be(courseId);
    }

    [Fact]
    public void Validator_LastDayBeforeTheFirst_ReportsPeriodInvalid()
    {
        var result = new GetRevenueQueryValidator().Validate(new GetRevenueQuery(_to, _from));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Report.PeriodInvalid.Code);
    }

    [Theory]
    [InlineData(1095, true)]
    [InlineData(1096, false)]
    public void Validator_Period_CoversAtMostThreeYears(int extraDays, bool valid)
    {
        var result = new GetRevenueQueryValidator().Validate(new GetRevenueQuery(_from, _from.AddDays(extraDays)));

        result.IsValid.Should().Be(valid);
        if (!valid)
            result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Report.PeriodTooLong.Code);
    }

    [Fact]
    public void LeadStatistics_ConversionRate_IsTheShareOfConvertedLeads()
    {
        var statistics = new LeadStatistics(
            8, 2, [new LeadSourceStatistics(LeadSource.Website, 3, 1)], [new LeadStatusCount(LeadStatus.New, 6)]);

        statistics.ConversionRate.Should().Be(0.25m);
        statistics.BySource.Single().ConversionRate.Should().Be(0.3333m);
        new LeadStatistics(0, 0, [], []).ConversionRate.Should().BeNull();
    }

    [Fact]
    public void ContractSummary_CompletionRate_CountsOnlyClosedContracts()
    {
        var summary = new ContractSummary(Active: 5, Completed: 3, Terminated: 1, AverageQualityScore: 4.1m);

        summary.Total.Should().Be(9);
        summary.CompletionRate.Should().Be(0.75m);
        new ContractSummary(2, 0, 0, null).CompletionRate.Should().BeNull();
    }

    [Fact]
    public void MonthlyRevenue_Net_IsReceivedLessRefunded()
    {
        new MonthlyRevenue(2026, 3, "RUB", 90000m, 15000m).Net.Should().Be(75000m);
    }
}
