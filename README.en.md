# KeyTrail

[中文](README.md) · [日本語](README.ja.md)

KeyTrail is a local, open-source, offline Windows application that turns your keyboard usage into privacy-first visual reports. It records only **key timings, key codes, and event types** — never text content, never screenshots, and never network traffic. All data stays on your machine.

## Features

- Background keyboard capture through the Windows low-level hook (`WH_KEYBOARD_LL`) across applications, without injecting into other processes.
- Counting rule: one physical "down → up" counts as one press; OS auto-repeat is excluded by default; injected events are tagged separately.
- Periods: day / week / month views using your local timezone; weeks start on Monday.
- Four time slots: late night / morning / noon / evening, with defaults of 0–6 / 6–12 / 12–18 / 18–24, editable in Settings.
- Habit insights: 24-hour activity curve, top keys, modifiers, keyboard shortcuts, key-interval statistics, typing bursts, short and long breaks.
- Visualizations: an isometric pseudo-3D keyboard heat map with ripple effects for live presses, 24h activity curve, daily trend bars, and four-slot share, all themed.
- System integration: start / pause / quit from the tray, optional start at login, minimize-to-tray on close.
- UI languages: Chinese / English / Japanese, switchable at runtime; beige and dark themes with smooth color transitions.
- Data management: SQLite in WAL mode, aggregate CSV export, retention cleanup, and one-click clear.

## Privacy boundary

- No text, IME candidates, or target-window information is recorded; no screenshots; no network. There is no network API in the codebase — audit it yourself.
- Raw detail contains key timestamps and key codes. In principle, a sequence could partially reveal what was typed (this is inherent to every tool of this kind). Pause recording from the tray before entering passwords or other sensitive text.
- A low-level keyboard hook looks like a keylogger to security software, so false positives are possible. Install only from trusted sources; this repository is fully open and can be audited and built by anyone.
- Permission boundary: when a foreground window runs at higher integrity (e.g., elevated/admin windows) or on the secure desktop (UAC prompts), Windows does not deliver keystrokes to a normal-privilege hook. This is a deliberate OS security behavior, not missing data we will "fix".

## Data and uninstall

- Data directory: `%LOCALAPPDATA%\KeyTrail\`
  - `keyboard.db` — main database (WAL mode);
  - `settings.json` — theme, language, autostart, time-slot boundaries, etc.;
  - `logs\app.log` — local diagnostics only.
- Use "Open data folder" and "Clear all statistics" from the Settings page; CSV export contains aggregate data only.
- Uninstall: the bundled uninstaller removes the program files and the autostart entry. The statistics database lives under the per-user data directory and is **kept by default** to avoid losing years of habit data. To fully remove it, delete `%LOCALAPPDATA%\KeyTrail` after uninstalling.

## Known limitations

- Elevated (admin-integrity) windows and the secure desktop are not counted, as explained above.
- The keyboard model is drawn as ANSI 104. JIS/ISO cap layouts are not mapped yet, but statistics themselves do not depend on keyboard layout.
- If the session is locked or no desktop input is available, there are naturally no keystrokes to record.

## Building

Requirements: Windows 10/11 x64 and the .NET SDK 10.

```powershell
# Debug build
dotnet build KeyTrail.slnx

# Self-contained single-file publish
.\eng\publish.ps1
```

For offline reproduction, run `.\eng\restore-offline.ps1` once to cache NuGet packages under `.packages\`, then use `eng\publish-offline.ps1`.

## Testing

- `dotnet run --project tests\SmokeProbe` reads a given database to help verify hook-to-disk counting.
- A suggested long-run acceptance procedure is in `docs\TESTING.md`.

## Stack and architecture

- C# / .NET 10 LTS + WPF;
- Microsoft.Data.Sqlite with SQLite WAL;
- Layered as `InputHookService` → bounded channel → batched database writes; `StatisticsService` serves queries to the view layer;
- Themes and localization use runtime-swappable `ResourceDictionary`; charts and the keyboard view are custom-drawn controls with event-driven animation.

## License

MIT — see [LICENSE](LICENSE).

