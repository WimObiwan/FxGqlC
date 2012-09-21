using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Reflection;

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
			/*case "T_SYSTEMVAR":
				expression = ParseExpressionSystemVar (tree);
				break;
			case "T_FUNCTIONCALL":
				expression = ParseExpressionFunctionCall (provider, tree);
				break;
			case "T_EXISTS":
				expression = ParseExpressionExists (tree);
				break;
			case "T_COLUMN":
				expression = ParseExpressionColumn (provider, tree);
				break;
			case "T_CASE":
				expression = ParseExpressionCase (provider, tree);
				break;
			case "T_VARIABLE":
				expression = ParseExpressionVariable (tree);
				break;
			case "T_SUBQUERY":
				expression = ParseExpressionSubquery (provider, tree);
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

			System.Linq.Expressions.ExpressionType op = GetUnaryExpressionType (operatorTree);
			return System.Linq.Expressions.Expression.MakeUnary (op, arg, arg.Type);
		}

		System.Linq.Expressions.Expression ParseNewExpressionOperatorBinary (IProvider provider, ITree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3, 4);
			
			string operatorText = operatorTree.GetChild (0).Text;
			if (operatorText == "T_BETWEEN") {
				IExpression oldExpr = ParseExpressionBetween (provider, operatorTree);
				return ExpressionBridge.Create (oldExpr, queryStatePrm);
			} else if (operatorText == "T_NOTBETWEEN") {
				IExpression oldExpr = 
					UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert (
					(a) => !a,
					ParseExpressionBetween (provider, operatorTree)
				);
				return ExpressionBridge.Create (oldExpr, queryStatePrm);
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				IExpression oldExpr = 
					ParseExpressionInSomeAnyAll (provider, operatorTree);
				return ExpressionBridge.Create (oldExpr, queryStatePrm);
			} else if (operatorText == "T_NOTIN") {
				IExpression oldExpr = 
					UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert (
					(a) => !a,
					ParseExpressionInSomeAnyAll (provider, operatorTree)
				);
				return ExpressionBridge.Create (oldExpr, queryStatePrm);
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
			case "T_LIKE":
			case "T_NOTLIKE":
				IExpression oldExpr = ParseExpressionOperatorBinary (provider, operatorTree);
				return ExpressionBridge.Create (oldExpr, queryStatePrm);
			default:
				System.Linq.Expressions.ExpressionType op = GetBinaryExpressionType (operatorTree);

				//Special treatment for string comparison
				if (arg1.Type == typeof(string)) {
					switch (op) {
					case System.Linq.Expressions.ExpressionType.Equal:
					case System.Linq.Expressions.ExpressionType.NotEqual:
					case System.Linq.Expressions.ExpressionType.LessThan:
					case System.Linq.Expressions.ExpressionType.GreaterThan:
					case System.Linq.Expressions.ExpressionType.GreaterThanOrEqual:
					case System.Linq.Expressions.ExpressionType.LessThanOrEqual:
						return CreateStringComparerExpression (op, arg1, arg2);
					}
				}

				return System.Linq.Expressions.Expression.MakeBinary (op, arg1, arg2);
			}
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

		System.Linq.Expressions.ExpressionType GetUnaryExpressionType (ITree operatorTree)
		{
			string operatorText = operatorTree.GetChild (0).Text;
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
			string operatorText = operatorTree.GetChild (0).Text;
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
	}
}

