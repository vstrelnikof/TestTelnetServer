using System.Text;

namespace TestService.TelnetServer
{
	public class ConnectedClient
	{
		public ClientInfo? Client;
		public string? CommandBuffer = string.Empty;
		public Encoding? TextEncoder;
	}
}
