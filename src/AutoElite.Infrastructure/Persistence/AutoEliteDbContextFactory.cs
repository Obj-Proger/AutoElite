using Microsoft.EntityFrameworkCore.Design;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// Builds the context for the EF Core tools (<c>dotnet ef migrations add</c>), so that they need no host project.
/// Adding a migration never connects, so the connection string is only a placeholder; <c>dotnet ef database update</c>
/// is given the real one with <c>--connection</c>.
/// </summary>
public sealed class AutoEliteDbContextFactory : IDesignTimeDbContextFactory<AutoEliteDbContext>
{
    /// <inheritdoc />
    public AutoEliteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AutoEliteDbContext>().UseAutoElite("Host=localhost;Database=autoelite_design").Options);
}
