using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Snap7;

namespace S7Simulator.Wpf.Plc;

public class PlcServer : IDisposable
{
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(PlcServer));
    private readonly S7Server _server = new S7Server();
    private readonly PlcMemory _memory;
    private readonly List<GCHandle> _gcHandles = new();
    private bool _disposed;
    
    // Client monitoring
    private readonly ConcurrentDictionary<string, DateTime> _clientLastActivity = new();
    private readonly Timer _eventPollingTimer;
    private readonly Timer _idleCheckTimer;
    private const int IdleTimeoutMinutes = 5;
    private const int EventPollingIntervalMs = 100; // Poll events every 100ms
    private const int IdleCheckIntervalMs = 30000; // Check for idle clients every 30 seconds

    public PlcServer(PlcMemory memory)
    {
        _memory = memory;

        // 注册所有内存区（Snap7 要求注册后才能被客户端读写）
        RegisterArea(S7Server.srvAreaDB, 0, _memory.DBs.GetOrAdd(0, _ => new byte[65536])); // DB0 特殊处理
        RegisterArea(S7Server.srvAreaMK, 0, _memory.MB);
        RegisterArea(S7Server.srvAreaCT, 0, _memory.IB);
        RegisterArea(S7Server.srvAreaTM, 0, _memory.QB);

        // 动态注册所有已加载的 DB
        foreach (var kv in _memory.DBs)
        {
            if (kv.Key == 0) continue;
            RegisterArea(S7Server.srvAreaDB, kv.Key, kv.Value);
        }
        
        // Initialize timers (but don't start them yet)
        _eventPollingTimer = new Timer(PollServerEvents, null, Timeout.Infinite, Timeout.Infinite);
        _idleCheckTimer = new Timer(CheckIdleClients, null, Timeout.Infinite, Timeout.Infinite);
        
        _log.Info("PlcServer initialized with memory areas");
    }

    public void RegisterDb(int dbNumber)
    {
        if (dbNumber <= 0) return;
        if (_memory.DBs.TryGetValue(dbNumber, out var buffer))
        {
            RegisterArea(S7Server.srvAreaDB, dbNumber, buffer);
            _log.Info($"Registered DB{dbNumber} with {buffer.Length} bytes");
        }
    }

    private void RegisterArea(int areaCode, int index, byte[] buffer)
    {
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        _gcHandles.Add(handle);
        
        // Use IntPtr overload to ensure we pass the pinned address
        int result = _server.RegisterArea(areaCode, index, handle.AddrOfPinnedObject(), buffer.Length);
        
        if (result != 0)
        {
            _log.Error($"Failed to register area {areaCode} index {index} (Size: {buffer.Length}): Error {result}");
            // If registration failed, we might want to free the handle? 
            // But maybe we retry? For now, let's keep it to avoid double-free if we logic changes.
            // But strictly speaking, if it failed, Snap7 doesn't use it.
        }
        else
        {
             _log.Info($"Successfully registered area {areaCode} index {index} (Size: {buffer.Length})");
        }
    }

    public void Start()
    {
        int result = _server.StartTo("0.0.0.0"); // 监听所有IP
        if (result == 0)
        {
            Console.WriteLine("S7 Server 启动成功 @ port 102");
            _log.Info("S7 Server started successfully on port 102");
            
            // Start monitoring timers
            _eventPollingTimer.Change(EventPollingIntervalMs, EventPollingIntervalMs);
            _idleCheckTimer.Change(IdleCheckIntervalMs, IdleCheckIntervalMs);
            _log.Info("Client monitoring started");
        }
        else
        {
            Console.WriteLine("S7 Server 启动失败: " + result);
            _log.Error($"S7 Server failed to start with error code: {result}");
        }
    }

    public void Stop()
    {
        // Stop monitoring timers
        _eventPollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _idleCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        _server.Stop();
        _clientLastActivity.Clear();

        // Free GC handles
        foreach (var handle in _gcHandles)
        {
            if (handle.IsAllocated)
                handle.Free();
        }
        _gcHandles.Clear();
        
        Console.WriteLine("S7 Server 已停止");
        _log.Info("S7 Server stopped");
    }

    private void PollServerEvents(object state)
    {
        try
        {
            var evt = new S7Server.USrvEvent();
            while (_server.PickEvent(ref evt))
            {
                ProcessServerEvent(evt);
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error polling server events", ex);
        }
    }

    private void ProcessServerEvent(S7Server.USrvEvent evt)
    {
        var eventTime = evt.EvtTime;
        var eventCode = evt.EvtCode;
        var eventSender = evt.EvtSender.ToString();
        
        if (eventCode == S7Server.evcClientAdded)
        {
            _clientLastActivity[eventSender] = DateTime.Now;
            _log.Info($"Client connected: {eventSender}");
        }
        else if (eventCode == S7Server.evcClientDisconnected || eventCode == S7Server.evcClientTerminated)
        {
            _clientLastActivity.TryRemove(eventSender, out _);
            _log.Info($"Client disconnected: {eventSender}");
        }
        else if (eventCode == S7Server.evcClientRejected)
        {
            _log.Warn($"Client rejected: {eventSender}");
        }
        else if(eventCode == S7Server.evcDataRead)
        {
            _log.Info($"Client wanted to read: {eventSender}");
            _log.Info($"param1:{evt.EvtParam1},param2:{evt.EvtParam2},param3:{evt.EvtParam3},param4:{evt.EvtParam4}");
            //根据参数从_memory读取变量值
            // EvtParam1: Area Code (e.g., S7Server.srvAreaDB)
            // EvtParam2: DB Number (if Area is DB)
            // EvtParam3: Start Offset
            // EvtParam4: Data Size (amount of data read)
            ReadNodeActivity(evt);

        }
        else if (eventCode == S7Server.evcDataWrite)
        {
            _log.Info($"Client wanted to write: {eventSender}");
            _log.Info($"param1:{evt.EvtParam1},param2:{evt.EvtParam2},param3:{evt.EvtParam3},param4:{evt.EvtParam4}");
            //根据参数将参数写入_memory
            // EvtParam1: Area Code (e.g., S7Server.srvAreaDB)
            // EvtParam2: DB Number (if Area is DB)
            // EvtParam3: Start Offset
            // EvtParam4: Data Size (amount of data read)
            WriteNodeActivity(evt);
        }
        else if (eventCode == S7Server.evcClientNoRoom)
        {
            _log.Warn($"Client connection failed - no room: {eventSender}");
        }
        else if (eventCode == S7Server.evcPDUincoming)
        {
            // Update last activity time for read/write operations
            if (_clientLastActivity.ContainsKey(eventSender))
            {
                _clientLastActivity[eventSender] = DateTime.Now;
                _log.Debug($"Client activity: {eventSender} - PDU incoming");
            }
        }
        else if (eventCode == S7Server.evcServerStarted)
        {
            _log.Info("Server started event");
        }
        else if (eventCode == S7Server.evcServerStopped)
        {
            _log.Info("Server stopped event");
        }
        else if (eventCode == S7Server.evcListenerCannotStart)
        {
            _log.Error("Listener cannot start");
        }
        else if (eventCode == S7Server.evcClientException)
        {
            _log.Error($"Client exception: {eventSender}");
        }
        else if (eventCode == S7Server.evcClientsDropped)
        {
            _log.Warn($"Clients dropped: {eventSender}");
        }
        else
        {
            _log.Debug($"Server event: Code={eventCode}, Sender={eventSender}");
        }
    }

    private void ReadNodeActivity(S7Server.USrvEvent evt)
    {
        try
        {
            int area = evt.EvtParam1;
            int dbNumber = evt.EvtParam2;
            int start = evt.EvtParam3;
            int size = evt.EvtParam4;
            
            byte[] data = null;
            string areaName = "";

            if (area == S7Client.S7AreaDB)
            {
                areaName = $"DB{dbNumber}";
                if (_memory.DBs.TryGetValue(dbNumber, out var dbData))
                {
                    data = dbData;
                }
            }
            else if (area == S7Client.S7AreaMK)
            {
                areaName = "M";
                data = _memory.MB;
            }
            else if (area == S7Client.S7AreaPE)
            {
                areaName = "I"; 
                data = _memory.IB;
            }
            else if (area == S7Client.S7AreaPA)
            {
                areaName = "Q"; 
                data = _memory.QB;
            }
            else if (area == S7Client.S7AreaCT)
            {
                areaName = "C";
                data = _memory.IB; // Note: Constructor mapped CT to IB, keeping consistency for now
            }
            else if (area == S7Client.S7AreaTM)
            {
                areaName = "T";
                data = _memory.QB; // Note: Constructor mapped TM to QB, keeping consistency for now
            }

            if (data != null)
            {
                if (start + size <= data.Length)
                {
                    var readData = new byte[size];
                    // Use Array.Copy to safely read the data
                    Array.Copy(data, start, readData, 0, size);
                    string hex = BitConverter.ToString(readData);
                    _log.Info($"[Read Activity] {areaName} Offset:{start} Size:{size} Value(Hex):{hex}");
                }
                else
                {
                    _log.Warn($"[Read Activity] Out of Bounds: {areaName} Offset:{start} Size:{size} AreaSize:{data.Length}");
                }
            }
            else
            {
                _log.Warn($"[Read Activity] Unknown Area or DB not found: Area:{area} DB:{dbNumber}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error in ReadNodeActivity", ex);
        }
    }

    private void WriteNodeActivity(S7Server.USrvEvent evt)
    {
        try
        {
            int area = evt.EvtParam1;
            int dbNumber = evt.EvtParam2;
            int start = evt.EvtParam3;
            int size = evt.EvtParam4;
            
            byte[] data = null;
            string areaName = "";

            if (area == S7Client.S7AreaDB)
            {
                areaName = $"DB{dbNumber}";
                if (_memory.DBs.TryGetValue(dbNumber, out var dbData))
                {
                    data = dbData;
                }
            }
            else if (area == S7Client.S7AreaMK)
            {
                areaName = "M";
                data = _memory.MB;
            }
            else if (area == S7Client.S7AreaPE)
            {
                areaName = "I"; 
                data = _memory.IB;
            }
            else if (area == S7Client.S7AreaPA)
            {
                areaName = "Q"; 
                data = _memory.QB;
            }
            else if (area == S7Client.S7AreaCT)
            {
                areaName = "C";
                data = _memory.IB; // Note: Constructor mapped CT to IB
            }
            else if (area == S7Client.S7AreaTM)
            {
                areaName = "T";
                data = _memory.QB; // Note: Constructor mapped TM to QB
            }

            if (data != null)
            {
                if (start + size <= data.Length)
                {
                    var writeData = new byte[size];
                    // The memory is already updated by S7Server (Snap7), so we just read the new value.
                    Array.Copy(data, start, writeData, 0, size);
                    string hex = BitConverter.ToString(writeData);
                    _log.Info($"[Write Activity] {areaName} Offset:{start} Size:{size} NewValue(Hex):{hex}");
                }
                else
                {
                    _log.Warn($"[Write Activity] Out of Bounds: {areaName} Offset:{start} Size:{size} AreaSize:{data.Length}");
                }
            }
            else
            {
                _log.Warn($"[Write Activity] Unknown Area or DB not found: Area:{area} DB:{dbNumber}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error in WriteNodeActivity", ex);
        }
    }

    private void CheckIdleClients(object state)
    {
        try
        {
            var now = DateTime.Now;
            var idleTimeout = TimeSpan.FromMinutes(IdleTimeoutMinutes);

            foreach (var kvp in _clientLastActivity)
            {
                var clientId = kvp.Key;
                var lastActivity = kvp.Value;
                var idleTime = now - lastActivity;

                if (idleTime > idleTimeout)
                {
                    _log.Warn($"Client {clientId} idle for {idleTime.TotalMinutes:F1} minutes, disconnecting...");
                    
                    // Try to disconnect the client
                    // Note: Sharp7 S7Server doesn't have a direct disconnect method for individual clients
                    // We'll remove from tracking and log it. The client will be disconnected on next operation.
                    _clientLastActivity.TryRemove(clientId, out _);
                    
                    _log.Info($"Client {clientId} removed from tracking due to idle timeout");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error checking idle clients", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _eventPollingTimer?.Dispose();
            _idleCheckTimer?.Dispose();
            Stop();
        }

        _disposed = true;
    }

    ~PlcServer()
    {
        Dispose(false);
    }
}
