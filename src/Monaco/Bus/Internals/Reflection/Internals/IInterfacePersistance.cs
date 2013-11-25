using System.Collections.Generic;

namespace Monaco.Bus.Internals.Reflection.Internals
{
	public interface IInterfacePersistance
	{
		IDictionary<string, object> Storage { get; }
		object Retrieve(string property);
		void Store(string property, object value);
		void Clear();
	}
}