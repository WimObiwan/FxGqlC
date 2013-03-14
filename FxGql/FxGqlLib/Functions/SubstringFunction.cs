using System;
using System.Globalization;

namespace FxGqlLib
{
	public class SubstringFunction : Expression<DataString>
	{
		readonly Expression<DataString> arg1;
		readonly Expression<DataInteger> arg2;
		readonly Expression<DataInteger> arg3;
		
		public SubstringFunction (IExpression arg1, IExpression arg2, CultureInfo cultureInfo)
			: this (arg1, arg2, null, cultureInfo)
		{
		}
		
		public SubstringFunction (IExpression arg1, IExpression arg2, IExpression arg3, CultureInfo cultureInfo)
		{
			this.arg1 = ConvertExpression.CreateDataString (arg1, cultureInfo);
			this.arg2 = ConvertExpression.CreateDataInteger (arg2, cultureInfo);
			if (arg3 != null) 
				this.arg3 = ConvertExpression.CreateDataInteger (arg3, cultureInfo);
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
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

		public override bool IsConstant ()
		{
			return arg1.IsConstant () && arg2.IsConstant () && arg3.IsConstant ();
		}
	}
}

