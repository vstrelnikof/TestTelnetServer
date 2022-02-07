namespace TestService.TelnetServer
{
	public struct TelnetCommandResult
	{
		public bool IsSucceeded {
			get;
			private set;
		}

		public string? ResponseText {
			get;
			private set;
		}
				
		public TelnetCommandResult(bool isSucceeded, string? responseText) {
			IsSucceeded = isSucceeded;
			ResponseText = responseText;
		}

		public static TelnetCommandResult Success(string? responseText = null) =>
			new TelnetCommandResult(true, responseText);

		public static TelnetCommandResult Fail(string? responseText = null) =>
			new TelnetCommandResult(false, responseText);
	}
}
