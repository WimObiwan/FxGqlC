using System;
using System.Globalization;

namespace FxGqlLib
{
	public class SubqueryExpression : IExpression
	{
		readonly IProvider provider;

		public SubqueryExpression (IProvider provider)
		{
			this.provider = provider;
		}

		public IExpression GetTyped (CultureInfo cultureInfo)
		{
			Type type = GetResultType ();
			return ConvertExpression.Create (type, this, cultureInfo);
		}

		#region IExpression implementation

		public IData EvaluateAsData (GqlQueryState gqlQueryState)
		{
			GqlQueryState gqlQueryState2 = new GqlQueryState (gqlQueryState);
			try {
				provider.Initialize (gqlQueryState2);
				if (!provider.GetNextRecord ()) {
					Type type = GetResultType ();
                    return DataTypeUtil.GetDefaultFromDataType(GetResultType());
				}
				if (provider.Record.Columns.Length != 1)
					throw new InvalidOperationException ("Expression subquery didn't return exactly 1 column");
				return provider.Record.Columns [0];
			} finally {
				provider.Uninitialize ();
			}
		}

		public Type GetResultType ()
		{
			return this.provider.GetColumnTypes () [0];
		}

		public bool IsAggregated ()
		{
			return false;
		}

        public bool HasState()
        {
            return false;
        }

        public bool IsConstant ()
		{
			return false;
		}

		public void Process (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}

		public IData ProcessCalculate (StateBin state)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}

		#endregion

	}
}



