using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Dialed;

/// Colours and fonts. One accent colour; everything else is primary or secondary text.
public static class Theme
{
    public static readonly FontFamily Mono = new("Consolas, Cascadia Mono, Courier New");
    public const double Size = 13, Small = 12, Big = 44, Width = 420;

    public static Brush Bg { get; private set; } = Brushes.White;
    public static Brush Primary { get; private set; } = Brushes.Black;
    public static Brush Secondary { get; private set; } = Brushes.Gray;
    public static Brush Accent { get; private set; } = Brushes.Blue;
    public static Brush Line { get; private set; } = Brushes.LightGray;

    /// True when apps use the dark theme.
    public static bool AppsDark => ReadFlag("AppsUseLightTheme") == 0;
    /// True when the taskbar is dark.
    public static bool TaskbarDark => ReadFlag("SystemUsesLightTheme") == 0;

    static Theme() => Load();

    /// Re-reads the Windows light/dark setting.
    public static void Load()
    {
        if (AppsDark)
        {
            Bg = Solid(0x1E, 0x1E, 0x1E);
            Primary = Solid(0xE6, 0xE6, 0xE6);
            Secondary = Solid(0x8C, 0x8C, 0x8C);
            Accent = Solid(0x66, 0xAD, 0xFF);
            Line = Solid(0x44, 0x44, 0x44);
        }
        else
        {
            Bg = Solid(0xF7, 0xF7, 0xF7);
            Primary = Solid(0x1A, 0x1A, 0x1A);
            Secondary = Solid(0x6E, 0x6E, 0x6E);
            Accent = Solid(0x00, 0x61, 0xE6);
            Line = Solid(0xC8, 0xC8, 0xC8);
        }
    }

    private static SolidColorBrush Solid(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static int ReadFlag(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue(name) is int v ? v : 1;
    }
}

/// A vertical stack with fixed spacing between items.
public class VStack : StackPanel
{
    private readonly double spacing;

    public VStack(double spacing = 0)
    {
        this.spacing = spacing;
        Orientation = Orientation.Vertical;
    }

    public VStack Add(FrameworkElement? e)
    {
        if (e == null) return this;
        if (Children.Count > 0)
        {
            var m = e.Margin;
            e.Margin = new Thickness(m.Left, m.Top + spacing, m.Right, m.Bottom);
        }
        Children.Add(e);
        return this;
    }
}

/// A horizontal stack with fixed spacing between items.
public class HStack : StackPanel
{
    private readonly double spacing;

    public HStack(double spacing = 0)
    {
        this.spacing = spacing;
        Orientation = Orientation.Horizontal;
    }

    public HStack Add(FrameworkElement? e)
    {
        if (e == null) return this;
        if (Children.Count > 0)
        {
            var m = e.Margin;
            e.Margin = new Thickness(m.Left + spacing, m.Top, m.Right, m.Bottom);
        }
        if (e.VerticalAlignment == VerticalAlignment.Stretch) e.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(e);
        return this;
    }
}

public static class Ui
{
    public static TextBlock Text(string text, Brush? color = null, double size = Theme.Size, bool bold = false)
    {
        var t = new TextBlock { Text = text, FontSize = size };
        if (color != null) t.Foreground = color;
        if (bold) t.FontWeight = FontWeights.Bold;
        return t;
    }

    /// One line of text made of runs and bars. Keeps column padding exact.
    public static TextBlock Line(params Inline[] parts)
    {
        var t = new TextBlock();
        t.Inlines.AddRange(parts);
        return t;
    }

    public static Run R(string text, Brush? color = null)
    {
        var run = new Run(text);
        if (color != null) run.Foreground = color;
        return run;
    }

