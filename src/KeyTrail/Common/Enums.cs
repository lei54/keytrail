namespace KeyTrail.Common;

public enum KeyEventKind : byte
{
    Down = 0,
    Up = 1,
    Repeat = 2,
}

public enum StatsPeriod
{
    Day = 0,
    Week = 1,
    Month = 2,
}

public enum TimeSlot : byte
{
    Night = 0,
    Morning = 1,
    Noon = 2,
    Evening = 3,
}

public enum AppLanguage
{
    Chinese,
    English,
    Japanese,
}

