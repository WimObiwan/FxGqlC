using System;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class StateProvider : IProvider
	{
		readonly IProvider provider;
        readonly ColumnName[] columnNameList;
        readonly IExpression[] outputColumns;
        StateBin[] data; // TODO: Later for OVER-clause: Dictionary, StateBin
		ProviderRecord record;
		GqlQueryState gqlQueryState;

		static IList<IExpression> GeneralGroupbyExpressionList { get; set; }

		public StateProvider(IProvider provider, IList<Column> outputColumns)
		{
			this.provider = provider;
			this.columnNameList = outputColumns.ToArray ();
            this.outputColumns = new IExpression[outputColumns.Count];
            for (int col = 0; col < outputColumns.Count; col++)
            {
                SingleColumn singleColumn = outputColumns[col] as SingleColumn;
                if (singleColumn != null)
                {
                    this.outputColumns[col] = singleColumn.Expression;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #region IProvider implementation

        public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			if (columnNameList == null)
				throw new InvalidOperationException (string.Format ("Column name {0} not found", columnName));
			
			return Array.FindIndex (columnNameList, a => a.CompareTo (columnName) == 0);
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
            provider.Initialize(gqlQueryState);
            this.gqlQueryState = gqlQueryState;

            data = new StateBin[outputColumns.Length];
            record = new ProviderRecord(this, true);
		}

		public bool GetNextRecord ()
		{
            bool moredata = provider.GetNextRecord();

			record.OriginalColumns = record.Columns;

            for (int col = 0; col < outputColumns.Length; col++)
            {
                IExpression expr = outputColumns[col];
                if (expr.HasState())
                {
                    if (data[col] == null)
                        data[col] = new StateBin();
                    expr.Process(data[col], gqlQueryState);
                    record.Columns[col] = expr.ProcessCalculate(data[col]);
                }
                else
                {
                    record.Columns[col] = expr.EvaluateAsData(gqlQueryState);
                }
            }
		
			return moredata;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			data = null;
			record = null;
			gqlQueryState = null;
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
	}
}

