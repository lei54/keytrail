using KeyTrail.Common;
using KeyTrail.Diagnostics;
using Microsoft.Win32;

namespace KeyTrail.Services;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeyTrail";

    public bool IsEnabled()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false)
                ?? throw new InvalidOperationException("Run key missing.");
            return key.GetValue(ValueName) is string value && value.Contains("KeyTrail", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read autostart state: {ex.Message}");
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enabled)
            {
                string exe = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Process path unavailable.");
                key.SetValue(ValueName, $"\"{exe}\" --tray");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to update autostart setting.", ex);
            throw;
        }
    }
}

