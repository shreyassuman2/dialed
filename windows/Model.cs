using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;

namespace Dialed;

public enum Difficulty { Easy, Medium, Hard }

public static class DifficultyExtensions
{
    /// Interview-length countdown for this difficulty.
    public static int Minutes(this Difficulty d) => d switch
    {
        Difficulty.Easy => 20,
        Difficulty.Medium => 35,
        _ => 45,
    };

    public static string Label(this Difficulty d) => d.ToString().ToLowerInvariant();
}

public static class Topics
{
    public static readonly string[] List =
    [
        "arrays/hashing", "two pointers", "sliding window", "stack",
        "binary search", "linked list", "trees", "tries",
        "heap", "backtracking", "graphs", "dp",
        "greedy", "intervals", "math/bit", "other",
    ];
}

/// The long-term goals. Created by first-run setup.
public class Goals
{
    public DateTime ReadyBy { get; set; }
    public int PaceCapPerDay { get; set; } = 4;
    public int StartingSolved { get; set; }
    public int TotalProblems { get; set; }
    /// Week (its Monday) in which the user dismissed the unrealistic-pace line.
    public DateTime? PaceDismissedWeek { get; set; }
    /// True once the "date has passed" prompt was dismissed.
    public bool DatePassedDismissed { get; set; }
}

public class Entry
{
    public DateTime Date { get; set; }
    public Difficulty Difficulty { get; set; }
    public string Topic { get; set; } = "other";
    public int? SecondsTaken { get; set; }
    /// LeetCode slug when this entry ticks off a NeetCode problem.
    public string? Neetcode { get; set; }
    /// Solved before the user started tracking: counts toward totals and NeetCode
    /// progress, but not toward any week.
    public bool Prior { get; set; }
}

/// One row per week. Stores only the target, so past weeks keep the target they had.
public class Week
{
    public DateTime StartDate { get; set; }
    public int TargetProblems { get; set; }
    public bool Edited { get; set; }
}

/// The running countdown. Saved so it survives a restart. Times are UTC.
public class ActiveTimer
{
    public DateTime StartedAt { get; set; }
    public Difficulty Difficulty { get; set; }
    public int DurationSeconds { get; set; }
    /// NeetCode problem being timed, if started from the NeetCode list.
    public string? Neetcode { get; set; }

    [JsonIgnore] public DateTime EndsAt => StartedAt.AddSeconds(DurationSeconds);
}

/// A finished timer that has not been logged or discarded yet.
public class PendingLog
{
    public Difficulty Difficulty { get; set; }
    public int SecondsTaken { get; set; }
    public DateTime FinishedAt { get; set; }
    public string? Neetcode { get; set; }
}

/// Everything that is saved to data.json.
public class Store
{
    public Goals? Goals { get; set; }
    public List<Entry> Entries { get; set; } = [];
    public List<Week> Weeks { get; set; } = [];
    public ActiveTimer? Active { get; set; }
    public PendingLog? Pending { get; set; }
    public string? LastTopic { get; set; }
    public string NcList { get; set; } = NeetCode.NeetCode150Key;
    public string Tab { get; set; } = "solve";
    /// Week whose Monday summary notification was already shown.
    public DateTime? SummaryShownWeek { get; set; }
}

/// Values edited on the setup screen.
public class GoalDraft
{
    public DateTime ReadyBy { get; set; } = DateTime.Today.AddDays(84);
    public int TotalProblems { get; set; } = 150;
    public int StartingSolved { get; set; }
    public int PaceCapPerDay { get; set; } = 4;
}

