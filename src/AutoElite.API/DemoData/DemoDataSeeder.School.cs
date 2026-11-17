using AutoElite.Application.Branches.CreateBranch;
using AutoElite.Application.Chats.OpenDirectChat;
using AutoElite.Application.Chats.SendChatMessage;
using AutoElite.Application.Contracts.DrawUpContract;
using AutoElite.Application.Courses.AddCourseTopic;
using AutoElite.Application.Courses.CreateCourse;
using AutoElite.Application.Documents.AttachDocument;
using AutoElite.Application.DrivingSlots.BookDrivingSlot;
using AutoElite.Application.DrivingSlots.CompleteDrivingLesson;
using AutoElite.Application.DrivingSlots.MarkDrivingNoShow;
using AutoElite.Application.DrivingSlots.OfferDrivingSlot;
using AutoElite.Application.DrivingSlots.RateDrivingLesson;
using AutoElite.Application.Exams.RecordPracticeExamResult;
using AutoElite.Application.Exams.ScheduleExam;
using AutoElite.Application.Exams.StartExamTest;
using AutoElite.Application.Payments.RegisterPayment;
using AutoElite.Application.Questions.CreateQuestion;
using AutoElite.Application.StaffTasks.CompleteStaffTask;
using AutoElite.Application.StaffTasks.CreateStaffTask;
using AutoElite.Application.StaffTasks.StartStaffTask;
using AutoElite.Application.StudyGroups.CreateStudyGroup;
using AutoElite.Application.StudyGroups.EnrollInStudyGroup;
using AutoElite.Application.TestSessions.AnswerTestQuestion;
using AutoElite.Application.TestSessions.GetTestSession;
using AutoElite.Application.TestSessions.StartTest;
using AutoElite.Application.TestTemplates;
using AutoElite.Application.TestTemplates.CreateTestTemplate;
using AutoElite.Application.TheoryLessons.MarkAttendance;
using AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;
using AutoElite.Application.TrainingGrounds.CreateTrainingGround;
using AutoElite.Application.Vehicles.RegisterVehicle;

namespace AutoElite.API.DemoData;

/// <content>
/// The school of the demo: its set-up, its people and three weeks of training behind the group, created in the order
/// they happened, then its future.
/// </content>
internal sealed partial class DemoDataSeeder
{
    // How long the group has been training, and how far its lessons are planned ahead.
    private const int DaysBehind = 21;
    private const int DaysAhead = 21;

    private static readonly DrivingSkill[] _skills =
    [
        DrivingSkill.VehicleControl, DrivingSkill.GearShifting, DrivingSkill.Manoeuvring,
        DrivingSkill.TrafficRules, DrivingSkill.Observation, DrivingSkill.CityDriving
    ];

    private async Task CreateAsync(string password, CancellationToken cancellationToken)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var start = today.AddDays(-DaysBehind);

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

        // Three weeks ago: the questions and the test, the contracts, the group and its plan.
        clock.Set(UtcOf(start, 9, 0, zone));
        var templateId = await CreateQuestionsAsync(theoryInstructor, topicIds, cancellationToken);
        var contractIds = await DrawUpContractsAsync(manager, students, courseId, start, cancellationToken);
        var groupId = await SendAsync(manager, new CreateStudyGroupCommand(
            "Группа B-1", courseId, theoryInstructor.UserId, start, start.AddDays(90), 20, branchId), cancellationToken);
        foreach (var contractId in contractIds)
            await SendAsync(manager, new EnrollInStudyGroupCommand(groupId, contractId), cancellationToken);

        var past = new List<(DateTime At, Func<Task> Step)>();
        await PlanTheoryAsync(theoryInstructor, groupId, topicIds, contractIds, start, today, zone, past, cancellationToken);
        await PlanPastDrivingAsync(
            drivingInstructor, students, contractIds, manualId, automaticId, groundId, start, today, zone, past, cancellationToken);
        await OfferStartedDrivingAsync(drivingInstructor, students, contractIds, manualId, groundId, now, cancellationToken);
        await PlanExamsAsync(
            theoryInstructor, drivingInstructor, students, contractIds, groupId, templateId, today, now, zone, past, cancellationToken);
        await PlanTasksAsync(manager, drivingInstructor, theoryInstructor, groupId, manualId, today, zone, past, cancellationToken);
        PlanTests(students, contractIds, templateId, today, zone, past, cancellationToken);
        PlanChats(drivingInstructor, theoryInstructor, students, today, zone, past, cancellationToken);
        PlanOffice(administrator, manager, branchId, contractIds, today, zone, past, cancellationToken);

