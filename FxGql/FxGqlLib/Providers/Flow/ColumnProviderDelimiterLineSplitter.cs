using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public abstract class ColumnProviderDelimiterLineSplitter
	{
		public abstract string[] Split (string line);

		public abstract bool IsSeparator (string line, int startAt, out int separatorLen);

		public abstract int IndexOfNextSeparator (string line, int startAt, out int nextColumn);

		public static ColumnProviderDelimiterLineSplitter Create (string columnDelimiter, string columnDelimiterRegex, char[] defaultDelimiter)
		{
			return Create (columnDelimiter != null ? columnDelimiter.ToCharArray () : null, columnDelimiterRegex, defaultDelimiter);
		}

		public static ColumnProviderDelimiterLineSplitter Create (char[] columnDelimiter, string columnDelimiterRegex, char[] defaultDelimiter)
		{
			if (columnDelimiter != null)
				return new ColumnProviderDelimiterLineSplitterSimple (columnDelimiter);
			else if (columnDelimiterRegex != null)
				return new ColumnProviderDelimiterLineSplitterRegex (columnDelimiterRegex);
			else if (defaultDelimiter != null)
				return new ColumnProviderDelimiterLineSplitterSimple (defaultDelimiter);
			else
				throw new NotSupportedException ();
		}

		public static ColumnProviderDelimiterLineSplitter Create (char[] defaultDelimiter)
		{
			if (defaultDelimiter != null)
				return new ColumnProviderDelimiterLineSplitterSimple (defaultDelimiter);
			else
				throw new NotSupportedException ();
		}
	}

	public class ColumnProviderDelimiterLineSplitterSimple : ColumnProviderDelimiterLineSplitter
	{
		readonly protected char[] separators;

		public ColumnProviderDelimiterLineSplitterSimple (char[] separators)
		{
			if (separators != null)
				this.separators = separators;
			else
				this.separators = new char[] { '\t' };
		}

		#region IColumnProviderDelimiterLineSplitter implementation

		public override string[] Split (string line)
		{
			return line.Split (separators, StringSplitOptions.None);
		}

		public override bool IsSeparator (string line, int startAt, out int separatorLen)
		{
			if (Array.Exists (separators, p => p == line [startAt])) {
				separatorLen = 1;
				return true;
			} else {
				separatorLen = 0;
				return false;
			}
		}

		public override int IndexOfNextSeparator (string line, int startAt, out int nextColumn)
		{
			int nextSep = line.IndexOfAny (separators, startAt);
			if (nextSep == -1)
				nextColumn = -1;
			else
				nextColumn = nextSep + 1;
			return nextSep;
		}

		#endregion

	}

	public class ColumnProviderDelimiterLineSplitterRegex : ColumnProviderDelimiterLineSplitter
	{
		readonly Regex regex;

		public ColumnProviderDelimiterLineSplitterRegex (string regex)
		{
			this.regex = new Regex (regex, RegexOptions.None);
		}

		#region IColumnProviderDelimiterLineSplitter implementation

		public override string[] Split (string line)
		{
			return regex.Split (line);
		}

		public override bool IsSeparator (string line, int startAt, out int separatorLen)
		{
			int nextColumn;
			int index = IndexOfNextSeparator (line, startAt, out nextColumn);
			if (index == startAt) {
				separatorLen = nextColumn - startAt;
				return true;
			} else {
				separatorLen = -1;
				return false;
			}
		}

		public override int IndexOfNextSeparator (string line, int startAt, out int nextColumn)
		{
			Match match = regex.Match (line, startAt);
			if (match.Success) {
				nextColumn = match.Index + match.Length;
				return match.Index;
			} else {
				nextColumn = -1;
				return -1;
			}
		}

		#endregion

	}
}

