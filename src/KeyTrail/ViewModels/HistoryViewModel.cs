using System.Collections.ObjectModel;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Localization;
using KeyTrail.Mvvm;
using KeyTrail.Settings;

namespace KeyTrail.ViewModels;

public sealed class HistoryViewModel : ObservableObject, IActivatingPage
{
    private readonly StatisticsProvider _stats;
    private readonly SettingsStore _settings;

    private StatsPeriod _period = StatsPeriod.Day;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);
    private long _total;
    private long _averagePerDay;
    private string _rangeText = string.Empty;
    private double[] _hourValues = [];
    private double[] _dayValues = [];
    private IReadOnlyDictionary<int, long> _heat = new Dictionary<int, long>();
    private bool _canGoNext;
    private bool _loading;
    private int _slotFilter = -1;

    public HistoryViewModel(StatisticsProvider stats, SettingsStore settings)
    {
        _stats = stats;
        _settings = settings;
        LocalizationService.LanguageChanged += async () => await LoadAsync();
    }

    public StatsPeriod Period
    {
        get => _period;
        private set => SetProperty(ref _period, value);
    }

    public DateOnly SelectedDate
    {
        get => _selectedDate;
        private set => SetProperty(ref _selectedDate, value);
    }

    public string DateText
    {
        get
        {
            return Period switch
            {
                StatsPeriod.Day => SelectedDate.ToString("yyyy-MM-dd"),
                StatsPeriod.Week => $"{DateMath.StartOfWeek(SelectedDate):yyyy-MM-dd} ~ {DateMath.StartOfWeek(SelectedDate).AddDays(6):yyyy-MM-dd}",
                StatsPeriod.Month => $"{SelectedDate.Year:0000}-{SelectedDate.Month:00}",
                _ => string.Empty,
            };
        }
    }

    public string RangeText
    {
        get => _rangeText;
        private set => SetProperty(ref _rangeText, value);
    }

    public long Total
    {
        get => _total;
        private set => SetProperty(ref _total, value);
    }

    public long AveragePerDay
    {
        get => _averagePerDay;
        private set => SetProperty(ref _averagePerDay, value);
    }

    public double[] HourValues
    {
        get => _hourValues;
        private set => SetProperty(ref _hourValues, value);
    }

    public double[] DayValues
    {
        get => _dayValues;
        private set => SetProperty(ref _dayValues, value);
    }

    public IReadOnlyDictionary<int, long> Heat
    {
        get => _heat;
        private set => SetProperty(ref _heat, value);
    }

    public long[] SlotCounts { get; private set; } = [0, 0, 0, 0];

    public bool CanGoNext
    {
        get => _canGoNext;
        private set => SetProperty(ref _canGoNext, value);
    }

    public ObservableCollection<TopKeyItem> TopKeys { get; } = [];
    public ObservableCollection<RankedItem> SlotItems { get; } = [];

    public async Task ActivateAsync() => await LoadAsync();

    public async Task SetSlotFilterAsync(int? slot)
    {
        _slotFilter = slot ?? -1;
        OnPropertyChanged(nameof(SlotFilterApplied));
        await LoadAsync();
    }

    public bool SlotFilterApplied => _slotFilter >= 0;

    public async Task SetPeriodAsync(StatsPeriod period)
    {
        if (Period == period)
        {
            return;
        }

        Period = period;
        OnPropertyChanged(nameof(DateText));
        await LoadAsync();
    }

    public async Task ShiftAsync(bool forward)
    {
        DateOnly next = Period switch
        {
            StatsPeriod.Day => SelectedDate.AddDays(forward ? 1 : -1),
            StatsPeriod.Week => SelectedDate.AddDays(forward ? 7 : -7),
            StatsPeriod.Month => SelectedDate.AddMonths(forward ? 1 : -1),
            _ => SelectedDate,
        };
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        if (next > today)
        {
            return;
        }

        SelectedDate = next;
        OnPropertyChanged(nameof(DateText));
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        try
        {
            (int from, int to) = DateMath.GetRange(SelectedDate, Period);
            OnPropertyChanged(nameof(DateText));

            TimeSlot? filter = _slotFilter is >= 0 and < SlotSchedule.Count
                ? (TimeSlot)_slotFilter
                : null;

            Task<long> totalTask = filter is { } slot
                ? _stats.CountInSlotAsync(from, to, slot)
                : _stats.CountAsync(from, to);
            Task<IReadOnlyList<KeyCount>> keysTask = filter is { } kSlot
                ? _stats.KeysInSlotAsync(from, to, kSlot)
                : _stats.KeysAsync(from, to);
            Task<long[]> hoursTask = filter is { } hSlot
                ? _stats.HoursInSlotAsync(from, to, hSlot)
                : _stats.HoursAsync(from, to);
            Task<long[]> slotsTask = _stats.SlotsAsync(from, to);
            Task<IReadOnlyList<DayTotal>> daysTask = filter is { } dSlot
                ? _stats.DaysInSlotAsync(from, to, dSlot)
                : _stats.DaysAsync(from, to);

            await Task.WhenAll(totalTask, keysTask, hoursTask, slotsTask, daysTask);

            Total = totalTask.Result;
            IReadOnlyList<DayTotal> days = daysTask.Result;
            int dayCount = (DateMath.ToDate(to).DayNumber - DateMath.ToDate(from).DayNumber) + 1;
            AveragePerDay = dayCount > 0 ? (long)Math.Ceiling(Total / (double)dayCount) : 0;
            HourValues = hoursTask.Result.Select(v => (double)v).ToArray();
            Heat = keysTask.Result.ToDictionary(k => k.Vk, k => k.Count);

            DayValues = new double[dayCount];
            var labels = new List<string>(dayCount);
            var dayTotals = days.ToDictionary(d => d.Day, d => d.Count);
            for (int i = 0; i < dayCount; i++)
            {
                int day = DateMath.ToDayNumber(DateMath.ToDate(from).AddDays(i));
                DayValues[i] = dayTotals.TryGetValue(day, out long c) ? c : 0;
                labels.Add(DateMath.ToDate(day).ToString("MM-dd"));
            }

            DayLabels = labels.ToArray();
            OnPropertyChanged(nameof(DayLabels));

            long[] slots = slotsTask.Result;
            long slotTotal = slots.Sum();
            SlotCounts = slots;
            OnPropertyChanged(nameof(SlotCounts));
            SlotItems.Clear();
            for (int i = 0; i < slots.Length; i++)
            {
                SlotItems.Add(new RankedItem
                {
                    Label = LocalizationService.Get(SlotSchedule.NameKey((TimeSlot)i)),
                    Count = slots[i],
                    Share = slotTotal > 0 ? slots[i] / (double)slotTotal : 0,
                });
            }

            RangeText = $"{DateMath.FormatDay(from)} ~ {DateMath.FormatDay(to)}";
            CanGoNext = SelectedDate < DateOnly.FromDateTime(DateTime.Now);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("History load failed.", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    public string[] DayLabels { get; private set; } = [];
}