        // What happened since, in the order it happened, each at its moment.
        foreach (var (at, step) in past.OrderBy(planned => planned.At))
        {
            clock.Set(at);
            await step();
        }

        // The present: the driving lessons of the next two weeks.
        clock.Reset();
        await OfferFutureDrivingAsync(
            drivingInstructor, students, contractIds, manualId, automaticId, groundId, today, zone, cancellationToken);
    }

    // The questions of the practice tests, written by the theory instructor, and the test itself.
    private async Task<Guid> CreateQuestionsAsync(DemoCaller theoryInstructor, List<Guid> topicIds, CancellationToken cancellationToken)
    {
        for (var index = 0; index < DemoQuestions.Topics.Count; index++)
        {
            foreach (var question in DemoQuestions.Topics[index].Questions)
            {
                await SendAsync(
                    theoryInstructor,
                    new CreateQuestionCommand(
                        question.Text, question.Explanation, topicIds[index], [LicenseCategory.B], QuestionSource.School, null,
                        question.Options),
                    cancellationToken);
            }
        }

        return await SendAsync(theoryInstructor, new CreateTestTemplateCommand(
            "Пробный экзамен", LicenseCategory.B, 10, 15, 2, null, 0, 0, [new TestBlockInput(10, null)]), cancellationToken);
    }

    // The contracts of the students, their payments and their scanned contracts, drawn up by the manager.
    private async Task<List<Guid>> DrawUpContractsAsync(
        DemoCaller manager, DemoCaller[] students, Guid courseId, DateOnly start, CancellationToken cancellationToken)
    {
        Transmission[] transmissions = [Transmission.Manual, Transmission.Manual, Transmission.Automatic];
        decimal[] payments = [45000m, 20000m, 0m];
        var contractIds = new List<Guid>();
        for (var index = 0; index < students.Length; index++)
        {
            var contractId = await SendAsync(manager, new DrawUpContractCommand(
                students[index].UserId, courseId, transmissions[index], start, start.AddDays(120)), cancellationToken);
            contractIds.Add(contractId);
            if (payments[index] > 0)
            {
                await SendAsync(manager, new RegisterPaymentCommand(
                    contractId, payments[index], PaymentMethod.BankCard, "Обучение по договору", ConfirmNow: true), cancellationToken);
            }

            await AttachPdfAsync(
                manager, EntityKind.Contract, contractId, "Договор.pdf", "Contract", "Подписанный договор", cancellationToken);
        }

        return contractIds;
    }

    // Theory on Mondays, Wednesdays and Fridays in the evening, through the topics of the course, three weeks back and
    // three weeks ahead; the instructor marks the attendance after each lesson held, with a few students away.
    private async Task PlanTheoryAsync(
        DemoCaller theoryInstructor,
        Guid groupId,
        List<Guid> topicIds,
        List<Guid> contractIds,
        DateOnly start,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        var lesson = 0;
        for (var day = start.AddDays(1); day <= today.AddDays(DaysAhead); day = day.AddDays(1))
        {
            if (day == today || day.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday))
                continue;

            var lessonId = await SendAsync(theoryInstructor, new ScheduleTheoryLessonCommand(
                groupId, topicIds[lesson % topicIds.Count], UtcOf(day, 18, 30, zone), UtcOf(day, 20, 0, zone)), cancellationToken);
            if (day < today)
            {
                var held = lesson;
                Plan(past, UtcOf(day, 20, 5, zone), () => SendAsync(
                    theoryInstructor,
                    new MarkAttendanceCommand(
                        lessonId,
                        [
                            new AttendanceMark(contractIds[0], true),
                            new AttendanceMark(contractIds[1], held != 2, held == 2 ? "Болела" : null),
                            new AttendanceMark(contractIds[2], held % 3 != 1)
                        ]),
                    cancellationToken));
            }

            lesson++;
        }
    }

    // Driving on the weekdays of the three weeks behind: the students booked lessons, the instructor completed them
    // with grades and a note, one student did not come once, and the first student rated their lessons.
    private async Task PlanPastDrivingAsync(
        DemoCaller drivingInstructor,
        DemoCaller[] students,
        List<Guid> contractIds,
        Guid manualId,
        Guid automaticId,
        Guid groundId,
        DateOnly start,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        string[] notes =
        [
            "Уверенно трогается на подъёме, поработать над парковкой задним ходом.",
            "Хорошо держит полосу, нужно чаще смотреть в зеркала.",
            "Отработали перекрёстки, к экзамену почти готов."
        ];
        var weekday = 0;
        for (var day = start.AddDays(1); day < today; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var booked = new List<(Guid SlotId, int Student, DateTime End)>();
            if (weekday % 2 == 0)
                booked.Add((await OfferAsync(manualId, day, 9, 0), 0, UtcOf(day, 10, 30, zone)));

            if (weekday % 3 == 1)
                booked.Add((await OfferAsync(manualId, day, 11, 0), 1, UtcOf(day, 12, 30, zone)));

            if (weekday % 4 == 2)
                booked.Add((await OfferAsync(automaticId, day, 15, 0), 2, UtcOf(day, 16, 30, zone)));

            foreach (var (slotId, student, end) in booked)
            {
                await SendAsync(students[student], new BookDrivingSlotCommand(slotId, contractIds[student]), cancellationToken);
                var grade = 3 + ((weekday + student) % 3);
                var note = notes[(weekday + student) % notes.Length];
                var isNoShow = student == 1 && weekday == 4;
                Plan(past, end.AddMinutes(5), async () =>
                {
                    if (isNoShow)
                    {
                        await SendAsync(
                            drivingInstructor, new MarkDrivingNoShowCommand(slotId, "Не пришла, не предупредила"), cancellationToken);

                        return;
                    }

                    await SendAsync(
                        drivingInstructor,
                        new CompleteDrivingLessonCommand(
                            slotId,
                            1.5m,
                            [.. _skills.Select((skill, index) => new SkillGradeInput(skill, Math.Clamp(grade + (index % 2), 1, 5)))],
                            note),
                        cancellationToken);
                    if (student == 0)
                    {
                        await SendAsync(
                            students[0],
                            new RateDrivingLessonCommand(slotId, grade >= 4 ? 5 : 4, "Спасибо, всё понятно"),
                            cancellationToken);
                    }
                });
            }

            weekday++;
        }

        Task<Guid> OfferAsync(Guid vehicleId, DateOnly day, int hour, int minute) =>
            SendAsync(drivingInstructor, new OfferDrivingSlotCommand(
                vehicleId, UtcOf(day, hour, minute, zone), UtcOf(day, hour, minute, zone).AddMinutes(90), groundId), cancellationToken);
    }

    // Two lessons of today that have begun and wait for the instructor, booked by the first two students: one to
    // complete with grades, one to mark missed.
    private async Task OfferStartedDrivingAsync(
        DemoCaller drivingInstructor,
        DemoCaller[] students,
        List<Guid> contractIds,
        Guid manualId,
        Guid groundId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var halfHour = TimeSpan.FromMinutes(30).Ticks;
        var latest = new DateTime(now.Ticks - (now.Ticks % halfHour), DateTimeKind.Utc).AddHours(-2);
        (DateTime Start, int Student)[] lessons = [(latest, 0), (latest.AddHours(-2), 1)];
        foreach (var (lessonStart, student) in lessons)
        {
            var slotId = await SendAsync(
                drivingInstructor,
                new OfferDrivingSlotCommand(manualId, lessonStart, lessonStart.AddMinutes(90), groundId),
                cancellationToken);
            await SendAsync(students[student], new BookDrivingSlotCommand(slotId, contractIds[student]), cancellationToken);
        }
    }

    // The exams of the group: a theory exam three days ago, which two students took, a practice exam that began two
    // hours ago with the result of the first student recorded, and a theory exam ahead.
    private async Task PlanExamsAsync(
        DemoCaller theoryInstructor,
        DemoCaller drivingInstructor,
        DemoCaller[] students,
        List<Guid> contractIds,
        Guid groupId,
        Guid templateId,
        DateOnly today,
        DateTime now,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        var theoryAt = UtcOf(today.AddDays(-3), 17, 0, zone);
        var theoryExamId = await SendAsync(
            theoryInstructor, new ScheduleExamCommand(groupId, ExamKind.Theory, theoryAt, templateId), cancellationToken);
        var practiceAt = now.AddHours(-2);
        var practiceExamId = await SendAsync(
            theoryInstructor, new ScheduleExamCommand(groupId, ExamKind.Practice, practiceAt), cancellationToken);
        await SendAsync(
            theoryInstructor,
            new ScheduleExamCommand(groupId, ExamKind.Theory, UtcOf(today.AddDays(10), 17, 0, zone), templateId),
            cancellationToken);

        Plan(past, theoryAt.AddMinutes(5), async () =>
        {
            var first = await SendAsync(students[0], new StartExamTestCommand(theoryExamId, contractIds[0]), cancellationToken);
            await AnswerAsync(students[0], first, mistakes: 1, cancellationToken);
            var second = await SendAsync(students[1], new StartExamTestCommand(theoryExamId, contractIds[1]), cancellationToken);
            await AnswerAsync(students[1], second, mistakes: 3, cancellationToken);
        });
        Plan(past, practiceAt.AddHours(1), () => SendAsync(
            drivingInstructor,
            new RecordPracticeExamResultCommand(practiceExamId, contractIds[0], true, 95, "Уверенно, одна мелкая ошибка на парковке"),
            cancellationToken));
    }

    // The tasks the manager gives the instructors: one overdue, one started, one recurring, one coming and one done.
    private async Task PlanTasksAsync(
        DemoCaller manager,
        DemoCaller drivingInstructor,
        DemoCaller theoryInstructor,
        Guid groupId,
        Guid manualId,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        await SendAsync(manager, new CreateStaffTaskCommand(
            "Пройти техосмотр Lada Vesta",
            "Записаться на техосмотр и привезти диагностическую карту в офис.",
            drivingInstructor.UserId,
            UtcOf(today.AddDays(-1), 18, 0, zone),
            EntityKind.Vehicle,
            manualId,
            TaskRecurrence.None), cancellationToken);
        var started = await SendAsync(manager, new CreateStaffTaskCommand(
            "Подготовить маршрут к экзамену",
            "Составить экзаменационный маршрут по району ГИБДД и прокатать его с учениками.",
            drivingInstructor.UserId,
            UtcOf(today.AddDays(3), 18, 0, zone),
            null,
            null,
            TaskRecurrence.None), cancellationToken);
        await SendAsync(manager, new CreateStaffTaskCommand(
            "Сдать путевые листы",
            null,
            drivingInstructor.UserId,
            UtcOf(today.AddDays(2), 12, 0, zone),
            null,
            null,
            TaskRecurrence.Weekly), cancellationToken);
        await SendAsync(manager, new CreateStaffTaskCommand(
            "Проверить посещаемость группы B-1",
            "Позвонить ученикам, пропустившим больше двух занятий.",
            theoryInstructor.UserId,
            UtcOf(today.AddDays(2), 18, 0, zone),
            EntityKind.StudyGroup,
            groupId,
            TaskRecurrence.None), cancellationToken);
        var done = await SendAsync(manager, new CreateStaffTaskCommand(
            "Подготовить билеты к пробному экзамену",
            null,
            theoryInstructor.UserId,
            UtcOf(today.AddDays(-4), 18, 0, zone),
            null,
            null,
            TaskRecurrence.None), cancellationToken);

        Plan(past, UtcOf(today.AddDays(-5), 11, 0, zone), () => SendAsync(
            theoryInstructor, new CompleteStaffTaskCommand(done), cancellationToken));
        Plan(past, UtcOf(today.AddDays(-1), 12, 0, zone), () => SendAsync(
            drivingInstructor, new StartStaffTaskCommand(started), cancellationToken));
    }

    // The practice tests the students took: the first failed one, then passed; the second passed.
    private void PlanTests(
        DemoCaller[] students,
        List<Guid> contractIds,
        Guid templateId,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        (int Student, int DaysAgo, int Mistakes)[] tests = [(0, 15, 4), (0, 10, 1), (1, 5, 0)];
        foreach (var (student, daysAgo, mistakes) in tests)
        {
            Plan(past, UtcOf(today.AddDays(-daysAgo), 21, 0, zone), async () =>
            {
                var sessionId = await SendAsync(
                    students[student], new StartTestCommand(contractIds[student], templateId), cancellationToken);
                await AnswerAsync(students[student], sessionId, mistakes, cancellationToken);
            });
        }
    }

    // The personal chats: the first student with the driving instructor, who sends the route of a lesson, and the
    // second with the theory instructor; the first student also keeps a medical certificate among their documents.
    private void PlanChats(
        DemoCaller drivingInstructor,
        DemoCaller theoryInstructor,
        DemoCaller[] students,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        Plan(past, UtcOf(today.AddDays(-19), 10, 0, zone), () => AttachPdfAsync(
            students[0], EntityKind.User, students[0].UserId, "Медицинская справка.pdf", "Medical certificate",
            "Медицинская справка 003-В/у", cancellationToken));

        var drivingChat = Guid.Empty;
        Plan(past, UtcOf(today.AddDays(-6), 10, 0, zone), async () =>
        {
            drivingChat = await SendAsync(students[0], new OpenDirectChatCommand(drivingInstructor.UserId), cancellationToken);
            await SendAsync(
                students[0],
                new SendChatMessageCommand(drivingChat, "Здравствуйте! Можно в четверг заниматься на автодроме, а не в городе?"),
                cancellationToken);
        });
        Plan(past, UtcOf(today.AddDays(-6), 10, 20, zone), async () =>
        {
            var route = await AttachPdfAsync(
                drivingInstructor, EntityKind.Chat, drivingChat, "Маршрут занятия.pdf", "Lesson route", null, cancellationToken);
            await SendAsync(
                drivingInstructor,
                new SendChatMessageCommand(drivingChat, "Добрый день! Да, начнём с автодрома, потом выедем по этому маршруту.", [route]),
                cancellationToken);
        });
        Plan(past, UtcOf(today.AddDays(-1), 20, 30, zone), () => SendAsync(
            students[0], new SendChatMessageCommand(drivingChat, "Спасибо за занятие!"), cancellationToken));

        var theoryChat = Guid.Empty;
        Plan(past, UtcOf(today.AddDays(-4), 19, 0, zone), async () =>
        {
            theoryChat = await SendAsync(students[1], new OpenDirectChatCommand(theoryInstructor.UserId), cancellationToken);
            await SendAsync(
                students[1],
                new SendChatMessageCommand(theoryChat, "Михаил Игоревич, я пропустила занятие, что прочитать?"),
                cancellationToken);
        });
        Plan(past, UtcOf(today.AddDays(-4), 19, 40, zone), () => SendAsync(
            theoryInstructor,
            new SendChatMessageCommand(theoryChat, "Разделы 6 и 13 ПДД, а в приложении пройдите тест по светофорам."),
            cancellationToken));
    }

    // The driving lessons of the next two weeks, as before: two on the manual car in the morning and one on the
    // automatic in the afternoon on weekdays; the students book some of them.
    private async Task OfferFutureDrivingAsync(
        DemoCaller drivingInstructor,
        DemoCaller[] students,
        List<Guid> contractIds,
        Guid manualId,
        Guid automaticId,
        Guid groundId,
        DateOnly today,
        TimeZoneInfo zone,
        CancellationToken cancellationToken)
    {
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

    // Adds a step to the past, which the seeder runs once the clock shows its time.
    private static void Plan(List<(DateTime At, Func<Task> Step)> past, DateTime at, Func<Task> step) => past.Add((at, step));

    // Answers the questions of an attempt in order, correctly but for the given number of mistakes first; the attempt
    // finishes with the last answer, or earlier once the mistakes are more than the test allows.
    private async Task AnswerAsync(DemoCaller student, Guid sessionId, int mistakes, CancellationToken cancellationToken)
    {
        var session = await QueryAsync(student, new GetTestSessionQuery(sessionId), cancellationToken);
        var correct = DemoQuestions.Topics
            .SelectMany(topic => topic.Questions)
            .ToDictionary(question => question.Text, question => question.Options.First(option => option.IsCorrect).Text);
        foreach (var question in session.Questions.OrderBy(question => question.Position))
        {
            var right = correct.GetValueOrDefault(question.Text);
            var option = mistakes > 0
                ? question.Options.First(option => option.Text != right)
                : question.Options.FirstOrDefault(option => option.Text == right) ?? question.Options[0];
            if (mistakes > 0)
                mistakes--;

            var status = await SendAsync(
                student, new AnswerTestQuestionCommand(sessionId, question.Id, option.OptionId), cancellationToken);
            if (status != TestSessionStatus.InProgress)
                return;
        }
    }

    // Attaches a one-page PDF document to a record on behalf of a user.
    private async Task<Guid> AttachPdfAsync(
        DemoCaller caller,
        EntityKind kind,
        Guid ownerId,
        string fileName,
        string title,
        string? description,
        CancellationToken cancellationToken)
    {
        var bytes = DemoFiles.Pdf(title);
        using var content = new MemoryStream(bytes);

        return await SendAsync(
            caller,
            new AttachDocumentCommand(kind, ownerId, fileName, "application/pdf", bytes.Length, description, content),
            cancellationToken);
    }
}
