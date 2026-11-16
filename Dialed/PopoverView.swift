import AppKit
import SwiftUI

struct PopoverView: View {
    let model: AppModel

    private enum Screen { case main, quickLog, setup }
    @State private var screen: Screen = .main

    var body: some View {
        Group {
            if model.goals == nil || screen == .setup {
                SetupView(model: model, firstRun: model.goals == nil) { screen = .main }
            } else if let pending = model.pending {
                LogView(model: model, pending: pending) { screen = .main }
            } else if screen == .quickLog {
                LogView(model: model, pending: nil) { screen = .main }
            } else {
                MainView(model: model, onQuickLog: { screen = .quickLog }, onEditGoals: { screen = .setup })
            }
        }
        .font(Style.font)
        .padding(16)
        .frame(width: Style.width, alignment: .leading)
        .onAppear { model.refresh() }
        .onReceive(NotificationCenter.default.publisher(for: NSWindow.didBecomeKeyNotification)) { _ in
            model.refresh()
        }
    }
}

// MARK: - Main

private struct MainView: View {
    let model: AppModel
    let onQuickLog: () -> Void
    let onEditGoals: () -> Void

    private enum Tab: String { case solve, week, neetcode }
    @AppStorage("tab") private var tab: Tab = .solve
    @State private var editing = false

