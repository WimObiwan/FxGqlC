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

	[Obsolete]
	class AntlrTreeEnumerator
	{
		ITree parent;
		IEnumerator<ITree> enumerator;
		ITree current;
        
		public ITree Current { get { return current; } }
        
		public AntlrTreeEnumerator (CommonTree parent)
		{
			this.parent = parent;
			enumerator = parent.Children.GetEnumerator ();
			if (enumerator.MoveNext ())
				current = enumerator.Current;
			else
				current = null;
		}
        
		public void MoveNext ()
		{
			if (current == null)
				throw new NotEnoughSubTokensAntlrException (parent);
			if (enumerator.MoveNext ())
				current = enumerator.Current;
			else
				current = null;
		}       
	}
    
}

