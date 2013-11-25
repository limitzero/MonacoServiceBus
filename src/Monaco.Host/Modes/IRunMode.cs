using System;
using System.Collections.Specialized;

namespace Monaco.Host.Modes
{
	public interface IRunMode
	{
		void Execute(StringDictionary commands);
	}
}