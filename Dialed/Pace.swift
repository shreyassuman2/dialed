import Foundation

/// Pure date and pace arithmetic. Foundation only, so it can be tested without the app.
enum Pace {
    /// Gregorian calendar with weeks that start on Monday.
    static let calendar: Calendar = {
        var c = Calendar(identifier: .gregorian)
        c.firstWeekday = 2
        return c
    }()

    static func weekStart(_ date: Date) -> Date {
        calendar.dateInterval(of: .weekOfYear, for: date)!.start
    }

    static func addWeeks(_ n: Int, to date: Date) -> Date {
        calendar.date(byAdding: .day, value: 7 * n, to: date)!
    }

    static func daysLeft(now: Date, readyBy: Date) -> Int {
        let a = calendar.startOfDay(for: now)
        let b = calendar.startOfDay(for: readyBy)
        return max(0, calendar.dateComponents([.day], from: a, to: b).day ?? 0)
    }

    static func isPast(now: Date, readyBy: Date) -> Bool {
        calendar.startOfDay(for: now) > calendar.startOfDay(for: readyBy)
    }

    /// Number of weeks left, counting the current week and the week that contains the date.
    static func weeksLeft(now: Date, readyBy: Date) -> Int {
        let days = calendar.dateComponents([.day], from: weekStart(now), to: weekStart(readyBy)).day ?? 0
        return max(1, days / 7 + 1)
    }

    /// ceil(remaining / weeksLeft), never negative.
    static func target(remaining: Int, weeksLeft: Int) -> Int {
        guard remaining > 0 else { return 0 }
        let w = max(1, weeksLeft)
        return (remaining + w - 1) / w
    }

    struct Fix: Equatable {
        /// Earliest ready-by date at which the weekly target fits under the cap.
        let newDate: Date
        /// Largest total the cap allows by the current date.
        let newTotal: Int
    }

    /// Returns the two fixes when the weekly target exceeds the cap, otherwise nil.
    /// `doneBeforeWeek` is the starting number plus everything logged before this week.
    static func fix(remaining: Int, weeksLeft: Int, capPerDay: Int, now: Date, doneBeforeWeek: Int) -> Fix? {
        let cap = capPerDay * 7
        guard cap > 0, target(remaining: remaining, weeksLeft: weeksLeft) > cap else { return nil }
        let weeksNeeded = (remaining + cap - 1) / cap
        let newDate = calendar.date(byAdding: .day, value: 7 * weeksNeeded - 1, to: weekStart(now))!
        return Fix(newDate: newDate, newTotal: doneBeforeWeek + cap * weeksLeft)
    }

    // MARK: Text drawing

    static func bar(done: Int, target: Int, width: Int = 12) -> String {
        let filled: Int
        if target <= 0 {
            filled = done > 0 ? width : 0
        } else {
            filled = min(width, Int((Double(done) / Double(target) * Double(width)).rounded(.down)))
        }
        return String(repeating: "█", count: filled) + String(repeating: "░", count: width - filled)
    }

    static func spark(done: Int, target: Int) -> String {
        let levels = ["▁", "▂", "▃", "▄", "▅", "▆", "▇", "█"]
        guard done > 0 else { return levels[0] }
        guard target > 0 else { return levels[7] }
        let ratio = min(1.0, Double(done) / Double(target))
        return levels[min(7, max(1, Int((ratio * 7).rounded(.down))))]
    }

    static func clock(_ seconds: Int) -> String {
        let s = max(0, seconds)
        return String(format: "%d:%02d", s / 60, s % 60)
    }
}
