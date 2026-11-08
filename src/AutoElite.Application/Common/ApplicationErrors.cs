namespace AutoElite.Application.Common;

/// <summary>
/// Catalog of errors that originate in the application layer, as opposed to
/// <see cref="DomainErrors"/>, which are raised by domain rules.
/// </summary>
public static class ApplicationErrors
{
    /// <summary>The code of the error returned when request validation fails.</summary>
    public const string ValidationFailedCode = "Validation.Failed";

    /// <summary>The code assigned to a field error when the validation rule did not specify one.</summary>
    public const string InvalidFieldCode = "Validation.Invalid";

    /// <summary>Creates the error returned when a request fails validation, carrying every failed field.</summary>
    /// <param name="fieldErrors">The failures of the individual fields.</param>
    public static Error ValidationFailed(IReadOnlyList<FieldError> fieldErrors) =>
        Error.Validation(ValidationFailedCode, "One or more validation errors occurred.", fieldErrors);

    /// <summary>
    /// Creates the error returned when an entity does not exist or is outside the caller's data scope,
    /// so that callers cannot learn about records they may not see. The code is <c>{Entity}.NotFound</c>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the missing entity.</typeparam>
    public static Error NotFound<TEntity>()
        where TEntity : BaseEntity =>
        Error.NotFound($"{typeof(TEntity).Name}.NotFound", $"{typeof(TEntity).Name} was not found.");

    /// <summary>Errors of user accounts and invitations.</summary>
    public static class Account
    {
        /// <summary>A new user is given no roles.</summary>
        public static readonly Error RolesEmpty = Error.Validation("Account.RolesEmpty", "At least one role is required.");

        /// <summary>A role is listed more than once.</summary>
        public static readonly Error DuplicateRoles = Error.Validation("Account.DuplicateRoles", "Each role can be listed only once.");

        /// <summary>A manager tries to invite a user with a role other than student.</summary>
        public static readonly Error ManagerInvitesOnlyStudents = Error.Forbidden(
            "Account.ManagerInvitesOnlyStudents", "Managers can invite only students.");

        /// <summary>A manager tries to act on a user who holds a staff role.</summary>
        public static readonly Error ManagerManagesOnlyStudents = Error.Forbidden(
            "Account.ManagerManagesOnlyStudents", "Managers can manage only students.");

        /// <summary>A user tries to deactivate their own account.</summary>
        public static readonly Error CannotDeactivateSelf = Error.Conflict(
            "Account.CannotDeactivateSelf", "You cannot deactivate your own account.");

        /// <summary>Another user already has the email address.</summary>
        public static readonly Error EmailTaken = Error.Conflict(
            "Account.EmailTaken", "A user with this email address already exists.");

        /// <summary>The user has already accepted the invitation and set a password.</summary>
        public static readonly Error AlreadyActivated = Error.Conflict(
            "Account.AlreadyActivated", "The user has already accepted the invitation.");

        /// <summary>The invitation link is wrong, expired or already used, or its user cannot be invited.</summary>
        public static readonly Error InvitationInvalid = Error.Validation(
            "Account.InvitationInvalid", "The invitation link is invalid or has expired. Ask the school for a new one.");

        /// <summary>No user has the email address, or the password is wrong.</summary>
        public static readonly Error InvalidCredentials = Error.Unauthorized(
            "Account.InvalidCredentials", "Email or password is incorrect.");

        /// <summary>Sign-in is blocked for a while after too many wrong passwords.</summary>
        public static readonly Error LockedOut = Error.Forbidden(
            "Account.LockedOut", "Too many failed sign-in attempts. Try again later.");

        /// <summary>The password is correct, but the email address is not confirmed.</summary>
        public static readonly Error EmailNotConfirmed = Error.Forbidden(
            "Account.EmailNotConfirmed", "Confirm your email address before signing in.");

        /// <summary>The password is correct, but the user is deactivated.</summary>
        public static readonly Error UserDeactivated = Error.Forbidden(
            "Account.UserDeactivated", "Your account is deactivated. Contact the school.");

        /// <summary>The refresh token is unknown, expired, revoked or used, or its user is deactivated.</summary>
        public static readonly Error RefreshTokenInvalid = Error.Unauthorized(
            "Account.RefreshTokenInvalid", "Your session has expired. Sign in again.");

        /// <summary>The password reset link is wrong, expired or already used, or its user is deactivated.</summary>
        public static readonly Error PasswordResetInvalid = Error.Validation(
            "Account.PasswordResetInvalid", "The password reset link is invalid or has expired. Request a new one.");

