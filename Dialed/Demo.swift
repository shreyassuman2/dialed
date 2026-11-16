import Foundation
import SwiftData

/// Sample data for screenshots. Used only when launched with DIALED_DEMO=1
/// (together with DIALED_DATA_DIR, so real data is never touched).
enum Demo {
    static func seedIfEmpty(_ context: ModelContext) {
        guard ((try? context.fetchCount(FetchDescriptor<Goals>())) ?? 0) == 0 else { return }
        let now = Date()
        let cal = Pace.calendar
        context.insert(Goals(readyBy: cal.date(byAdding: .day, value: 204, to: now)!,
                             paceCapPerDay: 4, startingSolved: 0, totalProblems: 150))

        var problems = NeetCode.all.makeIterator()
        let thisWeek = Pace.weekStart(now)
        for (i, done) in [5, 8, 9, 6, 8, 10].enumerated() {
            let start = Pace.addWeeks(i - 6, to: thisWeek)
            context.insert(Week(startDate: start, targetProblems: 8))
            for n in 0..<done {
                guard let p = problems.next() else { break }
                let date = cal.date(byAdding: .hour, value: 10 + n * 13, to: start)!
                // Two in three are timed, using 45-100% of the limit.
                let taken = n % 3 == 2 ? nil : p.difficulty.minutes * 60 * (45 + (i * 23 + n * 17) % 56) / 100
                context.insert(Entry(date: date, difficulty: p.difficulty, topic: NeetCode.topic(of: p.slug),
                                     secondsTaken: taken, neetcode: p.slug))
            }
        }
        try? context.save()
    }
}
