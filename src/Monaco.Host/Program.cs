using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Monaco.Host.Modes;

namespace Monaco.Host
{

	public class Program
	{
		private static readonly IDictionary<RunModes, IRunMode> _runModes = new Dictionary<RunModes, IRunMode>();

		public static void Main(string[] args)
		{
			InitializeRunModes(); 
			StringDictionary commands = UtilClass.SplitArgString(args);

			var command = GetCommandOption(commands);

			try
			{
				if(string.IsNullOrEmpty(command) || 
					command == RunModes.Help.ToString().ToLower() ||
					command == "?")
				{
					RunInMode(RunModes.Help, commands);
				}
				else if (command == RunModes.Debug.ToString().ToLower())
				{
					RunInMode(RunModes.Debug, commands);
				}
				else if (command == RunModes.Install.ToString().ToLower())
				{
					RunInMode(RunModes.Install, commands);
				}
				else if (command == RunModes.Uninstall.ToString().ToLower())
				{
					RunInMode(RunModes.Uninstall, commands);
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				Console.ReadKey();
			}

		}

		private static void InitializeRunModes()
		{
			_runModes.Add(RunModes.Help, new ShowUsageMode());
			_runModes.Add(RunModes.Debug, new DebugMode());
			_runModes.Add(RunModes.Install, new InstallMode());
			_runModes.Add(RunModes.Uninstall, new UninstallMode());
		}

		private static string GetCommandOption(StringDictionary commands)
		{
			// command should be the first option on the command line
			// (make it "debug" by default):
			var keys = new ArrayList(commands.Keys);

			var command = RunModes.Debug.ToString().ToLower();

			if(keys.Count > 0)
				command = keys[0] as string;

			if (string.IsNullOrEmpty(command))
				command = command.Trim();

			return command.ToLower();
		}

		private static void RunInMode(RunModes runMode, StringDictionary commands)
		{
			_runModes[runMode].Execute(commands);
		}

	}
}