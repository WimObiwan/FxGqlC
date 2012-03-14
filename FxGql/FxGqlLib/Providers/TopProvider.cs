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
		public void Initialize ()
		{
			provider.Initialize();
			GqlQueryState gqlQueryState = new GqlQueryState();
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

