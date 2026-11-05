using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Identity;
using AutoElite.Infrastructure.Persistence.Queries;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class UserQueriesTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly PageRequest _firstPage = new(1, PageRequest.MaxPageSize);

    // Every test searches by a marker of its own, so the users of other tests in the shared database stay out.
    private readonly string _marker = Guid.NewGuid().ToString("N")[..12];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private User NewUser(string lastName, Guid? branchId = null, string? email = null) =>
        User.Create(
                FullName.Create(lastName, "Имя" + _marker, "Отчество").Value,
                Email.Create(email ?? $"{Guid.NewGuid():N}@example.com").Value,
                PhoneNumber.Create("+79990000000").Value,
                branchId,
                FixedClock.Default.UtcNow)
            .Value;

    private static UserRoleAssignment RoleOf(User user, UserRole role) => UserRoleAssignment.Create(user.Id, role).Value;

    private static UserAccount AccountOf(User user, bool activated) =>
        new() { Id = user.Id, UserName = user.Email.Value, Email = user.Email.Value, PasswordHash = activated ? "hash" : null };

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static Branch NewBranch() =>
        Branch.Create($"Branch {Guid.NewGuid():N}", Address.Create("Екатеринбург", "ул. Ленина, 1").Value, null, "Asia/Yekaterinburg")
            .Value;

    private async Task<PagedList<UserListItem>> ListAsync(
        UserListFilter filter, DataScope? scope = null, PageRequest? page = null)
    {
        await using var context = database.CreateContext();

        return await new UserQueries(context).GetListAsync(filter, scope ?? DataScope.SchoolWide, page ?? _firstPage, CancellationToken);
    }

    private UserListFilter Search(UserRole? role = null, Guid? branchId = null, bool? isActive = null) =>
        new(_marker.ToUpperInvariant(), role, branchId, isActive);

    [Fact]
    public async Task GetDetails_ReadsTheUserWithBranchRolesAndAccountState()
    {
        var branch = NewBranch();
        var user = NewUser("Иванов", branch.Id);
        await SaveAsync(branch, user, RoleOf(user, UserRole.Manager), RoleOf(user, UserRole.Student), AccountOf(user, activated: true));
        await using var context = database.CreateContext();

        var details = await new UserQueries(context).GetDetailsAsync(user.Id, CancellationToken);

        details.Should().NotBeNull();
        details!.LastName.Should().Be("Иванов");
        details.FirstName.Should().Be("Имя" + _marker);
        details.MiddleName.Should().Be("Отчество");
        details.Email.Should().Be(user.Email.Value);
        details.Phone.Should().Be("+79990000000");
        details.BranchId.Should().Be(branch.Id);
        details.BranchName.Should().Be(branch.Name);
        details.PreferredLanguage.Should().Be(Language.Russian);
        details.IsActive.Should().BeTrue();
        details.HasAcceptedInvitation.Should().BeTrue();
        details.Roles.Should().BeEquivalentTo(new[] { UserRole.Manager, UserRole.Student });
    }

    [Fact]
    public async Task GetProfile_UserWithoutBranchRolesOrAccount_ReadsEmptyValues()
    {
        var user = NewUser("Петров");
        await SaveAsync(user);
        await using var context = database.CreateContext();
        var queries = new UserQueries(context);

        var profile = await queries.GetProfileAsync(user.Id, CancellationToken);

        profile!.BranchId.Should().BeNull();
        profile.BranchName.Should().BeNull();
        profile.Roles.Should().BeEmpty();
        (await queries.GetDetailsAsync(user.Id, CancellationToken))!.HasAcceptedInvitation.Should().BeFalse();
        (await queries.GetProfileAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetList_SortsByNameAndReadsThePage()
    {
        var first = NewUser("Алексеев");
        var second = NewUser("Борисов");
        var third = NewUser("Васильев");
        await SaveAsync(third, first, second, AccountOf(second, activated: true));

        var all = await ListAsync(Search());
        var secondPage = await ListAsync(Search(), page: new PageRequest(2, 2));

        all.Items.Select(item => item.Id).Should().Equal(first.Id, second.Id, third.Id);
        all.Items.Select(item => item.HasAcceptedInvitation).Should().Equal(false, true, false);
        secondPage.TotalCount.Should().Be(3);
        secondPage.Items.Select(item => item.Id).Should().Equal(third.Id);
    }

    [Fact]
    public async Task GetList_SearchFindsTheEmailAndTreatsWildcardsAsText()
    {
        var byEmail = NewUser("Почтов", email: $"mail-{Guid.NewGuid():N}@example.com");
        await SaveAsync(byEmail);

        var found = await ListAsync(new UserListFilter(byEmail.Email.Value[..20].ToUpperInvariant(), null, null, null));
        var wildcard = await ListAsync(new UserListFilter(_marker[..4] + "%" + _marker[6..], null, null, null));

        found.Items.Should().ContainSingle(item => item.Id == byEmail.Id);
        wildcard.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_FiltersByRoleBranchAndActivity()
    {
        var branch = NewBranch();
        var manager = NewUser("Менеджеров", branch.Id);
        var student = NewUser("Студентов");
        var deactivated = NewUser("Ушедший", branch.Id);
        deactivated.Deactivate(FixedClock.Default.UtcNow).IsSuccess.Should().BeTrue();
        await SaveAsync(branch, manager, student, deactivated, RoleOf(manager, UserRole.Manager), RoleOf(student, UserRole.Student));

        (await ListAsync(Search(role: UserRole.Manager))).Items.Select(item => item.Id).Should().Equal(manager.Id);
        (await ListAsync(Search(branchId: branch.Id))).Items.Select(item => item.Id).Should().Equal(manager.Id, deactivated.Id);
        (await ListAsync(Search(isActive: false))).Items.Select(item => item.Id).Should().Equal(deactivated.Id);
    }

    [Fact]
    public async Task GetList_BranchScope_ShowsTheBranchAndUsersWithoutOne()
    {
        var branch = NewBranch();
        var otherBranch = NewBranch();
        var ofBranch = NewUser("Свой", branch.Id);
        var withoutBranch = NewUser("Общий");
        var ofOtherBranch = NewUser("Чужой", otherBranch.Id);
        await SaveAsync(branch, otherBranch, ofBranch, withoutBranch, ofOtherBranch);

        var list = await ListAsync(Search(), DataScope.ForBranch(branch.Id));

        list.Items.Select(item => item.Id).Should().BeEquivalentTo(new[] { ofBranch.Id, withoutBranch.Id });
    }
}
