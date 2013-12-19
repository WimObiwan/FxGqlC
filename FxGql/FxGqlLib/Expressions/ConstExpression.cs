using System;

namespace FxGqlLib
{
	public class ConstExpression<T> : Expression<T> where T : IData
	{
		readonly T constValue;

		public ConstExpression (T constValue)
		{
			this.constValue = constValue;
		}

		public T GetConstValue ()
		{
			return constValue;
		}

		public override bool IsConstant ()
		{
			return true;
		}

		#region implemented abstract members of FxGqlLib.Expression[T]

		public override T Evaluate (GqlQueryState gqlQueryState)
		{
			return constValue;
		}

		#endregion

	}
}

