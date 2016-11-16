using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class CaseExpression : IExpression
	{
		public class WhenItem
		{
			public Expression<DataBoolean> Check { get; set; }

			public IExpression Result { get; set; }
		}

		readonly IList<WhenItem> whenItems;
		readonly IExpression elseResult;

		public CaseExpression (IList<WhenItem> whenItems, IExpression elseResult)
		{
			this.whenItems = whenItems;
			this.elseResult = elseResult;
		}

		#region IExpression implementation

		private IExpression GetResultExpression (GqlQueryState gqlQueryState)
		{
			foreach (WhenItem whenItem in whenItems) {
				if (whenItem.Check.Evaluate (gqlQueryState)) {
					return whenItem.Result;
				}
			}
			
			return elseResult;
		}

		public IData EvaluateAsData (GqlQueryState gqlQueryState)
		{
			return GetResultExpression (gqlQueryState).EvaluateAsData (gqlQueryState);
		}

		public Type GetResultType ()
		{
			if (whenItems.Count > 0)
				return whenItems [0].Result.GetResultType ();
			
			if (elseResult != null)
				return elseResult.GetResultType ();
			
			throw new InvalidOperationException ("Case statement without output");
		}

		public bool IsAggregated ()
		{
			//			throw new NotSupportedException("Aggregation with case expression not supported");

			return false;
			
			/*
			foreach (WhenItem whenItem in whenItems)
			{
				if (whenItem.Check.IsAggregated() || whenItem.Result.IsAggregated())
					return true;
			}
			
			if (elseResult != null && elseResult.IsAggregated())
				return true;
			
			return false;
			*/
			//TODO: Consistency check on aggregation
		}

        public bool HasState()
        {
            return false;
        }

        public bool IsConstant ()
		{
			foreach (WhenItem whenItem in whenItems) {
				if (!whenItem.Check.IsConstant ()) {
					return false;
				}
			}

			return true;
		}

		public void Process (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new NotSupportedException ("Aggregation with case expression not supported");

			//GetResultExpression(gqlQueryState).Aggregate(state, gqlQueryState);
		}

		public IData ProcessCalculate (StateBin state)
		{
			throw new NotSupportedException ("Aggregation with case expression not supported");

			/*
			foreach (WhenItem whenItem in whenItems)
			{
				if (whenItem.Check.IsAggregated() || whenItem.Result.IsAggregated())
					return true;
			}
			
			if (elseResult != null && elseResult.IsAggregated())
				return true;
			
			return false;
			*/
		}

		#endregion

	}
}

