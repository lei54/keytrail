using System.Windows;
using System.Windows.Media;
using KeyTrail.Localization;

namespace KeyTrail.Controls;

public sealed class SegmentBar : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values),
        typeof(IReadOnlyList<double>),
        typeof(SegmentBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly string[] SlotKeys = ["ChartSlot1", "ChartSlot2", "ChartSlot3", "ChartSlot4"];

    public SegmentBar()
    {
        IsHitTestVisible = false;
        ThemeService.ThemeChanged += () => InvalidateVisual();
    }

    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        IReadOnlyList<double> values = Values ?? [];
        if (values.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        double total = values.Sum();
        if (total <= 0)
        {
            return;
        }

        double x = 0;
        for (int i = 0; i < values.Count; i++)
        {
            double width = ActualWidth * values[i] / total;
            Color color = TryFindResource(SlotKeys[Math.Min(i, SlotKeys.Length - 1)]) is SolidColorBrush brush
                ? brush.Color
                : Colors.Gray;
            var segmentBrush = new SolidColorBrush(color);
            segmentBrush.Freeze();
            dc.DrawRectangle(segmentBrush, null, new Rect(x, 0, Math.Max(0, width - 2), ActualHeight));
            x += width;
        }
    }
}
