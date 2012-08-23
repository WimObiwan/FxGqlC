using System;
using System.Collections.Generic;

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
			// TODO: Support for NewLine inside a field - http://tools.ietf.org/html/rfc4180

			if (!provider.GetNextRecord ())
				return null;

			List<string> fields = new List<string> ();

			string line = provider.Record.Columns [0].ToString ();

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
					// Look for <",> with no <"",>
					int beginField = pos;
					do {
						int nextSep = line.IndexOfAny (separators, pos);
						int endQuote = (nextSep == -1 ? line.Length : nextSep) - 1;
						if (line [endQuote] != '"' || (endQuote - 1 != beginField && line [endQuote - 1] == '"')) {
							// Field is not terminated yet
							if (nextSep != -1)
								pos = nextSep + 1;
							else
								throw new NotImplementedException ("Multiline fields in CSV file not yet supported");
							continue;
						}
						fields.Add (line.Substring (beginField + 1, endQuote - beginField - 1).Replace ("\"\"", "\""));
						if (nextSep == -1)
							done = true;
						pos = endQuote + 2;
						break;
					} while (true);
				}
			} while (!done);

			return fields.ToArray ();
		}
	}
}

