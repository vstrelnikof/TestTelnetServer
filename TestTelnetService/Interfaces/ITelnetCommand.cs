using System.Collections.Generic;

namespace TestService.TelnetServer
{
	public interface ITelnetCommand
	{
		string CommandName {
			get;
		}

		string Description {
			get;
		}

		IEnumerable<CommandParameter>? Parameters {
			get;
		}

		bool IsMatch(string? spec);

		TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem);

		void ClearCache(ClientInfo client);
	}
}
