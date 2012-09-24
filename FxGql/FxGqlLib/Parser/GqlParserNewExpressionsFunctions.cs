using System;
using System.Collections.Generic;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Reflection;

namespace FxGqlLib
{
	partial class GqlParser
	{
		delegate System.Linq.Expressions.Expression GqlFunction (GqlParser gqlParser,params System.Linq.Expressions.Expression[] prms);
		static List<Dictionary<string, GqlFunction>> functionMap = new List<Dictionary<string, GqlFunction>> ();

		static void InitializeFunctionMap ()
		{
			FnAdd ("GETCURDIR", 0, new GqlFunction (FnGetCurDir));
		}

		static void FnAdd (string functionName, int prmCount, GqlFunction function)
		{
			for (int i = functionMap.Count; i <= prmCount; i++)
				functionMap.Add (new Dictionary<string, GqlFunction> ());
			
			functionMap [prmCount].Add (functionName, function);
		}

		GqlFunction FnGet (string functionName, int argCount)
		{
			GqlFunction function = null;
			if (argCount < functionMap.Count) {
				var functionMapItem = functionMap [argCount];
				functionMapItem.TryGetValue (functionName.ToUpperInvariant (), out function);
			}

			if (function == null)
				throw new NotSupportedException (
				string.Format ("Function '{0}' with {1} arguments does not exist.", functionName, argCount));

			return function;
		}

		static PropertyInfo GqlQueryStateCurrentDirectoryProperty = typeof(GqlQueryState).GetProperty ("CurrentDirectory");

		static System.Linq.Expressions.Expression FnGetCurDir (GqlParser gqlParser, params System.Linq.Expressions.Expression[] prms)
		{
			return
				System.Linq.Expressions.Expression.Property (
					gqlParser.queryStatePrm, GqlQueryStateCurrentDirectoryProperty);
		}

		System.Linq.Expressions.Expression ParseNewExpressionFunctionCall (IProvider provider, ITree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL", 1, -1);
			
			string functionName = functionCallTree.GetChild (0).Text;
			int argCount = functionCallTree.ChildCount - 1;

			System.Linq.Expressions.Expression result;
			// TODO: Remove fallback
			try {
				GqlFunction gqlFunction = FnGet (functionName, argCount);
				result = gqlFunction (this);
			} catch {
				IExpression oldExpr = ParseExpressionFunctionCall (provider, functionCallTree);
				result = ExpressionBridge.Create (oldExpr, queryStatePrm);
			}

			return result;
		}
	}
}

