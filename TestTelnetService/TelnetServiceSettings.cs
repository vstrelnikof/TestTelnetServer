namespace TestService.TelnetServer
{
	public class TelnetServiceSettings
	{
		public bool ListenAllAdapters {
			get;
			set;
		} = true;

		public string? LocalIPAddress {
			get;
			set;
		}

		public int PortNumber {
			get;
			set;
		}

		//US-ASCII
		public int Charset {
			get;
			set;
		} = 20127;

		public string PromtText {
			get;
			set;
		} = "TelnetService";

		public TelnetServiceSettings Clone() =>
			(TelnetServiceSettings)MemberwiseClone();
	}
}
