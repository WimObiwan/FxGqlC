using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class OrderbyProvider : IProvider
	{
		public enum OrderEnum
		{
			ASC,
			DESC
		}
		
		public class Column
		{
			public OrderEnum Order { get; set; }

			public IExpression Expression { get; set; }
		}
			
		class Key : ColumnsComparerKey
		{
			public IComparable[] OriginalColumns { get; set; }

			public long LineNo { get; set; }

			public string Source { get; set; }

			public IComparable[] Columns { get; set; }
		}
		
		IProvider provider;
		IList<Column> orderbyColumns;
		StringComparer stringComparer;
		bool dataRetrieved;
		List<Key> data;
		int currentRecord;
		ProviderRecord record = new ProviderRecord ();
		
		public OrderbyProvider (IProvider provider, IList<Column> orderbyColumns, StringComparer stringComparer)
		{
			this.provider = provider;
			this.orderbyColumns = orderbyColumns;
			this.stringComparer = stringComparer;
		}

		#region IProvider implementation
		public int GetColumnOrdinal(string columnName)
		{
			return provider.GetColumnOrdinal(columnName);
		}
		
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		public void Initialize ()
		{
			provider.Initialize ();
			dataRetrieved = false;
			data = new List<Key> ();
			currentRecord = -1;
		}

		public bool GetNextRecord ()
		{
			if (!dataRetrieved) {
				RetrieveData ();
				dataRetrieved = true;
			}
			
			currentRecord++;
			if (currentRecord < data.Count) {
				record.OriginalColumns = data [currentRecord].OriginalColumns;
				record.Source = data [currentRecord].Source;
				record.LineNo = data [currentRecord].LineNo;
				record.Columns = data [currentRecord].Columns;
			
				return true;
			}
			return false;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			data = null;
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

		private void RetrieveData ()
		{
			GqlQueryState gqlQueryState = new GqlQueryState ();
			gqlQueryState.TotalLineNumber = 0;
			gqlQueryState.UseOriginalColumns = true;
			bool[] descArray = new bool[orderbyColumns.Count];
			for (int i = 0; i < orderbyColumns.Count; i++) {
				descArray [i] = (orderbyColumns [i].Order == OrderEnum.DESC);
			}
			
			int[] fixedColumns = new int[orderbyColumns.Count];
			Type[] types = new Type[orderbyColumns.Count];
			for (int i = 0; i < orderbyColumns.Count; i++) {
				if (orderbyColumns [i].Expression is ConstExpression<long>) {
					fixedColumns [i] = (int)((ConstExpression<long>)orderbyColumns [i].Expression).Evaluate (null) - 1;
					if (fixedColumns [i] < 0)
						throw new Exception (string.Format ("Negative order by column ordinal ({0}) is not allowed", fixedColumns [i] + 1));
					types [i] = provider.GetColumnTypes () [fixedColumns [i]];
				} else {
					fixedColumns [i] = -1;
					types [i] = orderbyColumns [i].Expression.GetResultType ();
				}
			}
				
			while (provider.GetNextRecord()) {
				gqlQueryState.TotalLineNumber++;
				gqlQueryState.Record = provider.Record;
				Key key = new Key ();
				key.Members = new IComparable[orderbyColumns.Count];
				key.OriginalColumns = provider.Record.OriginalColumns;
				key.LineNo = provider.Record.LineNo;
				key.Source = provider.Record.Source;
				key.Columns = provider.Record.Columns;
				for (int i = 0; i < orderbyColumns.Count; i++) {
					if (fixedColumns [i] >= 0) {
						if (fixedColumns [i] >= provider.Record.Columns.Length)
							throw new Exception (string.Format ("Order by ordinal {0} is not allowed because only {1} columns are available", fixedColumns [i] + 1, provider.Record.Columns.Length));
						key.Members [i] = provider.Record.Columns [fixedColumns [i]];
					} else {
						key.Members [i] = orderbyColumns [i].Expression.EvaluateAsComparable (gqlQueryState);
					}
				}
				data.Add (key);
			}
			
			ColumnsComparer<Key > columnsComparer = new ColumnsComparer<Key> (types, descArray, stringComparer);
			data.Sort (columnsComparer);
		}
	}
}

