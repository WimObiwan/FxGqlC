using System;
using System.Collections.Generic;
using Antlr.Runtime.Tree;
using System.Collections;

namespace FxGqlLib
{
	public class AntlrTreeChildEnumerable : IEnumerable<ITree>
	{
		ITree parent;

		public AntlrTreeChildEnumerable (ITree parent)
		{
			this.parent = parent;
		}

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<ITree> GetEnumerator ()
		{
			for (int pos = 0; pos < parent.ChildCount; pos++)
				yield return parent.GetChild (pos);
		}

		#endregion

	}
}

