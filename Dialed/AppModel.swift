import Foundation
import Observation
import SwiftData

/// Values edited on the setup screen.
struct GoalDraft {
    var readyBy: Date = Pace.calendar.date(byAdding: .day, value: 84, to: Date())!
    var totalProblems = 150
    var startingSolved = 0
    var paceCapPerDay = 4
}

@MainActor
@Observable
final class AppModel {
    private let context: ModelContext
    private let defaults = UserDefaults.standard
    private var ticker: Timer?

    private(set) var goals: Goals?
    private(set) var entries: [Entry] = []
    private(set) var weeks: [Week] = []
    private(set) var now = Date()
    private(set) var active: ActiveTimer?
    private(set) var pending: PendingLog?
    var ncList: NCList {
        didSet { defaults.set(ncList.rawValue, forKey: Keys.ncList) }
    }

    init(context: ModelContext) {
        self.context = context
        ncList = NCList(rawValue: UserDefaults.standard.string(forKey: Keys.ncList) ?? "") ?? .neetcode150
        active = load(ActiveTimer.self, key: Keys.active)
        pending = load(PendingLog.self, key: Keys.pending)
        refresh()
        if active != nil { startTicker() }
    }

    // MARK: Derived state

    var weekStart: Date { Pace.weekStart(now) }
    var currentWeek: Week? { weeks.last(where: { $0.startDate == weekStart }) }

    var daysLeft: Int {
        guard let goals else { return 0 }
        return Pace.daysLeft(now: now, readyBy: goals.readyBy)
    }

    var datePassed: Bool {
        guard let goals else { return false }
        return Pace.isPast(now: now, readyBy: goals.readyBy)
    }

    func done(weekStarting start: Date) -> Int {
        let end = Pace.addWeeks(1, to: start)
        return entries.filter { !$0.prior && $0.date >= start && $0.date < end }.count
    }

    var doneThisWeek: Int { done(weekStarting: weekStart) }

    /// Everything counted toward the goal before `date`, including the starting number.
    func doneBefore(_ date: Date) -> Int {
        entries.filter { $0.prior || $0.date < date }.count + (goals?.startingSolved ?? 0)
    }

    var totalDone: Int { doneBefore(.distantFuture) }

    /// Seconds left on the running timer, or nil when no timer runs.
    var remainingSeconds: Int? {
        guard let active else { return nil }
        return max(0, Int(active.endsAt.timeIntervalSince(now).rounded(.up)))
    }

    var lastTopic: String { defaults.string(forKey: Keys.lastTopic) ?? topicList[0] }

    /// The last 8 weeks, oldest first, as (week start, done, target) for problems.
    var history: [(start: Date, done: Int, target: Int)] {
        weeks.suffix(8).map { ($0.startDate, done(weekStarting: $0.startDate), $0.targetProblems) }
    }

    /// The two fixes when this week's problem target exceeds the cap and the line was not dismissed.
    var paceFix: Pace.Fix? {
        guard let goals, !datePassed, goals.paceDismissedWeek != weekStart else { return nil }
        let before = doneBefore(weekStart)
        return Pace.fix(remaining: goals.totalProblems - before,
                        weeksLeft: Pace.weeksLeft(now: now, readyBy: goals.readyBy),
                        capPerDay: goals.paceCapPerDay, now: now, doneBeforeWeek: before)
    }

    var showDatePassedPrompt: Bool {
        guard let goals else { return false }
        return datePassed && !goals.datePassedDismissed
    }

    // MARK: Loading

    /// Re-reads everything and creates this week's row if it does not exist yet.
    func refresh() {
        now = Date()
        goals = (try? context.fetch(FetchDescriptor<Goals>()))?.first
        // Only problem entries. Older design/app/mock entries are ignored.
        let problemsOnly = #Predicate<Entry> { $0.kindRaw == "problems" }
        entries = (try? context.fetch(FetchDescriptor<Entry>(predicate: problemsOnly, sortBy: [SortDescriptor(\.date)]))) ?? []
        weeks = (try? context.fetch(FetchDescriptor<Week>(sortBy: [SortDescriptor(\.startDate)]))) ?? []
        if goals != nil, currentWeek == nil {
            let w = Week(startDate: weekStart, targetProblems: computedTarget())
            context.insert(w)
            try? context.save()
            weeks.append(w)
        }
        resolveExpiredTimer()
        scheduleWeeklySummary()
    }

