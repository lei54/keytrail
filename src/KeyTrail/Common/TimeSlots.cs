namespace KeyTrail.Common;

public sealed class SlotSchedule
{
    public const int Count = 4;
    public const int DefaultMorningEnd = 6;
    public const int DefaultNoonEnd = 12;
    public const int DefaultEveningEnd = 18;

    private readonly int[] _ends;

    public SlotSchedule(int morningEnd = DefaultMorningEnd, int noonEnd = DefaultNoonEnd, int eveningEnd = DefaultEveningEnd)
    {
        if (morningEnd <= 0 || morningEnd >= noonEnd || noonEnd >= eveningEnd || eveningEnd >= 24)
        {
            throw new ArgumentException("Slot boundaries must satisfy 0 < morning < noon < evening < 24.");
        }

        _ends = [morningEnd, noonEnd, eveningEnd];
    }

    public IReadOnlyList<int> EndHours => _ends;

    public int EndHour(TimeSlot slot) => slot switch
    {
        TimeSlot.Night => _ends[0],
        TimeSlot.Morning => _ends[1],
        TimeSlot.Noon => _ends[2],
        TimeSlot.Evening => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    public int StartHour(TimeSlot slot) => slot switch
    {
        TimeSlot.Night => 0,
        TimeSlot.Morning => _ends[0],
        TimeSlot.Noon => _ends[1],
        TimeSlot.Evening => _ends[2],
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    public TimeSlot SlotForHour(int hour)
    {
        if (hour < 0 || hour > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(hour));
        }

        if (hour < _ends[0]) return TimeSlot.Night;
        if (hour < _ends[1]) return TimeSlot.Morning;
        if (hour < _ends[2]) return TimeSlot.Noon;
        return TimeSlot.Evening;
    }

    public TimeSlot SlotForMinute(int minute) => SlotForHour(minute / 60);

    public static string NameKey(TimeSlot slot) => slot switch
    {
        TimeSlot.Night => "Slot.Night",
        TimeSlot.Morning => "Slot.Morning",
        TimeSlot.Noon => "Slot.Noon",
        TimeSlot.Evening => "Slot.Evening",
        _ => string.Empty,
    };
}

