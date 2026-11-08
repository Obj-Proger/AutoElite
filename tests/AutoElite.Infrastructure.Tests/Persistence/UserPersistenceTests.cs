using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class UserPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static User NewUser(Guid? branchId = null, string? middleName = "Иванович", PhoneNumber? phone = null) =>
        User.Create(
                FullName.Create("Иванов", "Иван", middleName).Value,
                Email.Create($"{Guid.NewGuid():N}@example.com").Value,
                phone,
                branchId,
                FixedClock.Default.UtcNow,
                Language.Russian)
            .Value;

    private static UserRoleAssignment RoleOf(User user, UserRole role) => UserRoleAssignment.Create(user.Id, role).Value;

    /// <summary>Saves new records in one transaction, in the order given.</summary>
    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task<Guid> SaveBranchAsync()
    {
        var branch = Branch.Create(
                $"Branch {Guid.NewGuid():N}", Address.Create("Екатеринбург", "ул. Ленина, 1").Value, null, "Asia/Yekaterinburg")
            .Value;
        await SaveAsync(branch);

        return branch.Id;
    }

    [Fact]
    public async Task Save_NewUser_StoresItsValuesAndTheRegistrationEventInTheOutbox()
    {
        var branchId = await SaveBranchAsync();
        var phone = PhoneNumber.Create("+79991234567").Value;
        var user = NewUser(branchId, phone: phone);
        var registered = (UserRegisteredDomainEvent)user.DomainEvents.Single();

        await SaveAsync(user);

        user.DomainEvents.Should().BeEmpty();
        await using var context = database.CreateContext();
        var loaded = await new UserRepository(context).GetByIdAsync(user.Id, CancellationToken);
        loaded!.FullName.Should().Be(user.FullName);
        loaded.Email.Should().Be(user.Email);
        loaded.Phone.Should().Be(phone);
        loaded.BranchId.Should().Be(branchId);
        loaded.PreferredLanguage.Should().Be(Language.Russian);
        loaded.IsActive.Should().BeTrue();

        var messages = await context.Set<OutboxMessage>()
            .Where(stored => stored.Type == nameof(UserRegisteredDomainEvent))
            .ToListAsync(CancellationToken);
        var message = messages.Single(stored => OutboxSerializer.Deserialize(stored.Type, stored.Payload).Equals(registered));
        message.OccurredOnUtc.Should().Be(registered.OccurredOnUtc);
        message.ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Save_UserWithoutMiddleNameOrPhone_StoresThemAsMissing()
    {
        var user = NewUser(middleName: null);
        await SaveAsync(user);

        await using var context = database.CreateContext();
        var loaded = await new UserRepository(context).GetByIdAsync(user.Id, CancellationToken);

        loaded!.FullName.MiddleName.Should().BeNull();
        loaded.Phone.Should().BeNull();
        loaded.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task Save_EmailTaken_ThrowsConcurrencyConflict()
    {
        var user = NewUser();
        await SaveAsync(user);
        var other = User.Create(FullName.Create("Петров", "Пётр").Value, user.Email, null, null, FixedClock.Default.UtcNow).Value;

        var save = () => SaveAsync(other);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Lookups_FindUsersByEmailAndByIdentifiers()
    {
        var first = NewUser();
        var second = NewUser();
        await SaveAsync(first, second);
        await using var context = database.CreateContext();
        var users = new UserRepository(context);

        (await users.EmailExistsAsync(first.Email, CancellationToken)).Should().BeTrue();
        (await users.EmailExistsAsync(Email.Create("nobody@example.com").Value, CancellationToken)).Should().BeFalse();
        (await users.GetByEmailAsync(second.Email, CancellationToken))!.Id.Should().Be(second.Id);
        (await users.GetByIdsAsync([first.Id, second.Id, Guid.CreateVersion7()], CancellationToken))
            .Select(user => user.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
        (await users.GetByIdsAsync([], CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task Roles_AreReadAndRemoved()
    {
        var user = NewUser();
        await SaveAsync(user, RoleOf(user, UserRole.Manager), RoleOf(user, UserRole.Student));

        await using (var context = database.CreateContext())
        {
            var roles = new UserRoleRepository(context);
            (await roles.GetRolesAsync(user.Id, CancellationToken))
                .Should().BeEquivalentTo(new[] { UserRole.Manager, UserRole.Student });
            (await roles.GetAsync(user.Id, UserRole.Administrator, CancellationToken)).Should().BeNull();

            roles.Remove((await roles.GetAsync(user.Id, UserRole.Student, CancellationToken))!);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        (await new UserRoleRepository(reader).GetRolesAsync(user.Id, CancellationToken)).Should().Equal(UserRole.Manager);
    }

    [Fact]
    public async Task Save_RoleHeldAlready_ThrowsConcurrencyConflict()
    {
        var user = NewUser();
        await SaveAsync(user, RoleOf(user, UserRole.Manager));

        var save = () => SaveAsync(RoleOf(user, UserRole.Manager));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task CountActiveAdministrators_LeavesOutDeactivatedUsers()
    {
        var active = NewUser();
        var deactivated = NewUser();
        deactivated.Deactivate(FixedClock.Default.UtcNow).IsSuccess.Should().BeTrue();
        await using var before = database.CreateContext();
        var countBefore = await new UserRoleRepository(before).CountActiveAdministratorsAsync(CancellationToken);
        await before.Database.RollbackTransactionAsync(CancellationToken);

        await SaveAsync(
            active, deactivated, RoleOf(active, UserRole.Administrator), RoleOf(deactivated, UserRole.Administrator));

        await using var context = database.CreateContext();
        (await new UserRoleRepository(context).CountActiveAdministratorsAsync(CancellationToken))
            .Should().Be(countBefore + 1);
    }

    [Fact]
    public async Task CountActiveAdministrators_LocksUntilTheRequestIsSaved()
    {
        var first = NewUser();
        var second = NewUser();
        await SaveAsync(first, second, RoleOf(first, UserRole.Administrator), RoleOf(second, UserRole.Administrator));
        await using var revoking = database.CreateContext();
        var revokingRoles = new UserRoleRepository(revoking);
        var count = await revokingRoles.CountActiveAdministratorsAsync(CancellationToken);

        await using var concurrent = database.CreateContext();
        var concurrentCount = new UserRoleRepository(concurrent).CountActiveAdministratorsAsync(CancellationToken);
        var finishedFirst = await Task.WhenAny(concurrentCount, Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken));

        finishedFirst.Should().NotBeSameAs(concurrentCount);
        revokingRoles.Remove((await revokingRoles.GetAsync(first.Id, UserRole.Administrator, CancellationToken))!);
        await new UnitOfWork(revoking).SaveChangesAsync(CancellationToken);
        (await concurrentCount.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken)).Should().Be(count - 1);
    }

    [Fact]
    public async Task GetActiveHolders_ReturnsHoldersWhoseScopeIncludesTheBranch()
    {
        var branchId = await SaveBranchAsync();
        var otherBranchId = await SaveBranchAsync();
        var schoolWide = NewUser();
        var ofBranch = NewUser(branchId);
        var ofOtherBranch = NewUser(otherBranchId);
        var deactivated = NewUser(branchId);
        deactivated.Deactivate(FixedClock.Default.UtcNow).IsSuccess.Should().BeTrue();
        var withAnotherRole = NewUser(branchId);
        await SaveAsync(
            schoolWide, ofBranch, ofOtherBranch, deactivated, withAnotherRole,
            RoleOf(schoolWide, UserRole.DrivingInstructor), RoleOf(ofBranch, UserRole.DrivingInstructor),
            RoleOf(ofOtherBranch, UserRole.DrivingInstructor), RoleOf(deactivated, UserRole.DrivingInstructor),
            RoleOf(withAnotherRole, UserRole.Manager));
        Guid[] mine = [schoolWide.Id, ofBranch.Id, ofOtherBranch.Id, deactivated.Id, withAnotherRole.Id];
        await using var context = database.CreateContext();
        var roles = new UserRoleRepository(context);

        var forBranch = await roles.GetActiveHoldersAsync(UserRole.DrivingInstructor, branchId, CancellationToken);
        var forSchool = await roles.GetActiveHoldersAsync(UserRole.DrivingInstructor, null, CancellationToken);

        forBranch.Select(user => user.Id).Where(id => mine.Contains(id))
            .Should().BeEquivalentTo(new[] { schoolWide.Id, ofBranch.Id });
        forSchool.Select(user => user.Id).Where(id => mine.Contains(id))
            .Should().BeEquivalentTo(new[] { schoolWide.Id, ofBranch.Id, ofOtherBranch.Id });
    }
}
