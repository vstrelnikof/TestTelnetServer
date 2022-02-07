using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TestService.TelnetServer
{
	public sealed class TCPServer: ITCPServer
	{
		private Socket? _serverSocket;
		private List<ClientInfo>? _clients;

		public List<ClientInfo>? Clients {
			get => _clients;
		}

		public int MaxWaitingClients {
			get;
			set;
		} = 100;

		public int ReadBufferSize {
			get;
			set;
		} = 1024;

		public event ClientConnectionEventHandler? ClientConnected;
		public event ClientConnectionEventHandler? ClientDisconnected;
		public event ClientDataEventHandler? DataReceived;

		public void StartListening(IPEndPoint endPoint) {
			_clients = new List<ClientInfo>();
			_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			try {
				_serverSocket.Bind(endPoint);
				_serverSocket.Listen(MaxWaitingClients);
				_serverSocket.BeginAccept(new AsyncCallback(_acceptCallback), _serverSocket);
			} catch {
				StopListening();
				throw;
			}
		}

		public void StopListening() {
			if (_serverSocket == null) {
				return;
			}

			try {
				_serverSocket.Close();
			} catch { }

			_serverSocket = null;

			while (_clients?.Count > 0) {
				ClientInfo client = _clients[0];

				try {
					client.ClientSocket.Close();
				} catch { }

				_clients.RemoveAt(0);
			}
		}

		private void _acceptCallback(IAsyncResult ar) {
			try {
				if (_serverSocket == null) {
					return;
				}

				Socket clientSocket = _serverSocket.EndAccept(ar);

				if (clientSocket == null) {
					return;
				}

				IPEndPoint remoteIpEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
				var clientInfo = new ClientInfo(remoteIpEndPoint.Address.ToString(), clientSocket, new byte[ReadBufferSize]);

				ClientConnected?.Invoke(this, clientInfo);
				clientSocket.BeginReceive(clientInfo.Buffer, 0, ReadBufferSize, 0, new AsyncCallback(_readCallback), clientInfo);                
				_serverSocket.BeginAccept(new AsyncCallback(_acceptCallback), _serverSocket);
			} catch { }
		}

		private void _readCallback(IAsyncResult ar)
		{
			try {
				var clientInfo = (ClientInfo)ar.AsyncState;
				Socket clientSocket = clientInfo.ClientSocket;

				int bytesRead = clientSocket.Connected ? clientSocket.EndReceive(ar) : 0;

				if (bytesRead > 0) {
					var data = new byte[bytesRead];
					Array.Copy(clientInfo.Buffer, 0, data, 0, bytesRead);

					DataReceived?.Invoke(this, clientInfo, data);

					try {
						clientSocket.BeginReceive(clientInfo.Buffer, 0, ReadBufferSize, 0, new AsyncCallback(_readCallback), clientInfo);
					} catch { }
				} else {
					ClientDisconnected?.Invoke(this, clientInfo);
				}
			} catch(ObjectDisposedException e) {
				Console.WriteLine($"ObjectDisposedException occurred: {e.Message}");
			} catch(Exception e) {
				Console.WriteLine($"Exception occurred: {e.Message}");
			}
		}
	}
}
