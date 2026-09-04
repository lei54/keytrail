using System.Threading.Channels;
using System.Diagnostics;
using KeyTrail.Common;
using KeyTrail.Data;
using KeyTrail.Diagnostics;
using KeyTrail.Input;
using KeyTrail.Models;

namespace KeyTrail.Services;

public sealed class RecordingController : IDisposable
{
    private const int FlushIntervalMs = 1500;
    private const int FlushThreshold = 200;

    private readonly KeyboardDatabase _database;
    private readonly object _pressLock = new();
    private readonly List<LivePress> _recentPresses = [];
    private readonly Channel<HookKeyEvent> _channel;
    private readonly CancellationTokenSource _cts = new();

    private LowLevelKeyboardHook? _hook;
    private Task? _worker;
    private volatile bool _recording;
    private bool _disposed;

    public event Action<bool>? StateChanged;

    public RecordingController(KeyboardDatabase database)
    {
        _database = database;
        _channel = Channel.CreateBounded<HookKeyEvent>(new BoundedChannelOptions(8192)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });
    }

    public bool IsRecording => _recording;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_recording)
        {
            return;
        }

        _hook = new LowLevelKeyboardHook(OnHookEvent);
        _hook.Start();
        _recording = true;
        _worker = Task.Run(WorkerLoopAsync);
        StateChanged?.Invoke(true);
    }

    public void Stop()
    {
        if (!_recording)
        {
            return;
        }

        _recording = false;
        _hook?.Dispose();
        _hook = null;
        try
        {
            _worker?.Wait(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            Log.Warn($"Recording worker did not stop in time: {ex.Message}");
        }

        StateChanged?.Invoke(false);
    }

    public void Toggle()
    {
        if (_recording)
        {
            Stop();
        }
        else
        {
            Start();
        }
    }

    public IReadOnlyList<LivePress> DrainRecentPresses()
    {
        lock (_pressLock)
        {
            if (_recentPresses.Count == 0)
            {
                return [];
            }

            var result = _recentPresses.ToArray();
            _recentPresses.Clear();
            return result;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _cts.Cancel();
        try
        {
            _worker?.Wait(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            Log.Warn($"Recording worker did not stop cleanly: {ex.Message}");
        }

        _cts.Dispose();
    }

    private void OnHookEvent(HookKeyEvent e)
    {
        if (_recording)
        {
            _ = _channel.Writer.TryWrite(e);
        }
    }

    private async Task WorkerLoopAsync()
    {
        var buffer = new List<StoredEvent>(FlushThreshold);
        var flushTimer = Stopwatch.StartNew();

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                bool readAny = false;
                while (_channel.Reader.TryRead(out HookKeyEvent e) && buffer.Count < FlushThreshold)
                {
                    AppendToBuffer(buffer, e);
                    readAny = true;
                }

                bool timeToFlush = readAny && flushTimer.ElapsedMilliseconds >= FlushIntervalMs;
                bool mustDrain = !_recording;
                if (buffer.Count >= FlushThreshold || timeToFlush || (mustDrain && buffer.Count > 0))
                {
                    Flush(buffer);
                    flushTimer.Restart();
                }

                if (mustDrain && buffer.Count == 0 && _channel.Reader.Count == 0)
                {
                    break;
                }

                await Task.Delay(20, _cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (Exception ex)
        {
            Log.Error("Recording worker failed.", ex);
        }
        finally
        {
            try
            {
                if (buffer.Count > 0)
                {
                    Flush(buffer);
                }
            }
            catch
            {
                // Nothing more we can do during shutdown.
            }
        }
    }

    private void AppendToBuffer(List<StoredEvent> buffer, HookKeyEvent e)
    {
        DateTime local = new DateTime(e.Ticks, DateTimeKind.Utc).ToLocalTime();
        buffer.Add(new StoredEvent(
            e.Ticks / TimeSpan.TicksPerMillisecond,
            DateMath.ToDayNumber(local),
            local.Hour * 60 + local.Minute,
            e.Vk,
            e.Kind,
            e.Injected));

        if (e.Kind == KeyEventKind.Down)
        {
            lock (_pressLock)
            {
                _recentPresses.Add(new LivePress(e.Vk, local));
                if (_recentPresses.Count > 200)
                {
                    _recentPresses.RemoveRange(0, _recentPresses.Count - 200);
                }
            }
        }
    }

    private void Flush(List<StoredEvent> buffer)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        try
        {
            _database.InsertBatch(buffer);
            buffer.Clear();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to flush events; buffered count will be retried.", ex);
        }
    }
}
