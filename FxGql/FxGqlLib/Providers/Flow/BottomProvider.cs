using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class BottomProvider : IProvider
	{
		readonly IProvider provider;
		readonly Expression<DataInteger> bottomValueExpression;

		GqlQueryState gqlQueryState;
		Queue<ProviderRecord> recordsQueue;
		ProviderRecord record;

		public BottomProvider (IProvider provider, Expression<DataInteger> bottomValueExpression)
		{
			this.provider = provider;
			this.bottomValueExpression = bottomValueExpression;
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			return provider.GetColumnNames ();
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return provider.GetColumnOrdinal (columnName);
		}
		
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = gqlQueryState;
			record = null;
		}

		public bool GetNextRecord ()
		{
			if (recordsQueue == null) {
				long maxCount = bottomValueExpression.Evaluate (gqlQueryState);
				recordsQueue = new Queue<ProviderRecord> ();
				try {
					provider.Initialize (gqlQueryState);
					while (provider.GetNextRecord ()) {
						recordsQueue.Enqueue (provider.Record.Clone ());
						if (recordsQueue.Count > maxCount)
							recordsQueue.Dequeue ();
					}
				} finally {
					provider.Uninitialize ();
				}
			}

			if (recordsQueue.Count > 0) {
				record = recordsQueue.Dequeue ();
				return true;
			} else {
				return false;
			}
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			recordsQueue = null;
			record = null;
		}

		public ProviderRecord Record {
			get {
				return record;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			provider.Dispose ();
		}
		#endregion
	}
}

