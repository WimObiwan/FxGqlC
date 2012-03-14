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
		public void Initialize ()
		{
			provider.Initialize ();
			gqlQueryState = new GqlQueryState ();
			gqlQueryState.TotalLineNumber = 0;
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
			provider.Dispose();
		}
		#endregion
	}
}

