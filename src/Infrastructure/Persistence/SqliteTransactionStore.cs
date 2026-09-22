using System.Text.Json;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Domain.ValueObjects;
using Microsoft.Data.Sqlite;

namespace FraudMonitor.Infrastructure.Persistence;

/// <summary>
/// Persistent SQLite Store simulating DynamoDB across independent OS processes (Worker and BackofficeApi).
/// Enables real-time synchronization between the stream processor worker and the backoffice REST API.
/// </summary>
public class SqliteTransactionStore : ITransactionStore
{
    private readonly string _connectionString;
    private readonly object _initLock = new();
    private bool _isInitialized;

    public SqliteTransactionStore(string? connectionString = null)
    {
        var dbPath = connectionString ?? ResolveDatabasePath();
        _connectionString = dbPath.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            ? dbPath
            : $"Data Source={dbPath};Cache=Shared;";

        EnsureDatabaseInitialized();
    }

    public static string ResolveDatabasePath()
    {
        var customPath = Environment.GetEnvironmentVariable("FRAUDMONITOR_DB_PATH");
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FraudMonitor.sln")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(root, "fraudmonitor.db");
    }

    private void EnsureDatabaseInitialized()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;

                CREATE TABLE IF NOT EXISTS Transactions (
                    TransactionId TEXT PRIMARY KEY,
                    ClientId TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Timestamp TEXT NOT NULL,
                    DeviceId TEXT,
                    Latitude REAL,
                    Longitude REAL,
                    City TEXT,
                    State TEXT,
                    Country TEXT,
                    Status TEXT NOT NULL,
                    RiskLevel TEXT NOT NULL,
                    TriggeredRules TEXT,
                    RiskReason TEXT,
                    ProcessedAt TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_Transactions_ClientId_Timestamp ON Transactions (ClientId, Timestamp DESC);
                CREATE INDEX IF NOT EXISTS IX_Transactions_Timestamp ON Transactions (Timestamp DESC);
            ";
            cmd.ExecuteNonQuery();
            _isInitialized = true;
        }
    }

    public async Task SaveTransactionAsync(Transaction transaction)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Transactions (
                TransactionId, ClientId, Amount, Timestamp, DeviceId,
                Latitude, Longitude, City, State, Country,
                Status, RiskLevel, TriggeredRules, RiskReason, ProcessedAt
            ) VALUES (
                @TransactionId, @ClientId, @Amount, @Timestamp, @DeviceId,
                @Latitude, @Longitude, @City, @State, @Country,
                @Status, @RiskLevel, @TriggeredRules, @RiskReason, @ProcessedAt
            )
            ON CONFLICT(TransactionId) DO UPDATE SET
                Status = excluded.Status,
                RiskLevel = excluded.RiskLevel,
                TriggeredRules = excluded.TriggeredRules,
                RiskReason = excluded.RiskReason,
                ProcessedAt = excluded.ProcessedAt;
        ";

        cmd.Parameters.AddWithValue("@TransactionId", transaction.TransactionId);
        cmd.Parameters.AddWithValue("@ClientId", transaction.ClientId);
        cmd.Parameters.AddWithValue("@Amount", (double)transaction.Amount);
        cmd.Parameters.AddWithValue("@Timestamp", transaction.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@DeviceId", (object?)transaction.DeviceId ?? DBNull.Value);

        if (transaction.Location != null)
        {
            cmd.Parameters.AddWithValue("@Latitude", transaction.Location.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", transaction.Location.Longitude);
            cmd.Parameters.AddWithValue("@City", (object?)transaction.Location.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@State", (object?)transaction.Location.State ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", (object?)transaction.Location.Country ?? DBNull.Value);
        }
        else
        {
            cmd.Parameters.AddWithValue("@Latitude", DBNull.Value);
            cmd.Parameters.AddWithValue("@Longitude", DBNull.Value);
            cmd.Parameters.AddWithValue("@City", DBNull.Value);
            cmd.Parameters.AddWithValue("@State", DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", DBNull.Value);
        }

        cmd.Parameters.AddWithValue("@Status", transaction.Status.ToString());
        cmd.Parameters.AddWithValue("@RiskLevel", transaction.RiskLevel.ToString());
        cmd.Parameters.AddWithValue("@TriggeredRules", JsonSerializer.Serialize(transaction.TriggeredRules));
        cmd.Parameters.AddWithValue("@RiskReason", (object?)transaction.RiskReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProcessedAt", transaction.ProcessedAt.HasValue ? transaction.ProcessedAt.Value.ToString("O") : DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync()
    {
        var list = new List<Transaction>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions ORDER BY Timestamp DESC";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapReader(reader));
        }

        return list;
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string transactionId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions WHERE TransactionId = @id LIMIT 1";
        cmd.Parameters.AddWithValue("@id", transactionId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReader(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Transaction>> GetRecentTransactionsByClientAsync(string clientId, TimeSpan window)
    {
        var list = new List<Transaction>();
        var cutoff = DateTime.UtcNow.Subtract(window).ToString("O");

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions WHERE ClientId = @clientId AND Timestamp >= @cutoff ORDER BY Timestamp DESC";
        cmd.Parameters.AddWithValue("@clientId", clientId);
        cmd.Parameters.AddWithValue("@cutoff", cutoff);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapReader(reader));
        }

        return list;
    }

    public async Task<Transaction?> GetLastTransactionByClientAsync(string clientId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions WHERE ClientId = @clientId ORDER BY Timestamp DESC LIMIT 1";
        cmd.Parameters.AddWithValue("@clientId", clientId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReader(reader);
        }

        return null;
    }

    private static Transaction MapReader(SqliteDataReader reader)
    {
        var transaction = new Transaction
        {
            TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
            ClientId = reader.GetString(reader.GetOrdinal("ClientId")),
            Amount = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("Amount"))),
            Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? string.Empty : reader.GetString(reader.GetOrdinal("DeviceId")),
            Status = Enum.TryParse<TransactionStatus>(reader.GetString(reader.GetOrdinal("Status")), out var st) ? st : TransactionStatus.Pending,
            RiskLevel = Enum.TryParse<RiskLevel>(reader.GetString(reader.GetOrdinal("RiskLevel")), out var rk) ? rk : RiskLevel.None,
            RiskReason = reader.IsDBNull(reader.GetOrdinal("RiskReason")) ? null : reader.GetString(reader.GetOrdinal("RiskReason")),
            ProcessedAt = reader.IsDBNull(reader.GetOrdinal("ProcessedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("ProcessedAt")), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };

        var latOrdinal = reader.GetOrdinal("Latitude");
        var lonOrdinal = reader.GetOrdinal("Longitude");
        if (!reader.IsDBNull(latOrdinal) && !reader.IsDBNull(lonOrdinal))
        {
            transaction.Location = new Location(
                reader.GetDouble(latOrdinal),
                reader.GetDouble(lonOrdinal),
                reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
                reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader.GetString(reader.GetOrdinal("State")),
                reader.IsDBNull(reader.GetOrdinal("Country")) ? string.Empty : reader.GetString(reader.GetOrdinal("Country"))
            );
        }

        var rulesOrdinal = reader.GetOrdinal("TriggeredRules");
        var rulesJson = reader.IsDBNull(rulesOrdinal) ? null : reader.GetString(rulesOrdinal);
        if (!string.IsNullOrWhiteSpace(rulesJson))
        {
            try
            {
                transaction.TriggeredRules = JsonSerializer.Deserialize<List<string>>(rulesJson) ?? new();
            }
            catch
            {
                transaction.TriggeredRules = new();
            }
        }

        return transaction;
    }
}
