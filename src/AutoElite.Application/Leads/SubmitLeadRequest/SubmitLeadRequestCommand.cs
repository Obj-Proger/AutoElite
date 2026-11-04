namespace AutoElite.Application.Leads.SubmitLeadRequest;

/// <summary>
/// Records a training request from the public website form, without signing in (FR-2.2). The request gets the
/// website source and waits for a manager; the caller learns nothing but success.
/// </summary>
/// <param name="ContactName">The name the person gave.</param>
/// <param name="Phone">The phone number in E.164 format, if given.</param>
/// <param name="Email">The email address, if given; a phone or an email is required.</param>
/// <param name="DesiredCategory">The licence category of interest, if chosen.</param>
/// <param name="Comment">A free-text comment, if any.</param>
/// <param name="BranchId">The branch chosen on the form, if any.</param>
[AllowAnonymousRequest]
public sealed record SubmitLeadRequestCommand(
    string? ContactName,
    string? Phone,
    string? Email,
    LicenseCategory? DesiredCategory,
    string? Comment,
    Guid? BranchId) : ICommand;
