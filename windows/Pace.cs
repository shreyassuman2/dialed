namespace Dialed;

/// Pure date and pace arithmetic. Weeks start on Monday.
public static class Pace
{
    public static DateTime WeekStart(DateTime date)
    {
        var d = date.Date;
        return d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
    }

    public static DateTime AddWeeks(int n, DateTime date) => date.AddDays(7 * n);

    public static int DaysLeft(DateTime now, DateTime readyBy) => Math.Max(0, (readyBy.Date - now.Date).Days);

    public static bool IsPast(DateTime now, DateTime readyBy) => now.Date > readyBy.Date;

    /// Number of weeks left, counting the current week and the week that contains the date.
    public static int WeeksLeft(DateTime now, DateTime readyBy) =>
        Math.Max(1, (WeekStart(readyBy) - WeekStart(now)).Days / 7 + 1);

    /// ceil(remaining / weeksLeft), never negative.
    public static int Target(int remaining, int weeksLeft)
    {
        if (remaining <= 0) return 0;
        int w = Math.Max(1, weeksLeft);
        return (remaining + w - 1) / w;
    }

    /// NewDate: earliest ready-by date at which the weekly target fits under the cap.
    /// NewTotal: largest total the cap allows by the current date.
    public record Fix(DateTime NewDate, int NewTotal);

    /// Returns the two fixes when the weekly target exceeds the cap, otherwise null.
    /// doneBeforeWeek is the starting number plus everything logged before this week.
    public static Fix? GetFix(int remaining, int weeksLeft, int capPerDay, DateTime now, int doneBeforeWeek)
    {
        int cap = capPerDay * 7;
        if (cap <= 0 || Target(remaining, weeksLeft) <= cap) return null;
        int weeksNeeded = (remaining + cap - 1) / cap;
        return new Fix(WeekStart(now).AddDays(7 * weeksNeeded - 1), doneBeforeWeek + cap * weeksLeft);
    }

    // Text drawing

    public static string Clock(int seconds)
    {
        int s = Math.Max(0, seconds);
        return $"{s / 60}:{s % 60:00}";
    }
}
