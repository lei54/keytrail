using System.Windows;

namespace KeyTrail.Localization;

public static class ThemeService
{
    private static readonly string[] KnownFiles = ["Light.xaml", "Dark.xaml"];

    public static event Action? ThemeChanged;

    public static string CurrentName { get; private set; } = "Light";

    public static void Apply(string theme)
    {
        string fileName = theme == "Dark" ? "Dark.xaml" : "Light.xaml";
        if (CurrentName == (theme == "Dark" ? "Dark" : "Light"))
        {
            return;
        }

        CurrentName = theme == "Dark" ? "Dark" : "Light";
        ResourceDictionary themeDictionary = new()
        {
            Source = new Uri($"/KeyTrail;component/Themes/{fileName}", UriKind.Relative),
        };

        IList<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;
        int themeIndex = FindThemeIndex(dictionaries);
        if (themeIndex >= 0)
        {
            dictionaries[themeIndex] = themeDictionary;
        }

        ThemeChanged?.Invoke();
    }

    public static void EnsureThemeDictionary()
    {
        if (FindThemeIndex(Application.Current.Resources.MergedDictionaries) < 0)
        {
            IList<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;
            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"/KeyTrail;component/Themes/{CurrentName}.xaml", UriKind.Relative),
            });
        }
    }

    private static int FindThemeIndex(IList<ResourceDictionary> dictionaries)
    {
        for (int i = 0; i < dictionaries.Count; i++)
        {
            string? source = dictionaries[i].Source?.OriginalString;
            if (source is not null && KnownFiles.Any(f => source.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }
}

