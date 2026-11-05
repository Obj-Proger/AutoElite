using AutoElite.Application.Common.Abstractions;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Behaviors;

public class AuthorizationBehaviorTests
{
    private bool _nextCalled;

    private Task<TResponse> Next<TResponse>(TResponse response)
    {
        _nextCalled = true;

        return Task.FromResult(response);
    }

    private Task<Result> Handle<TRequest>(TRequest request, ICurrentUser currentUser)
        where TRequest : IRequest<Result> =>
        new AuthorizationBehavior<TRequest, Result>(currentUser)
            .Handle(request, () => Next(Result.Success()), CancellationToken.None);

    [Fact]
    public async Task Handle_SystemRequestFromTheSystem_CallsNext()
    {
        var result = await Handle(new SystemCommand(), FakeCurrentUser.System());

        result.IsSuccess.Should().BeTrue();
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SystemRequestFromAnAdministrator_ReturnsForbiddenAndSkipsNext()
    {
        var result = await Handle(new SystemCommand(), FakeCurrentUser.SignedIn(UserRole.Administrator));

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemRequestFromAnonymousCaller_ReturnsUnauthenticated()
    {
        var result = await Handle(new SystemCommand(), FakeCurrentUser.Anonymous());

        result.Error.Should().Be(ApplicationErrors.Auth.Unauthenticated);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnmarkedRequestFromTheSystem_ReturnsUnauthenticated()
    {
        var result = await Handle(new TestCommand("name"), FakeCurrentUser.System());

        result.Error.Should().Be(ApplicationErrors.Auth.Unauthenticated);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnmarkedRequestFromAnonymousCaller_ReturnsUnauthenticatedAndSkipsNext()
    {
        var result = await Handle(new TestCommand("name"), FakeCurrentUser.Anonymous());

        result.Error.Should().Be(ApplicationErrors.Auth.Unauthenticated);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnmarkedRequestFromSignedInUser_CallsNext()
    {
        var result = await Handle(new TestCommand("name"), FakeCurrentUser.SignedIn(UserRole.Student));

        result.IsSuccess.Should().BeTrue();
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AnonymousRequestFromAnonymousCaller_CallsNext()
    {
        var result = await Handle(new PublicCommand(), FakeCurrentUser.Anonymous());

        result.IsSuccess.Should().BeTrue();
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RoleRequestFromAnonymousCaller_ReturnsUnauthenticated()
    {
        var result = await Handle(new ManagerCommand(), FakeCurrentUser.Anonymous());

        result.Error.Should().Be(ApplicationErrors.Auth.Unauthenticated);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RoleRequestFromUserWithoutTheRoles_ReturnsForbiddenAndSkipsNext()
    {
        var result = await Handle(new ManagerCommand(), FakeCurrentUser.SignedIn(UserRole.Student, UserRole.DrivingInstructor));

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _nextCalled.Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Administrator)]
    public async Task Handle_RoleRequestFromUserWithOneOfTheRoles_CallsNext(UserRole role)
    {
        var result = await Handle(new ManagerCommand(), FakeCurrentUser.SignedIn(UserRole.Student, role));

        result.IsSuccess.Should().BeTrue();
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_QueryFromUserWithoutTheRole_ReturnsTypedFailure()
    {
        var behavior = new AuthorizationBehavior<AdministratorQuery, Result<int>>(FakeCurrentUser.SignedIn(UserRole.Manager));

        var result = await behavior.Handle(new AdministratorQuery(), () => Next(Result.Success(1)), CancellationToken.None);

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _nextCalled.Should().BeFalse();
    }
}
