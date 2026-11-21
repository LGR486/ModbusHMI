using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Services
{
    public static class ModbusFactory
    {
        public static IModbusService Create(bool useTcp) =>
            useTcp ? new ModbusTcpService() : new ModbusRtuService();
    }
}
