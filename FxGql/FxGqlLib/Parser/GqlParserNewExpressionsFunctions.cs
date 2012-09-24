using System;
using System.Collections.Generic;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Reflection;

namespace FxGqlLib
{
	partial class GqlParser
	{
		List<Dictionary<string, System.Linq.Expressions.Expression>> functionMap = 
			new List<Dictionary<string, System.Linq.Expressions.Expression>> ();

		void FnAdd (string functionName, int prmCount, System.Linq.Expressions.Expression function)
		{
			for (int i = functionMap.Count; i <= prmCount; i++)
				functionMap.Add (new Dictionary<string, System.Linq.Expressions.Expression> ());
			
			functionMap [prmCount].Add (functionName, function);
		}

		void FnAdd<R> (string functionName, System.Linq.Expressions.Expression<Func<GqlQueryState, R>> functorExpr)
		{
			FnAdd (functionName, 0, functorExpr);
		}
		
		void FnAdd<T1, R> (string functionName, System.Linq.Expressions.Expression<Func<GqlQueryState, T1, R>> functorExpr)
		{
			FnAdd (functionName, 1, functorExpr);
		}
		
		void FnAdd<T1, T2, R> (string functionName, System.Linq.Expressions.Expression<Func<GqlQueryState, T1, T2, R>> functorExpr)
		{
			FnAdd (functionName, 2, functorExpr);
		}
		
		void FnAdd<T1, T2, T3, R> (string functionName, System.Linq.Expressions.Expression<Func<GqlQueryState, T1, T2, T3, R>> functorExpr)
		{
			FnAdd (functionName, 3, functorExpr);
		}
		
		void FnAdd<T1, T2, T3, T4, R> (string functionName, System.Linq.Expressions.Expression<Func<GqlQueryState, T1, T2, T3, T4, R>> functorExpr)
		{
			FnAdd (functionName, 4, functorExpr);
		}
		
		System.Linq.Expressions.Expression FnGet (string functionName, int argCount)
		{
			System.Linq.Expressions.Expression function = null;
			if (argCount < functionMap.Count) {
				var functionMapItem = functionMap [argCount];
				functionMapItem.TryGetValue (functionName.ToUpperInvariant (), out function);
			}

			if (function == null)
				throw new NotSupportedException (
					string.Format ("Function '{0}' with {1} arguments does not exist.", functionName, argCount));

			return function;
		}

		System.Linq.Expressions.Expression ParseNewExpressionFunctionCall (IProvider provider, ITree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL", 1, -1);
			
			string functionName = functionCallTree.GetChild (0).Text;
			int argCount = functionCallTree.ChildCount - 1;
			
			System.Linq.Expressions.Expression result;
			// TODO: Remove fallback
			try {
				result = FnGet (functionName, argCount);
				System.Linq.Expressions.Expression[] args = new System.Linq.Expressions.Expression[argCount + 1];
				args [0] = queryStatePrm;
				for (int i = 0; i < argCount; i++)
					args [i + 1] = ParseNewExpression (provider, functionCallTree.GetChild (i + 1));
				result = System.Linq.Expressions.Expression.Invoke (result, args);
			} catch {
				IExpression oldExpr = ParseExpressionFunctionCall (provider, functionCallTree);
				result = ExpressionBridge.Create (oldExpr, queryStatePrm);
			}
			
			return result;
		}

		void InitializeFunctionMap ()
		{
			// State
			FnAdd<string> ("GETCURDIR", (qs) => qs.CurrentDirectory);
			// Dates
			FnAdd<DateTime> ("GETDATE", (qs) => DateTime.Now);
			FnAdd<DateTime> ("GETUTCDATE", (qs) => DateTime.UtcNow);
			// Strings
			FnAdd<string, string> ("ESCAPEREGEX", (qs, s) => System.Text.RegularExpressions.Regex.Escape (s));
			FnAdd<string, string> ("LTRIM", (qs, s) => s.TrimStart ());
			FnAdd<string, string> ("RTRIM", (qs, s) => s.TrimEnd ());
			FnAdd<string, string> ("TRIM", (qs, s) => s.Trim ());
			FnAdd<string, long> ("LEN", (qs, s) => s.Length);
			FnAdd<string, string, bool> ("CONTAINS", (qs, s1, s2) => s1.IndexOf (s2, dataComparer.StringComparison) != -1);
			FnAdd<string, long, string> ("LEFT", (qs, s, l) => s.Substring (0, Math.Min ((int)l, s.Length)));
			FnAdd<string, long, string> ("RIGHT", (qs, s, l) => s.Substring (s.Length - Math.Min ((int)l, s.Length)));
			FnAdd<string, long, string> ("SUBSTRING", (qs, s, p) => s.Substring (Math.Min ((int)p, s.Length)));
			FnAdd<string, long, long, string> ("SUBSTRING", (qs, s, p, l) => s.Substring (Math.Min ((int)p, s.Length) - 1, Math.Min ((int)l, s.Length - Math.Min ((int)p, s.Length))));
			// Regex
			FnAdd<string, string, bool> (
				"MATCHREGEX", 
				(qs, s, p) => System.Text.RegularExpressions.Regex.IsMatch (
					s, p, dataComparer.CaseInsensitive ? 
						System.Text.RegularExpressions.RegexOptions.IgnoreCase : 
						System.Text.RegularExpressions.RegexOptions.None));
			/*case "DATEPART":
			result = UnaryExpression<DataDateTime, DataInteger>.CreateAutoConvert (
				(a) => DatePartHelper.Get ((arg1 as Token<DatePartType>).Value, a), arg2);
			break;		}*/
		}
	}
}
