using System;
using System.Net.Sockets;

namespace TestService.TelnetServer
{
	public sealed class ClientInfo
	{
		readonly Guid _clientID = Guid.NewGuid();
		readonly string _clientIP;
		readonly Socket _clientSocket;
		readonly byte[] _buffer;

		public Guid ClientID {
			get => _clientID;
		}

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
