using System;
using System.Linq.Expressions;

namespace Playground
{
	class MainClass
	{
		class MyState
		{
			public long MyProperty { get; set; }
		}

		public static void Main (string[] args)
		{
			MyState myState = new MyState ();
			myState.MyProperty = 35;

			//Expression<Func<MyState, long>> GetMyPropertyExpression = p => p.MyProperty;

			ParameterExpression param = Expression.Parameter (typeof(MyState), "$val");
			Expression GetMyPropertyExpression = Expression.Property (param, "MyProperty");

			Expression expr = Expression.Equal (GetMyPropertyExpression, 
			                                    Expression.Add (
				Expression.Constant ((long)17), Expression.Constant ((long)18)));

			//var block = Expression.Block (typeof(bool), new ParameterExpression[] { variable }, expr);
			Console.WriteLine (expr.ToString ());
			expr = expr.Reduce ();
			Console.WriteLine (expr.ToString ());

			var compiled = Expression.Lambda<Func<MyState, bool>> (expr, new ParameterExpression[] { param }).Compile ();

			Console.WriteLine (compiled (myState));

			/*
			//ConstExpression<DataBoolean> expr1 = new ConstExpression<DataBoolean> (true);
			ConstExpression expr1 = new ConstExpression (new DataString ("aaa"));
			ConstExpression expr2 = new ConstExpression (new DataString ("bbb"));

			Expression exprAdd = ExpressionOperationEngine.Get ().Operate (Operator.Add, expr1, expr2);

			Console.WriteLine (exprAdd.GetResultDataType ());
			//var exprR = ExpressionTree.ConstructOperation (Operation.Not, expr1);
			//Console.WriteLine (exprR.EvaluateAsData (null).ToDataString ().ToString ());
			Console.ReadLine ();
			*/
		}
	}
}
