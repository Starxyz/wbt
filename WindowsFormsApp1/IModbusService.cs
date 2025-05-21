using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public interface IModbusService
    {
        bool IsConnected { get; }
        event System.Action<bool> ConnectionStatusChanged; // True for connected, false for disconnected
        event System.Action<string> LogMessageRequested; // Requests a message to be logged
        Task ConnectAsync();
        void Disconnect();
        Task<string> GetWeightAsync();
        void Dispose(); // For cleaning up resources like the timer
    }
}
