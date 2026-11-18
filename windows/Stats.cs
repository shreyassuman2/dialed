namespace Dialed;

/// Counts and timings for one topic or one difficulty.
public class StatRow(string name)
{
    public string Name { get; } = name;
    /// Every entry, including quick logs and "solved before" ticks.
    public int Solved;
    /// Entries that came from a timer, so they have a time.
    public int Timed;
    public int Seconds;
    /// Sum of time taken divided by the timer limit, one term per timed entry.
    public double LimitShare;
    /// Timed entries where the countdown reached zero.
    public int RanOut;

    public int? AvgSeconds => Timed > 0 ? Seconds / Timed : null;
    /// Average share of the timer limit used, 0...1.
    public double? AvgShare => Timed > 0 ? LimitShare / Timed : null;
}

public static class Stats
{
    /// One row per name, in the given order. Entries whose key is not in `names` are skipped.
    public static List<StatRow> Rows(IEnumerable<Entry> entries, IEnumerable<string> names, Func<Entry, string> key)
    {
        var rows = names.Select(n => new StatRow(n)).ToList();
        var byName = rows.ToDictionary(r => r.Name);
        foreach (var entry in entries)
        {
            if (!byName.TryGetValue(key(entry), out var row)) continue;
            row.Solved++;
            if (entry.SecondsTaken is int taken)
            {
                int limit = entry.Difficulty.Minutes() * 60;
                row.Timed++;
                row.Seconds += taken;
                row.LimitShare += (double)taken / limit;
                if (taken >= limit) row.RanOut++;
            }
        }
        return rows;
    }

    /// Up to three rows that use the largest share of the limit. Needs two timed entries to count.
    public static List<StatRow> Slowest(IEnumerable<StatRow> rows) =>
        rows.Where(r => r.Timed >= 2).OrderByDescending(r => r.AvgShare ?? 0).Take(3).ToList();

    /// Up to three rows with the fewest entries. "other" is not a topic to practise.
    public static List<StatRow> LeastDone(IEnumerable<StatRow> rows) =>
        rows.Where(r => r.Name != "other").OrderBy(r => r.Solved).Take(3).ToList();
}
