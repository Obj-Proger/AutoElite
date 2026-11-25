namespace AutoElite.Application.Practice;

/// <summary>
/// The order in which a student goes through the questions of a practice: shuffled by a number the client chooses, the
/// same for the same number and questions, so that the client reads the practice in parts without the server keeping
/// any state. The shuffle needs no secrecy, so a small generator of its own (SplitMix64) is used, which gives the same
/// order on every server and after every restart.
/// </summary>
internal static class PracticeOrder
{
    /// <summary>Shuffles questions by a seed with the Fisher–Yates algorithm.</summary>
    /// <param name="questionIds">The questions, in an order that does not change between requests.</param>
    /// <param name="seed">The number the client chose for the practice.</param>
    /// <returns>The questions, shuffled.</returns>
    public static IReadOnlyList<Guid> Shuffle(IReadOnlyList<Guid> questionIds, int seed)
    {
        var order = questionIds.ToArray();
        var state = unchecked((ulong)seed);
        for (var last = order.Length - 1; last > 0; last--)
        {
            var other = (int)(Next(ref state) % (ulong)(last + 1));
            (order[last], order[other]) = (order[other], order[last]);
        }

        return order;
    }

    private static ulong Next(ref ulong state)
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15;
            var value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EB;

            return value ^ (value >> 31);
        }
    }
}
