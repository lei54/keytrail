using System.Windows;
using System.Windows.Threading;
using KeyTrail.Models;
using KeyTrail.ViewModels;

namespace KeyTrail.Views;

public partial class HomeView
{
    private readonly DispatcherTimer _pulseTimer;
    private readonly DispatcherTimer _refreshTimer;
    private bool _refreshing;

    public HomeView()
    {
        InitializeComponent();
        _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _pulseTimer.Tick += (_, _) => DrainPulses();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _refreshTimer.Tick += async (_, _) =>
        {
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
            try
            {
                if (DataContext is HomeViewModel vm)
                {
                    await vm.LoadAsync();
                }
            }
            finally
            {
                _refreshing = false;
            }
        };
        Loaded += (_, _) =>
        {
            _pulseTimer.Start();
            _refreshTimer.Start();
        };
        Unloaded += (_, _) =>
        {
            _pulseTimer.Stop();
            _refreshTimer.Stop();
        };
    }

    private void DrainPulses()
    {
        var recording = KeyTrail.App.Recorder;
        if (recording is null || !recording.IsRecording)
        {
            return;
        }

        foreach (LivePress press in recording.DrainRecentPresses())
        {
            HeatView.Pulse(press.Vk);
        }
    }
}
