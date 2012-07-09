using System;

namespace FxGqlLib
{
	public class FilterProvider : IProvider
	{
		IProvider provider;
		Expression<bool> filterExpression;
		GqlQueryState gqlQueryState;
		
		public FilterProvider (IProvider provider, Expression<bool> filterExpression)
		{
			this.provider = provider;
			this.filterExpression = filterExpression;
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
			provider.Initialize (gqlQueryState);
			this.gqlQueryState = new GqlQueryState (gqlQueryState.CurrentExecutionState, gqlQueryState.Variables);
			this.gqlQueryState.TotalLineNumber = 0;
		}

		public bool GetNextRecord ()
		{
			gqlQueryState.TotalLineNumber++;
			
			while (provider.GetNextRecord ()) {
				gqlQueryState.Record = provider.Record;
				if (filterExpression.Evaluate (gqlQueryState))					
					return true;
			}
			
			return false;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
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

