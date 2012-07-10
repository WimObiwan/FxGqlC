using System;

namespace FxGqlLib
{
	public class SubstringFunction : Expression<string>
	{
		readonly Expression<string> arg1;
		readonly Expression<long> arg2;
		readonly Expression<long> arg3;
		
		public SubstringFunction (IExpression arg1, IExpression arg2)
			: this (arg1, arg2, null)
		{
		}
		
		public SubstringFunction (IExpression arg1, IExpression arg2, IExpression arg3)
		{
			this.arg1 = ExpressionHelper.ConvertToStringIfNeeded (arg1);
			this.arg2 = ExpressionHelper.ConvertIfNeeded<long> (arg2);
			if (arg3 != null) 
				this.arg3 = ExpressionHelper.ConvertIfNeeded<long> (arg3);
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			string text = arg1.Evaluate (gqlQueryState);
			int start = (int)arg2.Evaluate (gqlQueryState) - 1;
			if (start < 0)
				start = 0;
			if (start >= text.Length)
				return "";
			if (arg3 != null) {
				int length = (int)arg3.Evaluate (gqlQueryState);
				if (start + length >= text.Length)
					return text.Substring (start);
				else
					return text.Substring (start, length);
			} else {
				return text.Substring (start);
			}
		}
		#endregion
	}
}

