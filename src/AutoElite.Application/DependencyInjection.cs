using AutoElite.Application.Chats;
using AutoElite.Application.Common.Behaviors;
using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Contracts;
using AutoElite.Application.Courses;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.DrivingSlots;
using AutoElite.Application.Exams;
using AutoElite.Application.Leads;
using AutoElite.Application.Notifications;
using AutoElite.Application.Notifications.DrivingLessons;
using AutoElite.Application.Notifications.StaffTasks;
using AutoElite.Application.Payments;
using AutoElite.Application.Questions;
using AutoElite.Application.Settings;
using AutoElite.Application.StaffTasks;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TestSessions;
using AutoElite.Application.TestTemplates;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.Invitations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.Application;

/// <summary>Registers the application layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the dispatcher, the domain event publisher, the error localizer, all request handlers, domain event handlers
    /// and validators found in this assembly,
    /// and the pipeline behaviors in execution order: logging, authorization, validation, then the unit of work.
    /// The host must register <see cref="Common.Abstractions.ICurrentUser"/> and <see cref="Common.Abstractions.IUnitOfWork"/>.
    /// Also registers the system clock as <see cref="TimeProvider"/> unless a clock is already registered,
    /// so that tests can substitute their own.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddSingleton<IErrorLocalizer, ResourceErrorLocalizer>();
        services.AddScoped<InvitationSender>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<ManagedUsers>();
        services.AddScoped<UserRegistration>();
        services.AddScoped<StaffMembers>();
        services.AddScoped<SchoolSettingsProvider>();
        services.AddScoped<CoursePrice>();
        services.AddScoped<LeadAccess>();
        services.AddScoped<LeadManagers>();
        services.AddScoped<ContractStudents>();
        services.AddScoped<ContractFactory>();
        services.AddScoped<ContractAccess>();
        services.AddScoped<PaymentAccess>();
        services.AddScoped<StudyGroupAccess>();
        services.AddScoped<GroupInstructors>();
        services.AddScoped<TheoryLessonAccess>();
        services.AddScoped<LessonInstructors>();
        services.AddScoped<DrivingRouteAccess>();
        services.AddScoped<DrivingSlotAccess>();
        services.AddScoped<QuestionAccess>();
        services.AddScoped<QuestionContent>();
        services.AddScoped<TestTemplateContent>();
        services.AddScoped<SessionQuestions>();
        services.AddScoped<TestAttempts>();
        services.AddScoped<ExamAccess>();
        services.AddScoped<ChatAccess>();
        services.AddScoped<ChatUsers>();
        services.AddScoped<LocalTimes>();
        services.AddScoped<Notifier>();
        services.AddScoped<GroupNotifier>();
        services.AddScoped<DrivingLessonReader>();
        services.AddScoped<StaffTaskAccess>();
        services.AddScoped<TaskAssignees>();
        services.AddScoped<TaskSubjects>();
        services.AddScoped<StaffTaskNotices>();

        RegisterOpenGenericImplementations(services, typeof(IRequestHandler<,>));
        RegisterOpenGenericImplementations(services, typeof(IValidator<>));
        RegisterOpenGenericImplementations(services, typeof(IDomainEventHandler<>));

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }

    private static void RegisterOpenGenericImplementations(IServiceCollection services, Type openGenericInterface)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        foreach (var implementationType in applicationAssembly.GetTypes())
        {
            if (implementationType.IsAbstract || implementationType.IsInterface)
                continue;

            foreach (var implementedInterface in implementationType.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == openGenericInterface)
                    services.AddScoped(implementedInterface, implementationType);
            }
        }
    }
}
