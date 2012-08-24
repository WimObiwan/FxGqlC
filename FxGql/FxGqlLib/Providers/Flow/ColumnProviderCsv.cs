using System;
using System.Collections.Generic;
using System.Text;

namespace FxGqlLib
{
	public class ColumnProviderCsv : ColumnProviderDelimiter
	{
		public ColumnProviderCsv (IProvider provider)
			: this(provider, null, -1)
		{
		}

		public ColumnProviderCsv (IProvider provider, char[] separators)
			: this(provider, separators, -1)
		{
		}

		public ColumnProviderCsv (IProvider provider, char[] separators, int columnCount)
			: base(provider, separators, columnCount)
		{
		}

		bool IsSeparator (char ch)
		{
			return Array.Exists (separators, p => p == ch);
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
					int nextSep = line.IndexOfAny (separators, pos);
					if (nextSep != -1) {
						fields.Add (line.Substring (pos, nextSep - pos));
						pos = nextSep + 1;
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
							done = nextQuote + 1 >= line.Length;
							char nextChar = line [nextQuote + 1];
							if (done || Array.Exists (separators, p => p == nextChar)) {
								// Valid field terminator
								string str = line.Substring (pos, nextQuote - pos);
								if (sb == null) {
									fields.Add (str);
								} else {
									sb.Append (str);
									fields.Add (sb.ToString ());
								}
								pos = nextQuote + 2;
								break;
							} else if (nextChar == '"') {
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

