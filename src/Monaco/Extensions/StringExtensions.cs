using System;

namespace Monaco.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// Extension: Converts the current string representation of a uri to a <seealso cref="Uri"/>
		/// instance.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static Uri ToUri(this string item)
		{
			return new Uri(item);
		}
	}
}