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
		readonly DataComparer dataComparer;

		Dictionary<string, Type> variableTypes = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase);
		Dictionary<string, ViewDefinition> views = new Dictionary<string, ViewDefinition> (StringComparer.InvariantCultureIgnoreCase);
		Stack<IProvider> subQueryProviderStack = new Stack<IProvider> ();
        
		public GqlParser (GqlEngineState gqlEngineState, string command)
            : this(gqlEngineState, command, CultureInfo.InvariantCulture, true)
		{
		}
        
		public GqlParser (GqlEngineState gqlEngineState, string command, CultureInfo cultureInfo, bool caseInsensitive)
		{
			this.gqlEngineState = gqlEngineState;
			this.command = command;
			this.dataComparer = new DataComparer (cultureInfo, caseInsensitive);
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
				FileOptions fileOptions = ParseCommandUse (tree);
				fileOptions.ValidateProviderOptions ();
				return new UseCommand (fileOptions);
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
					ViewDefinition viewDefinition = createView.Item2;
					views.Add (view, viewDefinition);
					return new CreateViewCommand (view, viewDefinition);
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
			Expression<DataInteger> topExpression;
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
					Expression<DataBoolean> whereExpression = ParseWhereClause (
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
                    
					Expression<DataBoolean> havingExpression;
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
                        dataComparer
					);
				} else {
					// e.g. select count(1) from [myfile.txt]
					if (outputColumns.Any (p => p is SingleColumn && ((SingleColumn)p).Expression.IsAggregated ())) {
						provider = new GroupbyProvider (
	                        provider,
	                        outputColumns,
	                        dataComparer
						);
					} else {
						provider = new ColumnProvider (outputColumns, provider);
					}
				}
                
				if (distinct)
					provider = new DistinctProvider (provider, dataComparer);
                
				if (enumerator.Current != null && enumerator.Current.Text == "T_ORDERBY") {
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
                        fromProvider,
                        enumerator.Current
					);
					enumerator.MoveNext ();
                    
					provider = new OrderbyProvider (provider, orderbyColumns, dataComparer);
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
        
		Expression<DataInteger> ParseTopClause (ITree topClauseTree)
		{
			ITree tree = GetSingleChild (topClauseTree);
			return ConvertExpression.CreateDataInteger (ParseExpression (null, tree));
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
			intoFile.ValidateProviderOptions ();

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
				case "T_FILESUBQUERY":
					provider = ParseFileSubqueryProvider (inputProviderTree);
					break;
				case "T_SUBQUERY":
					provider = ParseSubquery (null, inputProviderTree);
					break;
				case "T_VIEW":
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

		Expression<DataBoolean> ParseWhereClause (IProvider provider, ITree whereTree)
		{
			AssertAntlrToken (whereTree, "T_WHERE");
            
			ITree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (provider, expressionTree);
			if (!(expression is Expression<DataBoolean>)) {
				throw new ParserException (
                    "Expected boolean expression in WHERE clause.",
                    expressionTree
				);
			}
			return (Expression<DataBoolean>)expression;
		}
        
		Expression<DataBoolean> ParseHavingClause (IProvider provider, ITree whereTree)
		{
			AssertAntlrToken (whereTree, "T_HAVING");
            
			ITree expressionTree = GetSingleChild (whereTree);
			IExpression expression = ParseExpression (provider, expressionTree);
			if (!(expression is Expression<DataBoolean>)) {
				throw new ParserException (
                    "Expected boolean expression in HAVING clause.",
                    expressionTree
				);
			}
			return (Expression<DataBoolean>)expression;
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
			case "T_DATEPART":
				expression = ParseExpressionDatePart (tree);
				break;
			default:
				throw new UnexpectedTokenAntlrException (tree);
			}
            
			return expression;
		}

		IExpression ParseExpressionInteger (ITree expressionNumberTree)
		{
			string text;
			if (expressionNumberTree.ChildCount == 1)
				text = expressionNumberTree.GetChild (0).Text;
			else
				text = expressionNumberTree.GetChild (0).Text + expressionNumberTree.GetChild (1).Text;
			return new ConstExpression<DataInteger> (long.Parse (text));
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

		class Token<T> : IExpression
		{

			public T Value { get; set; }

			public Token (T value)
			{
				Value = value;
			}

			#region IExpression implementation
			public IData EvaluateAsData (GqlQueryState gqlQueryState)
			{
				throw new InvalidOperationException ();
			}

			public Type GetResultType ()
			{
				throw new InvalidOperationException ();
			}

			public bool IsAggregated ()
			{
				return false;
			}

			public bool IsConstant ()
			{
				return true;
			}

			public void Aggregate (StateBin state, GqlQueryState gqlQueryState)
			{
				throw new InvalidOperationException ();
			}

			public IData AggregateCalculate (StateBin state)
			{
				throw new InvalidOperationException ();
			}
			#endregion
		}

		IExpression ParseExpressionDatePart (ITree datePartTree)
		{
			ITree tree = GetSingleChild (datePartTree);

			DatePartType datePart;
			try {
				datePart = DatePartHelper.Parse (tree.Text);
			} catch (Exception x) {
				throw new ParserException ("Invalid DatePart type", tree, x);
			}

			return new Token<DatePartType> (datePart);
		}

		Expression<DataString> ParseExpressionString (ITree expressionStringTree)
		{
			ITree tree = GetSingleChild (expressionStringTree);
            
			string text = ParseString (tree);
			return new ConstExpression<DataString> (text);
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
			case 4:
				result = ParseExpressionFunctionCall_4 (
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
			case "GETDATE":
				result = new NullaryExpression<DataDateTime> (() => DateTime.Now);
				break;
			case "GETUTCDATE":
				result = new NullaryExpression<DataDateTime> (() => DateTime.UtcNow);
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
			if ((functionNameUpper == "T_COUNT" || functionNameUpper == "T_DISTINCTCOUNT") && functionCallTree.GetChild (1).Text == "T_ALLCOLUMNS") {
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
				arg = new ConstExpression<DataInteger> (1);
			} else {
				arg = ParseExpression (provider, functionCallTree.GetChild (1));
			}

			IExpression result;
            
			switch (functionNameUpper) {
			case "ESCAPEREGEX":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => Regex.Escape (a), arg);
				break;
			case "LTRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.TrimStart (), arg);
				break;
			case "RTRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.TrimEnd (), arg);
				break;
			case "TRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.Trim (), arg);
				break;
			//case "COUNT":
			case "T_COUNT":
				result = new AggregationExpression<IData, DataInteger, DataInteger> ((a) => 1, 
                    (s, a) => s + 1, 
                    (s) => s, 
                    ConvertExpression.CreateData (arg));
				break;
			case "T_DISTINCTCOUNT":
				result = new AggregationExpression<IData, SortedSet<ColumnsComparerKey>, DataInteger> 
					((a) => new SortedSet<ColumnsComparerKey> (), 
					 delegate(SortedSet<ColumnsComparerKey> s, IData a) {
					ColumnsComparerKey columnsComparerKey = new ColumnsComparerKey (new IData[] { a });
					if (!s.Contains (columnsComparerKey))
						s.Add (columnsComparerKey);
					return s;
				},
	                    (s) => s.Count, 
	                ConvertExpression.CreateData (arg));
				break;
			case "SUM":
				if (arg is Expression<DataInteger>)
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => s + a, 
                        (s) => s, 
                        arg as Expression<DataInteger>);
				else {
					throw new ParserException (
                        string.Format ("SUM aggregation function cannot be used on datatype '{0}'",
                               arg.GetResultType ().ToString ()),
                        functionCallTree);
				}
				break;
			case "MIN":
				if (arg.GetResultType () == typeof(DataString))
					result = new AggregationExpression<DataString, DataString, DataString> (
                        (a) => a, 
                        (s, a) => string.Compare (a, s) < 0 ? a : s, 
                        (s) => s, 
                        ConvertExpression.CreateDataString (arg));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => a < s ? a : s, 
                        (s) => s, 
                        ConvertExpression.CreateDataInteger (arg));
				else {
					throw new ParserException (
                        string.Format ("MIN aggregation function cannot be used on datatype '{0}'",
                               arg.GetResultType ().ToString ()),
                        functionCallTree);
				}
				break;
			case "MAX":
				if (arg.GetResultType () == typeof(DataString))
					result = new AggregationExpression<DataString, DataString, DataString> (
                        (a) => a, 
                        (s, a) => string.Compare (a, s) > 0 ? a : s, 
                        (s) => s, 
                        ConvertExpression.CreateDataString (arg));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => a > s ? a : s, 
                        (s) => s, 
                        ConvertExpression.CreateDataInteger (arg));
				else {
					throw new ParserException (
                        string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
                               arg.GetResultType ().ToString ()),
                        functionCallTree);
				}
				break;
			case "FIRST":
				if (arg.GetResultType () == typeof(DataString))
					result = new AggregationExpression<DataString, DataString, DataString> (
                        (a) => a, 
                        (s, a) => s, 
                        (s) => s, 
                        ConvertExpression.CreateDataString (arg));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => s, 
						(s) => s, 
                        ConvertExpression.CreateDataInteger (arg));
				else {
					throw new ParserException (
                        string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
                               arg.GetResultType ().ToString ()),
                        functionCallTree);
				}
				break;
			case "LAST":
				if (arg.GetResultType () == typeof(DataString))
					result = new AggregationExpression<DataString, DataString, DataString> (
                        (a) => a, 
                        (s, a) => a, 
                        (s) => s, 
                        ConvertExpression.CreateDataString (arg));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => a, 
                        (s) => s, 
                        ConvertExpression.CreateDataInteger (arg));
				else {
					throw new ParserException (
                        string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
                               arg.GetResultType ().ToString ()),
                        functionCallTree);
				}
				break;
			case "AVG":
				if (arg is Expression<DataInteger>) {
					Expression<DataInteger> resultSum = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => a, 
                        (s, a) => s + a, 
                        (s) => s, 
                        arg as Expression<DataInteger>);
					Expression<DataInteger> resultCount = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
                        (a) => 1, 
                        (s, a) => s + 1, 
                        (s) => s, 
                        arg as Expression<DataInteger>);
					result = new BinaryExpression<DataInteger, DataInteger, DataInteger> (
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
				result = BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
                    (a, b) => a.Value.IndexOf (b, dataComparer.StringComparison) != -1,
                    arg1,
                    arg2
				);
				break;
			case "LEFT":
				result = BinaryExpression<DataString, DataInteger, DataString>.CreateAutoConvert (
                    (a, b) => a.Value.Substring (0, Math.Min ((int)b, a.Value.Length)),
                    arg1,
                    arg2
				);
				break;
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive);
				break;
			case "RIGHT":
				result = BinaryExpression<DataString, DataInteger, DataString>.CreateAutoConvert (
                    (a, b) => a.Value.Substring (a.Value.Length - Math.Min ((int)b, a.Value.Length)),
                    arg1,
                    arg2
				);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2);
				break;
			case "DATEPART":
				result = UnaryExpression<DataDateTime, DataInteger>.CreateAutoConvert (
                    (a) => DatePartHelper.Get ((arg1 as Token<DatePartType>).Value, a), arg2);
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
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive, arg3);
				break;
			case "REPLACE":
				result = new ReplaceFunction (arg1, arg2, arg3, dataComparer.CaseInsensitive);
				break;
			case "REPLACEREGEX":
				result = new ReplaceRegexFunction (arg1, arg2, arg3, dataComparer.CaseInsensitive);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2, arg3);
				break;
			case "DATEADD":
				result = BinaryExpression<DataInteger, DataDateTime, DataDateTime>.CreateAutoConvert (
                    (a, b) => DatePartHelper.Add ((arg1 as Token<DatePartType>).Value, a.Value, b), arg2, arg3);
				break;
			case "DATEDIFF":
				result = BinaryExpression<DataDateTime, DataDateTime, DataInteger>.CreateAutoConvert (
                    (a, b) => DatePartHelper.Diff ((arg1 as Token<DatePartType>).Value, a.Value, b.Value), arg2, arg3);
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

		IExpression ParseExpressionFunctionCall_4 (IProvider provider, ITree functionCallTree, string functionName)
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
			IExpression arg4 = ParseExpression (
                provider,
                functionCallTree.GetChild (4)
			);
            
			AdjustAggregation (ref arg1, ref arg2, ref arg3, ref arg4);

			IExpression result;
            
			switch (functionName.ToUpperInvariant ()) {
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive, arg3, arg4);
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
			AssertAntlrToken (convertTree, "T_CONVERT", 2, 3);
            
			Type dataType = ParseDataType (convertTree.GetChild (0));
			IExpression expr = ParseExpression (
                provider,
                convertTree.GetChild (1)
			);

			string format;
			if (convertTree.ChildCount >= 3)
				format = ParseString (convertTree.GetChild (2));
			else
				format = null;
            
			return ConvertExpression.Create (dataType, expr, format);
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
				case "PROVIDER":
					FileOptions.ProviderEnum provider;
					if (!Enum.TryParse<FileOptions.ProviderEnum> (value, true, out provider))
						throw new ParserException (
                                    string.Format ("Unknown file option Provider={0}", value),
                                    tree
						);
					fileOptions.Provider = provider;
					break;
				case "CLIENT":
					fileOptions.Client = value;
					break;
				case "CONNECTIONSTRING":
					fileOptions.ConnectionString = value;
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
			FileOptions fileOptions = new FileOptions ();

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
				fileOptions.FileName = new ConstExpression<DataString> (fileNameText.Substring (1, fileNameText.Length - 2));
			} else if (fileNameText == "T_STRING" || fileNameText == "T_VARIABLE") {
				ITree fileTree = enumerator.Current;
				if (fileNameText == "T_VARIABLE")
					fileOptions.FileName = ConvertExpression.CreateDataString (ParseExpressionVariable (fileTree));
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
			fileOptions.ValidateProviderOptions ();
            
			IProvider provider;
			if (fileOptions.Provider == FileOptions.ProviderEnum.DontCare
				|| fileOptions.Provider == FileOptions.ProviderEnum.File) {
				provider = FileProviderFactory.Get (fileOptions, dataComparer.StringComparer);
            
				if (fileOptions.ColumnsRegex != null) {
					provider = new ColumnProviderRegex (provider, fileOptions.ColumnsRegex, dataComparer.CaseInsensitive);
				} else if (fileOptions.ColumnDelimiter != null) {
					provider = new ColumnProviderDelimiter (provider, fileOptions.ColumnDelimiter.ToCharArray ());
				} else if (fileOptions.Heading != GqlEngineState.HeadingEnum.Off) {
					provider = new ColumnProviderDelimiter (provider);
				}

				if (fileOptions.Heading != GqlEngineState.HeadingEnum.Off) {
					provider = new ColumnProviderTitleLine (provider, fileOptions.Heading);
				}
			} else if (fileOptions.Provider == FileOptions.ProviderEnum.Directory) {
				provider = new DirectoryProvider (fileOptions, dataComparer.StringComparer);
			} else if (fileOptions.Provider == FileOptions.ProviderEnum.Data) {
				provider = new DataProvider (fileOptions);
			} else {
				throw new ParserException (string.Format ("Invalid provider '{0}'", fileOptions.Provider), fileProvider);
			}

			return provider;
		}

		IProvider ParseFileSubqueryProvider (ITree fileSubqueryTree)
		{
			AssertAntlrToken (fileSubqueryTree, "T_FILESUBQUERY", 1);
            
			IProvider fileSubqueryProvider = ParseSubquery (null, fileSubqueryTree.GetChild (0));
			return new FileSubqueryProvider (fileSubqueryProvider);
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
			AssertAntlrToken (tree, "T_VIEW", 1, 2);

			string viewName = ParseViewName (tree.GetChild (0));

			ViewDefinition viewDefinition;
			if (!views.TryGetValue (viewName, out viewDefinition)) {
				if (!gqlEngineState.Views.TryGetValue (viewName, out viewDefinition)) {
					throw new ParserException (string.Format ("View '{0}' is not declared", viewName), tree);
				}
			}

			IExpression[] parameters;
			if (tree.ChildCount >= 2)
				parameters = ParseExpressionList (null, tree.GetChild (1));
			else
				parameters = null;

			int callerParameterCount = (parameters != null) ? parameters.Length : 0;
			int definitionParameterCount = (viewDefinition.Parameters != null) ? viewDefinition.Parameters.Count : 0;

			if (callerParameterCount != definitionParameterCount)
				throw new ParserException (string.Format ("Parameterized view has incorrect number of parameters.  The caller uses {0} parameters, but the definition has {1} parameters.", callerParameterCount, definitionParameterCount), tree);

			IProvider provider;
			if (definitionParameterCount == 0)
				provider = viewDefinition.Provider;
			else
				provider = new ParameterizedProvider (viewDefinition, parameters);

			return provider;
		}

		string ParseViewName (ITree tree)
		{
			AssertAntlrToken (tree, "T_VIEW_NAME", 1, 1);

			return tree.GetChild (0).Text;
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
					arg1 = new InvariantColumn (arg1, dataComparer);
				if (!arg2.IsAggregated ())
					arg2 = new InvariantColumn (arg2, dataComparer);
			}
		}        

		void AdjustAggregation (ref IExpression arg1, ref IExpression arg2, ref IExpression arg3)
		{
			if (arg1.IsAggregated () || arg2.IsAggregated () || arg3.IsAggregated ()) {
				if (!arg1.IsAggregated ())
					arg1 = new InvariantColumn (arg1, dataComparer);
				if (!arg2.IsAggregated ())
					arg2 = new InvariantColumn (arg2, dataComparer);
				if (!arg3.IsAggregated ())
					arg3 = new InvariantColumn (arg3, dataComparer);
			}
		}        

		void AdjustAggregation (ref IExpression arg1, ref IExpression arg2, ref IExpression arg3, ref IExpression arg4)
		{
			if (arg1.IsAggregated () || arg2.IsAggregated () || arg3.IsAggregated ()) {
				if (!arg1.IsAggregated ())
					arg1 = new InvariantColumn (arg1, dataComparer);
				if (!arg2.IsAggregated ())
					arg2 = new InvariantColumn (arg2, dataComparer);
				if (!arg3.IsAggregated ())
					arg3 = new InvariantColumn (arg3, dataComparer);
				if (!arg4.IsAggregated ())
					arg4 = new InvariantColumn (arg4, dataComparer);
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
				result = UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert ((a) => !a, arg);
				break;
			case "T_PLUS":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => a, arg);
				else {
					throw new ParserException (
                            string.Format ("Unary operator 'PLUS' cannot be used with datatype {0}",
                                   arg.GetResultType ().ToString ()),
                            operatorTree);
				}
				break;
			case "T_MINUS":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => -a, arg);
				else {
					throw new ParserException (
                            string.Format ("Unary operator 'MINUS' cannot be used with datatype {0}",
                                   arg.GetResultType ().ToString ()),
                            operatorTree);
				}
				break;
			case "T_BITWISE_NOT":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => ~a, arg);
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
				return UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert (
                    (a) => !a,
                    ParseExpressionBetween (provider, operatorTree)
				);
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseExpressionInSomeAnyAll (provider, operatorTree);
			} else if (operatorText == "T_NOTIN") {
				return UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert (
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
				result = BinaryExpression<DataBoolean, DataBoolean, DataBoolean>.CreateAutoConvert (
                    (a, b) => a && b,
                    arg1,
                    arg2
				);
				break;
			case "T_OR":
				result = BinaryExpression<DataBoolean, DataBoolean, DataBoolean>.CreateAutoConvert (
                    (a, b) => a || b,
                    arg1,
                    arg2
				);
				break;
			case "T_MATCH":
				result = new MatchOperator (arg1, arg2, dataComparer.CaseInsensitive);
				break;
			case "T_NOTMATCH":
				result = new UnaryExpression<DataBoolean, DataBoolean> (
                    (a) => !a,
                    new MatchOperator (arg1, arg2, dataComparer.CaseInsensitive)
				);
				break;
			case "T_LIKE":
				result = new LikeOperator (arg1, arg2, dataComparer.CaseInsensitive);
				break;
			case "T_NOTLIKE":
				result = new UnaryExpression<DataBoolean, DataBoolean> (
                    (a) => !a,
                    new LikeOperator (arg1, arg2, dataComparer.CaseInsensitive)
				);
				break;
			case "T_PLUS":
				{
					if (arg1 is Expression<DataString> || arg2 is Expression<DataString>)
						result = BinaryExpression<DataString, DataString, DataString>.CreateAutoConvert (
                            (a, b) => a + b,
                            arg1,
                            arg2
						);
					else if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
					if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
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
				if (arg1 is Expression<DataString> || arg2 is Expression<DataString>)
					result = 
                        BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (OperatorHelper.GetStringComparer (
                        operatorText,
                        false,
                        dataComparer.StringComparison
					),
                            arg1, arg2);
				else if (arg1 is Expression<DataBoolean> || arg2 is Expression<DataBoolean>)
					result = 
                        BinaryExpression<DataBoolean, DataBoolean, DataBoolean>.CreateAutoConvert (OperatorHelper.GetBooleanComparer (
                        operatorText,
                        false
					),
                            arg1, arg2);
				else if (arg1 is Expression<DataInteger>)
					result = 
                        BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (OperatorHelper.GetIntegerComparer (
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
			case "T_LESS":
			case "T_GREATER":
			case "T_NOTLESS":
			case "T_NOTGREATER":
				if (arg1 is Expression<DataString> || arg2 is Expression<DataString>)
					result = 
                        BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (OperatorHelper.GetStringComparer (
                        operatorText,
                        false,
                        dataComparer.StringComparison
					),
                            arg1, arg2);
				else if (arg1 is Expression<DataInteger>)
					result = 
                        BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (OperatorHelper.GetIntegerComparer (
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
			if (arg1 is Expression<DataString> || arg2 is Expression<DataString> || arg3 is Expression<DataString>)
				result = TernaryExpression<DataString, DataString, DataString, DataBoolean>.CreateAutoConvert (
                    (a, b, c) => 
                                                                      string.Compare (
                    a,
                    b,
                    dataComparer.StringComparison
				) >= 0 
					&& string.Compare (
                    a,
                    c,
                    dataComparer.StringComparison
				) <= 0,
                    arg1,
                    arg2,
                    arg3
				);
			else if (arg1 is Expression<DataInteger>)
				result = TernaryExpression<DataInteger, DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (
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
                        
			Expression<DataBoolean> result;
			if (target.Text == "T_EXPRESSIONLIST") {
				IExpression[] expressionList = ParseExpressionList (provider, target);
				if (arg2 is Expression<DataString>)
					result = new AnyListOperator<DataString> (
                        (Expression<DataString>)arg2,
                        expressionList,
                        OperatorHelper.GetStringComparer (op, all, dataComparer.StringComparison)
					);
				else if (arg2 is Expression<DataInteger>)
					result = new AnyListOperator<DataInteger> (
                        (Expression<DataInteger>)arg2,
                        expressionList,
                        OperatorHelper.GetIntegerComparer (op, all)
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
				if (arg2 is Expression<DataString>)
					result = new AnySubqueryOperator<DataString> (
                        (Expression<DataString>)arg2,
                        subProvider,
                        OperatorHelper.GetStringComparer (op, all, dataComparer.StringComparison)
					);
				else if (arg2 is Expression<DataInteger>)
					result = new AnySubqueryOperator<DataInteger> (
                        (Expression<DataInteger>)arg2,
                        subProvider,
                        OperatorHelper.GetIntegerComparer (op, all)
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
				result = new UnaryExpression<DataBoolean, DataBoolean> (a => !a, result);
                    
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
            
			return new AnySubqueryOperator<DataInteger> (
                new ConstExpression<DataInteger> (1),
                new SelectProvider (
                new IExpression[] { new ConstExpression<DataInteger> (1) }, 
                new TopProvider (
                ParseCommandSelect (expressionTree.GetChild (0)),
                new ConstExpression<DataInteger> (1)
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
					return new ColumnExpression<DataString> (providers, columnName);
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
				return new ColumnExpression<DataString> (provider, columnName);
			} else {
				int columnOrdinal = provider.GetColumnOrdinal (columnName);
				return ConstructColumnExpression (provider, columnOrdinal);
			}
		}        

		internal static IExpression ConstructColumnExpression (IProvider provider, int columnOrdinal)
		{
			Type type = provider.GetColumnTypes () [columnOrdinal];

			if (type == typeof(DataString)) {
				return new ColumnExpression<DataString> (provider, columnOrdinal);
			} else if (type == typeof(DataBoolean)) {
				return new ColumnExpression<DataBoolean> (provider, columnOrdinal);
			} else if (type == typeof(DataInteger)) {
				return new ColumnExpression<DataInteger> (provider, columnOrdinal);
			} else if (type == typeof(DataDateTime)) {
				return new ColumnExpression<DataDateTime> (provider, columnOrdinal);
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
					if (source is Expression<DataString> || destination is Expression<DataString>)
						whenItem.Check = 
                            BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (OperatorHelper.GetStringComparer (
                            "T_EQUAL",
                            false,
                            dataComparer.StringComparison
						),
                                source, destination);
					else if (source is Expression<DataInteger>)
						whenItem.Check = 
                            BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (OperatorHelper.GetIntegerComparer (
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
					if (destination is Expression<DataBoolean>)
						whenItem.Check = (Expression<DataBoolean>)destination;
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
			case "BOOL":
				return typeof(DataBoolean);
			case "STRING":
				return typeof(DataString);
			case "INT":
				return typeof(DataInteger);
			case "DATETIME":
				return typeof(DataDateTime);
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

		Tuple<string, ViewDefinition> ParseCommandCreateView (ITree tree)
		{
			AssertAntlrToken (tree, "T_CREATE_VIEW", 2, 3);

			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			enumerator.MoveNext ();

			string name = ParseViewName (enumerator.Current);
			enumerator.MoveNext ();

			IList<Tuple<string, Type>> parameters;
			if (enumerator.Current.Text == "T_DECLARE") {
				parameters = ParseCommandDeclare (enumerator.Current);
				enumerator.MoveNext ();
			} else {
				parameters = null;
			}

			Dictionary<string, Type> oldVariables = variableTypes;
			if (parameters != null && parameters.Count > 0) {
				variableTypes = new Dictionary<string, Type> (variableTypes);
				foreach (Tuple<string, Type> parameter in parameters)
					variableTypes [parameter.Item1] = parameter.Item2;
			}

			IProvider provider;
			try {
				provider = ParseCommandSelect (enumerator.Current);
			} finally {
				variableTypes = oldVariables;
			}


			ViewDefinition viewDefinition = new ViewDefinition (provider, parameters);

			return Tuple.Create (name, viewDefinition);
		}

		string ParseCommandDropView (ITree tree)
		{
			AssertAntlrToken (tree, "T_DROP_VIEW", 1, 1);

			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			enumerator.MoveNext ();

			return ParseViewName (enumerator.Current);
			;
		}

		FileOptions ParseCommandDropTable (ITree tree)
		{
			AssertAntlrToken (tree, "T_DROP_TABLE", 1, 1);

			return ParseFileSimple (tree.GetChild (0));
		}

	}
}

