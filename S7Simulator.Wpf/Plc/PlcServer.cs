using System;
using Sharp7;
using Snap7;

namespace S7Simulator.Wpf.Plc;

public class PlcServer
{
    private readonly S7Server _server = new S7Server();
    private readonly PlcMemory _memory;

    public PlcServer(PlcMemory memory)
    {
        _memory = memory;

        // 注册所有内存区（Snap7 要求注册后才能被客户端读写）
        _server.RegisterArea(S7Server.srvAreaDB, 0, _memory.DBs.GetOrAdd(0, _ => new byte[65536]), 65536); // DB0 特殊处理
        _server.RegisterArea(S7Server.srvAreaMK, 0, _memory.MB, _memory.MB.Length);
        _server.RegisterArea(S7Server.srvAreaCT, 0, _memory.IB, _memory.IB.Length);
        _server.RegisterArea(S7Server.srvAreaTM, 0, _memory.QB, _memory.QB.Length);

        // 动态注册所有已加载的 DB（DB（重要！）
        foreach (var kv in _memory.DBs)
        {
            if (kv.Key == 0) continue;
            _server.RegisterArea(S7Server.srvAreaDB, kv.Key, kv.Value,kv.Value.Length);
        }

        // 事件：客户端读写时触发（可用于记录日志或触发事件）
        //_server.ReadEvent += (sender, e) => Console.WriteLine($"Read  Area={e.Area} DB={e.DbNumber} Start={e.Start} Size={e.Size}");
        //_server.WriteEvent += (sender, e) => Console.WriteLine($"Write Area={e.Area} DB={e.DbNumber} Start={e.Start} Size={e.Size}");
    }

    public void Start()
    {
        int result = _server.StartTo("0.0.0.0"); // 监听所有IP
        if (result == 0)
            Console.WriteLine("S7 Server 启动成功 @ port 102");
        else
            Console.WriteLine("S7 Server 启动失败: " + result);
    }

    public void Stop()
    {
        _server.Stop();
        Console.WriteLine("S7 Server 已停止");
    }
}

