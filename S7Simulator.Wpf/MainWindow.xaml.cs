using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using S7Simulator.Wpf.Models;
using S7Simulator.Wpf.Plc;
using S7Simulator.Wpf.Services;
using S7Simulator.Wpf.Views;

namespace S7Simulator.Wpf;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<DbInfo> DbList { get; } = new();
    private readonly PlcMemory _memory = new();
    private readonly PlcServer _server;

    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    private string _status = "就绪";

    public string ServerStatus => _server != null ? "S7 Server 已启动 (端口 102)" : "Server 未启动";

    public ICommand ImportCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        DatabaseService.Initialize();

        ImportCommand = new RelayCommand(ImportExcel);
        RefreshCommand = new RelayCommand(LoadDbs);

        _server = new PlcServer(_memory);
        _server.Start();

        LoadDbs();
    }

    private void LoadDbs()
    {
        DbList.Clear();
        var dbs = DatabaseService.LoadAllDbs();
        foreach (var db in dbs)
        {
            _memory.ApplyDbStructure(db);
            DbList.Add(db);
        }
        Status = $"加载了 {DbList.Count} 个 DB";
    }

    private void ImportExcel()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Excel 文件|*.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                ExcelImporter.ImportFromExcel(dlg.FileName, _memory, s => Status = s);
                LoadDbs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is DbInfo db)
        {
            new DBDetailWindow(db, _memory) { Owner = this }.ShowDialog();
            OnPropertyChanged(nameof(DbList));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