public sealed class AppModel
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string path;
    private readonly DispatcherTimer ticker;

    public Store S { get; }
    public DateTime Now { get; private set; } = DateTime.Now;

    /// State changed: the view should be drawn again.
    public event Action? Changed;
    /// Half a second passed on the running timer.
    public event Action? Ticked;
    /// The countdown reached zero while the app was running.
    public event Action<Difficulty>? TimerRanOut;

    public AppModel(string folder)
    {
        Directory.CreateDirectory(folder);
        path = Path.Combine(folder, "data.json");
        S = Load();
        ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
        ticker.Tick += (_, _) => Tick();
        Refresh();
        if (S.Active != null) ticker.Start();
    }

    private Store Load()
    {
        if (!File.Exists(path)) return new Store();
        try
        {
            return JsonSerializer.Deserialize<Store>(File.ReadAllText(path), Json) ?? new Store();
        }
        catch (JsonException)
        {
            // Keep the unreadable file instead of overwriting it with an empty one.
            File.Move(path, Path.Combine(Path.GetDirectoryName(path)!, $"data.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json"));
            return new Store();
        }
    }

    private void Save()
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(S, Json));
        File.Move(tmp, path, overwrite: true);
    }

    // Derived state

    public Goals? Goals => S.Goals;
    public IReadOnlyList<Entry> Entries => S.Entries;
    public ActiveTimer? Active => S.Active;
    public PendingLog? Pending => S.Pending;

    public DateTime WeekStart => Pace.WeekStart(Now);
    public Week? CurrentWeek => S.Weeks.LastOrDefault(w => w.StartDate == WeekStart);

    public int DaysLeft => Goals is { } g ? Pace.DaysLeft(Now, g.ReadyBy) : 0;
    public bool DatePassed => Goals is { } g && Pace.IsPast(Now, g.ReadyBy);

    public int Done(DateTime weekStarting)
    {
        var end = Pace.AddWeeks(1, weekStarting);
        return S.Entries.Count(e => !e.Prior && e.Date >= weekStarting && e.Date < end);
    }

    public int DoneThisWeek => Done(WeekStart);

    /// Everything counted toward the goal before `date`, including the starting number.
    public int DoneBefore(DateTime date) =>
        S.Entries.Count(e => e.Prior || e.Date < date) + (Goals?.StartingSolved ?? 0);

    public int TotalDone => DoneBefore(DateTime.MaxValue);

    /// Seconds left on the running timer, or null when no timer runs.
    public int? RemainingSeconds =>
        S.Active is { } a ? Math.Max(0, (int)Math.Ceiling((a.EndsAt - DateTime.UtcNow).TotalSeconds)) : null;

    public string LastTopic => S.LastTopic ?? Topics.List[0];

    /// The last 8 weeks, oldest first.
    public List<(DateTime Start, int Done, int Target)> History =>
        S.Weeks.TakeLast(8).Select(w => (w.StartDate, Done(w.StartDate), w.TargetProblems)).ToList();

    /// The two fixes when this week's target exceeds the cap and the line was not dismissed.
    public Pace.Fix? PaceFix
    {
        get
        {
            if (Goals is not { } g || DatePassed || g.PaceDismissedWeek == WeekStart) return null;
            int before = DoneBefore(WeekStart);
            return Pace.GetFix(g.TotalProblems - before, Pace.WeeksLeft(Now, g.ReadyBy), g.PaceCapPerDay, Now, before);
        }
    }

    public bool ShowDatePassedPrompt => Goals is { } g && DatePassed && !g.DatePassedDismissed;

    // Loading

    /// Re-reads the clock and creates this week's row if it does not exist yet.
    public void Refresh()
    {
        Now = DateTime.Now;
        S.Entries = S.Entries.OrderBy(e => e.Date).ToList();
        S.Weeks = S.Weeks.OrderBy(w => w.StartDate).ToList();
        if (Goals != null && CurrentWeek == null)
        {
            S.Weeks.Add(new Week { StartDate = WeekStart, TargetProblems = ComputedTarget() });
            Save();
        }
        ResolveExpiredTimer(notify: false);
        Changed?.Invoke();
    }

    /// Target for the current week. After the ready-by date, the last week's target is kept.
    private int ComputedTarget()
    {
        if (Goals is not { } g) return 0;
        if (DatePassed && S.Weeks.LastOrDefault(w => w.StartDate < WeekStart) is { } previous)
            return previous.TargetProblems;
        return Pace.Target(g.TotalProblems - DoneBefore(WeekStart), Pace.WeeksLeft(Now, g.ReadyBy));
    }

    private void RecomputeCurrentWeek()
    {
        if (CurrentWeek is not { } week) return;
        week.TargetProblems = ComputedTarget();
        week.Edited = false;
    }

    private void Commit()
    {
        Save();
        Refresh();
    }

    // Goals

    public GoalDraft Draft() => Goals is { } g
        ? new GoalDraft
        {
            ReadyBy = g.ReadyBy, TotalProblems = g.TotalProblems,
            StartingSolved = g.StartingSolved, PaceCapPerDay = g.PaceCapPerDay,
        }
        : new GoalDraft();

    public void SaveGoals(GoalDraft d)
    {
        bool firstRun = S.Goals == null;
        var g = S.Goals ??= new Goals();
        g.ReadyBy = d.ReadyBy.Date;
        g.TotalProblems = Math.Max(0, d.TotalProblems);
        g.StartingSolved = Math.Max(0, d.StartingSolved);
        g.PaceCapPerDay = Math.Max(1, d.PaceCapPerDay);
        g.PaceDismissedWeek = null;
        g.DatePassedDismissed = false;
        Now = DateTime.Now;
        // There is no "last week" to summarise in the week the goals are first set.
        if (firstRun) S.SummaryShownWeek = WeekStart;
        RecomputeCurrentWeek();
        Commit();
    }

    public void ApplyMoveDate(Pace.Fix fix)
    {
        if (Goals is not { } g) return;
        g.ReadyBy = fix.NewDate;
        RecomputeCurrentWeek();
        Commit();
    }

    public void ApplyLowerTotal(Pace.Fix fix)
    {
        if (Goals is not { } g) return;
        g.TotalProblems = fix.NewTotal;
        RecomputeCurrentWeek();
        Commit();
    }

    public void DismissPaceFix()
    {
        if (Goals is not { } g) return;
        g.PaceDismissedWeek = WeekStart;
        Commit();
    }

    public void DismissDatePassed()
    {
        if (Goals is not { } g) return;
        g.DatePassedDismissed = true;
        Commit();
    }

    public void AdjustTarget(int delta)
    {
        if (CurrentWeek is not { } week) return;
        week.TargetProblems = Math.Max(0, week.TargetProblems + delta);
        week.Edited = true;
        Commit();
    }

    // View settings that are remembered

    public void SetTab(string tab)
    {
        S.Tab = tab;
        Save();
    }

    public void SetNcList(string list)
    {
        S.NcList = list;
        Commit();
    }

    // Logging

    public void LogProblem(Difficulty difficulty, string topic, int? secondsTaken, string? neetcode = null)
    {
        S.Entries.Add(new Entry
        {
            Date = DateTime.Now, Difficulty = difficulty, Topic = topic,
            SecondsTaken = secondsTaken, Neetcode = neetcode,
        });
        S.LastTopic = topic;
        Commit();
    }

    // NeetCode

    public HashSet<string> SolvedNeetCode =>
        S.Entries.Where(e => e.Neetcode != null).Select(e => e.Neetcode!).ToHashSet();

    public int SolvedInList
    {
        get
        {
            var solved = SolvedNeetCode;
            return NeetCode.ProblemsOf(S.NcList).Count(p => solved.Contains(p.Slug));
        }
    }

    /// First unsolved problem of the chosen list, in NeetCode order.
    public NCProblem? NextNeetCode
    {
        get
        {
            var solved = SolvedNeetCode;
            return NeetCode.ProblemsOf(S.NcList).FirstOrDefault(p => !solved.Contains(p.Slug));
        }
    }

    /// Ticks a problem off, or unticks it. `prior` marks it as solved before tracking,
    /// so it does not count toward this week.
    public void ToggleNeetCode(NCProblem problem, bool prior)
    {
        if (S.Entries.RemoveAll(e => e.Neetcode == problem.Slug) == 0)
        {
            S.Entries.Add(new Entry
            {
                Date = DateTime.Now, Difficulty = problem.Difficulty,
                Topic = NeetCode.TopicOf(problem.Slug), Neetcode = problem.Slug, Prior = prior,
            });
        }
        Commit();
    }

    public bool CanUndo => S.Entries.Count > 0;

    /// Removes the most recent problem entry.
    public void Undo()
    {
        if (S.Entries.Count == 0) return;
        S.Entries.RemoveAt(S.Entries.Count - 1);
        Commit();
    }

    // Timer

    public void StartTimer(Difficulty difficulty, string? neetcode = null)
    {
        S.Active = new ActiveTimer
        {
            StartedAt = DateTime.UtcNow, Difficulty = difficulty,
            DurationSeconds = difficulty.Minutes() * 60, Neetcode = neetcode,
        };
        S.Pending = null;
        Save();
        ticker.Start();
        Changed?.Invoke();
    }

    /// Stops the timer early and leaves a pre-filled log entry.
    public void StopTimer()
    {
        if (S.Active is not { } timer) return;
        int taken = Math.Min(timer.DurationSeconds, Math.Max(0, (int)(DateTime.UtcNow - timer.StartedAt).TotalSeconds));
        FinishTimer(timer, taken, DateTime.UtcNow);
        Changed?.Invoke();
    }

    public void DiscardPending()
    {
        S.Pending = null;
        Save();
        Changed?.Invoke();
    }

    public void LogPending(string topic)
    {
        if (S.Pending is not { } p) return;
        S.Pending = null;
        LogProblem(p.Difficulty, topic, p.SecondsTaken, p.Neetcode);
    }

    /// If the timer ran out (also while the app was closed), turn it into a pending log entry.
    private bool ResolveExpiredTimer(bool notify)
    {
        if (S.Active is not { } timer || DateTime.UtcNow < timer.EndsAt) return false;
        FinishTimer(timer, timer.DurationSeconds, timer.EndsAt);
        if (notify) TimerRanOut?.Invoke(timer.Difficulty);
        return true;
    }

    private void FinishTimer(ActiveTimer timer, int secondsTaken, DateTime at)
    {
        S.Pending = new PendingLog
        {
            Difficulty = timer.Difficulty, SecondsTaken = secondsTaken,
            FinishedAt = at, Neetcode = timer.Neetcode,
        };
        S.Active = null;
        Save();
        ticker.Stop();
    }

    private void Tick()
    {
        Now = DateTime.Now;
        if (ResolveExpiredTimer(notify: true)) Changed?.Invoke();
        else Ticked?.Invoke();
    }

    // Weekly summary

    /// Returns the Monday summary once per week, from Monday 09:00 on. Null when there is
    /// nothing to show. The app must be running (it starts at login) for this to appear.
    public (string Title, string Body)? TakeWeeklySummary()
    {
        if (Goals == null || CurrentWeek is not { } week) return null;
        var start = WeekStart;
        if (S.SummaryShownWeek == start || DateTime.Now < start.AddHours(9)) return null;
        S.SummaryShownWeek = start;
        Save();
        var lastStart = Pace.AddWeeks(-1, start);
        if (S.Weeks.LastOrDefault(w => w.StartDate == lastStart) is not { } last) return null;
        return ($"Last week: {Done(lastStart)}/{last.TargetProblems} problems",
                $"This week: {week.TargetProblems} problems");
    }

    // Demo

    /// Sample data for screenshots. Used only with DIALED_DEMO=1 and DIALED_DATA_DIR,
    /// so real data is never touched.
    public void SeedDemoIfEmpty()
    {
        if (S.Goals != null) return;
        var now = DateTime.Now;
        S.Goals = new Goals { ReadyBy = now.Date.AddDays(204), PaceCapPerDay = 4, TotalProblems = 150 };
        var thisWeek = Pace.WeekStart(now);
        int next = 0;
        int[] counts = [5, 8, 9, 6, 8, 10];
        for (int i = 0; i < counts.Length; i++)
        {
            var start = Pace.AddWeeks(i - 6, thisWeek);
            S.Weeks.Add(new Week { StartDate = start, TargetProblems = 8 });
            for (int n = 0; n < counts[i] && next < NeetCode.All.Length; n++)
            {
                var p = NeetCode.All[next++];
                // Two in three are timed, using 45-100% of the limit.
                int? taken = n % 3 == 2 ? null : p.Difficulty.Minutes() * 60 * (45 + (i * 23 + n * 17) % 56) / 100;
                S.Entries.Add(new Entry
                {
                    Date = start.AddHours(10 + n * 13), Difficulty = p.Difficulty,
                    Topic = NeetCode.TopicOf(p.Slug), SecondsTaken = taken, Neetcode = p.Slug,
                });
            }
        }
        S.SummaryShownWeek = thisWeek;
        Commit();
    }
}
