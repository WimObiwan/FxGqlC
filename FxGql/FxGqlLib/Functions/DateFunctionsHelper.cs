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
		Quarter,
		Month,
		DayOfYear,
		Day,
		DayOfWeek,
		Hour,
		Minute,
		Second,
		MilliSecond,
		MicroSecond,
		NanoSecond
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
			case "quarter":
			case "qq":
			case "q":
				return DatePartType.Quarter;
			case "month":
			case "mm":
			case "m":
				return DatePartType.Month;
			case "dayofyear":
			case "dy":
			case "y":
				return DatePartType.DayOfYear;
			case "day":
			case "dd":
			case "d":
				return DatePartType.Day;
			//week
			//wk , ww
			case "weekday":
			case "dw":
			case "w":
				return DatePartType.DayOfWeek;
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
				return DatePartType.MilliSecond;
			case "microsecond":
			case "mcs":
				return DatePartType.MicroSecond;
			case "nanosecond":
			case "ns":
				return DatePartType.NanoSecond;
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", value));
			}
		}

		public static DateTime Add (DatePartType datePart, long add, DateTime dt)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt.AddYears ((int)add);
			case DatePartType.Quarter:
				return dt.AddMonths ((int)add * 3);
			case DatePartType.Month:
				return dt.AddMonths ((int)add);
			case DatePartType.Day:
			case DatePartType.DayOfYear:
			case DatePartType.DayOfWeek:
				return dt.AddDays (add);
			case DatePartType.Hour:
				return dt.AddHours (add);
			case DatePartType.Minute:
				return dt.AddMinutes (add);
			case DatePartType.Second:
				return dt.AddSeconds (add);
			case DatePartType.MilliSecond:
				return dt.AddMilliseconds (add);
			case DatePartType.MicroSecond:
				return dt.AddTicks (add * TimeSpan.TicksPerMillisecond / 1000);
			case DatePartType.NanoSecond:
				return dt.AddTicks (add * TimeSpan.TicksPerMillisecond / 1000000);
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}

		public static long Get (DatePartType datePart, DateTime dt)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt.Year;
			case DatePartType.Quarter:
				return (dt.Month - 1) / 3 + 1;
			case DatePartType.Month:
				return dt.Month;
			case DatePartType.DayOfYear:
				return dt.DayOfYear;
			case DatePartType.Day:
				return dt.Day;
			case DatePartType.DayOfWeek:
				return (int)dt.DayOfWeek + 1;
			case DatePartType.Hour:
				return dt.Hour;
			case DatePartType.Minute:
				return dt.Minute;
			case DatePartType.Second:
				return dt.Second;
			case DatePartType.MilliSecond:
				return dt.Millisecond;
			case DatePartType.MicroSecond:
				return (dt.Ticks % TimeSpan.TicksPerSecond) / (TimeSpan.TicksPerMillisecond / 1000);
			case DatePartType.NanoSecond:
				return (dt.Ticks % TimeSpan.TicksPerSecond) * 1000 / (TimeSpan.TicksPerMillisecond / 1000);
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}

		public static long Diff (DatePartType datePart, DateTime dt1, DateTime dt2)
		{
			switch (datePart) {
			case DatePartType.Year:
				return dt2.Year - dt1.Year;
			case DatePartType.Quarter:
				return (dt2.Year - dt1.Year) * 4 + (dt2.Month - 1) / 3 - (dt1.Month - 1) / 3;
			case DatePartType.Month:
				return (dt2.Year - dt1.Year) * 12 + dt2.Month - dt1.Month;
			case DatePartType.Day:
			case DatePartType.DayOfYear:
			case DatePartType.DayOfWeek:
				return (dt2.Date - dt1.Date).Days;
			case DatePartType.Hour:
				return dt2.Ticks / TimeSpan.TicksPerHour - dt1.Ticks / TimeSpan.TicksPerHour;
			case DatePartType.Minute:
				return dt2.Ticks / TimeSpan.TicksPerMinute - dt1.Ticks / TimeSpan.TicksPerMinute;
			case DatePartType.Second:
				return dt2.Ticks / TimeSpan.TicksPerSecond - dt1.Ticks / TimeSpan.TicksPerSecond;
			case DatePartType.MilliSecond:
				return dt2.Ticks / TimeSpan.TicksPerMillisecond - dt1.Ticks / TimeSpan.TicksPerMillisecond;
			case DatePartType.MicroSecond:
				return dt2.Ticks / TimeSpan.TicksPerMillisecond / 1000 - dt1.Ticks / TimeSpan.TicksPerMillisecond / 1000;
			case DatePartType.NanoSecond:
				return dt2.Ticks / TimeSpan.TicksPerMillisecond / 1000000 - dt1.Ticks / TimeSpan.TicksPerMillisecond / 1000000;
			default:
				throw new InvalidOperationException (string.Format ("Unknown DatePart type '{0}'", datePart));
			}
		}
	}
}