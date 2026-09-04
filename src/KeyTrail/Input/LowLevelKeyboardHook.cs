using System.Runtime.InteropServices;
using KeyTrail.Common;

namespace KeyTrail.Input;

internal readonly record struct HookKeyEvent(int Vk, KeyEventKind Kind, bool Injected, long Ticks);

internal sealed class LowLevelKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const uint LlkhfInjected = 0x10;

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMessage(out NativeMessage lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(in NativeMessage lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(in NativeMessage lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, nuint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    private readonly Action<HookKeyEvent> _handler;
    private readonly object _downLock = new();
    private readonly Dictionary<int, bool> _down = [];
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly CancellationTokenSource _disposeCts = new();

    private HookProc? _proc;
    private nint _hookId;
    private Thread? _thread;
    private volatile bool _started;
    private volatile bool _disposed;

    public LowLevelKeyboardHook(Action<HookKeyEvent> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            return;
        }

        _started = true;
        _thread = new Thread(HookThread)
        {
            IsBackground = true,
            Name = "KeyTrailKeyboardHook",
        };
        _thread.Start();
        _ready.Wait(TimeSpan.FromSeconds(5));
        if (_hookId == 0)
        {
            throw new InvalidOperationException("Failed to install the low-level keyboard hook.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (_hookId != 0)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = 0;
            }

            if (_thread?.IsAlive == true)
            {
                PostThreadMessage((uint)_thread.ManagedThreadId, 0x0012 /* WM_QUIT */, 0, 0);
                _thread.Join(TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Error while stopping keyboard hook.", ex);
        }
        finally
        {
            _disposeCts.Dispose();
        }
    }

    private void HookThread()
    {
        try
        {
            _proc = HookCallback;
            _hookId = SetWindowsHookEx(
                WhKeyboardLl,
                _proc,
                GetModuleHandle(null),
                0);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Failed to set low-level keyboard hook.", ex);
            _ready.Set();
            return;
        }

        _ready.Set();

        try
        {
            while (!_disposed && GetMessage(out NativeMessage msg, 0, 0, 0))
            {
                _ = TranslateMessage(in msg);
                _ = DispatchMessage(in msg);
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Keyboard hook message loop stopped unexpectedly.", ex);
        }
        finally
        {
            if (_hookId != 0)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = 0;
            }
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && !_disposed)
        {
            try
            {
                KbdLlHookStruct info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                int message = (int)wParam;
                bool isDown = message is WmKeyDown or WmSysKeyDown;
                bool isUp = message is WmKeyUp or WmSysKeyUp;
                if (isDown || isUp)
                {
                    int vk = (int)info.VkCode;
                    KeyEventKind kind;

                    lock (_downLock)
                    {
                        bool wasDown = _down.TryGetValue(vk, out bool d) && d;
                        if (isUp)
                        {
                            kind = KeyEventKind.Up;
                            _down[vk] = false;
                        }
                        else if (wasDown)
                        {
                            kind = KeyEventKind.Repeat;
                        }
                        else
                        {
                            kind = KeyEventKind.Down;
                            _down[vk] = true;
                        }
                    }

                    bool injected = (info.Flags & LlkhfInjected) != 0;
                    _handler(new HookKeyEvent(vk, kind, injected, DateTime.UtcNow.Ticks));
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log.Warn($"Hook callback error: {ex.Message}");
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}

