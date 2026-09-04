using KeyTrail.Common;
using KeyTrail.Models;

namespace KeyTrail.Data;

public sealed class StatisticsService
{
    private readonly KeyboardDatabase _db;

    public StatisticsService(KeyboardDatabase db)
    {
        _db = db;
    }

    public Task<long> CountPressesAsync(int fromDay, int toDay) =>
        Task.Run(() => _db.CountPresses(fromDay, toDay));

    public Task<IReadOnlyList<KeyCount>> GetKeyCountsAsync(int fromDay, int toDay) =>
        Task.Run(() => _db.GetKeyCounts(fromDay, toDay));

    public Task<IReadOnlyList<DayTotal>> GetDayTotalsAsync(int fromDay, int toDay) =>
        Task.Run(() => _db.GetDayTotals(fromDay, toDay));

    public async Task<long[]> GetHourTotalsAsync(int fromDay, int toDay)
    {
        IReadOnlyList<MinuteCount> minutes = await Task.Run(() => _db.GetMinuteCounts(fromDay, toDay));
        var hours = new long[24];
        foreach (MinuteCount m in minutes)
        {
            hours[m.Minute / 60] += m.Count;
        }

        return hours;
    }

    public async Task<long[]> GetSlotTotalsAsync(
        int fromDay,
        int toDay,
        SlotSchedule schedule)
    {
        IReadOnlyList<MinuteCount> minutes = await Task.Run(() => _db.GetMinuteCounts(fromDay, toDay));
        var slots = new long[SlotSchedule.Count];
        foreach (MinuteCount m in minutes)
        {
            slots[(int)schedule.SlotForMinute(m.Minute)] += m.Count;
        }

        return slots;
    }

    public Task<IReadOnlyList<StoredEvent>> GetEventsOrderedAsync(int fromDay, int toDay) =>
        Task.Run(() => _db.GetEventsOrdered(fromDay, toDay));

    public Task<IReadOnlyList<MinuteCount>> GetMinuteCountsAsync(int fromDay, int toDay) =>
        Task.Run(() => _db.GetMinuteCounts(fromDay, toDay));

    public Task<long> CountPressesInSlotAsync(
        int fromDay,
        int toDay,
        SlotSchedule schedule,
        TimeSlot slot)
    {
        (int start, int end) = SlotMinutes(schedule, slot);
        return Task.Run(() => _db.CountPressesInMinutes(fromDay, toDay, start, end));
    }

    public Task<IReadOnlyList<KeyCount>> GetKeyCountsInSlotAsync(
        int fromDay,
        int toDay,
        SlotSchedule schedule,
        TimeSlot slot)
    {
        (int start, int end) = SlotMinutes(schedule, slot);
        return Task.Run(() => _db.GetKeyCountsInMinutes(fromDay, toDay, start, end));
    }

    public Task<IReadOnlyList<DayTotal>> GetDayTotalsInSlotAsync(
        int fromDay,
        int toDay,
        SlotSchedule schedule,
        TimeSlot slot)
    {
        (int start, int end) = SlotMinutes(schedule, slot);
        return Task.Run(() => _db.GetDayTotalsInMinutes(fromDay, toDay, start, end));
    }

    public async Task<long[]> GetHourTotalsInSlotAsync(
        int fromDay,
        int toDay,
        SlotSchedule schedule,
        TimeSlot slot)
    {
        (int start, int end) = SlotMinutes(schedule, slot);
        IReadOnlyList<MinuteCount> minutes = await Task.Run(
            () => _db.GetMinuteCountsInMinutes(fromDay, toDay, start, end));
        var hours = new long[24];
        foreach (MinuteCount m in minutes)
        {
            hours[m.Minute / 60] += m.Count;
        }

        return hours;
    }

    private static (int Start, int End) SlotMinutes(SlotSchedule schedule, TimeSlot slot)
    {
        int start = schedule.StartHour(slot) * 60;
        int end = schedule.EndHour(slot) * 60;
        return (start, end);
    }

    public async Task<GapStats> GetGapStatsAsync(int fromDay, int toDay)
    {
        IReadOnlyList<StoredEvent> events = await GetEventsOrderedAsync(fromDay, toDay);
        return InsightCalculator.CalculateGaps(events);
    }

    public async Task<IReadOnlyDictionary<int, GapStats>> GetDailyGapStatsAsync(int fromDay, int toDay)
    {
        IReadOnlyList<StoredEvent> events = await GetEventsOrderedAsync(fromDay, toDay);
        return InsightCalculator.CalculatePerDay(events);
    }

    public async Task<IReadOnlyList<(int Modifiers, int Vk, int Count)>> GetShortcutsAsync(
        int fromDay,
        int toDay)
    {
        IReadOnlyList<StoredEvent> events = await GetEventsOrderedAsync(fromDay, toDay);
        return InsightCalculator.CalculateShortcuts(events);
    }

    public IReadOnlyDictionary<KeyGroup, long> GroupKeyCounts(IReadOnlyList<KeyCount> counts)
    {
        var result = new Dictionary<KeyGroup, long>();
        foreach (KeyCount c in counts)
        {
            KeyGroup g = KeyCatalog.Group(c.Vk);
            result[g] = result.TryGetValue(g, out long v) ? v + c.Count : c.Count;
        }

        return result;
    }
}
