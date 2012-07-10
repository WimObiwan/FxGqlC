using System;

namespace FxGqlLib
{
	public class FormatListFunction : Expression<string>
	{
		readonly IExpression[] expression;
		readonly string separator;
		
		public FormatListFunction (IExpression[] expression, string separator)
		{
			this.expression = expression;
			this.separator = separator;
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			string[] texts = new string[expression.Length];
			for (int i = 0; i < expression.Length; i++) {
				if (i > 0)
					texts [i * 2 - 1] = separator;
				texts [i * 2] = expression [i].EvaluateAsString (gqlQueryState);
			}
			
			return string.Concat (texts);
		}
		#endregion
	}
}

