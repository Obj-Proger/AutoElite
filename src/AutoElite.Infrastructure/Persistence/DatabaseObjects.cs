using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// The database objects EF Core does not model, such as exclusion constraints against overlapping times. They are SQL
/// scripts in <c>Persistence/Sql</c>, embedded in this assembly: the migrations apply them with
/// <see cref="Create(MigrationBuilder)"/> and <see cref="Drop"/>, and a database created from the model without
/// migrations, as in tests, gets them with <see cref="CreateAsync"/>.
/// </summary>
internal static class DatabaseObjects
{
    /// <summary>The scripts in the order they are created; they are dropped in the reverse order.</summary>
    private static readonly string[] _scripts =
    [
        "01-theory-lesson-overlap",
        "02-driving-overlap",
        "03-notification-subject"
    ];

    /// <summary>Creates the objects as part of a migration.</summary>
    /// <param name="migrationBuilder">The builder of the migration.</param>
    public static void Create(MigrationBuilder migrationBuilder)
    {
        foreach (var script in _scripts)
            migrationBuilder.Sql(Read($"{script}.up.sql"));
    }

    /// <summary>Drops the objects as part of reverting a migration.</summary>
    /// <param name="migrationBuilder">The builder of the migration.</param>
    public static void Drop(MigrationBuilder migrationBuilder)
    {
        foreach (var script in Enumerable.Reverse(_scripts))
            migrationBuilder.Sql(Read($"{script}.down.sql"));
    }

    /// <summary>Creates the objects in a database whose tables were created from the model.</summary>
    /// <param name="dbContext">A context on the database.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public static async Task CreateAsync(AutoEliteDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var script in _scripts)
            await dbContext.Database.ExecuteSqlRawAsync(Read($"{script}.up.sql"), cancellationToken);
    }

    private static string Read(string fileName)
    {
        var resourceName = $"Sql/{fileName}";
        using var stream = typeof(DatabaseObjects).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded SQL script {resourceName} is missing.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
