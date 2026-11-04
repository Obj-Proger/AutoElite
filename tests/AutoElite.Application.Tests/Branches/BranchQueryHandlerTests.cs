using AutoElite.Application.Branches;
using AutoElite.Application.Branches.GetBranch;
using AutoElite.Application.Branches.GetBranches;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Branches;

public class BranchQueryHandlerTests
{
    private readonly FakeBranchQueries _queries = new();

    private BranchItem Seed(string name, bool isActive = true)
    {
        var branch = new BranchItem(Guid.CreateVersion7(), name, "Yekaterinburg", "Lenina 1", null, "Asia/Yekaterinburg", isActive);
        _queries.Seed(branch);

        return branch;
    }

    [Fact]
    public async Task GetBranches_ActiveFilter_ReturnsOnlyActiveBranchesByName()
    {
        var north = Seed("North");
        var central = Seed("Central");
        Seed("Closed", isActive: false);

        var result = await new GetBranchesQueryHandler(_queries)
            .Handle(new GetBranchesQuery(IsActive: true), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(central, north);
    }

    [Fact]
    public async Task GetBranch_ExistingBranch_ReturnsIt()
    {
        var branch = Seed("Central");

        var result = await new GetBranchQueryHandler(_queries)
            .Handle(new GetBranchQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value.Should().Be(branch);
    }

    [Fact]
    public async Task GetBranch_UnknownBranch_ReturnsBranchNotFound()
    {
        var result = await new GetBranchQueryHandler(_queries)
            .Handle(new GetBranchQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }
}
