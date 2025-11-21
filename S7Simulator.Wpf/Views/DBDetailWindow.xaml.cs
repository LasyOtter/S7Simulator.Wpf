using S7Simulator.Wpf.Models;
using S7Simulator.Wpf.Plc;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace S7Simulator.Wpf.Views
{
    /// <summary>
    /// DBDetailWindow.xaml 的交互逻辑（完整版）
    /// </summary>
    public partial class DBDetailWindow : Window, INotifyPropertyChanged
    {
        public DbInfo Db { get; }
        public PlcMemory Memory { get; }

        // 用于绑定到 DataGrid 的变量集合（包装后支持 CurrentValue 和编辑）
        public System.Collections.ObjectModel.ObservableCollection<VariableWrapper> Variables { get; }

        private readonly DispatcherTimer _refreshTimer;

        public DBDetailWindow(DbInfo db, PlcMemory memory)
        {
            InitializeComponent();

            Db = db ?? throw new ArgumentNullException(nameof(db));
            Memory = memory ?? throw new ArgumentNullException(nameof(memory));

            // 包装变量，增加实时值和编辑支持
            Variables = new System.Collections.ObjectModel.ObservableCollection<VariableWrapper>(
                db.Variables.Select(v => new VariableWrapper(v, Memory, db.DbNumber))
            );

            DataContext = this;
            Title = $"DB{db.DbNumber} - 变量详情（共 {Variables.Count} 个）";

            // 300ms 刷新一次当前值（性能与实时性平衡最佳）
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _refreshTimer.Tick += (s, e) =>
            {
                foreach (var wrapper in Variables)
                    wrapper.RefreshCurrentValue(); // 只刷新属性变更通知
            };
            _refreshTimer.Start();

            Loaded += (s, e) => _refreshTimer.Start();
            Closed += (s, e) => _refreshTimer.Stop();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }

    /// <summary>
    /// VariableInfo 的运行时包装类，支持 CurrentValue 实时读取 + 手动写值
    /// </summary>
    public class VariableWrapper : INotifyPropertyChanged
    {
        private readonly VariableInfo _var;
        private readonly PlcMemory _memory;
        private readonly int _dbNumber;

        public string Name => _var.Name;
        public string DataType => _var.DataType;
        public int ByteOffset => _var.ByteOffset;
        public int BitOffset => _var.BitOffset;
        public string InitialValue => _var.InitialValue;
        public string Comment => _var.Comment;

        // 完整 S7 地址，例如：DB1.DBX0.0、DB1.DBD10、DB1.DBW6
        public string Address
        {
            get
            {
                if (BitOffset >= 0)
                    return $"DB{_dbNumber}.DBX{ByteOffset}.{BitOffset}";
                return DataType switch
                {
                    "REAL" or "DINT" => $"DB{_dbNumber}.DBD{ByteOffset}",
                    "INT" => $"DB{_dbNumber}.DBW{ByteOffset}",
                    "BYTE" => $"DB{_dbNumber}.DBB{ByteOffset}",
                    _ => $"DB{_dbNumber}.DBX{ByteOffset}"
                };
            }
        }

        private string _currentValue = string.Empty;
        public string CurrentValue
        {
            get => _currentValue;
            private set
            {
                if (_currentValue != value)
                {
                    _currentValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public VariableWrapper(VariableInfo variable, PlcMemory memory, int dbNumber)
        {
            _var = variable;
            _memory = memory;
            _dbNumber = dbNumber;
            RefreshCurrentValue(); // 初始化一次
        }

        /// <summary>
        /// 强制刷新当前值（定时器调用）
        /// </summary>
        public void RefreshCurrentValue()
        {
            try
            {
                CurrentValue = ReadCurrentValue();
            }
            catch
            {
                CurrentValue = "<错误>";
            }
        }

        /// <summary>
        /// 手动写入新值（编辑结束后调用）
        /// </summary>
        public bool WriteValue(string newValue)
        {
            try
            {
                switch (DataType)
                {
                    case "BOOL":
                        _memory.WriteBool(_dbNumber, ByteOffset, BitOffset, bool.Parse(newValue));
                        break;
                    case "BYTE":
                        _memory.WriteByte(_dbNumber, ByteOffset, byte.Parse(newValue));
                        break;
                    case "INT":
                        _memory.WriteInt(_dbNumber, ByteOffset, short.Parse(newValue));
                        break;
                    case "DINT":
                        _memory.WriteDInt(_dbNumber, ByteOffset, int.Parse(newValue));
                        break;
                    case "REAL":
                        _memory.WriteReal(_dbNumber, ByteOffset, float.Parse(newValue.Replace(',', '.')));
                        break;
                    case "STRING":
                        _memory.WriteString(_dbNumber, ByteOffset, 254, newValue);
                        break;
                    default:
                        return false;
                }
                RefreshCurrentValue();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ReadCurrentValue()
        {
            return DataType switch
            {
                "BOOL" => _memory.ReadBool(_dbNumber, ByteOffset, BitOffset).ToString().ToLower(),
                "BYTE" => _memory.ReadByte(_dbNumber, ByteOffset).ToString(),
                "INT" => _memory.ReadInt(_dbNumber, ByteOffset).ToString(),
                "DINT" => _memory.ReadDInt(_dbNumber, ByteOffset).ToString(),
                "REAL" => _memory.ReadReal(_dbNumber, ByteOffset).ToString("F3"),
                "STRING" => $"\"{_memory.ReadString(_dbNumber, ByteOffset, 254)}\"",
                _ => "<未知类型>"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}