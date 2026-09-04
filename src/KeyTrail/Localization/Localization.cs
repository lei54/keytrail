using System.Globalization;
using System.Windows;
using KeyTrail.Common;

namespace KeyTrail.Localization;

public static class LocalizationService
{
    public static event Action? LanguageChanged;

    public static string Get(string key)
    {
        object? value = Application.Current.TryFindResource(key);
        return value as string ?? key;
    }

    public static string Code(string key) => Get(key);

    public static void Apply(AppLanguage language)
    {
        string fileName = language switch
        {
            AppLanguage.Chinese => "zh",
            AppLanguage.English => "en",
            AppLanguage.Japanese => "ja",
            _ => "zh",
        };

        CultureInfo culture = language switch
        {
            AppLanguage.Chinese => CultureInfo.GetCultureInfo("zh-CN"),
            AppLanguage.English => CultureInfo.GetCultureInfo("en-US"),
            AppLanguage.Japanese => CultureInfo.GetCultureInfo("ja-JP"),
            _ => CultureInfo.GetCultureInfo("zh-CN"),
        };

        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;

        ResourceDictionary dictionary = new()
        {
            Source = new Uri($"/KeyTrail;component/Localization/{fileName}.xaml", UriKind.Relative),
        };
        ResourceDictionary current = (Application.Current.Resources.MergedDictionaries.Count > 0
            ? Application.Current.Resources.MergedDictionaries[0]
            : null)!;
        if (current is not null)
        {
            Application.Current.Resources.MergedDictionaries[0] = dictionary;
        }

        LanguageChanged?.Invoke();
    }

    public static AppLanguage ParseLanguage(string value) => value switch
    {
        "en" => AppLanguage.English,
        "ja" => AppLanguage.Japanese,
        _ => AppLanguage.Chinese,
    };

    public static string LanguageCode(AppLanguage language) => language switch
    {
        AppLanguage.Chinese => "zh",
        AppLanguage.English => "en",
        AppLanguage.Japanese => "ja",
        _ => "zh",
    };
}

