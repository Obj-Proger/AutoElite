namespace AutoElite.Domain.Tests.Entities;

public class LeadTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = new(2026, 1, 2, 9, 30, 0, DateTimeKind.Utc);
    private static readonly PhoneNumber _phone = PhoneNumber.Create("+79991234567").Value;
    private static readonly Email _email = Email.Create("anna@example.com").Value;
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _managerId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _otherManagerId = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _contractId = new("55555555-5555-5555-5555-555555555555");

    private static Lead CreateLead() =>
        Lead.Create("Anna", _phone, null, LeadSource.Website, LicenseCategory.B, null, null, null, _now).Value;

    private static Lead CreateLeadInProgress()
    {
        var lead = CreateLead();
        lead.StartWork(_managerId, _now);

        return lead;
    }

    private static Lead CreateLostLead()
    {
        var lead = CreateLead();
        lead.MarkLost("Chose another school", _managerId, _now);

        return lead;
    }

    private static Lead CreateConvertedLead()
    {
        var lead = CreateLead();
        lead.MarkConverted(_contractId, _managerId, _now);

        return lead;
    }

    [Fact]
    public void Create_ValidData_ReturnsNewLeadWithGivenValues()
    {
        var result = Lead.Create("  Anna  ", _phone, _email, LeadSource.Referral, LicenseCategory.B, "  Evening group  ", _branchId, _managerId, _now);

        result.IsSuccess.Should().BeTrue();
        var lead = result.Value;
        lead.ContactName.Should().Be("Anna");
        lead.Phone.Should().Be(_phone);
        lead.Email.Should().Be(_email);
        lead.Source.Should().Be(LeadSource.Referral);
        lead.DesiredCategory.Should().Be(LicenseCategory.B);
        lead.Comment.Should().Be("Evening group");
        lead.BranchId.Should().Be(_branchId);
        lead.ManagerId.Should().BeNull();
        lead.Status.Should().Be(LeadStatus.New);
        lead.IsOpen.Should().BeTrue();
        lead.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Create_ValidData_RecordsCreationInStatusHistory()
    {
        var lead = Lead.Create("Anna", _phone, null, LeadSource.Website, null, null, null, _managerId, _now).Value;

        var entry = lead.StatusHistory.Should().ContainSingle().Which;
        entry.FromStatus.Should().BeNull();
        entry.ToStatus.Should().Be(LeadStatus.New);
        entry.ChangedAtUtc.Should().Be(_now);
        entry.ChangedByUserId.Should().Be(_managerId);
        entry.Reason.Should().BeNull();
    }

    [Fact]
    public void Create_ValidData_RaisesLeadCreatedEvent()
    {
        var lead = Lead.Create("Anna", _phone, null, LeadSource.Website, null, null, _branchId, null, _now).Value;

        lead.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new LeadCreatedDomainEvent(lead.Id, _branchId, LeadSource.Website, _now));
    }

    [Fact]
    public void Create_OnlyEmail_ReturnsLead()
    {
        var result = Lead.Create("Anna", null, _email, LeadSource.Website, null, null, null, null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().BeNull();
    }

    [Fact]
    public void Create_NoPhoneAndNoEmail_ReturnsContactRequiredError()
    {
        var result = Lead.Create("Anna", null, null, LeadSource.Website, null, null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ContactRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyContactName_ReturnsContactNameEmptyError(string? contactName)
    {
        var result = Lead.Create(contactName, _phone, null, LeadSource.Website, null, null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ContactNameEmpty);
    }

    [Fact]
    public void Create_ContactNameTooLong_ReturnsContactNameTooLongError()
    {
        var result = Lead.Create(new string('a', 201), _phone, null, LeadSource.Website, null, null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ContactNameTooLong);
    }

    [Fact]
    public void Create_CommentTooLong_ReturnsCommentTooLongError()
    {
        var result = Lead.Create("Anna", _phone, null, LeadSource.Website, null, new string('a', 2001), null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.CommentTooLong);
    }

    [Fact]
    public void Create_BlankComment_StoresNoComment()
    {
        var result = Lead.Create("Anna", _phone, null, LeadSource.Website, null, "   ", null, null, _now);

        result.Value.Comment.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Create_UndefinedSource_ReturnsSourceNotDefinedError(int rawSource)
    {
        var result = Lead.Create("Anna", _phone, null, (LeadSource)rawSource, null, null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.SourceNotDefined);
    }

    [Fact]
    public void Create_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = Lead.Create("Anna", _phone, null, LeadSource.Website, (LicenseCategory)99, null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.CategoryNotDefined);
    }

    [Fact]
    public void Create_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = Lead.Create("Anna", _phone, null, LeadSource.Website, null, null, Guid.Empty, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.BranchIdEmpty);
    }

    [Fact]
    public void Create_EmptyCreatorId_ReturnsUserIdEmptyError()
    {
        var result = Lead.Create("Anna", _phone, null, LeadSource.Website, null, null, null, Guid.Empty, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.UserIdEmpty);
    }

    [Fact]
    public void UpdateContact_OpenLead_ReplacesContactDetails()
    {
        var lead = CreateLead();

        var result = lead.UpdateContact("Anna Petrova", null, _email, LicenseCategory.A, "Prefers weekends");

        result.IsSuccess.Should().BeTrue();
        lead.ContactName.Should().Be("Anna Petrova");
        lead.Phone.Should().BeNull();
        lead.Email.Should().Be(_email);
        lead.DesiredCategory.Should().Be(LicenseCategory.A);
        lead.Comment.Should().Be("Prefers weekends");
    }

    [Fact]
    public void UpdateContact_RemovingLastContact_ReturnsContactRequiredErrorAndKeepsDetails()
    {
        var lead = CreateLead();

        var result = lead.UpdateContact("Anna", null, null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ContactRequired);
        lead.Phone.Should().Be(_phone);
    }

    [Fact]
    public void UpdateContact_ClosedLead_ReturnsNotOpenError()
    {
        var lead = CreateConvertedLead();

        var result = lead.UpdateContact("Anna Petrova", _phone, null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
        lead.ContactName.Should().Be("Anna");
    }

    [Fact]
    public void AssignBranch_OpenLead_SetsBranch()
    {
        var lead = CreateLead();

        var result = lead.AssignBranch(_branchId);

        result.IsSuccess.Should().BeTrue();
        lead.BranchId.Should().Be(_branchId);
    }

    [Fact]
    public void AssignBranch_EmptyGuid_ReturnsBranchIdEmptyError()
    {
        var lead = CreateLead();

        var result = lead.AssignBranch(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.BranchIdEmpty);
    }

    [Fact]
    public void AssignBranch_LostLead_ReturnsNotOpenError()
    {
        var lead = CreateLostLead();

        var result = lead.AssignBranch(_branchId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
    }

    [Fact]
    public void AssignManager_OpenLead_SetsManagerAndRaisesLeadAssignedEvent()
    {
        var lead = CreateLead();
        lead.ClearDomainEvents();

        var result = lead.AssignManager(_managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.ManagerId.Should().Be(_managerId);
        lead.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new LeadAssignedDomainEvent(lead.Id, _managerId, _later));
    }

    [Fact]
    public void AssignManager_DifferentManager_ReplacesManager()
    {
        var lead = CreateLead();
        lead.AssignManager(_managerId, _now);

        var result = lead.AssignManager(_otherManagerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.ManagerId.Should().Be(_otherManagerId);
    }

    [Fact]
    public void AssignManager_SameManager_ReturnsManagerAlreadyAssignedError()
    {
        var lead = CreateLead();
        lead.AssignManager(_managerId, _now);

        var result = lead.AssignManager(_managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ManagerAlreadyAssigned);
    }

    [Fact]
    public void AssignManager_EmptyGuid_ReturnsManagerIdEmptyError()
    {
        var lead = CreateLead();

        var result = lead.AssignManager(Guid.Empty, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ManagerIdEmpty);
    }

    [Fact]
    public void AssignManager_ConvertedLead_ReturnsNotOpenError()
    {
        var lead = CreateConvertedLead();

        var result = lead.AssignManager(_managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
    }

    [Fact]
    public void StartWork_NewLead_MovesToInProgressAndRecordsHistory()
    {
        var lead = CreateLead();

        var result = lead.StartWork(_managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.InProgress);
        var entry = lead.StatusHistory[^1];
        entry.FromStatus.Should().Be(LeadStatus.New);
        entry.ToStatus.Should().Be(LeadStatus.InProgress);
        entry.ChangedAtUtc.Should().Be(_later);
        entry.ChangedByUserId.Should().Be(_managerId);
    }

    [Fact]
    public void StartWork_LeadInProgress_ReturnsNotNewError()
    {
        var lead = CreateLeadInProgress();

        var result = lead.StartWork(_managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotNew);
        lead.StatusHistory.Should().HaveCount(2);
    }

    [Fact]
    public void StartWork_EmptyUserId_ReturnsUserIdEmptyErrorAndKeepsStatus()
    {
        var lead = CreateLead();

        var result = lead.StartWork(Guid.Empty, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.UserIdEmpty);
        lead.Status.Should().Be(LeadStatus.New);
        lead.StatusHistory.Should().ContainSingle();
    }

    [Theory]
    [InlineData(LeadStatus.New)]
    [InlineData(LeadStatus.InProgress)]
    public void MarkLost_OpenLead_MovesToLostWithReason(LeadStatus initialStatus)
    {
        var lead = initialStatus == LeadStatus.New ? CreateLead() : CreateLeadInProgress();

        var result = lead.MarkLost("  Too expensive  ", _managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Lost);
        lead.IsOpen.Should().BeFalse();
        lead.LostReason.Should().Be("Too expensive");
        lead.StatusHistory[^1].Reason.Should().Be("Too expensive");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkLost_EmptyReason_ReturnsLostReasonEmptyError(string? reason)
    {
        var lead = CreateLead();

        var result = lead.MarkLost(reason, _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.LostReasonEmpty);
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public void MarkLost_ReasonTooLong_ReturnsLostReasonTooLongError()
    {
        var lead = CreateLead();

        var result = lead.MarkLost(new string('a', 501), _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.LostReasonTooLong);
    }

    [Fact]
    public void MarkLost_LostLead_ReturnsNotOpenError()
    {
        var lead = CreateLostLead();

        var result = lead.MarkLost("Again", _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
    }

    [Fact]
    public void Reopen_LostLead_MovesToInProgressAndClearsReason()
    {
        var lead = CreateLostLead();

        var result = lead.Reopen(_managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.InProgress);
        lead.LostReason.Should().BeNull();
        lead.StatusHistory[^1].FromStatus.Should().Be(LeadStatus.Lost);
    }

    [Theory]
    [InlineData(LeadStatus.New)]
    [InlineData(LeadStatus.InProgress)]
    [InlineData(LeadStatus.Converted)]
    public void Reopen_LeadThatIsNotLost_ReturnsNotLostError(LeadStatus status)
    {
        var lead = status switch
        {
            LeadStatus.New => CreateLead(),
            LeadStatus.InProgress => CreateLeadInProgress(),
            _ => CreateConvertedLead()
        };

        var result = lead.Reopen(_managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotLost);
    }

    [Theory]
    [InlineData(LeadStatus.New)]
    [InlineData(LeadStatus.InProgress)]
    public void MarkConverted_OpenLead_MovesToConvertedAndStoresContract(LeadStatus initialStatus)
    {
        var lead = initialStatus == LeadStatus.New ? CreateLead() : CreateLeadInProgress();

        var result = lead.MarkConverted(_contractId, _managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Converted);
        lead.ContractId.Should().Be(_contractId);
        lead.StatusHistory[^1].ToStatus.Should().Be(LeadStatus.Converted);
    }

    [Fact]
    public void MarkConverted_OpenLead_RaisesLeadConvertedEvent()
    {
        var lead = CreateLead();
        lead.ClearDomainEvents();

        lead.MarkConverted(_contractId, _managerId, _later);

        lead.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new LeadConvertedDomainEvent(lead.Id, _contractId, _later));
    }

    [Fact]
    public void MarkConverted_LostLead_ReturnsNotOpenError()
    {
        var lead = CreateLostLead();

        var result = lead.MarkConverted(_contractId, _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
        lead.ContractId.Should().BeNull();
    }

    [Fact]
    public void MarkConverted_EmptyContractId_ReturnsContractIdEmptyError()
    {
        var lead = CreateLead();

        var result = lead.MarkConverted(Guid.Empty, _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.ContractIdEmpty);
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public void MarkConverted_EmptyUserId_ReturnsUserIdEmptyErrorWithoutChanges()
    {
        var lead = CreateLead();
        lead.ClearDomainEvents();

        var result = lead.MarkConverted(_contractId, Guid.Empty, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Lead.UserIdEmpty);
        lead.ContractId.Should().BeNull();
        lead.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddNote_ValidText_AddsTrimmedNoteWithAuthorAndTime()
    {
        var lead = CreateLead();

        var result = lead.AddNote("  Called back, will visit on Monday  ", _managerId, _later);

        result.IsSuccess.Should().BeTrue();
        var note = lead.Notes.Should().ContainSingle().Which;
        note.Should().BeSameAs(result.Value);
        note.Text.Should().Be("Called back, will visit on Monday");
        note.AuthorId.Should().Be(_managerId);
        note.WrittenAtUtc.Should().Be(_later);
    }

    [Fact]
    public void AddNote_ConvertedLead_AddsNote()
    {
        var lead = CreateConvertedLead();

        var result = lead.AddNote("Contract signed in person", _managerId, _later);

        result.IsSuccess.Should().BeTrue();
        lead.Notes.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddNote_EmptyText_ReturnsTextEmptyError(string? text)
    {
        var lead = CreateLead();

        var result = lead.AddNote(text, _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LeadNote.TextEmpty);
        lead.Notes.Should().BeEmpty();
    }

    [Fact]
    public void AddNote_TextAtMaximumLength_AddsNote()
    {
        var lead = CreateLead();

        var result = lead.AddNote(new string('a', 4000), _managerId, _later);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddNote_TextTooLong_ReturnsTextTooLongError()
    {
        var lead = CreateLead();

        var result = lead.AddNote(new string('a', 4001), _managerId, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LeadNote.TextTooLong);
    }

    [Fact]
    public void AddNote_EmptyAuthorId_ReturnsAuthorIdEmptyError()
    {
        var lead = CreateLead();

        var result = lead.AddNote("Called back", Guid.Empty, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LeadNote.AuthorIdEmpty);
    }
}
