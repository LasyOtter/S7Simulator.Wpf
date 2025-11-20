using S7Simulator.Wpf.Models;

namespace S7Simulator.Wpf.Plc;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class PlcMemory
{
    // 每个 DB 最大 64KB，实际 S7-1200 DB 最大 64KB
    public ConcurrentDictionary<int, byte[]> DBs = new();
    public byte[] MB = new byte[32768];   // M 区
    public byte[] IB = new byte[8192];    // 输入
    public byte[] QB = new byte[8192];    // 输出

    // 可选：保存符号表方便调试
    public class VariableInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int ByteOffset { get; set; }
        public int BitOffset { get; set; } = -1;
        public string Comment { get; set; }
    }

    public Dictionary<int, List<VariableInfo>> SymbolTable = new();

    // ==================== 读写封装 ====================
    public bool ReadBool(int dbNumber, int byteOffset, int bitOffset)
    {
        byte[] area = GetArea(dbNumber);
        return (area[byteOffset] & (1 << bitOffset)) != 0;
    }

    public void WriteBool(int dbNumber, int byteOffset, int bitOffset, bool value)
    {
        byte[] area = GetArea(dbNumber);
        if (value)
            area[byteOffset] |= (byte)(1 << bitOffset);
        else
            area[byteOffset] &= (byte)~(1 << bitOffset);
    }

    public byte ReadByte(int dbNumber, int byteOffset) => GetArea(dbNumber)[byteOffset];
    public void WriteByte(int dbNumber, int byteOffset, byte value) => GetArea(dbNumber)[byteOffset] = value;

    public short ReadInt(int dbNumber, int byteOffset) => BitConverter.ToInt16(GetArea(dbNumber), byteOffset);
    public void WriteInt(int dbNumber, int byteOffset, short value)
        => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, GetArea(dbNumber), byteOffset, 2);

    public int ReadDInt(int dbNumber, int byteOffset) => BitConverter.ToInt32(GetArea(dbNumber), byteOffset);
    public void WriteDInt(int dbNumber, int byteOffset, int value)
        => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, GetArea(dbNumber), byteOffset, 4);

    public float ReadReal(int dbNumber, int byteOffset) => BitConverter.ToSingle(GetArea(dbNumber), byteOffset);
    public void WriteReal(int dbNumber, int byteOffset, float value)
        => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, GetArea(dbNumber), byteOffset, 4);

    public string ReadString(int dbNumber, int byteOffset, int maxLen)
    {
        var data = GetArea(dbNumber);
        byte actualLen = data[byteOffset + 1];
        int len = Math.Min(actualLen, maxLen);
        return System.Text.Encoding.ASCII.GetString(data, byteOffset + 2, len);
    }

    private byte[] GetArea(int dbNumber)
    {
        if (dbNumber == 0) return MB; // 特殊：DB0 不存在，用 M 区代替
        return DBs.GetOrAdd(dbNumber, _ => new byte[65536]);
    }
    
    public void ApplyDbStructure(DbInfo db)
    {
        var raw = DBs.GetOrAdd(db.DbNumber, _ => new byte[65536]);

        foreach (var v in db.Variables)
        {
            try
            {
                switch (v.DataType)
                {
                    case "BOOL":
                        WriteBool(db.DbNumber, v.ByteOffset, v.BitOffset, bool.Parse(v.InitialValue));
                        break;
                    case "BYTE":
                        WriteByte(db.DbNumber, v.ByteOffset, byte.Parse(v.InitialValue));
                        break;
                    case "INT":
                        WriteInt(db.DbNumber, v.ByteOffset, short.Parse(v.InitialValue));
                        break;
                    case "DINT":
                        WriteDInt(db.DbNumber, v.ByteOffset, int.Parse(v.InitialValue));
                        break;
                    case "REAL":
                        WriteReal(db.DbNumber, v.ByteOffset, float.Parse(v.InitialValue.Replace('.', ',')));
                        break;
                    case "STRING":
                        WriteString(db.DbNumber, v.ByteOffset, 254, v.InitialValue);
                        break;
                }
            }
            catch { /* 忽略解析失败的初始值 */ }
        }
    }

    public void WriteString(int dbNumber, int byteOffset, int maxLen, string value)
    {
        var data = GetArea(dbNumber);
        int len = Math.Min(value.Length, maxLen);
        data[byteOffset] = (byte)maxLen;
        data[byteOffset + 1] = (byte)len;
        System.Text.Encoding.ASCII.GetBytes(value, 0, len, data, byteOffset + 2);
    }
}