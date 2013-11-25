using System.Collections.Generic;

namespace Monaco.Bus.Internals.Reflection.Internals.Impl
{
	public class InterfacePersistance : IInterfacePersistance
	{
		public InterfacePersistance()
		{
			Storage = new Dictionary<string, object>();
		}

		#region IInterfacePersistance Members

		public IDictionary<string, object> Storage { get; private set; }

		public object Retrieve(string property)
		{
			object value = null;

			if (Storage.ContainsKey(property))
			{
				value = Storage[property];
			}

			return value;
		}

		public void Store(string property, object value)
		{
			if (Storage.ContainsKey(property))
			{
				Storage.Remove(property);
			}
			Storage.Add(property, value);
		}

		public void Clear()
		{
			Storage.Clear();
		}

		#endregion
	}
}