using AutoElite.Application.Branches;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class BranchPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly Address _address = Address.Create("Екатеринбург", "ул. Ленина, 1").Value;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Branch NewBranch(string? name = null, PhoneNumber? phone = null) =>
        Branch.Create(name ?? $"Branch {Guid.NewGuid():N}", _address, phone, "Asia/Yekaterinburg").Value;

    private async Task<Branch> SaveNewAsync(Branch branch, ICurrentUser? user = null, TimeProvider? clock = null)
    {
        await using var context = database.CreateContext(user, clock);
        new BranchRepository(context).Add(branch);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);

        return branch;
    }

    private async Task<Branch?> LoadAsync(Guid branchId)
    {
        await using var context = database.CreateContext();

        return await new BranchRepository(context).GetByIdAsync(branchId, CancellationToken);
    }

    [Fact]
    public async Task Save_NewBranch_StoresItsValuesAndWhoCreatedIt()
    {
        var user = TestCurrentUser.Administrator();
        var phone = PhoneNumber.Create("+73432000000").Value;
        var branch = await SaveNewAsync(NewBranch(phone: phone), user, FixedClock.Default);

        var loaded = await LoadAsync(branch.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(branch.Name);
        loaded.Address.Should().Be(_address);
        loaded.Phone.Should().Be(phone);
        loaded.TimeZoneId.Should().Be("Asia/Yekaterinburg");
        loaded.IsActive.Should().BeTrue();
        loaded.CreatedAtUtc.Should().Be(FixedClock.Default.UtcNow);
        loaded.CreatedByUserId.Should().Be(user.UserId);
        loaded.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Save_ChangedBranch_StoresTheChangeAndWhoMadeIt()
    {
        var branch = await SaveNewAsync(NewBranch());
        var editor = TestCurrentUser.Administrator();
        var later = new FixedClock(FixedClock.Default.GetUtcNow().AddDays(1));

        await using (var context = database.CreateContext(editor, later))
        {
            var tracked = await new BranchRepository(context).GetByIdAsync(branch.Id, CancellationToken);
            tracked!.UpdateDetails("Renamed " + branch.Name, _address, null, "Europe/Moscow").IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        var loaded = await LoadAsync(branch.Id);

        loaded!.Name.Should().Be("Renamed " + branch.Name);
        loaded.TimeZoneId.Should().Be("Europe/Moscow");
        loaded.ModifiedAtUtc.Should().Be(later.UtcNow);
        loaded.ModifiedByUserId.Should().Be(editor.UserId);
        loaded.CreatedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task NameExists_IgnoresCaseAndTheBranchLeftOut()
    {
        var branch = await SaveNewAsync(NewBranch());
        await using var context = database.CreateContext();
        var repository = new BranchRepository(context);

        (await repository.NameExistsAsync(branch.Name.ToUpperInvariant(), cancellationToken: CancellationToken)).Should().BeTrue();
        (await repository.NameExistsAsync(branch.Name, branch.Id, CancellationToken)).Should().BeFalse();
        (await repository.NameExistsAsync("No such " + branch.Name, cancellationToken: CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Save_NameTakenInAnotherCase_ThrowsConcurrencyConflict()
    {
        var branch = await SaveNewAsync(NewBranch());

        var save = () => SaveNewAsync(NewBranch(branch.Name.ToUpperInvariant()));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Save_ChangeOfAStaleCopy_ThrowsConcurrencyConflict()
    {
        var branch = await SaveNewAsync(NewBranch());
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstCopy = await new BranchRepository(first).GetByIdAsync(branch.Id, CancellationToken);
        var secondCopy = await new BranchRepository(second).GetByIdAsync(branch.Id, CancellationToken);

        firstCopy!.Deactivate().IsSuccess.Should().BeTrue();
        await new UnitOfWork(first).SaveChangesAsync(CancellationToken);
        secondCopy!.UpdateDetails("Stale " + branch.Name, _address, null, "Europe/Moscow").IsSuccess.Should().BeTrue();

        var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task GetList_FiltersByActivityAndSortsByName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var active = await SaveNewAsync(NewBranch("b " + suffix));
        var first = await SaveNewAsync(NewBranch("A " + suffix));
        var inactive = NewBranch("c " + suffix);
        inactive.Deactivate().IsSuccess.Should().BeTrue();
        await SaveNewAsync(inactive);
        await using var context = database.CreateContext();
        var queries = new BranchQueries(context);

        var all = await queries.GetListAsync(null, CancellationToken);
        var activeOnly = await queries.GetListAsync(true, CancellationToken);

        all.Where(item => item.Name.EndsWith(suffix, StringComparison.Ordinal)).Select(item => item.Id)
            .Should().Equal(first.Id, active.Id, inactive.Id);
        activeOnly.Should().OnlyContain(item => item.IsActive).And.Contain(item => item.Id == active.Id);
    }

    [Fact]
    public async Task Get_ReadsTheBranch()
    {
        var phone = PhoneNumber.Create("+73432000001").Value;
        var branch = await SaveNewAsync(NewBranch(phone: phone));
        var withoutPhone = await SaveNewAsync(NewBranch());
        await using var context = database.CreateContext();
        var queries = new BranchQueries(context);

        var item = await queries.GetAsync(branch.Id, CancellationToken);
        var itemWithoutPhone = await queries.GetAsync(withoutPhone.Id, CancellationToken);

        item.Should().Be(new BranchItem(
            branch.Id, branch.Name, _address.City, _address.Street, phone.Value, "Asia/Yekaterinburg", true));
        itemWithoutPhone!.Phone.Should().BeNull();
        (await queries.GetAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}
