using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class ModbusRtuService : IModbusService
    {
        private SerialPort _port;
        private IModbusSerialMaster _master;
        private readonly object _lock = new();

        public bool IsConnected => _port?.IsOpen == true && _master != null;
        public event Action<string> Log;

        public string ComPort { get; set; } = "COM3";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;

        public Task ConnectAsync(DeviceConfig cfg, CancellationToken ct = default)
        {
            Disconnect();
            _port = new SerialPort(ComPort, BaudRate, Parity, DataBits, StopBits) { ReadTimeout = 1000, WriteTimeout = 1000 };
            _port.Open();
            _master = ModbusSerialMaster.CreateRtu(_port);
            _master.Transport.Retries = 2;
            Log?.Invoke($"[RTU] 已打开 {ComPort}");
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            try { _master?.Dispose(); } catch { }
            try { _port?.Close(); } catch { }
            _master = null;
            _port = null;
            Log?.Invoke("[RTU] 已关闭");
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(ushort start, ushort count, byte slaveId)
            => Task.Run(() => { lock (_lock) return _master.ReadHoldingRegisters(slaveId, start, count); });

        public Task<ushort[]> ReadInputRegistersAsync(ushort start, ushort count, byte slaveId)
            => Task.Run(() => { lock (_lock) return _master.ReadInputRegisters(slaveId, start, count); });

        public Task WriteSingleRegisterAsync(ushort addr, ushort value, byte slaveId)
            => Task.Run(() => { lock (_lock) _master.WriteSingleRegister(slaveId, addr, value); });

        public Task WriteMultipleRegistersAsync(ushort start, ushort[] values, byte slaveId)
            => Task.Run(() => { lock (_lock) _master.WriteMultipleRegisters(slaveId, start, values); });

        public void Dispose() => Disconnect();
    }
}
