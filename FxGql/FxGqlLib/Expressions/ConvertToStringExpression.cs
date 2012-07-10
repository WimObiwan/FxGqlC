using System;

namespace FxGqlLib
{
	public class ConvertToStringExpression : ConvertExpression<string>
	{
		public ConvertToStringExpression (IExpression expression)
			: base(expression)
		{
		}

		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAsString (gqlQueryState);
		}
		
		public override string EvaluateAsString (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAsString (gqlQueryState);
		}
	}
}

