using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using TestService.TelnetServer;

namespace TestTelnetServer.Commands
{
	class SumCommand: ITelnetCommand
	{
		private readonly ConcurrentDictionary<ConnectedClient, int> _clientSums;

		public string CommandName {
			get => "sum";
		}

		public string Description {
			get => "Sum entered values.";
		}

		public IEnumerable<CommandParameter> Parameters {
			get => null;
		}

		public SumCommand(ICollection<KeyValuePair<ConnectedClient, int>> storage) {
			_clientSums = (ConcurrentDictionary<ConnectedClient, int>)storage;
		}

		public bool IsMatch(string spec) =>
			int.TryParse(spec, out _);

		public TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem) {
			_clientSums.TryAdd(receivedCommandItem.Client, 0);
			if (!int.TryParse(receivedCommandItem.CommandName, out int newVal)) {
				return TelnetCommandResult.Fail("Incorrect value!");
			}
			int sum = _clientSums[receivedCommandItem.Client] += newVal;
			return TelnetCommandResult.Success($"Sum: {sum}");
		}

		public void ClearCache(ClientInfo client) {
			ConnectedClient item = _clientSums.FirstOrDefault(c => c.Key.Client == client).Key;
			if (item != null) {
				_clientSums.TryRemove(item, out int _);
			}
		}
	}
}
