using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S7Simulator.Wpf.Models;
using S7Simulator.Wpf.Plc;
using S7Simulator.Wpf.Services;
using S7Simulator.Wpf.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace S7Simulator.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));
    private readonly PlcMemory _memory = new();
    private PlcServer _server;  // Changed from readonly, will be initialized in Initialize()
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private string _status = "就绪";

    [ObservableProperty]
    private string _serverStatus = "Server 未启动";

    public ObservableCollection<DbInfo> DbList { get; } = new();

    public MainViewModel()
    {
        _databaseService = DatabaseService.Instance;
        // PlcServer creation moved to Initialize() to ensure log4net is ready
        
        // Initialize
        Initialize();
    }

    private async void Initialize()
    {
        try
        {
            await _databaseService.InitializeAsync();
            
            // Create PlcServer here, after log4net is initialized
            _server = new PlcServer(_memory);
            
            await LoadDbsAsync();

            _server.Start();
            
            ServerStatus = "S7 Server 已启动 (端口 102)";
            _log.Info("Application initialized successfully");
        }
        catch (Exception ex)
        {
            Status = $"初始化失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadDbsAsync()
    {
        try
        {
            DbList.Clear();
            var dbs = await _databaseService.LoadAllDbsAsync();
            foreach (var db in dbs)
            {
                _memory.ApplyDbStructure(db);
                _server.RegisterDb(db.DbNumber);
                DbList.Add(db);
            }
            Status = $"加载了 {DbList.Count} 个 DB";
            _log.Info($"Loaded {DbList.Count} DBs");
        }
        catch (Exception ex)
        {
            Status = $"加载 DB 失败: {ex.Message}";
            _log.Error("Failed to load DBs", ex);
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Excel 文件|*.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                Status = "正在导入...";
                // ExcelImporter is synchronous for now, we can wrap it in Task.Run if it's slow,
                // but for now let's keep it simple or refactor it later. 
                // Since ExcelImporter interacts with _memory (which is thread-safe mostly), 
                // and we want to update status.
                
                // Ideally ExcelImporter should be async too, but let's just run it.
                // Note: ExcelImporter.ImportFromExcel takes a callback for status.
                
                await ExcelImporter.ImportFromExcelAsync(dlg.FileName, _memory, s => 
                {
                    Application.Current.Dispatcher.Invoke(() => Status = s);
                });

                await LoadDbsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Status = "导入失败";
                _log.Error($"Failed to import Excel: {dlg.FileName}", ex);
            }
        }
    }

    public void OpenDbDetail(DbInfo db)
    {
        if (db == null) return;
        // We need to access Window to show dialog. 
        // In strict MVVM, we should use a service for Dialogs.
        // For this refactoring, we can keep it simple or pass the owner.
        // But ViewModel shouldn't know about View. 
        // Let's handle the double click in View code-behind but call ViewModel command, 
        // or just keep the window opening logic in View code-behind for now as it involves UI elements.
        // However, the requirement says "Remove business logic" from code-behind.
        // Opening a detail window is UI logic, so it's acceptable in code-behind or via a DialogService.
        // I will expose the PlcMemory so the View can pass it to the new Window if needed, 
        // or better, the ViewModel handles the data and the View just shows it.
        
        // But DBDetailWindow constructor takes (DbInfo, PlcMemory).
        // So I need to expose PlcMemory or provide a method to get it.
    }

    public PlcMemory Memory => _memory;
}
