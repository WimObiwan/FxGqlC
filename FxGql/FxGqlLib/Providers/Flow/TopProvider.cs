using System;

namespace FxGqlLib
{
	public class TopProvider : IProvider
	{
		readonly IProvider provider;
		readonly System.Linq.Expressions.Expression<Func<GqlQueryState, long>> topValueExpression;

		long linesToGo;

		public TopProvider (IProvider provider, System.Linq.Expressions.Expression<Func<GqlQueryState, long>> topValueExpression)
		{
			this.provider = provider;
			this.topValueExpression = topValueExpression;
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
		
		public Type[] GetNewColumnTypes ()
		{
			return provider.GetNewColumnTypes ();
		}
		
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
			linesToGo = topValueExpression.Compile () (gqlQueryState);
		}

		public bool GetNextRecord ()
		{
			if (linesToGo <= 0)
				return false;
			linesToGo--;
			return provider.GetNextRecord ();
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

