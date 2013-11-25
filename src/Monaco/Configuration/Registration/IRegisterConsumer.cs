using System;
using System.Collections.Generic;

namespace Monaco.Configuration.Registration
{
	/// <summary>
	/// Role responsible for regsitering message consumers into the underlying component container.
	/// </summary>
	public interface IRegisterConsumer
	{
		IEnumerable<string> Register(object consumer);
		IEnumerable<string> RegisterType(Type consumer);
	}
}