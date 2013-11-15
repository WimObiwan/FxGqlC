using System;
using System.Collections.Generic;
using System.Text;

namespace FxGqlLib
{
	public class ColumnProviderCsv : ColumnProviderDelimiter
	{
		public ColumnProviderCsv (IProvider provider)
			: this (provider, null, -1)
		{
		}

		public ColumnProviderCsv (IProvider provider, ColumnProviderDelimiterLineSplitter splitter)
			: this (provider, splitter, -1)
		{
		}

		public ColumnProviderCsv (IProvider provider, ColumnProviderDelimiterLineSplitter splitter, int columnCount)
			: base (provider, splitter, columnCount)
		{
		}

		protected override string[] ReadLine ()
		{
			if (!provider.GetNextRecord ())
				return null;
			string line = provider.Record.Columns [0].ToString ();

			List<string> fields = new List<string> ();

			int pos = 0;
			bool done = false;
			do {
				// Beginning of next field
				if (pos >= line.Length) {
					fields.Add ("");
					done = true;
				} else if (line [pos] != '"') {
					int newPos;
					int nextStep = splitter.IndexOfNextSeparator (line, pos, out newPos);
					if (nextStep != -1) {
						fields.Add (line.Substring (pos, nextStep - pos));
						pos = newPos;
					} else {
						fields.Add (line.Substring (pos));
						done = true;
					}
				} else {
					++pos;
					StringBuilder sb = null;
					do {
						int nextQuote = line.IndexOf ('"', pos);
						if (nextQuote == -1) {
							if (sb == null)
								sb = new StringBuilder ();
							sb.Append (line, pos, line.Length - pos);
							sb.AppendLine ();
							if (!provider.GetNextRecord ())
								return null;
							line = provider.Record.Columns [0].ToString ();
							pos = 0;
							continue;
						} else {
							done = (nextQuote + 1 >= line.Length);
							int separatorLen;
							if (done || splitter.IsSeparator (line, nextQuote + 1, out separatorLen)) {
								// Valid field terminator
								string str = line.Substring (pos, nextQuote - pos);
								if (sb == null) {
									fields.Add (str);
								} else {
									sb.Append (str);
									fields.Add (sb.ToString ());
								}
								pos = nextQuote + separatorLen + 1;
								break;
							} else if (line [nextQuote + 1] == '"') {
								if (sb == null)
									sb = new StringBuilder ();
								sb.Append (line, pos, nextQuote - pos + 1);
								pos = nextQuote + 2;
								continue;
							}
						}
					} while (true);
				}
			} while (!done);

			return fields.ToArray ();
		}
	}
}

