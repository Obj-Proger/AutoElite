namespace AutoElite.Domain.Common;

/// <summary>
/// Central catalog of domain-specific <see cref="Error"/> instances, grouped by concept.
/// </summary>
public static class DomainErrors
{
    /// <summary>Errors related to the <see cref="Entities.DrivingSlot"/> aggregate.</summary>
    public static class DrivingSlot
    {
        /// <summary>The instructor identifier is an empty GUID.</summary>
        public static readonly Error InstructorIdEmpty = new("DrivingSlot.InstructorIdEmpty", "Instructor ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The vehicle is out of service.</summary>
        public static readonly Error VehicleInactive = new("DrivingSlot.VehicleInactive", "An inactive vehicle cannot be used for a slot.", ErrorType.Conflict);

        /// <summary>The training ground is no longer used.</summary>
        public static readonly Error TrainingGroundInactive = new("DrivingSlot.TrainingGroundInactive", "An inactive training ground cannot be used for a slot.", ErrorType.Conflict);

        /// <summary>The route is no longer used.</summary>
        public static readonly Error RouteInactive = new("DrivingSlot.RouteInactive", "An inactive route cannot be used for a slot.", ErrorType.Conflict);

        /// <summary>The slot would start at or before the current time.</summary>
        public static readonly Error StartInPast = new("DrivingSlot.StartInPast", "A slot must start in the future.", ErrorType.Validation);

        /// <summary>The slot is longer than the maximum duration.</summary>
        public static readonly Error DurationTooLong = new("DrivingSlot.DurationTooLong", "A slot cannot be longer than 4 hours.", ErrorType.Validation);

        /// <summary>The slot is booked, completed or withdrawn.</summary>
        public static readonly Error NotAvailable = new("DrivingSlot.NotAvailable", "The slot is not available for booking.", ErrorType.Conflict);

        /// <summary>The lesson has already started.</summary>
        public static readonly Error AlreadyStarted = new("DrivingSlot.AlreadyStarted", "The lesson has already started.", ErrorType.Conflict);

        /// <summary>The lesson outcome can be recorded only once the lesson has started.</summary>
        public static readonly Error NotStarted = new("DrivingSlot.NotStarted", "The lesson has not started yet.", ErrorType.Conflict);

        /// <summary>Only an active contract can book a slot.</summary>
        public static readonly Error ContractNotActive = new("DrivingSlot.ContractNotActive", "Only an active contract can book a lesson.", ErrorType.Conflict);

        /// <summary>The contract's licence category differs from the vehicle's.</summary>
        public static readonly Error CategoryMismatch = new("DrivingSlot.CategoryMismatch", "The vehicle is for a different licence category than the contract.", ErrorType.Conflict);

        /// <summary>The slot has no active booking.</summary>
        public static readonly Error NotBooked = new("DrivingSlot.NotBooked", "The slot has no active booking.", ErrorType.Conflict);

        /// <summary>The slot is completed or already withdrawn.</summary>
        public static readonly Error NotOpen = new("DrivingSlot.NotOpen", "Only an available or booked slot can be withdrawn.", ErrorType.Conflict);

        /// <summary>The contract passed in is not the contract of the active booking.</summary>
        public static readonly Error ContractMismatch = new("DrivingSlot.ContractMismatch", "The contract does not belong to the booking.", ErrorType.Validation);

        /// <summary>The identifier of the user making the change was provided but is an empty GUID.</summary>
        public static readonly Error UserIdEmpty = new("DrivingSlot.UserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>No reason was given for the cancellation.</summary>
        public static readonly Error ReasonEmpty = new("DrivingSlot.ReasonEmpty", "A reason is required.", ErrorType.Validation);

        /// <summary>The reason exceeds the maximum allowed length.</summary>
        public static readonly Error ReasonTooLong = new("DrivingSlot.ReasonTooLong", "Reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The same skill is graded more than once.</summary>
        public static readonly Error DuplicateSkillGrade = new("DrivingSlot.DuplicateSkillGrade", "Each skill can be graded only once.", ErrorType.Validation);

        /// <summary>The instructor note exceeds the maximum allowed length.</summary>
        public static readonly Error NoteTooLong = new("DrivingSlot.NoteTooLong", "Note exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>Only a completed lesson can be rated.</summary>
        public static readonly Error LessonNotCompleted = new("DrivingSlot.LessonNotCompleted", "Only a completed lesson can be rated.", ErrorType.Conflict);

        /// <summary>The student has already rated the lesson.</summary>
        public static readonly Error AlreadyRated = new("DrivingSlot.AlreadyRated", "The lesson has already been rated.", ErrorType.Conflict);

        /// <summary>The rating is outside 1..5.</summary>
        public static readonly Error RatingOutOfRange = new("DrivingSlot.RatingOutOfRange", "Rating must be between 1 and 5.", ErrorType.Validation);

        /// <summary>The rating comment exceeds the maximum allowed length.</summary>
        public static readonly Error RatingCommentTooLong = new("DrivingSlot.RatingCommentTooLong", "Comment exceeds the maximum allowed length.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.Exam"/> aggregate.</summary>
    public static class Exam
    {
        /// <summary>The study group is inactive.</summary>
        public static readonly Error GroupInactive = new("Exam.GroupInactive", "An exam cannot be scheduled for an inactive group.", ErrorType.Conflict);

        /// <summary>The kind is not a defined <see cref="Enums.ExamKind"/> value.</summary>
        public static readonly Error KindNotDefined = new("Exam.KindNotDefined", "Exam kind is not valid.", ErrorType.Validation);

        /// <summary>A theory exam was scheduled without a test template.</summary>
        public static readonly Error TemplateRequired = new("Exam.TemplateRequired", "A theory exam requires a test template.", ErrorType.Validation);

        /// <summary>A practical exam was given a test template.</summary>
        public static readonly Error TemplateNotAllowed = new("Exam.TemplateNotAllowed", "A practical exam does not use a test template.", ErrorType.Validation);

        /// <summary>The test template is no longer used.</summary>
        public static readonly Error TemplateInactive = new("Exam.TemplateInactive", "An inactive test template cannot be used for an exam.", ErrorType.Conflict);

        /// <summary>The template is for another licence category than the group.</summary>
        public static readonly Error TemplateCategoryMismatch = new("Exam.TemplateCategoryMismatch", "The test template is for a different licence category than the group.", ErrorType.Conflict);

        /// <summary>The exam would start at or before the current time.</summary>
        public static readonly Error ScheduledInPast = new("Exam.ScheduledInPast", "An exam must be scheduled in the future.", ErrorType.Validation);

        /// <summary>The exam is cancelled.</summary>
        public static readonly Error NotScheduled = new("Exam.NotScheduled", "The exam is cancelled.", ErrorType.Conflict);

        /// <summary>The exam already has results, so it took place.</summary>
        public static readonly Error HasResults = new("Exam.HasResults", "An exam with results cannot be moved or cancelled.", ErrorType.Conflict);

        /// <summary>No reason was given for the cancellation.</summary>
        public static readonly Error CancellationReasonEmpty = new("Exam.CancellationReasonEmpty", "A reason is required to cancel an exam.", ErrorType.Validation);

        /// <summary>The cancellation reason exceeds the maximum allowed length.</summary>
        public static readonly Error CancellationReasonTooLong = new("Exam.CancellationReasonTooLong", "Cancellation reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The result does not match the kind of the exam.</summary>
        public static readonly Error WrongKind = new("Exam.WrongKind", "This kind of result cannot be recorded for this exam.", ErrorType.Conflict);

        /// <summary>The test attempt was not started for this exam.</summary>
        public static readonly Error SessionNotForExam = new("Exam.SessionNotForExam", "The test attempt does not belong to this exam.", ErrorType.Validation);

        /// <summary>The test attempt is still in progress.</summary>
        public static readonly Error SessionNotFinished = new("Exam.SessionNotFinished", "The test attempt is not finished yet.", ErrorType.Conflict);

        /// <summary>Results can be recorded only once the exam has started.</summary>
        public static readonly Error NotStarted = new("Exam.NotStarted", "The exam has not started yet.", ErrorType.Conflict);

        /// <summary>The group passed in is not the exam's group.</summary>
        public static readonly Error GroupMismatch = new("Exam.GroupMismatch", "The group does not belong to the exam.", ErrorType.Validation);

        /// <summary>The contract is not a current member of the exam's group.</summary>
        public static readonly Error NotGroupMember = new("Exam.NotGroupMember", "The student is not a member of the group.", ErrorType.Conflict);

        /// <summary>The student already has a result; correct it instead.</summary>
        public static readonly Error AlreadyRecorded = new("Exam.AlreadyRecorded", "The student already has a result for this exam.", ErrorType.Conflict);

        /// <summary>The student has no result to correct.</summary>
        public static readonly Error ResultNotFound = new("Exam.ResultNotFound", "The student has no result for this exam.", ErrorType.NotFound);

        /// <summary>The score is negative or above the maximum.</summary>
        public static readonly Error ScoreOutOfRange = new("Exam.ScoreOutOfRange", "Score must be between 0 and 1000.", ErrorType.Validation);

        /// <summary>The comment exceeds the maximum allowed length.</summary>
        public static readonly Error CommentTooLong = new("Exam.CommentTooLong", "Comment exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The identifier of the user entering the result is an empty GUID.</summary>
        public static readonly Error UserIdEmpty = new("Exam.UserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.Email"/> validation.</summary>
    public static class Email
    {
        /// <summary>The email address is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("Email.Empty", "Email address cannot be empty.", ErrorType.Validation);

        /// <summary>The email address exceeds the maximum allowed length.</summary>
        public static readonly Error TooLong = new("Email.TooLong", "Email address exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The email address does not match the expected structure.</summary>
        public static readonly Error InvalidFormat = new("Email.InvalidFormat", "Email address is not in a valid format.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="Entities.LessonAttendance"/>.</summary>
    public static class LessonAttendance
    {
        /// <summary>The comment exceeds the maximum allowed length.</summary>
        public static readonly Error CommentTooLong = new("LessonAttendance.CommentTooLong", "Comment exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The identifier of the user making the mark is an empty GUID.</summary>
        public static readonly Error MarkedByUserIdEmpty = new("LessonAttendance.MarkedByUserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.LicensePlate"/> validation.</summary>
    public static class LicensePlate
    {
        /// <summary>The plate is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("LicensePlate.Empty", "Licence plate cannot be empty.", ErrorType.Validation);

        /// <summary>The plate is not 2 to 15 letters and digits after removing spaces and hyphens.</summary>
        public static readonly Error InvalidFormat = new("LicensePlate.InvalidFormat", "Licence plate must consist of 2 to 15 letters and digits.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.Payment"/> aggregate.</summary>
    public static class Payment
    {
        /// <summary>The amount is not in the currency of the contract.</summary>
        public static readonly Error CurrencyMismatch = new("Payment.CurrencyMismatch", "Payment must be in the currency of the contract.", ErrorType.Validation);

        /// <summary>The amount is zero or negative.</summary>
        public static readonly Error AmountNotPositive = new("Payment.AmountNotPositive", "Payment amount must be greater than zero.", ErrorType.Validation);

        /// <summary>The method is not a defined <see cref="Enums.PaymentMethod"/> value.</summary>
        public static readonly Error MethodNotDefined = new("Payment.MethodNotDefined", "Payment method is not valid.", ErrorType.Validation);

        /// <summary>The purpose is null, empty, or whitespace.</summary>
        public static readonly Error PurposeEmpty = new("Payment.PurposeEmpty", "Payment purpose cannot be empty.", ErrorType.Validation);

        /// <summary>The purpose exceeds the maximum allowed length.</summary>
        public static readonly Error PurposeTooLong = new("Payment.PurposeTooLong", "Payment purpose exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The external reference is null, empty, or whitespace.</summary>
        public static readonly Error ExternalReferenceEmpty = new("Payment.ExternalReferenceEmpty", "External reference cannot be empty.", ErrorType.Validation);

        /// <summary>The external reference exceeds the maximum allowed length.</summary>
        public static readonly Error ExternalReferenceTooLong = new("Payment.ExternalReferenceTooLong", "External reference exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The identifier of the user recording the payment was provided but is an empty GUID.</summary>
        public static readonly Error UserIdEmpty = new("Payment.UserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>No reason was given for the refund or cancellation.</summary>
        public static readonly Error ReasonEmpty = new("Payment.ReasonEmpty", "A reason is required.", ErrorType.Validation);

        /// <summary>The reason exceeds the maximum allowed length.</summary>
        public static readonly Error ReasonTooLong = new("Payment.ReasonTooLong", "Reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The contract passed in is not the contract of the payment.</summary>
        public static readonly Error ContractMismatch = new("Payment.ContractMismatch", "The payment belongs to a different contract.", ErrorType.Validation);

        /// <summary>The operation requires a registered, not yet confirmed payment.</summary>
        public static readonly Error NotRegistered = new("Payment.NotRegistered", "Only a registered payment can be confirmed, cancelled or updated.", ErrorType.Conflict);

        /// <summary>Only a confirmed payment can be refunded.</summary>
        public static readonly Error NotConfirmed = new("Payment.NotConfirmed", "Only a confirmed payment can be refunded.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="ValueObjects.PhoneNumber"/> validation.</summary>
    public static class PhoneNumber
    {
        /// <summary>The phone number is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("PhoneNumber.Empty", "Phone number cannot be empty.", ErrorType.Validation);

        /// <summary>The phone number is not in international E.164 format.</summary>
        public static readonly Error InvalidFormat = new("PhoneNumber.InvalidFormat", "Phone number must be in international E.164 format, e.g. +79991234567.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.FullName"/> validation.</summary>
    public static class FullName
    {
        /// <summary>The last name is null, empty, or whitespace.</summary>
        public static readonly Error LastNameEmpty = new("FullName.LastNameEmpty", "Last name cannot be empty.", ErrorType.Validation);

        /// <summary>The last name exceeds the maximum allowed length.</summary>
        public static readonly Error LastNameTooLong = new("FullName.LastNameTooLong", "Last name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The first name is null, empty, or whitespace.</summary>
        public static readonly Error FirstNameEmpty = new("FullName.FirstNameEmpty", "First name cannot be empty.", ErrorType.Validation);

        /// <summary>The first name exceeds the maximum allowed length.</summary>
        public static readonly Error FirstNameTooLong = new("FullName.FirstNameTooLong", "First name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The middle name exceeds the maximum allowed length.</summary>
        public static readonly Error MiddleNameTooLong = new("FullName.MiddleNameTooLong", "Middle name exceeds the maximum allowed length.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.Address"/> validation.</summary>
    public static class Address
    {
        /// <summary>The city is null, empty, or whitespace.</summary>
        public static readonly Error CityEmpty = new("Address.CityEmpty", "City cannot be empty.", ErrorType.Validation);

        /// <summary>The city exceeds the maximum allowed length.</summary>
        public static readonly Error CityTooLong = new("Address.CityTooLong", "City exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The street part of the address is null, empty, or whitespace.</summary>
        public static readonly Error StreetEmpty = new("Address.StreetEmpty", "Street address cannot be empty.", ErrorType.Validation);

        /// <summary>The street part of the address exceeds the maximum allowed length.</summary>
        public static readonly Error StreetTooLong = new("Address.StreetTooLong", "Street address exceeds the maximum allowed length.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.Contract"/> aggregate.</summary>
    public static class Contract
    {
        /// <summary>The contract number is null, empty, or whitespace.</summary>
        public static readonly Error NumberEmpty = new("Contract.NumberEmpty", "Contract number cannot be empty.", ErrorType.Validation);

        /// <summary>The contract number exceeds the maximum allowed length.</summary>
        public static readonly Error NumberTooLong = new("Contract.NumberTooLong", "Contract number exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The student's account is deactivated.</summary>
        public static readonly Error StudentInactive = new("Contract.StudentInactive", "A contract cannot be drawn up for an inactive user.", ErrorType.Conflict);

        /// <summary>The course is no longer offered.</summary>
        public static readonly Error CourseInactive = new("Contract.CourseInactive", "A contract cannot be drawn up for an inactive course.", ErrorType.Conflict);

        /// <summary>The price is zero or negative.</summary>
        public static readonly Error PriceNotPositive = new("Contract.PriceNotPositive", "Contract price must be greater than zero.", ErrorType.Validation);

        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("Contract.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The end date is equal to or earlier than the start date.</summary>
        public static readonly Error EndNotAfterStart = new("Contract.EndNotAfterStart", "End date must be later than start date.", ErrorType.Validation);

        /// <summary>The amount is not in the currency of the contract.</summary>
        public static readonly Error CurrencyMismatch = new("Contract.CurrencyMismatch", "Amount must be in the currency of the contract.", ErrorType.Validation);

        /// <summary>The payment or refund amount is zero or negative.</summary>
        public static readonly Error AmountNotPositive = new("Contract.AmountNotPositive", "Amount must be greater than zero.", ErrorType.Validation);

        /// <summary>The refund is larger than the amount paid so far.</summary>
        public static readonly Error RefundExceedsPaidAmount = new("Contract.RefundExceedsPaidAmount", "Refund cannot exceed the amount paid.", ErrorType.Conflict);

        /// <summary>The lesson length is not positive, exceeds the maximum, or has more than two decimals.</summary>
        public static readonly Error LessonHoursOutOfRange = new("Contract.LessonHoursOutOfRange", "Lesson hours must be greater than 0 and at most 12, with at most two decimals.", ErrorType.Validation);

        /// <summary>The grade is outside 1..5 or has more than two decimals.</summary>
        public static readonly Error GradeOutOfRange = new("Contract.GradeOutOfRange", "Grade must be between 1 and 5, with at most two decimals.", ErrorType.Validation);

        /// <summary>There is no recorded theory attendance to take back.</summary>
        public static readonly Error NoTheoryAttendance = new("Contract.NoTheoryAttendance", "No theory attendance has been recorded.", ErrorType.Conflict);

        /// <summary>No reason was given for the termination.</summary>
        public static readonly Error TerminationReasonEmpty = new("Contract.TerminationReasonEmpty", "A reason is required to terminate a contract.", ErrorType.Validation);

        /// <summary>The termination reason exceeds the maximum allowed length.</summary>
        public static readonly Error TerminationReasonTooLong = new("Contract.TerminationReasonTooLong", "Termination reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The completion or termination date is earlier than the start date.</summary>
        public static readonly Error ClosedBeforeStart = new("Contract.ClosedBeforeStart", "A contract cannot be closed before its start date.", ErrorType.Validation);

        /// <summary>The contract cannot be completed while money is owed.</summary>
        public static readonly Error OutstandingDebt = new("Contract.OutstandingDebt", "A contract with an outstanding debt cannot be completed.", ErrorType.Conflict);

        /// <summary>The operation requires an active contract.</summary>
        public static readonly Error NotActive = new("Contract.NotActive", "Only an active contract can be changed.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.Course"/> aggregate.</summary>
    public static class Course
    {
        /// <summary>The course name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("Course.NameEmpty", "Course name cannot be empty.", ErrorType.Validation);

        /// <summary>The course name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("Course.NameTooLong", "Course name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The category is not a defined <see cref="Enums.LicenseCategory"/> value.</summary>
        public static readonly Error CategoryNotDefined = new("Course.CategoryNotDefined", "Category is not a valid licence category.", ErrorType.Validation);

        /// <summary>The price is zero or negative.</summary>
        public static readonly Error PriceNotPositive = new("Course.PriceNotPositive", "Course price must be greater than zero.", ErrorType.Validation);

        /// <summary>Theory or practice hours are negative or above the maximum.</summary>
        public static readonly Error HoursOutOfRange = new("Course.HoursOutOfRange", "Theory and practice hours must be between 0 and 1000.", ErrorType.Validation);

        /// <summary>Both theory and practice hours are zero.</summary>
        public static readonly Error NoHours = new("Course.NoHours", "A course must have theory or practice hours.", ErrorType.Validation);

        /// <summary>The description exceeds the maximum allowed length.</summary>
        public static readonly Error DescriptionTooLong = new("Course.DescriptionTooLong", "Description exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The topic does not belong to the course.</summary>
        public static readonly Error TopicNotFound = new("Course.TopicNotFound", "Topic was not found in the course.", ErrorType.NotFound);

        /// <summary>The target position is outside 1..number of topics.</summary>
        public static readonly Error TopicPositionOutOfRange = new("Course.TopicPositionOutOfRange", "Topic position is out of range.", ErrorType.Validation);

        /// <summary>Another topic of the course has the same title, ignoring letter case.</summary>
        public static readonly Error DuplicateTopicTitle = new("Course.DuplicateTopicTitle", "The course already has a topic with this title.", ErrorType.Conflict);

        /// <summary>The course is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("Course.AlreadyActive", "Course is already active.", ErrorType.Conflict);

        /// <summary>The course is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("Course.AlreadyInactive", "Course is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive course, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("Course.CannotModifyInactive", "An inactive course cannot be modified. Reactivate the course first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="Entities.CourseTopic"/>.</summary>
    public static class CourseTopic
    {
        /// <summary>The topic title is null, empty, or whitespace.</summary>
        public static readonly Error TitleEmpty = new("CourseTopic.TitleEmpty", "Topic title cannot be empty.", ErrorType.Validation);

        /// <summary>The topic title exceeds the maximum allowed length.</summary>
        public static readonly Error TitleTooLong = new("CourseTopic.TitleTooLong", "Topic title exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The topic description exceeds the maximum allowed length.</summary>
        public static readonly Error DescriptionTooLong = new("CourseTopic.DescriptionTooLong", "Topic description exceeds the maximum allowed length.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.Currency"/> validation.</summary>
    public static class Currency
    {
        /// <summary>The currency code is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("Currency.Empty", "Currency code cannot be empty.", ErrorType.Validation);

        /// <summary>The currency code is not three Latin letters.</summary>
        public static readonly Error InvalidFormat = new("Currency.InvalidFormat", "Currency code must be a three-letter ISO 4217 code, e.g. RUB.", ErrorType.Validation);

        /// <summary>The currency code is well formed but not in the list of supported currencies.</summary>
        public static readonly Error NotSupported = new("Currency.NotSupported", "Currency is not supported.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.Money"/> validation.</summary>
    public static class Money
    {
        /// <summary>The amount has more decimal places than its currency allows.</summary>
        public static readonly Error TooManyDecimalPlaces = new("Money.TooManyDecimalPlaces", "Amount has more decimal places than the currency allows.", ErrorType.Validation);

        /// <summary>The absolute amount exceeds the maximum.</summary>
        public static readonly Error TooLarge = new("Money.TooLarge", "Amount exceeds the maximum allowed value.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.Question"/> aggregate.</summary>
    public static class Question
    {
        /// <summary>The question text is null, empty, or whitespace.</summary>
        public static readonly Error TextEmpty = new("Question.TextEmpty", "Question text cannot be empty.", ErrorType.Validation);

        /// <summary>The question text exceeds the maximum allowed length.</summary>
        public static readonly Error TextTooLong = new("Question.TextTooLong", "Question text exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The explanation exceeds the maximum allowed length.</summary>
        public static readonly Error ExplanationTooLong = new("Question.ExplanationTooLong", "Explanation exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The image key exceeds the maximum allowed length.</summary>
        public static readonly Error ImageKeyTooLong = new("Question.ImageKeyTooLong", "Image key exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The topic identifier is an empty GUID.</summary>
        public static readonly Error TopicIdEmpty = new("Question.TopicIdEmpty", "Topic ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>No licence category was given.</summary>
        public static readonly Error NoCategories = new("Question.NoCategories", "A question must apply to at least one licence category.", ErrorType.Validation);

        /// <summary>A category is not a defined <see cref="Enums.LicenseCategory"/> value.</summary>
        public static readonly Error CategoryNotDefined = new("Question.CategoryNotDefined", "Category is not a valid licence category.", ErrorType.Validation);

        /// <summary>The same category is listed more than once.</summary>
        public static readonly Error DuplicateCategory = new("Question.DuplicateCategory", "Each category can be listed only once.", ErrorType.Validation);

        /// <summary>The source is not a defined <see cref="Enums.QuestionSource"/> value.</summary>
        public static readonly Error SourceNotDefined = new("Question.SourceNotDefined", "Question source is not valid.", ErrorType.Validation);

        /// <summary>The external code exceeds the maximum allowed length.</summary>
        public static readonly Error ExternalCodeTooLong = new("Question.ExternalCodeTooLong", "External code exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>Fewer than two answer options were given.</summary>
        public static readonly Error TooFewOptions = new("Question.TooFewOptions", "A question must have at least 2 answer options.", ErrorType.Validation);

        /// <summary>More than six answer options were given.</summary>
        public static readonly Error TooManyOptions = new("Question.TooManyOptions", "A question can have at most 6 answer options.", ErrorType.Validation);

        /// <summary>The text of an answer option is null, empty, or whitespace.</summary>
        public static readonly Error OptionTextEmpty = new("Question.OptionTextEmpty", "Answer option text cannot be empty.", ErrorType.Validation);

        /// <summary>The text of an answer option exceeds the maximum allowed length.</summary>
        public static readonly Error OptionTextTooLong = new("Question.OptionTextTooLong", "Answer option text exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>Two answer options have the same text, ignoring letter case.</summary>
        public static readonly Error DuplicateOptionText = new("Question.DuplicateOptionText", "Answer options must have different texts.", ErrorType.Validation);

        /// <summary>None or several answer options are marked correct.</summary>
        public static readonly Error NotExactlyOneCorrectOption = new("Question.NotExactlyOneCorrectOption", "Exactly one answer option must be correct.", ErrorType.Validation);

        /// <summary>The question is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("Question.AlreadyActive", "Question is already active.", ErrorType.Conflict);

        /// <summary>The question is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("Question.AlreadyInactive", "Question is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive question, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("Question.CannotModifyInactive", "An inactive question cannot be modified. Reactivate it first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.DrivingRoute"/> aggregate.</summary>
    public static class DrivingRoute
    {
        /// <summary>The name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("DrivingRoute.NameEmpty", "Route name cannot be empty.", ErrorType.Validation);

        /// <summary>The name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("DrivingRoute.NameTooLong", "Route name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The description is null, empty, or whitespace.</summary>
        public static readonly Error DescriptionEmpty = new("DrivingRoute.DescriptionEmpty", "Route description cannot be empty.", ErrorType.Validation);

        /// <summary>The description exceeds the maximum allowed length.</summary>
        public static readonly Error DescriptionTooLong = new("DrivingRoute.DescriptionTooLong", "Route description exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The author identifier is an empty GUID.</summary>
        public static readonly Error AuthorIdEmpty = new("DrivingRoute.AuthorIdEmpty", "Author ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The training ground identifier was provided but is an empty GUID.</summary>
        public static readonly Error TrainingGroundIdEmpty = new("DrivingRoute.TrainingGroundIdEmpty", "Training ground ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The route is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("DrivingRoute.AlreadyActive", "Route is already active.", ErrorType.Conflict);

        /// <summary>The route is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("DrivingRoute.AlreadyInactive", "Route is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive route, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("DrivingRoute.CannotModifyInactive", "An inactive route cannot be modified. Reactivate it first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="ValueObjects.SkillGrade"/> validation.</summary>
    public static class SkillGrade
    {
        /// <summary>The skill is not a defined <see cref="Enums.DrivingSkill"/> value.</summary>
        public static readonly Error SkillNotDefined = new("SkillGrade.SkillNotDefined", "Skill is not valid.", ErrorType.Validation);

        /// <summary>The grade is outside 1..5.</summary>
        public static readonly Error GradeOutOfRange = new("SkillGrade.GradeOutOfRange", "Skill grade must be between 1 and 5.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.StudyGroup"/> aggregate.</summary>
    public static class StudyGroup
    {
        /// <summary>The group name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("StudyGroup.NameEmpty", "Group name cannot be empty.", ErrorType.Validation);

        /// <summary>The group name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("StudyGroup.NameTooLong", "Group name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The course is no longer offered.</summary>
        public static readonly Error CourseInactive = new("StudyGroup.CourseInactive", "A group cannot be formed for an inactive course.", ErrorType.Conflict);

        /// <summary>The theory instructor identifier is an empty GUID.</summary>
        public static readonly Error InstructorIdEmpty = new("StudyGroup.InstructorIdEmpty", "Theory instructor ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("StudyGroup.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The end date is equal to or earlier than the start date.</summary>
        public static readonly Error EndNotAfterStart = new("StudyGroup.EndNotAfterStart", "End date must be later than start date.", ErrorType.Validation);

        /// <summary>The capacity is below 1 or above the maximum.</summary>
        public static readonly Error CapacityOutOfRange = new("StudyGroup.CapacityOutOfRange", "Capacity must be between 1 and 100.", ErrorType.Validation);

        /// <summary>The capacity is lower than the number of current members.</summary>
        public static readonly Error CapacityBelowMemberCount = new("StudyGroup.CapacityBelowMemberCount", "Capacity cannot be lower than the number of current members.", ErrorType.Conflict);

        /// <summary>Only an active contract can be enrolled.</summary>
        public static readonly Error ContractNotActive = new("StudyGroup.ContractNotActive", "Only an active contract can be enrolled in a group.", ErrorType.Conflict);

        /// <summary>The contract is for a different course than the group.</summary>
        public static readonly Error CourseMismatch = new("StudyGroup.CourseMismatch", "The contract is for a different course than the group.", ErrorType.Conflict);

        /// <summary>The contract is already a current member of the group.</summary>
        public static readonly Error AlreadyEnrolled = new("StudyGroup.AlreadyEnrolled", "The student is already a member of the group.", ErrorType.Conflict);

        /// <summary>The group has no free places.</summary>
        public static readonly Error GroupFull = new("StudyGroup.GroupFull", "The group is full.", ErrorType.Conflict);

        /// <summary>The contract is not a current member of the group.</summary>
        public static readonly Error NotEnrolled = new("StudyGroup.NotEnrolled", "The student is not a member of the group.", ErrorType.NotFound);

        /// <summary>The group is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("StudyGroup.AlreadyActive", "Group is already active.", ErrorType.Conflict);

        /// <summary>The group is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("StudyGroup.AlreadyInactive", "Group is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive group, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("StudyGroup.CannotModifyInactive", "An inactive group cannot be modified. Reactivate the group first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="ValueObjects.TestBlock"/> validation.</summary>
    public static class TestBlock
    {
        /// <summary>The question count is below 1 or above 100.</summary>
        public static readonly Error QuestionCountOutOfRange = new("TestBlock.QuestionCountOutOfRange", "A block must draw between 1 and 100 questions.", ErrorType.Validation);

        /// <summary>A topic identifier is an empty GUID.</summary>
        public static readonly Error TopicIdEmpty = new("TestBlock.TopicIdEmpty", "Topic ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The same topic is listed more than once.</summary>
        public static readonly Error DuplicateTopic = new("TestBlock.DuplicateTopic", "Each topic can be listed only once.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.TestRules"/> validation.</summary>
    public static class TestRules
    {
        /// <summary>The question count is below 1 or above 100.</summary>
        public static readonly Error QuestionCountOutOfRange = new("TestRules.QuestionCountOutOfRange", "Question count must be between 1 and 100.", ErrorType.Validation);

        /// <summary>The time limit is below 1 minute or above 3 hours.</summary>
        public static readonly Error TimeLimitOutOfRange = new("TestRules.TimeLimitOutOfRange", "Time limit must be between 1 minute and 3 hours.", ErrorType.Validation);

        /// <summary>The allowed mistakes are negative or not fewer than the questions.</summary>
        public static readonly Error MaxMistakesOutOfRange = new("TestRules.MaxMistakesOutOfRange", "Allowed mistakes must be at least 0 and fewer than the questions.", ErrorType.Validation);

        /// <summary>The mistakes allowed per block are negative or more than the mistakes allowed in total.</summary>
        public static readonly Error MaxMistakesPerBlockOutOfRange = new("TestRules.MaxMistakesPerBlockOutOfRange", "Mistakes allowed per block must be between 0 and the mistakes allowed in total.", ErrorType.Validation);

        /// <summary>The questions added per mistake are negative or above 20.</summary>
        public static readonly Error ExtraQuestionsOutOfRange = new("TestRules.ExtraQuestionsOutOfRange", "Questions added per mistake must be between 0 and 20.", ErrorType.Validation);

        /// <summary>The time added per mistake is negative or above 30 minutes.</summary>
        public static readonly Error ExtraTimeOutOfRange = new("TestRules.ExtraTimeOutOfRange", "Time added per mistake must be between 0 and 30 minutes.", ErrorType.Validation);

        /// <summary>Time is added per mistake although no questions are added.</summary>
        public static readonly Error ExtraTimeWithoutExtraQuestions = new("TestRules.ExtraTimeWithoutExtraQuestions", "Time can be added per mistake only together with added questions.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.TestSession"/> aggregate.</summary>
    public static class TestSession
    {
        /// <summary>The template is no longer used.</summary>
        public static readonly Error TemplateInactive = new("TestSession.TemplateInactive", "A test cannot be started from an inactive template.", ErrorType.Conflict);

        /// <summary>Only an active contract can start a test.</summary>
        public static readonly Error ContractNotActive = new("TestSession.ContractNotActive", "Only an active contract can start a test.", ErrorType.Conflict);

        /// <summary>The contract's licence category differs from the template's.</summary>
        public static readonly Error CategoryMismatch = new("TestSession.CategoryMismatch", "The template is for a different licence category than the contract.", ErrorType.Conflict);

        /// <summary>The exam identifier was provided but is an empty GUID.</summary>
        public static readonly Error ExamIdEmpty = new("TestSession.ExamIdEmpty", "Exam ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>Questions were given for a different number of blocks than the template has.</summary>
        public static readonly Error BlockCountMismatch = new("TestSession.BlockCountMismatch", "Questions must be given for every block of the template.", ErrorType.Validation);

        /// <summary>A block received a different number of questions than it draws.</summary>
        public static readonly Error BlockQuestionCountMismatch = new("TestSession.BlockQuestionCountMismatch", "Each block must receive exactly its number of questions.", ErrorType.Validation);

        /// <summary>A different number of added questions was given than the rules require.</summary>
        public static readonly Error AddOnCountMismatch = new("TestSession.AddOnCountMismatch", "Exactly the number of added questions in the rules must be given.", ErrorType.Validation);

        /// <summary>A question is taken out of use.</summary>
        public static readonly Error QuestionInactive = new("TestSession.QuestionInactive", "Inactive questions cannot be used in a test.", ErrorType.Validation);

        /// <summary>A question does not apply to the test's licence category.</summary>
        public static readonly Error QuestionCategoryMismatch = new("TestSession.QuestionCategoryMismatch", "A question does not apply to the licence category of the test.", ErrorType.Validation);

        /// <summary>A question's topic is not one of its block's topics.</summary>
        public static readonly Error QuestionOutsideBlock = new("TestSession.QuestionOutsideBlock", "A question does not belong to its block.", ErrorType.Validation);

        /// <summary>A question is repeated within the attempt.</summary>
        public static readonly Error DuplicateQuestion = new("TestSession.DuplicateQuestion", "A question cannot appear twice in one attempt.", ErrorType.Validation);

        /// <summary>The attempt is already finished.</summary>
        public static readonly Error NotInProgress = new("TestSession.NotInProgress", "The attempt is already finished.", ErrorType.Conflict);

        /// <summary>The deadline has passed.</summary>
        public static readonly Error TimeExpired = new("TestSession.TimeExpired", "The time for the attempt has run out.", ErrorType.Conflict);

        /// <summary>The deadline has not passed yet.</summary>
        public static readonly Error TimeNotExpired = new("TestSession.TimeNotExpired", "The attempt still has time.", ErrorType.Conflict);

        /// <summary>The question is not part of this attempt.</summary>
        public static readonly Error QuestionNotInSession = new("TestSession.QuestionNotInSession", "The question is not part of this attempt.", ErrorType.NotFound);

        /// <summary>The question has already been answered.</summary>
        public static readonly Error AlreadyAnswered = new("TestSession.AlreadyAnswered", "The question has already been answered.", ErrorType.Conflict);

        /// <summary>The option is not one of the question's options.</summary>
        public static readonly Error OptionNotInQuestion = new("TestSession.OptionNotInQuestion", "The option does not belong to the question.", ErrorType.Validation);

        /// <summary>No added questions are expected for the block.</summary>
        public static readonly Error AddOnNotPending = new("TestSession.AddOnNotPending", "No added questions are expected for this block.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.TestTemplate"/> aggregate.</summary>
    public static class TestTemplate
    {
        /// <summary>The name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("TestTemplate.NameEmpty", "Template name cannot be empty.", ErrorType.Validation);

        /// <summary>The name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("TestTemplate.NameTooLong", "Template name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The category is not a defined <see cref="Enums.LicenseCategory"/> value.</summary>
        public static readonly Error CategoryNotDefined = new("TestTemplate.CategoryNotDefined", "Category is not a valid licence category.", ErrorType.Validation);

        /// <summary>No block was given.</summary>
        public static readonly Error NoBlocks = new("TestTemplate.NoBlocks", "A template must have at least one block.", ErrorType.Validation);

        /// <summary>More blocks than allowed were given.</summary>
        public static readonly Error TooManyBlocks = new("TestTemplate.TooManyBlocks", "A template can have at most 10 blocks.", ErrorType.Validation);

        /// <summary>A block covering all topics is combined with other blocks.</summary>
        public static readonly Error AllTopicsBlockNotAlone = new("TestTemplate.AllTopicsBlockNotAlone", "A block covering all topics must be the only block.", ErrorType.Validation);

        /// <summary>The same topic belongs to more than one block.</summary>
        public static readonly Error TopicInSeveralBlocks = new("TestTemplate.TopicInSeveralBlocks", "A topic can belong to only one block.", ErrorType.Validation);

        /// <summary>The blocks together draw a different number of questions than the rules require.</summary>
        public static readonly Error BlocksDoNotMatchQuestionCount = new("TestTemplate.BlocksDoNotMatchQuestionCount", "The blocks must draw exactly the number of questions in the rules.", ErrorType.Validation);

        /// <summary>The source is not a defined <see cref="Enums.QuestionSource"/> value.</summary>
        public static readonly Error SourceNotDefined = new("TestTemplate.SourceNotDefined", "Question source is not valid.", ErrorType.Validation);

        /// <summary>The template is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("TestTemplate.AlreadyActive", "Template is already active.", ErrorType.Conflict);

        /// <summary>The template is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("TestTemplate.AlreadyInactive", "Template is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive template, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("TestTemplate.CannotModifyInactive", "An inactive template cannot be modified. Reactivate it first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.TheoryLesson"/> aggregate.</summary>
    public static class TheoryLesson
    {
        /// <summary>The study group is inactive.</summary>
        public static readonly Error GroupInactive = new("TheoryLesson.GroupInactive", "Lessons cannot be scheduled for an inactive group.", ErrorType.Conflict);

        /// <summary>The course passed in is not the group's course.</summary>
        public static readonly Error CourseMismatch = new("TheoryLesson.CourseMismatch", "The course does not belong to the group.", ErrorType.Validation);

        /// <summary>The topic is not part of the group's course.</summary>
        public static readonly Error TopicNotInCourse = new("TheoryLesson.TopicNotInCourse", "The topic does not belong to the group's course.", ErrorType.Validation);

        /// <summary>The instructor identifier is an empty GUID.</summary>
        public static readonly Error InstructorIdEmpty = new("TheoryLesson.InstructorIdEmpty", "Instructor ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The lesson would start at or before the current time.</summary>
        public static readonly Error StartInPast = new("TheoryLesson.StartInPast", "A lesson must start in the future.", ErrorType.Validation);

        /// <summary>The lesson is longer than the maximum duration.</summary>
        public static readonly Error DurationTooLong = new("TheoryLesson.DurationTooLong", "A lesson cannot be longer than 8 hours.", ErrorType.Validation);

        /// <summary>No reason was given for the cancellation.</summary>
        public static readonly Error CancellationReasonEmpty = new("TheoryLesson.CancellationReasonEmpty", "A reason is required to cancel a lesson.", ErrorType.Validation);

        /// <summary>The cancellation reason exceeds the maximum allowed length.</summary>
        public static readonly Error CancellationReasonTooLong = new("TheoryLesson.CancellationReasonTooLong", "Cancellation reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The operation requires a scheduled lesson.</summary>
        public static readonly Error NotScheduled = new("TheoryLesson.NotScheduled", "Only a scheduled lesson can be changed or cancelled.", ErrorType.Conflict);

        /// <summary>Attendance has been marked, so the lesson took place.</summary>
        public static readonly Error AttendanceAlreadyMarked = new("TheoryLesson.AttendanceAlreadyMarked", "The lesson already has attendance marks.", ErrorType.Conflict);

        /// <summary>Attendance cannot be marked for a cancelled lesson.</summary>
        public static readonly Error LessonCancelled = new("TheoryLesson.LessonCancelled", "The lesson was cancelled.", ErrorType.Conflict);

        /// <summary>Attendance can be marked only once the lesson has started.</summary>
        public static readonly Error NotStarted = new("TheoryLesson.NotStarted", "The lesson has not started yet.", ErrorType.Conflict);

        /// <summary>The group passed in is not the lesson's group.</summary>
        public static readonly Error GroupMismatch = new("TheoryLesson.GroupMismatch", "The group does not belong to the lesson.", ErrorType.Validation);

        /// <summary>The contract is not a current member of the lesson's group.</summary>
        public static readonly Error NotGroupMember = new("TheoryLesson.NotGroupMember", "The student is not a member of the group.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="ValueObjects.TimeInterval"/> validation.</summary>
    public static class TimeInterval
    {
        /// <summary>The start or the end is not a UTC time.</summary>
        public static readonly Error NotUtc = new("TimeInterval.NotUtc", "Start and end must be UTC times.", ErrorType.Validation);

        /// <summary>The end is equal to or earlier than the start.</summary>
        public static readonly Error EndNotAfterStart = new("TimeInterval.EndNotAfterStart", "End must be later than start.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.Branch"/> entity.</summary>
    public static class Branch
    {
        /// <summary>The branch name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("Branch.NameEmpty", "Branch name cannot be empty.", ErrorType.Validation);

        /// <summary>The branch name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("Branch.NameTooLong", "Branch name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The time zone identifier is null, empty, or whitespace.</summary>
        public static readonly Error TimeZoneEmpty = new("Branch.TimeZoneEmpty", "Time zone cannot be empty.", ErrorType.Validation);

        /// <summary>The time zone identifier is not a known IANA identifier.</summary>
        public static readonly Error TimeZoneUnknown = new("Branch.TimeZoneUnknown", "Time zone must be a known IANA identifier, e.g. Europe/Moscow.", ErrorType.Validation);

        /// <summary>The branch is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("Branch.AlreadyActive", "Branch is already active.", ErrorType.Conflict);

        /// <summary>The branch is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("Branch.AlreadyInactive", "Branch is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive branch, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("Branch.CannotModifyInactive", "An inactive branch cannot be modified. Reactivate the branch first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.Lead"/> aggregate.</summary>
    public static class Lead
    {
        /// <summary>The contact name is null, empty, or whitespace.</summary>
        public static readonly Error ContactNameEmpty = new("Lead.ContactNameEmpty", "Contact name cannot be empty.", ErrorType.Validation);

        /// <summary>The contact name exceeds the maximum allowed length.</summary>
        public static readonly Error ContactNameTooLong = new("Lead.ContactNameTooLong", "Contact name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>Neither a phone number nor an email address was given.</summary>
        public static readonly Error ContactRequired = new("Lead.ContactRequired", "A phone number or an email address is required.", ErrorType.Validation);

        /// <summary>The source is not a defined <see cref="Enums.LeadSource"/> value.</summary>
        public static readonly Error SourceNotDefined = new("Lead.SourceNotDefined", "Source is not a valid lead source.", ErrorType.Validation);

        /// <summary>The desired category is not a defined <see cref="Enums.LicenseCategory"/> value.</summary>
        public static readonly Error CategoryNotDefined = new("Lead.CategoryNotDefined", "Desired category is not a valid licence category.", ErrorType.Validation);

        /// <summary>The comment exceeds the maximum allowed length.</summary>
        public static readonly Error CommentTooLong = new("Lead.CommentTooLong", "Comment exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("Lead.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The manager identifier is an empty GUID.</summary>
        public static readonly Error ManagerIdEmpty = new("Lead.ManagerIdEmpty", "Manager ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The contract identifier is an empty GUID.</summary>
        public static readonly Error ContractIdEmpty = new("Lead.ContractIdEmpty", "Contract ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The identifier of the user making the change was provided but is an empty GUID.</summary>
        public static readonly Error UserIdEmpty = new("Lead.UserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>No reason was given for losing the lead.</summary>
        public static readonly Error LostReasonEmpty = new("Lead.LostReasonEmpty", "A reason is required when a lead is lost.", ErrorType.Validation);

        /// <summary>The lost reason exceeds the maximum allowed length.</summary>
        public static readonly Error LostReasonTooLong = new("Lead.LostReasonTooLong", "Lost reason exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The manager is already responsible for the lead.</summary>
        public static readonly Error ManagerAlreadyAssigned = new("Lead.ManagerAlreadyAssigned", "This manager is already assigned to the lead.", ErrorType.Conflict);

        /// <summary>The operation requires a new or in-progress lead.</summary>
        public static readonly Error NotOpen = new("Lead.NotOpen", "Only a new or in-progress lead can be changed.", ErrorType.Conflict);

        /// <summary>Work can be started only on a new lead.</summary>
        public static readonly Error NotNew = new("Lead.NotNew", "Only a new lead can be taken into work.", ErrorType.Conflict);

        /// <summary>Only a lost lead can be reopened.</summary>
        public static readonly Error NotLost = new("Lead.NotLost", "Only a lost lead can be reopened.", ErrorType.Conflict);
    }

    /// <summary>Errors related to <see cref="Entities.LeadNote"/>.</summary>
    public static class LeadNote
    {
        /// <summary>The note text is null, empty, or whitespace.</summary>
        public static readonly Error TextEmpty = new("LeadNote.TextEmpty", "Note text cannot be empty.", ErrorType.Validation);

        /// <summary>The note text exceeds the maximum allowed length.</summary>
        public static readonly Error TextTooLong = new("LeadNote.TextTooLong", "Note text exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The author identifier is an empty GUID.</summary>
        public static readonly Error AuthorIdEmpty = new("LeadNote.AuthorIdEmpty", "Author ID cannot be an empty GUID.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.TrainingGround"/> aggregate.</summary>
    public static class TrainingGround
    {
        /// <summary>The name is null, empty, or whitespace.</summary>
        public static readonly Error NameEmpty = new("TrainingGround.NameEmpty", "Training ground name cannot be empty.", ErrorType.Validation);

        /// <summary>The name exceeds the maximum allowed length.</summary>
        public static readonly Error NameTooLong = new("TrainingGround.NameTooLong", "Training ground name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The description exceeds the maximum allowed length.</summary>
        public static readonly Error DescriptionTooLong = new("TrainingGround.DescriptionTooLong", "Description exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("TrainingGround.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The training ground is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("TrainingGround.AlreadyActive", "Training ground is already active.", ErrorType.Conflict);

        /// <summary>The training ground is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("TrainingGround.AlreadyInactive", "Training ground is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive training ground, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("TrainingGround.CannotModifyInactive", "An inactive training ground cannot be modified. Reactivate it first.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.User"/> entity.</summary>
    public static class User
    {
        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("User.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The photo URL is null, empty, or whitespace.</summary>
        public static readonly Error PhotoUrlEmpty = new("User.PhotoUrlEmpty", "Photo URL cannot be empty.", ErrorType.Validation);

        /// <summary>The photo URL exceeds the maximum allowed length.</summary>
        public static readonly Error PhotoUrlTooLong = new("User.PhotoUrlTooLong", "Photo URL exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The user is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("User.AlreadyActive", "User is already active.", ErrorType.Conflict);

        /// <summary>The user is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("User.AlreadyInactive", "User is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive user, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("User.CannotModifyInactive", "An inactive user cannot be modified. Reactivate the user first.", ErrorType.Conflict);

        /// <summary>The new email address is the same as the current one.</summary>
        public static readonly Error EmailUnchanged = new("User.EmailUnchanged", "The new email address is the same as the current one.", ErrorType.Conflict);

        /// <summary>The user has no profile photo to remove.</summary>
        public static readonly Error PhotoNotSet = new("User.PhotoNotSet", "User has no profile photo.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.UserRoleAssignment"/> entity.</summary>
    public static class UserRoleAssignment
    {
        /// <summary>The user identifier is an empty GUID.</summary>
        public static readonly Error UserIdEmpty = new("UserRoleAssignment.UserIdEmpty", "User ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The role is not a defined <see cref="Enums.UserRole"/> value.</summary>
        public static readonly Error RoleNotDefined = new("UserRoleAssignment.RoleNotDefined", "Role is not a valid user role.", ErrorType.Validation);

        /// <summary>The role is granted to an inactive user.</summary>
        public static readonly Error UserInactive = new("UserRoleAssignment.UserInactive", "A role cannot be granted to an inactive user.", ErrorType.Conflict);

        /// <summary>The user already holds the role.</summary>
        public static readonly Error AlreadyAssigned = new("UserRoleAssignment.AlreadyAssigned", "User already holds this role.", ErrorType.Conflict);

        /// <summary>The user does not hold the role being taken away.</summary>
        public static readonly Error NotAssigned = new("UserRoleAssignment.NotAssigned", "User does not hold this role.", ErrorType.NotFound);

        /// <summary>The role cannot be combined with a role the user already holds.</summary>
        public static readonly Error IncompatibleRoles = new("UserRoleAssignment.IncompatibleRoles", "A student cannot hold an instructor role at the same time.", ErrorType.Conflict);

        /// <summary>The role being taken away belongs to the last active administrator.</summary>
        public static readonly Error LastAdministrator = new("UserRoleAssignment.LastAdministrator", "The school must keep at least one active administrator.", ErrorType.Conflict);
    }

    /// <summary>Errors related to the <see cref="Entities.Vehicle"/> aggregate.</summary>
    public static class Vehicle
    {
        /// <summary>The make is null, empty, or whitespace.</summary>
        public static readonly Error MakeEmpty = new("Vehicle.MakeEmpty", "Make cannot be empty.", ErrorType.Validation);

        /// <summary>The make exceeds the maximum allowed length.</summary>
        public static readonly Error MakeTooLong = new("Vehicle.MakeTooLong", "Make exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The model exceeds the maximum allowed length.</summary>
        public static readonly Error ModelTooLong = new("Vehicle.ModelTooLong", "Model exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The model year is before 1950 or after next year.</summary>
        public static readonly Error YearOutOfRange = new("Vehicle.YearOutOfRange", "Model year is out of range.", ErrorType.Validation);

        /// <summary>The category is not a defined <see cref="Enums.LicenseCategory"/> value.</summary>
        public static readonly Error CategoryNotDefined = new("Vehicle.CategoryNotDefined", "Category is not a valid licence category.", ErrorType.Validation);

        /// <summary>The transmission is not a defined <see cref="Enums.Transmission"/> value.</summary>
        public static readonly Error TransmissionNotDefined = new("Vehicle.TransmissionNotDefined", "Transmission is not valid.", ErrorType.Validation);

        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("Vehicle.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The vehicle is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("Vehicle.AlreadyActive", "Vehicle is already active.", ErrorType.Conflict);

        /// <summary>The vehicle is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("Vehicle.AlreadyInactive", "Vehicle is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive vehicle, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("Vehicle.CannotModifyInactive", "An inactive vehicle cannot be modified. Reactivate it first.", ErrorType.Conflict);
    }
}
