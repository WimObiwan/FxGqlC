
using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class OrderbyProvider : IProvider
	{
		public enum OrderEnum
		{
			ASC,
			DESC,
			ORIG
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
		IList<Column> origOrderByColumns;
		IList<Column> orderbyColumns;
		StringComparer stringComparer;
		bool dataRetrieved;
		List<Key> data;
		int nextRecord;
		ProviderRecord record = new ProviderRecord ();
		GqlQueryState gqlQueryState;
		GqlQueryState newGqlQueryState;
		int[] fixedColumns;
		ColumnsComparer<Key > columnsComparer;
		ColumnsComparer<ColumnsComparerKey > origColumnsComparer;
		bool moreData;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="FxGqlLib.OrderbyProvider"/> class.
		/// </summary>
		/// <param name='provider'>
		/// Provider.
		/// </param>
		/// <param name='orderbyColumns'>
		/// Orderby columns.
		/// </param>
		/// <param name='stringComparer'>
		/// String comparer.
		/// </param>
		public OrderbyProvider (IProvider provider, IList<Column> orderbyColumns, StringComparer stringComparer)
		{
			this.provider = provider;
			this.orderbyColumns = new List<Column> ();
			this.origOrderByColumns = null;
			var colEnum = orderbyColumns.GetEnumerator ();
			while (colEnum.MoveNext()) {
				if (colEnum.Current.Order == OrderEnum.ORIG) {
					if (origOrderByColumns == null)
						origOrderByColumns = new List<Column> ();
					origOrderByColumns.Add (colEnum.Current);
				} else {
					do {
						this.orderbyColumns.Add (colEnum.Current);
					} while (colEnum.MoveNext());
					break;
				}
			}

			this.stringComparer = stringComparer;
		}

		#region IProvider implementation
		/// <summary>
		/// Gets the column titles.
		/// </summary>
		/// <returns>
		/// The column titles.
		/// </returns>
		public string[] GetColumnTitles ()
		{
			return provider.GetColumnTitles ();
		}

		/// <summary>
		/// Gets the column ordinal.
		/// </summary>
		/// <returns>
		/// The column ordinal.
		/// </returns>
		/// <param name='columnName'>
		/// Column name.
		/// </param>
		public int GetColumnOrdinal (string columnName)
		{
			return provider.GetColumnOrdinal (columnName);
		}
		
		/// <summary>
		/// Gets the column types.
		/// </summary>
		/// <returns>
		/// The column types.
		/// </returns>
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		/// <summary>
		/// Initialize the specified gqlQueryState.
		/// </summary>
		/// <param name='gqlQueryState'>
		/// Gql query state.
		/// </param>
		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = gqlQueryState;
			provider.Initialize (gqlQueryState);
			dataRetrieved = false;
			data = new List<Key> ();
			nextRecord = -1;

			newGqlQueryState = new GqlQueryState (this.gqlQueryState.CurrentExecutionState, this.gqlQueryState.Variables);
			newGqlQueryState.TotalLineNumber = 0;
			newGqlQueryState.UseOriginalColumns = true;

			columnsComparer = CreateColumnsComparer<Key> (orderbyColumns);
			if (origOrderByColumns != null) {
				origColumnsComparer = CreateColumnsComparer<ColumnsComparerKey> (origOrderByColumns);
			} else {
				origColumnsComparer = null;
			}

			// Prefetch first row
			moreData = provider.GetNextRecord ();
		}

		/// <summary>
		/// Gets the next record.
		/// </summary>
		/// <returns>
		/// The next record.
		/// </returns>
		public bool GetNextRecord ()
		{
			if (!dataRetrieved) {
				if (!moreData)
					return false;
				RetrieveData ();
				dataRetrieved = true;

				if (data.Count == 0)
					return false;
				nextRecord = 0;
			}

			record.OriginalColumns = data [nextRecord].OriginalColumns;
			record.Source = data [nextRecord].Source;
			record.LineNo = data [nextRecord].LineNo;
			record.Columns = data [nextRecord].Columns;
			nextRecord++;

			if (nextRecord >= data.Count) {
				dataRetrieved = false;
			}

			return true;
		}

		/// <summary>
		/// Uninitialize this instance.
		/// </summary>
		public void Uninitialize ()
		{
			provider.Uninitialize ();
			data = null;
			gqlQueryState = null;
			newGqlQueryState = null;
			fixedColumns = null;
			columnsComparer = null;
			origColumnsComparer = null;
		}

		/// <summary>
		/// Gets the record.
		/// </summary>
		/// <value>
		/// The record.
		/// </value>
		public ProviderRecord Record {
			get {
				return record;
			}
		}

		#endregion

		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="FxGqlLib.OrderbyProvider"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="FxGqlLib.OrderbyProvider"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="FxGqlLib.OrderbyProvider"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="FxGqlLib.OrderbyProvider"/> so the
		/// garbage collector can reclaim the memory that the <see cref="FxGqlLib.OrderbyProvider"/> was occupying.
		/// </remarks>
		public void Dispose ()
		{
			provider.Dispose ();
		}

		#endregion

		private void RetrieveData ()
		{
			data.Clear ();

			IComparable[] lastOrigColumns = null;
			IComparable[] currentOrigColumns;
			if (origOrderByColumns != null) {
				currentOrigColumns = new IComparable[origOrderByColumns.Count];
			} else {
				currentOrigColumns = null;
			}

			do {
				newGqlQueryState.TotalLineNumber++;
				newGqlQueryState.Record = provider.Record;

				if (origOrderByColumns != null) {
					for (int i = 0; i < origOrderByColumns.Count; i++) {
						currentOrigColumns [i] = origOrderByColumns [i].Expression.EvaluateAsComparable (newGqlQueryState);
					}

					if (lastOrigColumns == null) {
						lastOrigColumns = new IComparable[origOrderByColumns.Count];
						currentOrigColumns.CopyTo (lastOrigColumns, 0);
					} else {
						if (origColumnsComparer.Compare (new ColumnsComparerKey (lastOrigColumns), new ColumnsComparerKey (currentOrigColumns)) != 0) {
							lastOrigColumns = null;
							break;
						}
					}
				}

				Key key = new Key ();
				key.Members = new IComparable[orderbyColumns.Count];
				key.OriginalColumns = (IComparable[])provider.Record.OriginalColumns;
				key.LineNo = provider.Record.LineNo;
				key.Source = provider.Record.Source;
				key.Columns = (IComparable[])provider.Record.Columns.Clone ();
				for (int i = 0; i < orderbyColumns.Count; i++) {
					if (fixedColumns [i] >= 0) {
						if (fixedColumns [i] >= provider.Record.Columns.Length)
							throw new Exception (string.Format ("Order by ordinal {0} is not allowed because only {1} columns are available", fixedColumns [i] + 1, provider.Record.Columns.Length));
						key.Members [i] = provider.Record.Columns [fixedColumns [i]];
					} else {
						key.Members [i] = orderbyColumns [i].Expression.EvaluateAsComparable (newGqlQueryState);
					}
				}
				data.Add (key);
				moreData = provider.GetNextRecord ();

			} while (moreData);

			data.Sort (columnsComparer);
		}

		ColumnsComparer<T> CreateColumnsComparer<T> (IList<Column> orderbyColumns) where T : ColumnsComparerKey
		{
			bool[] descArray = new bool[orderbyColumns.Count];
			for (int i = 0; i < orderbyColumns.Count; i++) {
				descArray [i] = (orderbyColumns [i].Order == OrderEnum.DESC);
			}
			
			fixedColumns = new int[orderbyColumns.Count];
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
				
			return new ColumnsComparer<T> (types, descArray, stringComparer);
		}
	}
}

