# Dialed

A macOS menu bar app for LeetCode prep: a problem timer, weekly targets, and a NeetCode 150 / Blind 75 checklist.

## Build

```sh
xcodebuild -project Dialed.xcodeproj -scheme Dialed -configuration Release -derivedDataPath build build
cp -R build/Build/Products/Release/Dialed.app /Applications/
```

Requires macOS 14+.

## Windows

The `windows/` folder holds a Windows version: a system tray app (C#, WPF) with the same timer, weekly targets, NeetCode checklist and stats.

```sh
dotnet publish windows -c Release -o windows/bin/publish
```

Run `windows/bin/publish/Dialed.exe`. Requires Windows 10 or 11 and the .NET 10 SDK to build. Data is saved in `%APPDATA%\Dialed\data.json`.
