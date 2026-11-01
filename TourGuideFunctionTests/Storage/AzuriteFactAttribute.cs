using System.Net.Sockets;

namespace TourGuideFunctionTests.Storage
{
    public sealed class AzuriteFactAttribute : FactAttribute
    {
        private static readonly Lazy<bool> TablesEndpointUp = new(() =>
        {
            try
            {
                using var client = new TcpClient();
                return client.ConnectAsync("127.0.0.1", 10002).Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception)
            {
                return false;
            }
        });

        public AzuriteFactAttribute()
        {
            if (!TablesEndpointUp.Value)
                Skip = "Azurite table endpoint (127.0.0.1:10002) is not running.";
        }
    }
}