    private static let dateFormat: DateFormatter = {
        let f = DateFormatter()
        f.calendar = Pace.calendar
        f.dateFormat = "yyyy-MM-dd"
        return f
    }()

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            tabBar
            switch tab {
            case .solve:
                timerSection
                logSection
            case .week:
                header
                prompts
                weekSection
                historySection
            case .neetcode:
                NeetCodeView(model: model) { tab = .solve }
            }
            footer
        }
    }

    private var tabBar: some View {
        HStack(spacing: 8) {
            tabButton(.solve, "solve")
            tabButton(.week, "week")
            tabButton(.neetcode, "neetcode")
        }
    }

    private func tabButton(_ t: Tab, _ label: String) -> some View {
        Button(label) { tab = t }
            .buttonStyle(TermButtonStyle(selected: tab == t))
            .monospacedDigit()
    }

    private var header: some View {
        HStack(alignment: .bottom) {
            HStack(alignment: .firstTextBaseline, spacing: 8) {
                Text("\(model.daysLeft)")
                    .font(Style.big)
                    .foregroundStyle(model.datePassed ? Color.secondary : Color.primary)
                Text(model.daysLeft == 1 ? "DAY LEFT" : "DAYS LEFT")
                    .font(Style.small.weight(.semibold))
                    .tracking(1.5)
                    .foregroundStyle(.secondary)
            }
            Spacer()
            if let goals = model.goals {
                VStack(alignment: .trailing, spacing: 2) {
                    Text("READY BY")
                        .font(Style.small.weight(.semibold))
                        .tracking(1.5)
                        .foregroundStyle(.secondary)
                    Text(Self.dateFormat.string(from: goals.readyBy))
                }
                .padding(.bottom, 8)
            }
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 8)
        .overlay(RoundedRectangle(cornerRadius: 8).strokeBorder(Color.secondary.opacity(0.35), lineWidth: 1))
    }

    @ViewBuilder
    private var prompts: some View {
        if let fix = model.paceFix, let goals = model.goals, let week = model.currentWeek {
            VStack(alignment: .leading, spacing: 6) {
                Text("pace \(week.targetProblems)/wk is over your cap of \(goals.paceCapPerDay * 7)/wk")
                    .foregroundStyle(Style.accent)
                Button("move date to \(Self.dateFormat.string(from: fix.newDate))") { model.applyMoveDate(fix) }
                    .buttonStyle(TermButtonStyle())
                HStack(spacing: 8) {
                    Button("lower total to \(fix.newTotal)") { model.applyLowerTotal(fix) }
                        .buttonStyle(TermButtonStyle())
                    Button("dismiss") { model.dismissPaceFix() }
                        .buttonStyle(LinkButtonStyle())
                }
            }
        }
        if model.showDatePassedPrompt {
            HStack(spacing: 8) {
                Text("ready-by date has passed").foregroundStyle(Style.accent)
                Button("set new date") { onEditGoals() }.buttonStyle(TermButtonStyle())
                Button("dismiss") { model.dismissDatePassed() }.buttonStyle(LinkButtonStyle())
            }
        }
    }

    private var weekSection: some View {
        let done = model.doneThisWeek
        let target = model.currentWeek?.targetProblems ?? 0
        return VStack(alignment: .leading, spacing: 5) {
            Text("this week").foregroundStyle(.secondary)
            HStack(spacing: 0) {
                Text(Pace.bar(done: done, target: target, width: 20))
                    .foregroundStyle(target > 0 && done >= target ? Style.accent : Color.primary)
                Button {
                    editing.toggle()
                } label: {
                    Text("  \(done)/\(target)")
                }
                .buttonStyle(LinkButtonStyle())
                .help("Click to change this week's target")
                if editing {
                    Button(" [-]") { model.adjustTarget(by: -1) }.buttonStyle(LinkButtonStyle())
                    Button(" [+]") { model.adjustTarget(by: 1) }.buttonStyle(LinkButtonStyle())
                }
            }
            if let goals = model.goals {
                Text("total     \(model.totalDone)/\(goals.totalProblems) problems")
                    .foregroundStyle(.secondary)
                    .padding(.top, 4)
            }
        }
    }

    private var timerSection: some View {
        VStack(alignment: .leading, spacing: 5) {
            if let seconds = model.remainingSeconds, let active = model.active {
                HStack(alignment: .firstTextBaseline, spacing: 10) {
                    Text(Pace.clock(seconds))
                        .font(.system(size: 24, weight: .bold, design: .monospaced))
                        .foregroundStyle(Style.accent)
                    Text(active.neetcode.flatMap(NeetCode.problem)?.name ?? active.difficulty.rawValue)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                    Spacer()
                    Button("stop") { model.stopTimer() }.buttonStyle(TermButtonStyle())
                }
            } else {
                HStack(spacing: 8) {
                    ForEach(Difficulty.allCases) { d in
                        Button("\(d.rawValue) \(d.minutes)") { model.startTimer(d) }
                            .buttonStyle(TermButtonStyle())
                    }
                }
            }
        }
    }

    private var logSection: some View {
        VStack(alignment: .leading, spacing: 5) {
            Button("+ problem") { onQuickLog() }
                .buttonStyle(TermButtonStyle())
        }
    }

    private var historySection: some View {
        HStack(spacing: 0) {
            Text("last 8 weeks  ").foregroundStyle(.secondary)
            ForEach(model.history, id: \.start) { week in
                Text(Pace.spark(done: week.done, target: week.target))
                    .foregroundStyle(week.target > 0 && week.done >= week.target ? Style.accent : Color.primary)
                    .help("Week of \(Self.dateFormat.string(from: week.start)): \(week.done)/\(week.target) problems")
            }
        }
    }

    private var footer: some View {
        VStack(alignment: .leading, spacing: 8) {
            Divider()
            HStack(spacing: 0) {
                Button("undo") { model.undo() }
                    .buttonStyle(LinkButtonStyle())
                    .disabled(!model.canUndo)
                    .help("Remove the most recent entry")
                Text(" · ").foregroundStyle(.secondary)
                Button("edit goals") { onEditGoals() }.buttonStyle(LinkButtonStyle())
                Spacer()
                Button("quit") { NSApplication.shared.terminate(nil) }.buttonStyle(LinkButtonStyle())
            }
        }
    }
}

// MARK: - Log a problem

private struct LogView: View {
    let model: AppModel
    /// Set when the entry comes from a finished timer. Difficulty and time are then fixed.
    let pending: PendingLog?
    let onDone: () -> Void

    @State private var difficulty: Difficulty = .medium

    private let columns = [GridItem(.flexible(), alignment: .leading), GridItem(.flexible(), alignment: .leading)]

    var body: some View {
        if let pending, let problem = pending.neetcode.flatMap(NeetCode.problem) {
            VStack(alignment: .leading, spacing: 12) {
                Text("log problem · \(pending.difficulty.rawValue) · \(Pace.clock(pending.secondsTaken))")
                Text(problem.name)
                HStack(spacing: 8) {
                    Button("solved it ⏎") { log(NeetCode.topic(of: problem.slug)) }
                        .buttonStyle(TermButtonStyle())
                        .keyboardShortcut(.defaultAction)
                    Spacer()
                    Button("discard") { model.discardPending(); onDone() }
                        .buttonStyle(LinkButtonStyle())
                        .keyboardShortcut(.cancelAction)
                }
            }
        } else {
            topicBody
        }
    }

