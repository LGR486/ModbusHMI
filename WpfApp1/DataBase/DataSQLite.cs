using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using WpfApp1.Models;
using WpfApp1.DataBaseService;

namespace WpfApp1.DataSQL
{
    public class DataSQLite : IDataBaseService
    {
        static string Connstr { get; set; } = "Data Source=HmiDb.db;Version=3;";
        private readonly object _dblock = new();
        public event Action<string> Log;

        public DataSQLite()
        {
            try//这里触发日志可能控件都没加载出来所以没显示
            {
                using SQLiteConnection conn = new(Connstr);
                conn.Open();
                using var cmd = new SQLiteCommand(
                @"CREATE TABLE IF NOT EXISTS HistoryData(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TagName TEXT NOT NULL,
                Address INTEGER NOT NULL,
                Value REAL NOT NULL,
                RawValue INTEGER NOT NULL,
                RecordTime TEXT NOT NULL)", conn);
                cmd.ExecuteNonQuery();
                
                ClearData();

            }
            catch (Exception ex)
            {
                Log?.Invoke($"{DateTime.Now:HH:mm:ss.fff}" + "创建失败:" + ex);
            }
        }
        /// <summary>
        /// 定期清理
        /// </summary>
        public void ClearData()
        {
            try
            {
                using SQLiteConnection conn = new(Connstr);
                conn.Open();
                using var cmd = new SQLiteCommand(
                    "DELETE FROM HistoryData WHERE RecordTime < DATEADD(DAY, -30, GETDATE());", conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"{DateTime.Now:HH:mm:ss.fff}" + "数据库清理失败:" + ex);
            }

        }
        public Task DataWriteAsync(List<HistoryRecord> records)
        {
            return Task.Run(() =>
            {
                if (records == null || records.Count == 0) return;

                try
                {
                    lock (_dblock)
                    {
                        using var conn = new SQLiteConnection(Connstr);
                        conn.Open();
                        using var transaction = conn.BeginTransaction();
                        using var cmd = new SQLiteCommand(
                            "INSERT INTO HistoryData(TagName, Address, Value, RawValue, RecordTime)" +
                            "VALUES(@tag, @addr, @val, @raw, @time)", conn, transaction);
                        var pTag = cmd.Parameters.Add("@tag", System.Data.DbType.String);
                        var pAddr = cmd.Parameters.Add("@addr", System.Data.DbType.Int32);
                        var pVal = cmd.Parameters.Add("@val", System.Data.DbType.Double);
                        var pRaw = cmd.Parameters.Add("@raw", System.Data.DbType.Int32);
                        var pTime = cmd.Parameters.Add("@time", System.Data.DbType.String);

                        string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");// 数据库精确到毫秒 不会出现排序错误
                        foreach (var record in records)
                        {
                            pTag.Value = record.TagName;
                            pAddr.Value = record.Address;
                            pVal.Value = record.Value;
                            pRaw.Value = record.RawValue;
                            pTime.Value = timeStr;
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"{DateTime.Now:HH:mm:ss.fff}" + "数据库写入失败:" + ex);
                }
            });
        }
        public Task DataReadAsync(string tagName)//改成字符串参数，下拉框
        {
            return Task.Run(() =>
            {
                try
                {
                    lock (_dblock)
                    {
                        using SQLiteConnection conn = new(Connstr);
                        conn.Open();
                        using var cmd = new SQLiteCommand(
                        "SELECT * FROM HistoryData WHERE TagName=@tag ORDER BY RecordTime DESC LIMIT 100", conn);
                        cmd.Parameters.AddWithValue("@tag", tagName);
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            Log?.Invoke($"{reader["TagName"]} {reader["Value"]} {reader["RecordTime"]}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"{DateTime.Now:HH:mm:ss.fff}" + "数据库读取失败:" + ex);
                }
            });
        }
        public void Dispose()
        {
            // pass
        }
    }
}