    /// Target for the current week. After the ready-by date, the last week's target is kept.
    private func computedTarget() -> Int {
        guard let goals else { return 0 }
        if datePassed, let previous = weeks.last(where: { $0.startDate < weekStart }) {
            return previous.targetProblems
        }
        return Pace.target(remaining: goals.totalProblems - doneBefore(weekStart),
                           weeksLeft: Pace.weeksLeft(now: now, readyBy: goals.readyBy))
    }

    private func recomputeCurrentWeek() {
        guard let week = currentWeek else { return }
        week.targetProblems = computedTarget()
        week.edited = false
    }

    // MARK: Goals

    var draft: GoalDraft {
        guard let goals else { return GoalDraft() }
        return GoalDraft(readyBy: goals.readyBy, totalProblems: goals.totalProblems,
                         startingSolved: goals.startingSolved, paceCapPerDay: goals.paceCapPerDay)
    }

    func saveGoals(_ d: GoalDraft) {
        let g = goals ?? {
            let new = Goals(readyBy: d.readyBy, paceCapPerDay: 4, startingSolved: 0, totalProblems: 0)
            context.insert(new)
            return new
        }()
        g.readyBy = d.readyBy
        g.totalProblems = max(0, d.totalProblems)
        g.startingSolved = max(0, d.startingSolved)
        g.paceCapPerDay = max(1, d.paceCapPerDay)
        g.paceDismissedWeek = nil
        g.datePassedDismissed = false
        goals = g
        now = Date()
        recomputeCurrentWeek()
        try? context.save()
        refresh()
    }

    func applyMoveDate(_ fix: Pace.Fix) {
        guard let goals else { return }
        goals.readyBy = fix.newDate
        recomputeCurrentWeek()
        try? context.save()
        refresh()
    }

    func applyLowerTotal(_ fix: Pace.Fix) {
        guard let goals else { return }
        goals.totalProblems = fix.newTotal
        recomputeCurrentWeek()
        try? context.save()
        refresh()
    }

    func dismissPaceFix() {
        goals?.paceDismissedWeek = weekStart
        try? context.save()
    }

    func dismissDatePassed() {
        goals?.datePassedDismissed = true
        try? context.save()
    }

    func adjustTarget(by delta: Int) {
        guard let week = currentWeek else { return }
        week.targetProblems = max(0, week.targetProblems + delta)
        week.edited = true
        try? context.save()
        scheduleWeeklySummary()
    }

    // MARK: Logging

    func logProblem(difficulty: Difficulty, topic: String, secondsTaken: Int?, neetcode: String? = nil) {
        context.insert(Entry(date: Date(), difficulty: difficulty, topic: topic,
                             secondsTaken: secondsTaken, neetcode: neetcode))
        defaults.set(topic, forKey: Keys.lastTopic)
        try? context.save()
        refresh()
    }

    // MARK: NeetCode 150

    var solvedNeetCode: Set<String> { Set(entries.compactMap(\.neetcode)) }

    var solvedInList: Int {
        let solved = solvedNeetCode
        return ncList.problems.filter { solved.contains($0.slug) }.count
    }

    /// First unsolved problem of the chosen list, in NeetCode order.
    var nextNeetCode: NCProblem? {
        let solved = solvedNeetCode
        return ncList.problems.first { !solved.contains($0.slug) }
    }

    /// Ticks a problem off, or unticks it. `prior` marks it as solved before tracking,
    /// so it does not count toward this week.
    func toggleNeetCode(_ problem: NCProblem, prior: Bool) {
        let matching = entries.filter { $0.neetcode == problem.slug }
        if matching.isEmpty {
            let entry = Entry(date: Date(), difficulty: problem.difficulty,
                              topic: NeetCode.topic(of: problem.slug), neetcode: problem.slug)
            entry.prior = prior
            context.insert(entry)
            try? context.save()
            refresh()
        } else {
            matching.forEach(context.delete)
            try? context.save()
            refresh()
        }
    }

