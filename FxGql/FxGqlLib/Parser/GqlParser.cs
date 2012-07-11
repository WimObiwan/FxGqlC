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
    
	class GqlParser
	{
		readonly GqlEngineState gqlEngineState;
		readonly string command;
		//readonly CultureInfo cultureInfo;
		readonly bool caseInsensitive;
		readonly StringComparer stringComparer;
		readonly StringComparison stringComparison;

		Dictionary<string, Type> variableTypes = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase);
		Dictionary<string, IProvider> views = new Dictionary<string, IProvider> (StringComparer.InvariantCultureIgnoreCase);
		Stack<IProvider> subQueryProviderStack = new Stack<IProvider> ();
        
		public GqlParser (GqlEngineState gqlEngineState, string command)
            : this(gqlEngineState, command, CultureInfo.InvariantCulture, true)
		{
		}
        
		public GqlParser (GqlEngineState gqlEngineState, string command, CultureInfo cultureInfo, bool caseInsensitive)
		{
			this.gqlEngineState = gqlEngineState;
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

		ITree GetSingleChild (ITree tree)
		{
			if (tree.ChildCount != 1)
				throw new NotEnoughSubTokensAntlrException (tree);
			return tree.GetChild (0);
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
				commands = ParseCommands ((ITree)result.Tree);
			} catch (RecognitionException) {
				// TODO: outer exception
				throw;
			}
            
			return commands;
		}
        
		IList<IGqlCommand> ParseCommands (ITree tree)
		{
			AssertAntlrToken (tree, "T_ROOT");
            
			List<IGqlCommand > commands = new List<IGqlCommand> ();
			foreach (ITree commandTree in new AntlrTreeChildEnumerable(tree)) {
				commands.Add (ParseCommand (commandTree));
			}
            
			return commands;
		}
        
		IGqlCommand ParseCommand (ITree tree)
		{
			switch (tree.Text) {
			case "T_SELECT":
				return new GqlQueryCommand (ParseCommandSelect (tree));
			case "T_USE":
				return new UseCommand (ParseCommandUse (tree));
			case "T_DECLARE":
				{
					var variableDeclaration = ParseCommandDeclare (tree);
					foreach (var variable in variableDeclaration) {
						variableTypes [variable.Item1] = variable.Item2;
					}
					return new DeclareCommand (variableDeclaration);
				}
			case "T_SET_VARIABLE":
				return new SetVariableCommand (ParseCommandSetVariable (tree));
			case "T_CREATE_VIEW":
				{
					var createView = ParseCommandCreateView (tree);
					string view = createView.Item1;
					IProvider provider = createView.Item2;
					views.Add (view, provider);
					return new CreateViewCommand (view, provider);
				}
			case "T_DROP_VIEW":
				{
					string view = ParseCommandDropView (tree);
					views.Remove (view);
					return new DropViewCommand (view);
				}
			case "T_DROP_TABLE":
				{
					return new DropTableCommand (ParseCommandDropTable (tree));
				}
			default:
				throw new UnexpectedTokenAntlrException (tree);
			}
		}

		IProvider ParseCommandSelect (ITree tree)
		{
			AssertAntlrToken (tree, "T_SELECT");
            
			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			if (!enumerator.MoveNext ())
				throw new NotEnoughSubTokensAntlrException (tree);
            
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
				topExpression = ParseTopClause (enumerator.Current);
				enumerator.MoveNext ();
			} else {
				topExpression = null;
			}

			// columns
			if (enumerator.Current == null)
				throw new NotEnoughSubTokensAntlrException (tree);
			ITree columnListEnumerator = enumerator.Current;
			enumerator.MoveNext ();
                
			// INTO
			FileOptionsIntoClause intoFile;
			if (enumerator.Current != null && enumerator.Current.Text == "T_INTO") {
				intoFile = ParseIntoClause (enumerator.Current);
				enumerator.MoveNext ();
			} else {
				intoFile = null;
			}
            
			// FROM
			IProvider provider;
			if (enumerator.Current != null && enumerator.Current.Text == "T_FROM") {
				IProvider fromProvider = ParseFromClause (enumerator.Current);
				provider = fromProvider;
				enumerator.MoveNext ();
                                
				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE") {
					Expression<bool> whereExpression = ParseWhereClause (
                        fromProvider,
                        enumerator.Current
					);
					enumerator.MoveNext ();

					provider = new FilterProvider (provider, whereExpression);
				}
                
				IList<Column> outputColumns;
				outputColumns = ParseColumnList (fromProvider, columnListEnumerator);

				if (enumerator.Current != null && enumerator.Current.Text == "T_GROUPBY") {
					IList<OrderbyProvider.Column> groupbyColumns = ParseGroupbyClause (
                        fromProvider,
                        enumerator.Current
					);
					enumerator.MoveNext ();
                    
					Expression<bool> havingExpression;
					if (enumerator.Current != null && enumerator.Current.Text == "T_HAVING") {
						havingExpression = ParseHavingClause (
	                        fromProvider,
	                        enumerator.Current
						);
						enumerator.MoveNext ();
					} else {
						havingExpression = null;
					}

					provider = new GroupbyProvider (
                        provider,
                        groupbyColumns.Where (p => p.Order == OrderbyProvider.OrderEnum.ORIG).Select (p => p.Expression).ToList (),
                        groupbyColumns.Where (p => p.Order != OrderbyProvider.OrderEnum.ORIG).Select (p => p.Expression).ToList (),
                        outputColumns,
						havingExpression,
                        stringComparer
					);
				} else {
					// e.g. select count(1) from [myfile.txt]
					if (outputColumns.Any (p => p is SingleColumn && ((SingleColumn)p).Expression.IsAggregated ())) {
						provider = new GroupbyProvider (
	                        provider,
	                        outputColumns,
	                        stringComparer
						);
					} else {
						provider = new ColumnProvider (outputColumns, provider);
					}
				}
                
				if (distinct)
					provider = new DistinctProvider (provider, stringComparer);
                
				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY") {
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
                        fromProvider,
                        enumerator.Current
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
                        tree
					);
                
				if (topExpression != null) 
					throw new ParserException (
                        "TOP clause not allowed without a FROM clause.",
                        tree
					);

				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE")
					throw new ParserException (
                        "WHERE clause not allowed without a FROM clause.",
                        tree
					);
                
				if (enumerator.Current != null && enumerator.Current.Text == "T_GROUPBY")
					throw new ParserException (
                        "GROUP BY clause not allowed without a FROM clause.",
                        tree
					);

				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY")
					throw new ParserException (
                        "ORDER BY clause not allowed without a FROM clause.",
                        tree
					);

				IList<Column > outputColumns;
				outputColumns = ParseColumnList (provider, columnListEnumerator);

				provider = new SelectProvider (outputColumns, provider);
			}
            
			if (intoFile != null)
				provider = new IntoProvider (provider, intoFile);
            
			return provider;
		}
        
		Expression<long> ParseTopClause (ITree topClauseTree)
		{
			ITree tree = GetSingleChild (topClauseTree);
			return ExpressionHelper.ConvertIfNeeded<long> (ParseExpression (null, tree));
		}
        
		IList<Column> ParseColumnList (IProvider provider, ITree outputListTree)
		{
			List<Column > outputColumnExpressions = new List<Column> ();
			AssertAntlrToken (outputListTree, "T_COLUMNLIST", 1, -1);
			foreach (ITree outputColumnTree in new AntlrTreeChildEnumerable(outputListTree)) {
				Column column = ParseColumn (provider, outputColumnTree);
				outputColumnExpressions.Add (column);
			}
            
			return outputColumnExpressions;
		}

		Column ParseColumn (IProvider provider, ITree outputColumnTree)
		{
			Column column;
			if (outputColumnTree.Text == "T_ALLCOLUMNS") {
				AssertAntlrToken (outputColumnTree, "T_ALLCOLUMNS", 0, 1);

				string providerAlias;
				if (outputColumnTree.ChildCount == 1)
					providerAlias = ParseProviderAlias (outputColumnTree.GetChild (0));
				else 
					providerAlias = null;

				column = new AllColums (providerAlias, provider);
			} else {
				AssertAntlrToken (outputColumnTree, "T_COLUMN", 1, 2);

				IExpression expression = ParseExpression (
                    provider,
                    outputColumnTree.GetChild (0)
				);

				ColumnName columnName;
				if (outputColumnTree.ChildCount == 2) {
					string name = ParseColumnName (outputColumnTree.GetChild (1));
					columnName = new ColumnName (name);
				} else if (expression is IColumnExpression) {
					IColumnExpression columnExpression = (IColumnExpression)expression;
					columnName = columnExpression.ColumnName;
				} else {
					columnName = null;
				}
				column = new SingleColumn (columnName, expression);
			}
            
			return column; 
		}

		FileOptionsIntoClause ParseIntoClause (ITree intoClauseTree)
		{
			AssertAntlrToken (intoClauseTree, "T_INTO", 1);
                
			ITree fileTree = GetSingleChild (intoClauseTree);
			FileOptionsIntoClause intoFile = ParseFileIntoClause (fileTree);
            
			return intoFile;
		}
        
		IProvider ParseFromClause (ITree fromClauseTree)
		{
			AssertAntlrToken (fromClauseTree, "T_FROM", 1, -1);
            
			var providers = new List<IProvider> ();
            
			string providerAlias = null;
			foreach (ITree inputProviderTree in new AntlrTreeChildEnumerable(fromClauseTree)) {
				IProvider provider;
				switch (inputProviderTree.Text) {
				case "T_FILE":
					provider = ParseFileProvider (inputProviderTree);
					break;
				case "T_SUBQUERY":
					provider = ParseSubquery (null, inputProviderTree);
					break;
				case "T_VIEW_NAME":
					provider = ParseViewProvider (inputProviderTree);
					break;
				case "T_TABLE_ALIAS":
					providerAlias = ParseProviderAlias (inputProviderTree);
					continue;
				default:
					throw new UnexpectedTokenAntlrException (inputProviderTree);
				}

				providers.Add (provider);
			}
            
			IProvider fromProvider;
			if (providers.Count == 1)
				fromProvider = providers [0];
			else
				fromProvider = new MergeProvider (providers);
            
			if (providerAlias != null)
				fromProvider = new NamedProvider (fromProvider, providerAlias);

			return fromProvider;
		}

		Expression<bool> ParseWhereClause (IProvider provider, ITree whereTree)
		{
			AssertAntlrToken (whereTree, "T_WHERE");
            
			ITree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (provider, expressionTree);
			if (!(expression is Expression<bool>)) {
				throw new ParserException (
                    "Expected boolean expression in WHERE clause.",
                    expressionTree
				);
			}
			return (Expression<bool>)expression;
		}
        
		Expression<bool> ParseHavingClause (IProvider provider, ITree whereTree)
		{
			AssertAntlrToken (whereTree, "T_HAVING");
            
			ITree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (provider, expressionTree);
			if (!(expression is Expression<bool>)) {
				throw new ParserException (
                    "Expected boolean expression in HAVING clause.",
                    expressionTree
				);
			}
			return (Expression<bool>)expression;
		}
        
		IList<OrderbyProvider.Column> ParseGroupbyClause (IProvider provider, ITree groupbyTree)
		{
			AssertAntlrToken (groupbyTree, "T_GROUPBY", 1, -1);
        
			return ParseOrderbyList (provider, groupbyTree);
		}
        
		IList<OrderbyProvider.Column> ParseOrderbyClause (IProvider provider, ITree orderbyTree)
		{
			AssertAntlrToken (orderbyTree, "T_ORDERBY", 1, -1);
                        
			return ParseOrderbyList (provider, orderbyTree);
		}
        
		IList<OrderbyProvider.Column> ParseOrderbyList (IProvider provider, ITree orderbyTree)
		{
			List<OrderbyProvider.Column > orderbyColumns = new List<OrderbyProvider.Column> ();

			bool nonOrig = false;
			foreach (ITree orderbyColumnTree in new AntlrTreeChildEnumerable(orderbyTree)) {
				OrderbyProvider.Column column = ParseOrderbyColumn (provider, orderbyColumnTree);

				if (column.Order == OrderbyProvider.OrderEnum.ORIG) {
					if (nonOrig)
						throw new ParserException ("ORIG order/group by column order specifications must precede any other order specifications", orderbyColumnTree);
				} else {
					nonOrig = true;
				}

				orderbyColumns.Add (column);
			}
            
			return orderbyColumns;
		}

		OrderbyProvider.Column ParseOrderbyColumn (IProvider provider, ITree orderbyColumnTree)
		{
			AssertAntlrToken (orderbyColumnTree, "T_ORDERBY_COLUMN", 1, 2);
            
			OrderbyProvider.Column orderbyColumn = new OrderbyProvider.Column ();
			orderbyColumn.Expression = ParseExpression (
                provider,
                orderbyColumnTree.GetChild (0)
			);
			if (orderbyColumnTree.ChildCount > 1) {
				string order = orderbyColumnTree.GetChild (1).Text;
				switch (order) {
				case "T_ORDERBY_ASC":
					orderbyColumn.Order = OrderbyProvider.OrderEnum.ASC;
					break;
				case "T_ORDERBY_DESC":
					orderbyColumn.Order = OrderbyProvider.OrderEnum.DESC;
					break;
				case "T_ORDERBY_ORIG":
					orderbyColumn.Order = OrderbyProvider.OrderEnum.ORIG;
					break;
				default:
					throw new ParserException ("Expected ASC, DESC or ORIG as ORDER BY column order",
                                              orderbyColumnTree.GetChild (1));
				}
			} else {
				orderbyColumn.Order = OrderbyProvider.OrderEnum.ASC;
			}
            
			return orderbyColumn;
		}

		IExpression ParseExpression (IProvider provider, ITree tree)
		{
			IExpression expression;
			switch (tree.Text.ToUpperInvariant ()) {
			case "T_INTEGER":
				expression = ParseExpressionInteger (tree);
				break;
			case "T_STRING":
				expression = ParseExpressionString (tree);
				break;
			case "T_SYSTEMVAR":
				expression = ParseExpressionSystemVar (tree);
				break;
			case "T_FUNCTIONCALL":
				expression = ParseExpressionFunctionCall (provider, tree);
				break;
			case "T_CONVERT":
				expression = ParseExpressionConvert (provider, tree);
				break;
			case "T_OP_UNARY":
				expression = ParseExpressionOperatorUnary (provider, tree);
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
			default:
				throw new UnexpectedTokenAntlrException (tree);
			}
            
			return expression;
		}

		Expression<T> ParseExpression<T> (IProvider provider, ITree expressionTree) where T : IComparable
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
        
		Expression<long> ParseExpressionInteger (ITree expressionNumberTree)
		{
			ITree tree = GetSingleChild (expressionNumberTree);
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

		Expression<string> ParseExpressionString (ITree expressionStringTree)
		{
			ITree tree = GetSingleChild (expressionStringTree);
            
			string text = ParseString (tree);
			return new ConstExpression<string> (text);
		}

		IExpression ParseExpressionSystemVar (ITree expressionSystemVarTree)
		{
			ITree tree = GetSingleChild (expressionSystemVarTree);
            
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

		IExpression ParseExpressionFunctionCall (IProvider provider, ITree functionCallTree)
		{
			AssertAntlrToken (functionCallTree, "T_FUNCTIONCALL", 1, -1);

			string functionName = functionCallTree.GetChild (0).Text;
            
			IExpression result;
			int argCount = functionCallTree.ChildCount - 1;
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
                
		IExpression ParseExpressionFunctionCall_0 (IProvider provider, ITree functionCallTree, string functionName)
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

		IExpression ParseExpressionFunctionCall_1 (IProvider provider, ITree functionCallTree, string functionName)
		{
			IExpression arg;

			string functionNameUpper = functionName.ToUpperInvariant ();
			if (functionNameUpper == "COUNT" && functionCallTree.GetChild (1).Text == "T_ALLCOLUMNS") {
				ITree allColumnsTree = functionCallTree.GetChild (1);
				string providerAlias;
				if (allColumnsTree.ChildCount == 1)
					providerAlias = ParseProviderAlias (allColumnsTree.GetChild (0));
				else 
					providerAlias = null;

				if (providerAlias != null) {
					string[] providerAliases = provider.GetAliases ();
					if (!providerAliases.Any (p => StringComparer.InvariantCultureIgnoreCase.Compare (p, providerAlias) == 0)) {
						throw new InvalidOperationException (string.Format ("Invalid provider alias '{0}'", providerAlias));
					}
				}
				arg = new ConstExpression<long> (1);
			} else {
				arg = ParseExpression (provider, functionCallTree.GetChild (1));
			}

			IExpression result;
            
			switch (functionNameUpper) {
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

		IExpression ParseExpressionFunctionCall_2 (IProvider provider, ITree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression (
                provider,
                functionCallTree.GetChild (1)
			);
			IExpression arg2 = ParseExpression (
                provider,
                functionCallTree.GetChild (2)
			);
            
			AdjustAggregation (ref arg1, ref arg2);

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

		IExpression ParseExpressionFunctionCall_3 (IProvider provider, ITree functionCallTree, string functionName)
		{
			IExpression arg1 = ParseExpression (
                provider,
                functionCallTree.GetChild (1)
			);
			IExpression arg2 = ParseExpression (
                provider,
                functionCallTree.GetChild (2)
			);
			IExpression arg3 = ParseExpression (
                provider,
                functionCallTree.GetChild (3)
			);
            
			AdjustAggregation (ref arg1, ref arg2, ref arg3);

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

		IExpression ParseExpressionConvert (IProvider provider, ITree convertTree)
		{
			AssertAntlrToken (convertTree, "T_CONVERT", 2);
            
			Type dataType = ParseDataType (convertTree.GetChild (0));
			IExpression expr = ParseExpression (
                provider,
                convertTree.GetChild (1)
			);
            
			IExpression result;
			if (dataType == typeof(long)) {
				result = new ConvertExpression<long> (expr);
			} else if (dataType == typeof(string)) {
				result = new ConvertToStringExpression (expr);
			} else {    
				throw new ParserException (
                    string.Format (
                    "Datatype {0} not supported in CONVERT function.",
                    dataType
				),
                    convertTree.GetChild (1)
				);
			}
			return result;
		}

		FileOptionsFromClause ParseFileFromClause (ITree fileProvider)
		{
			FileOptionsFromClause fileOptions = new FileOptionsFromClause ();

			List<Tuple<string, string, ITree>> options = ParseFileCommon (
                fileProvider,
                fileOptions
			);

			foreach (Tuple<string, string, ITree> option in options) {
				string key = option.Item1;
				string value = option.Item2;
				ITree tree = option.Item3;
				switch (key.ToUpperInvariant ()) {
				case "FILEORDER":
					FileOptionsFromClause.FileOrderEnum order;
					if (!Enum.TryParse<FileOptionsFromClause.FileOrderEnum> (
                        value,
                        true,
                        out order
					))
						throw new ParserException (
                                    string.Format ("Unknown file option FileOrder={0}", value),
                                    tree
						);
					fileOptions.FileOrder = order;
					break;
				case "RECURSE":
					fileOptions.Recurse = true;
					break;
				case "HEADING":
					GqlEngineState.HeadingEnum heading;
					if (!Enum.TryParse<GqlEngineState.HeadingEnum> (value, true, out heading))
						throw new ParserException (
                                    string.Format ("Unknown file option Heading={0}", value),
                                    tree
						);
					fileOptions.Heading = heading;
					break;
				case "COLUMNS":
					fileOptions.ColumnsRegex = value;
					break;
				case "SKIP":
					fileOptions.Skip = long.Parse (value);
					break;
				case "COLUMNDELIMITER":
					fileOptions.ColumnDelimiter = System.Text.RegularExpressions.Regex.Unescape (value);
					break;
				default:
					throw new ParserException (
                                string.Format ("Unknown file option '{0}'", option),
                                tree
					);  
				}
			}

			return fileOptions;
		}

		FileOptionsIntoClause ParseFileIntoClause (ITree fileProvider)
		{
			FileOptionsIntoClause fileOptions = new FileOptionsIntoClause ();

			List<Tuple<string, string, ITree>> options = ParseFileCommon (
                fileProvider,
                fileOptions
			);

			foreach (Tuple<string, string, ITree> option in options) {
				string key = option.Item1;
				string value = option.Item2;
				ITree tree = option.Item3;
				switch (key.ToUpperInvariant ()) {
				case "LINEEND":
					FileOptionsIntoClause.NewLineEnum lineEnd;
					if (!Enum.TryParse<FileOptionsIntoClause.NewLineEnum> (
                        value,
                        true,
                        out lineEnd
					))
						throw new ParserException (
                                    string.Format ("Unknown file option LineEnd={0}", value),
                                    tree
						);
					fileOptions.NewLine = lineEnd;
					break;
				case "APPEND":
					fileOptions.Append = true;
					break;
				case "OVERWRITE":
					fileOptions.Overwrite = true;
					break;
				case "HEADING":
					GqlEngineState.HeadingEnum heading;
					if (!Enum.TryParse<GqlEngineState.HeadingEnum> (value, true, out heading))
						throw new ParserException (
                                    string.Format ("Unknown file option Heading={0}", value),
                                    tree
						);
					fileOptions.Heading = heading;
					break;
				case "COLUMNDELIMITER":
					fileOptions.ColumnDelimiter = System.Text.RegularExpressions.Regex.Unescape (value);
					break;
				default:
					throw new ParserException (
                                string.Format ("Unknown file option '{0}'", option),
                                tree
					);  
				}
			}

			return fileOptions;
		}

		FileOptions ParseFileSimple (ITree tree)
		{
			FileOptionsIntoClause fileOptions = new FileOptionsIntoClause ();

			List<Tuple<string, string, ITree>> options = ParseFileCommon (
                tree,
                fileOptions
			);

			foreach (Tuple<string, string, ITree> option in options) {
				string key = option.Item1;
				//string value = option.Item2;
				ITree optionTree = option.Item3;
				switch (key.ToUpperInvariant ()) {
				default:
					throw new ParserException (
                                string.Format ("Unknown file option '{0}'", option),
                                optionTree
					);  
				}
			}

			return fileOptions;
		}

		List<Tuple<string, string, ITree>> ParseFileCommon (ITree tree, FileOptions fileOptions)
		{
			AssertAntlrToken (tree, "T_FILE", 1, -1);
            
			//AntlrTreeEnumerator enumerator = new AntlrTreeEnumerator (commonTree);
			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			if (!enumerator.MoveNext ())
				throw new NotEnoughSubTokensAntlrException (tree);

			List<Tuple<string, string, ITree>> options = new List<Tuple<string, string, ITree>> ();
			string fileNameText = enumerator.Current.Text;
			if (fileNameText.StartsWith ("[")) {
				fileOptions.FileName = new ConstExpression<string> (fileNameText.Substring (1, fileNameText.Length - 2));
			} else if (fileNameText == "T_STRING" || fileNameText == "T_VARIABLE") {
				ITree fileTree = enumerator.Current;
				if (fileNameText == "T_VARIABLE")
					fileOptions.FileName = ExpressionHelper.ConvertToStringIfNeeded (ParseExpressionVariable (fileTree));
				else
					fileOptions.FileName = ParseExpressionString (fileTree);
                
				while (enumerator.MoveNext ()) {
					string option;
					string value;
					ParseFileOption (enumerator.Current, out option, out value);

					switch (option.ToUpperInvariant ()) {
					default:
						options.Add (Tuple.Create (option, value, enumerator.Current));
						break;
					}
				}
			}

			return options;
		}

		void ParseFileOption (ITree fileOptionTree, out string option, out string value)
		{
			AssertAntlrToken (fileOptionTree, "T_FILEOPTION", 1, 2);
            
			option = fileOptionTree.GetChild (0).Text;
			if (fileOptionTree.ChildCount > 1)
				value = ParseStringValue (fileOptionTree.GetChild (1));
			else
				value = null;
		}

		IProvider ParseFileProvider (ITree fileProvider)
		{
			FileOptionsFromClause fileOptions = ParseFileFromClause (fileProvider);
            
			IProvider provider = FileProviderFactory.Get (fileOptions, stringComparer);
            
			if (fileOptions.ColumnsRegex != null) {
				provider = new ColumnProviderRegex (provider, fileOptions.ColumnsRegex, caseInsensitive);
			} else if (fileOptions.ColumnDelimiter != null) {
				provider = new ColumnProviderDelimiter (provider, fileOptions.ColumnDelimiter.ToCharArray ());
			} else if (fileOptions.Heading != GqlEngineState.HeadingEnum.Off) {
				provider = new ColumnProviderDelimiter (provider);
			}

			if (fileOptions.Heading != GqlEngineState.HeadingEnum.Off) {
				provider = new ColumnProviderTitleLine (provider, fileOptions.Heading);
			}

			return provider;
		}
        
		IProvider ParseSubquery (IProvider provider, ITree subqueryTree)
		{
			AssertAntlrToken (subqueryTree, "T_SUBQUERY");
            
			ITree selectTree = GetSingleChild (subqueryTree);
			try {
				if (provider != null)
					this.subQueryProviderStack.Push (provider);
				return ParseCommandSelect (selectTree);
			} finally {
				if (provider != null) {
					IProvider verify = this.subQueryProviderStack.Pop ();
					if (verify != provider)
						throw new InvalidProgramException ();
				}
			}
		}
        
		IProvider ParseViewProvider (ITree tree)
		{
			AssertAntlrToken (tree, "T_VIEW_NAME", 1, 1);

			string viewName = tree.GetChild (0).Text;
			IProvider provider;
			if (!views.TryGetValue (viewName, out provider)) {
				if (!gqlEngineState.Views.TryGetValue (viewName, out provider)) {
					throw new ParserException (string.Format ("View '{0}' is not declared", viewName), tree);
				}
			}

			return provider;
		}

		string ParseProviderAlias (ITree tree)
		{
			AssertAntlrToken (tree, "T_TABLE_ALIAS", 1, 1);

			string alias = tree.GetChild (0).Text;
			if (!alias.StartsWith ("[") && !alias.EndsWith ("]")) {
				throw new ParserException ("Provider alias must have square brackets", tree);
			}

			return alias.Substring (1, alias.Length - 2);
		}

		void AdjustAggregation (ref IExpression arg1, ref IExpression arg2)
		{
			if (arg1.IsAggregated () || arg2.IsAggregated ()) {
				if (!arg1.IsAggregated ())
					arg1 = new InvariantColumn (arg1, stringComparer);
				if (!arg2.IsAggregated ())
					arg2 = new InvariantColumn (arg2, stringComparer);
			}
		}        

		void AdjustAggregation (ref IExpression arg1, ref IExpression arg2, ref IExpression arg3)
		{
			if (arg1.IsAggregated () || arg2.IsAggregated () || arg3.IsAggregated ()) {
				if (!arg1.IsAggregated ())
					arg1 = new InvariantColumn (arg1, stringComparer);
				if (!arg2.IsAggregated ())
					arg2 = new InvariantColumn (arg2, stringComparer);
				if (!arg3.IsAggregated ())
					arg3 = new InvariantColumn (arg3, stringComparer);
			}
		}        

		IExpression ParseExpressionOperatorUnary (IProvider provider, ITree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_UNARY", 2);
            
			IExpression arg = ParseExpression (
                provider,
                operatorTree.GetChild (1)
			);          
			IExpression result;
            
			string operatorText = operatorTree.GetChild (0).Text;
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

		IExpression ParseExpressionOperatorBinary (IProvider provider, ITree operatorTree)
		{
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3, 4);
            
			string operatorText = operatorTree.GetChild (0).Text;
			if (operatorText == "T_BETWEEN") {
				return ParseExpressionBetween (provider, operatorTree);
			} else if (operatorText == "T_NOTBETWEEN") {
				return new UnaryExpression<bool, bool> (
                    (a) => !a,
                    ParseExpressionBetween (provider, operatorTree)
				);
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseExpressionInSomeAnyAll (provider, operatorTree);
			} else if (operatorText == "T_NOTIN") {
				return new UnaryExpression<bool, bool> (
                    (a) => !a,
                    ParseExpressionInSomeAnyAll (provider, operatorTree)
				);
			} 
            
			AssertAntlrToken (operatorTree, "T_OP_BINARY", 3);

			IExpression arg1 = ParseExpression (
                provider,
                operatorTree.GetChild (1)
			);          
			IExpression arg2 = ParseExpression (
                provider,
                operatorTree.GetChild (2)
			);          
			IExpression result;

			AdjustAggregation (ref arg1, ref arg2);
            
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
        
		IExpression ParseExpressionBetween (IProvider provider, ITree betweenTree)
		{
			AssertAntlrToken (betweenTree, "T_OP_BINARY", 3);
			//AssertAntlrToken (betweenTree.Children [0], "T_BETWEEN"); or T_NOTBETWEEN
			ITree andTree = betweenTree.GetChild (2);
			AssertAntlrToken (andTree, "T_OP_BINARY", 3);
			AssertAntlrToken (andTree.GetChild (0), "T_AND");
            
			IExpression arg1 = ParseExpression (
                provider,
                betweenTree.GetChild (1)
			);
			IExpression arg2 = ParseExpression (
                provider,
                andTree.GetChild (1)
			);
			IExpression arg3 = ParseExpression (
                provider,
                andTree.GetChild (2)
			);

			AdjustAggregation (ref arg1, ref arg2, ref arg3);

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

		IExpression ParseExpressionInSomeAnyAll (IProvider provider, ITree inTree)
		{
			AssertAntlrToken (inTree, "T_OP_BINARY", 3, 4);
			//AssertAntlrToken (inTree.Children [0], "T_IN"); or T_NOTIN, T_ANY, T_ALL
            
			IExpression arg2;
			ITree target;          
			bool all;
			string op;
			switch (inTree.GetChild (0).Text) {
			case "T_IN":
			case "T_NOTIN":
				arg2 = ParseExpression (provider, inTree.GetChild (1));
				target = inTree.GetChild (2);
				all = false;
				op = "T_EQUAL";
				break;
			case "T_ANY":
				arg2 = ParseExpression (provider, inTree.GetChild (2));
				target = inTree.GetChild (3);
				all = false;
				op = inTree.GetChild (1).Text;
				break;
			case "T_ALL":
				arg2 = ParseExpression (provider, inTree.GetChild (2));
				target = inTree.GetChild (3);
				all = true;
				op = inTree.GetChild (1).Text;
				break;
			default:
				throw new ParserException (
                    string.Format ("Unexpected token {0}", inTree.GetChild (0).Text),
                    inTree.GetChild (0)
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
                        inTree.GetChild (0).Text,
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
                        inTree.GetChild (0).Text,
                        target.Text
					),
                        target
					);
			} else {
				throw new ParserException (
                    string.Format (
                    "Binary operator '{0}' cannot be used with argument {1}",
                    inTree.GetChild (0).Text,
                    arg2.GetResultType ().ToString ()
				),
                    target
				);
			}
        
			if (all)
				result = new UnaryExpression<bool, bool> (a => !a, result);
                    
			return result;
		}

		IExpression[] ParseExpressionList (IProvider provider, ITree expressionListTree)
		{
			AssertAntlrToken (expressionListTree, "T_EXPRESSIONLIST", 1, -1);
            
			IExpression[] result = new IExpression[expressionListTree.ChildCount];
			for (int i = 0; i < expressionListTree.ChildCount; i++) {
				result [i] = ParseExpression (
                    provider,
                    expressionListTree.GetChild (i)
				);
			}           
            
			return result;
		}

		IExpression ParseExpressionExists (ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_EXISTS", 1, 1);
            
			return new AnySubqueryOperator<long> (
                new ConstExpression<long> (1),
                new SelectProvider (
                new IExpression[] { new ConstExpression<long> (1) }, 
                new TopProvider (
                ParseCommandSelect (expressionTree.GetChild (0)),
                new ConstExpression<long> (1)
			)
			),
                (a, b) => a == b);
			;
		}
        
		IExpression ParseExpressionColumn (IProvider provider, ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_COLUMN", 1, 2);

			string column = ParseColumnName (expressionTree.GetChild (0));

			string providerAlias;
			if (expressionTree.ChildCount > 1)
				providerAlias = ParseProviderAlias (expressionTree.GetChild (1));
			else
				providerAlias = null;

			if (provider == null)
				throw new ParserException (string.Format ("Columnname [{0}] not allowed outside the context of a query", column), expressionTree);
            
			try {
				this.subQueryProviderStack.Push (provider);
				return ConstructColumnExpression (this.subQueryProviderStack.ToArray (), new ColumnName (providerAlias, column));
			} catch (Exception x) {
				throw new ParserException (string.Format ("Could not construct column expression for column '{0}'", column), expressionTree, x);
			} finally {
				IProvider verify = this.subQueryProviderStack.Pop ();
				if (verify != provider)
					throw new InvalidProgramException ();
			}
		}

		internal static IExpression ConstructColumnExpression (IProvider[] providers, ColumnName columnName)
		{
			foreach (IProvider provider in providers) {
				if (provider.GetColumnNames () == null) {
					return new ColumnExpression<string> (providers, columnName);
				} else {
					int columnOrdinal = provider.GetColumnOrdinal (columnName);
					if (columnOrdinal >= 0)
						return ConstructColumnExpression (provider, columnOrdinal);
				}
			}
			throw new NotSupportedException (string.Format ("Column name {0} not found", columnName));
		}        

		internal static IExpression ConstructColumnExpression (IProvider provider, ColumnName columnName)
		{
			if (provider.GetColumnNames () == null) {
				return new ColumnExpression<string> (provider, columnName);
			} else {
				int columnOrdinal = provider.GetColumnOrdinal (columnName);
				return ConstructColumnExpression (provider, columnOrdinal);
			}
		}        

		internal static IExpression ConstructColumnExpression (IProvider provider, int columnOrdinal)
		{
			Type type = provider.GetColumnTypes () [columnOrdinal];

			if (type == typeof(long)) {
				return new ColumnExpression<long> (provider, columnOrdinal);
			} else if (type == typeof(string)) {
				return new ColumnExpression<string> (provider, columnOrdinal);
			} else {
				throw new Exception (string.Format ("Invalid datatype '{0}'", type.ToString ()));
			}
		}        

		string ParseColumnName (ITree columnNameTree)
		{
			string column = columnNameTree.Text;
            
			if (column.StartsWith ("[") && column.EndsWith ("]"))
				column = column.Substring (1, column.Length - 2);
            
			return column;
		}
        
		IExpression ParseExpressionCase (IProvider provider, ITree expressionTree)
		{
			AssertAntlrToken (expressionTree, "T_CASE", 1, -1);
            
			List<CaseExpression.WhenItem> whenItems = new List<CaseExpression.WhenItem> ();
			IExpression elseResult = null;

			string text = expressionTree.GetChild (0).Text;
			if (text != "T_CASE_WHEN" && text != "T_CASE_ELSE") {
				// CASE source WHEN destination THEN target ELSE other END
				IExpression source = ParseExpression (
                    provider,
                    expressionTree.GetChild (0)
				);
				int whenNo;
				for (whenNo = 1; expressionTree.GetChild(whenNo).Text == "T_CASE_WHEN"; whenNo++) {
					ITree whenTree = expressionTree.GetChild (whenNo);
					IExpression destination = ParseExpression (
                        provider,
                        whenTree.GetChild (0)
					);
					IExpression target = ParseExpression (
                        provider,
                        whenTree.GetChild (1)
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
                
				if (whenNo < expressionTree.ChildCount - 1)
					throw new Exception ("Invalid case statement");
                
				if (whenNo == expressionTree.ChildCount - 1) {
					ITree elseTree = expressionTree.GetChild (whenNo);
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
                    
					elseResult = ParseExpression (provider, elseTree.GetChild (0));
				}
			} else {
				// CASE WHEN a THEN x ELSE y END
				int whenNo;
				for (whenNo = 0; expressionTree.GetChild(whenNo).Text == "T_CASE_WHEN"; whenNo++) {
					ITree whenTree = expressionTree.GetChild (whenNo);
					IExpression destination = ParseExpression (
                        provider,
                        whenTree.GetChild (0)
					);
					IExpression target = ParseExpression (
                        provider,
                        whenTree.GetChild (1)
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
                
				if (whenNo < expressionTree.ChildCount - 1)
					throw new Exception ("Invalid case statement");
                
				if (whenNo == expressionTree.ChildCount - 1) {
					ITree elseTree = expressionTree.GetChild (whenNo);
					AssertAntlrToken (elseTree, "T_CASE_ELSE", 1, 1);
                    
					elseResult = ParseExpression (provider, elseTree.GetChild (0));
				}
			}

			return new CaseExpression (whenItems, elseResult);
		}

		IExpression ParseExpressionVariable (ITree expressionTree)
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

			return new VariableExpression (variableName, type).GetTyped ();
		}

		FileOptions ParseCommandUse (ITree tree)
		{
			AssertAntlrToken (tree, "T_USE", 1);

			return ParseFileSimple (GetSingleChild (tree));
		}

		IList<Tuple<string, Type>> ParseCommandDeclare (ITree declareTree)
		{
			AssertAntlrToken (declareTree, "T_DECLARE", 1, -1);

			List<Tuple<string, Type>> declarations = new List<Tuple<string, Type>> ();
			foreach (ITree declarationTree in new AntlrTreeChildEnumerable(declareTree)) {
				declarations.Add (ParseDeclaration (declarationTree));
			}

			return declarations;
		}

		Tuple<string, Type> ParseDeclaration (ITree declarationTree)
		{
			AssertAntlrToken (declarationTree, "T_DECLARATION", 2, 2);

			ITree variableName = declarationTree.GetChild (0);
			AssertAntlrToken (variableName, "T_VARIABLE", 1, 1);

			string variable = variableName.GetChild (0).Text;
			Type datatype = ParseDataType (declarationTree.GetChild (1));

			return Tuple.Create (variable, datatype);
		}

		Type ParseDataType (ITree dataTypeTree)
		{
			string text = dataTypeTree.Text;
			switch (text.ToUpperInvariant ()) {
			case "STRING":
				return typeof(string);
			case "INT":
				return typeof(long);
			default:
				throw new ParserException (string.Format ("Unknown datatype '{0}'", text), dataTypeTree);
			}
		}

		Tuple<string, IExpression> ParseCommandSetVariable (ITree tree)
		{
			AssertAntlrToken (tree, "T_SET_VARIABLE", 2, 2);

			ITree variableName = tree.GetChild (0);
			AssertAntlrToken (variableName, "T_VARIABLE", 1, 1);

			string variable = variableName.GetChild (0).Text;
			IExpression expression = ParseExpression (null, tree.GetChild (1));

			return Tuple.Create (variable, expression);
		}

		IExpression ParseExpressionSubquery (IProvider parentProvider, ITree subqueryTree)
		{
			IProvider provider = ParseSubquery (parentProvider, subqueryTree);

			return new SubqueryExpression (provider).GetTyped ();
		}

		Tuple<string, IProvider> ParseCommandCreateView (ITree tree)
		{
			AssertAntlrToken (tree, "T_CREATE_VIEW", 2, 3);

			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			enumerator.MoveNext ();

			ITree viewNameTree = enumerator.Current;
			AssertAntlrToken (viewNameTree, "T_VIEW_NAME", 1, 1);
			string name = viewNameTree.GetChild (0).Text;

			enumerator.MoveNext ();
			if (enumerator.Current.Text == "T_DECLARE") {
				enumerator.MoveNext ();
			}

			IProvider provider = ParseCommandSelect (enumerator.Current);

			return Tuple.Create (name, provider);
		}

		string ParseCommandDropView (ITree tree)
		{
			AssertAntlrToken (tree, "T_DROP_VIEW", 1, 1);

			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			enumerator.MoveNext ();

			ITree viewNameTree = enumerator.Current;
			AssertAntlrToken (viewNameTree, "T_VIEW_NAME", 1, 1);
			return viewNameTree.GetChild (0).Text;
		}

		FileOptions ParseCommandDropTable (ITree tree)
		{
			AssertAntlrToken (tree, "T_DROP_TABLE", 1, 1);

			return ParseFileSimple (tree.GetChild (0));
		}

	}
}

