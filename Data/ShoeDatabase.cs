using Microsoft.Data.Sqlite;
using Sportswear.Models;

namespace Sportswear.Data;

public sealed class ShoeDatabase
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Shoes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Brand TEXT,
            Model TEXT,
            Color TEXT,
            PurchaseDate TEXT,
            InitialKm REAL NOT NULL DEFAULT 0,
            RetireKm REAL NOT NULL DEFAULT 800,
            Notes TEXT,
            IsRetired INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS RunLogs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ShoeId INTEGER NOT NULL,
            RunDate TEXT NOT NULL,
            DistanceKm REAL NOT NULL,
            Notes TEXT,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (ShoeId) REFERENCES Shoes(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_RunLogs_ShoeId_RunDate ON RunLogs(ShoeId, RunDate);
        """;

    public ShoeDatabase()
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (!File.Exists(DatabasePath) && File.Exists(LegacyDatabasePath))
            File.Copy(LegacyDatabasePath, DatabasePath);

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
        EnsureColumn(connection, "RunLogs", "Weather", "TEXT");
        EnsureColumn(connection, "RunLogs", "Temperature", "TEXT");
        EnsureColumn(connection, "RunLogs", "StartTime", "TEXT");
    }

    public string DatabasePath { get; } = Path.Combine(
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory,
        "sportswear.db");

    private static readonly string LegacyDatabasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sportswear",
        "sportswear.db");

    public IReadOnlyList<Shoe> GetShoes()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.Id, s.Name, s.Brand, s.Model, s.Color, s.PurchaseDate,
                s.InitialKm, s.RetireKm, s.Notes, s.IsRetired,
                IFNULL(SUM(r.DistanceKm), 0) AS LoggedKm,
                COUNT(r.Id) AS RunCount
            FROM Shoes s
            LEFT JOIN RunLogs r ON r.ShoeId = s.Id
            GROUP BY s.Id
            """;

        var shoes = new List<Shoe>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            shoes.Add(new Shoe
            {
                Id = reader.GetInt64(0),
                Name = ReadString(reader, 1),
                Brand = ReadString(reader, 2),
                Model = ReadString(reader, 3),
                Color = ReadString(reader, 4),
                PurchaseDate = ReadDate(reader, 5),
                InitialKm = reader.GetDouble(6),
                RetireKm = reader.GetDouble(7),
                Notes = ReadString(reader, 8),
                IsRetired = reader.GetInt64(9) != 0,
                LoggedKm = reader.GetDouble(10),
                RunCount = (int)reader.GetInt64(11)
            });
        }

        shoes.Sort(CompareShoes);
        return shoes;
    }

    public IReadOnlyList<RunEntry> GetRuns(long shoeId) => QueryRuns(shoeId);

    public IReadOnlyList<RunEntry> GetAllRuns() => QueryRuns(null);

    public double GetKmSince(DateTime startDate)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IFNULL(SUM(DistanceKm), 0)
            FROM RunLogs
            WHERE RunDate >= $start
            """;
        command.Parameters.AddWithValue("$start", Dates.ToStorage(startDate));
        return Convert.ToDouble(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public long AddShoe(Shoe shoe)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Shoes
                (Name, Brand, Model, Color, PurchaseDate, InitialKm, RetireKm, Notes, IsRetired, CreatedAt)
            VALUES
                ($name, $brand, $model, $color, $purchase, $initial, $retire, $notes, $retired, $created)
            """;
        BindShoe(command, shoe);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return LastId(connection);
    }

    public void UpdateShoe(Shoe shoe)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Shoes SET
                Name = $name,
                Brand = $brand,
                Model = $model,
                Color = $color,
                PurchaseDate = $purchase,
                InitialKm = $initial,
                RetireKm = $retire,
                Notes = $notes,
                IsRetired = $retired
            WHERE Id = $id
            """;
        BindShoe(command, shoe);
        command.Parameters.AddWithValue("$id", shoe.Id);
        command.ExecuteNonQuery();
    }

    public void SetRetired(long id, bool retired)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Shoes SET IsRetired = $retired WHERE Id = $id";
        command.Parameters.AddWithValue("$retired", retired ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void DeleteShoe(long id)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM RunLogs WHERE ShoeId = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();

        command.CommandText = "DELETE FROM Shoes WHERE Id = $id";
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public long AddRun(RunEntry run)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO RunLogs (ShoeId, RunDate, DistanceKm, Notes, Weather, Temperature, StartTime, CreatedAt)
            VALUES ($shoeId, $date, $distance, $notes, $weather, $temperature, $startTime, $created)
            """;
        BindRun(command, run);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return LastId(connection);
    }

    public void UpdateRun(RunEntry run)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE RunLogs SET
                ShoeId = $shoeId,
                RunDate = $date,
                DistanceKm = $distance,
                Notes = $notes,
                Weather = $weather,
                Temperature = $temperature,
                StartTime = $startTime
            WHERE Id = $id
            """;
        BindRun(command, run);
        command.Parameters.AddWithValue("$id", run.Id);
        command.ExecuteNonQuery();
    }

    public void DeleteRun(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM RunLogs WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private static void BindShoe(SqliteCommand command, Shoe shoe)
    {
        command.Parameters.AddWithValue("$name", shoe.Name.Trim());
        command.Parameters.AddWithValue("$brand", DbText(shoe.Brand));
        command.Parameters.AddWithValue("$model", DbText(shoe.Model));
        command.Parameters.AddWithValue("$color", DbText(shoe.Color));
        command.Parameters.AddWithValue("$purchase", shoe.PurchaseDate is DateTime date ? Dates.ToStorage(date) : DBNull.Value);
        command.Parameters.AddWithValue("$initial", shoe.InitialKm);
        command.Parameters.AddWithValue("$retire", shoe.RetireKm);
        command.Parameters.AddWithValue("$notes", DbText(shoe.Notes));
        command.Parameters.AddWithValue("$retired", shoe.IsRetired ? 1 : 0);
    }

    private static void BindRun(SqliteCommand command, RunEntry run)
    {
        command.Parameters.AddWithValue("$shoeId", run.ShoeId);
        command.Parameters.AddWithValue("$date", Dates.ToStorage(run.RunDate));
        command.Parameters.AddWithValue("$distance", run.DistanceKm);
        command.Parameters.AddWithValue("$notes", DbText(run.Notes));
        command.Parameters.AddWithValue("$weather", DbText(run.Weather));
        command.Parameters.AddWithValue("$temperature", DbText(run.Temperature));
        command.Parameters.AddWithValue("$startTime", DbText(run.StartTime));
    }

    private IReadOnlyList<RunEntry> QueryRuns(long? shoeId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        var sql = """
            SELECT r.Id, r.ShoeId, r.RunDate, r.DistanceKm, r.Notes,
                   r.Weather, r.Temperature, r.StartTime, s.Name
            FROM RunLogs r
            JOIN Shoes s ON s.Id = r.ShoeId
            """;
        if (shoeId is long id)
        {
            sql += " WHERE r.ShoeId = $shoeId";
            command.Parameters.AddWithValue("$shoeId", id);
        }

        sql += """
             ORDER BY r.RunDate DESC,
                      CASE WHEN r.StartTime IS NULL OR r.StartTime = '' THEN 1 ELSE 0 END,
                      r.StartTime DESC,
                      r.Id DESC
            """;
        command.CommandText = sql;

        var runs = new List<RunEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            runs.Add(new RunEntry
            {
                Id = reader.GetInt64(0),
                ShoeId = reader.GetInt64(1),
                RunDate = ReadDate(reader, 2) ?? DateTime.Today,
                DistanceKm = reader.GetDouble(3),
                Notes = ReadString(reader, 4),
                Weather = ReadString(reader, 5),
                Temperature = ReadString(reader, 6),
                StartTime = ReadString(reader, 7),
                ShoeName = ReadString(reader, 8)
            });
        }

        return runs;
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string type)
    {
        var exists = false;
        using (var info = connection.CreateCommand())
        {
            info.CommandText = $"PRAGMA table_info({table})";
            using var reader = info.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
            return;

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
        alter.ExecuteNonQuery();
    }

    private static long LastId(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT last_insert_rowid()";
        return (long)command.ExecuteScalar()!;
    }

    private static object DbText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string ReadString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);

    private static DateTime? ReadDate(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        return Dates.TryParse(reader.GetString(ordinal), out var date) ? date : null;
    }

    private static int CompareShoes(Shoe left, Shoe right)
    {
        var retired = left.IsRetired.CompareTo(right.IsRetired);
        if (retired != 0)
            return retired;

        var progress = right.ProgressRatio.CompareTo(left.ProgressRatio);
        if (progress != 0)
            return progress;

        return string.Compare(left.Name, right.Name, StringComparison.CurrentCulture);
    }
}
