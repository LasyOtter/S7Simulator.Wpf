using Microsoft.Data.Sqlite;
using S7Simulator.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // 删除旧变量
        var del = conn.CreateCommand();
        del.CommandText = "DELETE FROM Variables WHERE DbNumber = $db";
        del.Parameters.AddWithValue("$db", db.DbNumber);
        del.ExecuteNonQuery();

        // 更新 DB 信息
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO DbInfo (DbNumber, Name, ImportedAt)
            VALUES ($num, $name, $time)
            """;
        cmd.Parameters.AddWithValue("$num", db.DbNumber);
        cmd.Parameters.AddWithValue("$name", db.Name);
        cmd.Parameters.AddWithValue("$time", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();

        // 插入新变量
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
        var list = new List<DbInfo>();
        using var conn = new SqliteConnection(ConnStr);
        conn.Open();

        var dbCmd = conn.CreateCommand();
        dbCmd.CommandText = "SELECT DbNumber, Name FROM DbInfo ORDER BY DbNumber";
        using var reader = dbCmd.ExecuteReader();
        while (reader.Read())
        {
            int dbNum = reader.GetInt32(0);
            string name = reader.GetString(1);
            var db = new DbInfo { DbNumber = dbNum, Name = name };

            var varCmd = conn.CreateCommand();
            varCmd.CommandText = "SELECT Name, DataType, ByteOffset, BitOffset, InitialValue, Comment FROM Variables WHERE DbNumber = $db";
            varCmd.Parameters.AddWithValue("$db", dbNum);
            using var vreader = varCmd.ExecuteReader();
            while (vreader.Read())
            {
                db.Variables.Add(new VariableInfo
                {
                    Name = vreader.GetString(0),
                    DataType = vreader.GetString(1),
                    ByteOffset = vreader.GetInt32(2),
                    BitOffset = vreader.IsDBNull(3) ? -1 : vreader.GetInt32(3),
                    InitialValue = vreader.IsDBNull(4) ? "" : vreader.GetString(4),
                    Comment = vreader.IsDBNull(5) ? "" : vreader.GetString(5)
                });
            }
            list.Add(db);
        }
        return list;
    }
}
