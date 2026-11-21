using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class DeviceConfig
    {
        public string Name { get; set; } = "PLC-1";
        public string Ip { get; set; } = "192.168.1.10";
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 1;
        public int PollIntervalMs { get; set; } = 500;//轮询周期
    }
}
