using KeyTrail.Common;

namespace KeyTrail.Models;

public readonly record struct StoredEvent(
    long TsUtcMs,
    int Day,
    int Minute,
    int Vk,
    KeyEventKind Kind,
    bool Injected);

