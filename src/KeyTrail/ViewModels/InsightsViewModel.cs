using System.Collections.ObjectModel;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Localization;
using KeyTrail.Models;
using KeyTrail.Mvvm;
using KeyTrail.Settings;

namespace KeyTrail.ViewModels;

public sealed class InsightsViewModel : ObservableObject, IActivatingPage
{
    private readonly StatisticsProvider _stats;
    private readonly SettingsStore _settings;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);
    private bool _loading;

    private string _presses = "0";
    private string _averageGap = "0";
    private string _medianGap = "0";
    private string _bursts = "0";
    private string _averageBurst = "0";
    private string _shortBreaks = "0";
    private string _longBreaks = "0";
    private string _activeMinutes = "0";
    private string _longestActive = "0";

    public InsightsViewModel(StatisticsProvider stats, SettingsStore settings)
    {
        _stats = stats;
        _settings = settings;
        LocalizationService.LanguageChanged += async () => await LoadAsync();
    }

    public string Presses { get => _presses; private set => SetProperty(ref _presses, value); }
    public string AverageGap { get => _averageGap; private set => SetProperty(ref _averageGap, value); }
    public string MedianGap { get => _medianGap; private set => SetProperty(ref _medianGap, value); }
    public string Bursts { get => _bursts; private set => SetProperty(ref _bursts, value); }
    public string AverageBurst { get => _averageBurst; private set => SetProperty(ref _averageBurst, value); }
    public string ShortBreaks { get => _shortBreaks; private set => SetProperty(ref _shortBreaks, value); }
    public string LongBreaks { get => _longBreaks; private set => SetProperty(ref _longBreaks, value); }
    public string ActiveMinutes { get => _activeMinutes; private set => SetProperty(ref _activeMinutes, value); }
    public string LongestActive { get => _longestActive; private set => SetProperty(ref _longestActive, value); }

    public string DateText
    {
        get
        {
            DateOnly start = DateMath.StartOfWeek(_selectedDate);
            return $"{start:yyyy-MM-dd} ~ {start.AddDays(6):yyyy-MM-dd}";
        }
    }

    public ObservableCollection<RankedItem> WeekdayItems { get; } = [];
    public ObservableCollection<RankedItem> GroupItems { get; } = [];
    public ObservableCollection<RankedItem> ModifierItems { get; } = [];
    public ObservableCollection<RankedItem> ShortcutItems { get; } = [];

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
            if (_selectedDate > today)
            {
                _selectedDate = today;
            }

            DateOnly weekStart = DateMath.StartOfWeek(_selectedDate);
            int from = DateMath.ToDayNumber(weekStart);
            int to = DateMath.ToDayNumber(weekStart.AddDays(6));
            OnPropertyChanged(nameof(DateText));

            Task<GapStats> gapsTask = _stats.GapsAsync(from, to);
            Task<IReadOnlyList<KeyCount>> keysTask = _stats.KeysAsync(from, to);
            Task<IReadOnlyDictionary<int, GapStats>> dailyTask = _stats.DailyGapsAsync(from, to);
            Task<IReadOnlyList<(int Modifiers, int Vk, int Count)>> shortcutsTask = _stats.ShortcutsAsync(from, to);

            await Task.WhenAll(gapsTask, keysTask, dailyTask, shortcutsTask);

            GapStats gaps = gapsTask.Result;
            Presses = gaps.Presses.ToString("N0");
            AverageGap = gaps.AverageGapMs.ToString("0");
            MedianGap = gaps.MedianGapMs.ToString("0");
            Bursts = gaps.BurstCount.ToString("N0");
            AverageBurst = gaps.AverageBurstLength.ToString("0.0");
            ShortBreaks = gaps.ShortBreaks.ToString("N0");
            LongBreaks = gaps.LongBreaks.ToString("N0");
            ActiveMinutes = gaps.ActiveMinutes.ToString("N0");
            LongestActive = gaps.LongestActiveMinutes.ToString("N0");

            IReadOnlyList<KeyCount> keys = keysTask.Result;
            FillGroups(keys);
            FillModifiers(keys);
            FillWeekdays(dailyTask.Result, from, to);
            FillShortcuts(shortcutsTask.Result);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Insights load failed.", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    private void FillGroups(IReadOnlyList<KeyCount> keys)
    {
        IReadOnlyDictionary<KeyGroup, long> groups = _stats.GroupCounts(keys);
        long total = groups.Values.Sum();
        GroupItems.Clear();
        foreach (KeyGroup group in Enum.GetValues<KeyGroup>())
        {
            long count = groups.TryGetValue(group, out long c) ? c : 0;
            if (count > 0)
            {
                GroupItems.Add(new RankedItem
                {
                    Label = LocalizationService.Get($"Group.{group}"),
                    Count = count,
                    Share = total > 0 ? count / (double)total : 0,
                });
            }
        }
    }

    private void FillModifiers(IReadOnlyList<KeyCount> keys)
    {
        var map = new Dictionary<string, long>();
        foreach (KeyCount key in keys)
        {
            if (!KeyCatalog.IsModifier(key.Vk))
            {
                continue;
            }

            string name = key.Vk is 0xA0 or 0xA1 ? "Shift" :
                key.Vk is 0xA2 or 0xA3 ? "Ctrl" :
                key.Vk is 0xA4 or 0xA5 ? "Alt" : "Win";
            map[name] = map.TryGetValue(name, out long c) ? c + key.Count : key.Count;
        }

        long total = map.Values.Sum();
        ModifierItems.Clear();
        foreach ((string name, long count) in map.OrderByDescending(kvp => kvp.Value))
        {
            ModifierItems.Add(new RankedItem
            {
                Label = name,
                Count = count,
                Share = total > 0 ? count / (double)total : 0,
            });
        }
    }

    private void FillWeekdays(IReadOnlyDictionary<int, GapStats> daily, int fromDay, int toDay)
    {
        DateOnly start = DateMath.ToDate(fromDay);
        WeekdayItems.Clear();
        var values = new long[7];
        for (int i = 0; i < 7; i++)
        {
            int day = DateMath.ToDayNumber(start.AddDays(i));
            if (daily.TryGetValue(day, out GapStats? stats))
            {
                values[i] = stats.Presses;
            }
        }

        long max = values.Max();
        for (int i = 0; i < 7; i++)
        {
            string key = start.AddDays(i).DayOfWeek switch
            {
                DayOfWeek.Monday => "Weekday.Mon",
                DayOfWeek.Tuesday => "Weekday.Tue",
                DayOfWeek.Wednesday => "Weekday.Wed",
                DayOfWeek.Thursday => "Weekday.Thu",
                DayOfWeek.Friday => "Weekday.Fri",
                DayOfWeek.Saturday => "Weekday.Sat",
                _ => "Weekday.Sun",
            };
            WeekdayItems.Add(new RankedItem
            {
                Label = LocalizationService.Get(key),
                Count = values[i],
                Share = max > 0 ? values[i] / (double)max : 0,
            });
        }
    }

    private void FillShortcuts(IReadOnlyList<(int Modifiers, int Vk, int Count)> shortcuts)
    {
        ShortcutItems.Clear();
        foreach ((int modifiers, int vk, int count) in shortcuts)
        {
            ShortcutItems.Add(new RankedItem
            {
                Label = KeyCatalog.ShortcutText(modifiers, vk),
                Count = count,
                Share = 0,
            });
        }
    }
}
