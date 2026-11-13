using System.Reflection;

namespace AutoElite.API.Hosting;

/// <summary>
/// The build of the API writes its OpenAPI document, from which the client of the API is generated: the build tool
/// (GetDocument.Insider) starts the host with a mock server and reads the document. The host then needs only what
/// describes the endpoints, not the database, the keys, the mail or the background jobs, which the build does not
/// have, so it leaves them out.
/// </summary>
internal static class OpenApiDocumentGeneration
{
    /// <summary>Gets a value indicating whether the host runs to write the OpenAPI document during the build.</summary>
    public static bool IsRunning { get; } = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
