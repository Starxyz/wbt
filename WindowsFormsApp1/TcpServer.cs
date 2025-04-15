using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace WindowsFormsApp1
{
    public class TcpServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public int Port { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public string Endpoint => $"{IPAddress}:{Port}";

        public event Action<string> LogMessage;
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> MessageReceived;

        public TcpServer(int port)
        {
            Port = port;
            IPAddress = GetLocalIPAddress();
        }

        private IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);
            
            try
            {
                _listener.Start();
                _isRunning = true;
                LogMessage?.Invoke($"TCP服务已启动，监听地址: {Endpoint}");
                Logger.Info($"TCP服务已启动，监听地址: {Endpoint}");

                while (!_cts.IsCancellationRequested)
                {
                    var acceptTask = _listener.AcceptTcpClientAsync();
                    var completedTask = await Task.WhenAny(acceptTask, Task.Delay(-1, _cts.Token));
                    
                    if (completedTask == acceptTask)
                    {
                        var client = await acceptTask;
                        _ = HandleClientAsync(client);
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                LogMessage?.Invoke($"端口 {Port} 已被占用，请更换端口");
                Logger.Error(ex, $"端口 {Port} 已被占用");
                throw;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"TCP服务错误: {ex.Message}");
                Logger.Error(ex, "TCP服务错误");
                throw;
            }
            finally
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    _listener?.Stop();
                    LogMessage?.Invoke("TCP服务已停止");
                    Logger.Info("TCP服务已停止");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            ConnectionStatusChanged?.Invoke(true);
            LogMessage?.Invoke($"客户端已连接: {client.Client.RemoteEndPoint}");
            Logger.Info($"客户端已连接: {client.Client.RemoteEndPoint}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    while (!_cts.IsCancellationRequested)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                        if (bytesRead == 0) break;

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogMessage?.Invoke($"收到消息: {message}");
                        Logger.Debug($"收到消息: {message}");
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"客户端处理错误: {ex.Message}");
                Logger.Error(ex, "客户端处理错误");
            }
            finally
            {
                ConnectionStatusChanged?.Invoke(false);
                LogMessage?.Invoke("客户端已断开");
                Logger.Info("客户端已断开");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            
            _cts?.Cancel();
            _isRunning = false;
            LogMessage?.Invoke("TCP服务已停止");
            Logger.Info("TCP服务已停止");
        }
    }
}
