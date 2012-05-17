using System;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GroupbyProvider : IProvider
	{
		IProvider provider;
		IList<IExpression> groupbyColumns;
		string[] columnNameList;
		IExpression[] outputColumns;
		StringComparer stringComparer;
		Dictionary<ColumnsComparerKey, AggregationState> data;
		int currentRecord;
		IEnumerator<KeyValuePair<ColumnsComparerKey, AggregationState>> enumerator;
		ProviderRecord record;
		
		class InvariantColumn : IExpression
		{
			IExpression expression;
			StringComparer stringComparer;
			
			public InvariantColumn(IExpression expression, StringComparer stringComparer)
			{
				this.expression = expression;
				this.stringComparer = stringComparer;
			}
			
			#region IExpression implementation
			public IComparable EvaluateAsComparable (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAsComparable(gqlQueryState);
			}

			public Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAs<Y>(gqlQueryState);
			}

			public string EvaluateAsString (GqlQueryState gqlQueryState)
			{
				return expression.EvaluateAsString(gqlQueryState);
			}

			public Type GetResultType ()
			{
				return expression.GetResultType();
			}

			public bool IsAggregated ()
			{
				return true;
			}

			public void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
			{
				IComparable comparable1 = expression.EvaluateAsComparable(gqlQueryState);
				IComparable comparable2;
				if (!state.GetState<IComparable>(this, out comparable2)) {
					state.SetState(this, comparable1);
				}
				else {
					if (!comparable1.GetType().Equals(comparable2.GetType()))
						throw new Exception("Expression returned different data types");
					bool invariant;
					if (comparable1 is string)
						invariant = (stringComparer.Compare(comparable1 as string, comparable2 as string) == 0);
					else
						invariant = (comparable1.CompareTo(comparable2) == 0);
					
					if (!invariant) throw new Exception("Column Expression is not invariant during group by");
				}
			}

			public IComparable AggregateCalculate (AggregationState state)
			{
				IComparable comparable;
				state.GetState<IComparable>(this, out comparable);
				// TODO: If not found? Shouldn't happen?
				return comparable;
			}
			#endregion
		}
		
		public GroupbyProvider (IProvider provider, IList<IExpression> groupbyColumns, IList<Column> outputColumns, StringComparer stringComparer)
		{
			this.provider = provider;
			this.groupbyColumns = groupbyColumns;
			this.stringComparer = stringComparer;
			
			this.columnNameList = outputColumns.Select(a => a.Name).ToArray();
			this.outputColumns = new IExpression[outputColumns.Count];
			for (int col = 0; col < outputColumns.Count; col++) {
				if (!outputColumns[col].Expression.IsAggregated())
					this.outputColumns[col] = new InvariantColumn(outputColumns[col].Expression, stringComparer);
				else
					this.outputColumns[col] = outputColumns[col].Expression;
			}
		}

		#region IProvider implementation
		public int GetColumnOrdinal(string columnName)
		{
			if (columnNameList == null)
				throw new NotSupportedException(string.Format("Column name '{0}' not found", columnName));
			
			return Array.FindIndex(columnNameList, a => string.Compare(a, columnName, StringComparison.InvariantCultureIgnoreCase) == 0);
		}
		
		public Type[] GetColumnTypes()
		{
			Type[] types = new Type[outputColumns.Length];
			
			for (int i = 0; i < outputColumns.Length; i++) {
				types[i] = outputColumns[i].GetResultType();
			}
			
			return types;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
			data = null;
			currentRecord = -1;
			enumerator = null;
			record = new ProviderRecord();
			record.Source = "(aggregated)";
			
		}

		public bool GetNextRecord ()
		{
			if (enumerator == null) {
				RetrieveData ();
				enumerator = data.GetEnumerator();
			}
			
			if (!enumerator.MoveNext()) return false;
			
			currentRecord++;
			record.LineNo = currentRecord;
			record.Columns = new IComparable[outputColumns.Length];
			for (int col = 0; col < outputColumns.Length; col++) {
				record.Columns[col] = outputColumns[col].AggregateCalculate(enumerator.Current.Value);
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
			
			Type[] types = new Type[groupbyColumns.Count];
			for (int i = 0; i < groupbyColumns.Count; i++) {
				types [i] = groupbyColumns [i].GetResultType ();
			}

			ColumnsComparer<ColumnsComparerKey> columnsComparer = new ColumnsComparer<ColumnsComparerKey> (types, stringComparer);
			data = new Dictionary<ColumnsComparerKey, AggregationState> (columnsComparer);
			
			while (provider.GetNextRecord()) {
				gqlQueryState.TotalLineNumber++;
				gqlQueryState.Record = provider.Record;
				ColumnsComparerKey key = new ColumnsComparerKey ();
				key.Members = new IComparable[groupbyColumns.Count];
				for (int i = 0; i < groupbyColumns.Count; i++) {
					key.Members [i] = groupbyColumns [i].EvaluateAsComparable (gqlQueryState);
				}
				
				AggregationState state;
				if (!data.TryGetValue(key, out state)) {
					state = new AggregationState();
					//foreach (var column in aggregationColumns)
					//	column.AggregateInitialize(state);
					data.Add (key, state);
				}
				
				// Aggregate
				foreach (var column in outputColumns)
					column.Aggregate(state, gqlQueryState);
			}
		}
	}
}

