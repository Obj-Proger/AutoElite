namespace AutoElite.Domain.Tests.Entities;

public class StudyGroupTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = new(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _instructorId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _otherInstructorId = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Course _course = CreateCourse("Category B");
    private static readonly Course _otherCourse = CreateCourse("Category A");

    private static Course CreateCourse(string name) =>
        Course.Create(name, LicenseCategory.B, Money.Create(45000m, _rub).Value, 134, 56, null).Value;

    private static Contract CreateContract(Course? course = null)
    {
        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;

        return Contract.Create("AE-1", student, course ?? _course, Money.Create(45000m, _rub).Value, null, _start, _end, _now).Value;
    }

    private static StudyGroup CreateGroup(int capacity = 20) =>
        StudyGroup.Create("B-2026-02", _course, _instructorId, _branchId, _start, _end, capacity).Value;

    [Fact]
    public void Create_ValidData_ReturnsActiveEmptyGroupWithGivenValues()
    {
        var result = StudyGroup.Create("  B-2026-02  ", _course, _instructorId, _branchId, _start, _end, 25);

        result.IsSuccess.Should().BeTrue();
        var group = result.Value;
        group.Name.Should().Be("B-2026-02");
        group.CourseId.Should().Be(_course.Id);
        group.Category.Should().Be(LicenseCategory.B);
        group.TheoryInstructorId.Should().Be(_instructorId);
        group.BranchId.Should().Be(_branchId);
        group.StartDate.Should().Be(_start);
        group.EndDate.Should().Be(_end);
        group.Capacity.Should().Be(25);
        group.IsActive.Should().BeTrue();
        group.Enrollments.Should().BeEmpty();
        group.CurrentMemberCount.Should().Be(0);
    }

    [Fact]
    public void Create_InactiveCourse_ReturnsCourseInactiveError()
    {
        var course = CreateCourse("Old course");
        course.Deactivate();

        var result = StudyGroup.Create("B-2026-02", course, _instructorId, null, _start, _end, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CourseInactive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = StudyGroup.Create(name, _course, _instructorId, null, _start, _end, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.NameEmpty);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = StudyGroup.Create(new string('a', 201), _course, _instructorId, null, _start, _end, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.NameTooLong);
    }

    [Fact]
    public void Create_EmptyInstructorId_ReturnsInstructorIdEmptyError()
    {
        var result = StudyGroup.Create("B-2026-02", _course, Guid.Empty, null, _start, _end, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.InstructorIdEmpty);
    }

    [Fact]
    public void Create_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = StudyGroup.Create("B-2026-02", _course, _instructorId, Guid.Empty, _start, _end, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.BranchIdEmpty);
    }

    [Fact]
    public void Create_EndNotAfterStart_ReturnsEndNotAfterStartError()
    {
        var result = StudyGroup.Create("B-2026-02", _course, _instructorId, null, _start, _start, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.EndNotAfterStart);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_CapacityAtLimit_ReturnsGroup(int capacity)
    {
        var result = StudyGroup.Create("B-2026-02", _course, _instructorId, null, _start, _end, capacity);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_CapacityOutOfRange_ReturnsCapacityOutOfRangeError(int capacity)
    {
        var result = StudyGroup.Create("B-2026-02", _course, _instructorId, null, _start, _end, capacity);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CapacityOutOfRange);
    }

    [Fact]
    public void UpdateDetails_ActiveGroup_ReplacesDetails()
    {
        var group = CreateGroup();
        var newEnd = new DateOnly(2026, 6, 30);

        var result = group.UpdateDetails("B-2026-02 evening", _otherInstructorId, _start, newEnd, 30);

        result.IsSuccess.Should().BeTrue();
        group.Name.Should().Be("B-2026-02 evening");
        group.TheoryInstructorId.Should().Be(_otherInstructorId);
        group.EndDate.Should().Be(newEnd);
        group.Capacity.Should().Be(30);
    }

    [Fact]
    public void UpdateDetails_CapacityBelowCurrentMembers_ReturnsCapacityBelowMemberCountError()
    {
        var group = CreateGroup();
        group.Enroll(CreateContract(), _now);
        group.Enroll(CreateContract(), _now);

        var result = group.UpdateDetails("B-2026-02", _instructorId, _start, _end, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CapacityBelowMemberCount);
        group.Capacity.Should().Be(20);
    }

    [Fact]
    public void UpdateDetails_CapacityEqualToCurrentMembers_IsAccepted()
    {
        var group = CreateGroup();
        group.Enroll(CreateContract(), _now);
        group.Enroll(CreateContract(), _now);

        var result = group.UpdateDetails("B-2026-02", _instructorId, _start, _end, 2);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_InactiveGroup_ReturnsCannotModifyInactiveError()
    {
        var group = CreateGroup();
        group.Deactivate();

        var result = group.UpdateDetails("B-2026-02", _instructorId, _start, _end, 30);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CannotModifyInactive);
    }

    [Fact]
    public void Enroll_ActiveContractForSameCourse_AddsCurrentEnrollment()
    {
        var group = CreateGroup();
        var contract = CreateContract();

        var result = group.Enroll(contract, _later);

        result.IsSuccess.Should().BeTrue();
        var enrollment = result.Value;
        enrollment.ContractId.Should().Be(contract.Id);
        enrollment.StudentId.Should().Be(contract.StudentId);
        enrollment.EnrolledAtUtc.Should().Be(_later);
        enrollment.IsCurrent.Should().BeTrue();
        group.Enrollments.Should().ContainSingle().Which.Should().BeSameAs(enrollment);
        group.CurrentMemberCount.Should().Be(1);
    }

    [Fact]
    public void Enroll_ActiveContract_RaisesStudentEnrolledEvent()
    {
        var group = CreateGroup();
        var contract = CreateContract();

        group.Enroll(contract, _later);

        group.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new StudentEnrolledDomainEvent(group.Id, contract.Id, contract.StudentId, _later));
    }

    [Fact]
    public void Enroll_InactiveGroup_ReturnsCannotModifyInactiveError()
    {
        var group = CreateGroup();
        group.Deactivate();

        var result = group.Enroll(CreateContract(), _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CannotModifyInactive);
    }

    [Fact]
    public void Enroll_TerminatedContract_ReturnsContractNotActiveError()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        contract.Terminate("Moved away", _start, _now);

        var result = group.Enroll(contract, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.ContractNotActive);
        group.Enrollments.Should().BeEmpty();
    }

    [Fact]
    public void Enroll_ContractForAnotherCourse_ReturnsCourseMismatchError()
    {
        var group = CreateGroup();

        var result = group.Enroll(CreateContract(_otherCourse), _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.CourseMismatch);
    }

    [Fact]
    public void Enroll_ContractAlreadyEnrolled_ReturnsAlreadyEnrolledError()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        group.Enroll(contract, _now);

        var result = group.Enroll(contract, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.AlreadyEnrolled);
        group.CurrentMemberCount.Should().Be(1);
    }

    [Fact]
    public void Enroll_FullGroup_ReturnsGroupFullError()
    {
        var group = CreateGroup(capacity: 1);
        group.Enroll(CreateContract(), _now);

        var result = group.Enroll(CreateContract(), _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.GroupFull);
    }

    [Fact]
    public void Enroll_AfterAMemberWithdrew_UsesTheFreedPlace()
    {
        var group = CreateGroup(capacity: 1);
        var first = CreateContract();
        group.Enroll(first, _now);
        group.Withdraw(first.Id, _now);

        var result = group.Enroll(CreateContract(), _later);

        result.IsSuccess.Should().BeTrue();
        group.CurrentMemberCount.Should().Be(1);
    }

    [Fact]
    public void Enroll_WithdrawnContractAgain_CreatesNewEnrollmentAndKeepsHistory()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        group.Enroll(contract, _now);
        group.Withdraw(contract.Id, _now);

        var result = group.Enroll(contract, _later);

        result.IsSuccess.Should().BeTrue();
        group.Enrollments.Should().HaveCount(2);
        group.Enrollments[0].IsCurrent.Should().BeFalse();
        group.Enrollments[1].IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_CurrentMember_EndsEnrollmentAndRaisesStudentWithdrawnEvent()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        group.Enroll(contract, _now);
        group.ClearDomainEvents();

        var result = group.Withdraw(contract.Id, _later);

        result.IsSuccess.Should().BeTrue();
        group.Enrollments[0].WithdrawnAtUtc.Should().Be(_later);
        group.CurrentMemberCount.Should().Be(0);
        group.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new StudentWithdrawnDomainEvent(group.Id, contract.Id, contract.StudentId, _later));
    }

    [Fact]
    public void Withdraw_InactiveGroup_IsAllowed()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        group.Enroll(contract, _now);
        group.Deactivate();

        var result = group.Withdraw(contract.Id, _later);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_NotAMember_ReturnsNotEnrolledError()
    {
        var group = CreateGroup();

        var result = group.Withdraw(CreateContract().Id, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.NotEnrolled);
    }

    [Fact]
    public void Withdraw_AlreadyWithdrawn_ReturnsNotEnrolledError()
    {
        var group = CreateGroup();
        var contract = CreateContract();
        group.Enroll(contract, _now);
        group.Withdraw(contract.Id, _now);

        var result = group.Withdraw(contract.Id, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.NotEnrolled);
    }

    [Fact]
    public void Deactivate_ActiveGroup_BecomesInactive()
    {
        var group = CreateGroup();

        var result = group.Deactivate();

        result.IsSuccess.Should().BeTrue();
        group.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_InactiveGroup_ReturnsAlreadyInactiveError()
    {
        var group = CreateGroup();
        group.Deactivate();

        var result = group.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_InactiveGroup_BecomesActive()
    {
        var group = CreateGroup();
        group.Deactivate();

        var result = group.Reactivate();

        result.IsSuccess.Should().BeTrue();
        group.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_ActiveGroup_ReturnsAlreadyActiveError()
    {
        var group = CreateGroup();

        var result = group.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.StudyGroup.AlreadyActive);
    }
}
