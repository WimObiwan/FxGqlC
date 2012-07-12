using System;

namespace FxGqlLib
{
	public class VariableExpression : IExpression
	{
		readonly string variable;
		readonly Type type;

		public VariableExpression (string variable, Type type)
		{
			this.variable = variable;
			this.type = type;
		}

		public IExpression GetTyped ()
		{
			Type type = GetResultType ();
			return ConvertExpression.Create (type, this);
		}
        
        #region IExpression implementation
		public IData EvaluateAsData (GqlQueryState gqlQueryState)
		{
			Variable variable;
			if (!gqlQueryState.Variables.TryGetValue (this.variable, out variable))
				throw new InvalidOperationException (string.Format ("Variable '{0}' not declared", this.variable));

			return variable.Value;
		}

		public Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
		{
			IData value = EvaluateAsData (gqlQueryState);
			if (value is Y)
				return (Y)value;
			else
				return (Y)Convert.ChangeType (value, typeof(Y));
		}

		public DataString EvaluateAsString (GqlQueryState gqlQueryState)
		{
			IData value = EvaluateAsData (gqlQueryState);
			if (value is DataString)
				return (DataString)value;
			else
				return value.ToString ();
		}

		public Type GetResultType ()
		{
			return type;
		}
        
		public bool IsAggregated ()
		{
			return false;
		}
        
		public bool IsConstant ()
		{
			return false;
		}

		public void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
        
		public IData AggregateCalculate (StateBin state)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
        #endregion
	}
}

