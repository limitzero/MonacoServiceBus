using System.Collections;
using System.Collections.Generic;

namespace Monaco.Bus.Internals.Collections
{
	public class ThreadSafeDictionary<TKEY, TVALUE> : IThreadSafeDictionary<TKEY, TVALUE>
	{
		private readonly IDictionary<TKEY, TVALUE> _dictionary = new Dictionary<TKEY, TVALUE>();
		private readonly object _padLock = new object();

		#region IThreadSafeDictionary<TKEY,TVALUE> Members

		public IEnumerator<KeyValuePair<TKEY, TVALUE>> GetEnumerator()
		{
			lock (_padLock)
			{
				return _dictionary.GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			lock (_padLock)
			{
				return _dictionary.GetEnumerator();
			}
		}

		public void Add(KeyValuePair<TKEY, TVALUE> item)
		{
			lock (_padLock)
			{
				_dictionary.Add(item);
			}
		}

		public void Clear()
		{
			lock (_padLock)
			{
				_dictionary.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKEY, TVALUE> item)
		{
			lock (_padLock)
			{
				return _dictionary.Contains(item);
			}
		}

		public void CopyTo(KeyValuePair<TKEY, TVALUE>[] array, int arrayIndex)
		{
			lock (_padLock)
			{
				_dictionary.CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(KeyValuePair<TKEY, TVALUE> item)
		{
			lock (_padLock)
			{
				return _dictionary.Remove(item);
			}
		}

		public int Count
		{
			get
			{
				lock (_padLock)
				{
					return _dictionary.Count;
				}
			}
		}

		public bool IsReadOnly
		{
			get
			{
				lock (_padLock)
				{
					return _dictionary.IsReadOnly;
				}
			}
		}

		public bool ContainsKey(TKEY key)
		{
			lock (_padLock)
			{
				return _dictionary.ContainsKey(key);
			}
		}

		public void Add(TKEY key, TVALUE value)
		{
			lock (_padLock)
			{
				_dictionary.Add(key, value);
			}
		}

		public bool Remove(TKEY key)
		{
			lock (_padLock)
			{
				return _dictionary.Remove(key);
			}
		}

		public bool TryGetValue(TKEY key, out TVALUE value)
		{
			lock (_padLock)
			{
				return _dictionary.TryGetValue(key, out value);
			}
		}

		public TVALUE this[TKEY key]
		{
			get
			{
				lock (_padLock)
				{
					return _dictionary[key];
				}
			}

			set
			{
				lock (_padLock)
				{
					_dictionary[key] = value;
				}
			}
		}

		public ICollection<TKEY> Keys
		{
			get
			{
				lock (_padLock)
				{
					return _dictionary.Keys;
				}
			}
		}

		public ICollection<TVALUE> Values
		{
			get
			{
				lock (_padLock)
				{
					return _dictionary.Values;
				}
			}
		}

		#endregion
	}
}