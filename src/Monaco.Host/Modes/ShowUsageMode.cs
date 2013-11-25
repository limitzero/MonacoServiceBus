using System;
using System.Collections.Specialized;
using System.IO;

namespace Monaco.Host.Modes
{
	public class ShowUsageMode : IRunMode
	{
		public void Execute(StringDictionary commands)
		{
			try
			{
				Console.Title = "Monaco Service Host Help";

				Stream stream = typeof(MonacoServiceBusHost).Assembly
					.GetManifestResourceStream("Monaco.Host.Content.Help.txt");

				if (stream != null)
				{
					using (TextReader reader = new StreamReader(stream))
					{
						string usage = reader.ReadToEnd();
						Console.WriteLine(usage);
						Console.Read();
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				Console.ReadKey();
			}
		}
	}
}