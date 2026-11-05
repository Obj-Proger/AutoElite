using AutoElite.Application.Analytics;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Records the document and the format and returns fixed bytes; the rendering is the infrastructure's job.</summary>
internal sealed class FakeReportRenderer : IReportRenderer
{
    public ReportDocument? LastDocument { get; private set; }

    public ReportFormat? LastFormat { get; private set; }

    public Task<byte[]> RenderAsync(ReportDocument document, ReportFormat format, CancellationToken cancellationToken = default)
    {
        LastDocument = document;
        LastFormat = format;

        return Task.FromResult<byte[]>([1, 2, 3]);
    }
}
