using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;

namespace TicTacToeFancy.Services;

public record PlayerStat(string Name, int Wins, int Losses, int Draws, DateTime LastPlayed);

public class DatabaseService
{

    private string _connectionString = string.Empty;

    public DatabaseService()
    {

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            // 1. Pfad zu %AppData% (Local) ermitteln
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appDataPath, "TictacToefancy");

            // 2. Ordner erstellen, falls er nicht existiert
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string dbPath = Path.Combine(folderPath, "game_stats.db");
            _connectionString = $"Data Source={dbPath}";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Leaderboard (
                    Name TEXT PRIMARY KEY,
                    Wins INTEGER DEFAULT 0,
                    Losses INTEGER DEFAULT 0,
                    Draws INTEGER DEFAULT 0,
                    LastPlayed TEXT
                )";
            tableCmd.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"[DatabaseService] Init error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates stats asynchronously to avoid blocking the UI thread.
    /// Uses UPSERT (INSERT OR IGNORE + UPDATE) for better performance.
    /// </summary>
    public async Task UpdateStatsAsync(string name, string result) // result: "WIN", "LOSS", "DRAW"
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // UPSERT: Insert if not exists, then update
            await using var upsertCmd = connection.CreateCommand();
            upsertCmd.CommandText = @"
                    INSERT INTO Leaderboard (Name, Wins, Losses, Draws, LastPlayed)
                    VALUES ($name, 0, 0, 0, $date)
                    ON CONFLICT(Name) DO NOTHING";
            upsertCmd.Parameters.AddWithValue("$name", name);
            upsertCmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
            await upsertCmd.ExecuteNonQueryAsync();

            string column = result switch
            {
                "WIN" => "Wins",
                "LOSS" => "Losses",
                "DRAW" => "Draws",
                _ => throw new ArgumentException($"Invalid result: {result}")
            };

            await using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = $"UPDATE Leaderboard SET {column} = {column} + 1, LastPlayed = $date WHERE Name = $name";
            updateCmd.Parameters.AddWithValue("$name", name);
            updateCmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
            await updateCmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"[DatabaseService] UpdateStats error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronous version for backwards compatibility.
    /// </summary>
    public void UpdateStats(string name, string result)
    {
        // Fire-and-forget async call (safe for game end scenarios)
        _ = UpdateStatsAsync(name, result);
    }

    /// <summary>
    /// Fetches leaderboard asynchronously.
    /// </summary>
    public async Task<List<PlayerStat>> GetLeaderboardAsync()
    {
        var stats = new List<PlayerStat>();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Name, Wins, Losses, Draws, LastPlayed FROM Leaderboard ORDER BY Wins DESC, Losses ASC";

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add(new PlayerStat(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.MinValue
                ));
            }
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"[DatabaseService] GetLeaderboard error: {ex.Message}");
        }

        return stats;
    }

}
