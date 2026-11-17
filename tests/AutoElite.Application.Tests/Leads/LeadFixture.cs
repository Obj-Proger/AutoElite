using AutoElite.Application.Leads;
using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Leads;

/// <summary>Leads, branches and staff shared by the lead command tests.</summary>
internal sealed class LeadFixture
{
    public LeadFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
    }

    public InMemoryRepository<Lead> Leads { get; } = new();

    public InMemoryRepository<Branch> Branches { get; } = new();

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public LeadManagers CreateLeadManagers() => new(new StaffMembers(Users, Roles));

    public LeadAccess CreateLeadAccess(FakeCurrentUser caller) => new(Leads, caller);

    public LeadCapacity CreateLeadCapacity(FakeCurrentUser caller) => new(Users, new FakeLeadQueries(Leads.Items), caller);

    /// <summary>Lets the manager work on no more leads at a time than the limit.</summary>
    public void SetLeadLimit(FakeCurrentUser manager, int limit) =>
        Users.Items.Single(user => user.Id == manager.UserId).SetLeadLimit(limit).IsSuccess.Should().BeTrue();

    /// <summary>Seeds a lead that the manager is working on.</summary>
    public Lead SeedLeadInProgress(FakeCurrentUser manager, Guid? branchId = null)
    {
        var lead = SeedLead(branchId);
        lead.AssignManager(manager.UserId!.Value, null, TestData.Now).IsSuccess.Should().BeTrue();
        lead.StartWork(null, TestData.Now).IsSuccess.Should().BeTrue();
        lead.ClearDomainEvents();

        return lead;
    }

    public Branch SeedBranch(string name = "Central")
    {
        var branch = TestData.NewBranch(name);
        Branches.Seed(branch);

        return branch;
    }

    /// <summary>Seeds an active manager and returns the manager as a caller.</summary>
    public FakeCurrentUser SeedManager(Guid? branchId, string email = "manager@example.com")
    {
        var user = TestData.NewUser(email, branchId);
        Users.Seed(user);
        Roles.Seed(user.Id, UserRole.Manager);

        return new FakeCurrentUser { UserId = user.Id, BranchId = branchId, Roles = new HashSet<UserRole> { UserRole.Manager } };
    }

    public Lead SeedLead(Guid? branchId = null)
    {
        var lead = Lead.Create("Anna", PhoneNumber.Create("+79991234567").Value, null, LeadSource.PhoneCall, null, null, branchId, null, TestData.Now)
            .Value;
        lead.ClearDomainEvents();
        Leads.Seed(lead);

        return lead;
    }
}
