# Dialed

A macOS menu bar app for LeetCode interview prep.

- Counts down to a ready-by date and turns your problem goal into a weekly target.
- Logs a problem in a few seconds: difficulty, topic, and an optional interview-length timer (easy 20 / medium 35 / hard 45 min).
- NeetCode 150 checklist by category, with a "next" problem, LeetCode links and per-problem timers.
- Last-8-weeks history and a Monday summary notification.

## Build

Requires macOS 14+ and Xcode.

```sh
xcodebuild -project Dialed.xcodeproj -scheme Dialed -configuration Debug -derivedDataPath build build
open build/Build/Products/Debug/Dialed.app
```

Data is stored locally in `~/Library/Application Support/Dialed/`.
