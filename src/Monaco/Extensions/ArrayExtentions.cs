using System;
using System.Collections.Generic;
using System.Linq;

namespace Monaco.Extensions
{
	public static class ArrayExtentions
	{
		public static List<object> ToList(this Array array)
		{
			var list = new List<object>();

			foreach (object item in array)
			{
				list.Add(item);
			}

			return list;
		}

		public static HashSet<object> ToHashSet(this Array array)
		{
			var list = new HashSet<object>();

			foreach (object item in array)
			{
				object type = (from match in list
				               where match.GetType() == item.GetType()
				               select match).FirstOrDefault();

				if (type == null)
				{
					list.Add(item);
				}
			}

			return list;
		}

		public static HashSet<T> ToHashSet<T>(this Array array)
		{
			var list = new HashSet<T>();

			foreach (object item in array)
			{
				T type = (from match in list
				          where match.GetType() == item.GetType()
				          select match).FirstOrDefault();

				if (type == null)
				{
					try
					{
						list.Add((T) item);
					}
					catch
					{
					}
				}
			}

			return list;
		}
	}
}