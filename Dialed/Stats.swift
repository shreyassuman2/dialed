import SwiftUI

/// Counts and timings per topic or per difficulty, from the logged entries.
enum Stats {
    struct Row: Identifiable {
        let name: String
        /// Every entry, including quick logs and "solved before" ticks.
        var solved = 0
        /// Entries that came from a timer, so they have a time.
        var timed = 0
        var seconds = 0
        /// Sum of time taken divided by the timer limit, one term per timed entry.
        var limitShare = 0.0
        /// Timed entries where the countdown reached zero.
        var ranOut = 0

        var id: String { name }
        var avgSeconds: Int? { timed > 0 ? seconds / timed : nil }
        /// Average share of the timer limit used, 0...1.
        var avgShare: Double? { timed > 0 ? limitShare / Double(timed) : nil }
    }

    /// One row per name, in the given order. Entries whose key is not in `names` are skipped.
    static func rows(_ entries: [Entry], names: [String], key: (Entry) -> String?) -> [Row] {
        var byName = Dictionary(uniqueKeysWithValues: names.map { ($0, Row(name: $0)) })
        for entry in entries {
            guard let name = key(entry), var row = byName[name] else { continue }
            row.solved += 1
            if let taken = entry.secondsTaken, let difficulty = Difficulty(rawValue: entry.difficultyRaw ?? "") {
                let limit = difficulty.minutes * 60
                row.timed += 1
                row.seconds += taken
                row.limitShare += Double(taken) / Double(limit)
                if taken >= limit { row.ranOut += 1 }
            }
            byName[name] = row
        }
        return names.compactMap { byName[$0] }
    }

    /// Up to three rows that use the largest share of the limit. Needs two timed entries to count.
    static func slowest(_ rows: [Row]) -> [Row] {
        rows.filter { $0.timed >= 2 }
            .sorted { ($0.avgShare ?? 0) > ($1.avgShare ?? 0) }
            .prefix(3)
            .map { $0 }
    }

    /// Up to three rows with the fewest entries. "other" is not a topic to practise.
    static func leastDone(_ rows: [Row]) -> [Row] {
        rows.filter { $0.name != "other" }
            .sorted { $0.solved < $1.solved }
            .prefix(3)
            .map { $0 }
    }
}

struct StatsView: View {
    let model: AppModel

    var body: some View {
        let topics = Stats.rows(model.entries, names: topicList) { $0.topic }
        let levels = Stats.rows(model.entries, names: Difficulty.allCases.map(\.rawValue)) { $0.difficultyRaw }
        let slow = Stats.slowest(topics)
        let timed = levels.reduce(0) { $0 + $1.timed }
        let ranOut = levels.reduce(0) { $0 + $1.ranOut }

        VStack(alignment: .leading, spacing: 10) {
            if model.entries.isEmpty {
                Text("nothing logged yet").foregroundStyle(.secondary)
            } else {
                VStack(alignment: .leading, spacing: 4) {
                    if !slow.isEmpty {
                        summary("slowest", slow)
                    }
                    summary("least done", Stats.leastDone(topics))
                }
                table("topic", topics, slow: Set(slow.map(\.name)))
                table("difficulty", levels, slow: [])
                if timed > 0 {
                    Text("timer ran out on \(ranOut) of \(timed) timed problems")
                        .foregroundStyle(.secondary)
                } else {
                    Text("avg and limit fill in once you log problems from the timer")
                        .font(Style.small).foregroundStyle(.secondary)
                }
            }
        }
    }

    private func summary(_ label: String, _ rows: [Stats.Row]) -> some View {
        HStack(alignment: .top, spacing: 0) {
            Text(label.padded(12)).foregroundStyle(.secondary)
            Text(rows.map(\.name).joined(separator: " · "))
                .fixedSize(horizontal: false, vertical: true)
        }
    }

    private func table(_ title: String, _ rows: [Stats.Row], slow: Set<String>) -> some View {
        let most = rows.map(\.solved).max() ?? 0
        return VStack(alignment: .leading, spacing: 3) {
            Text(title.padded(15) + "solved".padded(12) + "avg".leftPadded(7) + "limit".leftPadded(7))
                .foregroundStyle(.secondary)
                .help("avg: average time on timed problems. limit: share of the timer that average uses.")
            ForEach(rows) { row in
                HStack(spacing: 0) {
                    Text(row.name.padded(15))
                    Text(Pace.bar(done: row.solved, target: most, width: 8))
                    Text("\(row.solved)".leftPadded(4))
                    Text((row.avgSeconds.map(Pace.clock) ?? "—").leftPadded(7))
                        .foregroundStyle(row.timed > 0 ? Color.primary : Color.secondary)
                    Text((row.avgShare.map { "\(Int(($0 * 100).rounded()))%" } ?? "—").leftPadded(7))
                        .foregroundStyle(slow.contains(row.name) ? Style.accent
                                         : row.timed > 0 ? Color.primary : Color.secondary)
                }
                .foregroundStyle(row.solved > 0 ? Color.primary : Color.secondary)
                .help(row.timed > 0
                      ? "\(row.solved) solved, \(row.timed) timed, timer ran out on \(row.ranOut)"
                      : "\(row.solved) solved, none timed")
            }
        }
    }
}

private extension String {
    func leftPadded(_ length: Int) -> String {
        String(repeating: " ", count: max(0, length - count)) + self
    }
}