        /// <summary>The email confirmation link is wrong, expired or used, or its user is deactivated.</summary>
        public static readonly Error EmailChangeInvalid = Error.Validation(
            "Account.EmailChangeInvalid", "The email confirmation link is invalid or has expired. Request the change again.");

        /// <summary>The current password given to change it is wrong.</summary>
        public static readonly Error CurrentPasswordWrong = Error.Validation(
            "Account.CurrentPasswordWrong", "The current password is incorrect.");
    }

    /// <summary>Errors of branches that need more than one branch to check.</summary>
    public static class Branch
    {
        /// <summary>Another branch already has the name.</summary>
        public static readonly Error NameTaken = Error.Conflict("Branch.NameTaken", "A branch with this name already exists.");

        /// <summary>A record would be tied to an inactive branch.</summary>
        public static readonly Error Inactive = Error.Conflict(
            "Branch.Inactive", "The branch is inactive, so nothing new can be tied to it.");
    }

    /// <summary>Errors of contracts that need more than the contract to check.</summary>
    public static class Contract
    {
        /// <summary>Another contract already has the number.</summary>
        public static readonly Error NumberTaken = Error.Conflict("Contract.NumberTaken", "A contract with this number already exists.");

        /// <summary>The user chosen as the student does not hold the student role.</summary>
        public static readonly Error NotAStudent = Error.Validation(
            "Contract.NotAStudent", "A contract can be drawn up only for a user with the student role.");

        /// <summary>The day a contract would be completed or terminated on is in the future.</summary>
        public static readonly Error ClosedInFuture = Error.Validation(
            "Contract.ClosedInFuture", "A contract cannot be closed on a future day.");

        /// <summary>The status filter of the contract list is not a contract status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation("Contract.StatusNotDefined", "Contract status is not valid.");
    }

    /// <summary>Errors of courses that need more than the course to check.</summary>
    public static class Course
    {
        /// <summary>Questions, theory lessons or test templates refer to the topic.</summary>
        public static readonly Error TopicInUse = Error.Conflict(
            "Course.TopicInUse", "The topic is used by questions, lessons or test templates and cannot be removed.");
    }

    /// <summary>Errors of reports.</summary>
    public static class Report
    {
        /// <summary>The last day of the period is earlier than the first.</summary>
        public static readonly Error PeriodInvalid = Error.Validation(
            "Report.PeriodInvalid", "The last day of the period cannot be earlier than the first.");

        /// <summary>The period is longer than <see cref="Analytics.ReportQueryValidator{TQuery}.MaxPeriodDays"/> days.</summary>
        public static readonly Error PeriodTooLong = Error.Validation(
            "Report.PeriodTooLong", "A report can cover at most three years.");

        /// <summary>The branch filter is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = Error.Validation("Report.BranchIdEmpty", "Branch ID cannot be an empty GUID.");

        /// <summary>The course filter is an empty GUID.</summary>
        public static readonly Error CourseIdEmpty = Error.Validation("Report.CourseIdEmpty", "Course ID cannot be an empty GUID.");

        /// <summary>The report to export is not a <see cref="Analytics.ReportKind"/>.</summary>
        public static readonly Error KindNotDefined = Error.Validation("Report.KindNotDefined", "Report kind is not valid.");

        /// <summary>The file format is not a <see cref="Analytics.ReportFormat"/>.</summary>
        public static readonly Error FormatNotDefined = Error.Validation("Report.FormatNotDefined", "Report format is not valid.");
    }

    /// <summary>Errors of chats.</summary>
    public static class Chat
    {
        /// <summary>An attachment is not a document the sender attached to this chat.</summary>
        public static readonly Error AttachmentInvalid = Error.Validation(
            "Chat.AttachmentInvalid", "Attachments must be documents you attached to this chat.");

        /// <summary>An attachment was already sent with another message.</summary>
        public static readonly Error AttachmentAlreadySent = Error.Conflict(
            "Chat.AttachmentAlreadySent", "The document was already sent with another message.");
    }

    /// <summary>Errors of documents.</summary>
    public static class Document
    {
        /// <summary>The file content is not as long as declared, or its signature does not match its type.</summary>
        public static readonly Error ContentMismatch = Error.Validation(
            "Document.ContentMismatch", "The file content does not match its type or size.");

        /// <summary>Documents cannot be attached to records of this kind.</summary>
        public static readonly Error OwnerKindNotSupported = Error.Validation(
            "Document.OwnerKindNotSupported", "Documents cannot be attached to records of this kind.");

