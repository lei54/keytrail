namespace KeyTrail.Data;

public readonly record struct HourCount(int Hour, long Count);
public readonly record struct DayTotal(int Day, long Count);
public readonly record struct MinuteCount(int Day, int Minute, long Count);
public readonly record struct KeyCount(int Vk, long Count);
public readonly record struct GroupCount(string GroupKey, long Count);

public sealed record GapStats(
    long Presses,
    double AverageGapMs,
    double MedianGapMs,
    int BurstCount,
    double AverageBurstLength,
    int ShortBreaks,
    int LongBreaks,
    long ShortBreakMs,
    long LongBreakMs,
    int ActiveMinutes,
    int LongestActiveMinutes);

public sealed record DayStats(
    int Day,
    long Total,
    int ActiveMinutes,
    double AverageGapMs,
    double MedianGapMs,
    int Bursts,
    int LongBreaks,
    int LongestActiveMinutes);

