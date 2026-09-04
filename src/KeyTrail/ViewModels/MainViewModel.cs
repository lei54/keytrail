using System.Collections.ObjectModel;
using System.Windows.Input;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Localization;
using KeyTrail.Mvvm;
using KeyTrail.Settings;
using KeyTrail.Services;

namespace KeyTrail.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly RecordingController _recording;
    private object _current;

    public MainViewModel(
        StatisticsProvider stats,
        SettingsStore settings,
        RecordingController recording,
        KeyboardDatabase database,
        AutostartService autostart)
    {
        _recording = recording;
        Home = new HomeViewModel(stats, settings);
        History = new HistoryViewModel(stats, settings);
        Insights = new InsightsViewModel(stats, settings);
        SettingsPage = new SettingsViewModel(settings, database, autostart);
        _current = Home;

        ShowHomeCommand = new RelayCommand(_ => Navigate("Home"));
        ShowHistoryCommand = new RelayCommand(_ => Navigate("History"));
        ShowInsightsCommand = new RelayCommand(_ => Navigate("Insights"));
        ShowSettingsCommand = new RelayCommand(_ => Navigate("Settings"));
        ToggleRecordingCommand = new RelayCommand(_ => ToggleRecording());

        LocalizationService.LanguageChanged += () => RefreshRecordingLabels();
        _recording.StateChanged += _ => RefreshRecordingLabels();
        RefreshRecordingLabels();
    }

    public HomeViewModel Home { get; }
    public HistoryViewModel History { get; }
    public InsightsViewModel Insights { get; }
    public SettingsViewModel SettingsPage { get; }

    public ObservableCollection<string> NavLabels { get; } = [];

    public object Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public ICommand ShowHomeCommand { get; }
    public ICommand ShowHistoryCommand { get; }
    public ICommand ShowInsightsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ToggleRecordingCommand { get; }

    public string StatusText { get; private set; } = string.Empty;
    public bool IsRecording => _recording.IsRecording;

    public void Navigate(string? page)
    {
        object vm = page switch
        {
            "History" => History,
            "Insights" => Insights,
            "Settings" => SettingsPage,
            _ => Home,
        };
        Current = vm;
        if (vm is IActivatingPage activating)
        {
            _ = ActivateAsync(activating);
        }
    }

    private static async Task ActivateAsync(IActivatingPage page) => await page.ActivateAsync();

    private void ToggleRecording()
    {
        if (_recording.IsRecording)
        {
            _recording.Stop();
        }
        else
        {
            _recording.Start();
        }

        OnPropertyChanged(nameof(IsRecording));
        RefreshRecordingLabels();
    }

    private void RefreshRecordingLabels()
    {
        StatusText = LocalizationService.Get(_recording.IsRecording ? "Status.Recording" : "Status.Paused");
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsRecording));
    }
}

public interface IActivatingPage
{
    Task ActivateAsync();
}
