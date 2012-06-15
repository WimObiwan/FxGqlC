using System;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	partial class gqlParser
	{
		public AstParserRuleReturnScope<object, IToken> Parse ()
		{
			return parse ();
		}
		
		public override void ReportError (RecognitionException e)
		{
			throw new ParserException (e);
		}
	}
	
	public abstract class PositionException : Exception
	{
		public PositionException (string message, int line, int pos, Exception innerException)
			: base(string.Format("{0} At line {1}, position {2}.", message, line, pos), innerException)
		{
			Line = line;
			Pos = pos;
		}

		public PositionException (string message, ITree tree)
			: this(message, tree.Line, tree.CharPositionInLine + 1, null)
		{
		}
		
		public int Line { get; private set; }

		public int Pos { get; private set; }
	}
	
	public class ParserException : PositionException
	{
		public ParserException (RecognitionException recognitionException)
			: base("Parsing failed. " + recognitionException.Message, recognitionException.Line, recognitionException.CharPositionInLine, recognitionException)
		{
		}

		public ParserException (string message, ITree tree)
			: base(message, tree)
		{
		}
	}
	
	public class UnexpectedTokenAntlrException : PositionException
	{
		public UnexpectedTokenAntlrException (string expectedToken, ITree tree)
			: base(string.Format("Unexpected token. Expected {0}, but found {1}.", expectedToken, tree.Text), tree)
		{
		}

		public UnexpectedTokenAntlrException (ITree tree)
			: base(string.Format("Unexpected token. Found {0}, but expected another token.", tree.Text), tree)
		{
		}
	}
	
	public class NotEnoughSubTokensAntlrException : PositionException
	{
		public NotEnoughSubTokensAntlrException (ITree tree)
			: base("Not enough sub-tokens.", tree)
		{
		}
	}
	
	class AntlrTreeEnumerator
	{
		ITree parent;
		IEnumerator<ITree> enumerator;
		ITree current;
		
		public ITree Current { get { return current; } }
		
		public AntlrTreeEnumerator (CommonTree parent)
		{
			this.parent = parent;
			enumerator = parent.Children.GetEnumerator ();
			if (enumerator.MoveNext ())
				current = enumerator.Current;
			else
				current = null;
		}
		
		public void MoveNext ()
		{
			if (current == null)
				throw new NotEnoughSubTokensAntlrException (parent);
			if (enumerator.MoveNext ())
				current = enumerator.Current;
			else
				current = null;
		}		
	}
	
	class GqlParser
	{
		readonly string command;
		//readonly CultureInfo cultureInfo;
		readonly bool caseInsensitive;
		readonly StringComparer stringComparer;
		readonly StringComparison stringComparison;
		
		public GqlParser (string command)
			: this(command, CultureInfo.InvariantCulture, true)
		{
		}
		
		public GqlParser (string command, CultureInfo cultureInfo, bool caseInsensitive)
		{
			this.command = command;
			//this.cultureInfo = cultureInfo;
			this.caseInsensitive = caseInsensitive;
			this.stringComparer = StringComparer.Create (cultureInfo, true);
			this.stringComparison = StringComparison.InvariantCultureIgnoreCase;
		}

		private void AssertAntlrToken (ITree tree, string expectedToken)
		{
			AssertAntlrToken (tree, expectedToken, -1, -1);
		}
		
		private void AssertAntlrToken (ITree tree, string expectedToken, int childCount)
		{
			AssertAntlrToken (tree, expectedToken, childCount, childCount);
		}
		
		private void AssertAntlrToken (ITree tree, string expectedToken, int minChildCount, int maxChildCount)
		{
			if (expectedToken != null && tree.Text != expectedToken)
				throw new UnexpectedTokenAntlrException (expectedToken, tree);
			if (minChildCount >= 0 && minChildCount == maxChildCount && minChildCount != tree.ChildCount)
				throw new ParserException (
					string.Format ("Expected exact {0} childnode(s).", minChildCount),
					tree
				);
			if (minChildCount >= 0 && minChildCount > tree.ChildCount)
				throw new ParserException (
					string.Format ("Expected at least {0} childnode(s).", minChildCount),
					tree
				);
			if (maxChildCount >= 0 && maxChildCount < tree.ChildCount)
				throw new ParserException (
					string.Format ("Expected maximum {0} childnode(s).", maxChildCount),
					tree
				);
		}
		
		private void AssertAntlrSubTokenMinCount (ITree tree, string expectedToken)
		{
			if (tree.Text != expectedToken)
				throw new UnexpectedTokenAntlrException (expectedToken, tree);
		}

		CommonTree GetSingleChild (CommonTree tree)
		{
			if (tree.Children == null)
				throw new NotEnoughSubTokensAntlrException (tree);
			var childEnumerator = tree.Children.GetEnumerator ();
			if (!childEnumerator.MoveNext ())
				throw new NotEnoughSubTokensAntlrException (tree);
			return (CommonTree)childEnumerator.Current;
		}
		
		public IList<IGqlCommand> Parse ()
		{
			gqlLexer lex = new gqlLexer (new ANTLRStringStream (this.command));
			CommonTokenStream tokens = new CommonTokenStream (lex);
	 
			gqlParser parser = new gqlParser (tokens);
			parser.TreeAdaptor = new CommonTreeAdaptor ();
				
			IList<IGqlCommand > commands;
			try {
				var result = parser.Parse ();
				CommonTree rootTree = (CommonTree)result.Tree;
				commands = ParseCommands (rootTree);
			} catch (RecognitionException) {
				// TODO: outer exception
				throw;
			}
			
			return commands;
		}
		
		IList<IGqlCommand> ParseCommands (CommonTree commandsTree)
		{
			AssertAntlrToken (commandsTree, "T_ROOT");
			
			List<IGqlCommand > commands = new List<IGqlCommand> ();
			if (commandsTree.Children != null) {
				foreach (CommonTree commandTree in commandsTree.Children) {
					commands.Add (ParseCommand (commandTree));
				}
			}
			
			return commands;
		}
		
		IGqlCommand ParseCommand (CommonTree commandTree)
		{
			switch (commandTree.Text) {
			case "T_SELECT":
				return new GqlQueryCommand (ParseCommandSelect (commandTree));
			case "T_USE":
				return new UseCommand (ParseCommandUse (commandTree));
			default:
				throw new UnexpectedTokenAntlrException (commandTree);
			}
		}

		IProvider ParseCommandSelect (CommonTree selectTree)
		{
			AssertAntlrToken (selectTree, "T_SELECT");
			
			AntlrTreeEnumerator enumerator = new AntlrTreeEnumerator (selectTree);
			
			// DISTINCT / ALL
			bool distinct = false;
			if (enumerator.Current != null 
				&& (enumerator.Current.Text == "T_DISTINCT" || enumerator.Current.Text == "T_ALL")) {
				distinct = (enumerator.Current.Text == "T_DISTINCT");
				enumerator.MoveNext ();
			}
			
			
			// TOP
			Expression<long > topExpression;
			if (enumerator.Current != null && enumerator.Current.Text == "T_TOP") {
				topExpression = ParseTopClause ((CommonTree)enumerator.Current);
				enumerator.MoveNext ();
			} else {
				topExpression = null;
			}

			// columns
			if (enumerator.Current == null)
				throw new NotEnoughSubTokensAntlrException (selectTree);
			CommonTree columnListEnumerator = (CommonTree)enumerator.Current;
			enumerator.MoveNext ();
				
			// INTO
			FileOptions intoFile;
			if (enumerator.Current != null && enumerator.Current.Text == "T_INTO") {
				intoFile = ParseIntoClause ((CommonTree)enumerator.Current);
				enumerator.MoveNext ();
			} else {
				intoFile = null;
			}
			
			// FROM
			IProvider provider;
			if (enumerator.Current != null && enumerator.Current.Text == "T_FROM") {
				IProvider fromProvider = ParseFromClause ((CommonTree)enumerator.Current);
				provider = fromProvider;
				enumerator.MoveNext ();
								
				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE") {
					Expression<bool > whereExpression = ParseWhereClause (
						fromProvider,
						(CommonTree)enumerator.Current
					);
					enumerator.MoveNext ();

					provider = new FilterProvider (provider, whereExpression);
				}
				
				IList<Column > outputColumns;
				outputColumns = ParseColumnList (fromProvider, columnListEnumerator);
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_GROUPBY") {
					IList<IExpression> groupbyColumns = ParseGroupbyClause (
						fromProvider,
						(CommonTree)enumerator.Current
					);
					enumerator.MoveNext ();
					
					provider = new GroupbyProvider (
						provider,
						groupbyColumns,
						outputColumns,
						stringComparer
					);
				} else {
					provider = new SelectProvider (outputColumns, provider);
				}
				
				if (distinct)
					provider = new DistinctProvider (provider, stringComparer);
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY") {
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
						fromProvider,
						(CommonTree)enumerator.Current
					);
					enumerator.MoveNext ();
					
					provider = new OrderbyProvider (provider, orderbyColumns, stringComparer);
				}

				if (topExpression != null)
					provider = new TopProvider (provider, topExpression);
			} else {
				provider = new NullProvider ();
			
				if (distinct)
					throw new ParserException (
						"DISTINCT clause not allowed without a FROM clause.",
						selectTree
					);
				
				if (topExpression != null) 
					throw new ParserException (
						"TOP clause not allowed without a FROM clause.",
						selectTree
					);

				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE")
					throw new ParserException (
						"WHERE clause not allowed without a FROM clause.",
						selectTree
					);
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_GROUPBY")
					throw new ParserException (
						"GROUP BY clause not allowed without a FROM clause.",
						selectTree
					);

				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY")
					throw new ParserException (
						"ORDER BY clause not allowed without a FROM clause.",
						selectTree
					);

				IList<Column > outputColumns;
				outputColumns = ParseColumnList (provider, columnListEnumerator);

				provider = new SelectProvider (outputColumns, provider);
			}
			
			if (intoFile != null)
				provider = new IntoProvider (provider, intoFile);
			
			return provider;
		}
		
		Expression<long> ParseTopClause (CommonTree topClauseTree)
		{
			CommonTree tree = GetSingleChild (topClauseTree);
			return ExpressionHelper.ConvertIfNeeded<long> (ParseExpression (null, tree));
		}
		
		IList<Column> ParseColumnList (IProvider provider, CommonTree outputListTree)
		{
			List<Column > outputColumnExpressions = new List<Column> ();
			AssertAntlrToken (outputListTree, "T_COLUMNLIST", 1, -1);
			foreach (CommonTree outputColumnTree in outputListTree.Children) {
				Column column = ParseColumn (provider, outputColumnTree);
				outputColumnExpressions.Add (column);
			}
			
			return outputColumnExpressions;
		}

		Column ParseColumn (IProvider provider, CommonTree outputColumnTree)
		{
			AssertAntlrToken (outputColumnTree, "T_COLUMN", 1, 2);
			
			Column column = new Column ();
			column.Expression = ParseExpression (
				provider,
				(CommonTree)outputColumnTree.Children [0]
			);
			if (outputColumnTree.Children.Count == 2) {
				column.Name = ParseColumnName ((CommonTree)outputColumnTree.Children [1]);
			} else {
				column.Name = null;
			}
			
			return column; 
		}

		FileOptions ParseIntoClause (CommonTree intoClauseTree)
		{
			AssertAntlrToken (intoClauseTree, "T_INTO", 1);
				
			CommonTree fileTree = GetSingleChild (intoClauseTree);
			FileOptions intoFile = ParseFile (fileTree, true);			
			
			return intoFile;
		}
		
		IProvider ParseFromClause (CommonTree fromClauseTree)
		{
			AssertAntlrToken (fromClauseTree, "T_FROM", 1, -1);
			
			IProvider[] provider = new IProvider[fromClauseTree.ChildCount];
			
			for (int i = 0; i < fromClauseTree.ChildCount; i++) {
				CommonTree inputProviderTree = GetSingleChild (fromClauseTree);
				switch (inputProviderTree.Text) {
				case "T_FILE":
					provider [i] = ParseFileProvider (inputProviderTree);
					break;
				case "T_SUBQUERY":
					provider [i] = ParseSubquery (inputProviderTree);
					break;
				default:
					throw new UnexpectedTokenAntlrException (inputProviderTree);
				}
			}
			
			IProvider fromProvider;
			if (provider.Length == 1)
				fromProvider = provider [0];
			else
				fromProvider = new MergeProvider (provider);
			
			return fromProvider;
		}

		Expression<bool> ParseWhereClause (IProvider provider, CommonTree whereTree)
		{
			AssertAntlrToken (whereTree, "T_WHERE");
			
			CommonTree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (provider, expressionTree);
			if (!(expression is Expression<bool>)) {
				throw new ParserException (
					"Expected boolean expression in WHERE clause.",
					expressionTree
				);
			}
			return (Expression<bool>)expression;
		}
		
		IList<IExpression> ParseGroupbyClause (IProvider provider, CommonTree groupbyTree)
		{
			AssertAntlrToken (groupbyTree, "T_GROUPBY", 1, 1);
		
			return ParseExpressionList (provider, (CommonTree)groupbyTree.Children [0]);
		}
		
		IList<OrderbyProvider.Column> ParseOrderbyClause (IProvider provider, CommonTree orderbyTree)
		{
			AssertAntlrToken (orderbyTree, "T_ORDERBY");
						
			List<OrderbyProvider.Column > orderbyColumns = new List<OrderbyProvider.Column> ();
			foreach (CommonTree orderbyColumnTree in orderbyTree.Children) {
				orderbyColumns.Add (ParseOrderbyColumn (provider, orderbyColumnTree));
			}
			
			return orderbyColumns;
		}
		
		OrderbyProvider.Column ParseOrderbyColumn (IProvider provider, CommonTree orderbyColumnTree)
		{
			AssertAntlrToken (orderbyColumnTree, "T_ORDERBY_COLUMN", 1, 2);
			
			OrderbyProvider.Column orderbyColumn = new OrderbyProvider.Column ();
			orderbyColumn.Expression = ParseExpression (
				provider,
				(CommonTree)orderbyColumnTree.Children [0]
			);
			if (orderbyColumnTree.Children.Count > 1) {
				string order = orderbyColumnTree.Children [1].Text;
				switch (order) {
				case "T_ORDERBY_ASC":
					orderbyColumn.Order = OrderbyProvider.OrderEnum.ASC;
					break;
				case "T_ORDERBY_DESC":
					orderbyColumn.Order = OrderbyProvider.OrderEnum.DESC;
					break;
				default:
					throw new ParserException ("Expected ASC or DESC as ORDER BY column order",
					                          orderbyColumnTree.Children [1]);
				}
			} else {
				orderbyColumn.Order = OrderbyProvider.OrderEnum.ASC;
			}
			
			return orderbyColumn;
		}

		IExpression ParseExpression (IProvider provider, CommonTree expressionTree)
		{
			IExpression expression;
			switch (expressionTree.Text.ToUpperInvariant ()) {
			case "*":
				expression = new LineSystemVar ();
				break;
			case "T_INTEGER":
				expression = ParseExpressionInteger (expressionTree);
				break;
			case "T_STRING":
				expression = ParseExpressionString (expressionTree);
				break;
			case "T_SYSTEMVAR":
				expression = ParseExpressionSystemVar (expressionTree);
				break;
			case "T_FUNCTIONCALL":
				expression = ParseExpressionFunctionCall (provider, expressionTree);
				break;
			case "T_CONVERT":
				expression = ParseExpressionConvert (provider, expressionTree);
				break;
			case "T_OP_UNARY":
				expression = ParseExpressionOperatorUnary (provider, expressionTree);
				break;
			case "T_OP_BINARY":
				expression = ParseExpressionOperatorBinary (provider, expressionTree);
				break;
			case "T_EXISTS":
				expression = ParseExpressionExists (expressionTree);
				break;
			case "T_COLUMN":
				expression = ParseExpressionColumn (provider, expressionTree);
				break;
			case "T_CASE":
				expression = ParseExpressionCase (provider, expressionTree);
				break;
			default:
				throw new UnexpectedTokenAntlrException (expressionTree);
			}
			
			return expression;
		}

		Expression<T> ParseExpression<T> (IProvider provider, CommonTree expressionTree) where T : IComparable
		{
			IExpression expression = ParseExpression (provider, expressionTree);
			Expression<T > expressionT = expression as Expression<T>;
			if (expressionT == null)
				throw new ParserException (
					string.Format ("Expected expression of type '{0}'", typeof(T).Name),
					expressionTree
				);
			return expressionT;
		}
		
		Expression<long> ParseExpressionInteger (CommonTree expressionNumberTree)
		{
			CommonTree tree = GetSingleChild (expressionNumberTree);
			return new ConstExpression<long> (long.Parse (tree.Text));
		}

		string ParseString (ITree tree)
		{
			string text = tree.Text;
			if (text.Length < 2 || text [0] != '\'' || text [text.Length - 1] != '\'')
				throw new ParserException ("Invalid string format.", tree);
			return ParseString (text);
		}
		
		string ParseStringValue (ITree tree)
		{
			string text = tree.Text;
			if (text [0] == '\'') {
				if (text [text.Length - 1] != '\'')
					throw new ParserException ("Invalid string format.", tree);

				return ParseString (text);
			} else {
				return text;
			}
		}

		string ParseString (string text)
		{
			text = text.Substring (1, text.Length - 2);
			text = text.Replace ("''", "'");
			return text;
		}

		Expression<string> ParseExpressionString (CommonTree expressionStringTree)
		{
			CommonTree tree = GetSingleChild (expressionStringTree);
			
			string text = ParseString (tree);
			return new ConstExpression<string> (text);
		}

		IExpression ParseExpressionSystemVar (CommonTree expressionSystemVarTree)
		{
			CommonTree tree = GetSingleChild (expressionSystemVarTree);
			
			IExpression expression;
			switch (tree.Text.ToUpperInvariant ()) {
			case "$LINE":
				expression = new LineSystemVar ();
				break;
			case "$TOTALLINENO":
				expression = new TotalLineNoSystemVar ();
				break;
			case "$FILENAME":
				expression = new FileNameSystemVar (false);
				break;
			case "$FULLFILENAME":
				expression = new FileNameSystemVar (true);
				break;
			case "$LINENO":
				expression = new LineNoSystemVar ();
				break;
			default:
				throw new ParserException (
					string.Format ("Unknown system variable '{0}'.", tree.Text),
					tree
				);
			}
			
			return expression;
		}

		IExpression ParseExpressionFunctionCall (IProvider provider, CommonTree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL");

			string functionName = functionCallTree.Children [0].Text;
			
			IExpression result;
			int argCount = functionCallTree.Children.Count - 1;
			switch (argCount) {
			case 0:
				result = ParseExpressionFunctionCall_0 (
					provider,
					functionCallTree,
					functionName
				);
				break;
			case 1:
				result = ParseExpressionFunctionCall_1 (
					provider,
					functionCallTree,
					functionName
				);
				break;
			case 2:
				result = ParseExpressionFunctionCall_2 (
					provider,
					functionCallTree,
					functionName
				);
				break;
			case 3:
				result = ParseExpressionFunctionCall_3 (
					provider,
					functionCallTree,
					functionName
				);
				break;
			default:
				throw new ParserException (
					string.Format (
					"Function call with '{0}' arguments not supported.",
					argCount
				),
					functionCallTree
				);
			}
			
			return result;
		}
				
		IExpression ParseExpressionFunctionCall_0 (IProvider provider, CommonTree functionCallTree, string functionName)
		{
			IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			case "GETCURDIR":
				result = new GetCurDirFunction ();
				break;
			default:
				throw new ParserException (string.Format (
					"Function call to {0} with 0 parameters not supported.",
					functionName
				), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionFunctionCall_1 (IProvider provider, CommonTree functionCallTree, string functionName)
		{
			IExpression arg = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [1]
			);

			IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			case "ESCAPEREGEX":
				result = new UnaryExpression<string, string> ((a) => Regex.Escape (a), arg);
				break;
			case "LTRIM":
				result = new UnaryExpression<string, string> ((a) => a.TrimStart (), arg);
				break;
			case "RTRIM":
				result = new UnaryExpression<string, string> ((a) => a.TrimEnd (), arg);
				break;
			case "TRIM":
				result = new UnaryExpression<string, string> ((a) => a.Trim (), arg);
				break;
			case "COUNT":
				if (arg is Expression<string>)
					result = new AggregationExpression<string, long, long> ((a) => 1, 
						(s, a) => s + 1, 
						(s) => s, 
						(Expression<string>)arg);
				else if (arg is Expression<long>)
					result = new AggregationExpression<long, long, long> ((a) => 1, 
						(s, a) => s + 1, 
						(s) => s, 
						(Expression<long>)arg);
				else {
					throw new ParserException (
						string.Format ("COUNT aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "SUM":
				if (arg is Expression<long>)
					result = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => s + a, 
						(s) => s, 
						(Expression<long>)arg);
				else {
					throw new ParserException (
						string.Format ("SUM aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "MIN":
				if (arg.GetResultType () == typeof(string))
					result = new AggregationExpression<string, string, string> (
						(a) => a, 
						(s, a) => string.Compare (a, s) < 0 ? a : s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<string> (arg));
				else if (arg.GetResultType () == typeof(long))
					result = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => a < s ? a : s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<long> (arg));
				else {
					throw new ParserException (
						string.Format ("MIN aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "MAX":
				if (arg.GetResultType () == typeof(string))
					result = new AggregationExpression<string, string, string> (
						(a) => a, 
						(s, a) => string.Compare (a, s) > 0 ? a : s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<string> (arg));
				else if (arg.GetResultType () == typeof(long))
					result = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => a > s ? a : s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<long> (arg));
				else {
					throw new ParserException (
						string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "FIRST":
				if (arg.GetResultType () == typeof(string))
					result = new AggregationExpression<string, string, string> (
						(a) => a, 
						(s, a) => s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<string> (arg));
				else if (arg.GetResultType () == typeof(long))
					result = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => s, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<long> (arg));
				else {
					throw new ParserException (
						string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "LAST":
				if (arg.GetResultType () == typeof(string))
					result = new AggregationExpression<string, string, string> (
						(a) => a, 
						(s, a) => a, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<string> (arg));
				else if (arg.GetResultType () == typeof(long))
					result = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => a, 
						(s) => s, 
						ExpressionHelper.ConvertIfNeeded<long> (arg));
				else {
					throw new ParserException (
						string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "AVG":
				if (arg is Expression<long>) {
					Expression<long> resultSum = new AggregationExpression<long, long, long> (
						(a) => a, 
						(s, a) => s + a, 
						(s) => s, 
						(Expression<long>)arg);
					Expression<long> resultCount = new AggregationExpression<long, long, long> (
						(a) => 1, 
						(s, a) => s + 1, 
						(s) => s, 
						(Expression<long>)arg);
					result = new BinaryExpression<long, long, long> (
						(a, b) => a / b, resultSum, resultCount);
				} else {
					throw new ParserException (
						string.Format ("SUM aggregation function cannot be used on datatype '{0}'",
				               arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			default:
				throw new ParserException (string.Format (
					"Function call to {0} with 1 parameters not supported.",
					functionName
				), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionFunctionCall_2 (IProvider provider, CommonTree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [1]
			);
			IExpression arg2 = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [2]
			);
			
			IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			case "CONTAINS":
				result = new BinaryExpression<string, string, bool> (
					(a, b) => a.IndexOf (b, stringComparison) != -1,
					arg1,
					arg2
				);
				break;
			case "LEFT":
				result = new BinaryExpression<string, int, string> (
					(a, b) => a.Substring (0, Math.Min (b, a.Length)),
					arg1,
					arg2
				);
				break;
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, caseInsensitive);
				break;
			case "RIGHT":
				result = new BinaryExpression<string, int, string> (
					(a, b) => a.Substring (a.Length - Math.Min (b, a.Length)),
					arg1,
					arg2
				);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2);
				break;
			default:
				throw new ParserException (string.Format (
					"Function call to {0} with 2 parameters not supported.",
					functionName
				), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionFunctionCall_3 (IProvider provider, CommonTree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [1]
			);
			IExpression arg2 = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [2]
			);
			IExpression arg3 = ParseExpression (
				provider,
				(CommonTree)functionCallTree.Children [3]
			);
			
			IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, caseInsensitive, arg3);
				break;
			case "REPLACE":
				result = new ReplaceFunction (arg1, arg2, arg3, caseInsensitive);
				break;
			case "REPLACEREGEX":
				result = new ReplaceRegexFunction (arg1, arg2, arg3, caseInsensitive);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2, arg3);
				break;
			default:
				throw new ParserException (string.Format (
					"Function call to {0} with 2 parameters not supported.",
					functionName
				), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionConvert (IProvider provider, CommonTree convertTree)
		{
			AssertAntlrToken (convertTree, "T_CONVERT", 2);
			
			string dataType = convertTree.Children [0].Text;
			IExpression expr = ParseExpression (
				provider,
				(CommonTree)convertTree.Children [1]
			);
			
			IExpression result;
			switch (dataType.ToUpperInvariant ()) {
			case "INT":
				result = new ConvertExpression<long> (expr);
				break;
			case "STRING":
				result = new ConvertToStringExpression (expr);
				break;
			default:
				throw new ParserException (
					string.Format (
					"Datatype {0} not supported in CONVERT function.",
					dataType
				),
					convertTree.Children [1]
				);
			}
			return result;
		}

		FileOptions ParseFile (CommonTree fileProvider, bool intoClause)
		{
			AssertAntlrToken (fileProvider, "T_FILE", 1, -1);
			
			AntlrTreeEnumerator enumerator = new AntlrTreeEnumerator (fileProvider);

			FileOptions fileOptions = new FileOptions ();
			string fileNameText = enumerator.Current.Text;
			if (fileNameText.StartsWith ("[")) {
				fileOptions.FileName = fileNameText.Substring (1, fileNameText.Length - 2);
			} else if (fileNameText.StartsWith ("\'")) {
				CommonTree fileTree = (CommonTree)enumerator.Current;
				fileOptions.FileName = ParseString (fileTree);
				
				enumerator.MoveNext ();
				while (enumerator.Current != null) {
					string option;
					string value;
					ParseFileOption ((CommonTree)enumerator.Current, out option, out value);
					if (intoClause) {
						switch (option.ToUpperInvariant ()) {
						case "LINEEND":
							FileOptions.NewLineEnum lineEnd;
							if (!Enum.TryParse<FileOptions.NewLineEnum> (value, true, out lineEnd))
								throw new ParserException (
									string.Format ("Unknown file option LineEnd={0}", value),
									enumerator.Current
								);
							fileOptions.NewLine = lineEnd;
							break;
						case "APPEND":
							fileOptions.Append = true;
							break;
						case "OVERWRITE":
							fileOptions.Overwrite = true;
							break;
						default:
							throw new ParserException (
								string.Format ("Unknown file option '{0}'", option),
								enumerator.Current
							);  
						}
					} else {
						switch (option.ToUpperInvariant ()) {
						case "FILEORDER":
							FileOptions.FileOrderEnum order;
							if (!Enum.TryParse<FileOptions.FileOrderEnum> (value, true, out order))
								throw new ParserException (
									string.Format ("Unknown file option FileOrder={0}", value),
									enumerator.Current
								);
							fileOptions.FileOrder = order;
							break;
						case "RECURSE":
							fileOptions.Recurse = true;
							break;
						case "TITLELINE":
							fileOptions.TitleLine = true;
							break;
						case "COLUMNS":
							fileOptions.ColumnsRegex = ParseString (value);
							break;
						case "SKIP":
							fileOptions.Skip = long.Parse (value);
							break;
						default:
							throw new ParserException (
								string.Format ("Unknown file option '{0}'", option),
								enumerator.Current
							);  
						}
					}
					
					enumerator.MoveNext ();
				}
			} else
				throw new ParserException (
					"Expected '[' or '\'' for file specification",
					enumerator.Current
				);
			
			return fileOptions;
		}

		void ParseFileOption (CommonTree fileOptionTree, out string option, out string value)
		{
			AssertAntlrToken (fileOptionTree, "T_FILEOPTION", 1, 2);
			
			option = fileOptionTree.Children [0].Text;
			if (fileOptionTree.Children.Count > 1)
				value = ParseStringValue(fileOptionTree.Children [1]);
			else
				value = null;
		}

		IProvider ParseFileProvider (CommonTree fileProvider)
		{
			FileOptions fileOptions = ParseFile (fileProvider, false);
			
			IProvider provider = FileProviderFactory.Get (fileOptions, stringComparer);
			
			if (fileOptions.TitleLine) {
				provider = new ColumnProviderTitleLine (provider, new char[] {'\t'});
			} else if (fileOptions.ColumnsRegex != null) {
				provider = new ColumnProviderRegex (
					provider,
					fileOptions.ColumnsRegex,
					caseInsensitive
				);
			}
			
			return provider;
		}
		
		IProvider ParseSubquery (CommonTree subqueryTree)
		{
			AssertAntlrToken (subqueryTree, "T_SUBQUERY");
			
			CommonTree selectTree = GetSingleChild (subqueryTree);
			return ParseCommandSelect (selectTree);
		}
		
		IExpression ParseExpressionOperatorUnary (IProvider provider, CommonTree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_UNARY", 2);
			
			IExpression arg = ParseExpression (
				provider,
				(CommonTree)operatorTree.Children [1]
			);			
			IExpression result;
			
			string operatorText = operatorTree.Children [0].Text;
			switch (operatorText) {
			case "T_NOT":
				result = new UnaryExpression<bool, bool> ((a) => !a, arg);
				break;
			case "T_PLUS":
				if (arg is Expression<long>)
					result = new UnaryExpression<long, long> ((a) => a, arg);
				else {
					throw new ParserException (
							string.Format ("Unary operator 'PLUS' cannot be used with datatype {0}",
					               arg.GetResultType ().ToString ()),
							operatorTree);
				}
				break;
			case "T_MINUS":
				if (arg is Expression<long>)
					result = new UnaryExpression<long, long> ((a) => -a, arg);
				else {
					throw new ParserException (
							string.Format ("Unary operator 'MINUS' cannot be used with datatype {0}",
					               arg.GetResultType ().ToString ()),
							operatorTree);
				}
				break;
			case "T_BITWISE_NOT":
				if (arg is Expression<long>)
					result = new UnaryExpression<long, long> ((a) => ~a, arg);
				else {
					throw new ParserException (
							string.Format ("Unary operator 'MINUS' cannot be used with datatype {0}",
					               arg.GetResultType ().ToString ()),
							operatorTree);
				}
				break;
			default:
				throw new ParserException (
					string.Format ("Unknown unary operator '{0}'.", operatorText),
					operatorTree
				);
			}
			
			return result;
		}

		IExpression ParseExpressionOperatorBinary (IProvider provider, CommonTree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3, 4);
			
			string operatorText = operatorTree.Children [0].Text;
			if (operatorText == "T_BETWEEN") {
				return ParseExpressionBetween (provider, (CommonTree)operatorTree);
			} else if (operatorText == "T_NOTBETWEEN") {
				return new UnaryExpression<bool, bool> (
					(a) => !a,
					ParseExpressionBetween (provider, (CommonTree)operatorTree)
				);
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseExpressionInSomeAnyAll (provider, (CommonTree)operatorTree);
			} else if (operatorText == "T_NOTIN") {
				return new UnaryExpression<bool, bool> (
					(a) => !a,
					ParseExpressionInSomeAnyAll (provider, (CommonTree)operatorTree)
				);
			} 
			
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3);

			IExpression arg1 = ParseExpression (
				provider,
				(CommonTree)operatorTree.Children [1]
			);			
			IExpression arg2 = ParseExpression (
				provider,
				(CommonTree)operatorTree.Children [2]
			);			
			IExpression result;
			
			switch (operatorText) {
			case "T_AND":
				result = new BinaryExpression<bool, bool, bool> (
					(a, b) => a && b,
					arg1,
					arg2
				);
				break;
			case "T_OR":
				result = new BinaryExpression<bool, bool, bool> (
					(a, b) => a || b,
					arg1,
					arg2
				);
				break;
			case "T_MATCH":
				result = new MatchOperator (arg1, arg2, caseInsensitive);
				break;
			case "T_NOTMATCH":
				result = new UnaryExpression<bool, bool> (
					(a) => !a,
					new MatchOperator (arg1, arg2, caseInsensitive)
				);
				break;
			case "T_LIKE":
				result = new LikeOperator (arg1, arg2, caseInsensitive);
				break;
			case "T_NOTLIKE":
				result = new UnaryExpression<bool, bool> (
					(a) => !a,
					new LikeOperator (arg1, arg2, caseInsensitive)
				);
				break;
			case "T_PLUS":
				{
					if (arg1 is Expression<string> || arg2 is Expression<string>)
						result = new BinaryExpression<string, string, string> (
							(a, b) => a + b,
							arg1,
							arg2
						);
					else if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a + b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'PLUS' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_MINUS":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a - b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'MINUS' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_DIVIDE":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a / b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'DIVIDE' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_PRODUCT":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a * b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'PRODUCT' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_MODULO":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a % b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'MODULO' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_AND":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a & b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'BITWISE AND' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_OR":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a | b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'BITWISE OR' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_XOR":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> (
							(a, b) => a ^ b,
							arg1,
							arg2
						);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'BITWISE XOR' cannot be used with datatypes {0} and {1}",
							arg1.GetResultType ().ToString (),
							arg2.GetResultType ().ToString ()
						),
							operatorTree);
					}
				}
				break;
			case "T_EQUAL":
			case "T_NOTEQUAL":
			case "T_LESS":
			case "T_GREATER":
			case "T_NOTLESS":
			case "T_NOTGREATER":
				if (arg1 is Expression<string> || arg2 is Expression<string>)
					result = 
						new BinaryExpression<string, string, bool> (OperatorHelper.GetStringComparer (
						operatorText,
						false,
						stringComparison
					),
							arg1, arg2);
				else if (arg1 is Expression<long>)
					result = 
						new BinaryExpression<long, long, bool> (OperatorHelper.GetLongComparer (
						operatorText,
						false
					),
							arg1, arg2);
				else {
					throw new ParserException (
						string.Format (
						"Binary operator 'EQUAL' cannot be used with datatypes {0} and {1}",
						arg1.GetResultType ().ToString (),
						arg2.GetResultType ().ToString ()
					),
						operatorTree);
				}
				break;
			default:
				throw new ParserException (
					string.Format ("Unknown binary operator '{0}'.", operatorText),
					operatorTree
				);
			}
			
			return result;
		}
		
		IExpression ParseExpressionBetween (IProvider provider, CommonTree betweenTree)
		{
			AssertAntlrToken (betweenTree, "T_OP_BINARY", 3);
			//AssertAntlrToken (betweenTree.Children [0], "T_BETWEEN"); or T_NOTBETWEEN
			CommonTree andTree = (CommonTree)betweenTree.Children [2];
			AssertAntlrToken (andTree, "T_OP_BINARY", 3);
			AssertAntlrToken (andTree.Children [0], "T_AND");
			
			IExpression arg1 = ParseExpression (
				provider,
				(CommonTree)betweenTree.Children [1]
			);
			IExpression arg2 = ParseExpression (
				provider,
				(CommonTree)andTree.Children [1]
			);
			IExpression arg3 = ParseExpression (
				provider,
				(CommonTree)andTree.Children [2]
			);

			IExpression result;
			if (arg1 is Expression<string> || arg2 is Expression<string> || arg3 is Expression<string>)
				result = new TernaryExpression<string, string, string, bool> (
					(a, b, c) => 
				                                                      string.Compare (
					a,
					b,
					stringComparison
				) >= 0 
					&& string.Compare (
					a,
					c,
					stringComparison
				) <= 0,
					arg1,
					arg2,
					arg3
				);
			else if (arg1 is Expression<long>)
				result = new TernaryExpression<long, long, long, bool> (
					(a, b, c) => (a >= b) && (a <= c),
					arg1,
					arg2,
					arg3
				);
			else {
				throw new ParserException (
					string.Format ("Ternary operator 'BETWEEN' cannot be used with datatypes {0}, {1} and {2}",
			               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString (),
				           arg3.GetResultType ().ToString ()),
					betweenTree);
			}
			
			return result;
		}

		IExpression ParseExpressionInSomeAnyAll (IProvider provider, CommonTree inTree)
		{
			AssertAntlrToken (inTree, "T_OP_BINARY", 3, 4);
			//AssertAntlrToken (inTree.Children [0], "T_IN"); or T_NOTIN, T_ANY, T_ALL
			
			IExpression arg2;
			CommonTree target;			
			bool all;
			string op;
			switch (inTree.Children [0].Text) {
			case "T_IN":
			case "T_NOTIN":
				arg2 = ParseExpression (provider, (CommonTree)inTree.Children [1]);
				target = (CommonTree)inTree.Children [2];
				all = false;
				op = "T_EQUAL";
				break;
			case "T_ANY":
				arg2 = ParseExpression (provider, (CommonTree)inTree.Children [2]);
				target = (CommonTree)inTree.Children [3];
				all = false;
				op = inTree.Children [1].Text;
				break;
			case "T_ALL":
				arg2 = ParseExpression (provider, (CommonTree)inTree.Children [2]);
				target = (CommonTree)inTree.Children [3];
				all = true;
				op = inTree.Children [1].Text;
				break;
			default:
				throw new ParserException (
					string.Format ("Unexpected token {0}", inTree.Children [0].Text),
					inTree.Children [0]
				);
			}
						
			Expression<bool > result;
			if (target.Text == "T_EXPRESSIONLIST") {
				IExpression[] expressionList = ParseExpressionList (provider, target);
				if (arg2 is Expression<string>)
					result = new AnyListOperator<string> (
						(Expression<string>)arg2,
						expressionList,
						OperatorHelper.GetStringComparer (op, all, stringComparison)
					);
				else if (arg2 is Expression<long>)
					result = new AnyListOperator<long> (
						(Expression<long>)arg2,
						expressionList,
						OperatorHelper.GetLongComparer (op, all)
					);
				else
					throw new ParserException (
						string.Format (
						"Binary operator '{0}' cannot be used with datatype {1}",
						inTree.Children [0].Text,
						target.Text
					),
						target
					);
			} else if (target.Text == "T_SELECT") {
				IProvider subProvider = ParseCommandSelect (target);
				if (arg2 is Expression<string>)
					result = new AnySubqueryOperator<string> (
						(Expression<string>)arg2,
						subProvider,
						OperatorHelper.GetStringComparer (op, all, stringComparison)
					);
				else if (arg2 is Expression<long>)
					result = new AnySubqueryOperator<long> (
						(Expression<long>)arg2,
						subProvider,
						OperatorHelper.GetLongComparer (op, all)
					);
				else
					throw new ParserException (
						string.Format (
						"Binary operator '{0}' cannot be used with datatype {1}",
						inTree.Children [0].Text,
						target.Text
					),
						target
					);
			} else {
				throw new ParserException (
					string.Format (
					"Binary operator '{0}' cannot be used with argument {1}",
					inTree.Children [0].Text,
					arg2.GetResultType ().ToString ()
				),
					target
				);
			}
		
			if (all)
				result = new UnaryExpression<bool, bool> (a => !a, result);
					
			return result;
		}

		IExpression[] ParseExpressionList (IProvider provider, CommonTree expressionListTree)
		{
			AssertAntlrToken (expressionListTree, "T_EXPRESSIONLIST", 1, -1);
			
			IExpression[] result = new IExpression[expressionListTree.Children.Count];
			for (int i = 0; i < expressionListTree.Children.Count; i++) {
				result [i] = ParseExpression (
					provider,
					(CommonTree)expressionListTree.Children [i]
				);
			}			
			
			return result;
		}

		IExpression ParseExpressionExists (CommonTree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_EXISTS", 1, 1);
			
			return new AnySubqueryOperator<long> (
				new ConstExpression<long> (1),
				new SelectProvider (
				new IExpression[] { new ConstExpression<long> (1) }, 
				new TopProvider (
				ParseCommandSelect ((CommonTree)expressionTree.Children [0]),
				new ConstExpression<long> (1)
			)
			),
				(a, b) => a == b);
			;
		}
		
		IExpression ParseExpressionColumn (IProvider provider, CommonTree expressionTree)
		{
			
			AssertAntlrToken (expressionTree, "T_COLUMN", 1, 1);
			
			string column = ParseColumnName ((CommonTree)expressionTree.Children [0]);
			return new ColumnExpression (provider, column);
		}
		
		string ParseColumnName (CommonTree columnNameTree)
		{
			string column = columnNameTree.Text;
			
			if (column.StartsWith ("[") && column.EndsWith ("]"))
				column = column.Substring (1, column.Length - 2);
			
			return column;
		}
		
		IExpression ParseExpressionCase (IProvider provider, CommonTree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_CASE", 1, -1);
			
			List<CaseExpression.WhenItem> whenItems = new List<CaseExpression.WhenItem> ();
			IExpression elseResult = null;

			string text = expressionTree.Children [0].Text;
			if (text != "T_CASE_WHEN" && text != "T_CASE_ELSE") {
				// CASE source WHEN destination THEN target ELSE other END
				IExpression source = ParseExpression (
					provider,
					(CommonTree)expressionTree.Children [0]
				);
				int whenNo;
				for (whenNo = 1; expressionTree.Children [whenNo].Text == "T_CASE_WHEN"; whenNo++) {
					CommonTree whenTree = (CommonTree)expressionTree.Children [whenNo];
					IExpression destination = ParseExpression (
						provider,
						(CommonTree)whenTree.Children [0]
					);
					IExpression target = ParseExpression (
						provider,
						(CommonTree)whenTree.Children [1]
					);
					CaseExpression.WhenItem whenItem = new CaseExpression.WhenItem ();
					
					//TODO: Don't re-evaluate source for every item
					if (source is Expression<string> || destination is Expression<string>)
						whenItem.Check = 
							new BinaryExpression<string, string, bool> (OperatorHelper.GetStringComparer (
							"T_EQUAL",
							false,
							stringComparison
						),
								source, destination);
					else if (source is Expression<long>)
						whenItem.Check = 
							new BinaryExpression<long, long, bool> (OperatorHelper.GetLongComparer (
							"T_EQUAL",
							false
						),
								source, destination);
					else {
						throw new ParserException (
							string.Format (
							"Binary operator 'EQUAL' cannot be used with datatypes {0} and {1}",
							source.GetResultType ().ToString (),
							destination.GetResultType ().ToString ()
						),
							whenTree);
					}
					whenItem.Result = target;
					
					whenItems.Add (whenItem);
				}
				
				if (whenNo < expressionTree.Children.Count - 1)
					throw new Exception ("Invalid case statement");
				
				if (whenNo == expressionTree.Children.Count - 1) {
					CommonTree elseTree = (CommonTree)expressionTree.Children [whenNo];
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
					
					elseResult = ParseExpression (provider, (CommonTree)elseTree.Children [0]);
				}
			} else {
				// CASE WHEN a THEN x ELSE y END
				int whenNo;
				for (whenNo = 0; expressionTree.Children [whenNo].Text == "T_CASE_WHEN"; whenNo++) {
					CommonTree whenTree = (CommonTree)expressionTree.Children [whenNo];
					IExpression destination = ParseExpression (
						provider,
						(CommonTree)whenTree.Children [0]
					);
					IExpression target = ParseExpression (
						provider,
						(CommonTree)whenTree.Children [1]
					);
					CaseExpression.WhenItem whenItem = new CaseExpression.WhenItem ();
					
					//TODO: Don't re-evaluate source for every item
					if (destination is Expression<bool>)
						whenItem.Check = (Expression<bool>)destination;
					else {
						throw new ParserException (
							string.Format ("CASE WHEN expression must evaluate to datatype boolean instead of {0}",
					               destination.GetResultType ().ToString ()),
							whenTree);
					}
					whenItem.Result = target;
					
					whenItems.Add (whenItem);
				}
				
				if (whenNo < expressionTree.Children.Count - 1)
					throw new Exception ("Invalid case statement");
				
				if (whenNo == expressionTree.Children.Count - 1) {
					CommonTree elseTree = (CommonTree)expressionTree.Children [whenNo];
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
					
					elseResult = ParseExpression (provider, (CommonTree)elseTree.Children [0]);
				}
			}

			return new CaseExpression (whenItems, elseResult);
		}

		FileOptions ParseCommandUse (CommonTree selectTree)
		{
			AssertAntlrToken (selectTree, "T_USE", 1);

			return ParseFile ((CommonTree)selectTree.Children [0], false);
		}
	}
}

