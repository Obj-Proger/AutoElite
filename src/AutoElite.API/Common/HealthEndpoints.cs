namespace AutoElite.API.Common;

/// <summary>
/// The health endpoints, for a container orchestrator or a monitoring service: <see cref="Live"/> answers while the
/// process runs, <see cref="Ready"/> only while the database answers too.
/// </summary>
internal static class HealthEndpoints
{
    /// <summary>The endpoint that tells the process runs.</summary>
    public const string Live = "/health/live";

    /// <summary>The endpoint that tells the host can serve requests.</summary>
    public const string Ready = "/health/ready";

    /// <summary>The tag of the checks that <see cref="Ready"/> runs.</summary>
    public const string ReadyTag = "ready";
}
