namespace AutoElite.Domain.Tests.Entities;

public class DrivingRouteTests
{
    private static readonly Guid _authorId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _groundId = new("55555555-5555-5555-5555-555555555555");

    private static DrivingRoute CreateRoute() =>
        DrivingRoute.Create("Exam route 1", "Lenina - Malysheva - roundabout", _authorId, null).Value;

    [Fact]
    public void Create_ValidData_ReturnsActiveRouteWithGivenValues()
    {
        var result = DrivingRoute.Create("  Exam route 1  ", "  Lenina - Malysheva  ", _authorId, _groundId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Exam route 1");
        result.Value.Description.Should().Be("Lenina - Malysheva");
        result.Value.AuthorId.Should().Be(_authorId);
        result.Value.TrainingGroundId.Should().Be(_groundId);
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = DrivingRoute.Create(name, "Lenina", _authorId, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.DrivingRoute.NameEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyDescription_ReturnsDescriptionEmptyError(string? description)
    {
        var result = DrivingRoute.Create("Exam route 1", description, _authorId, null);

        result.Error.Should().Be(DomainErrors.DrivingRoute.DescriptionEmpty);
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsDescriptionTooLongError()
    {
        var result = DrivingRoute.Create("Exam route 1", new string('a', 4001), _authorId, null);

        result.Error.Should().Be(DomainErrors.DrivingRoute.DescriptionTooLong);
    }

    [Fact]
    public void Create_EmptyAuthorId_ReturnsAuthorIdEmptyError()
    {
        var result = DrivingRoute.Create("Exam route 1", "Lenina", Guid.Empty, null);

        result.Error.Should().Be(DomainErrors.DrivingRoute.AuthorIdEmpty);
    }

    [Fact]
    public void Create_EmptyTrainingGroundId_ReturnsTrainingGroundIdEmptyError()
    {
        var result = DrivingRoute.Create("Exam route 1", "Lenina", _authorId, Guid.Empty);

        result.Error.Should().Be(DomainErrors.DrivingRoute.TrainingGroundIdEmpty);
    }

    [Fact]
    public void UpdateDetails_ActiveRoute_ReplacesDetailsAndKeepsAuthor()
    {
        var route = CreateRoute();

        var result = route.UpdateDetails("Exam route 2", "Bebelya - Kirova", _groundId);

        result.IsSuccess.Should().BeTrue();
        route.Name.Should().Be("Exam route 2");
        route.Description.Should().Be("Bebelya - Kirova");
        route.TrainingGroundId.Should().Be(_groundId);
        route.AuthorId.Should().Be(_authorId);
    }

    [Fact]
    public void UpdateDetails_InactiveRoute_ReturnsCannotModifyInactiveError()
    {
        var route = CreateRoute();
        route.Deactivate();

        var result = route.UpdateDetails("Exam route 2", "Bebelya", null);

        result.Error.Should().Be(DomainErrors.DrivingRoute.CannotModifyInactive);
        route.Name.Should().Be("Exam route 1");
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var route = CreateRoute();
        route.Deactivate();

        var result = route.Reactivate();

        result.IsSuccess.Should().BeTrue();
        route.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveRoute_ReturnsAlreadyInactiveError()
    {
        var route = CreateRoute();
        route.Deactivate();

        route.Deactivate().Error.Should().Be(DomainErrors.DrivingRoute.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveRoute_ReturnsAlreadyActiveError()
    {
        CreateRoute().Reactivate().Error.Should().Be(DomainErrors.DrivingRoute.AlreadyActive);
    }
}
