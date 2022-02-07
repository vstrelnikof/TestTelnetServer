using System.Net.Sockets;

namespace TestService.TelnetServer
{
	public sealed class ClientInfo
	{
		private readonly string _clientIP;
		private readonly Socket _clientSocket;
		private readonly byte[] _buffer;

		public string ClientIP {
			get => _clientIP;
		}

		public Socket ClientSocket {
			get => _clientSocket;
		}

		public byte[] Buffer {
			get => _buffer;
		}

		public ClientInfo(string ip, Socket clientSocket, byte[] buffer) {
			_clientIP = ip;
			_clientSocket = clientSocket;
			_buffer = buffer;
		}
	}
}
