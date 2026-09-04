using System.Collections.ObjectModel;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Localization;
using KeyTrail.Models;
using KeyTrail.Mvvm;
using KeyTrail.Settings;
using KeyTrail.Services;

namespace KeyTrail.ViewModels;

public sealed class HomeViewModel : ObservableObject, IActivatingPage
{
    private readonly StatisticsProvider _stats;
    private readonly SettingsStore _settings;
    private bool _loading;

    private long _totalToday;
    private long _totalYesterday;
    private string _compareText = string.Empty;
    private string _currentSlotName = string.Empty;
    private string _currentSlotShare = string.Empty;
    private int _activeMinutes;
    private IReadOnlyDictionary<int, long> _heat = new Dictionary<int, long>();
    private double[] _hourValues = [];
    private long[] _slotValues = [0, 0, 0, 0];

    public HomeViewModel(StatisticsProvider stats, SettingsStore settings)
    {
        _stats = stats;
        _settings = settings;
        LocalizationService.LanguageChanged += RefreshTexts;
    }

    public long TotalToday
    {
        get => _totalToday;
        private set => SetProperty(ref _totalToday, value);
    }

    public long TotalYesterday
    {
        get => _totalYesterday;
        private set => SetProperty(ref _totalYesterday, value);
    }

    public string CompareText
    {
        get => _compareText;
        private set => SetProperty(ref _compareText, value);
    }

    public string CurrentSlotName
    {
        get => _currentSlotName;
        private set => SetProperty(ref _currentSlotName, value);
    }

    public string CurrentSlotShare
    {
        get => _currentSlotShare;
        private set => SetProperty(ref _currentSlotShare, value);
    }

    public int ActiveMinutes
    {
        get => _activeMinutes;
        private set => SetProperty(ref _activeMinutes, value);
    }

    public IReadOnlyDictionary<int, long> Heat
    {
        get => _heat;
        private set => SetProperty(ref _heat, value);
    }

    public double[] HourValues
    {
        get => _hourValues;
        private set => SetProperty(ref _hourValues, value);
    }

    public ObservableCollection<TopKeyItem> TopKeys { get; } = [];

    public async Task ActivateAsync() => await LoadAsync();

    public async Task LoadAsync()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            int day = DateMath.ToDayNumber(today);
            int yesterdayDay = DateMath.ToDayNumber(today.AddDays(-1));

            Task<long> totalTask = _stats.CountAsync(day, day);
            Task<long> yesterdayTask = _stats.CountAsync(yesterdayDay, yesterdayDay);
            Task<IReadOnlyList<KeyCount>> keysTask = _stats.KeysAsync(day, day);
            Task<long[]> hoursTask = _stats.HoursAsync(day, day);
            Task<long[]> slotsTask = _stats.SlotsAsync(day, day);
            Task<IReadOnlyList<MinuteCount>> minutesTask = _stats.MinutesAsync(day, day);

            await Task.WhenAll(totalTask, yesterdayTask, keysTask, hoursTask, slotsTask, minutesTask);

            TotalToday = totalTask.Result;
            TotalYesterday = yesterdayTask.Result;
            ActiveMinutes = minutesTask.Result.Count;
            HourValues = hoursTask.Result.Select(v => (double)v).ToArray();
            _slotValues = slotsTask.Result;
            Heat = keysTask.Result.ToDictionary(k => k.Vk, k => k.Count);

            FillTopKeys(keysTask.Result);

            SlotSchedule schedule = _settings.GetSchedule();
            int nowHour = DateTime.Now.Hour;
            TimeSlot slot = schedule.SlotForHour(nowHour);
            CurrentSlotName = LocalizationService.Get(SlotSchedule.NameKey(slot));
            long slotTotal = _slotValues[(int)slot];
            CurrentSlotShare = TotalToday > 0
                ? $"{slotTotal * 100.0 / TotalToday:0.#}%"
                : "0%";

            RefreshCompareText();
            OnPropertyChanged(nameof(CurrentSlotShare));
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Home load failed.", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    private void FillTopKeys(IReadOnlyList<KeyCount> keys)
    {
        TopKeys.Clear();
        long total = keys.Count == 0 ? 0 : keys.Sum(k => k.Count);
        foreach (KeyCount key in keys.Take(8))
        {
            TopKeys.Add(new TopKeyItem
            {
                Label = KeyCatalog.Name(key.Vk),
                Count = key.Count,
                Share = total > 0 ? key.Count / (double)total : 0,
            });
        }
    }

    private void RefreshTexts()
    {
        RefreshCompareText();
        if (_settings.GetSchedule().SlotForHour(DateTime.Now.Hour) is { } slot)
        {
            CurrentSlotName = LocalizationService.Get(SlotSchedule.NameKey(slot));
        }
    }

    private void RefreshCompareText()
    {
        if (TotalYesterday <= 0)
        {
            CompareText = "—";
            return;
        }

        double pct = (TotalToday - TotalYesterday) * 100.0 / TotalYesterday;
        string arrow = pct > 0 ? "↑" : pct < 0 ? "↓" : "=";
        string direction = pct > 0
            ? LocalizationService.Get("Home.CompareUp")
            : pct < 0
                ? LocalizationService.Get("Home.CompareDown")
                : LocalizationService.Get("Home.CompareSame");
        CompareText = $"{arrow} {Math.Abs(pct):0.#}% · {direction}";
    }
}
