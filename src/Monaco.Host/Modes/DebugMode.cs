using System;
using System.Collections.Specialized;

namespace Monaco.Host.Modes
{
	public class DebugMode : IRunMode
	{
		public void Execute(StringDictionary commands)
		{
			var host = new MonacoServiceBusHost();
			host.SetArguements(commands);

			try
			{
				host.Start(new string[0]);
				System.Console.WriteLine("Press any key to exit the host:");
				Console.ReadKey();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception.ToString());
				Console.ReadKey();
			}
			finally
			{
				host.Stop();
			}

		}
	}
}