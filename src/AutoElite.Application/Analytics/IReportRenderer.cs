namespace AutoElite.Application.Analytics;

/// <summary>Writes reports as files (FR-14.6); the infrastructure chooses the libraries.</summary>
public interface IReportRenderer
{
    /// <summary>Renders the report in the format.</summary>
    /// <param name="document">The report.</param>
    /// <param name="format">The file format.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The content of the file.</returns>
    Task<byte[]> RenderAsync(ReportDocument document, ReportFormat format, CancellationToken cancellationToken = default);
}
