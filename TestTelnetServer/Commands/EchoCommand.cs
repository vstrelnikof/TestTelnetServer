using System.Collections.Generic;
using TestService.TelnetServer;

namespace TestTelnetServer.Commands
{
	class EchoCommand : ITelnetCommand {
		readonly CommandParameter[] _parameters = new CommandParameter[] {
			new CommandParameter("message", true, "Your message.")
		};

		public string CommandName {
			get => "echo";
		}

		public string Description {
			get => "Echos the entered message.";
		}

		public IEnumerable<CommandParameter> Parameters {
			get => _parameters;
		}

		public bool IsMatch(string spec) => spec == CommandName;

		public TelnetCommandResult Execute(ReceivedCommandItem receivedCommandItem) {
			Dictionary<string, string> parameters = receivedCommandItem.Parameters;
			if (parameters.ContainsKey("message")) {
				string enteredMessage = parameters["message"];
				return TelnetCommandResult.Success($"You entered: {enteredMessage}");
			}
			return TelnetCommandResult.Fail("Please enter message parameter!");
		}

		public void ClearCache(ClientInfo client) { }
	}
}