    var canUndo: Bool { !entries.isEmpty }

    /// Removes the most recent problem entry.
    func undo() {
        guard let last = entries.last else { return }
        context.delete(last)
        try? context.save()
        refresh()
    }

    // MARK: Timer

    func startTimer(_ difficulty: Difficulty, neetcode: String? = nil) {
        let timer = ActiveTimer(startedAt: Date(), difficultyRaw: difficulty.rawValue,
                                durationSeconds: difficulty.minutes * 60, neetcode: neetcode)
        active = timer
        pending = nil
        store(timer, key: Keys.active)
        store(PendingLog?.none, key: Keys.pending)
        now = Date()
        Notifier.scheduleTimerEnd(at: timer.endsAt, difficulty: difficulty)
        startTicker()
    }

    /// Stops the timer early and leaves a pre-filled log entry.
    func stopTimer() {
        guard let timer = active else { return }
        Notifier.cancelTimerEnd()
        let taken = min(timer.durationSeconds, max(0, Int(Date().timeIntervalSince(timer.startedAt))))
        finishTimer(timer, secondsTaken: taken, at: Date())
    }

    func discardPending() {
        pending = nil
        store(PendingLog?.none, key: Keys.pending)
    }

    func logPending(topic: String) {
        guard let p = pending else { return }
        discardPending()
        logProblem(difficulty: p.difficulty, topic: topic, secondsTaken: p.secondsTaken, neetcode: p.neetcode)
    }

    /// If the timer ran out (also while the app was closed), turn it into a pending log entry.
    private func resolveExpiredTimer() {
        guard let timer = active, Date() >= timer.endsAt else { return }
        finishTimer(timer, secondsTaken: timer.durationSeconds, at: timer.endsAt)
    }

    private func finishTimer(_ timer: ActiveTimer, secondsTaken: Int, at date: Date) {
        let p = PendingLog(difficultyRaw: timer.difficultyRaw, secondsTaken: secondsTaken,
                           finishedAt: date, neetcode: timer.neetcode)
        active = nil
        pending = p
        store(ActiveTimer?.none, key: Keys.active)
        store(p, key: Keys.pending)
        stopTicker()
    }

    private func startTicker() {
        stopTicker()
        let t = Timer(timeInterval: 0.5, repeats: true) { [weak self] _ in
            Task { @MainActor in self?.tick() }
        }
        RunLoop.main.add(t, forMode: .common)
        ticker = t
    }

    private func stopTicker() {
        ticker?.invalidate()
        ticker = nil
    }

    private func tick() {
        now = Date()
        resolveExpiredTimer()
    }

    // MARK: Weekly summary

    /// Schedules next Monday 09:00 with last week's result and the new week's targets.
    private func scheduleWeeklySummary() {
        guard let goals, let week = currentWeek else { Notifier.cancelWeekly(); return }
        let nextStart = Pace.addWeeks(1, to: weekStart)
        let fireAt = Pace.calendar.date(byAdding: .hour, value: 9, to: nextStart)!
        let next = Pace.isPast(now: nextStart, readyBy: goals.readyBy)
            ? week.targetProblems
            : Pace.target(remaining: goals.totalProblems - totalDone,
                          weeksLeft: max(1, Pace.weeksLeft(now: nextStart, readyBy: goals.readyBy)))
        Notifier.scheduleWeekly(at: fireAt, title: "Last week: \(doneThisWeek)/\(week.targetProblems) problems",
                                body: "This week: \(next) problems")
    }

    // MARK: UserDefaults helpers

    private enum Keys {
        static let active = "activeTimer"
        static let pending = "pendingLog"
        static let lastTopic = "lastTopic"
        static let ncList = "neetcodeList"
    }

    private func load<T: Decodable>(_ type: T.Type, key: String) -> T? {
        guard let data = defaults.data(forKey: key) else { return nil }
        return try? JSONDecoder().decode(type, from: data)
    }

    private func store<T: Encodable>(_ value: T?, key: String) {
        if let value, let data = try? JSONEncoder().encode(value) {
            defaults.set(data, forKey: key)
        } else {
            defaults.removeObject(forKey: key)
        }
    }
}
