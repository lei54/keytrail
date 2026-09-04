using System.Windows;
using System.Windows.Controls;
using KeyTrail.ViewModels;

namespace KeyTrail.Views;

public partial class SettingsView
{
    private bool _initializedLanguage;
    private bool _initializedTheme;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                EnsureInitialRadios(vm);
            }
        };
    }

    private void EnsureInitialRadios(SettingsViewModel vm)
    {
        if (!_initializedLanguage)
        {
            foreach (RadioButton radio in FindRadios("lang"))
            {
                if (radio.Tag?.ToString() == vm.Language)
                {
                    radio.IsChecked = true;
                    break;
                }
            }

            _initializedLanguage = true;
        }

        if (!_initializedTheme)
        {
            foreach (RadioButton radio in FindRadios("theme"))
            {
                if (radio.Tag?.ToString() == (vm.IsDark ? "Dark" : "Light"))
                {
                    radio.IsChecked = true;
                    break;
                }
            }

            _initializedTheme = true;
        }
    }

    private IEnumerable<RadioButton> FindRadios(string group)
    {
        return FindVisualChildren<RadioButton>(this).Where(r => r.GroupName == group);
    }

    private void Language_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initializedLanguage || DataContext is not SettingsViewModel vm ||
            sender is not RadioButton { Tag: string code })
        {
            return;
        }

        vm.SetLanguage(code);
        _initializedTheme = false;
        EnsureInitialRadios(vm);
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initializedTheme || DataContext is not SettingsViewModel vm ||
            sender is not RadioButton { Tag: string theme })
        {
            return;
        }

        vm.SetTheme(theme == "Dark");
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }
}

