using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Monaco.Bus.Internals.Collections
{
	public class ThreadSafeList<TITEM> : IThreadSafeList<TITEM>
	{
		private static readonly object _padLock = new object();
		private readonly List<TITEM> _list;

		public ThreadSafeList()
		{
			_list = new List<TITEM>();
		}

		#region IThreadSafeList<TITEM> Members

		public void AddUnique(TITEM item)
		{
			lock (_padLock)
			{
				if (_list.Contains(item) == false)
				{
					try
					{
						_list.Add(item);
					}
					catch
					{
					}
				}
			}
		}

		public ReadOnlyCollection<TITEM> Instance
		{
			get
			{
				lock (_padLock)
				{
					return _list.AsReadOnly();
				}
			}
		}

		public IEnumerator<TITEM> GetEnumerator()
		{
			lock (_padLock)
			{
				return _list.GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			lock (_padLock)
			{
				return _list.GetEnumerator();
			}
		}

		public void Add(TITEM item)
		{
			lock (_padLock)
			{
				_list.Add(item);
			}
		}

		public void Clear()
		{
			lock (_padLock)
			{
				_list.Clear();
			}
		}

		public bool Contains(TITEM item)
		{
			lock (_padLock)
			{
				return _list.Contains(item);
			}
		}

		public void CopyTo(TITEM[] array, int arrayIndex)
		{
			lock (_padLock)
			{
				_list.CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(TITEM item)
		{
			lock (_padLock)
			{
				return _list.Remove(item);
			}
		}

		public int Count
		{
			get
			{
				lock (_padLock)
				{
					return _list.Count;
				}
			}
		}

		public bool IsReadOnly
		{
			get
			{
				lock (_padLock)
				{
					return ((IList<TITEM>) _list).IsReadOnly;
				}
			}
		}

		public int IndexOf(TITEM item)
		{
			lock (_padLock)
			{
				return (_list.IndexOf(item));
			}
		}

		public void Insert(int index, TITEM item)
		{
			lock (_padLock)
			{
				_list.Insert(index, item);
			}
		}

		public void RemoveAt(int index)
		{
			lock (_padLock)
			{
				_list.RemoveAt(index);
			}
		}

		public TITEM this[int index]
		{
			get
			{
				lock (_padLock)
				{
					return _list[index];
				}
			}

			set
			{
				lock (_padLock)
				{
					_list[index] = value;
				}
			}
		}

		#endregion
	}
}