    /// Width of one character of the monospaced font at the normal size.
    public static readonly double CharWidth = new FormattedText("0", System.Globalization.CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight, new Typeface(Theme.Mono, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
        Theme.Size, Brushes.Black, 1.0).WidthIncludingTrailingWhitespace;

    /// A progress bar as wide as `width` characters, so it lines up with padded text.
    /// Drawn, not typed: block characters are taller than the text line in Windows fonts.
    public static Inline Bar(int done, int target, int width, Brush color)
    {
        double share = target <= 0 ? (done > 0 ? 1 : 0) : Math.Min(1.0, (double)done / target);
        double full = width * CharWidth;
        double filled = Math.Floor(share * width) * CharWidth;
        var track = new Grid { Width = full, Height = 9 };
        track.Children.Add(new Border { Background = Theme.Secondary, Opacity = 0.3 });
        track.Children.Add(new Border { Background = color, Width = filled, HorizontalAlignment = HorizontalAlignment.Left });
        return new InlineUIContainer(track) { BaselineAlignment = BaselineAlignment.Center };
    }

    /// One week of the history: a column whose height is done/target, in eight steps.
    public static FrameworkElement Spark(int done, int target, Brush color, string tip)
    {
        int level = done <= 0 ? 0 : target <= 0 ? 7 : Math.Min(7, Math.Max(1, (int)Math.Floor(Math.Min(1.0, (double)done / target) * 7)));
        return new Border
        {
            Width = CharWidth + 1,
            Height = 14,
            Background = Brushes.Transparent,
            ToolTip = tip,
            Padding = new Thickness(0, 0, 2, 0),
            Child = new Border { Background = color, Height = 1.5 * (level + 1) + 1, VerticalAlignment = VerticalAlignment.Bottom },
        };
    }

    /// A bracketed text button: `[ easy 20 ]`.
    public static FrameworkElement Term(string label, Action onClick, bool selected = false, string? tip = null)
    {
        var text = new Run(label)
        {
            Foreground = selected ? Theme.Accent : Theme.Primary,
            FontWeight = selected ? FontWeights.Bold : FontWeights.Normal,
        };
        var t = new TextBlock();
        t.Inlines.Add(new Run("[ ") { Foreground = Theme.Secondary });
        t.Inlines.Add(text);
        t.Inlines.Add(new Run(" ]") { Foreground = Theme.Secondary });
        return Clickable(t, onClick, hovering => text.Foreground = selected || hovering ? Theme.Accent : Theme.Primary, true, tip);
    }

    /// A plain text link: `undo`.
    public static FrameworkElement Link(string label, Action onClick, bool enabled = true, string? tip = null, bool trim = false)
    {
        var t = Text(label, Theme.Secondary);
        if (trim) t.TextTrimming = TextTrimming.CharacterEllipsis;
        return Clickable(t, onClick, hovering => t.Foreground = hovering ? Theme.Accent : Theme.Secondary, enabled, tip);
    }

    /// A tab: plain text, the selected one bold and underlined in the accent colour.
    public static FrameworkElement Tab(string label, bool selected, Action onClick)
    {
        var t = Text(label, selected ? Theme.Accent : Theme.Secondary, bold: selected);
        var underline = new Border
        {
            Child = t,
            Padding = new Thickness(0, 0, 0, 6),
            BorderThickness = new Thickness(0, 0, 0, 2),
            BorderBrush = selected ? Theme.Accent : Brushes.Transparent,
        };
        return Clickable(underline, onClick, hovering =>
        {
            if (!selected) t.Foreground = hovering ? Theme.Primary : Theme.Secondary;
        }, true, null);
    }

    /// Any content that reacts to hover and click.
    public static Border Clickable(UIElement content, Action onClick, Action<bool> hover, bool enabled, string? tip)
    {
        var b = new Border
        {
            Child = content,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Left,
            Opacity = enabled ? 1 : 0.35,
            ToolTip = tip,
        };
        if (!enabled) return b;
        b.Cursor = Cursors.Hand;
        b.MouseEnter += (_, _) => hover(true);
        b.MouseLeave += (_, _) => { hover(false); b.Opacity = 1; };
        b.MouseLeftButtonDown += (_, e) => { b.Opacity = 0.5; e.Handled = true; };
        b.MouseLeftButtonUp += (_, e) =>
        {
            b.Opacity = 1;
            e.Handled = true;
            // Run after the mouse event finishes: the action usually rebuilds the whole view.
            b.Dispatcher.BeginInvoke(onClick);
        };
        return b;
    }

    public static Border Divider() => new() { Height = 1, Background = Theme.Line };

    /// `left` fills the row, `right` sits at the right edge.
    public static DockPanel Split(FrameworkElement left, FrameworkElement? right)
    {
        var d = new DockPanel();
        if (right != null)
        {
            DockPanel.SetDock(right, Dock.Right);
            if (right.VerticalAlignment == VerticalAlignment.Stretch) right.VerticalAlignment = VerticalAlignment.Center;
            d.Children.Add(right);
        }
        if (left.VerticalAlignment == VerticalAlignment.Stretch) left.VerticalAlignment = VerticalAlignment.Center;
        d.Children.Add(left);
        return d;
    }
}
