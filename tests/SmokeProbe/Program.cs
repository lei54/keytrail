using Microsoft.Data.Sqlite;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: SmokeProbe <database-file>");
    return 1;
}

SQLitePCL.Batteries_V2.Init();
using var connection = new SqliteConnection($"Data Source={Path.GetFullPath(args[0])}");
connection.Open();

using (SqliteCommand cmd = connection.CreateCommand())
{
    cmd.CommandText =
        """
        SELECT kind, COUNT(*) FROM key_events
        WHERE vk = 124
        GROUP BY kind ORDER BY kind;
        """;
    using SqliteDataReader reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"kind={reader.GetInt32(0)} count={reader.GetInt64(1)}");
    }
}

using (SqliteCommand cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM key_events;";
    Console.WriteLine($"total_events={cmd.ExecuteScalar()}");
}

return 0;

