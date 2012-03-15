using System;
using System.Diagnostics;
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
		}

		public PositionException (string message, ITree tree)
			: this(message, tree.Line, tree.CharPositionInLine, null)
		{
		}
	}
	
	public class ParserException : PositionException
	{
		public ParserException (RecognitionException recognitionException)
			: base("Parsing failed.", recognitionException.Line, recognitionException.CharPositionInLine, recognitionException)
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
				throw new ParserException (string.Format ("Expected exact {0} childnode(s).", minChildCount), tree);
			if (minChildCount >= 0 && minChildCount > tree.ChildCount)
				throw new ParserException (string.Format ("Expected at least {0} childnode(s).", minChildCount), tree);
			if (maxChildCount >= 0 && maxChildCount < tree.ChildCount)
				throw new ParserException (string.Format ("Expected maximum {0} childnode(s).", maxChildCount), tree);
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
			foreach (CommonTree commandTree in commandsTree.Children) {
				commands.Add (ParseCommand (commandTree));
			}
			
			return commands;
		}
		
		IGqlCommand ParseCommand (CommonTree commandTree)
		{
			switch (commandTree.Text) {
			case "T_SELECT":
				return new GqlQueryCommand (ParseCommandSelect (commandTree));
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
			IList<IExpression > outputColumns;
			outputColumns = ParseColumnList ((CommonTree)enumerator.Current);
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
				provider = ParseFromClause ((CommonTree)enumerator.Current);					
				enumerator.MoveNext ();
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE") {
					Expression<bool > whereExpression = ParseWhereClause ((CommonTree)enumerator.Current);
					enumerator.MoveNext ();

					provider = new FilterProvider (provider, whereExpression);
				}
				
				provider = new SelectProvider (outputColumns, provider);
				
				if (distinct)
					provider = new DistinctProvider (provider, stringComparer);
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY") {
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause ((CommonTree)enumerator.Current);
					enumerator.MoveNext ();
					
					provider = new OrderbyProvider (provider, orderbyColumns);
				}

				if (topExpression != null)
					provider = new TopProvider (provider, topExpression);
			} else {
				provider = new NullProvider ();
			
				if (distinct)
					throw new ParserException ("DISTINCT clause not allowed without a FROM clause.", selectTree);
				
				if (topExpression != null) 
					throw new ParserException ("TOP clause not allowed without a FROM clause.", selectTree);

				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE")
					throw new ParserException ("WHERE clause not allowed without a FROM clause.", selectTree);
				
				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY")
					throw new ParserException ("ORDER BY clause not allowed without a FROM clause.", selectTree);

				provider = new SelectProvider (outputColumns, provider);
			}
			
			if (intoFile != null)
				provider = new IntoProvider (provider, intoFile);
			
			return provider;
		}
		
		Expression<long> ParseTopClause (CommonTree topClauseTree)
		{
			CommonTree tree = GetSingleChild (topClauseTree);
			return ExpressionHelper.ConvertIfNeeded<long> (ParseExpression (tree));
		}

		IList<IExpression> ParseColumnList (CommonTree outputListTree)
		{
			List<IExpression > outputColumnExpressions = new List<IExpression> ();
			AssertAntlrToken (outputListTree, "T_COLUMNLIST", 1, -1);
			foreach (CommonTree outputColumnTree in outputListTree.Children) {
				IExpression columnExpression = ParseColumn (outputColumnTree);
				outputColumnExpressions.Add (columnExpression);
			}
			
			return outputColumnExpressions;
		}

		IExpression ParseColumn (CommonTree outputColumnTree)
		{
			return ParseExpression (outputColumnTree);
		}

		FileOptions ParseIntoClause (CommonTree intoClauseTree)
		{
			AssertAntlrToken (intoClauseTree, "T_INTO", 1);
				
			CommonTree fileTree = GetSingleChild (intoClauseTree);
			FileOptions intoFile = ParseFile (fileTree);			
			
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

		Expression<bool> ParseWhereClause (CommonTree whereTree)
		{
			AssertAntlrToken (whereTree, "T_WHERE");
			
			CommonTree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (expressionTree);
			if (!(expression is Expression<bool>)) {
				throw new ParserException ("Expected boolean expression in WHERE clause.", expressionTree);
			}
			return (Expression<bool>)expression;
		}
		
		IList<OrderbyProvider.Column> ParseOrderbyClause (CommonTree orderbyTree)
		{
			AssertAntlrToken (orderbyTree, "T_ORDERBY");
						
			List<OrderbyProvider.Column > orderbyColumns = new List<OrderbyProvider.Column> ();
			foreach (CommonTree orderbyColumnTree in orderbyTree.Children) {
				orderbyColumns.Add (ParseOrderbyColumn (orderbyColumnTree));
			}
			
			return orderbyColumns;
		}
		
		OrderbyProvider.Column ParseOrderbyColumn (CommonTree orderbyColumnTree)
		{
			AssertAntlrToken (orderbyColumnTree, "T_ORDERBY_COLUMN", 1, 2);
			
			OrderbyProvider.Column orderbyColumn = new OrderbyProvider.Column ();
			orderbyColumn.Expression = ParseExpression ((CommonTree)orderbyColumnTree.Children [0]);
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

		IExpression ParseExpression (CommonTree expressionTree)
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
				expression = ParseExpressionFunctionCall (expressionTree);
				break;
			case "T_CONVERT":
				expression = ParseExpressionConvert (expressionTree);
				break;
			case "T_OP_UNARY":
				expression = ParseExpressionOperatorUnary (expressionTree);
				break;
			case "T_OP_BINARY":
				expression = ParseExpressionOperatorBinary (expressionTree);
				break;
			case "T_EXISTS":
				expression = ParseExpressionExists (expressionTree);
				break;
			default:
				throw new UnexpectedTokenAntlrException (expressionTree);
			}
			
			return expression;
		}

		Expression<T> ParseExpression<T> (CommonTree expressionTree) where T : IComparable
		{
			IExpression expression = ParseExpression (expressionTree);
			Expression<T > expressionT = expression as Expression<T>;
			if (expressionT == null)
				throw new ParserException (string.Format ("Expected expression of type '{0}'", typeof(T).Name), expressionTree);
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
				expression = new FileNameSystemVar ();
				break;
			case "$LINENO":
				expression = new LineNoSystemVar ();
				break;
			default:
				throw new ParserException (string.Format ("Unknown system variable '{0}'.", tree.Text), tree);
			}
			
			return expression;
		}

		IExpression ParseExpressionFunctionCall (CommonTree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL");

			string functionName = functionCallTree.Children [0].Text;
			
			IExpression result;
			int argCount = functionCallTree.Children.Count - 1;
			switch (argCount) {
			case 0:
				result = ParseExpressionFunctionCall_0 (functionCallTree, functionName);
				break;
			case 1:
				result = ParseExpressionFunctionCall_1 (functionCallTree, functionName);
				break;
			case 2:
				result = ParseExpressionFunctionCall_2 (functionCallTree, functionName);
				break;
			case 3:
				result = ParseExpressionFunctionCall_3 (functionCallTree, functionName);
				break;
			default:
				throw new ParserException (string.Format ("Function call with '{0}' arguments not supported.", argCount), functionCallTree);
			}
			
			return result;
		}
				
		IExpression ParseExpressionFunctionCall_0 (CommonTree functionCallTree, string functionName)
		{
			//IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			default:
				throw new ParserException (string.Format ("Function call to {0} with 0 parameters not supported.", functionName), 
					functionCallTree);
			}
			
			//return result;
		}

		IExpression ParseExpressionFunctionCall_1 (CommonTree functionCallTree, string functionName)
		{
			IExpression arg = ParseExpression ((CommonTree)functionCallTree.Children [1]);

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
			default:
				throw new ParserException (string.Format ("Function call to {0} with 1 parameters not supported.", functionName), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionFunctionCall_2 (CommonTree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression ((CommonTree)functionCallTree.Children [1]);
			IExpression arg2 = ParseExpression ((CommonTree)functionCallTree.Children [2]);
			
			IExpression result;
			
			switch (functionName.ToUpperInvariant ()) {
			case "CONTAINS":
				result = new BinaryExpression<string, string, bool> ((a, b) => a.IndexOf (b, stringComparison) != -1, arg1, arg2);
				break;
			case "LEFT":
				result = new BinaryExpression<string, int, string> ((a, b) => a.Substring (0, Math.Min (b, a.Length)), arg1, arg2);
				break;
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, caseInsensitive);
				break;
			case "RIGHT":
				result = new BinaryExpression<string, int, string> ((a, b) => a.Substring (a.Length - Math.Min (b, a.Length)), arg1, arg2);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2);
				break;
			default:
				throw new ParserException (string.Format ("Function call to {0} with 2 parameters not supported.", functionName), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionFunctionCall_3 (CommonTree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression ((CommonTree)functionCallTree.Children [1]);
			IExpression arg2 = ParseExpression ((CommonTree)functionCallTree.Children [2]);
			IExpression arg3 = ParseExpression ((CommonTree)functionCallTree.Children [3]);
			
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
				throw new ParserException (string.Format ("Function call to {0} with 2 parameters not supported.", functionName), 
					functionCallTree);
			}
			
			return result;
		}

		IExpression ParseExpressionConvert (CommonTree convertTree)
		{
			AssertAntlrToken (convertTree, "T_CONVERT", 2);
			
			string dataType = convertTree.Children [0].Text;
			IExpression expr = ParseExpression ((CommonTree)convertTree.Children [1]);
			
			IExpression result;
			switch (dataType.ToUpperInvariant ()) {
			case "INT":
				result = new ConvertExpression<long> (expr);
				break;
			case "STRING":
				result = new ConvertToStringExpression (expr);
				break;
			default:
				throw new ParserException (string.Format ("Datatype {0} not supported in CONVERT function.", dataType), convertTree.Children [1]);
			}
			return result;
		}

		FileOptions ParseFile (CommonTree fileProvider)
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
					switch (option.ToUpperInvariant ()) {
					case "RECURSE":
						fileOptions.Recurse = true;
						break;
					case "LINEEND":
						FileOptions.NewLineEnum lineEnd;
						if (!Enum.TryParse<FileOptions.NewLineEnum> (value, true, out lineEnd))
							throw new ParserException (string.Format ("Unknown file option LineEnd={0}", value), enumerator.Current);
						fileOptions.NewLine = lineEnd;
						break;
					case "APPEND":
						fileOptions.Append = true;
						break;
					default:
						throw new ParserException (string.Format ("Unknown file option '{0}'", option), enumerator.Current);  
					}
					
					enumerator.MoveNext ();
				}
			} else
				throw new ParserException ("Expected '[' or '\'' for file specification", enumerator.Current);
			
			return fileOptions;
		}

		void ParseFileOption (CommonTree fileOptionTree, out string option, out string value)
		{
			AssertAntlrToken (fileOptionTree, "T_FILEOPTION", 1, 2);
			
			option = fileOptionTree.Children [0].Text;
			if (fileOptionTree.Children.Count > 1)
				value = fileOptionTree.Children [1].Text;
			else
				value = null;
		}

		IProvider ParseFileProvider (CommonTree fileProvider)
		{
			FileOptions fileOptions = ParseFile (fileProvider);
			
			IProvider provider = FileProviderFactory.Get (fileOptions);
			
			return provider;
		}
		
		IProvider ParseSubquery (CommonTree subqueryTree)
		{
			AssertAntlrToken (subqueryTree, "T_SUBQUERY");
			
			CommonTree selectTree = GetSingleChild (subqueryTree);
			return ParseCommandSelect (selectTree);
		}
		
		IExpression ParseExpressionOperatorUnary (CommonTree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_UNARY", 2);
			
			IExpression arg = ParseExpression ((CommonTree)operatorTree.Children [1]);			
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
				throw new ParserException (string.Format ("Unknown unary operator '{0}'.", operatorText), operatorTree);
			}
			
			return result;
		}

		IExpression ParseExpressionOperatorBinary (CommonTree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3, 4);
			
			string operatorText = operatorTree.Children [0].Text;
			if (operatorText == "T_BETWEEN") {
				return ParseExpressionBetween ((CommonTree)operatorTree);
			} else if (operatorText == "T_NOTBETWEEN") {
				return new UnaryExpression<bool, bool> ((a) => !a, ParseExpressionBetween ((CommonTree)operatorTree));
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseExpressionInSomeAnyAll ((CommonTree)operatorTree);
			} else if (operatorText == "T_NOTIN") {
				return new UnaryExpression<bool, bool> ((a) => !a, ParseExpressionInSomeAnyAll ((CommonTree)operatorTree));
			} 
			
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3);

			IExpression arg1 = ParseExpression ((CommonTree)operatorTree.Children [1]);			
			IExpression arg2 = ParseExpression ((CommonTree)operatorTree.Children [2]);			
			IExpression result;
			
			switch (operatorText) {
			case "T_AND":
				result = new BinaryExpression<bool, bool, bool> ((a, b) => a && b, arg1, arg2);
				break;
			case "T_OR":
				result = new BinaryExpression<bool, bool, bool> ((a, b) => a || b, arg1, arg2);
				break;
			case "T_MATCH":
				result = new MatchOperator (arg1, arg2, caseInsensitive);
				break;
			case "T_NOTMATCH":
				result = new UnaryExpression<bool, bool> ((a) => !a, new MatchOperator (arg1, arg2, caseInsensitive));
				break;
			case "T_LIKE":
				result = new LikeOperator (arg1, arg2, caseInsensitive);
				break;
			case "T_NOTLIKE":
				result = new UnaryExpression<bool, bool> ((a) => !a, new LikeOperator (arg1, arg2, caseInsensitive));
				break;
			case "T_PLUS":
				{
					if (arg1 is Expression<string> || arg2 is Expression<string>)
						result = new BinaryExpression<string, string, string> ((a, b) => a + b, arg1, arg2);
					else if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a + b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'PLUS' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_MINUS":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a - b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'MINUS' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_DIVIDE":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a / b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'DIVIDE' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_PRODUCT":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a * b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'PRODUCT' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_MODULO":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a % b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'MODULO' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_AND":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a & b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'BITWISE AND' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_OR":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a | b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'BITWISE OR' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
							operatorTree);
					}
				}
				break;
			case "T_BITWISE_XOR":
				{
					if (arg1 is Expression<long>)
						result = new BinaryExpression<long, long, long> ((a, b) => a ^ b, arg1, arg2);
					else {
						throw new ParserException (
							string.Format ("Binary operator 'BITWISE XOR' cannot be used with datatypes {0} and {1}",
					               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
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
						new BinaryExpression<string, string, bool> (OperatorHelper.GetStringComparer(operatorText, false, stringComparison),
							arg1, arg2);
				else if (arg1 is Expression<long>)
					result = 
						new BinaryExpression<long, long, bool> (OperatorHelper.GetLongComparer(operatorText, false),
							arg1, arg2);
				else {
					throw new ParserException (
						string.Format ("Binary operator 'EQUAL' cannot be used with datatypes {0} and {1}",
				               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString ()),
						operatorTree);
				}
				break;
			default:
				throw new ParserException (string.Format ("Unknown binary operator '{0}'.", operatorText), operatorTree);
			}
			
			return result;
		}
		
		IExpression ParseExpressionBetween (CommonTree betweenTree)
		{
			AssertAntlrToken (betweenTree, "T_OP_BINARY", 3);
			//AssertAntlrToken (betweenTree.Children [0], "T_BETWEEN"); or T_NOTBETWEEN
			CommonTree andTree = (CommonTree)betweenTree.Children [2];
			AssertAntlrToken (andTree, "T_OP_BINARY", 3);
			AssertAntlrToken (andTree.Children [0], "T_AND");
			
			IExpression arg1 = ParseExpression ((CommonTree)betweenTree.Children [1]);
			IExpression arg2 = ParseExpression ((CommonTree)andTree.Children [1]);
			IExpression arg3 = ParseExpression ((CommonTree)andTree.Children [2]);

			IExpression result;
			if (arg1 is Expression<string> || arg2 is Expression<string> || arg3 is Expression<string>)
				result = new TernaryExpression<string, string, string, bool> ((a, b, c) => 
				                                                      string.Compare (a, b, stringComparison) >= 0 
				                                                      && string.Compare (a, c, stringComparison) <= 0, arg1, arg2, arg3);
			else if (arg1 is Expression<long>)
				result = new TernaryExpression<long, long, long, bool> ((a, b, c) => (a >= b) && (a <= c), arg1, arg2, arg3);
			else {
				throw new ParserException (
					string.Format ("Ternary operator 'BETWEEN' cannot be used with datatypes {0}, {1} and {2}",
			               arg1.GetResultType ().ToString (), arg2.GetResultType ().ToString (),
				           arg3.GetResultType ().ToString ()),
					betweenTree);
			}
			
			return result;
		}

		IExpression ParseExpressionInSomeAnyAll (CommonTree inTree)
		{
			AssertAntlrToken (inTree, "T_OP_BINARY", 3, 4);
			//AssertAntlrToken (inTree.Children [0], "T_IN"); or T_NOTIN, T_ANY, T_ALL
			
			IExpression arg2;
			CommonTree target;			
			bool all;
			string op;
			switch (inTree.Children[0].Text) {
			case "T_IN":
			case "T_NOTIN":
				arg2 = ParseExpression ((CommonTree)inTree.Children [1]);
				target = (CommonTree)inTree.Children [2];
				all = false;
				op = "T_EQUAL";
				break;
			case "T_ANY":
				arg2 = ParseExpression ((CommonTree)inTree.Children [2]);
				target = (CommonTree)inTree.Children [3];
				all = false;
				op = inTree.Children [1].Text;
				break;
			case "T_ALL":
				arg2 = ParseExpression ((CommonTree)inTree.Children [2]);
				target = (CommonTree)inTree.Children [3];
				all = true;
				op = inTree.Children [1].Text;
				break;
			default:
				throw new ParserException(string.Format("Unexpected token {0}", inTree.Children[0].Text), inTree.Children[0]);
			}
						
			Expression<bool> result;
			if (target.Text == "T_EXPRESSIONLIST") {
				IExpression[] expressionList = ParseExpressionList (target);
				if (arg2 is Expression<string>)
					result = new AnyListOperator<string> ((Expression<string>)arg2, expressionList, OperatorHelper.GetStringComparer(op, all, stringComparison));
				else if (arg2 is Expression<long>)
					result = new AnyListOperator<long> ((Expression<long>)arg2, expressionList, OperatorHelper.GetLongComparer(op, all));
				else
					throw new ParserException (string.Format ("Binary operator '{0}' cannot be used with datatype {1}", inTree.Children[0].Text, target.Text), target);
			} else if (target.Text == "T_SELECT") {
				IProvider provider = ParseCommandSelect (target);
				if (arg2 is Expression<string>)
					result = new AnySubqueryOperator<string> ((Expression<string>)arg2, provider, OperatorHelper.GetStringComparer(op, all, stringComparison));
				else if (arg2 is Expression<long>)
					result = new AnySubqueryOperator<long> ((Expression<long>)arg2, provider, OperatorHelper.GetLongComparer(op, all));
				else
					throw new ParserException (string.Format ("Binary operator '{0}' cannot be used with datatype {1}", inTree.Children[0].Text, target.Text), target);
			} else {
				throw new ParserException (string.Format ("Binary operator '{0}' cannot be used with argument {1}", inTree.Children[0].Text, arg2.GetResultType ().ToString ()), target);
			}
		
			if (all)
				result = new UnaryExpression<bool, bool>(a => !a, result);
					
			return result;
		}

		IExpression[] ParseExpressionList (CommonTree expressionListTree)
		{
			AssertAntlrToken (expressionListTree, "T_EXPRESSIONLIST", 1, -1);
			
			IExpression[] result = new IExpression[expressionListTree.Children.Count];
			for (int i = 0; i < expressionListTree.Children.Count; i++) {
				result [i] = ParseExpression ((CommonTree)expressionListTree.Children [i]);
			}			
			
			return result;
		}

		IExpression ParseExpressionExists (CommonTree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_EXISTS", 1, 1);
			
			return new AnySubqueryOperator<long>(
				new ConstExpression<long>(1),
				new SelectProvider(new IExpression[] { new ConstExpression<long>(1) }, 
					new TopProvider(ParseCommandSelect((CommonTree)expressionTree.Children[0]), new ConstExpression<long>(1))),
				(a, b) => a == b);
			;
		}
	}
}

