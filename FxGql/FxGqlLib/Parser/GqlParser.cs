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
    
	partial class GqlParser
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
			AssertAntlrToken (tree, "T_SELECT", 1, 2);

			var enumerator = new AntlrTreeChildEnumerable (tree).GetEnumerator ();
			if (!enumerator.MoveNext ())
				throw new NotEnoughSubTokensAntlrException (tree);

			ITree selectSimpleOrUnionTree = enumerator.Current;
			ITree orderByClauseTree;
			if (enumerator.MoveNext ())
				orderByClauseTree = enumerator.Current;
			else
				orderByClauseTree = null;

			IProvider provider;
			if (selectSimpleOrUnionTree.Text == "T_SELECT_SIMPLE"
				|| selectSimpleOrUnionTree.Text == "T_SUBQUERY") {
				provider = ParseCommandSelectSimple (selectSimpleOrUnionTree, orderByClauseTree);
			} else {
				FileOptionsIntoClause intoClause = null;

				List<IProvider> unionProviders = new List<IProvider> ();
				bool first = true;
				do {
					AssertAntlrToken (selectSimpleOrUnionTree, "T_SELECT_UNION", 2);
					ITree firstChildTree = selectSimpleOrUnionTree.GetChild (0);
					IProvider itemProvider = ParseCommandSelectSimple (firstChildTree, null);
					if (first) {
						first = false;
						IntoProvider intoProvider = itemProvider as IntoProvider;
						if (intoProvider != null) {
							itemProvider = intoProvider.InnerProvider;
							intoClause = intoProvider.FileOptions;
						}
					} else {
						if (itemProvider is IntoProvider)
							throw new ParserException ("INTO clause is only supported on first UNION select query", firstChildTree);
					}
					unionProviders.Add (itemProvider);
					selectSimpleOrUnionTree = selectSimpleOrUnionTree.GetChild (1);
				} while (selectSimpleOrUnionTree.Text == "T_SELECT_UNION");

				IProvider lastProvider = ParseCommandSelectSimple (selectSimpleOrUnionTree, null);
				if (lastProvider is IntoProvider)
					throw new ParserException ("INTO clause is only supported on first UNION select query", selectSimpleOrUnionTree);
				unionProviders.Add (lastProvider);

				provider = new MergeProvider (unionProviders);

				if (orderByClauseTree != null) {
					AssertAntlrToken (orderByClauseTree, "T_ORDERBY");
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
                        provider,
                        orderByClauseTree
					);
                    
					provider = new OrderbyProvider (provider, orderbyColumns, dataComparer);
				}

				if (intoClause != null) {
					provider = new IntoProvider (provider, intoClause);
				}
			}

			return provider;
		}

		IProvider ParseCommandSelectSimple (ITree selectSimpleTree, ITree orderByClauseTree)
		{
			if (selectSimpleTree.Text == "T_SUBQUERY") {
				IProvider providerSubQuery = ParseSubquery (null, selectSimpleTree);

				if (orderByClauseTree != null) {
					AssertAntlrToken (orderByClauseTree, "T_ORDERBY");
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
                        providerSubQuery,
                        orderByClauseTree
					);
                    
					providerSubQuery = new OrderbyProvider (providerSubQuery, orderbyColumns, dataComparer);
				}

				return providerSubQuery;
			}

			AssertAntlrToken (selectSimpleTree, "T_SELECT_SIMPLE");
            
			var enumerator = new AntlrTreeChildEnumerable (selectSimpleTree).GetEnumerator ();
			if (!enumerator.MoveNext ())
				throw new NotEnoughSubTokensAntlrException (selectSimpleTree);
            
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
				throw new NotEnoughSubTokensAntlrException (selectSimpleTree);
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
				
				IProvider groupbyProvider = null;
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
					groupbyProvider = provider;
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
                
				if (orderByClauseTree != null) {
					AssertAntlrToken (orderByClauseTree, "T_ORDERBY");
					IList<OrderbyProvider.Column> orderbyColumns = ParseOrderbyClause (
                        groupbyProvider ?? fromProvider,
                        orderByClauseTree
					);
                    
					provider = new OrderbyProvider (provider, orderbyColumns, dataComparer);
				}

				if (topExpression != null)
					provider = new TopProvider (provider, topExpression);
			} else {
				provider = new NullProvider ();
            
				if (distinct)
					throw new ParserException (
                        "DISTINCT clause not allowed without a FROM clause.",
                        selectSimpleTree
					);
                
				if (topExpression != null) 
					throw new ParserException (
                        "TOP clause not allowed without a FROM clause.",
                        selectSimpleTree
					);

				if (enumerator.Current != null && enumerator.Current.Text == "T_WHERE")
					throw new ParserException (
                        "WHERE clause not allowed without a FROM clause.",
                        selectSimpleTree
					);
                
				if (enumerator.Current != null && enumerator.Current.Text == "T_GROUPBY")
					throw new ParserException (
                        "GROUP BY clause not allowed without a FROM clause.",
                        selectSimpleTree
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
				case "FORMAT":
					FileOptionsFromClause.FormatEnum format;
					if (!Enum.TryParse<FileOptionsFromClause.FormatEnum> (
                        value,
                        true,
                        out format
					))
						throw new ParserException (
                                    string.Format ("Unknown file option Format={0}", format),
                                    tree
						);
					fileOptions.Format = format;
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
				case "FORMAT":
					FileOptionsIntoClause.FormatEnum format;
					if (!Enum.TryParse<FileOptionsIntoClause.FormatEnum> (
                        value,
                        true,
                        out format
					))
						throw new ParserException (
                                    string.Format ("Unknown file option Format={0}", format),
                                    tree
						);
					fileOptions.Format = format;
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
            
				if (fileOptions.Format == FileOptionsFromClause.FormatEnum.Csv) {
					char[] separators = fileOptions.ColumnDelimiter != null ? fileOptions.ColumnDelimiter.ToCharArray () : new char[] { ',' };
					provider = new ColumnProviderCsv (provider, separators);
				} else if (fileOptions.ColumnsRegex != null) {
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
				IProvider subQueryProvider = ParseCommandSelect (selectTree);
				if (subQueryProvider is IntoProvider)
					throw new ParserException ("INTO clause is not supported in a subquery", subqueryTree);
				return subQueryProvider;
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

