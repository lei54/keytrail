using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KeyTrail.Settings;
using KeyTrail.ViewModels;

namespace KeyTrail;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settings;
    private bool _allowClose;

    public MainWindow(MainViewModel viewModel, SettingsStore settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settings = settings;
        Closed += MainWindow_Closed;
        HomeRadio.IsChecked = true;
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private void NavRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not RadioButton button)
        {
            return;
        }

        vm.Navigate(button.Tag?.ToString());
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || !_settings.Current.MinimizeToTray)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (!_allowClose && !_settings.Current.MinimizeToTray && Application.Current is App app)
        {
            app.RequestExit();
        }
    }
}
