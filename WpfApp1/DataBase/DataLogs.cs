using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Data.SqlTypes;

namespace WpfApp1.DataBaseService
{
    public class DataLogs : IDataBaseService
    {
        // 保存txt文件

        public event Action<string> Log;

        private readonly object _filelock = new();
        // 线程安全队列，避免多线程写入冲突
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        // 定时器
        private readonly Timer _timer;
        // 保存间隔（毫秒）
        private readonly int _intervalMs = 5000;
        public DataLogs()
        {
            // 启动定时保存（首次触发在 _intervalMs 后）
            _timer = new Timer(_ => AutoSaveLogsAsync(), null, _intervalMs, _intervalMs);

            // 程序退出时兜底保存
            //AppDomain.CurrentDomain.ProcessExit += (s, e) => AutoSaveLogsAsync();
        }
        public void ClearData()
        {
            //pass
        }
        public void AddQueue(string line)
        {
            //if (string.IsNullOrWhiteSpace(line)) return;
            _logQueue.Enqueue(line);
        }
        
        public Task AutoSaveLogsAsync()
        {
            return Task.Run( () =>
            {
                try
                {
                    if (_logQueue.IsEmpty) return;// 队列为空直接退出
                    string file = $"Logs_{DateTime.Now:yyyyMMdd}.txt";// 生成文件的文件路径有一个坑 在bin里
                    // 取出当前所有待写日志
                    var sb = new StringBuilder();// 开出一个缓冲区
                    while (_logQueue.TryDequeue(out var line))
                    {
                        sb.AppendLine(line);
                    }
                    if (sb.Length == 0) return;
                    
                    lock (_filelock)
                    {
                        File.AppendAllText(file, sb.ToString(), Encoding.UTF8);
                    }

                }
                catch (Exception ex)
                {
                    Log?.Invoke("自动保存日志失败" + ex);
                    // 可继续添加失败处理
                }
            });
        }
        public void Dispose()
        {
            _timer?.Dispose();
            AutoSaveLogsAsync(); // 退出前最后保存一次
        }
    }
}
