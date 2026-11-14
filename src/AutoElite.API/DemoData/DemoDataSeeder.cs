using AutoElite.API.Authentication;
using AutoElite.Application.Branches.CreateBranch;
using AutoElite.Application.Contracts.DrawUpContract;
using AutoElite.Application.Courses.AddCourseTopic;
using AutoElite.Application.Courses.CreateCourse;
using AutoElite.Application.DrivingSlots.BookDrivingSlot;
using AutoElite.Application.DrivingSlots.OfferDrivingSlot;
using AutoElite.Application.Payments.RegisterPayment;
using AutoElite.Application.Questions.CreateQuestion;
using AutoElite.Application.StudyGroups.CreateStudyGroup;
using AutoElite.Application.StudyGroups.EnrollInStudyGroup;
using AutoElite.Application.TestTemplates;
using AutoElite.Application.TestTemplates.CreateTestTemplate;
using AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;
using AutoElite.Application.TrainingGrounds.CreateTrainingGround;
using AutoElite.Application.Users;
using AutoElite.Application.Users.AcceptInvitation;
using AutoElite.Application.Vehicles.RegisterVehicle;
using AutoElite.Domain.Entities;
using AutoElite.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace AutoElite.API.DemoData;

/// <summary>
/// Creates the demo data when the host starts, once, in a school that does not have it yet. Each step is a command of
/// the application, sent on behalf of the demo user who would take it, such as the theory instructor who schedules
/// their lessons, so that the data passes the same rules and has the same side effects as data entered by hand; only
/// the users are registered directly, as an invitation would, with the password of the options. A failure is logged
/// and the host starts anyway; a new database starts again.
/// </summary>
internal sealed partial class DemoDataSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoDataOptions> options,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger) : IHostedService
{
    /// <summary>The domain of the addresses of the demo users, reserved for examples, so that no email reaches anybody.</summary>
    public const string EmailDomain = "autoelite.example";

