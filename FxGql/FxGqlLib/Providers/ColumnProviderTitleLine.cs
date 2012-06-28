using System;
using System.Linq;

namespace FxGqlLib
{
	public class ColumnProviderTitleLine : ColumnProviderDelimiter
	{
		bool rule;

		public ColumnProviderTitleLine (IProvider provider, bool rule, char[] separators)
			: base(provider, separators)
		{
			this.rule = rule;
		}

		#region IProvider implementation
		public override void Initialize (GqlQueryState gqlQueryState)
		{
			base.Initialize (gqlQueryState);

			if (base.firstLine != null) {
				string[] columns = base.firstLine.Split (separators);
				for (int i = 0; i < columns.Length && i < columnNameList.Length; i++)
					if (columns [i] != "")
						columnNameList [i] = columns [i];
				base.firstLine = null;
			}

			if (rule) {
				provider.GetNextRecord ();
			}
		}
		#endregion
	}
}

