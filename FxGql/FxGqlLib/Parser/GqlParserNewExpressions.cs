using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Reflection;
using System.Collections;

namespace FxGqlLib
{
	partial class GqlParser
	{
		System.Linq.Expressions.ParameterExpression queryStatePrm;

		System.Linq.Expressions.Expression ParseNewExpression (IProvider provider, ITree tree)
		{
			System.Linq.Expressions.Expression expression;
			switch (tree.Text.ToUpperInvariant ()) {
			case "T_INTEGER":
				expression = ParseNewExpressionInteger (tree);
				break;
			case "T_STRING":
				expression = ParseNewExpressionString (tree);
				break;
			case "T_CONVERT":
				expression = ParseNewExpressionConvert (provider, tree);
				break;
			case "T_OP_UNARY":
				expression = ParseNewExpressionOperatorUnary (provider, tree);
				break;
			case "T_OP_BINARY":
				expression = ParseNewExpressionOperatorBinary (provider, tree);
				break;
			case "T_CASE":
				expression = ParseNewExpressionCase (provider, tree);
				break;
			case "T_EXISTS":
				expression = ParseNewExpressionExists (tree);
				break;
			case "T_SUBQUERY":
				expression = ParseNewExpressionSubquery (provider, tree);
				break;
			case "T_VARIABLE":
				expression = ParseNewExpressionVariable (tree);
				break;
			case "T_SYSTEMVAR":
				expression = ParseNewExpressionSystemVar (tree);
				break;
			case "T_FUNCTIONCALL":
				expression = ParseNewExpressionFunctionCall (provider, tree);
				break;
			/*case "T_COLUMN":
				expression = ParseExpressionColumn (provider, tree);
				break;
			case "T_DATEPART":
				expression = ParseExpressionDatePart (tree);
				break;*/
			default:
				{
					IExpression oldExpr = ParseExpression (provider, tree);
					expression = ExpressionBridge.Create (oldExpr, queryStatePrm);
					break;
				}
			//throw new UnexpectedTokenAntlrException (tree);
			}
			
			return expression;
		}

		System.Linq.Expressions.Expression ParseNewExpressionInteger (ITree expressionNumberTree)
		{
			string text;
			if (expressionNumberTree.ChildCount == 1)
				text = expressionNumberTree.GetChild (0).Text;
			else
				text = expressionNumberTree.GetChild (0).Text + expressionNumberTree.GetChild (1).Text;
			return System.Linq.Expressions.Expression.Constant (long.Parse (text));
		}

		System.Linq.Expressions.Expression ParseNewExpressionString (ITree expressionStringTree)
		{
			ITree tree = GetSingleChild (expressionStringTree);
			
			string text = ParseString (tree);
			return System.Linq.Expressions.Expression.Constant (text);
		}

		System.Linq.Expressions.Expression ParseNewExpressionConvert (IProvider provider, ITree convertTree)
		{
			AssertAntlrToken (convertTree, "T_CONVERT", 2, 3);
			
			Type dataType = ParseDataType (convertTree.GetChild (0));
			System.Linq.Expressions.Expression expr = ParseNewExpression (
				provider,
				convertTree.GetChild (1)
			);
			
			string format;
			if (convertTree.ChildCount >= 3)
				format = ParseString (convertTree.GetChild (2));
			else
				format = null;

			return NewConvertExpression.Create (dataType, expr, format);
		}

		System.Linq.Expressions.Expression ParseNewExpressionOperatorUnary (IProvider provider, ITree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_UNARY", 2);
			
			System.Linq.Expressions.Expression arg = ParseNewExpression (
				provider,
				operatorTree.GetChild (1)
			);

			System.Linq.Expressions.ExpressionType op = GetUnaryExpressionType (operatorTree.GetChild (0));
			return System.Linq.Expressions.Expression.MakeUnary (op, arg, arg.Type);
		}

		System.Linq.Expressions.Expression ParseNewExpressionOperatorBinary (IProvider provider, ITree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3, 4);
			
			string operatorText = operatorTree.GetChild (0).Text;
			if (operatorText == "T_BETWEEN" || operatorText == "T_NOTBETWEEN") {
				return ParseNewExpressionBetween (provider, operatorTree, operatorText == "T_NOTBETWEEN");
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseNewExpressionInSomeAnyAll (provider, operatorTree);
			} 
			
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3);
			
