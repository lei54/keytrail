using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Models;
using KeyTrail.Settings;

namespace KeyTrail.ViewModels;

public sealed class StatisticsProvider
{
    private readonly StatisticsService _service;
    private readonly SettingsStore _settings;

    public StatisticsProvider(StatisticsService service, SettingsStore settings)
    {
        _service = service;
        _settings = settings;
    }

    public SlotSchedule Schedule => _settings.GetSchedule();

    public Task<long> CountAsync(int from, int to) => _service.CountPressesAsync(from, to);
    public Task<IReadOnlyList<KeyCount>> KeysAsync(int from, int to) => _service.GetKeyCountsAsync(from, to);
    public Task<IReadOnlyList<DayTotal>> DaysAsync(int from, int to) => _service.GetDayTotalsAsync(from, to);
    public Task<long[]> HoursAsync(int from, int to) => _service.GetHourTotalsAsync(from, to);
    public Task<long[]> SlotsAsync(int from, int to) => _service.GetSlotTotalsAsync(from, to, Schedule);
    public Task<GapStats> GapsAsync(int from, int to) => _service.GetGapStatsAsync(from, to);
    public Task<IReadOnlyDictionary<int, GapStats>> DailyGapsAsync(int from, int to) =>
        _service.GetDailyGapStatsAsync(from, to);
    public Task<IReadOnlyList<(int Modifiers, int Vk, int Count)>> ShortcutsAsync(int from, int to) =>
        _service.GetShortcutsAsync(from, to);
    public Task<IReadOnlyList<MinuteCount>> MinutesAsync(int from, int to) =>
        _service.GetMinuteCountsAsync(from, to);

    public Task<long> CountInSlotAsync(int from, int to, TimeSlot slot) =>
        _service.CountPressesInSlotAsync(from, to, Schedule, slot);

    public Task<IReadOnlyList<KeyCount>> KeysInSlotAsync(int from, int to, TimeSlot slot) =>
        _service.GetKeyCountsInSlotAsync(from, to, Schedule, slot);

    public Task<IReadOnlyList<DayTotal>> DaysInSlotAsync(int from, int to, TimeSlot slot) =>
        _service.GetDayTotalsInSlotAsync(from, to, Schedule, slot);

    public Task<long[]> HoursInSlotAsync(int from, int to, TimeSlot slot) =>
        _service.GetHourTotalsInSlotAsync(from, to, Schedule, slot);

    public IReadOnlyDictionary<KeyGroup, long> GroupCounts(IReadOnlyList<KeyCount> counts) =>
        _service.GroupKeyCounts(counts);
}