        /// <summary>The record does not exist or the caller may not see its documents.</summary>
        public static readonly Error OwnerNotFound = Error.NotFound("Document.OwnerNotFound", "The record was not found.");

        /// <summary>The document is an attachment of a chat message, which is deleted together with the message.</summary>
        public static readonly Error SentInMessage = Error.Conflict(
            "Document.SentInMessage", "The document was sent in a message; delete the message instead.");

        /// <summary>A profile photo or an illustration must be a JPEG, PNG or WebP image.</summary>
        public static readonly Error ImageRequired = Error.Validation(
            "Document.ImageRequired", "The document must be a JPEG, PNG or WebP image.");

        /// <summary>No file content was given.</summary>
        public static readonly Error ContentMissing = Error.Validation("Document.ContentMissing", "The file content is missing.");
    }

    /// <summary>Errors of staff tasks.</summary>
    public static class StaffTask
    {
        /// <summary>The assignee is not an active staff member.</summary>
        public static readonly Error AssigneeNotStaff = Error.Validation(
            "StaffTask.AssigneeNotStaff", "Tasks can be given only to active staff members.");

        /// <summary>The assignee works for a branch outside the manager's scope.</summary>
        public static readonly Error AssigneeOfAnotherBranch = Error.Conflict(
            "StaffTask.AssigneeOfAnotherBranch", "The staff member works for another branch.");

        /// <summary>Only one of the kind and the identifier of the linked record is given.</summary>
        public static readonly Error SubjectIncomplete = Error.Validation(
            "StaffTask.SubjectIncomplete", "The linked record needs both a kind and an identifier.");

        /// <summary>The linked record does not exist.</summary>
        public static readonly Error SubjectNotFound = Error.Validation(
            "StaffTask.SubjectNotFound", "The linked record does not exist.");

        /// <summary>The status filter of the task list is not a task status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation("StaffTask.StatusNotDefined", "Task status is not valid.");
    }

    /// <summary>Errors of leads that need more than the lead to check.</summary>
    public static class Lead
    {
        /// <summary>The user chosen as the responsible manager is not an active manager.</summary>
        public static readonly Error NotAManager = Error.Validation(
            "Lead.NotAManager", "The responsible person must be an active manager.");

        /// <summary>The manager works for another branch than the lead's.</summary>
        public static readonly Error ManagerOfAnotherBranch = Error.Conflict(
            "Lead.ManagerOfAnotherBranch", "The manager works for another branch than the lead.");

        /// <summary>The status filter of the lead list is not a lead status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation("Lead.StatusNotDefined", "Lead status is not valid.");
    }

    /// <summary>Errors of study groups that need more than the group to check.</summary>
    public static class StudyGroup
    {
        /// <summary>The user chosen as the responsible instructor is not an active theory instructor.</summary>
        public static readonly Error NotATheoryInstructor = Error.Validation(
            "StudyGroup.NotATheoryInstructor", "The responsible instructor must be an active theory instructor.");

        /// <summary>The instructor works for another branch than the group's.</summary>
        public static readonly Error InstructorOfAnotherBranch = Error.Conflict(
            "StudyGroup.InstructorOfAnotherBranch", "The instructor works for another branch than the group.");

        /// <summary>The contract and the group are tied to different branches.</summary>
        public static readonly Error ContractOfAnotherBranch = Error.Conflict(
            "StudyGroup.ContractOfAnotherBranch", "The contract belongs to another branch than the group.");
    }

    /// <summary>Errors of payment queries.</summary>
    public static class Payment
    {
        /// <summary>The end of the report period is before its start.</summary>
        public static readonly Error PeriodInvalid = Error.Validation(
            "Payment.PeriodInvalid", "The end of the period cannot be before its start.");

        /// <summary>The status filter of the payment report is not a payment status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation("Payment.StatusNotDefined", "Payment status is not valid.");
    }

    /// <summary>Errors of theory lessons that need more than the lesson to check.</summary>
    public static class TheoryLesson
    {
        /// <summary>The group or the instructor already has a lesson at that time.</summary>
        public static readonly Error TimeConflict = Error.Conflict(
            "TheoryLesson.TimeConflict", "The group or the instructor already has a lesson at this time.");

        /// <summary>An attendance command carries no marks.</summary>
        public static readonly Error MarksEmpty = Error.Validation("TheoryLesson.MarksEmpty", "At least one attendance mark is required.");

