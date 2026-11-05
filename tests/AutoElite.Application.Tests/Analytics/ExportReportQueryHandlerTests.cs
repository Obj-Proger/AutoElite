using AutoElite.Application.Analytics;
using AutoElite.Application.Analytics.ExportReport;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Analytics;

public class ExportReportQueryHandlerTests
{
    private static readonly DateOnly _from = new(2026, 3, 1);
    private static readonly DateOnly _to = new(2026, 3, 31);

    private readonly FakeBranchRepository _branches = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeAnalyticsQueries _queries = new();
    private readonly FakeReportRenderer _renderer = new();

    /// <summary>A branch in Asia/Yekaterinburg, five hours ahead of UTC.</summary>
    private readonly Branch _branch = TestData.NewBranch();

    public ExportReportQueryHandlerTests()
    {
        _branches.Seed(_branch);
    }

    private FakeCurrentUser SeedManager(Language language)
    {
        var user = TestData.NewUser("manager@example.com", _branch.Id, language);
        _users.Seed(user);

        return new FakeCurrentUser { UserId = user.Id, BranchId = _branch.Id, Roles = new HashSet<UserRole> { UserRole.Manager } };
    }

    private Task<Result<ReportFile>> Export(FakeCurrentUser caller, ReportKind kind, ReportFormat format)
    {
        var localTimes = new LocalTimes(_branches, new FakeSchoolSettingsRepository());

        return new ExportReportQueryHandler(
                new ReportScopes(_branches, localTimes, caller), _queries, _renderer, _branches, _users, caller, TestData.Clock())
            .Handle(new ExportReportQuery(kind, _from, _to, format), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Export_RevenueAsPdf_LaysTheReportOutInTheCallersLanguage()
    {
        var result = await Export(SeedManager(Language.Russian), ReportKind.Revenue, ReportFormat.Pdf);

        var file = result.Value;
        file.FileName.Should().Be("revenue-2026-03-01-2026-03-31.pdf");
        file.ContentType.Should().Be("application/pdf");
        file.Content.Should().Equal(1, 2, 3);
        _renderer.LastFormat.Should().Be(ReportFormat.Pdf);
        var document = _renderer.LastDocument!;
        document.Title.Should().Be("Выручка по месяцам");
        document.Details.Should().Equal(
            "Период: 1 марта 2026 – 31 марта 2026", "Филиал: Central", "Сформирован: 1 марта 2026, 13:00");
        document.Tables.Single().Columns.Select(column => column.Kind).Should().Equal(
            ReportValueKind.Month, ReportValueKind.Text, ReportValueKind.Money, ReportValueKind.Money, ReportValueKind.Money);
    }

    [Fact]
    public async Task Export_LeadsAsExcelForAnEnglishSpeakingManager_UsesEnglishAndTheWorkbookType()
    {
        var result = await Export(SeedManager(Language.English), ReportKind.LeadStatistics, ReportFormat.Excel);

        result.Value.FileName.Should().Be("leads-2026-03-01-2026-03-31.xlsx");
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var document = _renderer.LastDocument!;
        document.Title.Should().Be("Leads and conversion");
        document.Tables.Select(table => table.Title).Should().Equal("Summary", "By source", "By status");
        document.Tables[0].Rows.Single().Should().Equal(0, 0, null);
    }

    [Fact]
    public async Task Export_ManagerAskingForAnotherBranch_ReturnsForbiddenAndRendersNothing()
    {
        var caller = SeedManager(Language.Russian);
        var localTimes = new LocalTimes(_branches, new FakeSchoolSettingsRepository());

        var result = await new ExportReportQueryHandler(
                new ReportScopes(_branches, localTimes, caller), _queries, _renderer, _branches, _users, caller, TestData.Clock())
            .Handle(
                new ExportReportQuery(ReportKind.Revenue, _from, _to, ReportFormat.Pdf, Guid.CreateVersion7()),
                TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _renderer.LastDocument.Should().BeNull();
    }

    [Fact]
    public void Validator_UnknownKind_ReportsKindNotDefined()
    {
        var result = new ExportReportQueryValidator().Validate(new ExportReportQuery((ReportKind)0, _from, _to, ReportFormat.Pdf));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Report.KindNotDefined.Code);
    }
}
