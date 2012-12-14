using System;
using System.Collections.Generic;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Reflection;

namespace FxGqlLib
{
	partial class GqlParser
	{
		delegate System.Linq.Expressions.Expression ExpressionBuilder (System.Linq.Expressions.Expression[] args);
		List<Dictionary<string, ExpressionBuilder>> functionMap = 
			new List<Dictionary<string, ExpressionBuilder>> ();

		void FnAdd (string functionName, int prmCount, System.Linq.Expressions.Expression function)
		{
			ExpressionBuilder del = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return System.Linq.Expressions.Expression.Invoke (function, args);
			});

			FnAdd (functionName, prmCount, del);
		}

		void FnAdd (string functionName, int prmCount, ExpressionBuilder del)
		{
			for (int i = functionMap.Count; i <= prmCount; i++)
				functionMap.Add (new Dictionary<string, ExpressionBuilder> ());
			
			functionMap [prmCount].Add (functionName, del);
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

		void FnAdd (string functionName, Delegate del)
		{
			int prmCount = del.Method.GetParameters ().Length;
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return (System.Linq.Expressions.Expression)del.DynamicInvoke (args);
			});
			FnAdd (functionName, prmCount, expressionBuilder);
		}
		
		void FnAdd (
			string functionName, 
			Func<System.Linq.Expressions.Expression> func)
		{
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return func ();
			});
			FnAdd (functionName, 0, expressionBuilder);
		}
		
		void FnAdd (
			string functionName, 
			Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression> func)
		{
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return func (args [0]);
			});
			FnAdd (functionName, 1, expressionBuilder);
		}
		
		void FnAdd (
			string functionName, 
			Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression> func)
		{
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return func (args [0], args [1]);
			});
			FnAdd (functionName, 2, expressionBuilder);
		}
		
		void FnAdd (
			string functionName, 
			Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression> func)
		{
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return func (args [0], args [1], args [2]);
			});
			FnAdd (functionName, 3, expressionBuilder);
		}

		void FnAdd (
			string functionName, 
			Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Linq.Expressions.Expression> func)
		{
			ExpressionBuilder expressionBuilder = new ExpressionBuilder (
				delegate(System.Linq.Expressions.Expression[] args) {
				return func (args [0], args [1], args [2], args [3]);
			});
			FnAdd (functionName, 4, expressionBuilder);
		}

		ExpressionBuilder FnGetExpressionBuilder (string functionName, int argCount)
		{
			ExpressionBuilder expressionBuilder = null;
			if (argCount < functionMap.Count) {
				var functionMapItem = functionMap [argCount];
				functionMapItem.TryGetValue (functionName.ToUpperInvariant (), out expressionBuilder);
			}

			if (expressionBuilder == null)
				throw new NotSupportedException (
					string.Format ("Function '{0}' with {1} arguments does not exist.", functionName, argCount));

			return expressionBuilder;
		}

		System.Linq.Expressions.Expression ParseNewExpressionFunctionCall (IProvider provider, ITree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL", 1, -1);
			
			string functionName = functionCallTree.GetChild (0).Text;
			int argCount = functionCallTree.ChildCount - 1;
			
			System.Linq.Expressions.Expression result;
			ExpressionBuilder expressionBuilder = FnGetExpressionBuilder (functionName, argCount);
			System.Linq.Expressions.Expression[] args = new System.Linq.Expressions.Expression[argCount + 1];
			args [0] = queryStatePrm;
			for (int i = 0; i < argCount; i++)
				args [i + 1] = ParseNewExpression (provider, functionCallTree.GetChild (i + 1));
			result = expressionBuilder (args);
			
			return result;
		}

		void InitializeFunctionMap ()
		{
			// State
			FnAdd<string> ("GETCURDIR", (qs) => qs.CurrentDirectory);
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
			FnAdd ("MATCHREGEX", BuildMatchRegexExpression);
			FnAdd ("MATCHREGEX", BuildMatchRegexExpression2);
			FnAdd ("MATCHREGEX", BuildMatchRegexExpression3);
			FnAdd<string, string, string, string> ("REPLACE", (qs, s, p, r) => System.Text.RegularExpressions.Regex.Replace (s, p, r));
			FnAdd<string, string, string, string> ("REPLACEREGEX", (qs, s, p, r) => System.Text.RegularExpressions.Regex.Replace (s, p, r));
			// Dates
			FnAdd<DateTime> ("GETDATE", (qs) => DateTime.Now);
			FnAdd<DateTime> ("GETUTCDATE", (qs) => DateTime.UtcNow);
			FnAdd<DatePartType, DateTime, long> ("DATEPART", (qs, dp, dt) => DatePartHelper.Get (dp, dt));
			FnAdd<DatePartType, long, DateTime, DateTime> ("DATEADD", (qs, dp, add, dt) => DatePartHelper.Add (dp, add, dt));
			FnAdd<DatePartType, DateTime, DateTime, long> ("DATEDIFF", (qs, dp, dt1, dt2) => DatePartHelper.Diff (dp, dt1, dt2));
		}

		static MethodInfo RegexMatchMethod = typeof(System.Text.RegularExpressions.Regex).GetMethod (
			"Match", BindingFlags.Static | BindingFlags.Public, 
			null,
			new Type[] { typeof(string), typeof(string), typeof(System.Text.RegularExpressions.RegexOptions) },
			null);
		static PropertyInfo MatchSuccessProperty = typeof(System.Text.RegularExpressions.Match).GetProperty ("Success");
		static PropertyInfo GqlQueryStateSkipLineProperty = typeof(GqlQueryState).GetProperty ("SkipLine");

		System.Linq.Expressions.Expression BuildMatchRegexExpression2 (
			System.Linq.Expressions.Expression input, 
			System.Linq.Expressions.Expression pattern)
		{
			return BuildMatchRegexExpression (input, pattern, null, null);
		}
		
		System.Linq.Expressions.Expression BuildMatchRegexExpression3 (
			System.Linq.Expressions.Expression input, 
			System.Linq.Expressions.Expression pattern, 
			System.Linq.Expressions.Expression extract)
		{
			return BuildMatchRegexExpression (input, pattern, extract, null);
		}
		
		System.Linq.Expressions.Expression BuildMatchRegexExpression (
			System.Linq.Expressions.Expression input, 
			System.Linq.Expressions.Expression pattern, 
			System.Linq.Expressions.Expression extract,
			System.Linq.Expressions.Expression def)
		{
			System.Linq.Expressions.ParameterExpression matchVariable =
				System.Linq.Expressions.Expression.Variable (typeof(System.Text.RegularExpressions.Match), "Match");
			System.Linq.Expressions.ParameterExpression extractVariable =
				System.Linq.Expressions.Expression.Variable (typeof(string), "Extract");

			System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.CultureInvariant;
			if (dataComparer.CaseInsensitive)
				options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;

			System.Linq.Expressions.Expression successExpr;
			if (extract != null) {
				System.Linq.Expressions.Expression<Func<System.Text.RegularExpressions.Match, string, string>> func = 
					(m, x) => m.Result (x);
				successExpr = System.Linq.Expressions.Expression.Lambda<Func<System.Text.RegularExpressions.Match, string, string>> (
					func, matchVariable, extractVariable);
			} else {
				System.Linq.Expressions.Expression<Func<System.Text.RegularExpressions.Match, string>> func = 
					(m) => m.Groups [m.Groups.Count > 1 ? 1 : 0].Value;
				successExpr = System.Linq.Expressions.Expression.Lambda<Func<System.Text.RegularExpressions.Match, string>> (
					func, matchVariable);
			}

			System.Linq.Expressions.Expression failExpr;
			if (def != null) {
				failExpr = def;
			} else {
				failExpr =
					System.Linq.Expressions.Expression.Block (
						System.Linq.Expressions.Expression.IfThen (
							System.Linq.Expressions.Expression.ReferenceNotEqual (
								queryStatePrm,
								System.Linq.Expressions.Expression.Constant (null)),
							System.Linq.Expressions.Expression.Assign (
								System.Linq.Expressions.Expression.Property (queryStatePrm, GqlQueryStateSkipLineProperty),
								System.Linq.Expressions.Expression.Constant (true))),
						System.Linq.Expressions.Expression.Constant (""));
			}
			System.Linq.Expressions.Expression expr =
				System.Linq.Expressions.Expression.Block (
				typeof(bool), 
				new System.Linq.Expressions.ParameterExpression[] {
					matchVariable,
					extractVariable
				},
				System.Linq.Expressions.Expression.Assign (
					matchVariable,
					System.Linq.Expressions.Expression.Call (
						RegexMatchMethod, input, pattern, System.Linq.Expressions.Expression.Constant (options))),
				System.Linq.Expressions.Expression.Assign (matchVariable, extract),
				System.Linq.Expressions.Expression.IfThenElse (
					System.Linq.Expressions.Expression.Property (matchVariable, MatchSuccessProperty),
					successExpr,
					failExpr));

			return expr;
		}
	}
}
