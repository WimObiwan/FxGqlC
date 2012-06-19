using System;

namespace FxGqlLib
{
    public class SubqueryExpression : IExpression
    {
        IProvider provider;

        public SubqueryExpression (IProvider provider)
        {
            this.provider = provider;
        }
        
        #region IExpression implementation
        public IComparable EvaluateAsComparable (GqlQueryState gqlQueryState)
        {
            GqlQueryState gqlQueryState2 = new GqlQueryState (gqlQueryState.CurrentExecutionState, gqlQueryState.Variables);
            gqlQueryState2.CurrentDirectory = gqlQueryState.CurrentDirectory;
            try {
                provider.Initialize (gqlQueryState2);
                if (!provider.GetNextRecord ())
                    throw new InvalidOperationException ("Expression subquery returned no records");
                if (provider.Record.Columns.Length != 1)
                    throw new InvalidOperationException ("Expression subquery didn't return exactly 1 record");
                return provider.Record.Columns [0];
            } finally {
                provider.Uninitialize ();
            }
        }

        public Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
        {
            IComparable value = EvaluateAsComparable (gqlQueryState);
            if (value is Y)
                return (Y)value;
            else
                return (Y)Convert.ChangeType (value, typeof(Y));
        }

        public string EvaluateAsString (GqlQueryState gqlQueryState)
        {
            IComparable value = EvaluateAsComparable (gqlQueryState);
            if (value is string)
                return (string)value;
            else
                return value.ToString ();
        }

        public Type GetResultType ()
        {
            return this.provider.GetColumnTypes () [0];
        }
        
        public bool IsAggregated ()
        {
            return false;
        }
        
        public void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
        {
            throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
        }
        
        public IComparable AggregateCalculate (AggregationState state)
        {
            throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
        }
        #endregion
    }
}