    private var topicBody: some View {
        VStack(alignment: .leading, spacing: 12) {
            if let pending {
                Text("log problem · \(pending.difficulty.rawValue) · \(Pace.clock(pending.secondsTaken))")
            } else {
                Text("log problem")
                HStack(spacing: 8) {
                    ForEach(Difficulty.allCases) { d in
                        Button(d.rawValue) { difficulty = d }
                            .buttonStyle(TermButtonStyle(selected: d == difficulty))
                    }
                }
            }

            Text("topic").foregroundStyle(.secondary)
            LazyVGrid(columns: columns, alignment: .leading, spacing: 6) {
                ForEach(topicList, id: \.self) { topic in
                    Button(topic) { log(topic) }
                        .buttonStyle(TermButtonStyle(selected: topic == model.lastTopic))
                }
            }

            HStack(spacing: 8) {
                Button("log \(model.lastTopic) ⏎") { log(model.lastTopic) }
                    .buttonStyle(TermButtonStyle())
                    .keyboardShortcut(.defaultAction)
                Spacer()
                Button(pending == nil ? "cancel" : "discard") {
                    if pending != nil { model.discardPending() }
                    onDone()
                }
                .buttonStyle(LinkButtonStyle())
                .keyboardShortcut(.cancelAction)
            }
        }
    }

    private func log(_ topic: String) {
        if pending != nil {
            model.logPending(topic: topic)
        } else {
            model.logProblem(difficulty: difficulty, topic: topic, secondsTaken: nil)
        }
        onDone()
    }
}

// MARK: - Goals setup

private struct SetupView: View {
    let model: AppModel
    let firstRun: Bool
    let onDone: () -> Void

    @State private var draft = GoalDraft()
    @State private var showCalendar = false

    private static let dateFormat: DateFormatter = {
        let f = DateFormatter()
        f.calendar = Pace.calendar
        f.dateFormat = "yyyy-MM-dd"
        return f
    }()

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(firstRun ? "dialed · set your goals" : "edit goals")

            HStack(spacing: 0) {
                Text("ready by".padded(18)).foregroundStyle(.secondary)
                Button(Self.dateFormat.string(from: draft.readyBy)) { showCalendar.toggle() }
                    .buttonStyle(TermButtonStyle(selected: showCalendar))
                    .help("Pick a date")
                Button("  -1w") { shiftWeeks(-1) }.buttonStyle(LinkButtonStyle())
                Button("  +1w") { shiftWeeks(1) }.buttonStyle(LinkButtonStyle())
                Spacer(minLength: 0)
                Text("\(Pace.daysLeft(now: Date(), readyBy: draft.readyBy))d")
                    .foregroundStyle(.secondary)
            }
            if showCalendar {
                DatePicker("", selection: $draft.readyBy, in: Date()..., displayedComponents: .date)
                    .labelsHidden()
                    .datePickerStyle(.graphical)
                    .frame(maxWidth: .infinity, alignment: .center)
            }
            numberRow("problems total", $draft.totalProblems)
            numberRow("already solved", $draft.startingSolved)
            numberRow("max problems/day", $draft.paceCapPerDay)

            HStack(spacing: 8) {
                Button("save") {
                    model.saveGoals(draft)
                    onDone()
                }
                .buttonStyle(TermButtonStyle())
                .keyboardShortcut(.defaultAction)
                if !firstRun {
                    Button("cancel") { onDone() }
                        .buttonStyle(LinkButtonStyle())
                        .keyboardShortcut(.cancelAction)
                }
                Spacer()
                if firstRun {
                    Button("quit") { NSApplication.shared.terminate(nil) }.buttonStyle(LinkButtonStyle())
                }
            }
            .padding(.top, 4)
        }
        .onAppear { draft = model.draft }
    }

    private func shiftWeeks(_ n: Int) {
        let moved = Pace.addWeeks(n, to: draft.readyBy)
        draft.readyBy = max(moved, Pace.calendar.startOfDay(for: Date()))
    }

    private func numberRow(_ label: String, _ value: Binding<Int>) -> some View {
        HStack {
            Text(label.padded(18)).foregroundStyle(.secondary)
            TextField("", value: value, format: .number)
                .textFieldStyle(.roundedBorder)
                .frame(width: 80)
        }
    }
}

// MARK: - NeetCode 150

private struct NeetCodeView: View {
    let model: AppModel
    /// Called after a timer is started from the list.
    let onStartTimer: () -> Void

