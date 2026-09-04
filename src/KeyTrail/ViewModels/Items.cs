using KeyTrail.Mvvm;

namespace KeyTrail.ViewModels;

public sealed class TopKeyItem : ObservableObject
{
    private string _label = string.Empty;
    private long _count;
    private double _share;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public long Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public double Share
    {
        get => _share;
        set => SetProperty(ref _share, value);
    }
}

public sealed class RankedItem : ObservableObject
{
    private string _label = string.Empty;
    private long _count;
    private double _share;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public long Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public double Share
    {
        get => _share;
        set => SetProperty(ref _share, value);
    }
}

public sealed class MetricItem : ObservableObject
{
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string _unit = string.Empty;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }
}

