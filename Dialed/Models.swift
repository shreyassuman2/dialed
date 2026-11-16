import Foundation
import SwiftData

enum Difficulty: String, CaseIterable, Identifiable {
    case easy, medium, hard
    var id: String { rawValue }
    /// Interview-length countdown for this difficulty.
    var minutes: Int {
        switch self {
        case .easy: return 20
        case .medium: return 35
        case .hard: return 45
        }
    }
}

let topicList = [
    "arrays/hashing", "two pointers", "sliding window", "stack",
    "binary search", "linked list", "trees", "tries",
    "heap", "backtracking", "graphs", "dp",
    "greedy", "intervals", "math/bit", "other",
]

/// The single row of long-term goals. Created by first-run setup.
@Model
final class Goals {
    var readyBy: Date
    var paceCapPerDay: Int
    var startingSolved: Int
    var totalProblems: Int
    // No longer used. Kept so existing data loads without a migration.
    var totalDesign: Int = 0
    var totalApps: Int = 0
    var totalMocks: Int = 0
    /// Week (its Monday) in which the user dismissed the unrealistic-pace line.
    var paceDismissedWeek: Date?
    /// True once the "date has passed" prompt was dismissed.
    var datePassedDismissed: Bool = false

    init(readyBy: Date, paceCapPerDay: Int, startingSolved: Int, totalProblems: Int) {
        self.readyBy = readyBy
        self.paceCapPerDay = paceCapPerDay
        self.startingSolved = startingSolved
        self.totalProblems = totalProblems
    }
}

@Model
final class Entry {
    var date: Date
    /// Always "problems" now. Older rows may hold other kinds; those are ignored.
    var kindRaw: String
    // Stored so a topic breakdown can be added later.
    var difficultyRaw: String?
    var topic: String?
    var secondsTaken: Int?
    /// LeetCode slug when this entry ticks off a NeetCode 150 problem.
    var neetcode: String?
    /// Solved before the user started tracking: counts toward totals and NeetCode
    /// progress, but not toward any week.
    var prior: Bool = false

    init(date: Date, difficulty: Difficulty, topic: String, secondsTaken: Int? = nil, neetcode: String? = nil) {
        self.date = date
        self.kindRaw = "problems"
        self.difficultyRaw = difficulty.rawValue
        self.topic = topic
        self.secondsTaken = secondsTaken
        self.neetcode = neetcode
    }
}

/// One row per week. Stores only the targets, so past weeks keep the target they had.
@Model
final class Week {
    var startDate: Date
    var targetProblems: Int
    // No longer used. Kept so existing data loads without a migration.
    var targetDesign: Int = 0
    var targetApps: Int = 0
    var targetMocks: Int = 0
    var edited: Bool = false

    init(startDate: Date, targetProblems: Int) {
        self.startDate = startDate
        self.targetProblems = targetProblems
    }
}

/// The running countdown. Kept in UserDefaults so it survives a restart.
struct ActiveTimer: Codable, Equatable {
    var startedAt: Date
    var difficultyRaw: String
    var durationSeconds: Int
    /// NeetCode problem being timed, if started from the NeetCode list.
    var neetcode: String? = nil

    var endsAt: Date { startedAt.addingTimeInterval(TimeInterval(durationSeconds)) }
    var difficulty: Difficulty { Difficulty(rawValue: difficultyRaw) ?? .medium }
}

/// A finished timer that has not been logged or discarded yet.
struct PendingLog: Codable, Equatable {
    var difficultyRaw: String
    var secondsTaken: Int
    var finishedAt: Date
    var neetcode: String? = nil

    var difficulty: Difficulty { Difficulty(rawValue: difficultyRaw) ?? .medium }
}
