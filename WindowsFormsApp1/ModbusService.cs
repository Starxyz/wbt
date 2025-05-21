using EasyModbus;
using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class ModbusService : IModbusService, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ModbusClient _modbusClient;
        private bool _modbusConnected = false;
        private System.Windows.Forms.Timer _modbusKeepAliveTimer;
        private DateTime _lastModbusActivity = DateTime.MinValue;
        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const int KEEPALIVE_INTERVAL = 3000; // 3 秒检查一次
        private const string DEFAULT_WEIGHT_ERROR_VALUE = "--.-";

        private readonly string _ipAddress;
        private readonly int _port;

        public bool IsConnected => _modbusConnected;

        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> LogMessageRequested;

        public ModbusService(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;

            _modbusClient = new ModbusClient();
            // IPAddress and Port will be set in ConnectAsync

            _modbusKeepAliveTimer = new System.Windows.Forms.Timer();
            _modbusKeepAliveTimer.Interval = KEEPALIVE_INTERVAL;
            _modbusKeepAliveTimer.Tick += ModbusKeepAlive_Tick;
        }

        public async Task ConnectAsync()
        {
            try
            {
                _modbusClient.IPAddress = _ipAddress;
                _modbusClient.Port = _port;
                _modbusClient.ConnectionTimeout = 5000; // 5秒超时

                // EasyModbusClient.Connect() is synchronous, wrap in Task.Run
                await Task.Run(() => _modbusClient.Connect());

                _modbusConnected = true;
                _lastModbusActivity = DateTime.Now;
                _reconnectAttempts = 0;

                if (!_modbusKeepAliveTimer.Enabled)
                {
                    _modbusKeepAliveTimer.Start();
                }
                LogMessageRequested?.Invoke("Modbus连接成功");
                ConnectionStatusChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                _modbusConnected = false;
                Logger.Error(ex, "Modbus连接失败");
                LogMessageRequested?.Invoke($"Modbus连接失败: {ex.Message}");
                ConnectionStatusChanged?.Invoke(false);
                // Optionally re-throw or handle more specifically if Form1 needs to know about the failure reason directly
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_modbusKeepAliveTimer.Enabled)
                {
                    _modbusKeepAliveTimer.Stop();
                }

                if (_modbusClient.Connected)
                {
                    _modbusClient.Disconnect();
                }
                _modbusConnected = false;
                LogMessageRequested?.Invoke("Modbus已断开");
                ConnectionStatusChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Modbus断开失败");
                LogMessageRequested?.Invoke($"Modbus断开失败: {ex.Message}");
                // Even if disconnect fails, we consider it disconnected for the application state
                _modbusConnected = false; 
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        private void ModbusKeepAlive_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastModbusActivity).TotalMilliseconds >= KEEPALIVE_INTERVAL)
            {
                Logger.Debug("执行 Modbus 连接保持检查");
                PerformModbusHeartbeat();
            }
        }

        private void PerformModbusHeartbeat()
        {
            try
            {
                if (_modbusClient == null || !_modbusClient.Connected)
                {
                    Logger.Info("Modbus 连接已断开，尝试重新连接...");
                    LogMessageRequested?.Invoke("Modbus 连接已断开，尝试重新连接...");
                    ReconnectModbusClient();
                    return;
                }

                _modbusClient.ReadHoldingRegisters(0, 1); // Simple read as heartbeat
                _lastModbusActivity = DateTime.Now;
                _reconnectAttempts = 0;
                Logger.Debug("Modbus 连接保持成功");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Modbus 连接保持失败: {ex.Message}");
                LogMessageRequested?.Invoke($"Modbus 连接保持失败: {ex.Message}");
                ReconnectModbusClient();
            }
        }

        private void ReconnectModbusClient()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                Logger.Error($"在 {MAX_RECONNECT_ATTEMPTS} 次尝试后无法重新连接到 Modbus");
                LogMessageRequested?.Invoke($"在 {MAX_RECONNECT_ATTEMPTS} 次尝试后无法重新连接到 Modbus");
                _modbusKeepAliveTimer.Stop();
                
                if (_modbusConnected) // Only change status if it was previously connected
                {
                    _modbusConnected = false;
                    ConnectionStatusChanged?.Invoke(false);
                }
                return;
            }

            _reconnectAttempts++;
            LogMessageRequested?.Invoke($"Modbus 第 {_reconnectAttempts} 次重连尝试");

            try
            {
                if (_modbusClient != null && _modbusClient.Connected)
                {
                    _modbusClient.Disconnect();
                }

                int delay = 500 * _reconnectAttempts;
                Logger.Info($"等待 {delay}ms 后进行第 {_reconnectAttempts} 次重连尝试");
                System.Threading.Thread.Sleep(delay); // Consider Task.Delay for async context if this method becomes async

                _modbusClient.Connect(); // This is synchronous
                _lastModbusActivity = DateTime.Now;
                
                if (!_modbusConnected) // Only change status if it was previously disconnected
                {
                    _modbusConnected = true;
                    ConnectionStatusChanged?.Invoke(true);
                }
                LogMessageRequested?.Invoke("成功重新连接到 Modbus");
                Logger.Info("成功重新连接到 Modbus");
            }
            catch (Exception ex)
            {
                Logger.Error($"第 {_reconnectAttempts} 次重连尝试失败: {ex.Message}");
                LogMessageRequested?.Invoke($"第 {_reconnectAttempts} 次重连尝试失败: {ex.Message}");
                if (_modbusConnected) // If connection fails, update status
                {
                    _modbusConnected = false;
                    ConnectionStatusChanged?.Invoke(false);
                }
            }
        }

        public async Task<string> GetWeightAsync()
        {
            try
            {
                if (_modbusClient == null || !_modbusClient.Connected)
                {
                    Logger.Info("Modbus 未连接，正在尝试连接...");
                    LogMessageRequested?.Invoke("Modbus 未连接，正在尝试连接...");
                    await ConnectAsync(); // Attempt to connect
                    if (!_modbusConnected) // If connection failed
                    {
                        LogMessageRequested?.Invoke("获取重量失败: Modbus 连接不成功。");
                        return DEFAULT_WEIGHT_ERROR_VALUE;
                    }
                }

                // ReadHoldingRegisters is synchronous, wrap in Task.Run
                int[] result = await Task.Run(() => _modbusClient.ReadHoldingRegisters(0, 10));
                Logger.Info($"读取到原始值: {result[0]}");
                LogMessageRequested?.Invoke($"读取到原始值: {result[0]}");

                _lastModbusActivity = DateTime.Now;

                int rawValue = result[0];
                string formattedValue = (rawValue / 1000.0).ToString("F2", CultureInfo.InvariantCulture);
                
                return formattedValue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "读取重量失败");
                LogMessageRequested?.Invoke($"读取重量失败: {ex.Message}");
                ReconnectModbusClient(); // Attempt to reconnect for next time
                return DEFAULT_WEIGHT_ERROR_VALUE;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_modbusKeepAliveTimer != null)
                {
                    _modbusKeepAliveTimer.Stop();
                    _modbusKeepAliveTimer.Dispose();
                    _modbusKeepAliveTimer = null;
                }
                if (_modbusClient != null)
                {
                    if (_modbusClient.Connected)
                    {
                        _modbusClient.Disconnect();
                    }
                    // ModbusClient itself might not be IDisposable, so no _modbusClient.Dispose()
                    _modbusClient = null; 
                }
                _modbusConnected = false;
                ConnectionStatusChanged?.Invoke(false); // Notify that it's disconnected due to disposal
            }
        }
    }
}
