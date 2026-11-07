using AutoElite.Application.Analytics;
using AutoElite.Application.Branches;
using AutoElite.Application.Chats;
using AutoElite.Application.Contracts;
using AutoElite.Application.Documents;
using AutoElite.Application.Courses;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.DrivingSlots;
using AutoElite.Application.Exams;
using AutoElite.Application.Leads;
using AutoElite.Application.Notifications;
using AutoElite.Application.Payments;
using AutoElite.Application.Questions;
using AutoElite.Application.Settings;
using AutoElite.Application.StaffTasks;
using AutoElite.Application.StudentCards;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TestSessions;
using AutoElite.Application.TestTemplates;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.TrainingGrounds;
using AutoElite.Application.Users;
using AutoElite.Application.Vehicles;
using AutoElite.Application.Users.Authentication;
using AutoElite.Infrastructure.Authentication;
using AutoElite.Infrastructure.BackgroundJobs;
using AutoElite.Infrastructure.Encryption;
using AutoElite.Infrastructure.Identity;
using AutoElite.Infrastructure.Mail;
using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence;
using AutoElite.Infrastructure.Persistence.Interceptors;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure;

/// <summary>Registers the infrastructure layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string of the school's database.</summary>
    public const string ConnectionStringName = "AutoElite";

    /// <summary>
    /// Registers the database context, the unit of work, the sign-in accounts and session tokens, the authentication of
    /// requests, the repositories and read services, and the outbox worker and the scheduled jobs that run in the host.
    /// The host must register <see cref="ICurrentUser"/>, which reports the system outside a request, since the outbox
    /// handlers and the jobs run on its behalf, and logging; and configure the <see cref="JwtOptions.SectionName"/>,
    /// <see cref="ClientOptions.SectionName"/> and <see cref="ChatEncryptionOptions.SectionName"/> sections.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configuration">The configuration with the connection string <see cref="ConnectionStringName"/>.</param>
    /// <returns>The same services.</returns>
    /// <exception cref="InvalidOperationException">The connection string is missing.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string \"{ConnectionStringName}\" is missing. Set ConnectionStrings:{ConnectionStringName} "
                + $"with dotnet user-secrets locally or the ConnectionStrings__{ConnectionStringName} environment variable when deployed.");
        }

        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<AutoEliteDbContext>((provider, options) =>
            options.UseAutoElite(connectionString, provider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // A repository with its own interface also serves as the generic one, which some handlers ask for.
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IRepository<Branch>>(provider => provider.GetRequiredService<IBranchRepository>());
        services.AddScoped<IBranchQueries, BranchQueries>();
        services.AddScoped<ISchoolSettingsRepository, SchoolSettingsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRepository<User>>(provider => provider.GetRequiredService<IUserRepository>());
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRepository<UserRoleAssignment>>(provider => provider.GetRequiredService<IUserRoleRepository>());
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IRepository<Lead>, LeadRepository>();
        services.AddScoped<ILeadQueries, LeadQueries>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IRepository<Course>>(provider => provider.GetRequiredService<ICourseRepository>());
        services.AddScoped<ICourseQueries, CourseQueries>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IRepository<Contract>>(provider => provider.GetRequiredService<IContractRepository>());
        services.AddScoped<IContractQueries, ContractQueries>();
        services.AddScoped<IContractNumberGenerator, ContractNumberGenerator>();
        services.AddScoped<IStudyGroupRepository, StudyGroupRepository>();
        services.AddScoped<IRepository<StudyGroup>>(provider => provider.GetRequiredService<IStudyGroupRepository>());
        services.AddScoped<IStudyGroupQueries, StudyGroupQueries>();
        services.AddScoped<ITheoryLessonRepository, TheoryLessonRepository>();
        services.AddScoped<IRepository<TheoryLesson>>(provider => provider.GetRequiredService<ITheoryLessonRepository>());
        services.AddScoped<ITheoryLessonQueries, TheoryLessonQueries>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IRepository<Vehicle>>(provider => provider.GetRequiredService<IVehicleRepository>());
        services.AddScoped<IVehicleQueries, VehicleQueries>();
        services.AddScoped<IRepository<TrainingGround>, TrainingGroundRepository>();
        services.AddScoped<ITrainingGroundQueries, TrainingGroundQueries>();
        services.AddScoped<IRepository<DrivingRoute>, DrivingRouteRepository>();
        services.AddScoped<IDrivingRouteQueries, DrivingRouteQueries>();
        services.AddScoped<IDrivingSlotRepository, DrivingSlotRepository>();
        services.AddScoped<IRepository<DrivingSlot>>(provider => provider.GetRequiredService<IDrivingSlotRepository>());
        services.AddScoped<IDrivingSlotQueries, DrivingSlotQueries>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IRepository<Question>>(provider => provider.GetRequiredService<IQuestionRepository>());
        services.AddScoped<IQuestionQueries, QuestionQueries>();
        services.AddScoped<IRepository<TestTemplate>, TestTemplateRepository>();
        services.AddScoped<ITestTemplateQueries, TestTemplateQueries>();
        services.AddScoped<ITestSessionRepository, TestSessionRepository>();
        services.AddScoped<IRepository<TestSession>>(provider => provider.GetRequiredService<ITestSessionRepository>());
        services.AddScoped<ITestSessionQueries, TestSessionQueries>();
        services.AddScoped<IQuestionPicker, QuestionPicker>();
        services.AddScoped<IExamRepository, ExamRepository>();
        services.AddScoped<IRepository<Exam>>(provider => provider.GetRequiredService<IExamRepository>());
        services.AddScoped<IExamQueries, ExamQueries>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IRepository<Chat>>(provider => provider.GetRequiredService<IChatRepository>());
        services.AddScoped<IRepository<ChatMessage>, ChatMessageRepository>();
        services.AddScoped<IChatQueries, ChatQueries>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRepository<Notification>>(provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<IEmailQueue, EmailQueue>();
        services.AddScoped<IRepository<Payment>, PaymentRepository>();
        services.AddScoped<IPaymentQueries, PaymentQueries>();
        services.AddScoped<IStaffTaskRepository, StaffTaskRepository>();
        services.AddScoped<IRepository<StaffTask>>(provider => provider.GetRequiredService<IStaffTaskRepository>());
        services.AddScoped<IStaffTaskQueries, StaffTaskQueries>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IRepository<Document>>(provider => provider.GetRequiredService<IDocumentRepository>());
        services.AddScoped<IDocumentQueries, DocumentQueries>();
        services.AddScoped<IStudentCardQueries, StudentCardQueries>();
        services.AddScoped<IEntityReferences, EntityReferences>();
        services.AddScoped<IAnalyticsQueries, AnalyticsQueries>();

        AddChatEncryption(services, configuration);
        AddOutbox(services, configuration);
        AddBackgroundJobs(services, configuration, connectionString);
        AddAccounts(services);
        AddSessions(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers Hangfire with its tables in the school's database, in the schema
    /// <see cref="BackgroundJobsOptions.SchemaName"/> that Hangfire creates and upgrades itself, its server in the host,
    /// and the schedule of the recurring jobs.
    /// The job classes are not registered: Hangfire creates them in the scope of each job.
    /// </summary>
    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BackgroundJobsOptions>>(
            new SettingsValidator<BackgroundJobsOptions>(options => options.Problems()));
        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                storage => storage.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = BackgroundJobsOptions.SchemaName,
                    PrepareSchemaIfNecessary = true
                }));
        services.AddHangfireServer((provider, server) =>
            server.WorkerCount = provider.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value.WorkerCount);
        services.AddHostedService<RecurringJobsRegistrar>();
    }

    /// <summary>
    /// Registers the outbox processor and the worker that runs it in the host, from the optional
    /// <see cref="OutboxOptions.SectionName"/> section.
    /// </summary>
    private static void AddOutbox(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboxOptions>>(
            new SettingsValidator<OutboxOptions>(options => options.Problems()));
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();
    }

    /// <summary>
    /// Registers the cipher of the chat texts, which the database context is built with, from validated settings: a
    /// host with missing or invalid keys fails at startup rather than at the first message.
    /// </summary>
    private static void AddChatEncryption(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ChatEncryptionOptions>()
            .Bind(configuration.GetSection(ChatEncryptionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ChatEncryptionOptions>>(
            new SettingsValidator<ChatEncryptionOptions>(options => options.Problems()));
        services.AddSingleton(provider => new ChatTextCipher(provider.GetRequiredService<IOptions<ChatEncryptionOptions>>().Value));
    }

    /// <summary>
    /// Registers ASP.NET Core Identity for the sign-in accounts, with the password rules of <see cref="PasswordPolicy"/>,
    /// a lockout after repeated wrong passwords, and data protection keys kept in the database.
    /// </summary>
    private static void AddAccounts(IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("AutoElite")
            .PersistKeysToDbContext<AutoEliteDbContext>();

        services.AddIdentityCore<UserAccount>(options =>
            {
                options.Password.RequiredLength = PasswordPolicy.MinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedSignIns;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;

                // The sign-in name is the email address, so any character an address may contain is allowed.
                options.User.AllowedUserNameCharacters = string.Empty;
            })
            .AddUserStore<AccountStore>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<InvitationTokenProvider>(InvitationTokenProvider.ProviderName);

        // Password reset and email change links: short-lived, since the user is waiting for them.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = EmailLinkLifetime);
        services.AddScoped<IIdentityService, IdentityService>();
    }

    /// <summary>
    /// Registers the session tokens and the links to the web client, with their settings checked when the host starts,
    /// and the authentication of requests by access token, with the same rules the tokens are created with.
    /// </summary>
    private static void AddSessions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>>(new SettingsValidator<JwtOptions>(options => options.Problems()));
        services.AddOptions<ClientOptions>().Bind(configuration.GetSection(ClientOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<ClientOptions>>(
            new SettingsValidator<ClientOptions>(options => options.Problems()));

        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IClientLinks, ClientLinks>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                // Claims keep the names the factory writes, such as "sub" and "role".
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = JwtAccessTokenFactory.ValidationParameters(jwt.Value);
            });
    }

    /// <summary>The number of wrong passwords in a row after which an account is locked.</summary>
    internal const int MaxFailedSignIns = 5;

    /// <summary>How long a locked account stays locked.</summary>
    internal static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>How long a password reset or email change link is valid.</summary>
    internal static readonly TimeSpan EmailLinkLifetime = TimeSpan.FromHours(2);
}
