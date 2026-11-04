using AutoElite.Application.Contracts.DrawUpContract;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Contracts;

public class DrawUpContractCommandHandlerTests
{
    private readonly ContractFixture _fixture = new();

    private Task<Result<Guid>> Handle(DrawUpContractCommand command, FakeCurrentUser? caller = null)
    {
        var user = caller ?? TestData.Administrator();

        return new DrawUpContractCommandHandler(
                _fixture.CreateContractStudents(user), _fixture.CreateContractFactory(), _fixture.Branches, user)
            .Handle(command, TestContext.Current.CancellationToken);
    }

    private DrawUpContractCommand Command(Guid studentId) =>
        new(studentId, _fixture.Course.Id, Transmission.Automatic, ContractFixture.Start, ContractFixture.End);

    [Fact]
    public async Task Handle_Student_DrawsUpTheContractAtTheCoursePriceWithTheNextNumber()
    {
        var branch = _fixture.SeedBranch();
        var student = _fixture.SeedStudent(branchId: branch.Id);

        var result = await Handle(Command(student.Id));

        var contract = _fixture.Contracts.Items.Should().ContainSingle().Which;
        contract.Id.Should().Be(result.Value);
        contract.Number.Should().Be("2026-000001");
        contract.StudentId.Should().Be(student.Id);
        contract.CourseId.Should().Be(_fixture.Course.Id);
        contract.Price.Should().Be(_fixture.Course.Price);
        contract.Transmission.Should().Be(Transmission.Automatic);
        contract.BranchId.Should().Be(branch.Id);
        contract.StartDate.Should().Be(ContractFixture.Start);
    }

    [Fact]
    public async Task Handle_AgreedPriceAndNumber_UsesThemInTheCourseCurrency()
    {
        var student = _fixture.SeedStudent();

        await Handle(Command(student.Id) with { PriceAmount = 40000m, Number = " AE-17 " });

        var contract = _fixture.Contracts.Items.Single();
        contract.Price.Should().Be(Money.Create(40000m, _fixture.Course.Price.Currency).Value);
        contract.Number.Should().Be("AE-17");
    }

    [Fact]
    public async Task Handle_NumberOfAnotherContract_ReturnsNumberTaken()
    {
        var student = _fixture.SeedStudent();
        await Handle(Command(student.Id) with { Number = "AE-17" });

        var result = await Handle(Command(student.Id) with { Number = "ae-17" });

        result.Error.Should().Be(ApplicationErrors.Contract.NumberTaken);
        _fixture.Contracts.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_UserWithoutTheStudentRole_ReturnsNotAStudent()
    {
        var manager = _fixture.SeedStudent(role: UserRole.Manager);

        var result = await Handle(Command(manager.Id));

        result.Error.Should().Be(ApplicationErrors.Contract.NotAStudent);
    }

    [Fact]
    public async Task Handle_StudentOfAnotherBranch_ReturnsUserNotFoundToTheManager()
    {
        var student = _fixture.SeedStudent(branchId: Guid.CreateVersion7());

        var result = await Handle(Command(student.Id), TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Handle_UnknownCourse_ReturnsCourseNotFound()
    {
        var student = _fixture.SeedStudent();

        var result = await Handle(Command(student.Id) with { CourseId = Guid.CreateVersion7() });

        result.Error.Should().Be(ApplicationErrors.NotFound<Course>());
    }

    [Fact]
    public async Task Handle_InactiveCourse_ReturnsCourseInactive()
    {
        var student = _fixture.SeedStudent();
        _fixture.Course.Deactivate();

        var result = await Handle(Command(student.Id));

        result.Error.Should().Be(DomainErrors.Contract.CourseInactive);
    }

    [Fact]
    public async Task Handle_EndBeforeStart_ReturnsEndNotAfterStart()
    {
        var student = _fixture.SeedStudent();

        var result = await Handle(Command(student.Id) with { EndDate = ContractFixture.Start });

        result.Error.Should().Be(DomainErrors.Contract.EndNotAfterStart);
        _fixture.Contracts.Items.Should().BeEmpty();
    }
}
