using NLog;
using System;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class TcpService : ITcpService, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly WindowsFormsApp1.TcpServer _tcpServer; // Assuming this is the existing custom TCP server class
        private bool _tcpRunning = false;
        private readonly int _port;

        public bool IsRunning => _tcpRunning;

        public event Action<string> MessageReceived;
        public event Action<bool, string> ServerStatusChanged;
        public event Action<bool, string> ClientConnectionChanged;
        public event Action<string> LogMessageRequested;

        public TcpService(int port)
        {
            _port = port;
            _tcpServer = new WindowsFormsApp1.TcpServer(_port); // Instantiate the custom TcpServer

            // Subscribe to the internal events of the custom TcpServer
            // Assuming these event names match what was used in Form1.cs or are the actual events of TcpServer
            _tcpServer.LogMessage += InternalLogMessageHandler; 
            _tcpServer.ServerStatusChanged += InternalServerStatusChangedHandler;
            _tcpServer.ConnectionStatusChanged += InternalClientConnectionStatusChangedHandler;
            _tcpServer.MessageReceived += InternalMessageReceivedHandler;
        }

        public async Task StartAsync()
        {
            try
            {
                LogMessageRequested?.Invoke("Attempting to start TCP server...");
                await _tcpServer.StartAsync(); // Assuming StartAsync exists and is awaitable
                // _tcpRunning state and ServerStatusChanged event will be handled by InternalServerStatusChangedHandler
                // Logger.Info($"TCP server started on port {_port}."); // Logged by InternalServerStatusChangedHandler
                // LogMessageRequested?.Invoke($"TCP server successfully started on port {_port}."); // Logged by InternalServerStatusChangedHandler
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to start TCP server on port {_port}.");
                LogMessageRequested?.Invoke($"Error starting TCP server: {ex.Message}");
                _tcpRunning = false; // Ensure state is correct on failure
                // Manually trigger status update if StartAsync fails before _tcpServer can.
                ServerStatusChanged?.Invoke(false, "Error starting"); 
            }
        }

        public void Stop()
        {
            try
            {
                LogMessageRequested?.Invoke("Attempting to stop TCP server...");
                _tcpServer.Stop(); // Assuming Stop exists
                // _tcpRunning state and ServerStatusChanged event will be handled by InternalServerStatusChangedHandler
                // Logger.Info("TCP server stopped."); // Logged by InternalServerStatusChangedHandler
                // LogMessageRequested?.Invoke("TCP server successfully stopped."); // Logged by InternalServerStatusChangedHandler
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to stop TCP server.");
                LogMessageRequested?.Invoke($"Error stopping TCP server: {ex.Message}");
                // Manually trigger status update if Stop fails before _tcpServer can.
                // If it was running, reflect that it might have failed to stop cleanly.
                ServerStatusChanged?.Invoke(_tcpRunning, "Error stopping");
            }
        }

        private void InternalLogMessageHandler(string message)
        {
            LogMessageRequested?.Invoke($"[TcpServerInternal] {message}");
        }

        private void InternalServerStatusChangedHandler(bool started)
        {
            _tcpRunning = started;
            string endpointInfo = "Unknown Endpoint";
            if (started) {
                // Attempt to get endpoint information, similar to Form1
                // This assumes _tcpServer has an Endpoint property or similar
                // If _tcpServer.Endpoint is null or doesn't have ToString(), this needs adjustment
                try {
                     endpointInfo = _tcpServer.Endpoint?.ToString() ?? $"Port {_port}";
                } catch (Exception ex) {
                    Logger.Warn(ex, "Could not retrieve endpoint info from _tcpServer.");
                    endpointInfo = $"Port {_port} (Endpoint info error)";
                }
            } else {
                endpointInfo = "Not running";
            }
            
            ServerStatusChanged?.Invoke(started, endpointInfo);
            LogMessageRequested?.Invoke(started ? $"TCP server is now running on {endpointInfo}" : "TCP server is now stopped.");
            Logger.Info(started ? $"TCP server is now running on {endpointInfo}" : "TCP server is now stopped.");
        }

        private void InternalClientConnectionStatusChangedHandler(bool connected)
        {
            // The actual client IP/info might come from a "CONNECT|" message.
            // This event is more about the raw socket connection status if TcpServer provides it.
            // Form1 will handle specific client identification.
            string clientInfo = connected ? "Client stream connected" : "Client stream disconnected";
            ClientConnectionChanged?.Invoke(connected, clientInfo);
            LogMessageRequested?.Invoke(clientInfo);
            Logger.Info(clientInfo);
        }

        private void InternalMessageReceivedHandler(string message)
        {
            Logger.Debug($"TcpService received raw message: {message}");
            MessageReceived?.Invoke(message);
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
                // Unsubscribe from events to prevent issues if _tcpServer is used elsewhere or for longer
                _tcpServer.LogMessage -= InternalLogMessageHandler;
                _tcpServer.ServerStatusChanged -= InternalServerStatusChangedHandler;
                _tcpServer.ConnectionStatusChanged -= InternalClientConnectionStatusChangedHandler;
                _tcpServer.MessageReceived -= InternalMessageReceivedHandler;

                if (_tcpRunning)
                {
                    _tcpServer.Stop();
                }
                // If _tcpServer implements IDisposable, call its Dispose method.
                // For example: if (_tcpServer is IDisposable disposableTcpServer) disposableTcpServer.Dispose();
                // Assuming WindowsFormsApp1.TcpServer might be IDisposable:
                if (_tcpServer is IDisposable disposableServer)
                {
                    disposableServer.Dispose();
                    Logger.Info("Disposed the internal TcpServer instance.");
                }
                 _tcpRunning = false;
            }
        }
    }
}
