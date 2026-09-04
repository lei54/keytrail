using System.Windows;
using System.Threading;
using System.Windows.Threading;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Diagnostics;
using KeyTrail.Localization;
using KeyTrail.Settings;
using KeyTrail.Services;
using KeyTrail.ViewModels;

namespace KeyTrail;

public partial class App : Application
{
    private const string SingleInstanceEventName = "KeyTrail_ShowMainWindow";

    private EventWaitHandle? _showSignal;
    private SettingsStore? _settings;
    private KeyboardDatabase? _database;
    private RecordingController? _recording;
    private TrayIconService? _tray;
    private MainWindow? _window;

    public static RecordingController? Recorder { get; private set; }

    public void RequestExit() => ExitApplication();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, SingleInstanceEventName, out createdNew);
        if (!createdNew)
        {
            _ = _showSignal.Set();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error("Unhandled dispatcher exception.", args.Exception);
            args.Handled = true;
        };

        try
        {
            AppPaths.EnsureCreated();
            _settings = new SettingsStore();
            _settings.Load();

            _database = new KeyboardDatabase();
            _database.Open();

            var stats = new StatisticsService(_database);
            var provider = new StatisticsProvider(stats, _settings);
            _recording = new RecordingController(_database);
            Recorder = _recording;
            var autostart = new AutostartService();

            LocalizationService.Apply(LocalizationService.ParseLanguage(_settings.Current.Language));
            ThemeService.EnsureThemeDictionary();
            ThemeService.Apply(_settings.Current.Theme);

            _recording.StateChanged += recording =>
            {
                _tray?.SetRecording(recording);
            };

            var mainVm = new MainViewModel(provider, _settings, _recording, _database, autostart);
            _window = new MainWindow(mainVm, _settings);
            MainWindow = _window;
            _window.Show();

            if (e.Args.Contains("--tray", StringComparer.OrdinalIgnoreCase))
            {
                _window.Hide();
            }

            if (_settings.Current.RecordOnStartup)
            {
                _recording.Start();
            }

            if (_settings.Current.RetentionDays > 0)
            {
                KeyboardDatabase db = _database;
                int threshold = DateMath.ToDayNumber(DateTime.Now.AddDays(-_settings.Current.RetentionDays));
                Task.Run(() => db.DeleteOlderThan(threshold));
            }

            _tray = new TrayIconService();
            _tray.SetRecording(_recording.IsRecording);
            _tray.ShowRequested += () => Dispatcher.Invoke(ShowMainWindow);
            _tray.ToggleRequested += () => Dispatcher.Invoke(_recording.Toggle);
            _tray.ExitRequested += ExitApplication;

            Task _ = Task.Run(() => _showSignal.WaitOne()).ContinueWith(_ =>
                Dispatcher.Invoke(ShowMainWindow));
        }
        catch (Exception ex)
        {
            Log.Error("Startup failed.", ex);
            _ = MessageBox.Show(
                $"KeyTrail failed to start.\n\n{ex.Message}",
                "KeyTrail",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _recording?.Dispose();
        _tray?.Dispose();
        _database?.Dispose();
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    private void ExitApplication()
    {
        _window?.ForceClose();
        Shutdown();
    }
}
