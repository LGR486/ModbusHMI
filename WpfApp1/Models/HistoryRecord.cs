using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{   
    /// <summary>
    /// 定义了数据库里的历史数据类型
    /// </summary>
    public class HistoryRecord
    {
        public string TagName { get; set; }
        public int Address { get; set; } 
        public double Value { get; set; }
        public int RawValue { get; set; }
        public DateTime RecordTime { get; set; }
    }
}
