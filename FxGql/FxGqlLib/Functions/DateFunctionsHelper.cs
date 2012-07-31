using System;

namespace FxGqlLib
{
	public enum DatePartType
	{
		//year
		//yy , yyyy
		//quarter
		//qq , q
		//month
		//mm , m
		//dayofyear
		//dy , y
		//day
		//dd , d
		//week
		//wk , ww
		//weekday
		//dw , w
		//hour
		//hh
		//minute
		//mi , n
		//second
		//ss , s
		//millisecond
		//ms
		//microsecond
		//mcs
		//nanosecond
		//ns
		Year,
		Month,
		Day,
		Hour,
		Minute,
		Second,
		Millisecond
	}

	public static class DatePartHelper
	{
		public static DatePartType Parse (string value)
		{
			switch (value.ToLower ()) {
			case "year":
			case "yy":
			case "yyyy":
				return DatePartType.Year;
			//quarter
			//qq , q
			case "month":
			case "mm":
			case "m":
				return DatePartType.Month;
			//dayofyear
			//dy , y
			case "day":
			case "dd":
			case "d":
				return DatePartType.Day;
			//week
			//wk , ww
			//weekday
			//dw , w
			case "hour":
			case "hh":
				return DatePartType.Hour;
			case "minute":
			case "mi":
			case "n":
				return DatePartType.Minute;
			case "second":
			case "ss":
			case "s":
				return DatePartType.Second;
			case "millisecond":
			case "ms":
				return DatePartType.Millisecond;
			//microsecond
			//mcs
			//nanosecond
			//ns
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", value));
			}
		}

		public static DateTime Add (DatePartType datePart, int add, DateTime dt)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt.AddYears (add);
			case DatePartType.Month:
				return dt.AddMonths (add);
			case DatePartType.Day:
				return dt.AddDays (add);
			case DatePartType.Hour:
				return dt.AddHours (add);
			case DatePartType.Minute:
				return dt.AddMinutes (add);
			case DatePartType.Second:
				return dt.AddSeconds (add);
			case DatePartType.Millisecond:
				return dt.AddMilliseconds (add);
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}

		public static int Get (DatePartType datePart, DateTime dt)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt.Year;
			case DatePartType.Month:
				return dt.Month;
			case DatePartType.Day:
				return dt.Day;
			case DatePartType.Hour:
				return dt.Hour;
			case DatePartType.Minute:
				return dt.Minute;
			case DatePartType.Second:
				return dt.Second;
			case DatePartType.Millisecond:
				return dt.Millisecond;
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}

		public static int Diff (DatePartType datePart, DateTime dt1, DateTime dt2)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt2.Year - dt1.Year;
			case DatePartType.Month:
				return (dt2.Year - dt1.Year) * 12 + dt2.Month - dt1.Month;
			case DatePartType.Day:
				return (dt2.Date - dt1.Date).Days;
			case DatePartType.Hour:
				return (int)(dt2.Ticks / TimeSpan.TicksPerHour - dt1.Ticks / TimeSpan.TicksPerHour);
			case DatePartType.Minute:
				return (int)(dt2.Ticks / TimeSpan.TicksPerMinute - dt1.Ticks / TimeSpan.TicksPerMinute);
			case DatePartType.Second:
				return (int)(dt2.Ticks / TimeSpan.TicksPerSecond - dt1.Ticks / TimeSpan.TicksPerSecond);
			case DatePartType.Millisecond:
				return (int)(dt2.Ticks / TimeSpan.TicksPerMillisecond - dt1.Ticks / TimeSpan.TicksPerMillisecond);
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}
	}
}