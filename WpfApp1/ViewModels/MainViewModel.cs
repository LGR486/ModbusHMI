using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Helpers;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.DataSQL;
using WpfApp1.DataBaseService;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DeviceConfig _cfg = new() { Ip = "127.0.0.1", Port = 502 };
        private IModbusService? _svc;
        private CancellationTokenSource? _pollCts;// 可取消的信号 里面有 .Token成员

        public DataSQLite? Datasql = new();

        // ObservableCpllection<T>动态集合 添加删除刷新都会发出通知 且里面有数据列表 常用于实时显示数据
        public ObservableCollection<PointItem> Points { get; } = new();
        public ObservableCollection<string> Logs { get; } = new();// ListBox绑定的就是Logs
        public ObservableCollection<string> TagNames { get; } = new ObservableCollection<string>();//下拉列表
        public DataLogs DataLogs { get; } = new DataLogs();
        private readonly Dictionary<string, double> _lastSavedValues = new Dictionary<string, double>();

        // 当前选中的名字
        private string _selectedTagName;
        public string SelectedTagName
        {
            get => _selectedTagName;
            set => Set(ref _selectedTagName, value);
        }

        private int _failcount { get; set; } = 0;

        private bool _useTcp = true;//默认Tcp连接
        public bool UseTcp { get => _useTcp; set => Set(ref _useTcp, value); }

        private bool _isConnected;//连接状态
        public bool IsConnected { get => _isConnected; set => Set(ref _isConnected, value); }

        private string _ip = "127.0.0.1";
        public string Ip { get => _ip; set => Set(ref _ip, value); }

        private string _port = "502";
        public string Port { get => _port; set => Set(ref _port, value); }

        // 各个按钮控件绑定的命令
        public RelayCommand ConnectCmd { get; }
        public RelayCommand DisconnectCmd { get; }
        public RelayCommand WriteCmd { get; }
        public RelayCommand ReadHistory { get; }

        public MainViewModel()
        {
            // 示例点位：保持寄存器 0..9，缩放0.1
            for (ushort i = 0; i < 10; i++)
                Points.Add(new PointItem { Name = $"压力{i}", Address = i, Scale = 0.1, Unit = "MPa" });
            foreach (var pt in Points)
                TagNames.Add(pt.Name);//填充下拉框名称

            SelectedTagName = TagNames.FirstOrDefault();//下拉框默认第一个
            // 这里就是绑定按钮的与事件
            ConnectCmd = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
            DisconnectCmd = new RelayCommand(_ => Disconnect(), _ => IsConnected);
            WriteCmd = new RelayCommand(async p => await WriteAsync(p as PointItem), p => IsConnected && p is PointItem);

            Datasql.Log += PrintData;
            DataLogs.Log += AddLog;
            ReadHistory = new RelayCommand(async _ => await Datasql.DataReadAsync(SelectedTagName), _ => true);
        }

        #region 日志输出
        // 日志的固定写法 用户执行了操作 这里相关联的添加日志Application.Current.Dispatcher.Invoke(关联的操作)关联
        /// <summary>
        /// 输出调试信息 并给到保存日志队列
        /// </summary>
        /// <param name="s"></param>
        private void AddLog(string s)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {s}";
            // 添加保存日志文件内容 给到DataLogs的队列
            DataLogs.AddQueue(line);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, line);
                if (Logs.Count > 300) Logs.RemoveAt(Logs.Count - 1);
            });
        }
        /// <summary>
        /// 打印数据库专用
        /// </summary>
        /// <param name="s"></param>
        private void PrintData(string s)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, $" {s}");
                if (Logs.Count > 300) Logs.RemoveAt(Logs.Count - 1);
            });
        }
        #endregion

        private async Task ConnectAsync()
        {
            try
            {
                _svc = ModbusFactory.Create(UseTcp);
                _svc.Log += AddLog;
                _cfg.Ip = Ip;
                _cfg.Port = int.TryParse(Port, out var p) ? p : 502;
                await _svc.ConnectAsync(_cfg);
                IsConnected = true;
                StartPolling();// 轮询读取数据
            }
            catch (Exception ex)
            {
                AddLog("连接失败: " + ex.Message);
                IsConnected = false;
            }
        }

        private void Disconnect()
        {
            _pollCts?.Cancel();
            _svc?.Disconnect();
            _svc?.Dispose();
            _svc = null;
            IsConnected = false;
        }

        private void StartPolling()
        {
            _pollCts = new CancellationTokenSource();
            var token = _pollCts.Token;
            _ = Task.Run(async () => // 后台连续读取数据
            {
                while (!token.IsCancellationRequested )//&& _svc?.IsConnected == true)//自动重连要注释掉
                {
                    try
                    {
                        if (Points.Count > 0)
                        {
                            var start = Points.Min(p => p.Address);
                            var count = (ushort)(Points.Max(p => p.Address) - start + 1);
                            var regs = await _svc.ReadHoldingRegistersAsync(start, count, _cfg.SlaveId);
                            var recordsToSave = new List<HistoryRecord>(); // 准备要存库的数据
                            var now = DateTime.Now;
                            foreach (var pt in Points)
                            {
                                var idx = pt.Address - start;
                                if (idx >= 0 && idx < regs.Length)
                                {
                                    pt.Value = regs[idx] * pt.Scale;
                                    double newValue = pt.Value;

                                    // 检查字典里有没有这个点位的历史值
                                    if (!_lastSavedValues.TryGetValue(pt.Name, out double lastValue))
                                    {
                                        // 第一次读到这个点，必定存库
                                        _lastSavedValues[pt.Name] = newValue;
                                        recordsToSave.Add(CreateRecord(pt, now));
                                    }
                                    else
                                    {
                                        // 比较差值：如果变化幅度大于 0.01（防止浮点数精度微调导致狂写）
                                        if (Math.Abs(newValue - lastValue) > 0.01)
                                        {
                                            _lastSavedValues[pt.Name] = newValue; // 更新缓存
                                            recordsToSave.Add(CreateRecord(pt, now));
                                        }
                                    }
                                    
                                }    
                            }
                            // 2. 如果有变化的数据，批量存入 SQLite
                            if (recordsToSave.Count > 0)
                            {
                                await Datasql.DataWriteAsync(recordsToSave);
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        AddLog("轮询异常: " + ex.Message);
                        _failcount++;
                        if (_failcount >= 3)
                        {
                            AddLog("连续失败 3 次，尝试重连...");
                            Disconnect();
                            await Task.Delay(3000);
                            await ConnectAsync();
                            _failcount = 0;
                        }
                    }
                    //这里是设置了轮询的时间间隔 并且可取消 可在后台等待唤醒不空跑
                    await Task.Delay(_cfg.PollIntervalMs, token);//token参数 表可取消
                }
            }, token);
        }

        // 辅助方法：生成历史记录对象
        private HistoryRecord CreateRecord(PointItem pt, DateTime time)
        {
            return new HistoryRecord
            {
                TagName = pt.Name,
                Address = pt.Address,
                Value = pt.Value,
                //RawValue = pt.RawValue,
                RecordTime = time
            };
        }
        private async Task WriteAsync(PointItem pt)
        {
            if (pt == null || _svc == null) return;
            try
            {
                var raw = (ushort)Math.Round(pt.WriteValue / pt.Scale);
                await _svc.WriteSingleRegisterAsync(pt.Address, raw, _cfg.SlaveId);
                AddLog($"写寄存器[{pt.Address}]={raw} 成功");
            }
            catch (Exception ex)
            {
                AddLog("写失败: " + ex.Message);
            }
        }
    }
}