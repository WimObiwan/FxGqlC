using System;

namespace FxGqlLib
{
	public static class ObsoleteExpressionConvertor
	{
		public static Expression<DataInteger> Convert (Expression<long> topValueExpression)
		{
			return new UnaryExpression<long, DataInteger> ((a) => new DataInteger (a), topValueExpression);
		}
	}
}

