using System;

namespace FxGqlLib
{
	public class ConvertExpression<ToT> : Expression<ToT> where ToT : IComparable
	{
		protected IExpression expression;
			
		public ConvertExpression (IExpression  expression)
		{
			this.expression = expression;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[ToT]
		public override ToT Evaluate (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAs<ToT> (gqlQueryState);
		}
		#endregion
	}
}

