using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Localization;
using KeyTrail.Models;
using KeyTrail.Mvvm;
using KeyTrail.Settings;
using KeyTrail.Services;
using Microsoft.Win32;

namespace KeyTrail.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _settings;
    private readonly KeyboardDatabase _db;
    private readonly AutostartService _autostart;

    private string _language = "zh";
    private bool _isDark;
    private bool _autoStart;
    private bool _recordOnStartup = true;
    private bool _minimizeToTray = true;
    private string _morningEnd = "6";
    private string _noonEnd = "12";
    private string _eveningEnd = "18";
    private string _retention = "0";
    private string _dataPath = string.Empty;
    private bool _isBusy;

    public SettingsViewModel(SettingsStore settings, KeyboardDatabase db, AutostartService autostart)
    {
        _settings = settings;
        _db = db;
        _autostart = autostart;
        Load();
    }

    public string Language
    {
        get => _language;
        private set => SetProperty(ref _language, value);
    }

    public bool IsDark
    {
        get => _isDark;
        private set => SetProperty(ref _isDark, value);
    }

    public bool AutoStart
    {
        get => _autoStart;
        set
        {
            if (SetProperty(ref _autoStart, value))
            {
                try
                {
                    _autostart.SetEnabled(value);
                    _settings.Current.AutoStart = value;
                    _settings.Save();
                }
                catch
                {
                    _ = MessageBox.Show(
                        LocalizationService.Get("Settings.AutostartError"),
                        LocalizationService.Get("App.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
    }

    public bool RecordOnStartup
    {
        get => _recordOnStartup;
        set
        {
            if (SetProperty(ref _recordOnStartup, value))
            {
                _settings.Current.RecordOnStartup = value;
                _settings.Save();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (SetProperty(ref _minimizeToTray, value))
            {
                _settings.Current.MinimizeToTray = value;
                _settings.Save();
            }
        }
    }

    public string MorningEnd
    {
        get => _morningEnd;
        set => SetProperty(ref _morningEnd, value);
    }

    public string NoonEnd
    {
        get => _noonEnd;
        set => SetProperty(ref _noonEnd, value);
    }

    public string EveningEnd
    {
        get => _eveningEnd;
        set => SetProperty(ref _eveningEnd, value);
    }

    public string Retention
    {
        get => _retention;
        set => SetProperty(ref _retention, value);
    }

    public string DataPath
    {
        get => _dataPath;
        private set => SetProperty(ref _dataPath, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public RelayCommand SaveScheduleCommand => new(_ => SaveSchedule());
    public AsyncRelayCommand ClearCommand => new(async () => await ClearAsync());
    public AsyncRelayCommand ExportCommand => new(async () => await ExportAsync());
    public RelayCommand OpenDataCommand => new(_ => OpenDataFolder());

    public void SetLanguage(string code)
    {
        if (Language == code)
        {
            return;
        }

        Language = code;
        _settings.Current.Language = code;
        _settings.Save();
        LocalizationService.Apply(LocalizationService.ParseLanguage(code));
        Load();
    }

    public void SetTheme(bool dark)
    {
        IsDark = dark;
        _settings.Current.Theme = dark ? "Dark" : "Light";
        _settings.Save();
        ThemeService.Apply(_settings.Current.Theme);
    }

    private void Load()
    {
        _settings.Load();
        Language = _settings.Current.Language;
        IsDark = string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
        AutoStart = _settings.Current.AutoStart;
        RecordOnStartup = _settings.Current.RecordOnStartup;
        MinimizeToTray = _settings.Current.MinimizeToTray;
        MorningEnd = _settings.Current.MorningEndHour.ToString(CultureInfo.InvariantCulture);
        NoonEnd = _settings.Current.NoonEndHour.ToString(CultureInfo.InvariantCulture);
        EveningEnd = _settings.Current.EveningEndHour.ToString(CultureInfo.InvariantCulture);
        Retention = _settings.Current.RetentionDays.ToString(CultureInfo.InvariantCulture);
        DataPath = AppPaths.Root;
    }

    private void SaveSchedule()
    {
        if (!TryParseBoundaries(out int morning, out int noon, out int evening))
        {
            _ = MessageBox.Show(
                LocalizationService.Get("Settings.ScheduleHint"),
                LocalizationService.Get("App.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Load();
            return;
        }

        _settings.Current.MorningEndHour = morning;
        _settings.Current.NoonEndHour = noon;
        _settings.Current.EveningEndHour = evening;
        _settings.Current.RetentionDays = int.TryParse(Retention, NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) && r >= 0
            ? r
            : 0;
        _settings.Save();

        if (_settings.Current.RetentionDays > 0)
        {
            int threshold = DateMath.ToDayNumber(DateTime.Now.AddDays(-_settings.Current.RetentionDays));
            Task.Run(() => _db.DeleteOlderThan(threshold));
        }
    }

    private async Task ClearAsync()
    {
        MessageBoxResult result = MessageBox.Show(
            LocalizationService.Get("Settings.ClearConfirm"),
            LocalizationService.Get("App.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(_db.ClearAll);
            _ = MessageBox.Show(
                LocalizationService.Get("Settings.ClearDone"),
                LocalizationService.Get("App.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = LocalizationService.Get("Settings.ExportCsv"),
            Filter = "CSV|*.csv",
            FileName = $"KeyTrail-export-{DateTime.Now:yyyyMMdd}.csv",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("date,total_presses");
                foreach (DayTotal day in _db.GetDayTotals(19000101, 99991231))
                {
                    sb.AppendLine($"{DateMath.ToDate(day.Day):yyyy-MM-dd},{day.Count}");
                }

                sb.AppendLine();
                sb.AppendLine("key,key_name,total");
                foreach (KeyCount key in _db.GetKeyCounts(19000101, 99991231))
                {
                    sb.AppendLine($"{key.Vk},{KeyCatalog.Name(key.Vk)},{key.Count}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            });
            _ = MessageBox.Show(
                $"{LocalizationService.Get("Settings.ExportDone")}\n{dialog.FileName}",
                LocalizationService.Get("App.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenDataFolder()
    {
        AppPaths.EnsureCreated();
        Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.Root,
            UseShellExecute = true,
        });
    }

    private bool TryParseBoundaries(out int morning, out int noon, out int evening)
    {
        morning = int.TryParse(MorningEnd, out int m) ? m : 0;
        noon = int.TryParse(NoonEnd, out int n) ? n : 0;
        evening = int.TryParse(EveningEnd, out int e) ? e : 0;
        return morning > 0 && morning < noon && noon < evening && evening < 24;
    }
}
