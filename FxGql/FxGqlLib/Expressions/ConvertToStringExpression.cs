using System;

namespace FxGqlLib
{
	public class ConvertToStringExpression : ConvertExpression<DataString>
	{
		public ConvertToStringExpression (IExpression expression)
			: base(expression)
		{
		}

		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAsString (gqlQueryState);
		}
		
		public override DataString EvaluateAsString (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAsString (gqlQueryState);
		}
	}
}

