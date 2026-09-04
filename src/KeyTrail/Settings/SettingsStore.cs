using System.IO;
using System.Text.Json;
using KeyTrail.Common;

namespace KeyTrail.Settings;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly object _gate = new();

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        lock (_gate)
        {
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    string json = File.ReadAllText(AppPaths.SettingsFile);
                    AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded is not null)
                    {
                        Current = Normalize(loaded);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log.Warn($"Failed to read settings, using defaults. {ex.Message}");
            }

            Current = Normalize(new AppSettings());
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            try
            {
                AppPaths.EnsureCreated();
                string json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(AppPaths.SettingsFile, json);
            }
            catch (Exception ex)
            {
                Diagnostics.Log.Error($"Failed to save settings. {ex.Message}");
            }
        }
    }

    public SlotSchedule GetSchedule()
    {
        lock (_gate)
        {
            return new SlotSchedule(Current.MorningEndHour, Current.NoonEndHour, Current.EveningEndHour);
        }
    }

    private static AppSettings Normalize(AppSettings s)
    {
        if (s is null) s = new AppSettings();
        if (string.IsNullOrWhiteSpace(s.Theme)) s.Theme = "Light";
        if (string.IsNullOrWhiteSpace(s.Language)) s.Language = "zh";
        if (s.MorningEndHour is < 1 or > 11) s.MorningEndHour = 6;
        if (s.NoonEndHour <= s.MorningEndHour || s.NoonEndHour is < 2 or > 17) s.NoonEndHour = 12;
        if (s.EveningEndHour <= s.NoonEndHour || s.EveningEndHour is < 3 or > 23) s.EveningEndHour = 18;
        if (s.RetentionDays < 0) s.RetentionDays = 0;
        return s;
    }
}
