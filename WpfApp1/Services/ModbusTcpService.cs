using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.Models;

using Modbus.Device;
using System.Collections;
using System.Windows;

namespace WpfApp1.Services
{
    public class ModbusTcpService  : IModbusService
    {
        private TcpClient _tcp;
        private IModbusMaster _master;
        private readonly object _lock = new();

        public bool IsConnected => _tcp?.Connected == true && _master != null;
        // 对外的接口 .Log += AddLog 一般在类里添加日志信息是这样写的
        public event Action<string> Log;
        public async Task ConnectAsync(DeviceConfig cfg, CancellationToken ct = default)
        {
            Disconnect();
            _tcp = new TcpClient();
            await _tcp.ConnectAsync(cfg.Ip, cfg.Port);//用不上ct???用不上取消通知参数
            _master = ModbusIpMaster.CreateIp(_tcp);
            _master.Transport.ReadTimeout = 1000;
            _master.Transport.WriteTimeout = 1000;
            _master.Transport.Retries = 2;
            Log?.Invoke($"[TCP]已连接{cfg.Ip}:{cfg.Port}");
        }
        public void Disconnect()
        {
            try { _master?.Dispose(); } catch { }
            try { _tcp?.Close(); } catch { }
            _master = null;
            _tcp = null;
            Log?.Invoke("[TCP]已断开");
        }

        //Task.Run(函数闭包) 异步io lock关键字 锁 lock (_lock) _lock是obj
        //Task.Run()就是挂线程
        //这里lock的逻辑是单读单写 _lock被上锁 此ModbusTcpService类其他线程就只能等待_lock被释放
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
