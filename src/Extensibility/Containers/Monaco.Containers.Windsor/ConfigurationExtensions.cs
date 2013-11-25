using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Monaco.Containers.Windsor;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static IContainerConfiguration UsingWindsor(this IContainerConfiguration configuration)
		{
			configuration.Container = new WindsorContainerAdapter();
			return configuration;
		}

	}
}
