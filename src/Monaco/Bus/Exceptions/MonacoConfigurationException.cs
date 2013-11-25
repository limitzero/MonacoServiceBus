using System;

namespace Monaco.Bus.Exceptions
{
	public class MonacoConfigurationException : ApplicationException
	{
		public MonacoConfigurationException(string description)
			: base(description)
		{
		}

		public MonacoConfigurationException(string description, Exception inner)
			: base(description, inner)
		{
		}
	}
}