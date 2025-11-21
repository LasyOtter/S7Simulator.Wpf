using Microsoft.Data.Sqlite;
using S7Simulator.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace S7Simulator.Wpf.Services;

public static class DatabaseService
{
    private static readonly string DbPath = "simulator.db";
    private static readonly string ConnStr = $"Data Source={DbPath}";

    public static void Initialize()
    {
        using var conn = new SqliteConnection(ConnStr);
        conn.Open();
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
        cmd.ExecuteNonQuery();
    }

    public static void SaveDb(DbInfo db)
    {
        using var conn = new SqliteConnection(ConnStr);
        conn.Open();
        using var tran = conn.BeginTransaction();

        var del = conn.CreateCommand();
        del.CommandText = "DELETE FROM Variables WHERE DbNumber = $db";
        del.Parameters.AddWithValue("$db", db.DbNumber);
        del.ExecuteNonQuery();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO DbInfo (DbNumber, Name, ImportedAt)
            VALUES ($num, $name, $time)
            """;

        var importedAt = DateTime.Now;
        db.ImportedAt = importedAt;
        cmd.Parameters.AddWithValue("$num", db.DbNumber);
        cmd.Parameters.AddWithValue("$name", db.Name);
        cmd.Parameters.AddWithValue("$time", importedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        foreach (var v in db.Variables)
        {
            var vcmd = conn.CreateCommand();
            vcmd.CommandText = """
                INSERT INTO Variables 
                (DbNumber, Name, DataType, ByteOffset, BitOffset, InitialValue, Comment)
                VALUES ($db, $name, $type, $off, $bit, $init, $cmt)
                """;
            vcmd.Parameters.AddWithValue("$db", db.DbNumber);
            vcmd.Parameters.AddWithValue("$name", v.Name);
            vcmd.Parameters.AddWithValue("$type", v.DataType);
            vcmd.Parameters.AddWithValue("$off", v.ByteOffset);
            vcmd.Parameters.AddWithValue("$bit", v.BitOffset);
            vcmd.Parameters.AddWithValue("$init", v.InitialValue ?? (object)DBNull.Value);
            vcmd.Parameters.AddWithValue("$cmt", v.Comment ?? (object)DBNull.Value);
            vcmd.ExecuteNonQuery();
        }

        tran.Commit();
    }

    public static List<DbInfo> LoadAllDbs()
    {
        using var conn = new SqliteConnection(ConnStr);
        conn.Open();

        var result = new Dictionary<int, DbInfo>();

        var dbCmd = conn.CreateCommand();
        dbCmd.CommandText = "SELECT DbNumber, Name, ImportedAt FROM DbInfo ORDER BY DbNumber";
        using (var reader = dbCmd.ExecuteReader())
        {
            while (reader.Read())
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

        using (var reader = varCmd.ExecuteReader())
        {
            while (reader.Read())
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
