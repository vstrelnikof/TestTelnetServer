using System;
using System.Text;
using System.Collections.Concurrent;
using TestService.TelnetServer;
using TestTelnetServer.Commands;

namespace TestTelnetServer
{
	internal class Program
	{
		static void Main(string[] args) {
			if (args.Length == 0 || !ushort.TryParse(args[0], out ushort port)) {
				Console.WriteLine("Please specify correct port as an application argument!");
				return;
			}

			var sumsStorage = new ConcurrentDictionary<ConnectedClient, int>();
			var server = new TCPServer();
			var commands = new ITelnetCommand[] {
				new HelloCommand(),
				new EchoCommand(),
				new SumCommand(sumsStorage),
				new ListCommand(sumsStorage)
			};
			var service = new TelnetService(server, commands);

			var settings = new TelnetServiceSettings {
				PromtText = $"TestTelnetServerApp@{Environment.MachineName}",
				PortNumber = port,
				Charset = Encoding.Default.CodePage,
				ListenAllAdapters = true
			};

			service.Start(settings);

			Console.WriteLine("Telnet Service is running.\r\nPress any key to stop application.");
			Console.ReadKey();

			service.Stop();
		}
	}
}
