using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Dialed;

/// The panel that opens above the tray icon. The whole view is rebuilt from the model
/// on every change; only the countdown text is updated in place.
public sealed class PopupWindow : Window
{
    private enum Screen { Main, QuickLog, Setup }

    private readonly AppModel model;
    private readonly Border root;
    private readonly DispatcherTimer quitReset;

    private Screen screen = Screen.Main;
    private bool editing;
    private bool confirmQuit;
    /// Ticks made while true are "solved before": they skip this week's count.
    private bool prior;
    private bool showCalendar;
    private Difficulty logDifficulty = Difficulty.Medium;
    private readonly HashSet<string> open = [];
    private GoalDraft? draft;

    private TextBlock? clockText;
    private ScrollViewer? listScroll;
    private double listOffset;
    private Action? defaultAction;
    private Action? cancelAction;

    /// When the panel was last hidden (UTC). The tray uses it to tell a closing click from an opening one.
    public DateTime HiddenAt { get; private set; } = DateTime.MinValue;
    public event Action? QuitRequested;

    public PopupWindow(AppModel model)
    {
        this.model = model;
        Title = "Dialed";
        Width = Theme.Width;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = true;
        FontFamily = Theme.Mono;
        FontSize = Theme.Size;
        UseLayoutRounding = true;

        root = new Border { Padding = new Thickness(16), BorderThickness = new Thickness(1) };
        Content = root;

        quitReset = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        quitReset.Tick += (_, _) =>
        {
            quitReset.Stop();
            confirmQuit = false;
            if (IsVisible) Render();
        };

        model.Changed += () => { if (IsVisible) Render(); };
        model.Ticked += () =>
        {
            if (clockText != null && model.RemainingSeconds is int s) clockText.Text = Pace.Clock(s);
        };
        Deactivated += (_, _) => HidePopup();
        SizeChanged += (_, _) => Reposition();
        SourceInitialized += (_, _) => ApplyWindowStyle();
        PreviewKeyDown += OnKey;
        // Closing the window (Alt+F4) only hides it. The app quits from the tray or the quit link.
        Closing += (_, e) => { e.Cancel = true; Dispatcher.BeginInvoke(new Action(HidePopup)); };
    }

    public void ShowPopup()
    {
        Theme.Load();
        editing = false;
        confirmQuit = false;
        if (model.S.Tab == "neetcode") prior = model.SolvedNeetCode.Count == 0;
        model.Refresh();
        Render();
        Show();
        Activate();
        Reposition();
    }

    public void HidePopup()
    {
        if (!IsVisible) return;
        HiddenAt = DateTime.UtcNow;
        Hide();
    }

