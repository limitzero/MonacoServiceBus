using System.Collections.Generic;

namespace Monaco.Testing.Internals.Interceptors
{
	public interface IInterfacePersistance
	{
		IDictionary<string, object> Storage { get; }
		object Retrieve(string property);
		void Store(string property, object value);
		void Clear();
	}
}