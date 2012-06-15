using System;

namespace FxGqlLib
{
	public class TopProvider : IProvider
	{
		IProvider provider;
		Expression<long> topValueExpression;
		long linesToGo;
		
		public TopProvider (IProvider provider, Expression<long> topValueExpression)
		{
			this.provider = provider;
			this.topValueExpression = topValueExpression;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return provider.GetColumnTitles();
		}

		public int GetColumnOrdinal(string columnName)
		{
			return provider.GetColumnOrdinal(columnName);
		}
		
		public Type[] GetColumnTypes()
		{
			return provider.GetColumnTypes();
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize(gqlQueryState);
			linesToGo = topValueExpression.Evaluate(gqlQueryState);
		}

		public bool GetNextRecord ()
		{
			if (linesToGo <= 0) return false;
			linesToGo--;
			return provider.GetNextRecord();
		}

		public void Uninitialize ()
		{
			provider.Uninitialize();
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
			provider.Dispose();
		}
		#endregion
	}
}

