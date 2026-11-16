# Lock In — spec

A macOS menu bar app for CS interview prep. It counts down to a self-set ready-by date, turns long-term count goals into a weekly target, and makes logging a LeetCode problem take about three seconds.

## Principles

These come from what killed past trackers: broken streaks, upkeep, and tracking without direction.

- **No daily streak.** The week is the unit. A bad day costs nothing if the week lands.
- **No required upkeep.** Every recurring step is auto-filled. The user edits only when they disagree.
- **Direction is pace math only.** The app says how many, never which problem.
- **One popover.** No main window, no settings pane in v1.

## Platform and stack

- Native SwiftUI, `MenuBarExtra` with `.window` style. macOS 14+.
- SwiftData, local only. No sync, no export, no account.
- No Dock icon (`LSUIElement`). Launch at login is on by default (`SMAppService`).
- Personal use: built and run from Xcode. No notarization or distribution work in v1.

## Menu bar item

| State | Shows |
|---|---|
| Timer running | Remaining time, e.g. `18:42` |
| Otherwise | A small monochrome icon |

Nothing else: no badge, no behind-pace dot.

## Goals

Four categories. Each long-term goal is a count to reach by the ready-by date.

| Category | Pace math | Logging |
|---|---|---|
| LeetCode problems | Yes, with pace cap | Difficulty + topic |
| System design / CS concepts | Simple division only | +1 |
| Applications | Simple division only | +1 |
| Mock interviews | Simple division only | +1 |

### First-run setup

One screen, shown in the popover:

- Ready-by date
- Problem goal total, and problems already solved (a single starting number, no per-problem detail)
- Totals for the three counters (0 hides that counter)
- Pace cap: maximum problems per day (default 4)

The same screen is reachable later from a small `edit goals` link. This is the only configuration in v1.

### Weekly target

- Weeks start Monday.
- At the start of each week: `target = ceil(remaining / weeksLeft)`, where `remaining = total − starting − logged` and `weeksLeft` counts the current week.
- The target is fixed for the week, so it does not move while the user works.
- The same formula applies to each counter.
- Each week's targets are editable by clicking the number. Editing is optional.
- A missed week is not carried over as a debt. The next Monday's recalculation absorbs it.

### Unrealistic pace

Checked on Monday, and whenever goals are edited. If the weekly problem target exceeds `paceCap × 7`, the popover shows one line with two fixes, each pre-calculated:

- **Move the date** to the earliest date where the pace equals the cap.
- **Lower the total** to what the cap allows by the current date.

One click applies either. The line can be dismissed and returns the next Monday if still true. The counters never trigger this.

### After the ready-by date

The countdown shows `0d` and the app keeps working with the last weekly target. It prompts once for a new date.

## Popover layout

Monochrome terminal look: monospaced type, black and white following system light/dark, one accent colour, text-drawn bars. Top to bottom:

```
47d                          ready by 2027-01-01

this week   problems  ████████░░░░   8/12
            design    █░            1/2
            apps      ███░░         3/5
            mocks     ░             0/1

total       70/150 problems

[ easy 20 ] [ medium 35 ] [ hard 45 ]     timer
[ + problem ] [ + design ] [ + app ] [ + mock ]

last 8 weeks  ▃▅▇▇▂▇▆▅        undo · edit goals
```

- The days-left number is the largest element.
- The last-8-weeks row is one bar per week of problems logged, marked against that week's target. This is the only history view.

## Timer

An interview-length countdown, one per problem.

- Start by picking difficulty: easy 20 min, medium 35, hard 45. Fixed in v1.
- While running, the popover shows the remaining time and a `stop` button, and the menu bar shows the remaining time.
- **Stop before zero:** the log sheet opens with difficulty and time taken pre-filled. The user picks a topic and confirms, or discards.
- **Reaches zero:** one notification, and the timer stops. No overtime, no outcome prompt. The pre-filled log entry waits in the popover until confirmed or discarded.
- One timer at a time. The timer survives closing the popover and is restored from its start timestamp if the app restarts.

## Logging

Two paths to the same record:

1. **From the timer**, as above.
2. **Quick log** (`+ problem`): pick difficulty, pick topic, done. No time recorded.

Fields: difficulty and topic only. No problem name, no outcome.

Topic is a single choice from a fixed list: arrays/hashing, two pointers, sliding window, stack, binary search, linked list, trees, tries, heap, backtracking, graphs, DP, greedy, intervals, math/bit, other. The last-used topic is preselected.

The three counters are one click each, with no details.

**Undo** removes the most recent entry of any kind. Nothing else is editable.

## Notifications

Two only:

- Timer reached zero.
- Weekly summary, Monday 09:00: last week's result against target, and the new week's targets.

No daily reminders.

## Data model

```
Settings      readyBy: Date, paceCapPerDay: Int, startingSolved: Int,
              totals: [Category: Int]
Entry         id, date, category,
              difficulty?, topic?, secondsTaken?     // problems only
Week          startDate (Monday), targets: [Category: Int], edited: Bool
ActiveTimer   startedAt, difficulty, durationSeconds  // at most one
```

Progress is always derived from `Entry` rows. `Week` stores only the targets, so past weeks keep the target they actually had.

## Out of scope for v1

- Settings pane (timer lengths, week start, hotkey). Add only if a default becomes annoying.
- LeetCode sync or import, problem lists, spaced review, weak-topic hints
- Problem names, outcomes, notes
- Free-text goals and milestones
- Heatmap, topic breakdown, full entry list
- iCloud sync, export, iPhone app
- Classic pomodoro cycles and breaks

Topic and time taken are stored now even though no v1 screen reads them, so a topic breakdown can be added later without a migration.

## Decisions made without asking

These were not covered in the interview. Change any of them freely.

- The weekly target is frozen on Monday rather than recalculated daily.
- A timer that reaches zero leaves a pending log entry in the popover.
- Missed weeks are spread over the remaining weeks, not added to the next one.
- The weekly summary fires Monday 09:00.
- The fixed topic list above.
- macOS 14 minimum.
