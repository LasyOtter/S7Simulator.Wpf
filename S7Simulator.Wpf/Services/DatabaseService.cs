using Microsoft.Data.Sqlite;
using S7Simulator.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace S7Simulator.Wpf.Services;

public class DatabaseService
{
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DatabaseService));
    private static readonly string DbPath = "simulator.db";
    private static readonly string ConnStr = $"Data Source={DbPath}";

    // Simple Singleton for now, can be replaced by DI later
    public static DatabaseService Instance { get; } = new DatabaseService();

    private DatabaseService() { }

    public async Task InitializeAsync()
    {
        using var conn = new SqliteConnection(ConnStr);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS DbInfo (
                DbNumber INTEGER PRIMARY KEY,
                Name TEXT,
                ImportedAt TEXT
            );
            CREATE TABLE IF NOT EXISTS Variables (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DbNumber INTEGER,
                Name TEXT,
                DataType TEXT,
                ByteOffset INTEGER,
                BitOffset INTEGER,
                InitialValue TEXT,
                Comment TEXT
            );
            """;
        await cmd.ExecuteNonQueryAsync();
        _log.Info("Database initialized successfully");
    }

    public async Task SaveDbAsync(DbInfo db)
    {
        using var conn = new SqliteConnection(ConnStr);
        await conn.OpenAsync();
        using var tran = (SqliteTransaction)await conn.BeginTransactionAsync();

        var del = conn.CreateCommand();
        del.Transaction = tran;
        del.CommandText = "DELETE FROM Variables WHERE DbNumber = $db";
        del.Parameters.AddWithValue("$db", db.DbNumber);
        await del.ExecuteNonQueryAsync();

        var cmd = conn.CreateCommand();
        cmd.Transaction = tran;
        cmd.CommandText = """
            INSERT OR REPLACE INTO DbInfo (DbNumber, Name, ImportedAt)
            VALUES ($num, $name, $time)
            """;

        var importedAt = DateTime.Now;
        db.ImportedAt = importedAt;
        cmd.Parameters.AddWithValue("$num", db.DbNumber);
        cmd.Parameters.AddWithValue("$name", db.Name);
        cmd.Parameters.AddWithValue("$time", importedAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync();

        var vcmd = conn.CreateCommand();
        vcmd.Transaction = tran;
        vcmd.CommandText = """
            INSERT INTO Variables 
            (DbNumber, Name, DataType, ByteOffset, BitOffset, InitialValue, Comment)
            VALUES ($db, $name, $type, $off, $bit, $init, $cmt)
            """;
        
        // Create parameters once
        var pDb = vcmd.Parameters.Add("$db", SqliteType.Integer);
        var pName = vcmd.Parameters.Add("$name", SqliteType.Text);
        var pType = vcmd.Parameters.Add("$type", SqliteType.Text);
        var pOff = vcmd.Parameters.Add("$off", SqliteType.Integer);
        var pBit = vcmd.Parameters.Add("$bit", SqliteType.Integer);
        var pInit = vcmd.Parameters.Add("$init", SqliteType.Text);
        var pCmt = vcmd.Parameters.Add("$cmt", SqliteType.Text);

        foreach (var v in db.Variables)
        {
            pDb.Value = db.DbNumber;
            pName.Value = v.Name;
            pType.Value = v.DataType;
            pOff.Value = v.ByteOffset;
            pBit.Value = v.BitOffset;
            pInit.Value = v.InitialValue ?? (object)DBNull.Value;
            pCmt.Value = v.Comment ?? (object)DBNull.Value;
            
            await vcmd.ExecuteNonQueryAsync();
        }

        await tran.CommitAsync();
        _log.Info($"Saved DB{db.DbNumber} with {db.Variables.Count} variables");
    }

    public async Task<List<DbInfo>> LoadAllDbsAsync()
    {
        using var conn = new SqliteConnection(ConnStr);
        await conn.OpenAsync();

        var result = new Dictionary<int, DbInfo>();

        var dbCmd = conn.CreateCommand();
        dbCmd.CommandText = "SELECT DbNumber, Name, ImportedAt FROM DbInfo ORDER BY DbNumber";
        using (var reader = await dbCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var dbNumber = reader.GetInt32(0);
                var db = new DbInfo
                {
                    DbNumber = dbNumber,
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    ImportedAt = ParseImportedAt(reader.IsDBNull(2) ? null : reader.GetString(2))
                };

                result[dbNumber] = db;
            }
        }

        if (result.Count == 0)
        {
            return new List<DbInfo>();
        }

        var varCmd = conn.CreateCommand();
        varCmd.CommandText = """
            SELECT DbNumber, Name, DataType, ByteOffset, BitOffset, InitialValue, Comment
            FROM Variables
            ORDER BY DbNumber, ByteOffset, BitOffset
            """;

        using (var reader = await varCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var dbNumber = reader.GetInt32(0);
                if (!result.TryGetValue(dbNumber, out var db))
                {
                    continue;
                }

                db.Variables.Add(new VariableInfo
                {
                    DbNumber = dbNumber,
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    DataType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    ByteOffset = reader.GetInt32(3),
                    BitOffset = reader.IsDBNull(4) ? -1 : reader.GetInt32(4),
                    InitialValue = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Comment = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                });
            }
        }

        return result.Values.OrderBy(db => db.DbNumber).ToList();
    }

    private static DateTime ParseImportedAt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.MinValue;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTime.MinValue;
    }
}
