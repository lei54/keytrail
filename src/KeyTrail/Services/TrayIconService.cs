using System.Drawing;
using System.Windows.Forms;
using KeyTrail.Localization;

namespace KeyTrail.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu = new();
    private bool _recording;

    public TrayIconService()
    {
        _icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "KeyTrail.exe") ??
                   SystemIcons.Application,
            Text = "KeyTrail 键迹",
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
        RebuildMenu();
        LocalizationService.LanguageChanged += RebuildMenu;
    }

    public event Action? ShowRequested;
    public event Action? ToggleRequested;
    public event Action? ExitRequested;

    public void SetRecording(bool recording)
    {
        _recording = recording;
        RebuildMenu();
    }

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= RebuildMenu;
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        var show = new ToolStripMenuItem(LocalizationService.Get("Tray.Show"));
        show.Click += (_, _) => ShowRequested?.Invoke();

        var toggle = new ToolStripMenuItem(
            LocalizationService.Get(_recording ? "Tray.Pause" : "Tray.Start"));
        toggle.Click += (_, _) => ToggleRequested?.Invoke();

        var exit = new ToolStripMenuItem(LocalizationService.Get("Tray.Exit"));
        exit.Click += (_, _) => ExitRequested?.Invoke();

        _ = _menu.Items.Add(show);
        _ = _menu.Items.Add(toggle);
        _ = _menu.Items.Add(new ToolStripSeparator());
        _ = _menu.Items.Add(exit);
        _icon.ContextMenuStrip = _menu;
    }
}

