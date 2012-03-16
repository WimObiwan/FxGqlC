using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FxGqlLib
{
	public class SelectProvider : IProvider
	{
		IList<IExpression> outputList;
		IProvider provider;
		GqlQueryState gqlQueryState;
		ProviderRecord record;
		
		public SelectProvider (IList<IExpression> outputList, IProvider provider)
		{
			this.outputList = outputList;
			this.provider = provider;
		}

		#region IProvider implementation
		public Type[] GetColumnTypes()
		{
			Type[] types = new Type[outputList.Count];
			
			for (int i = 0; i < outputList.Count; i++) {
				types[i] = outputList[i].GetResultType();
			}
			
			return types;
		}

		public void Initialize ()
		{
			provider.Initialize ();
			gqlQueryState = new GqlQueryState ();
			gqlQueryState.TotalLineNumber = 0;
			record = new ProviderRecord ();
			record.Source = "(subQuery)";
			record.LineNo = 0;
		}

		public bool GetNextRecord ()
		{
			if (!provider.GetNextRecord ())
				return false;
			gqlQueryState.Record = provider.Record;
			gqlQueryState.TotalLineNumber++;
			
			record.Columns = new IComparable[outputList.Count];
			for (int i = 0; i < outputList.Count; i++) {
				record.Columns [i] = outputList [i].EvaluateAsComparable (gqlQueryState);
			}
			
			record.OriginalColumns = provider.Record.Columns;
			record.LineNo = gqlQueryState.TotalLineNumber;
			
			return true;
		}

		public void Uninitialize ()
		{
			record = null;
			gqlQueryState = null;
			if (provider != null)
				provider.Uninitialize ();
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
			if (provider != null)
				provider.Dispose ();
		}
		#endregion
	}
}

