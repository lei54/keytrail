namespace KeyTrail.Common;

public static class DateMath
{
    public static int ToDayNumber(DateTime local) => local.Year * 10000 + local.Month * 100 + local.Day;

    public static int ToDayNumber(DateOnly date) => date.Year * 10000 + date.Month * 100 + date.Day;

    public static DateOnly ToDate(int dayNumber) =>
        new(dayNumber / 10000, (dayNumber / 100) % 100, dayNumber % 100);

    public static DateTime ToLocalFromUtcMs(long utcMs) =>
        DateTimeOffset.FromUnixTimeMilliseconds(utcMs).LocalDateTime;

    public static long ToUtcMs(DateTime local) =>
        new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUnixTimeMilliseconds();

    public static DateOnly StartOfWeek(DateOnly date)
    {
        int offset = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
        return date.AddDays(-offset);
    }

    public static (int StartDay, int EndDay) GetRange(DateOnly date, StatsPeriod period)
    {
        return period switch
        {
            StatsPeriod.Day => (ToDayNumber(date), ToDayNumber(date)),
            StatsPeriod.Week => (
                ToDayNumber(StartOfWeek(date)),
                ToDayNumber(StartOfWeek(date).AddDays(6))),
            StatsPeriod.Month => (
                ToDayNumber(new DateOnly(date.Year, date.Month, 1)),
                ToDayNumber(new DateOnly(date.Year, date.Month, 1).AddMonths(1).AddDays(-1))),
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };
    }

    public static string FormatDay(int dayNumber) => ToDate(dayNumber).ToString("yyyy-MM-dd");
}
