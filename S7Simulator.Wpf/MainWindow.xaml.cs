using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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

        // 实时刷新当前值定时器
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (s, e) => DbList.ToList().ForEach(db => db.Variables.ToList());
        timer.Start();
    }

    private void LoadDbs()
    {
        DbList.Clear();
        var dbs = DatabaseService.LoadAllDbs();
        foreach (var db in dbs)
        {
            _memory.ApplyDbStructure(db); // 确保内存里有最新初始值
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
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is DbInfo db)
        {
            new DBDetailWindow(db, _memory) { Owner = this }.ShowDialog();
            // 双击后刷新当前值
            OnPropertyChanged(nameof(DbList));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
