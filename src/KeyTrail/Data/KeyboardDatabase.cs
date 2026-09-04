using System.Globalization;
using KeyTrail.Common;
using KeyTrail.Models;
using Microsoft.Data.Sqlite;

namespace KeyTrail.Data;

public sealed class KeyboardDatabase : IDisposable
{
    private const string InsertSql =
        """
        INSERT INTO key_events (ts, day, minute, vk, kind, injected)
        VALUES ($ts, $day, $minute, $vk, $kind, $injected);
        """;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private SqliteConnection? _connection;
    private bool _disposed;

    public void Open()
    {
        _gate.Wait();
        try
        {
            if (_connection is not null)
            {
                return;
            }

            AppPaths.EnsureCreated();
            SQLitePCL.Batteries_V2.Init();

            var connection = new SqliteConnection($"Data Source={AppPaths.DatabaseFile}");
            connection.Open();

            using (SqliteCommand pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
                pragma.ExecuteNonQuery();
            }

            using (SqliteCommand create = connection.CreateCommand())
            {
                create.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS key_events (
                        id INTEGER PRIMARY KEY,
                        ts INTEGER NOT NULL,
                        day INTEGER NOT NULL,
                        minute INTEGER NOT NULL,
                        vk INTEGER NOT NULL,
                        kind INTEGER NOT NULL,
                        injected INTEGER NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_key_events_day ON key_events(day, minute, vk);
                    CREATE INDEX IF NOT EXISTS ix_key_events_day_kind ON key_events(day, kind);
                    CREATE INDEX IF NOT EXISTS ix_key_events_ts ON key_events(ts);
                    CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                    """;
                create.ExecuteNonQuery();
            }

            try
            {
                using SqliteCommand check = connection.CreateCommand();
                check.CommandText = "PRAGMA quick_check;";
                string? result = check.ExecuteScalar()?.ToString();
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    Diagnostics.Log.Error($"Database quick_check failed: {result}");
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log.Warn($"quick_check unavailable: {ex.Message}");
            }

            _connection = connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InsertBatch(IReadOnlyList<StoredEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteTransaction tx = _connection!.BeginTransaction();
            using SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = InsertSql;
            cmd.Parameters.Add("$ts", SqliteType.Integer);
            cmd.Parameters.Add("$day", SqliteType.Integer);
            cmd.Parameters.Add("$minute", SqliteType.Integer);
            cmd.Parameters.Add("$vk", SqliteType.Integer);
            cmd.Parameters.Add("$kind", SqliteType.Integer);
            cmd.Parameters.Add("$injected", SqliteType.Integer);

            foreach (StoredEvent e in events)
            {
                cmd.Parameters["$ts"].Value = e.TsUtcMs;
                cmd.Parameters["$day"].Value = e.Day;
                cmd.Parameters["$minute"].Value = e.Minute;
                cmd.Parameters["$vk"].Value = e.Vk;
                cmd.Parameters["$kind"].Value = (int)e.Kind;
                cmd.Parameters["$injected"].Value = e.Injected ? 1 : 0;
                _ = cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Failed to insert key events.", ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public long CountPresses(int fromDay, int toDay)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM key_events WHERE day BETWEEN $from AND $to AND kind = 0;";
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<MinuteCount> GetMinuteCounts(int fromDay, int toDay)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT day, minute, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to AND kind = 0
                GROUP BY day, minute;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);

            var list = new List<MinuteCount>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MinuteCount(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt64(2)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<DayTotal> GetDayTotals(int fromDay, int toDay)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT day, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to AND kind = 0
                GROUP BY day
                ORDER BY day;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);

            var list = new List<DayTotal>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DayTotal(reader.GetInt32(0), reader.GetInt64(1)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<KeyCount> GetKeyCounts(int fromDay, int toDay)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT vk, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to AND kind = 0
                GROUP BY vk
                ORDER BY COUNT(*) DESC;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);

            var list = new List<KeyCount>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KeyCount(reader.GetInt32(0), reader.GetInt64(1)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<StoredEvent> GetEventsOrdered(int fromDay, int toDay)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT ts, day, minute, vk, kind, injected
                FROM key_events
                WHERE day BETWEEN $from AND $to AND kind IN (0, 1)
                ORDER BY ts;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);

            var list = new List<StoredEvent>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new StoredEvent(
                    reader.GetInt64(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    (KeyEventKind)reader.GetInt32(4),
                    reader.GetInt32(5) != 0));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public long CountPressesInMinutes(
        int fromDay,
        int toDay,
        int startMinute,
        int endMinute)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT COUNT(*) FROM key_events
                WHERE day BETWEEN $from AND $to
                  AND kind = 0
                  AND minute >= $start AND minute < $end;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);
            _ = cmd.Parameters.AddWithValue("$start", startMinute);
            _ = cmd.Parameters.AddWithValue("$end", endMinute);
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<KeyCount> GetKeyCountsInMinutes(
        int fromDay,
        int toDay,
        int startMinute,
        int endMinute)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT vk, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to
                  AND kind = 0
                  AND minute >= $start AND minute < $end
                GROUP BY vk
                ORDER BY COUNT(*) DESC;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);
            _ = cmd.Parameters.AddWithValue("$start", startMinute);
            _ = cmd.Parameters.AddWithValue("$end", endMinute);

            var list = new List<KeyCount>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KeyCount(reader.GetInt32(0), reader.GetInt64(1)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<DayTotal> GetDayTotalsInMinutes(
        int fromDay,
        int toDay,
        int startMinute,
        int endMinute)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT day, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to
                  AND kind = 0
                  AND minute >= $start AND minute < $end
                GROUP BY day
                ORDER BY day;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);
            _ = cmd.Parameters.AddWithValue("$start", startMinute);
            _ = cmd.Parameters.AddWithValue("$end", endMinute);

            var list = new List<DayTotal>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DayTotal(reader.GetInt32(0), reader.GetInt64(1)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<MinuteCount> GetMinuteCountsInMinutes(
        int fromDay,
        int toDay,
        int startMinute,
        int endMinute)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText =
                """
                SELECT day, minute, COUNT(*)
                FROM key_events
                WHERE day BETWEEN $from AND $to
                  AND kind = 0
                  AND minute >= $start AND minute < $end
                GROUP BY day, minute;
                """;
            _ = cmd.Parameters.AddWithValue("$from", fromDay);
            _ = cmd.Parameters.AddWithValue("$to", toDay);
            _ = cmd.Parameters.AddWithValue("$start", startMinute);
            _ = cmd.Parameters.AddWithValue("$end", endMinute);

            var list = new List<MinuteCount>();
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MinuteCount(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt64(2)));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void DeleteOlderThan(int day)
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText = "DELETE FROM key_events WHERE day < $day;";
            _ = cmd.Parameters.AddWithValue("$day", day);
            _ = cmd.ExecuteNonQuery();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ClearAll()
    {
        _gate.Wait();
        try
        {
            EnsureOpen();
            using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText = "DELETE FROM key_events;";
            _ = cmd.ExecuteNonQuery();
        }
        finally
        {
            _gate.Release();
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
            _gate.Wait(TimeSpan.FromSeconds(5));
            try
            {
                _connection?.Dispose();
                _connection = null;
            }
            finally
            {
                _gate.Release();
            }
        }
        catch
        {
            // Best-effort shutdown.
        }

        _gate.Dispose();
    }

    private void EnsureOpen()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("Database has not been opened.");
        }
    }
}
