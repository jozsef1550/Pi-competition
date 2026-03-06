using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PiSearch.App.Controls;

/// <summary>
/// A "Matrix-style" animation control that shows falling π digits and
/// highlights a matching sequence in neon green when one is found.
/// </summary>
public partial class MatrixAnimationControl : UserControl
{
    // ─── Dependency properties ─────────────────────────────────────────────

    public static readonly DependencyProperty PiDigitFeedProperty =
        DependencyProperty.Register(nameof(PiDigitFeed), typeof(string),
            typeof(MatrixAnimationControl),
            new PropertyMetadata(string.Empty, OnFeedChanged));

    public static readonly DependencyProperty FoundIndexProperty =
        DependencyProperty.Register(nameof(FoundIndex), typeof(long),
            typeof(MatrixAnimationControl),
            new PropertyMetadata(-1L, OnFoundChanged));

    public static readonly DependencyProperty FoundLengthProperty =
        DependencyProperty.Register(nameof(FoundLength), typeof(int),
            typeof(MatrixAnimationControl),
            new PropertyMetadata(0));

    // ─── CLR wrappers ──────────────────────────────────────────────────────

    public string PiDigitFeed
    {
        get => (string)GetValue(PiDigitFeedProperty);
        set => SetValue(PiDigitFeedProperty, value);
    }

    public long FoundIndex
    {
        get => (long)GetValue(FoundIndexProperty);
        set => SetValue(FoundIndexProperty, value);
    }

    public int FoundLength
    {
        get => (int)GetValue(FoundLengthProperty);
        set => SetValue(FoundLengthProperty, value);
    }

    // ─── Internal state ────────────────────────────────────────────────────

    private readonly DispatcherTimer _animTimer;
    private readonly List<TextBlock> _columnBlocks = new();
    private readonly Random _rng = new();
    private int _feedOffset;
    private string _currentFeed = string.Empty;

    // Pre-defined "rain" colours (dark → bright green gradient)
    private static readonly Color[] RainColors =
    {
        Color.FromRgb(0x00, 0x33, 0x00),
        Color.FromRgb(0x00, 0x66, 0x00),
        Color.FromRgb(0x00, 0x99, 0x00),
        Color.FromRgb(0x00, 0xCC, 0x00),
        Color.FromRgb(0x39, 0xFF, 0x14), // neon green – leading digit
    };

    // ─── Construction ──────────────────────────────────────────────────────

    public MatrixAnimationControl()
    {
        InitializeComponent();

        _animTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(80)
        };
        _animTimer.Tick += OnAnimTick;

        Loaded   += (_, _) => { BuildColumns(); _animTimer.Start(); };
        Unloaded += (_, _) => _animTimer.Stop();
        SizeChanged += (_, _) => BuildColumns();
    }

    // ─── Column setup ──────────────────────────────────────────────────────

    private void BuildColumns()
    {
        DigitColumns.Items.Clear();
        _columnBlocks.Clear();

        double charWidth = 14; // approximate character width at font size 13
        int count = Math.Max(1, (int)(ActualWidth / charWidth));

        for (int i = 0; i < count; i++)
        {
            var tb = new TextBlock
            {
                FontFamily  = new FontFamily("Consolas"),
                FontSize    = 13,
                Foreground  = new SolidColorBrush(RainColors[_rng.Next(RainColors.Length)]),
                Text        = "0",
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin      = new Thickness(0, _rng.Next(0, (int)ActualHeight), 0, 0),
            };
            _columnBlocks.Add(tb);
            DigitColumns.Items.Add(tb);
        }
    }

    // ─── Animation tick ────────────────────────────────────────────────────

    private void OnAnimTick(object? sender, EventArgs e)
    {
        if (_columnBlocks.Count == 0) return;

        for (int i = 0; i < _columnBlocks.Count; i++)
        {
            var tb = _columnBlocks[i];

            // Pick the next digit from the feed (or a random one if feed is empty)
            char digit = NextDigit();
            tb.Text = digit.ToString();

            // Scroll: increment margin to simulate falling
            double newTop = tb.Margin.Top + 14;
            if (newTop > ActualHeight)
                newTop = -14 - _rng.Next(0, 200); // restart from above with random delay

            tb.Margin = new Thickness(0, newTop, 0, 0);

            // Leading digit is bright neon; trailing digits fade
            int colorIdx = i % RainColors.Length;
            if (tb.Foreground is SolidColorBrush sb)
                sb.Color = RainColors[colorIdx];
        }
    }

    private char NextDigit()
    {
        if (!string.IsNullOrEmpty(_currentFeed))
        {
            char c = _currentFeed[_feedOffset % _currentFeed.Length];
            _feedOffset++;
            return c;
        }
        return (char)('0' + _rng.Next(10));
    }

    // ─── Property-change callbacks ─────────────────────────────────────────

    private static void OnFeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MatrixAnimationControl ctrl)
            ctrl._currentFeed = (string)e.NewValue ?? string.Empty;
    }

    private static void OnFoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MatrixAnimationControl ctrl)
        {
            long idx = (long)e.NewValue;
            if (idx >= 0)
                ctrl.ShowFoundAnimation(idx);
            else
                ctrl.HideFoundOverlay();
        }
    }

    // ─── Found-state animation ─────────────────────────────────────────────

    private void ShowFoundAnimation(long index)
    {
        FoundPatternText.Text = string.IsNullOrEmpty(PiDigitFeed)
            ? "MATCH FOUND"
            : PiDigitFeed[..(Math.Min(FoundLength, PiDigitFeed.Length))];

        FoundIndexText.Text = $"at decimal place {index:N0}";

        FoundOverlay.Visibility = Visibility.Visible;

        // Neon-flash storyboard
        var flashAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(4),
        };
        FoundOverlay.BeginAnimation(OpacityProperty, flashAnim);
    }

    private void HideFoundOverlay()
    {
        FoundOverlay.Visibility = Visibility.Collapsed;
    }
}
