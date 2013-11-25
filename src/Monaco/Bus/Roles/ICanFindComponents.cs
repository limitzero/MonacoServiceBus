using System;
using System.Collections.Generic;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role that allows for resolving of registered components.
	/// </summary>
	public interface ICanFindComponents
	{
		TComponent Find<TComponent>();
		//TComponent Find<TComponent>(string key);
		object Find(Type component);
		//object Find(Type component, string key);
		ICollection<TComponent> FindAll<TComponent>();
	}
}