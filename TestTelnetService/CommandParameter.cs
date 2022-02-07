namespace TestService.TelnetServer
{
	public struct CommandParameter
	{
		public string Name {
			get;
			private set;
		}

		public bool IsRequired {
			get;
			private set;
		}

		public string Description {
			get;
			private set;
		}

		public CommandParameter(string name, bool isRequired, string description) {
			Name = name;
			IsRequired = isRequired;
			Description = description;
		}
	}
}
