using System.Collections.Generic;

namespace TestService.TelnetServer
{
	public class ReceivedCommandItem
	{
		public ConnectedClient? Client {
			get;
			set;
		}

		public string? InputCommand {
			get;
			set;
		}

		public string? CommandName {
			get;
			set;
		}

		public bool IsSucceed {
			get;
			set;
		}

		public string? ResponseMessage {
			get;
			set;
		}

		public Dictionary<string, string?>? Parameters {
			get;
			set;
		}
	}
}
