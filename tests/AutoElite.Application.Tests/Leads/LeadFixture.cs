using AutoElite.Application.Leads;
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

    public LeadManagers CreateLeadManagers() => new(Users, Roles);

    public LeadAccess CreateLeadAccess(FakeCurrentUser caller) => new(Leads, caller);

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
