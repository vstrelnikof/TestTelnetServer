using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using TestService.TelnetServer;

namespace TestTelnetServer.Commands
{
	class ListCommand : ITelnetCommand
	{
		readonly ConcurrentDictionary<ConnectedClient, int> _clientSums;

		public string CommandName {
			get => "list";
		}

		public string Description {
			get => "List entered sums.";
		}

		public IEnumerable<CommandParameter> Parameters {
			get => null;
		}

		public ListCommand(ICollection<KeyValuePair<ConnectedClient, int>> storage) {
			_clientSums = (ConcurrentDictionary<ConnectedClient, int>)storage;
		}

		public bool IsMatch(string spec) => spec == CommandName;

		public TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem) {
			if (_clientSums.Count == 0) {
				return TelnetCommandResult.Success("No data available!");
			}
			var sb = new StringBuilder();
			foreach (var item in _clientSums) {
				sb.AppendFormat("{0} [{1}]: {2}\r\n", item.Key.Client.ClientIP, item.Key.Client.ClientID, item.Value);
			}
			return TelnetCommandResult.Success(sb.ToString());
		}

		public void ClearCache(ClientInfo client) { }
	}
}
