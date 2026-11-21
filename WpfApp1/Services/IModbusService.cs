using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.Models;


namespace WpfApp1.Services
{
    public interface IModbusService : IDisposable
    {
        bool IsConnected { get; }
        event Action<string> Log;
        Task ConnectAsync(DeviceConfig cfg, CancellationToken ct = default);
        void Disconnect();

        Task<ushort[]> ReadHoldingRegistersAsync(ushort start, ushort count, byte slaveId);
        Task<ushort[]> ReadInputRegistersAsync(ushort start, ushort count, byte slaveId);
        Task WriteSingleRegisterAsync(ushort addr, ushort value, byte slaveId);
        Task WriteMultipleRegistersAsync(ushort start, ushort[] values, byte slaveId);

    }
}
