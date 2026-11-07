using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoElite.Infrastructure.Tests.Database;

public sealed class MigrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    /// <summary>The exclusion constraints of the SQL scripts, which EF Core does not model.</summary>
    private static readonly string[] _exclusionConstraints =
    [
        "ex_theory_lessons_group_time",
        "ex_theory_lessons_instructor_time",
        "ex_driving_slots_instructor_time",
        "ex_driving_slots_vehicle_time",
        "ex_driving_bookings_student_time"
    ];

    /// <summary>What reverting every migration leaves: the history of the migrations, now empty.</summary>
    private static readonly string[] _historyTableOnly = [PersistenceOptions.MigrationsHistoryTable];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void Model_HasNoChangesMissingFromTheMigrations()
    {
        using var context = database.CreateContext();

        context.Database.HasPendingModelChanges().Should().BeFalse(
            because: "a change to the model needs a migration: dotnet ef migrations add <Name>");
    }

    [Fact]
    public async Task Migrations_AreAllApplied_WithTheObjectsTheModelDoesNotDescribe()
    {
        await using var context = database.CreateContext();

        (await context.Database.GetPendingMigrationsAsync(CancellationToken)).Should().BeEmpty();
        (await ConstraintsAsync(context)).Should().Contain(_exclusionConstraints);
    }

    [Fact]
    public async Task Migrations_RevertedAndAppliedAgain_RecreateTheSchema()
    {
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(Migration.InitialDatabase, cancellationToken: CancellationToken);
        var tablesAfterRevert = await TablesAsync(context);
        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        tablesAfterRevert.Should().BeEquivalentTo(_historyTableOnly);
        (await context.Database.GetPendingMigrationsAsync(CancellationToken)).Should().BeEmpty();
        (await ConstraintsAsync(context)).Should().Contain("ex_driving_bookings_student_time");
    }

    private static async Task<List<string>> TablesAsync(AutoEliteDbContext context) =>
        await context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync(CancellationToken);

    private static async Task<List<string>> ConstraintsAsync(AutoEliteDbContext context) =>
        await context.Database
            .SqlQuery<string>($"SELECT conname::text AS \"Value\" FROM pg_constraint WHERE contype = 'x'")
            .ToListAsync(CancellationToken);
}