    @State private var open: Set<String> = []
    /// Ticks made while true are "solved before": they skip this week's count.
    @State private var prior = false

    var body: some View {
        let solved = model.solvedNeetCode
        let priorSolved = Set(model.entries.filter(\.prior).compactMap(\.neetcode))
        VStack(alignment: .leading, spacing: 10) {
            HStack(spacing: 8) {
                ForEach(NCList.allCases) { list in
                    Button(list.title) { model.ncList = list }
                        .buttonStyle(TermButtonStyle(selected: model.ncList == list))
                }
            }
            HStack(spacing: 0) {
                Text(Pace.bar(done: model.solvedInList, target: model.ncList.problems.count, width: 20))
                    .foregroundStyle(model.nextNeetCode == nil ? Style.accent : Color.primary)
                Text("  \(model.solvedInList)/\(model.ncList.problems.count)")
            }
            if let next = model.nextNeetCode {
                HStack(spacing: 6) {
                    Text("next").foregroundStyle(.secondary)
                    Button(next.name) { NSWorkspace.shared.open(next.url) }
                        .buttonStyle(LinkButtonStyle())
                        .help("Open on LeetCode")
                        .lineLimit(1)
                    Spacer(minLength: 0)
                    if model.active == nil {
                        Button("▶") { model.startTimer(next.difficulty, neetcode: next.slug); onStartTimer() }
                            .buttonStyle(LinkButtonStyle())
                            .help("Start a \(next.difficulty.minutes)-minute timer")
                    }
                }
            }
            HStack(spacing: 8) {
                Text("ticks are").foregroundStyle(.secondary)
                Button("new") { prior = false }
                    .buttonStyle(TermButtonStyle(selected: !prior))
                    .help("Counts toward this week")
                Button("solved before") { prior = true }
                    .buttonStyle(TermButtonStyle(selected: prior))
                    .help("Counts toward your total, not this week")
            }
            ScrollView {
                VStack(alignment: .leading, spacing: 4) {
                    ForEach(model.ncList.sections) { section in
                        sectionRow(section, solved: solved)
                        if open.contains(section.name) {
                            ForEach(section.problems) {
                                problemRow($0, solved: solved.contains($0.slug), before: priorSolved.contains($0.slug))
                            }
                                .padding(.leading, 14)
                                .padding(.bottom, 2)
                        }
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)
            }
            .frame(height: 300)
            Text("☐ tick · ☒ solved before · ▶ timer")
                .font(Style.small).foregroundStyle(.secondary)
        }
        // Start in "solved before" mode while nothing is ticked yet: that's the catch-up pass.
        .onAppear { prior = model.solvedNeetCode.isEmpty }
    }

    private func sectionRow(_ section: NCSection, solved: Set<String>) -> some View {
        let done = section.problems.filter { solved.contains($0.slug) }.count
        let total = section.problems.count
        let isOpen = open.contains(section.name)
        return Button {
            if isOpen { open.remove(section.name) } else { open.insert(section.name) }
        } label: {
            HStack(spacing: 0) {
                Text((isOpen ? "▾ " : "▸ ") + section.name.padded(22))
                Text(Pace.bar(done: done, target: total, width: 8))
                    .foregroundStyle(done >= total ? Style.accent : Color.primary)
                Text("  \(done)/\(total)").foregroundStyle(.secondary)
                Spacer(minLength: 0)
            }
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
    }

    private func problemRow(_ problem: NCProblem, solved: Bool, before: Bool) -> some View {
        HStack(spacing: 6) {
            Button(before ? "☒" : solved ? "☑" : "☐") { model.toggleNeetCode(problem, prior: prior) }
                .buttonStyle(LinkButtonStyle())
                .help(solved ? "Untick" : prior ? "Mark solved before (not this week)" : "Mark solved (counts this week)")
            Button(problem.name) { NSWorkspace.shared.open(problem.url) }
                .buttonStyle(LinkButtonStyle())
                .lineLimit(1)
                .help("Open on LeetCode")
            Spacer(minLength: 0)
            Text(String(problem.difficulty.rawValue.prefix(1)).uppercased())
                .foregroundStyle(problem.difficulty == .hard ? Style.accent : Color.secondary)
            if model.active == nil && !solved {
                Button("▶") { model.startTimer(problem.difficulty, neetcode: problem.slug); onStartTimer() }
                    .buttonStyle(LinkButtonStyle())
                    .help("Start a \(problem.difficulty.minutes)-minute timer")
            }
        }
    }
}