        /// <summary>A contract is marked more than once in one command.</summary>
        public static readonly Error DuplicateMark = Error.Validation(
            "TheoryLesson.DuplicateMark", "Each student can appear only once among the marks.");

        /// <summary>The status filter of the schedule is not a lesson status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation(
            "TheoryLesson.StatusNotDefined", "Lesson status is not valid.");
    }

    /// <summary>Errors of the fleet that need more than the vehicle to check.</summary>
    public static class Vehicle
    {
        /// <summary>Another vehicle already has the licence plate.</summary>
        public static readonly Error PlateTaken = Error.Conflict("Vehicle.PlateTaken", "Another vehicle already has this licence plate.");
    }

    /// <summary>Errors of driving routes that need more than the route to check.</summary>
    public static class DrivingRoute
    {
        /// <summary>The route would start from a training ground that is no longer used.</summary>
        public static readonly Error TrainingGroundInactive = Error.Conflict(
            "DrivingRoute.TrainingGroundInactive", "A route can start only from a training ground in use.");
    }

    /// <summary>Errors of driving slots that need more than the slot to check.</summary>
    public static class DrivingSlot
    {
        /// <summary>The instructor or the vehicle already has a slot at that time.</summary>
        public static readonly Error TimeConflict = Error.Conflict(
            "DrivingSlot.TimeConflict", "The instructor or the vehicle already has a slot at this time.");

        /// <summary>The student already has a driving lesson at that time.</summary>
        public static readonly Error StudentBusy = Error.Conflict(
            "DrivingSlot.StudentBusy", "You already have a driving lesson at this time.");

        /// <summary>The status filter of the driving schedule is not a slot status.</summary>
        public static readonly Error StatusNotDefined = Error.Validation("DrivingSlot.StatusNotDefined", "Slot status is not valid.");
    }

    /// <summary>Errors of the question bank that need more than the question to check.</summary>
    public static class Question
    {
        /// <summary>Another question already has the external code.</summary>
        public static readonly Error ExternalCodeTaken = Error.Conflict(
            "Question.ExternalCodeTaken", "Another question already has this external code.");

        /// <summary>Someone other than an administrator writes an official question.</summary>
        public static readonly Error OfficialAdministratorOnly = Error.Forbidden(
            "Question.OfficialAdministratorOnly", "Only an administrator can add or change official questions.");

        /// <summary>Some rows of an imported file are invalid; their errors are listed by row and nothing is imported.</summary>
        public static readonly Error ImportInvalid = Error.Validation(
            "Question.ImportInvalid", "Some questions in the file are invalid, so nothing was imported.");

        /// <summary>The imported file has no questions.</summary>
        public static readonly Error ImportEmpty = Error.Validation("Question.ImportEmpty", "The file has no questions.");

        /// <summary>The imported file has more questions than one import takes.</summary>
        public static readonly Error ImportTooManyRows = Error.Validation(
            "Question.ImportTooManyRows", "A file can have at most 2000 questions.");

        /// <summary>The file is not in a format the import reads.</summary>
        public static readonly Error ImportFormatNotSupported = Error.Validation(
            "Question.ImportFormatNotSupported", "The file format is not supported.");

        /// <summary>The file could not be read, for example because it is damaged or its columns are wrong.</summary>
        public static readonly Error ImportUnreadable = Error.Validation(
            "Question.ImportUnreadable", "The file could not be read.");

        /// <summary>No file content was given.</summary>
        public static readonly Error ImportContentMissing = Error.Validation(
            "Question.ImportContentMissing", "The file content is missing.");

        /// <summary>A licence category in a row is not one the school knows.</summary>
        public static readonly Error ImportCategoryUnknown = Error.Validation(
            "Question.ImportCategoryUnknown", "The licence category is not known.");

        /// <summary>The same external code appears in more than one row.</summary>
        public static readonly Error ImportExternalCodeRepeated = Error.Validation(
            "Question.ImportExternalCodeRepeated", "The external code appears in more than one row.");

        /// <summary>The external code belongs to a question of the other source.</summary>
        public static readonly Error ImportSourceMismatch = Error.Conflict(
            "Question.ImportSourceMismatch", "A question with this external code comes from another source.");

        /// <summary>A row names an illustration the file does not have.</summary>
        public static readonly Error ImportImageMissing = Error.Validation(
            "Question.ImportImageMissing", "The image of the question is not in the file.");
    }

    /// <summary>Errors of test attempts that need more than the attempt to check.</summary>
    public static class TestSession
    {
        /// <summary>The contract already has an attempt in progress.</summary>
        public static readonly Error AlreadyInProgress = Error.Conflict(
            "TestSession.AlreadyInProgress", "Finish the test in progress before starting a new one.");

        /// <summary>The bank does not have enough active questions for a block or for the questions added for a mistake.</summary>
        public static readonly Error NotEnoughQuestions = Error.Conflict(
            "TestSession.NotEnoughQuestions", "The question bank does not have enough active questions for this test.");
    }

    /// <summary>Errors of exams that need more than the exam to check.</summary>
    public static class Exam
    {
        /// <summary>The contract already has an attempt at the theory exam.</summary>
        public static readonly Error AttemptAlreadyTaken = Error.Conflict(
            "Exam.AttemptAlreadyTaken", "The exam has already been taken on this contract.");
    }

    /// <summary>Errors of the rules in <see cref="Users.PasswordPolicy"/>.</summary>
    public static class Password
    {
        /// <summary>The password is empty.</summary>
        public static readonly Error Empty = Error.Validation("Password.Empty", "Password cannot be empty.");

        /// <summary>The password is shorter than the minimum length.</summary>
        public static readonly Error TooShort = Error.Validation("Password.TooShort", "Password must be at least 8 characters long.");

        /// <summary>The password is longer than the maximum length.</summary>
        public static readonly Error TooLong = Error.Validation("Password.TooLong", "Password must be at most 128 characters long.");

        /// <summary>The password has no letter.</summary>
        public static readonly Error LetterRequired = Error.Validation("Password.LetterRequired", "Password must contain a letter.");

        /// <summary>The password has no digit.</summary>
        public static readonly Error DigitRequired = Error.Validation("Password.DigitRequired", "Password must contain a digit.");

        /// <summary>The new password is the same as the current one.</summary>
        public static readonly Error SameAsCurrent = Error.Validation(
            "Password.SameAsCurrent", "The new password must be different from the current one.");
    }

    /// <summary>Errors of the period of schedule queries.</summary>
    public static class Schedule
    {
        /// <summary>A schedule query asks for a period longer than <see cref="SchedulePeriod.MaxLength"/>.</summary>
        public static readonly Error PeriodTooLong = Error.Validation(
            "Schedule.PeriodTooLong", "The schedule period can be at most 92 days long.");
    }

    /// <summary>Errors of the page number and size of paged queries.</summary>
    public static class Paging
    {
        /// <summary>The page number is below 1 or above <see cref="Pagination.PageRequest.MaxPage"/>.</summary>
        public static readonly Error PageOutOfRange = Error.Validation(
            "Paging.PageOutOfRange", "Page must be between 1 and 100000.");

        /// <summary>The page size is below 1 or above <see cref="Pagination.PageRequest.MaxPageSize"/>.</summary>
        public static readonly Error PageSizeOutOfRange = Error.Validation(
            "Paging.PageSizeOutOfRange", "Page size must be between 1 and 100.");

        /// <summary>The search text of a list is longer than <see cref="Pagination.PageRequest.MaxSearchLength"/>.</summary>
        public static readonly Error SearchTooLong = Error.Validation(
            "Paging.SearchTooLong", "Search text must be at most 200 characters long.");
    }

    /// <summary>Errors of saving changes.</summary>
    public static class Concurrency
    {
        /// <summary>Another request changed a record after this request loaded it.</summary>
        public static readonly Error Conflict = Error.Conflict(
            "Concurrency.Conflict", "The data was changed by someone else. Reload it and try again.");
    }

    /// <summary>Errors of reading a request, before it reaches a command or a query.</summary>
    public static class Request
    {
        /// <summary>
        /// A value of the request could not be read, such as malformed JSON, text where a number is expected or an
        /// unknown name of an enumeration; reported per field within <see cref="ValidationFailed"/>.
        /// </summary>
        public static readonly Error UnreadableValue = Error.Validation("Request.UnreadableValue", "The value could not be read.");
    }

    /// <summary>Errors of authentication and authorization.</summary>
    public static class Auth
    {
        /// <summary>The request requires a signed-in user, but the caller is anonymous.</summary>
        public static readonly Error Unauthenticated = Error.Unauthorized("Auth.Unauthenticated", "You need to sign in.");

        /// <summary>The signed-in user does not have a role that the request requires.</summary>
        public static readonly Error Forbidden = Error.Forbidden("Auth.Forbidden", "You do not have permission to perform this action.");
    }
}
