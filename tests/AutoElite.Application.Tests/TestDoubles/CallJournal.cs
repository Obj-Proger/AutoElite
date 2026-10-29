namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class CallJournal
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public CancellationToken LastToken { get; set; }

    public void Add(string entry) => _entries.Add(entry);
}
