using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TestService.TelnetServer
{
	public partial class TelnetService: ITelnetService {
		static readonly Regex TokenizerRegex = new Regex("(\"[^\"]+\")|([^ \":]+)|(:)", RegexOptions.Compiled);
		bool _isRunning;
		readonly ITCPServer _TCPServer;
		Thread? _thrCommandProcessor;
		List<ITelnetCommand>? _commands;
		readonly ConcurrentDictionary<Guid, ConnectedClient> _clients =
			new ConcurrentDictionary<Guid, ConnectedClient>();
		readonly ConcurrentQueue<ReceivedCommandItem> _receivedCommands =
			new ConcurrentQueue<ReceivedCommandItem>();
		TelnetServiceSettings _settings = new TelnetServiceSettings();

		public bool IsRunning {
			get => _isRunning;
		}

		public TelnetServiceSettings Settings {
			get => _settings.Clone();
		}

		private string PromptText {
			get => $"{_settings.PromtText}>";
		}

		public TelnetService(ITCPServer tcpServer, ITelnetCommand[] customCommands) {
			_TCPServer = tcpServer;
			_buildCommandsCatalog(customCommands ?? new ITelnetCommand[0]);
		}

		public bool Start(TelnetServiceSettings settings) {
			if (_isRunning) {
				return true;
			}

			_settings = settings.Clone();

			int portNo = _settings.PortNumber;
			string? ipAddress = null;
			if (!_settings.ListenAllAdapters && !string.IsNullOrEmpty(_settings.LocalIPAddress)) {
				ipAddress = _settings.LocalIPAddress;
			}

			IPAddress address = string.IsNullOrEmpty(ipAddress)
				? IPAddress.Any
				: IPAddress.Parse(ipAddress);
			var endPoint = new IPEndPoint(address, portNo);

			_clearClientsAndReceivedCommands();

			_TCPServer.ClientConnected += _TCPServerClientConnected;
			_TCPServer.ClientDisconnected += _TCPServerClientDisconnected;
			_TCPServer.DataReceived += _TCPServerDataReceived;

			try {
				_TCPServer.StartListening(endPoint);
				_isRunning = true;

				_thrCommandProcessor = new Thread(_commandProcessorThread) {
					Name = "TelnetCommandProcessor"
				};
				_thrCommandProcessor.Start();
			} catch (Exception ex) {
				Console.WriteLine($"TelnetService could not be started: {ex.Message}");
				Stop();
			}

			return true;
		}

		public void Stop() {
			if (!_isRunning) {
				return;
			}

			_isRunning = false;

			_TCPServer.ClientConnected -= _TCPServerClientConnected;
			_TCPServer.ClientDisconnected -= _TCPServerClientDisconnected;
			_TCPServer.DataReceived -= _TCPServerDataReceived;

			_TCPServer.StopListening();

			_clearClientsAndReceivedCommands();
		}

		void _buildCommandsCatalog(ITelnetCommand[] customCommands) {
			_commands = new List<ITelnetCommand>();

			var builtInCommands = Assembly.GetAssembly(typeof(ITelnetCommand)).GetTypes()
				.Where(x => !x.IsInterface && typeof(ITelnetCommand).IsAssignableFrom(x));

			foreach (Type commandType in builtInCommands) {
				_addCommandToCatalog((ITelnetCommand)Activator.CreateInstance(commandType));
			}

			for (int i = 0; i < customCommands.Length; i++) {
				_addCommandToCatalog(customCommands[i]);
			}
		} 
				
		void _addCommandToCatalog(ITelnetCommand command) {
			if (_commands != null && !_commands.Contains(command)) {
				_commands.Add(command);
			}
		}

		private void _clearClientsAndReceivedCommands() {
			while (!_receivedCommands.IsEmpty) {
				_receivedCommands.TryDequeue(out _);
			}
			_clients.Clear();
		}

		void _commandProcessorThread() {
			while (_isRunning) {
				if (_receivedCommands.Count == 0) {
					Thread.Sleep(100);
					continue;
				}

				_receivedCommands.TryDequeue(out ReceivedCommandItem cmd);

				if (cmd == null) {
					continue;
				}

				_processCommandItem(cmd);
			}
		}

		void _processCommandItem(ReceivedCommandItem commandItem) {
			bool parseFailed = false;
			try {
				_parseCommandLine(commandItem);
			} catch (Exception ex) {
				commandItem.ResponseMessage = $"Command parse error: {ex.Message}";
				commandItem.IsSucceed = false;
				parseFailed = true;
			}

			try {
				if (!parseFailed) {
					_executeCommand(commandItem);
				}
			} catch (Exception ex) {
				commandItem.ResponseMessage = $"Command execution error: {ex.Message}";
				commandItem.IsSucceed = false;
			}

			_sendCommandResult(commandItem);
		}

		void _parseCommandLine(ReceivedCommandItem commandItem) {
			var commandStack = new Stack<string>();
			var parameters = new Dictionary<string, string?>();
			Match regExMatch = TokenizerRegex.Match(commandItem.InputCommand);

			bool parameter = false;

			while (regExMatch.Success) {
				string temp = regExMatch.Value.Replace("\"", "").Replace("'", "");
				switch (temp) {
					case ":":
						if (parameter) {
							throw new ArgumentException("Invalid Parameters");
						}
						regExMatch = regExMatch.NextMatch();
						if (regExMatch.Success) {
							parameters.Add(commandStack.Pop(), regExMatch.Value.Replace("\"", "").Replace("'", ""));
						}
						parameter = true;
						break;
					default:
						commandStack.Push(temp);
						parameter = false;
						break;
				}
				regExMatch = regExMatch.NextMatch();
			}

			while (commandStack.Count > 1) {
				parameters.Add(commandStack.Pop(), null);
			}

			commandItem.CommandName = commandStack.Pop().ToLowerInvariant();
			commandItem.Parameters = parameters;
		}

		void _executeCommand(ReceivedCommandItem commandItem) {
			if (commandItem.CommandName == "exit") {
				_processExitCommand(commandItem);
				return;
			}

			commandItem.IsSucceed = false;
			if (_commands == null) {
				commandItem.ResponseMessage = "No commands registered!";
				return;
			}

			ITelnetCommand? cmd = _commands.FirstOrDefault(c => c.IsMatch(commandItem.CommandName));
			if (cmd == null) {
				commandItem.ResponseMessage = "Unknown command.";
				return;
			}

			try {
				TelnetCommandResult? result = cmd?.Execute(commandItem);
				commandItem.IsSucceed = result?.IsSucceeded ?? false;
				commandItem.ResponseMessage = result?.ResponseText;
			} catch (Exception ex) {
				commandItem.IsSucceed = false;
				commandItem.ResponseMessage = $"Error: {ex.Message}";
			}
		}

		void _sendCommandResult(ReceivedCommandItem commandItem) {
			var strResult = new StringBuilder(commandItem.IsSucceed ? "OK:" : "FAILED:");
			if (!string.IsNullOrEmpty(commandItem.ResponseMessage)) {
				strResult.AppendFormat("\r\n{0}", commandItem.ResponseMessage);
			}
			strResult.AppendFormat("\r\n{0}", PromptText);

			try {
				ConnectedClient? client = commandItem.Client;
				if (client?.Client?.ClientSocket.Connected ?? false) {
					client.Client.ClientSocket.Send(client.TextEncoder?.GetBytes(strResult.ToString()));
				}
			} catch { }
		}

		void _processExitCommand(ReceivedCommandItem commandItem) {
			if (commandItem == null) {
				return;
			}
			try {
				ConnectedClient? commandClient = commandItem.Client;
				commandClient?.Client?.ClientSocket.Send(commandClient?.TextEncoder?.GetBytes("OK: Bye!"));
				commandClient?.Client?.ClientSocket.Close();
				commandItem.IsSucceed = true;
			} catch {
				commandItem.IsSucceed = false;
			}
		}

		void _TCPServerClientConnected(ITCPServer server, ClientInfo client) {
			var _connectedClient = new ConnectedClient {
				Client = client,
				CommandBuffer = null,
				TextEncoder = Encoding.GetEncoding(_settings.Charset)
			};
			_clients.TryAdd(client.ClientID, _connectedClient);

			Console.WriteLine($"New client connected: {client.ClientIP} [{client.ClientID}]");

			_connectedClient.Client.ClientSocket.Send(_connectedClient.TextEncoder.GetBytes(" \r\n" + PromptText));
		}

		void _TCPServerClientDisconnected(ITCPServer server, ClientInfo client) {
			if (_clients.TryRemove(client.ClientID, out _)) {
				Console.WriteLine($"Client disconnected: {client.ClientIP}");
			}
			_commands?.ForEach(c => c.ClearCache(client));
		}

		void _TCPServerDataReceived(ITCPServer server, ClientInfo client, byte[] data) {
			if (!_clients.TryGetValue(client.ClientID, out ConnectedClient connectedClient)) {
				return;
			}

			if (data.Length == 0) {
				return;
			}

			//Ignore delete key
			if (data[0] == 127) {
				return;
			}

			//Ignore arrow keys
			if (data.Length == 3) {
				if (data[0] == 27 && data[1] == 91) {
					byte _thirdByte = data[2];

					//65,66,67,68:Up,Down,Right,Left
					if (_thirdByte > 64 && _thirdByte < 69) {
						return;
					}
				}
			}

			int startIndex = 0;
			do {
				//Skip Telnet handshaking data
				if (data[startIndex] == 255) {
					startIndex += 3;
				} else {
					break;
				}
			} while (startIndex <= data.Length - 1);

			if (startIndex > data.Length - 1) {
				return;
			}

			Encoding? clientEncoder = connectedClient.TextEncoder;
			string textData = clientEncoder?.GetString(data, startIndex, data.Length - startIndex) ?? string.Empty;

			//Backspace
			if (textData == "\b") {
				if (connectedClient.CommandBuffer?.Length > 0) {
					connectedClient.CommandBuffer = connectedClient.CommandBuffer.Remove(connectedClient.CommandBuffer.Length - 1);
				}
				connectedClient.Client?.ClientSocket.Send(new byte[] { 0x20, 0x08 });
				return;
			}

			// Add to buffer
			connectedClient.CommandBuffer += textData;

			int endOfLineIndex = connectedClient.CommandBuffer.IndexOf('\r');
			if (endOfLineIndex > -1) {
				do {
					if (endOfLineIndex > 0) {
						string cmd = connectedClient.CommandBuffer
							.Substring(0, endOfLineIndex + 1)
							.Replace("\r", "")
							.Replace("\n", "")
							.Trim();

						if (!string.IsNullOrEmpty(cmd)) {
							ReceivedCommandItem _cmdInfo = new ReceivedCommandItem {
								Client = connectedClient,
								InputCommand = cmd
							};

							_receivedCommands.Enqueue(_cmdInfo);
						}
					}

					connectedClient.CommandBuffer = connectedClient.CommandBuffer[(endOfLineIndex + 1)..];
					endOfLineIndex = connectedClient.CommandBuffer.IndexOf('\r');
				} while (endOfLineIndex > -1);
			}
		}
	}
}
