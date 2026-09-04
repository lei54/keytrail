namespace KeyTrail.Settings;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public string Language { get; set; } = "zh";
    public bool AutoStart { get; set; }
    public bool RecordOnStartup { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public int MorningEndHour { get; set; } = 6;
    public int NoonEndHour { get; set; } = 12;
    public int EveningEndHour { get; set; } = 18;
    public int RetentionDays { get; set; }
}