    private const string TimeZoneId = "Asia/Yekaterinburg";

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Password is not { Length: > 0 } password)
        {
            LogNoPassword(logger);

            return;
        }

        try
        {
            if (await HasDemoDataAsync(cancellationToken))
                return;

            await CreateAsync(password, cancellationToken);
            LogCreated(logger, EmailDomain);
        }
        catch (InvalidOperationException exception)
        {
            LogFailed(logger, exception.Message);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Email EmailOf(string name) => Email.Create($"{name}@{EmailDomain}").Value;

    // The local time of the branch on a day, in UTC.
    private static DateTime UtcOf(DateOnly day, int hour, int minute, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(new TimeOnly(hour, minute)), zone);

    private async Task<bool> HasDemoDataAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        return await users.EmailExistsAsync(EmailOf("admin"), cancellationToken);
    }

    private async Task CreateAsync(string password, CancellationToken cancellationToken)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime);

        // The administrator first, who sets up the school.
        var administrator = await RegisterAsync(
            "admin", "Иванова", "Ольга", "Сергеевна", null, UserRole.Administrator, null, password, cancellationToken);
        var branchId = await SendAsync(administrator, new CreateBranchCommand(
            "Центральный", "Екатеринбург", "ул. Ленина, 1", "+73432000000", TimeZoneId), cancellationToken);
        var groundId = await SendAsync(
            administrator,
            new CreateTrainingGroundCommand(
                "Автодром на Сибирском тракте", "Екатеринбург", "Сибирский тракт, 10", "Площадка с эстакадой и разметкой", branchId),
            cancellationToken);
        var manualId = await SendAsync(administrator, new RegisterVehicleCommand(
            "А101АА196", "Lada", "Vesta", 2023, LicenseCategory.B, Transmission.Manual, branchId), cancellationToken);
        var automaticId = await SendAsync(administrator, new RegisterVehicleCommand(
            "В202ВВ196", "Kia", "Rio", 2022, LicenseCategory.B, Transmission.Automatic, branchId), cancellationToken);
        var courseId = await SendAsync(
            administrator,
            new CreateCourseCommand(
                "Категория B", LicenseCategory.B, 45000m, null, 134, 56, "Обучение вождению легкового автомобиля с нуля"),
            cancellationToken);
        var topicIds = new List<Guid>();
        foreach (var topic in DemoQuestions.Topics)
            topicIds.Add(await SendAsync(administrator, new AddCourseTopicCommand(courseId, topic.Title, null), cancellationToken));

        // The staff and the students of the branch.
        Task<DemoCaller> Register(string name, string lastName, string firstName, string? middleName, string phone, UserRole role) =>
            RegisterAsync(name, lastName, firstName, middleName, phone, role, branchId, password, cancellationToken);

        var manager = await Register("manager", "Петрова", "Анна", "Викторовна", "+79990000001", UserRole.Manager);
        var theoryInstructor = await Register("theory", "Сидоров", "Михаил", "Игоревич", "+79990000002", UserRole.TheoryInstructor);
        var drivingInstructor = await Register("driving", "Кузнецов", "Алексей", "Петрович", "+79990000003", UserRole.DrivingInstructor);
        DemoCaller[] students =
        [
            await Register("student1", "Смирнов", "Олег", null, "+79990000011", UserRole.Student),
            await Register("student2", "Волкова", "Мария", "Андреевна", "+79990000012", UserRole.Student),
            await Register("student3", "Новиков", "Дмитрий", null, "+79990000013", UserRole.Student)
        ];

        // The questions of the practice tests, written by the theory instructor, and the test itself.
        for (var index = 0; index < DemoQuestions.Topics.Count; index++)
        {
            foreach (var question in DemoQuestions.Topics[index].Questions)
            {
                await SendAsync(
                    theoryInstructor,
                    new CreateQuestionCommand(
                        question.Text,
                        question.Explanation,
                        topicIds[index],
                        [LicenseCategory.B],
                        QuestionSource.School,
                        null,
                        question.Options),
                    cancellationToken);
            }
        }

        await SendAsync(theoryInstructor, new CreateTestTemplateCommand(
            "Пробный экзамен", LicenseCategory.B, 10, 15, 2, null, 0, 0, [new TestBlockInput(10, null)]), cancellationToken);

        // The contracts of the students, their payments and their group, drawn up by the manager.
        Transmission[] transmissions = [Transmission.Manual, Transmission.Manual, Transmission.Automatic];
        decimal[] payments = [45000m, 20000m, 0m];
        var contractIds = new List<Guid>();
        for (var index = 0; index < students.Length; index++)
        {
            var contractId = await SendAsync(manager, new DrawUpContractCommand(
                students[index].UserId, courseId, transmissions[index], today, today.AddDays(120)), cancellationToken);
            contractIds.Add(contractId);
            if (payments[index] > 0)
            {
                await SendAsync(manager, new RegisterPaymentCommand(
                    contractId, payments[index], PaymentMethod.BankCard, "Обучение по договору", ConfirmNow: true), cancellationToken);
            }
        }

        var groupId = await SendAsync(manager, new CreateStudyGroupCommand(
            "Группа B-1", courseId, theoryInstructor.UserId, today, today.AddDays(90), 20, branchId), cancellationToken);
        foreach (var contractId in contractIds)
            await SendAsync(manager, new EnrollInStudyGroupCommand(groupId, contractId), cancellationToken);

        // Theory on Mondays, Wednesdays and Fridays in the evening, through the topics of the course.
        var lesson = 0;
        for (var day = today.AddDays(1); lesson < 9; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday))
                continue;

            await SendAsync(theoryInstructor, new ScheduleTheoryLessonCommand(
                groupId, topicIds[lesson % topicIds.Count], UtcOf(day, 18, 30, zone), UtcOf(day, 20, 0, zone)), cancellationToken);
            lesson++;
        }

        // Driving on weekdays: two lessons on the manual car in the morning and one on the automatic in the afternoon;
        // the students book some of them.
        var weekday = 0;
        for (var day = today.AddDays(1); weekday < 10; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var morning = await SendAsync(drivingInstructor, new OfferDrivingSlotCommand(
                manualId, UtcOf(day, 9, 0, zone), UtcOf(day, 10, 30, zone), groundId), cancellationToken);
            var noon = await SendAsync(drivingInstructor, new OfferDrivingSlotCommand(
                manualId, UtcOf(day, 11, 0, zone), UtcOf(day, 12, 30, zone), groundId), cancellationToken);
            var afternoon = await SendAsync(drivingInstructor, new OfferDrivingSlotCommand(
                automaticId, UtcOf(day, 15, 0, zone), UtcOf(day, 16, 30, zone), groundId), cancellationToken);

            if (weekday is 0 or 2 or 4)
                await SendAsync(students[0], new BookDrivingSlotCommand(morning, contractIds[0]), cancellationToken);

            if (weekday is 1 or 3)
                await SendAsync(students[1], new BookDrivingSlotCommand(noon, contractIds[1]), cancellationToken);

            if (weekday == 1)
                await SendAsync(students[2], new BookDrivingSlotCommand(afternoon, contractIds[2]), cancellationToken);

            weekday++;
        }
    }

    // Registers a user with a role and a sign-in account, as an invitation does, and accepts the invitation with the
    // password, as the user would.
    private async Task<DemoCaller> RegisterAsync(
        string name,
        string lastName,
        string firstName,
        string? middleName,
        string? phone,
        UserRole role,
        Guid? branchId,
        string password,
        CancellationToken cancellationToken)
    {
        var user = User.Create(
                FullName.Create(lastName, firstName, middleName).Value,
                EmailOf(name),
                phone is null ? null : PhoneNumber.Create(phone).Value,
                branchId,
                timeProvider.GetUtcNow().UtcDateTime,
                Language.Russian)
            .Value;

        string token;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            services.GetRequiredService<IUserRepository>().Add(user);
            services.GetRequiredService<IUserRoleRepository>().Add(UserRoleAssignment.Create(user.Id, role).Value);
            var identity = services.GetRequiredService<IIdentityService>();
            await identity.CreateAccountAsync(user.Id, user.Email, cancellationToken);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            token = await identity.GenerateInvitationTokenAsync(user.Id, cancellationToken);
        }

        await SendAsync(null, new AcceptInvitationCommand(user.Id, token, password), cancellationToken);

        return new DemoCaller(user.Id, branchId, role);
    }

    private async Task<TValue> SendAsync<TValue>(DemoCaller caller, ICommand<TValue> command, CancellationToken cancellationToken) =>
        (await SendAsync<Result<TValue>>(caller, command, cancellationToken)).Value;

    // Sends a command on behalf of a demo user, or of nobody, in a scope of its own, as a request of the API would be.
    private async Task<TResult> SendAsync<TResult>(DemoCaller? caller, IRequest<TResult> command, CancellationToken cancellationToken)
        where TResult : Result
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        if (caller is not null)
            scope.ServiceProvider.GetRequiredService<ScopeCaller>().ActAs(caller);

        var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(command, cancellationToken);

        return result.IsFailure
            ? throw new InvalidOperationException($"{command.GetType().Name} failed: {result.Error.Code} {result.Error.Message}")
            : result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "DemoData:Password is not set, so no demo data was created")]
    private static partial void LogNoPassword(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the demo data; its users sign in at @{EmailDomain}")]
    private static partial void LogCreated(ILogger logger, string emailDomain);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The demo data could not be created: {Reason}. Start with a new database to try again")]
    private static partial void LogFailed(ILogger logger, string reason);
}
