using System.Collections.Generic;
using TestService.TelnetServer;

namespace TestTelnetServer.Commands
{
	class HelloCommand: ITelnetCommand
	{
		public string CommandName {
			get => "hello";
		}

		public string Description {
			get => "Displays hello message.";
		}

		public IEnumerable<CommandParameter> Parameters {
			get => null;
		}

		public bool IsMatch(string spec) => spec == CommandName;

		public TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem) =>
			TelnetCommandResult.Success("Hello from Telnet service!");

		public void ClearCache(ClientInfo client) { }
	}
}
