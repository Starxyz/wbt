using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public interface IApplicationOrchestrator
    {
        event System.Action<string> LogMessageRequested; // For general logging
        event System.Action<string> DetailedMatchFailureLogRequested; // For specific match failure details
        event System.Action<string> WeightAvailable;

        Task ProcessIncomingMessageAsync(string rawMessage);
    }
}
