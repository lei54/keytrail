using System.Globalization;
using System.Windows;
using System.Windows.Media;
using KeyTrail.Localization;

namespace KeyTrail.Controls;

public sealed class ActivityChart : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values),
        typeof(IReadOnlyList<double>),
        typeof(ActivityChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels),
        typeof(IReadOnlyList<string>),
        typeof(ActivityChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode),
        typeof(ChartMode),
        typeof(ActivityChart),
        new FrameworkPropertyMetadata(ChartMode.Line, FrameworkPropertyMetadataOptions.AffectsRender));

    private Color _accent;
    private Color _primary;
    private Color _secondary;
    private Color _grid;

    public ActivityChart()
    {
        IsHitTestVisible = false;
        ThemeService.ThemeChanged += () =>
        {
            LoadColors();
            InvalidateVisual();
        };
        Loaded += (_, _) => LoadColors();
    }

    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IReadOnlyList<string>? Labels
    {
        get => (IReadOnlyList<string>?)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    public ChartMode Mode
    {
        get => (ChartMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        IReadOnlyList<double> values = Values ?? [];
        if (values.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        LoadColors();
        const double leftPad = 10;
        const double bottomPad = 18;
        double plotWidth = ActualWidth - leftPad - 6;
        double plotHeight = ActualHeight - bottomPad - 8;
        double maxValue = Math.Max(1, values.Max());
        double niceMax = NiceCeiling(maxValue);

        // Horizontal grid.
        for (int i = 0; i <= 4; i++)
        {
            double y = 4 + plotHeight * i / 4.0;
            dc.DrawLine(new Pen(GetBrush(_grid), 1), new Point(leftPad, y), new Point(leftPad + plotWidth, y));
        }

        if (Mode == ChartMode.Bars)
        {
            double slot = plotWidth / values.Count;
            double barWidth = Math.Max(2, slot * 0.7);
            for (int i = 0; i < values.Count; i++)
            {
                double h = plotHeight * (values[i] / niceMax);
                if (h < 1 && values[i] > 0)
                {
                    h = 1;
                }

                var bar = new Rect(leftPad + slot * i + (slot - barWidth) / 2, 4 + plotHeight - h, barWidth, h);
                dc.DrawRoundedRectangle(GetBrush(_accent), null, bar, 2, 2);
            }
        }
        else
        {
            double slot = plotWidth / Math.Max(1, values.Count - 1);
            var points = new Point[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                points[i] = new Point(
                    leftPad + slot * i,
                    4 + plotHeight - plotHeight * (values[i] / niceMax));
            }

            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], isFilled: false, isClosed: false);
                if (points.Length == 1)
                {
                    ctx.LineTo(points[0], isStroked: true, isSmoothJoin: false);
                }
                else
                {
                    for (int i = 1; i < points.Length; i++)
                    {
                        ctx.LineTo(points[i], isStroked: true, isSmoothJoin: false);
                    }
                }
            }

            geometry.Freeze();
            dc.DrawGeometry(null, new Pen(GetBrush(_accent), 2.2), geometry);

            if (values.Count > 1)
            {
                var area = new StreamGeometry();
                using (StreamGeometryContext ctx = area.Open())
                {
                    ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
                    for (int i = 1; i < points.Length; i++)
                    {
                        ctx.LineTo(points[i], isStroked: false, isSmoothJoin: false);
                    }

                    ctx.LineTo(new Point(points[^1].X, 4 + plotHeight), isStroked: false, isSmoothJoin: false);
                    ctx.LineTo(new Point(points[0].X, 4 + plotHeight), isStroked: false, isSmoothJoin: false);
                }

                area.Freeze();
                var gradient = new LinearGradientBrush(
                    Color.FromArgb(70, _accent.R, _accent.G, _accent.B),
                    Color.FromArgb(0, _accent.R, _accent.G, _accent.B),
                    90);
                gradient.Freeze();
                dc.DrawGeometry(gradient, null, area);
            }
        }

        // Bottom labels.
        IReadOnlyList<string>? labels = Labels;
        int labelStep = Math.Max(1, (int)Math.Ceiling(values.Count / 8.0));
        for (int i = 0; i < values.Count; i += labelStep)
        {
            string label = labels is { Count: > 0 } && i < labels.Count
                ? labels[i]
                : i.ToString(CultureInfo.CurrentUICulture);
            var text = new FormattedText(
                label,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                GetBrush(_secondary),
                1);
            double x = Mode == ChartMode.Bars
                ? leftPad + plotWidth * ((i + 0.5) / values.Count) - text.Width / 2
                : leftPad + plotWidth * (i / Math.Max(1, values.Count - 1.0)) - text.Width / 2;
            x = Math.Clamp(x, 0, Math.Max(0, ActualWidth - text.Width));
            dc.DrawText(text, new Point(x, ActualHeight - 14));
        }
    }

    private void LoadColors()
    {
        _accent = ColorOf("Accent");
        _primary = ColorOf("TextPrimary");
        _secondary = ColorOf("TextSecondary");
        _grid = ColorOf("ChartGrid");
    }

    private Color ColorOf(string key) =>
        TryFindResource(key) is SolidColorBrush brush ? brush.Color : Colors.Gray;

    private static Brush GetBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static double NiceCeiling(double value)
    {
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        double normalized = value / magnitude;
        double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return nice * magnitude;
    }
}

public enum ChartMode
{
    Line,
    Bars,
}

