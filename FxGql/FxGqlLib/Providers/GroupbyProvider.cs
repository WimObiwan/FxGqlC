using System;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GroupbyProvider : IProvider
	{
		IProvider provider;
		IList<IExpression> origGroupbyColumns;
		IList<IExpression> groupbyColumns;
		string[] columnNameList;
		IExpression[] outputColumns;
		StringComparer stringComparer;
		Dictionary<ColumnsComparerKey, AggregationState> data;
		int currentRecord;
		IEnumerator<KeyValuePair<ColumnsComparerKey, AggregationState>> enumerator;
		ProviderRecord record;
		GqlQueryState gqlQueryState;
		GqlQueryState newGqlQueryState;
		ColumnsComparer<ColumnsComparerKey> origColumnsComparer;
		bool moreData;

		static IList<IExpression> GeneralGroupbyExpressionList { get; set; }

		static GroupbyProvider ()
		{
			GeneralGroupbyExpressionList = new List<IExpression> ();
			GeneralGroupbyExpressionList.Add (new ConstExpression<int> (0));
		}

		class InvariantColumn : IExpression
		{
			IExpression expression;
			StringComparer stringComparer;
			
			public InvariantColumn (IExpression expression, StringComparer stringComparer)
			{
				this.expression = expression;
				this.stringComparer = stringComparer;
			}
			
			#region IExpression implementation
			public IComparable EvaluateAsComparable (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAsComparable (gqlQueryState);
			}

			public Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAs<Y> (gqlQueryState);
			}

			public string EvaluateAsString (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAsString (gqlQueryState);
			}

			public Type GetResultType ()
			{
				return expression.GetResultType ();
			}

			public bool IsAggregated ()
			{
				return true;
			}

			public void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
			{
				IComparable comparable1 = expression.EvaluateAsComparable (gqlQueryState);
				IComparable comparable2;
				if (!state.GetState<IComparable> (this, out comparable2)) {
					state.SetState (this, comparable1);
				} else {
					if (!comparable1.GetType ().Equals (comparable2.GetType ()))
						throw new Exception ("Expression returned different data types");
					bool invariant;
					if (comparable1 is string)
						invariant = (stringComparer.Compare (comparable1 as string, comparable2 as string) == 0);
					else
						invariant = (comparable1.CompareTo (comparable2) == 0);
					
					if (!invariant)
						throw new Exception ("Column Expression is not invariant during group by");
				}
			}

			public IComparable AggregateCalculate (AggregationState state)
			{
				IComparable comparable;
				state.GetState<IComparable> (this, out comparable);
				// TODO: If not found? Shouldn't happen?
				return comparable;
			}
			#endregion
		}

		public GroupbyProvider (IProvider provider, IList<Column> outputColumns, StringComparer stringComparer)
			: this (provider, null, GeneralGroupbyExpressionList, outputColumns, stringComparer)
		{
		}

		public GroupbyProvider (IProvider provider, IList<IExpression> origGroupbyColumns, IList<IExpression> groupbyColumns, IList<Column> outputColumns, StringComparer stringComparer)
		{
			this.provider = provider;
			if (origGroupbyColumns != null && origGroupbyColumns.Count > 0) 
				this.origGroupbyColumns = ConvertColumnOrdinals (origGroupbyColumns, outputColumns);
			else
				this.origGroupbyColumns = null;
			this.groupbyColumns = ConvertColumnOrdinals (groupbyColumns, outputColumns);
			this.stringComparer = stringComparer;

			this.columnNameList = outputColumns.Select (a => a.Name).ToArray ();
			this.outputColumns = new IExpression[outputColumns.Count];
			for (int col = 0; col < outputColumns.Count; col++) {
				if (!outputColumns [col].Expression.IsAggregated ())
					this.outputColumns [col] = new InvariantColumn (outputColumns [col].Expression, stringComparer);
				else
					this.outputColumns [col] = outputColumns [col].Expression;
			}
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (string columnName)
		{
			if (columnNameList == null)
				throw new NotSupportedException (string.Format ("Column name '{0}' not found", columnName));
			
			return Array.FindIndex (columnNameList, a => string.Compare (a, columnName, StringComparison.InvariantCultureIgnoreCase) == 0);
		}
		
		public Type[] GetColumnTypes ()
		{
			Type[] types = new Type[outputColumns.Length];
			
			for (int i = 0; i < outputColumns.Length; i++) {
				types [i] = outputColumns [i].GetResultType ();
			}
			
			return types;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = gqlQueryState;
			provider.Initialize (gqlQueryState);
			data = null;
			currentRecord = -1;
			enumerator = null;
			record = new ProviderRecord ();
			record.Source = "(aggregated)";
			
			newGqlQueryState = new GqlQueryState (this.gqlQueryState.CurrentExecutionState, this.gqlQueryState.Variables);
			newGqlQueryState.TotalLineNumber = 0;
			newGqlQueryState.UseOriginalColumns = true;
			
			ColumnsComparer<ColumnsComparerKey> columnsComparer = CreateComparer (groupbyColumns, stringComparer);
			data = new Dictionary<ColumnsComparerKey, AggregationState> (columnsComparer);			
			if (origGroupbyColumns != null)
				origColumnsComparer = CreateComparer (origGroupbyColumns, stringComparer);
			else
				origColumnsComparer = null;

			moreData = this.provider.GetNextRecord ();
		}

		public bool GetNextRecord ()
		{
			if (enumerator != null) {
				if (!enumerator.MoveNext ())
					enumerator = null;
			}

			while (enumerator == null) {
				if (!moreData)
					return false;
				RetrieveData ();

				enumerator = data.GetEnumerator ();
				if (!enumerator.MoveNext ())
					enumerator = null;
			}

			currentRecord++;
			record.LineNo = currentRecord;
			record.Columns = new IComparable[outputColumns.Length];
			for (int col = 0; col < outputColumns.Length; col++) {
				record.Columns [col] = outputColumns [col].AggregateCalculate (enumerator.Current.Value);
			}
			
			record.OriginalColumns = record.Columns;
		
			return true;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			enumerator = null;
			data = null;
			record = null;
			gqlQueryState = null;
			newGqlQueryState = null;
			origColumnsComparer = null;
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
			data.Clear ();

			IComparable[] lastOrigColumns = null;
			IComparable[] currentOrigColumns;
			if (origGroupbyColumns != null) {
				currentOrigColumns = new IComparable[origGroupbyColumns.Count];
			} else {
				currentOrigColumns = null;
			}

			do {
				newGqlQueryState.TotalLineNumber++;
				newGqlQueryState.Record = provider.Record;

				if (origGroupbyColumns != null) {
					for (int i = 0; i < origGroupbyColumns.Count; i++) {
						if (origColumnsComparer.FixedColumns [i] >= 0) {
							if (origColumnsComparer.FixedColumns [i] >= provider.Record.Columns.Length)
								throw new Exception (string.Format ("Order by ordinal {0} is not allowed because only {1} columns are available", origColumnsComparer.FixedColumns [i] + 1, provider.Record.Columns.Length));
							currentOrigColumns [i] = provider.Record.Columns [origColumnsComparer.FixedColumns [i]];
						} else {
							currentOrigColumns [i] = origGroupbyColumns [i].EvaluateAsComparable (newGqlQueryState);
						}
					}

					if (lastOrigColumns == null) {
						lastOrigColumns = new IComparable[origGroupbyColumns.Count];
						currentOrigColumns.CopyTo (lastOrigColumns, 0);
					} else {
						if (origColumnsComparer.Compare (new ColumnsComparerKey (lastOrigColumns), new ColumnsComparerKey (currentOrigColumns)) != 0) {
							lastOrigColumns = null;
							newGqlQueryState.TotalLineNumber--;
							break;
						}
					}
				}

				ColumnsComparerKey key = new ColumnsComparerKey ();
				key.Members = new IComparable[groupbyColumns.Count];
				for (int i = 0; i < groupbyColumns.Count; i++) {
					key.Members [i] = groupbyColumns [i].EvaluateAsComparable (newGqlQueryState);
				}
				
				AggregationState state;
				if (!data.TryGetValue (key, out state)) {
					state = new AggregationState ();
					//foreach (var column in aggregationColumns)
					//	column.AggregateInitialize(state);
					data.Add (key, state);
				}
				
				// Aggregate
				foreach (var column in outputColumns)
					column.Aggregate (state, newGqlQueryState);

				moreData = provider.GetNextRecord ();
			} while (moreData);
		}

		IList<IExpression> ConvertColumnOrdinals (IList<IExpression> groupByColumns, IList<Column> outputColumns)
		{
			List<IExpression> convertedGroupByColumns = new List<IExpression> (groupByColumns);
			for (int i = 0; i < convertedGroupByColumns.Count; i++) {
				if (convertedGroupByColumns [i] is ConstExpression<long>) {
					int col = (int)convertedGroupByColumns [i].EvaluateAs<long> (null) - 1;
					if (col < 0 || col >= outputColumns.Count) {
						throw new Exception (string.Format ("Invalid group by column ordinal ({0})", col));
					}
					convertedGroupByColumns [i] = outputColumns [col].Expression;
				}
			}

			return convertedGroupByColumns;
		}		

		ColumnsComparer<ColumnsComparerKey> CreateComparer (IList<IExpression> groupbyColumns, StringComparer stringComparer)
		{
			Type[] types = new Type[groupbyColumns.Count];
			int[] fixedColumns = new int[groupbyColumns.Count];

			for (int i = 0; i < groupbyColumns.Count; i++) {
				types [i] = groupbyColumns [i].GetResultType ();
				if (groupbyColumns [i] is ConstExpression<long>) {
					fixedColumns [i] = (int)((ConstExpression<long>)groupbyColumns [i]).Evaluate (null) - 1;
					if (fixedColumns [i] < 0)
						throw new Exception (string.Format ("Negative order by column ordinal ({0}) is not allowed", fixedColumns [i] + 1));
				} else {
					fixedColumns [i] = -1;
				}
			}

			return new ColumnsComparer<ColumnsComparerKey> (types, fixedColumns, stringComparer);
		}
	}
}

