using System;

namespace Monaco.Extensions
{
	public static class TimeExtensions
	{
		/// <summary>
		/// This will create a timespan object from the interval "hh:mm:ss"
		/// (hours, minutes, seconds)
		/// </summary>
		/// <example>
		/// TimeSpan.CreateFromInterval("00:10:00");
		/// </example>
		/// <param name="value">Current TimeSpan object</param>
		/// <param name="interval">Current interval to create for TimeSpan</param>
		/// <returns></returns>
		public static TimeSpan? CreateFromInterval(this TimeSpan value, string interval)
		{
			TimeSpan? theTimeSpan = null;

			try
			{
				string[] theParts = interval.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (theParts.Length != 4) return theTimeSpan;

				int theDays = 0;
				Int32.TryParse(theParts[0], out theDays);

				int theHours = 0;
				Int32.TryParse(theParts[1], out theHours);

				int theMinutes = 0;
				Int32.TryParse(theParts[2], out theMinutes);

				int theSeconds = 0;
				Int32.TryParse(theParts[3], out theSeconds);

				theTimeSpan = new TimeSpan(theDays,  theHours, theMinutes, theSeconds);
			}
			catch
			{
			}

			return theTimeSpan;
		}

		/// <summary>
		/// Extension: Creates a <seealso cref="TimeSpan"/> instance based on the current offset of the numerical
		/// value for days from the current date/time.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TimeIncrement Days(this int value)
		{
			return new TimeIncrement(value, TimeInterval.Days);
		}

		/// <summary>
		/// Extension: Creates a <seealso cref="TimeSpan"/> instance based on the current offset of the numerical
		/// value for hours from the current date/time.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TimeIncrement Hours(this int value)
		{
			return new TimeIncrement(value, TimeInterval.Hours);
		}

		/// <summary>
		/// Extension: Creates a <seealso cref="TimeSpan"/> instance based on the current offset of the numerical
		/// value for minutes from the current date/time.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TimeIncrement Minutes(this int value)
		{
			return new TimeIncrement(value, TimeInterval.Minutes);
		}

		/// <summary>
		/// Extension: Creates a <seealso cref="TimeSpan"/> instance based on the current offset of the numerical
		/// value for seconds from the current date/time.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TimeIncrement Seconds(this int value)
		{
			return new TimeIncrement(value, TimeInterval.Seconds);
		}

		public static string ToInterval(this TimeSpan timeSpan)
		{
			const string interval = "{0}:{1}:{2}:{3}";

			string days = timeSpan.Days < 9 ? string.Concat("0", timeSpan.Days.ToString()) : timeSpan.Days.ToString();
			string hours = timeSpan.Hours < 9 ? string.Concat("0", timeSpan.Hours.ToString()) : timeSpan.Hours.ToString();
			string minutes = timeSpan.Minutes < 9 ? string.Concat("0", timeSpan.Minutes.ToString()) : timeSpan.Minutes.ToString();
			string seconds = timeSpan.Seconds < 9 ? string.Concat("0", timeSpan.Seconds.ToString()) : timeSpan.Seconds.ToString();

			return string.Format(interval, days, hours, minutes, seconds);
		}
	}

	public enum TimeInterval
	{
		Days,
		Hours,
		Minutes,
		Seconds,
		Milliseconds
	}

	public class TimeIncrement
	{
		private readonly TimeInterval _timeInterval;
		private readonly int _value;

		public TimeIncrement(int value, TimeInterval timeInterval)
		{
			_value = value;
			_timeInterval = timeInterval;
		}

		public TimeSpan FromNow()
		{
			TimeSpan? nillableTimespan = null;

			switch (_timeInterval)
			{
				case TimeInterval.Days:
					nillableTimespan = TimeSpan.FromDays(_value);
					break;

				case TimeInterval.Hours:
					nillableTimespan = TimeSpan.FromHours(_value);
					break;

				case TimeInterval.Minutes:
					nillableTimespan = TimeSpan.FromMinutes(_value);
					break;

				case TimeInterval.Seconds:
					nillableTimespan = TimeSpan.FromSeconds(_value);
					break;
			}

			return nillableTimespan.Value;
		}
	}
}