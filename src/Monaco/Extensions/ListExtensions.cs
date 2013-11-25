using System;
using System.Collections.Generic;
using System.Text;

namespace Monaco.Extensions
{
	public static class ListExtensions
	{
		/// <summary>
		/// Extension: This will add a unique item to a enumerable list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="item"></param>
		public static void AddUnique<T>(this List<T> list, T item)
		{
			if (list.Contains(item) == false)
			{
				list.Add(item);
			}
		}

		/// <summary>
		/// Extension: This will add a range of enumerable items to an existing list checking for 
		/// uniqueness for items in the underlying list before adding the element.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="items"></param>
		public static void AddRangeDistinct<T>(this List<T> list, IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				list.AddUnique(item);
			}
		}

		/// <summary>
		/// Extension: Returns a comma separate list of the names of the items in the list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		public static string ToItemList<T>(this IEnumerable<T> list)
		{
			var builder = new StringBuilder();

			foreach (T item in list)
			{
				string name = string.Empty;

				if (item is Type)
					name = (item as Type).Name;
				else
				{
					name = item.GetType().Name;
				}

				builder.Append(name).Append(",");
			}

			return builder.ToString().TrimEnd(new[] {','});
		}

		/// <summary>
		/// Extension: This will attempt to add a key value pair to a dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="K"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="item"></param>
		/// <returns>bool: true if add operation is successful</returns>
		public static bool TryAddValue<T, K>(this IDictionary<T, K> dictionary, T key, K item)
		{
			bool isAdded = false;

			if (dictionary.ContainsKey(key) == false)
			{
				dictionary.Add(key, item);
				isAdded = true;
			}

			return isAdded;
		}


	}
}