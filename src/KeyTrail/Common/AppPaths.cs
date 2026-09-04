using System.IO;

namespace KeyTrail.Common;

public static class AppPaths
{
    private static readonly Lazy<string> RootLazy = new(() =>
    {
        string? overridePath = Environment.GetEnvironmentVariable("KEYTRAIL_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeyTrail");
    });

    public static string Root => RootLazy.Value;
    public static string DatabaseFile => Path.Combine(Root, "keyboard.db");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string LogDirectory => Path.Combine(Root, "logs");
    public static string LogFile => Path.Combine(LogDirectory, "app.log");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogDirectory);
    }
}
