using KeyTrail.Common;
using KeyTrail.Models;

namespace KeyTrail.Data;

public static class InsightCalculator
{
    private const int BurstThresholdMs = 2000;
    private const int ShortBreakMs = 5000;
    private const int LongBreakMs = 60000;

    public static GapStats CalculateGaps(IReadOnlyList<StoredEvent> events)
    {
        List<long> pressTimes = [];
        HashSet<int> activeMinutes = [];
        foreach (StoredEvent e in events)
        {
            if (e.Kind == KeyEventKind.Down)
            {
                pressTimes.Add(e.TsUtcMs);
                _ = activeMinutes.Add(e.Day * 1440 + e.Minute);
            }
        }

        if (pressTimes.Count == 0)
        {
            return new GapStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var gaps = new List<long>(pressTimes.Count - 1);
        var burstLengths = new List<int>();
        int currentBurst = 1;
        int shortBreaks = 0;
        int longBreaks = 0;
        long shortBreakMs = 0;
        long longBreakMs = 0;
        long segmentStart = pressTimes[0];
        long longestSegmentMs = 0;

        for (int i = 1; i < pressTimes.Count; i++)
        {
            long gap = pressTimes[i] - pressTimes[i - 1];
            if (gap < 0)
            {
                continue; // Defensive: timestamps must be monotonic.
            }

            gaps.Add(gap);
            if (gap <= BurstThresholdMs)
            {
                currentBurst++;
            }
            else
            {
                if (currentBurst >= 2)
                {
                    burstLengths.Add(currentBurst);
                }

                currentBurst = 1;

                if (gap < LongBreakMs)
                {
                    shortBreaks++;
                    shortBreakMs += gap;
                }
                else
                {
                    longBreaks++;
                    longBreakMs += gap;
                    longestSegmentMs = Math.Max(longestSegmentMs, pressTimes[i - 1] - segmentStart);
                    segmentStart = pressTimes[i];
                }
            }
        }

        if (currentBurst >= 2)
        {
            burstLengths.Add(currentBurst);
        }

        longestSegmentMs = Math.Max(longestSegmentMs, pressTimes[^1] - segmentStart);
        gaps.Sort();
        double median = gaps.Count > 0
            ? gaps.Count % 2 == 1
                ? gaps[gaps.Count / 2]
                : (gaps[gaps.Count / 2 - 1] + gaps[gaps.Count / 2]) / 2.0
            : 0;
        double average = gaps.Count > 0 ? gaps.Average() : 0;
        double averageBurst = burstLengths.Count > 0 ? burstLengths.Average() : 0;

        return new GapStats(
            pressTimes.Count,
            average,
            median,
            burstLengths.Count,
            averageBurst,
            shortBreaks,
            longBreaks,
            shortBreakMs,
            longBreakMs,
            activeMinutes.Count,
            (int)Math.Ceiling(longestSegmentMs / 60_000.0));
    }

    public static IReadOnlyDictionary<int, GapStats> CalculatePerDay(
        IReadOnlyList<StoredEvent> events)
    {
        var groups = events.GroupBy(e => e.Day).OrderBy(g => g.Key).ToList();
        var result = new Dictionary<int, GapStats>();
        foreach (IGrouping<int, StoredEvent> group in groups)
        {
            result[group.Key] = CalculateGaps(group.ToArray());
        }

        return result;
    }

    public static IReadOnlyList<(int Modifiers, int Vk, int Count)> CalculateShortcuts(
        IReadOnlyList<StoredEvent> events)
    {
        var counts = new Dictionary<(int Modifiers, int Vk), int>();
        int held = 0;

        foreach (StoredEvent e in events)
        {
            int modifierBit = ModifierBit(e.Vk);
            if (modifierBit != 0)
            {
                if (e.Kind == KeyEventKind.Down)
                {
                    held |= modifierBit;
                }
                else if (e.Kind == KeyEventKind.Up)
                {
                    held &= ~modifierBit;
                }

                continue;
            }

            if (e.Kind == KeyEventKind.Down && held != 0)
            {
                var key = (held, e.Vk);
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            }
        }

        return counts
            .OrderByDescending(kvp => kvp.Value)
            .Take(12)
            .Select(kvp => (kvp.Key.Modifiers, kvp.Key.Vk, kvp.Value))
            .ToArray();
    }

    public static long SumShortcutActivations(IReadOnlyList<(int Modifiers, int Vk, int Count)> shortcuts) =>
        shortcuts.Sum(s => (long)s.Count);

    private static int ModifierBit(int vk)
    {
        return vk switch
        {
            0x11 or 0xA2 or 0xA3 => 1,
            0x10 or 0xA0 or 0xA1 => 2,
            0x12 or 0xA4 or 0xA5 => 4,
            0x5B or 0x5C => 8,
            _ => 0,
        };
    }
}

