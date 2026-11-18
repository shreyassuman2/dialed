using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace Dialed;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // DIALED_DATA_DIR points at another data folder (used for demo recordings).
        string? overrideDir = Environment.GetEnvironmentVariable("DIALED_DATA_DIR");
        string folder = string.IsNullOrEmpty(overrideDir)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dialed")
            : overrideDir;
        bool isOverride = !string.IsNullOrEmpty(overrideDir);

        // One instance on the real data: a second one would overwrite the first one's file.
        using var mutex = new Mutex(true, "Dialed-SingleInstance", out bool first);
        if (!first && !isOverride)
        {
            MessageBox.Show("Dialed is already running. Look for its icon in the system tray.", "Dialed");
            return;
        }

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var model = new AppModel(folder);
        if (isOverride && Environment.GetEnvironmentVariable("DIALED_DEMO") == "1") model.SeedDemoIfEmpty();

        var window = new PopupWindow(model);
        using var tray = new Tray(model, window);
        window.QuitRequested += app.Shutdown;
        tray.QuitRequested += app.Shutdown;

#if !DEBUG
        if (!isOverride) LoginItem.Register();
#endif

        // First run: open the setup screen instead of waiting for a click on the tray icon.
        if (model.Goals == null || isOverride) window.ShowPopup();
        app.Run();
    }
}

/// Launch at login. The entry is rewritten on every start so it follows the exe if it moves.
/// Turning it off in Settings > Apps > Startup (or Task Manager) stays respected: Windows
/// keeps that switch separately.
internal static class LoginItem
{
    public static void Register()
    {
        if (Environment.ProcessPath is not { } exe) return;
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        key.SetValue("Dialed", $"\"{exe}\"");
    }
}

/// The system tray icon: a lock when idle, the minutes left while a timer runs.
internal sealed class Tray : IDisposable
{
    private readonly AppModel model;
    private readonly PopupWindow window;
    private readonly WF.NotifyIcon icon = new() { Text = "Dialed" };
    private readonly DispatcherTimer minute;
    private string drawn = "";
    private IntPtr handle;
    private bool closingClick;

    public event Action? QuitRequested;

    public Tray(AppModel model, PopupWindow window)
    {
        this.model = model;
        this.window = window;

        var menu = new WF.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => window.ShowPopup());
        menu.Items.Add("Quit", null, (_, _) => QuitRequested?.Invoke());
        icon.ContextMenuStrip = menu;

        // A click on the icon while the panel is open first deactivates (and hides) the panel.
        // That click should leave it closed, not open it again.
        icon.MouseDown += (_, e) =>
        {
            if (e.Button == WF.MouseButtons.Left)
                closingClick = (DateTime.UtcNow - window.HiddenAt).TotalMilliseconds < 300;
        };
        icon.MouseClick += (_, e) =>
        {
            if (e.Button != WF.MouseButtons.Left) return;
            if (window.IsVisible) window.HidePopup();
            else if (!closingClick) window.ShowPopup();
        };
        icon.BalloonTipClicked += (_, _) => window.ShowPopup();

        model.Changed += Update;
        model.Ticked += Update;
        model.TimerRanOut += d =>
            Notify("Time's up", $"{d.Minutes()} minutes for {(d == Difficulty.Easy ? "an" : "a")} {d.Label()} problem have passed.");

        // Week rollover, the Monday summary, and taskbar theme changes.
        minute = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        minute.Tick += (_, _) => Housekeeping();
        minute.Start();

        Update();
        icon.Visible = true;
        Housekeeping();
    }

    private void Housekeeping()
    {
        if (!window.IsVisible) model.Refresh();
        Update();
        if (model.TakeWeeklySummary() is { } summary) Notify(summary.Title, summary.Body);
    }

    private void Notify(string title, string body) => icon.ShowBalloonTip(10000, title, body, WF.ToolTipIcon.None);

    private void Update()
    {
        int? seconds = model.RemainingSeconds;
        // Minutes left, rounded up; seconds in the last minute.
        string label = seconds is int s ? (s > 60 ? ((s + 59) / 60).ToString() : s.ToString()) : "";
        string tip = seconds is int t ? $"Dialed · {Pace.Clock(t)} left" : "Dialed";
        if (icon.Text != tip) icon.Text = tip;

        bool dark = Theme.TaskbarDark;
        string key = $"{label}|{dark}";
        if (key == drawn) return;
        drawn = key;

        IntPtr old = handle;
        handle = Draw(label, dark);
        icon.Icon = SD.Icon.FromHandle(handle);
        if (old != IntPtr.Zero) DestroyIcon(old);
    }

    private static IntPtr Draw(string label, bool dark)
    {
        const int size = 32;
        using var bitmap = new SD.Bitmap(size, size);
        using var g = SD.Graphics.FromImage(bitmap);
        g.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = SD.Text.TextRenderingHint.AntiAlias;
        g.Clear(SD.Color.Transparent);

        using var brush = new SD.SolidBrush(dark ? SD.Color.White : SD.Color.Black);
        using var format = new SD.StringFormat(SD.StringFormat.GenericTypographic)
        {
            Alignment = SD.StringAlignment.Center,
            LineAlignment = SD.StringAlignment.Center,
        };
        bool idle = label.Length == 0;
        using var font = idle
            ? new SD.Font("Segoe MDL2 Assets", 24, SD.FontStyle.Regular, SD.GraphicsUnit.Pixel)   // lock glyph
            : new SD.Font("Segoe UI", label.Length > 1 ? 23 : 27, SD.FontStyle.Bold, SD.GraphicsUnit.Pixel);
        g.DrawString(idle ? "" : label, font, brush, new SD.RectangleF(0, 0, size, size), format);
        return bitmap.GetHicon();
    }

    public void Dispose()
    {
        minute.Stop();
        icon.Visible = false;
        icon.Dispose();
        if (handle != IntPtr.Zero) DestroyIcon(handle);
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
