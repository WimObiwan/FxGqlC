using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

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
			/*case "T_SYSTEMVAR":
				expression = ParseExpressionSystemVar (tree);
				break;
			case "T_FUNCTIONCALL":
				expression = ParseExpressionFunctionCall (provider, tree);
				break;
			case "T_OP_BINARY":
				expression = ParseExpressionOperatorBinary (provider, tree);
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
	}
}

