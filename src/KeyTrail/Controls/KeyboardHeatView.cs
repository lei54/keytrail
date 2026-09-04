using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using KeyTrail.Localization;
using KeyTrail.Models;

namespace KeyTrail.Controls;

public sealed class KeyboardHeatView : FrameworkElement
{
    private static readonly DependencyProperty KeyCountsProperty = DependencyProperty.Register(
        nameof(KeyCounts),
        typeof(IReadOnlyDictionary<int, long>),
        typeof(KeyboardHeatView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

    private readonly List<PulseAnimation> _pulses = [];
    private readonly DispatcherTimer _animationTimer;

    private Color _accent;
    private Color _cap;
    private Color _capSide;
    private Color _capText;
    private Color _keyboardBase;
    private SolidColorBrush[] _heatBrushes = [];
    private IReadOnlyDictionary<int, long> _counts = new Dictionary<int, long>();
    private Dictionary<int, double> _heat = [];
    private long _maxCount;

    public KeyboardHeatView()
    {
        IsHitTestVisible = false;
        ThemeService.ThemeChanged += HandleThemeChanged;
        _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _animationTimer.Tick += (_, _) =>
        {
            InvalidateVisual();
            if (_pulses.Count == 0)
            {
                _animationTimer.Stop();
            }
        };
        Loaded += (_, _) => HandleThemeChanged();
    }

    public IReadOnlyDictionary<int, long>? KeyCounts
    {
        get => (IReadOnlyDictionary<int, long>?)GetValue(KeyCountsProperty);
        set => SetValue(KeyCountsProperty, value);
    }

    public void Pulse(int vk)
    {
        _pulses.Add(new PulseAnimation(vk, Environment.TickCount64));
        if (!_animationTimer.IsEnabled)
        {
            _animationTimer.Start();
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        double unit = Math.Min(ActualWidth / 21.5, ActualHeight / 11.9);
        double offsetX = (ActualWidth - 21.5 * unit) / 2;
        double offsetY = (ActualHeight - 11.9 * unit) / 2;

        dc.PushTransform(new TranslateTransform(offsetX, offsetY));

        // Base plate.
        var baseRect = new Rect(-0.7 * unit, -0.5 * unit, 21.4 * unit, 11.6 * unit);
        dc.DrawRoundedRectangle(GetSolid(_keyboardBase), null, baseRect, unit * 0.75, unit * 0.75);

        foreach (KeySpec key in KeyboardLayout.Keys)
        {
            double x = key.X * unit;
            double y = key.Y * unit;
            double w = key.Width * unit;
            double h = 1.0 * unit;

            var faceRect = new Rect(x, y, w, h);
            var sideRect = new Rect(x, y + 0.16 * unit, w, h);

            double heat = _heat.TryGetValue(key.Vk, out double hh) ? hh : 0;
            Brush faceBrush = heat > 0.001
                ? _heatBrushes[Math.Clamp((int)(heat * (_heatBrushes.Length - 1)), 0, _heatBrushes.Length - 1)]
                : GetSolid(_cap);

            dc.DrawRoundedRectangle(GetSolid(_capSide), null, sideRect, unit * 0.28, unit * 0.28);
            dc.DrawRoundedRectangle(faceBrush, null, faceRect, unit * 0.28, unit * 0.28);

            double cx = x + w / 2;
            double cy = y + h / 2;

            long now = Environment.TickCount64;
            PulseAnimation? active = null;
            foreach (PulseAnimation p in _pulses)
            {
                if (p.Vk == key.Vk && now - p.StartedAt < 700)
                {
                    active = p;
                    break;
                }
            }

            if (active is { } pulse)
            {
                double t = (now - pulse.StartedAt) / 700.0;
                double radius = unit * (0.8 + 1.8 * t);
                double alpha = (1.0 - t) * 0.65;
                Color ripple = Color.FromArgb((byte)(alpha * 255), _accent.R, _accent.G, _accent.B);
                dc.DrawEllipse(new SolidColorBrush(ripple), null, new Point(cx, cy), radius, radius);
            }

            double fontSize = Math.Max(6, Math.Min(unit * 0.46, key.Width * unit * 0.55));
            string label = key.Label;
            var formatted = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                fontSize,
                GetSolid(_capText),
                1.0);
            dc.DrawText(formatted, new Point(cx - formatted.Width / 2, cy - formatted.Height / 2));
        }

        _pulses.RemoveAll(p => Environment.TickCount64 - p.StartedAt > 700);
        dc.Pop();
    }

    private void HandleThemeChanged()
    {
        void Reload()
        {
            _accent = ColorOf("Accent");
            _cap = ColorOf("KeyCap");
            _capSide = ColorOf("KeyCapSide");
            _capText = ColorOf("KeyText");
            _keyboardBase = ColorOf("KeyboardBase");

            var brushes = new SolidColorBrush[18];
            for (int i = 0; i < brushes.Length; i++)
            {
                double k = i / (double)(brushes.Length - 1);
                Color c = Color.FromArgb(
                    (byte)Math.Round(20 + 235 * k),
                    _accent.R,
                    _accent.G,
                    _accent.B);
                brushes[i] = new SolidColorBrush(c);
                brushes[i].Freeze();
            }

            _heatBrushes = brushes;
            InvalidateVisual();
        }

        if (Dispatcher.CheckAccess())
        {
            Reload();
        }
        else
        {
            _ = Dispatcher.InvokeAsync(Reload);
        }
    }

    private Color ColorOf(string key)
    {
        return TryFindResource(key) is SolidColorBrush brush
            ? brush.Color
            : Colors.Gray;
    }

    private static SolidColorBrush GetSolid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not KeyboardHeatView view)
        {
            return;
        }

        view._counts = e.NewValue as IReadOnlyDictionary<int, long> ?? new Dictionary<int, long>();
        view._maxCount = view._counts.Count == 0 ? 0 : view._counts.Values.Max();
        view._heat = [];
        if (view._maxCount > 0)
        {
            double logMax = Math.Log(1 + view._maxCount);
            foreach ((int vk, long count) in view._counts)
            {
                if (count > 0)
                {
                    view._heat[vk] = Math.Log(1 + count) / logMax;
                }
            }
        }

        view.InvalidateVisual();
    }

    private readonly record struct PulseAnimation(int Vk, long StartedAt);
}
