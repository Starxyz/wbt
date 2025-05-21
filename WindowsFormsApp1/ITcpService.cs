using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public interface ITcpService
    {
        bool IsRunning { get; }
        event System.Action<string> MessageReceived; // Event for when a complete message is received from a client
        event System.Action<bool, string> ServerStatusChanged; // bool isRunning, string endpointInfo (e.g., "IP:Port" or "Not running")
        event System.Action<bool, string> ClientConnectionChanged; // bool isConnected, string clientInfo (e.g., "IP:Port of client" or "Disconnected")
        event System.Action<string> LogMessageRequested; // Requests a message to be logged by the main form

        Task StartAsync();
        void Stop();
        void Dispose(); // For cleaning up the TcpServer
    }
}