			System.Linq.Expressions.Expression arg1 = ParseNewExpression (
				provider,
				operatorTree.GetChild (1)
			);          
			System.Linq.Expressions.Expression arg2 = ParseNewExpression (
				provider,
				operatorTree.GetChild (2)
			);          

			// FIX
			//AdjustAggregation (ref arg1, ref arg2);

			// Backward compatibility
			operatorText = operatorTree.GetChild (0).Text;
			switch (operatorText) {
			case "T_MATCH":
			case "T_NOTMATCH":
				return CreateMatchExpression (arg1, arg2, operatorText == "T_NOTMATCH");
			case "T_LIKE":
			case "T_NOTLIKE":
				return CreateLikeExpression (arg1, arg2, operatorText == "T_NOTLIKE");
			default:
				System.Linq.Expressions.ExpressionType op = GetBinaryExpressionType (operatorTree.GetChild (0));

				switch (op) {
				case System.Linq.Expressions.ExpressionType.Equal:
				case System.Linq.Expressions.ExpressionType.NotEqual:
				case System.Linq.Expressions.ExpressionType.LessThan:
				case System.Linq.Expressions.ExpressionType.GreaterThan:
				case System.Linq.Expressions.ExpressionType.GreaterThanOrEqual:
				case System.Linq.Expressions.ExpressionType.LessThanOrEqual:
					return CreateComparerExpression (op, arg1, arg2);
				default:
					return System.Linq.Expressions.Expression.MakeBinary (op, arg1, arg2);
				}
			}
		}

		System.Linq.Expressions.Expression CreateComparerExpression (
			System.Linq.Expressions.ExpressionType op, 
			System.Linq.Expressions.Expression arg1, 
			System.Linq.Expressions.Expression arg2)
		{
			//Special treatment for string comparison
			if (arg1.Type == typeof(string))
				return CreateStringComparerExpression (op, arg1, arg2);
			else
				return System.Linq.Expressions.Expression.MakeBinary (op, arg1, arg2);
		}

		static MethodInfo StringComparerCompareMethod = typeof(StringComparer).GetMethod (
			"Compare", new Type[] { typeof(string), typeof(string)});

		System.Linq.Expressions.Expression CreateStringComparerExpression (
			System.Linq.Expressions.ExpressionType op, 
		    System.Linq.Expressions.Expression arg1, 
		    System.Linq.Expressions.Expression arg2)
		{
			System.Linq.Expressions.Expression compareExpression =
				System.Linq.Expressions.Expression.Call (
					System.Linq.Expressions.Expression.Constant (this.dataComparer.StringComparer),
					StringComparerCompareMethod,
					arg1, arg2);

			return System.Linq.Expressions.Expression.MakeBinary (
				op, compareExpression, 
				System.Linq.Expressions.Expression.Constant (0));
		}

		static MethodInfo RegexIsMatchMethod = typeof(Regex).GetMethod (
			"IsMatch", new Type[] { typeof(string), typeof(string), typeof(RegexOptions)});

		System.Linq.Expressions.Expression CreateMatchExpression (
			System.Linq.Expressions.Expression arg1, 
			System.Linq.Expressions.Expression arg2, 
			bool not)
		{
			RegexOptions regexOptions = RegexOptions.None;
			if (dataComparer.CaseInsensitive)
				regexOptions |= RegexOptions.IgnoreCase;
			
			System.Linq.Expressions.Expression expr =
				System.Linq.Expressions.Expression.Call (
					RegexIsMatchMethod, 
					arg1,
					arg2,
					System.Linq.Expressions.Expression.Constant (regexOptions));
			
			if (not)
				expr = System.Linq.Expressions.Expression.Not (expr);
			
			return expr;
		}

		static MethodInfo RegexEscapeMethod = typeof(Regex).GetMethod (
			"Escape", new Type[] { typeof(string) });
		static MethodInfo StringConcatMethod = typeof(String).GetMethod (
			"Concat", new Type[] { typeof(string), typeof(string), typeof(string) });
		static MethodInfo StringReplaceStringMethod = typeof(String).GetMethod (
			"Replace", new Type[] { typeof(string), typeof(string) });
		static MethodInfo StringReplaceCharMethod = typeof(String).GetMethod (
			"Replace", new Type[] { typeof(char), typeof(char) });

		System.Linq.Expressions.Expression CreateLikeExpression (
			System.Linq.Expressions.Expression arg1, 
			System.Linq.Expressions.Expression arg2, 
			bool not)
		{
			System.Linq.Expressions.Expression expr = 
				System.Linq.Expressions.Expression.Call (RegexEscapeMethod, arg2);

			expr = 
				System.Linq.Expressions.Expression.Call (
					expr,
					StringReplaceCharMethod, 
					System.Linq.Expressions.Expression.Constant ('_'),
					System.Linq.Expressions.Expression.Constant ('.'));
			
			expr = 
				System.Linq.Expressions.Expression.Call (
					expr,
					StringReplaceStringMethod, 
					System.Linq.Expressions.Expression.Constant ("%"),
					System.Linq.Expressions.Expression.Constant (".*"));

			expr = 
				System.Linq.Expressions.Expression.Call (
					StringConcatMethod, 
					System.Linq.Expressions.Expression.Constant ("^"),
					expr,
					System.Linq.Expressions.Expression.Constant ("$"));

			return CreateMatchExpression (arg1, expr, not);
		}

		System.Linq.Expressions.Expression CreateBetweenExpression (
			System.Linq.Expressions.Expression arg1, 
			System.Linq.Expressions.Expression arg2, 
			System.Linq.Expressions.Expression arg3, 
			bool not)
		{
			System.Linq.Expressions.Expression expr = 
				System.Linq.Expressions.Expression.AndAlso (
					CreateComparerExpression (
						System.Linq.Expressions.ExpressionType.GreaterThanOrEqual,
						arg1, arg2),
					CreateComparerExpression (
						System.Linq.Expressions.ExpressionType.LessThanOrEqual,
						arg1, arg3)
			);

			if (not)
				expr = System.Linq.Expressions.Expression.Not (expr);
			
			return expr;
		}

		System.Linq.Expressions.ExpressionType GetUnaryExpressionType (ITree operatorTree)
		{
			string operatorText = operatorTree.Text;
			switch (operatorText) {
			case "T_NOT":
				return System.Linq.Expressions.ExpressionType.Not;
			case "T_PLUS":
				return System.Linq.Expressions.ExpressionType.UnaryPlus;
			case "T_MINUS":
				return System.Linq.Expressions.ExpressionType.Negate;
			case "T_BITWISE_NOT":
				return System.Linq.Expressions.ExpressionType.Not;
			default:
				throw new ParserException (
					string.Format ("Unknown unary operator '{0}'.", operatorText),
					operatorTree
				);
			}
		}

		System.Linq.Expressions.ExpressionType GetBinaryExpressionType (ITree operatorTree)
		{
			string operatorText = operatorTree.Text;
			switch (operatorText) {
			case "T_AND":
				return System.Linq.Expressions.ExpressionType.AndAlso;
			case "T_OR":
				return System.Linq.Expressions.ExpressionType.OrElse;
			case "T_PLUS":
				return System.Linq.Expressions.ExpressionType.Add;
			case "T_MINUS":
				return System.Linq.Expressions.ExpressionType.Subtract;
			case "T_DIVIDE":
				return System.Linq.Expressions.ExpressionType.Divide;
			case "T_PRODUCT":
				return System.Linq.Expressions.ExpressionType.Multiply;
			case "T_MODULO":
				return System.Linq.Expressions.ExpressionType.Modulo;
			case "T_BITWISE_AND":
				return System.Linq.Expressions.ExpressionType.And;
			case "T_BITWISE_OR":
				return System.Linq.Expressions.ExpressionType.Or;
			case "T_BITWISE_XOR":
				return System.Linq.Expressions.ExpressionType.ExclusiveOr;
			case "T_EQUAL":
				return System.Linq.Expressions.ExpressionType.Equal;
			case "T_NOTEQUAL":
				return System.Linq.Expressions.ExpressionType.NotEqual;
			case "T_LESS":
				return System.Linq.Expressions.ExpressionType.LessThan;
			case "T_GREATER":
				return System.Linq.Expressions.ExpressionType.GreaterThan;
			case "T_NOTLESS":
				return System.Linq.Expressions.ExpressionType.GreaterThanOrEqual;
			case "T_NOTGREATER":
				return System.Linq.Expressions.ExpressionType.LessThanOrEqual;
			default:
				throw new ParserException (
					string.Format ("Unknown binary operator '{0}'.", operatorText),
					operatorTree
				);
			}
		}

		System.Linq.Expressions.Expression ParseNewExpressionBetween (IProvider provider, ITree betweenTree, bool not)
		{
			AssertAntlrToken (betweenTree, "T_OP_BINARY", 3);
			//AssertAntlrToken (betweenTree.Children [0], "T_BETWEEN"); or T_NOTBETWEEN
			ITree andTree = betweenTree.GetChild (2);
			AssertAntlrToken (andTree, "T_OP_BINARY", 3);
			AssertAntlrToken (andTree.GetChild (0), "T_AND");
			
			System.Linq.Expressions.Expression arg1 = ParseNewExpression (
				provider,
				betweenTree.GetChild (1)
			);
			System.Linq.Expressions.Expression arg2 = ParseNewExpression (
				provider,
				andTree.GetChild (1)
			);
			System.Linq.Expressions.Expression arg3 = ParseNewExpression (
				provider,
				andTree.GetChild (2)
			);
			
			//AdjustAggregation (ref arg1, ref arg2, ref arg3);

			return CreateBetweenExpression (arg1, arg2, arg3, not);
		}

		System.Linq.Expressions.Expression ParseNewExpressionInSomeAnyAll (IProvider provider, ITree inTree)
		{
			AssertAntlrToken (inTree, "T_OP_BINARY", 3, 4);
			//AssertAntlrToken (inTree.Children [0], "T_IN"); or T_NOTIN, T_ANY, T_ALL
			
			System.Linq.Expressions.Expression arg2;
			ITree target;          
			bool all;
			bool not;
			System.Linq.Expressions.ExpressionType op;
			switch (inTree.GetChild (0).Text) {
			case "T_IN":
			case "T_NOTIN":
				arg2 = ParseNewExpression (provider, inTree.GetChild (1));
				target = inTree.GetChild (2);
				all = false;
				not = inTree.GetChild (0).Text == "T_NOTIN";
				op = System.Linq.Expressions.ExpressionType.Equal;
				break;
			case "T_ANY":
				arg2 = ParseNewExpression (provider, inTree.GetChild (2));
				target = inTree.GetChild (3);
				all = false;
				not = false;
				op = GetBinaryExpressionType (inTree.GetChild (1));
				break;
			case "T_ALL":
				arg2 = ParseNewExpression (provider, inTree.GetChild (2));
				target = inTree.GetChild (3);
				all = true;
				not = false;
				op = GetBinaryExpressionType (inTree.GetChild (1));
				break;
			default:
				throw new ParserException (
					string.Format ("Unexpected token {0}", inTree.GetChild (0).Text),
					inTree.GetChild (0)
				);
			}

			System.Linq.Expressions.Expression result;
			if (target.Text == "T_EXPRESSIONLIST") {
				System.Linq.Expressions.Expression[] expressionList = ParseNewExpressionList (provider, target);
				result = CreateAnyListExpression (arg2, expressionList, op, all, not);
			} else if (target.Text == "T_SELECT") {
				IProvider subProvider = ParseInnerSelect (null, target);
				result = CreateAnySubqueryExpression (arg2, subProvider, op, all, not);
			} else {
				throw new ParserException (
					string.Format (
					"Binary operator '{0}' cannot be used with argument {1}",
					inTree.GetChild (0).Text,
					target.Text
				),
					target
				);
			}
			
			return result;
		}

		System.Linq.Expressions.Expression CreateAnyListExpression (
			System.Linq.Expressions.Expression arg2, 
			System.Linq.Expressions.Expression[] expressionList, 
			System.Linq.Expressions.ExpressionType op,
			bool all, bool not)
		{
			System.Linq.Expressions.ExpressionType aggregator;
			if (all)
				aggregator = System.Linq.Expressions.ExpressionType.AndAlso;
			else
				aggregator = System.Linq.Expressions.ExpressionType.OrElse;

			System.Linq.Expressions.Expression expr = null;
			foreach (var expressionListItem in expressionList.Reverse ()) {
				System.Linq.Expressions.Expression item = CreateComparerExpression (op, arg2, expressionListItem);
				if (expr == null) {
					expr = item;
				} else {
					expr = System.Linq.Expressions.Expression.MakeBinary (aggregator, item, expr);
				}
			}

			if (expr == null) {
				expr = System.Linq.Expressions.Expression.Constant (false);
			}

			if (not)
				expr = System.Linq.Expressions.Expression.Not (expr);

			return expr;
		}

		static MethodInfo GetValuesFromSubqueryMethod = typeof(GqlParser).GetMethod (
			"GetValuesFromSubquery", BindingFlags.Static | BindingFlags.NonPublic, 
			null,
			new Type[] { typeof(IProvider), typeof(GqlQueryState) },
			null);

		System.Linq.Expressions.Expression CreateAnySubqueryExpression (System.Linq.Expressions.Expression arg, IProvider provider, System.Linq.Expressions.ExpressionType op, bool all, bool not)
		{
			Type[] types = provider.GetColumnTypes ();
			if (types.Length != 1) 
				throw new InvalidOperationException ("Subquery should contain only 1 column");

			Type type = ExpressionBridge.GetNewType (types [0]);

			PropertyInfo ListCountProperty = typeof(ICollection<>).MakeGenericType (type).GetProperty ("Count");

			Type listType = typeof(IList<>).MakeGenericType (type);
			System.Linq.Expressions.ParameterExpression valuesVariable =
				System.Linq.Expressions.Expression.Variable (listType, "ValueList");
			System.Linq.Expressions.LabelTarget returnTarget = 
				System.Linq.Expressions.Expression.Label (typeof(bool));

			System.Linq.Expressions.Expression expr =
				System.Linq.Expressions.Expression.Block (
					typeof(bool), 
					new System.Linq.Expressions.ParameterExpression[] {
						valuesVariable
					},
					new System.Linq.Expressions.Expression[] {
						System.Linq.Expressions.Expression.Assign (
							valuesVariable, 
						    GetCachedExpression (
								System.Linq.Expressions.Expression.Call (
									GetValuesFromSubqueryMethod.MakeGenericMethod (type),
									System.Linq.Expressions.Expression.Constant (provider),
									this.queryStatePrm))),

						System.Linq.Expressions.Expression.IfThen (
							System.Linq.Expressions.Expression.Equal (
								System.Linq.Expressions.Expression.Property (valuesVariable, ListCountProperty),
								System.Linq.Expressions.Expression.Constant (0)),
							System.Linq.Expressions.Expression.Return (returnTarget, System.Linq.Expressions.Expression.Constant (false))),

						System.Linq.Expressions.Expression.Return (
							returnTarget,
							CreateCheckCollectionExpression (type, valuesVariable, arg, op, all)),

						System.Linq.Expressions.Expression.Label (
							returnTarget, 
							System.Linq.Expressions.Expression.Constant (false))
				}
			);

			if (not)
				expr = System.Linq.Expressions.Expression.Not (expr);
			
			return expr;
		}

		static MethodInfo EnumeratorMoveNextMethod = typeof(IEnumerator).GetMethod ("MoveNext");

		System.Linq.Expressions.Expression CreateCheckCollectionExpression (
			Type type,
			System.Linq.Expressions.ParameterExpression valuesVariable, 
			System.Linq.Expressions.Expression arg, 
			System.Linq.Expressions.ExpressionType op,
			bool all)
		{
			//return System.Linq.Expressions.Expression.Constant (true);
			MethodInfo EnumerableGetEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType (type).GetMethod (
				"GetEnumerator");
			PropertyInfo EnumeratorCurrentProperty = typeof(IEnumerator<>).MakeGenericType (type).GetProperty (
				"Current");
			Type enumeratorType = typeof(IEnumerator<>).MakeGenericType (type);
			System.Linq.Expressions.ParameterExpression enumeratorVariable =
				System.Linq.Expressions.Expression.Variable (enumeratorType);

			System.Linq.Expressions.LabelTarget returnTarget = 
				System.Linq.Expressions.Expression.Label (typeof(bool));

			System.Linq.Expressions.Expression compareExpression =
				CreateComparerExpression (
					op,
					arg,
					System.Linq.Expressions.Expression.Property (
						enumeratorVariable,
						EnumeratorCurrentProperty));

			System.Linq.Expressions.Expression bodyExpression;
			if (all) {
				bodyExpression = System.Linq.Expressions.Expression.IfThen (
					System.Linq.Expressions.Expression.Not (compareExpression),
					System.Linq.Expressions.Expression.Return (
						returnTarget,
						System.Linq.Expressions.Expression.Constant (false)));
			} else {
				bodyExpression = System.Linq.Expressions.Expression.IfThen (
					compareExpression,
					System.Linq.Expressions.Expression.Return (
						returnTarget,
						System.Linq.Expressions.Expression.Constant (true)));
			}

			bodyExpression = 
				System.Linq.Expressions.Expression.IfThenElse (
					System.Linq.Expressions.Expression.Call (
						enumeratorVariable,
						EnumeratorMoveNextMethod),
					bodyExpression,
					System.Linq.Expressions.Expression.Return (returnTarget, System.Linq.Expressions.Expression.Constant (all)));

			System.Linq.Expressions.Expression expr =
				System.Linq.Expressions.Expression.Block (
					typeof(bool), 
					new System.Linq.Expressions.ParameterExpression[] {
					enumeratorVariable
				},
				new System.Linq.Expressions.Expression[] {
					System.Linq.Expressions.Expression.Assign (
						enumeratorVariable, 
						System.Linq.Expressions.Expression.Call (
							valuesVariable,
							EnumerableGetEnumeratorMethod)),

					System.Linq.Expressions.Expression.Loop (bodyExpression),

			// When we arrive here,
			//    When "all", ==> all compare expressions were true  --> return true
			//    otherwise   ==> none of the compare expressions were true --> return false
					System.Linq.Expressions.Expression.Label (
						returnTarget, 
						System.Linq.Expressions.Expression.Constant (all))
				}
			);

			return expr;
		}

		static List<T> GetValuesFromSubquery<T> (IProvider provider, GqlQueryState gqlQueryState)
		{
			try {
				provider.Initialize (gqlQueryState);
				
				List<T> values = new List<T> ();
				while (provider.GetNextRecord()) {
					values.Add (ExpressionBridge.ConvertFromOld<T> (provider.Record.Columns [0]));
				}
				
				return values;
			} finally {
				provider.Uninitialize ();
			}
		}
		
		static T GetValueFromSubquery<T> (IProvider provider, GqlQueryState gqlQueryState)
		{
			try {
				provider.Initialize (gqlQueryState);

				return ExpressionBridge.ConvertFromOld<T> (provider.Record.Columns [0]);
			} finally {
				provider.Uninitialize ();
			}
		}
		
		System.Linq.Expressions.Expression[] ParseNewExpressionList (IProvider provider, ITree expressionListTree)
		{
			AssertAntlrToken (expressionListTree, "T_EXPRESSIONLIST", 1, -1);
			
			System.Linq.Expressions.Expression[] result = new System.Linq.Expressions.Expression[expressionListTree.ChildCount];
			for (int i = 0; i < expressionListTree.ChildCount; i++) {
				result [i] = ParseNewExpression (
					provider,
					expressionListTree.GetChild (i)
				);
			}           
			
			return result;
		}

		MethodInfo GqlQueryStateGetCacheMethod = typeof(GqlQueryState).GetMethod ("GetCache");
		MethodInfo GqlQueryStateSetCacheMethod = typeof(GqlQueryState).GetMethod ("SetCache");

		System.Linq.Expressions.Expression GetCachedExpression (System.Linq.Expressions.Expression creationExpr)
		{
			Guid cacheKey = Guid.NewGuid ();
			Type type = creationExpr.Type;

			System.Linq.Expressions.ParameterExpression cacheVariable =
				System.Linq.Expressions.Expression.Variable (type, "Cache");

			System.Linq.Expressions.Expression expr =
				System.Linq.Expressions.Expression.Block (
					type, 
					new System.Linq.Expressions.ParameterExpression[] {
					cacheVariable
				},
				new System.Linq.Expressions.Expression[] {
					System.Linq.Expressions.Expression.Assign (
						cacheVariable, 

						System.Linq.Expressions.Expression.Convert (
							System.Linq.Expressions.Expression.Call (
								queryStatePrm,
								GqlQueryStateGetCacheMethod,
								System.Linq.Expressions.Expression.Constant (cacheKey)),
							type)),

					System.Linq.Expressions.Expression.IfThen (
						System.Linq.Expressions.Expression.Equal (
							cacheVariable,
							System.Linq.Expressions.Expression.Constant (null)),

						System.Linq.Expressions.Expression.Block (
							type,
							new System.Linq.Expressions.Expression[] {
								System.Linq.Expressions.Expression.Assign (
									cacheVariable, 
									creationExpr),

								System.Linq.Expressions.Expression.Call (
									queryStatePrm,
									GqlQueryStateSetCacheMethod,
									System.Linq.Expressions.Expression.Constant (cacheKey),
									cacheVariable),

								cacheVariable
							})),

					cacheVariable
				});

			return expr;
		}

		System.Linq.Expressions.Expression ParseNewExpressionCase (IProvider provider, ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_CASE", 1, -1);
			
			List<Tuple<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression>> whenItems = 
				new List<Tuple<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression>> ();
			System.Linq.Expressions.Expression elseResult;

			string text = expressionTree.GetChild (0).Text;
			if (text != "T_CASE_WHEN" && text != "T_CASE_ELSE") {
				// CASE source WHEN destination THEN target ELSE other END
				System.Linq.Expressions.Expression source = ParseNewExpression (
					provider,
					expressionTree.GetChild (0)
				);

				Type resultType = null;
				int whenNo;
				for (whenNo = 1; expressionTree.GetChild(whenNo).Text == "T_CASE_WHEN"; whenNo++) {
					ITree whenTree = expressionTree.GetChild (whenNo);
					System.Linq.Expressions.Expression destination = ParseNewExpression (
						provider,
						whenTree.GetChild (0)
					);
					System.Linq.Expressions.Expression target = ParseNewExpression (
						provider,
						whenTree.GetChild (1)
					);
					if (whenNo == 1)
						resultType = target.Type;

					System.Linq.Expressions.Expression testExpr = CreateComparerExpression (
						System.Linq.Expressions.ExpressionType.Equal, source, destination);
					whenItems.Add (Tuple.Create (testExpr, target));
				}
				
				if (whenNo < expressionTree.ChildCount - 1)
					throw new Exception ("Invalid case statement");

				if (whenNo == expressionTree.ChildCount - 1) {
					ITree elseTree = expressionTree.GetChild (whenNo);
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
					
					elseResult = ParseNewExpression (provider, elseTree.GetChild (0));
				} else {
					elseResult = GetNullValue (resultType);
				}
			} else {
				// CASE WHEN a THEN x ELSE y END
				Type resultType = null;
				int whenNo;
				for (whenNo = 0; expressionTree.GetChild(whenNo).Text == "T_CASE_WHEN"; whenNo++) {
					ITree whenTree = expressionTree.GetChild (whenNo);
					System.Linq.Expressions.Expression testExpr = ParseNewExpression (
						provider,
						whenTree.GetChild (0)
					);
					System.Linq.Expressions.Expression target = ParseNewExpression (
						provider,
						whenTree.GetChild (1)
					);

					if (whenNo == 1)
						resultType = target.Type;
					
					whenItems.Add (Tuple.Create (testExpr, target));
				}
				
				if (whenNo < expressionTree.ChildCount - 1)
					throw new Exception ("Invalid case statement");
				
				if (whenNo == expressionTree.ChildCount - 1) {
					ITree elseTree = expressionTree.GetChild (whenNo);
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
					
					elseResult = ParseNewExpression (provider, elseTree.GetChild (0));
				} else {
					elseResult = GetNullValue (resultType);
				}
			}

			System.Linq.Expressions.Expression expr = elseResult;
			foreach (var item in 
			         ((IEnumerable<Tuple<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression>>)whenItems).Reverse ()) {
				expr = System.Linq.Expressions.Expression.IfThenElse (item.Item1, item.Item2, expr);
			}

			return expr;
		}
		
		System.Linq.Expressions.Expression ParseNewExpressionExists (ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_EXISTS", 1, 1);

			IProvider subProvider = new ColumnProvider (
				new IExpression[] { new ConstExpression<DataInteger> (1) }, 
				new TopProvider (
					ParseInnerSelect (null, expressionTree.GetChild (0)), new ConstExpression<DataInteger> (1)));

			return CreateAnySubqueryExpression (
				System.Linq.Expressions.Expression.Constant (1), 

				subProvider, System.Linq.Expressions.ExpressionType.Equal, false, false);
		}

		static MethodInfo GetValueFromSubqueryMethod = typeof(GqlParser).GetMethod (
			"GetValueFromSubquery", BindingFlags.Static | BindingFlags.NonPublic, 
			null,
			new Type[] { typeof(IProvider), typeof(GqlQueryState) },
		null);

		System.Linq.Expressions.Expression ParseNewExpressionSubquery (IProvider parentProvider, ITree subqueryTree)
		{
			IProvider provider = ParseSubquery (parentProvider, subqueryTree);
			provider = new TopProvider (provider, new ConstExpression<DataInteger> (1));

			Type[] types = provider.GetColumnTypes ();
			if (types.Length != 1) 
				throw new InvalidOperationException ("Subquery should contain only 1 column");
			
			Type type = ExpressionBridge.GetNewType (types [0]);

			return System.Linq.Expressions.Expression.Call (
				GetValueFromSubqueryMethod.MakeGenericMethod (type),
				System.Linq.Expressions.Expression.Constant (provider),
				this.queryStatePrm);
		}

		static MethodInfo GetVariableValueMethod = typeof(GqlParser).GetMethod (
			"GetVariableValue", BindingFlags.Static | BindingFlags.NonPublic, 
			null,
			new Type[] { typeof(GqlQueryState), typeof(string) },
			null);
		
		System.Linq.Expressions.Expression ParseNewExpressionVariable (ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_VARIABLE", 1, 1);
			
			string variableName = expressionTree.GetChild (0).Text;

			Type type;
			if (!variableTypes.TryGetValue (variableName, out type)) {
				Variable variable;
				if (!gqlEngineState.Variables.TryGetValue (variableName, out variable))
					throw new ParserException (string.Format ("Variable {0} not declared", variable), expressionTree);
				type = variable.Type;
			}

			type = ExpressionBridge.GetNewType (type);

			MethodInfo GetVariableValueMethodSpecialized = GetVariableValueMethod.MakeGenericMethod (type);

			return System.Linq.Expressions.Expression.Call (
					GetVariableValueMethodSpecialized,
					queryStatePrm,
					System.Linq.Expressions.Expression.Constant (variableName));
		}

		static T GetVariableValue<T> (GqlQueryState gqlQueryState, string variableName)
		{
			Variable variable;
			if (!gqlQueryState.Variables.TryGetValue (variableName, out variable))
				throw new InvalidOperationException (string.Format ("Variable '{0}' not declared", variableName));

			return ExpressionBridge.ConvertFromOld<T> (variable.Value);
		}

		static PropertyInfo GqlQueryStateRecordProperty = typeof(GqlQueryState).GetProperty ("Record");
		static PropertyInfo GqlQueryStateUseOriginalColumnsProperty = typeof(GqlQueryState).GetProperty ("UseOriginalColumns");
		static PropertyInfo ProviderRecordLineNoProperty = typeof(ProviderRecord).GetProperty ("LineNo");
		static PropertyInfo ProviderRecordTotalLineNoProperty = typeof(ProviderRecord).GetProperty ("TotalLineNo");
		static PropertyInfo ProviderRecordSourceProperty = typeof(ProviderRecord).GetProperty ("Source");
		static MethodInfo PathGetFileNameMethod = typeof(System.IO.Path).GetMethod ("GetFileName");
		static MethodInfo ProviderRecordGetLineMethod = typeof(ProviderRecord).GetMethod ("GetLine");

		System.Linq.Expressions.Expression ParseNewExpressionSystemVar (ITree expressionSystemVarTree)
		{
			ITree tree = GetSingleChild (expressionSystemVarTree);
			
			System.Linq.Expressions.Expression expression;
			switch (tree.Text.ToUpperInvariant ()) {
			case "$LINE":
				expression = 
					System.Linq.Expressions.Expression.Call (
						System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateRecordProperty),
						ProviderRecordGetLineMethod,
						System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateUseOriginalColumnsProperty));
				break;
			case "$LINENO":
				expression = 
					System.Linq.Expressions.Expression.Property (
						System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateRecordProperty),
						ProviderRecordLineNoProperty);
				break;
			case "$TOTALLINENO":
				expression = 
					System.Linq.Expressions.Expression.Property (
						System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateRecordProperty),
						ProviderRecordTotalLineNoProperty);
				break;
			case "$FILENAME":
				expression = 
					System.Linq.Expressions.Expression.Call (
						PathGetFileNameMethod,
						System.Linq.Expressions.Expression.Property (
							System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateRecordProperty),
							ProviderRecordSourceProperty));
				break;
			case "$FULLFILENAME":
				expression = 
					System.Linq.Expressions.Expression.Property (
						System.Linq.Expressions.Expression.Property (
							queryStatePrm, GqlQueryStateRecordProperty),
						ProviderRecordSourceProperty);
				break;
			default:
				IExpression oldExpr = ParseExpressionSystemVar (expressionSystemVarTree);
				expression = ExpressionBridge.Create (oldExpr, queryStatePrm);
				break;
//				throw new ParserException (
//					string.Format ("Unknown system variable '{0}'.", tree.Text),
//					tree
//				);
			}
			
			return expression;
		}
		
		System.Linq.Expressions.Expression GetNullValue (Type resultType)
		{
			return System.Linq.Expressions.Expression.Default (resultType);
		}
	}
}

