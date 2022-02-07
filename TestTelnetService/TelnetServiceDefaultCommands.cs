using System.Collections.Generic;

namespace TestService.TelnetServer
{
	partial class TelnetService
	{
		class ExitCommand: ITelnetCommand
		{
			public string CommandName {
				get => "exit";
			}

			public string Description {
				get => "Closes the terminal connection.";
			}

			public IEnumerable<CommandParameter>? Parameters {
				get => null;
			}

			public bool IsMatch(string? spec) => spec == CommandName;

			public TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem) =>
				TelnetCommandResult.Success();

			public void ClearCache(ClientInfo client) { }
		}
	}
}