    /// Bottom-right corner of the work area, next to the tray.
    private void Reposition()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 12;
        Top = area.Bottom - ActualHeight - 12;
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && defaultAction != null)
        {
            e.Handled = true;
            defaultAction();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (cancelAction != null) cancelAction();
            else HidePopup();
        }
    }

    // Rendering

    private void Render()
    {
        if (listScroll != null) listOffset = listScroll.VerticalOffset;
        clockText = null;
        listScroll = null;
        defaultAction = null;
        cancelAction = null;

        Background = Theme.Bg;
        Foreground = Theme.Primary;
        root.Background = Theme.Bg;
        root.BorderBrush = Theme.Line;

        if (model.Goals == null || screen == Screen.Setup) root.Child = SetupView(model.Goals == null);
        else if (model.Pending is { } pending) root.Child = LogView(pending);
        else if (screen == Screen.QuickLog) root.Child = LogView(null);
        else root.Child = MainView();
    }

    private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    // Main

    private FrameworkElement MainView()
    {
        var col = new VStack(14).Add(TabBar());
        switch (model.S.Tab)
        {
            case "week":
                col.Add(Header());
                col.Add(PaceFixPrompt());
                col.Add(DatePassedPrompt());
                col.Add(WeekSection());
                col.Add(HistorySection());
                break;
            case "neetcode":
                col.Add(NeetCodeView());
                break;
            case "stats":
                col.Add(StatsView());
                break;
            default:
                col.Add(TimerSection());
                col.Add(Ui.Term("+ problem", () => { screen = Screen.QuickLog; Render(); }));
                break;
        }
        return col.Add(Footer());
    }

    private void SelectTab(string tab)
    {
        model.SetTab(tab);
        editing = false;
        // Start in "solved before" mode while nothing is ticked yet: that's the catch-up pass.
        if (tab == "neetcode") prior = model.SolvedNeetCode.Count == 0;
        Render();
    }

    private FrameworkElement TabBar()
    {
        var tabs = new HStack(18);
        foreach (string tab in new[] { "solve", "week", "neetcode", "stats" })
        {
            bool selected = model.S.Tab == tab || (tab == "solve" && model.S.Tab is not ("week" or "neetcode" or "stats"));
            tabs.Add(Ui.Tab(tab, selected, () => SelectTab(tab)));
        }
        return new VStack().Add(tabs).Add(Ui.Divider());
    }

    private FrameworkElement Header()
    {
        var left = new HStack(8)
            .Add(Bottom(Ui.Text(model.DaysLeft.ToString(), model.DatePassed ? Theme.Secondary : Theme.Primary, Theme.Big, bold: true)))
            .Add(Bottom(Ui.Text(model.DaysLeft == 1 ? "DAY LEFT" : "DAYS LEFT", Theme.Secondary, Theme.Small, bold: true), 10));
        FrameworkElement? right = null;
        if (model.Goals is { } goals)
        {
            var date = Ui.Text(Day(goals.ReadyBy));
            date.HorizontalAlignment = HorizontalAlignment.Right;
            var label = Ui.Text("READY BY", Theme.Secondary, Theme.Small, bold: true);
            label.HorizontalAlignment = HorizontalAlignment.Right;
            right = Bottom(new VStack(2).Add(label).Add(date), 10);
        }
        return new Border
        {
            Child = Ui.Split(left, right),
            Padding = new Thickness(14, 4, 14, 4),
            BorderThickness = new Thickness(1),
            BorderBrush = Theme.Line,
            CornerRadius = new CornerRadius(8),
        };
    }

    private static FrameworkElement Bottom(FrameworkElement e, double lift = 0)
    {
        e.VerticalAlignment = VerticalAlignment.Bottom;
        e.Margin = new Thickness(0, 0, 0, lift);
        return e;
    }

    private FrameworkElement? PaceFixPrompt()
    {
        if (model.PaceFix is not { } fix || model.Goals is not { } goals || model.CurrentWeek is not { } week) return null;
        return new VStack(6)
            .Add(Ui.Text($"pace {week.TargetProblems}/wk is over your cap of {goals.PaceCapPerDay * 7}/wk", Theme.Accent))
            .Add(Ui.Term($"move date to {Day(fix.NewDate)}", () => model.ApplyMoveDate(fix)))
            .Add(new HStack(8)
                .Add(Ui.Term($"lower total to {fix.NewTotal}", () => model.ApplyLowerTotal(fix)))
                .Add(Ui.Link("dismiss", model.DismissPaceFix)));
    }

    private FrameworkElement? DatePassedPrompt()
    {
        if (!model.ShowDatePassedPrompt) return null;
        return new HStack(8)
            .Add(Ui.Text("ready-by date has passed", Theme.Accent))
            .Add(Ui.Term("set new date", EditGoals))
            .Add(Ui.Link("dismiss", model.DismissDatePassed));
    }

    private FrameworkElement WeekSection()
    {
        int done = model.DoneThisWeek;
        int target = model.CurrentWeek?.TargetProblems ?? 0;
        var row = new HStack()
            .Add(Ui.Line(Ui.Bar(done, target, 20, target > 0 && done >= target ? Theme.Accent : Theme.Primary)))
            .Add(Ui.Link($"  {done}/{target}", () => { editing = !editing; Render(); }, tip: "Click to change this week's target"));
        if (editing)
        {
            row.Add(Ui.Link(" [-]", () => model.AdjustTarget(-1)));
            row.Add(Ui.Link(" [+]", () => model.AdjustTarget(1)));
        }
        var col = new VStack(5).Add(Ui.Text("this week", Theme.Secondary)).Add(row);
        if (model.Goals is { } goals)
        {
            var total = Ui.Text($"total     {model.TotalDone}/{goals.TotalProblems} problems", Theme.Secondary);
            total.Margin = new Thickness(0, 4, 0, 0);
            col.Add(total);
        }
        return col;
    }

    private FrameworkElement TimerSection()
    {
        if (model.RemainingSeconds is int seconds && model.Active is { } active)
        {
            clockText = Ui.Text(Pace.Clock(seconds), Theme.Accent, 24, bold: true);
            var name = Ui.Text(NeetCode.Problem(active.Neetcode)?.Name ?? active.Difficulty.Label(), Theme.Secondary);
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            name.Margin = new Thickness(10, 0, 10, 0);
            name.VerticalAlignment = VerticalAlignment.Center;
            var stop = Ui.Term("stop", model.StopTimer);
            DockPanel.SetDock(clockText, Dock.Left);
            DockPanel.SetDock(stop, Dock.Right);
            stop.VerticalAlignment = VerticalAlignment.Center;
            var row = new DockPanel();
            row.Children.Add(clockText);
            row.Children.Add(stop);
            row.Children.Add(name);
            return row;
        }
        var buttons = new HStack(8);
        foreach (var d in Enum.GetValues<Difficulty>())
            buttons.Add(Ui.Term($"▶ {d.Label()} {d.Minutes()}m", () => model.StartTimer(d)));
        return buttons;
    }

    private FrameworkElement HistorySection()
    {
        var row = new HStack().Add(Ui.Text("last 8 weeks  ", Theme.Secondary));
        foreach (var week in model.History)
        {
            row.Add(Ui.Spark(week.Done, week.Target,
                week.Target > 0 && week.Done >= week.Target ? Theme.Accent : Theme.Primary,
                $"Week of {Day(week.Start)}: {week.Done}/{week.Target} problems"));
        }
        return row;
    }

    private FrameworkElement Footer()
    {
        var left = new HStack()
            .Add(Ui.Link("undo", model.Undo, enabled: model.CanUndo, tip: "Remove the most recent entry"))
            .Add(Ui.Text(" · ", Theme.Secondary))
            .Add(Ui.Link("edit goals", EditGoals));
        return new VStack(8).Add(Ui.Divider()).Add(Ui.Split(left, QuitButton()));
    }

    /// Two-step quit so a stray click doesn't close the app. Resets after 3 seconds.
    private FrameworkElement QuitButton()
    {
        if (!confirmQuit)
        {
            return Ui.Link("quit", () =>
            {
                confirmQuit = true;
                quitReset.Stop();
                quitReset.Start();
                Render();
            });
        }
        return new HStack()
            .Add(Ui.Text("quit? ", Theme.Secondary))
            .Add(Ui.Term("yes", () => QuitRequested?.Invoke()))
            .Add(Ui.Text(" · ", Theme.Secondary))
            .Add(Ui.Link("no", () => { confirmQuit = false; Render(); }));
    }

    private void EditGoals()
    {
        draft = null;
        showCalendar = false;
        screen = Screen.Setup;
        Render();
    }

    // Log a problem

    /// `pending` is set when the entry comes from a finished timer. Difficulty and time are then fixed.
    private FrameworkElement LogView(PendingLog? pending)
    {
        void Log(string topic)
        {
            screen = Screen.Main;
            if (pending != null) model.LogPending(topic);
            else model.LogProblem(logDifficulty, topic, null);
        }

        void Cancel()
        {
            screen = Screen.Main;
            if (pending != null) model.DiscardPending();
            else Render();
        }

        cancelAction = Cancel;
        var col = new VStack(12);

        if (pending != null && NeetCode.Problem(pending.Neetcode) is { } problem)
        {
            defaultAction = () => Log(NeetCode.TopicOf(problem.Slug));
            return col
                .Add(Ui.Text($"log problem · {pending.Difficulty.Label()} · {Pace.Clock(pending.SecondsTaken)}"))
                .Add(Ui.Text(problem.Name))
                .Add(Ui.Split(Ui.Term("solved it ⏎", defaultAction), Ui.Link("discard", Cancel)));
        }

        if (pending != null)
        {
            col.Add(Ui.Text($"log problem · {pending.Difficulty.Label()} · {Pace.Clock(pending.SecondsTaken)}"));
        }
        else
        {
            col.Add(Ui.Text("log problem"));
            var levels = new HStack(8);
            foreach (var d in Enum.GetValues<Difficulty>())
                levels.Add(Ui.Term(d.Label(), () => { logDifficulty = d; Render(); }, selected: d == logDifficulty));
            col.Add(levels);
        }

        col.Add(Ui.Text("topic", Theme.Secondary));
        var grid = new UniformGrid { Columns = 2 };
        foreach (string topic in Topics.List)
        {
            var button = Ui.Term(topic, () => Log(topic), selected: topic == model.LastTopic);
            button.Margin = new Thickness(0, 0, 0, 6);
            grid.Children.Add(button);
        }
        col.Add(grid);

        string last = model.LastTopic;
        defaultAction = () => Log(last);
        return col.Add(Ui.Split(Ui.Term($"log {last} ⏎", defaultAction), Ui.Link(pending == null ? "cancel" : "discard", Cancel)));
    }

    // Goals setup

    private FrameworkElement SetupView(bool firstRun)
    {
        var d = draft ??= model.Draft();

        void Leave()
        {
            draft = null;
            showCalendar = false;
            screen = Screen.Main;
        }

        void Save()
        {
            Leave();
            model.SaveGoals(d);
        }

        void ShiftWeeks(int n)
        {
            var moved = Pace.AddWeeks(n, d.ReadyBy);
            d.ReadyBy = moved < DateTime.Today ? DateTime.Today : moved;
            Render();
        }

        defaultAction = Save;
        if (!firstRun) cancelAction = () => { Leave(); Render(); };

        var col = new VStack(10).Add(Ui.Text(firstRun ? "dialed · set your goals" : "edit goals"));

        var dateControls = new HStack()
            .Add(Ui.Text("ready by".PadRight(18), Theme.Secondary))
            .Add(Ui.Term(Day(d.ReadyBy), () => { showCalendar = !showCalendar; Render(); }, selected: showCalendar, tip: "Pick a date"))
            .Add(Ui.Link("  -1w", () => ShiftWeeks(-1)))
            .Add(Ui.Link("  +1w", () => ShiftWeeks(1)));
        col.Add(Ui.Split(dateControls, Ui.Text($"{Pace.DaysLeft(DateTime.Now, d.ReadyBy)}d", Theme.Secondary)));

        if (showCalendar)
        {
            var shown = d.ReadyBy.Date < DateTime.Today ? DateTime.Today : d.ReadyBy.Date;
            var calendar = new System.Windows.Controls.Calendar
            {
                DisplayDateStart = DateTime.Today,
                DisplayDate = shown,
                SelectedDate = shown,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            calendar.SelectedDatesChanged += (_, _) =>
            {
                if (calendar.SelectedDate is not DateTime picked || picked == d.ReadyBy.Date) return;
                d.ReadyBy = picked;
                // The calendar keeps the mouse captured after a click; release it before rebuilding.
                Mouse.Capture(null);
                Dispatcher.BeginInvoke(new Action(Render));
            };
            col.Add(calendar);
        }

        col.Add(NumberRow("problems total", d.TotalProblems, v => d.TotalProblems = v));
        col.Add(NumberRow("already solved", d.StartingSolved, v => d.StartingSolved = v));
        col.Add(NumberRow("max problems/day", d.PaceCapPerDay, v => d.PaceCapPerDay = v));

        var buttons = new HStack(8).Add(Ui.Term("save", Save));
        if (!firstRun) buttons.Add(Ui.Link("cancel", cancelAction!));
        var bottom = Ui.Split(buttons, firstRun ? Ui.Link("quit", () => QuitRequested?.Invoke()) : null);
        bottom.Margin = new Thickness(0, 4, 0, 0);
        return col.Add(bottom);
    }

    private static FrameworkElement NumberRow(string label, int value, Action<int> set)
    {
        var box = new TextBox
        {
            Text = value.ToString(CultureInfo.InvariantCulture),
            Width = 80,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.Transparent,
            Foreground = Theme.Primary,
            CaretBrush = Theme.Primary,
            BorderBrush = Theme.Line,
        };
        box.TextChanged += (_, _) =>
        {
            if (int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) set(v);
        };
        box.GotKeyboardFocus += (_, _) => box.SelectAll();
        return new HStack().Add(Ui.Text(label.PadRight(18), Theme.Secondary)).Add(box);
    }

    // NeetCode

    private void StartFromList(NCProblem problem)
    {
        model.SetTab("solve");
        model.StartTimer(problem.Difficulty, problem.Slug);
    }

    private FrameworkElement NeetCodeView()
    {
        var solved = model.SolvedNeetCode;
        var priorSolved = model.Entries.Where(e => e.Prior && e.Neetcode != null).Select(e => e.Neetcode!).ToHashSet();
        string list = model.S.NcList;
        int total = NeetCode.ProblemsOf(list).Count;
        var next = model.NextNeetCode;
        var col = new VStack(10);

        var lists = new HStack(8);
        foreach (var (key, title) in NeetCode.Lists)
            lists.Add(Ui.Term(title, () => model.SetNcList(key), selected: list == key));
        col.Add(lists);

        col.Add(Ui.Line(
            Ui.Bar(model.SolvedInList, total, 20, next == null ? Theme.Accent : Theme.Primary),
            Ui.R($"  {model.SolvedInList}/{total}")));

        if (next != null)
        {
            var row = new DockPanel();
            var label = Ui.Text("next", Theme.Secondary);
            label.Margin = new Thickness(0, 0, 6, 0);
            DockPanel.SetDock(label, Dock.Left);
            row.Children.Add(label);
            if (model.Active == null)
            {
                var start = Ui.Link("▶", () => StartFromList(next), tip: $"Start a {next.Difficulty.Minutes()}-minute timer");
                start.Margin = new Thickness(6, 0, 0, 0);
                DockPanel.SetDock(start, Dock.Right);
                row.Children.Add(start);
            }
            row.Children.Add(Ui.Link(next.Name, () => OpenUrl(next.Url), tip: "Open on LeetCode", trim: true));
            col.Add(row);
        }

        col.Add(new HStack(8)
            .Add(Ui.Text("ticks are", Theme.Secondary))
            .Add(Ui.Term("new", () => { prior = false; Render(); }, selected: !prior, tip: "Counts toward this week"))
            .Add(Ui.Term("solved before", () => { prior = true; Render(); }, selected: prior, tip: "Counts toward your total, not this week")));

        var rows = new VStack(4);
        foreach (var section in NeetCode.SectionsOf(list))
        {
            rows.Add(SectionRow(section, solved));
            if (!open.Contains(section.Name)) continue;
            foreach (var problem in section.Problems)
            {
                var row = ProblemRow(problem, solved.Contains(problem.Slug), priorSolved.Contains(problem.Slug));
                row.Margin = new Thickness(14, 0, 0, 0);
                rows.Add(row);
            }
        }
        listScroll = new ScrollViewer
        {
            Content = rows,
            Height = 300,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Focusable = false,
        };
        // Keep the scroll position when the view is rebuilt after a tick.
        double offset = listOffset;
        listScroll.Loaded += (s, _) => ((ScrollViewer)s).ScrollToVerticalOffset(offset);
        col.Add(listScroll);

        return col.Add(Ui.Text("☐ tick · ☒ solved before · ▶ timer", Theme.Secondary, Theme.Small));
    }

    private FrameworkElement SectionRow(NCSection section, HashSet<string> solved)
    {
        int done = section.Problems.Count(p => solved.Contains(p.Slug));
        int total = section.Problems.Length;
        bool isOpen = open.Contains(section.Name);
        var line = Ui.Line(
            Ui.R((isOpen ? "▾ " : "▸ ") + section.Name.PadRight(22)),
            Ui.Bar(done, total, 8, done >= total ? Theme.Accent : Theme.Primary),
            Ui.R($"  {done}/{total}", Theme.Secondary));
        var row = Ui.Clickable(line, () =>
        {
            if (!open.Remove(section.Name)) open.Add(section.Name);
            Render();
        }, _ => { }, true, null);
        row.HorizontalAlignment = HorizontalAlignment.Stretch;
        return row;
    }

    private FrameworkElement ProblemRow(NCProblem problem, bool solved, bool before)
    {
        var row = new DockPanel();

        var tick = Ui.Link(before ? "☒" : solved ? "☑" : "☐", () => model.ToggleNeetCode(problem, prior),
            tip: solved ? "Untick" : prior ? "Mark solved before (not this week)" : "Mark solved (counts this week)");
        tick.Margin = new Thickness(0, 0, 6, 0);
        DockPanel.SetDock(tick, Dock.Left);
        row.Children.Add(tick);

        if (model.Active == null && !solved)
        {
            var start = Ui.Link("▶", () => StartFromList(problem), tip: $"Start a {problem.Difficulty.Minutes()}-minute timer");
            start.Margin = new Thickness(6, 0, 4, 0);
            DockPanel.SetDock(start, Dock.Right);
            row.Children.Add(start);
        }

        var level = Ui.Text(problem.Difficulty.Label()[..1].ToUpperInvariant(),
            problem.Difficulty == Difficulty.Hard ? Theme.Accent : Theme.Secondary);
        level.Margin = new Thickness(6, 0, model.Active == null && !solved ? 0 : 4, 0);
        DockPanel.SetDock(level, Dock.Right);
        row.Children.Add(level);

        row.Children.Add(Ui.Link(problem.Name, () => OpenUrl(problem.Url), tip: "Open on LeetCode", trim: true));
        return row;
    }

    // Stats

    private FrameworkElement StatsView()
    {
        if (model.Entries.Count == 0) return Ui.Text("nothing logged yet", Theme.Secondary);

        var topics = Stats.Rows(model.Entries, Topics.List, e => e.Topic);
        var levels = Stats.Rows(model.Entries, Enum.GetValues<Difficulty>().Select(d => d.Label()), e => e.Difficulty.Label());
        var slow = Stats.Slowest(topics);
        int timed = levels.Sum(r => r.Timed);
        int ranOut = levels.Sum(r => r.RanOut);

        var summary = new VStack(4);
        if (slow.Count > 0) summary.Add(Summary("slowest", slow));
        summary.Add(Summary("least done", Stats.LeastDone(topics)));

        return new VStack(10)
            .Add(summary)
            .Add(Table("topic", topics, slow.Select(r => r.Name).ToHashSet()))
            .Add(Table("difficulty", levels, []))
            .Add(timed > 0
                ? Ui.Text($"timer ran out on {ranOut} of {timed} timed problems", Theme.Secondary)
                : Ui.Text("avg and limit fill in once you log problems from the timer", Theme.Secondary, Theme.Small));
    }

    private static FrameworkElement Summary(string label, List<StatRow> rows)
    {
        var names = Ui.Text(string.Join(" · ", rows.Select(r => r.Name)));
        names.TextWrapping = TextWrapping.Wrap;
        var name = Ui.Text(label.PadRight(12), Theme.Secondary);
        name.VerticalAlignment = VerticalAlignment.Top;
        DockPanel.SetDock(name, Dock.Left);
        var row = new DockPanel();
        row.Children.Add(name);
        row.Children.Add(names);
        return row;
    }

    private static FrameworkElement Table(string title, List<StatRow> rows, HashSet<string> slow)
    {
        int most = rows.Count == 0 ? 0 : rows.Max(r => r.Solved);
        var header = Ui.Text(title.PadRight(15) + "solved".PadRight(12) + "avg".PadLeft(7) + "limit".PadLeft(7), Theme.Secondary);
        header.ToolTip = "avg: average time on timed problems. limit: share of the timer that average uses.";
        var col = new VStack(3).Add(header);
        foreach (var row in rows)
        {
            var main = row.Solved > 0 ? Theme.Primary : Theme.Secondary;
            var time = row.Timed > 0 ? Theme.Primary : Theme.Secondary;
            string avg = row.AvgSeconds is int s ? Pace.Clock(s) : "—";
            string share = row.AvgShare is double a ? $"{(int)Math.Round(a * 100)}%" : "—";
            var line = Ui.Line(
                Ui.R(row.Name.PadRight(15), main),
                Ui.Bar(row.Solved, most, 8, main),
                Ui.R(row.Solved.ToString().PadLeft(4), main),
                Ui.R(avg.PadLeft(7), time),
                Ui.R(share.PadLeft(7), slow.Contains(row.Name) ? Theme.Accent : time));
            line.ToolTip = row.Timed > 0
                ? $"{row.Solved} solved, {row.Timed} timed, timer ran out on {row.RanOut}"
                : $"{row.Solved} solved, none timed";
            col.Add(line);
        }
        return col;
    }

    // Window chrome

    /// Rounded corners (Windows 11) and no Alt+Tab entry.
    private void ApplyWindowStyle()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int round = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref round, sizeof(int));
        const int exStyle = -20, toolWindow = 0x80;
        SetWindowLong(hwnd, exStyle, GetWindowLong(hwnd, exStyle) | toolWindow);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
