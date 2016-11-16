using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Globalization;

namespace FxGqlLib
{
	partial class GqlParser
	{
		IExpression ParseExpression (IProvider provider, ITree tree)
		{
			IExpression expression;
			switch (tree.Text.ToUpperInvariant ()) {
			case "T_NUMBER":
				expression = ParseExpressionNumber (tree);
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

		IExpression ParseExpressionNumber (ITree expressionNumberTree)
		{
			string text;
			if (expressionNumberTree.ChildCount == 1)
				text = expressionNumberTree.GetChild (0).Text;
			else
				text = expressionNumberTree.GetChild (0).Text + expressionNumberTree.GetChild (1).Text;

			long multiplier = 1;
			if (text.Length > 0) {
				char suffix = text [text.Length - 1];
				if (!char.IsDigit (suffix)) {
					switch (suffix) {
					case 'k':
						multiplier = 1000L;
						break;
					case 'M':
						multiplier = 1000L * 1000;
						break;
					case 'G':
						multiplier = 1000L * 1000 * 1000;
						break;
					case 'T':
						multiplier = 1000L * 1000 * 1000 * 1000;
						break;
					case 'P':
						multiplier = 1000L * 1000 * 1000 * 1000 * 1000;
						break;
					case 'E':
						multiplier = 1000L * 1000 * 1000 * 1000 * 1000 * 1000;
						break;
					default:
						throw new Exception (string.Format ("Unknown number suffix '{0}'", suffix));
					}
					text = text.Substring (0, text.Length - 1);
				}
			}

			/* Number Literals are 'invariant' (with '.' as decimal point indicator) */
			/* Numbers converted from strings (string literals or string input from file) follow the current culture, 
			as specified by SET CULTURE */
			if (text.Contains ('.'))
				return new ConstExpression<DataFloat> (double.Parse (text, CultureInfo.InvariantCulture.NumberFormat) * multiplier);
			else
				return new ConstExpression<DataInteger> (long.Parse (text, CultureInfo.InvariantCulture.NumberFormat) * multiplier);
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

            public bool HasState()
            {
                return false;
            }

            public bool IsConstant ()
			{
				return true;
			}

			public void Process (StateBin state, GqlQueryState gqlQueryState)
			{
				throw new InvalidOperationException ();
			}

			public IData ProcessCalculate (StateBin state)
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
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => Regex.Escape (a), arg, cultureInfo);
				break;
			case "LTRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.TrimStart (), arg, cultureInfo);
				break;
			case "RTRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.TrimEnd (), arg, cultureInfo);
				break;
			case "TRIM":
				result = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => a.Value.Trim (), arg, cultureInfo);
				break;
			case "LEN":
				result = UnaryExpression<DataString, DataInteger>.CreateAutoConvert ((a) => a.Value.Length, arg, cultureInfo);
				break;
			case "ABS":
				if (arg.GetResultType () == typeof(DataFloat))
					result = UnaryExpression<DataFloat, DataFloat>.CreateAutoConvert ((a) => Math.Abs (a.Value), arg, cultureInfo);
				else
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => Math.Abs (a.Value), arg, cultureInfo);
				break;
            case "LAG":
                result = new StateExpression<IData, Tuple<IData, IData>, IData>(
                    (a) => new Tuple<IData, IData>(DataTypeUtil.GetDefaultFromDataType(arg.GetResultType()), a),
                    delegate (Tuple<IData, IData> s, IData a) { s = new Tuple<IData, IData>(s.Item2, a); return s; },
                    (s) => s.Item1,
                    ConvertExpression.CreateData(arg)
                    );
                break;
			//case "COUNT":
			case "T_COUNT":
				result = new AggregationExpression<IData, DataInteger, DataInteger> ((a) => 1, 
					(s, a) => s + 1, 
					(s) => s, 
					ConvertExpression.CreateData (arg));
				break;
			case "T_DISTINCTCOUNT":
				result = new AggregationExpression<IData, SortedSet<ColumnsComparerKey>, DataInteger> (
					(a) => new SortedSet<ColumnsComparerKey> (),
					delegate(SortedSet<ColumnsComparerKey> s, IData a) {
						ColumnsComparerKey columnsComparerKey = new ColumnsComparerKey (new IData[] { a });
						if (!s.Contains (columnsComparerKey))
							s.Add (columnsComparerKey);
						return s;
					},
					(s) => s.Count, 
					ConvertExpression.CreateData (arg),
					true);
				break;
			case "SUM":
				if (arg is Expression<DataInteger>)
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
						(a) => a, 
						(s, a) => s + a, 
						(s) => s, 
						arg as Expression<DataInteger>);
				else if (arg is Expression<DataFloat>)
					result = new AggregationExpression<DataFloat, DataFloat, DataFloat> (
						(a) => a, 
						(s, a) => s + a, 
						(s) => s, 
						arg as Expression<DataFloat>);
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
						(s, a) => string.Compare (a, s, dataComparer.StringComparison) < 0 ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataString (arg, cultureInfo));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
						(a) => a, 
						(s, a) => a < s ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataInteger (arg, cultureInfo));
				else if (arg.GetResultType () == typeof(DataFloat))
					result = new AggregationExpression<DataFloat, DataFloat, DataFloat> (
						(a) => a, 
						(s, a) => a < s ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataFloat (arg, cultureInfo));
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
						(s, a) => string.Compare (a, s, dataComparer.StringComparison) > 0 ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataString (arg, cultureInfo));
				else if (arg.GetResultType () == typeof(DataInteger))
					result = new AggregationExpression<DataInteger, DataInteger, DataInteger> (
						(a) => a, 
						(s, a) => a > s ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataInteger (arg, cultureInfo));
				else if (arg.GetResultType () == typeof(DataFloat))
					result = new AggregationExpression<DataFloat, DataFloat, DataFloat> (
						(a) => a, 
						(s, a) => a > s ? a : s, 
						(s) => s, 
						ConvertExpression.CreateDataFloat (arg, cultureInfo));
				else {
					throw new ParserException (
						string.Format ("MAX aggregation function cannot be used on datatype '{0}'",
							arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "FIRST":
				result = new AggregationExpression<IData, IData, IData> (
					(a) => a, 
					(s, a) => s, 
					(s) => s, 
					ConvertExpression.CreateData (arg));
				break;
			case "LAST":
				result = new AggregationExpression<IData, IData, IData> (
					(a) => a, 
					(s, a) => a, 
					(s) => s, 
					ConvertExpression.CreateData (arg));
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
				} else if (arg is Expression<DataFloat>) {
					Expression<DataFloat> resultSum = new AggregationExpression<DataFloat, DataFloat, DataFloat> (
						                                  (a) => a, 
						                                  (s, a) => s + a, 
						                                  (s) => s, 
						                                  arg as Expression<DataFloat>);
					Expression<DataInteger> resultCount = new AggregationExpression<DataFloat, DataInteger, DataInteger> (
						                                      (a) => 1, 
						                                      (s, a) => s + 1, 
						                                      (s) => s, 
						                                      arg as Expression<DataFloat>);
					result = new BinaryExpression<DataFloat, DataInteger, DataFloat> (
						(a, b) => a / b, resultSum, resultCount);
				} else {
					throw new ParserException (
						string.Format ("SUM aggregation function cannot be used on datatype '{0}'",
							arg.GetResultType ().ToString ()),
						functionCallTree);
				}
				break;
			case "PREFIX":
				result = new AggregationExpression<DataString, DataString, DataString> (
					(a) => a, 
					(s, a) => s.CommonPrefix (a), 
					(s) => s, 
					ConvertExpression.CreateDataString (arg, cultureInfo));
				break;
			case "ENLIST":
				result = new AggregationExpression<IData, List<IData>, DataString> ((a) => new List<IData> (), 
					delegate(List<IData> s, IData a) {
						s.Add (a);
						return s;
					},
					(s) => s.Enlist ((i) => i.ToString ()), 
					ConvertExpression.CreateData (arg),
					true);
				break;
			case "ENLISTDISTINCT":
				result = new AggregationExpression<IData, SortedSet<ColumnsComparerKey>, DataString> ((a) => new SortedSet<ColumnsComparerKey> (), 
					delegate(SortedSet<ColumnsComparerKey> s, IData a) {
						ColumnsComparerKey columnsComparerKey = new ColumnsComparerKey (new IData[] { a });
						if (!s.Contains (columnsComparerKey))
							s.Add (columnsComparerKey);
						return s;
					},
					(s) => s.Enlist ((i) => i.Members [0].ToString ()), 
					ConvertExpression.CreateData (arg),
					true);
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
					arg2,
					cultureInfo
				);
				break;
			case "LEFT":
				result = BinaryExpression<DataString, DataInteger, DataString>.CreateAutoConvert (
					(a, b) => a.Value.Substring (0, Math.Max (0, Math.Min ((int)b, a.Value.Length))),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "MATCHREGEX":
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive, cultureInfo);
				break;
			case "RIGHT":
				result = BinaryExpression<DataString, DataInteger, DataString>.CreateAutoConvert (
					(a, b) => a.Value.Substring (Math.Max (0, a.Value.Length - Math.Min ((int)b, a.Value.Length))),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2, cultureInfo);
				break;
			case "DATEPART":
				result = UnaryExpression<DataDateTime, DataInteger>.CreateAutoConvert (
					(a) => DatePartHelper.Get ((arg1 as Token<DatePartType>).Value, a), arg2, cultureInfo);
				break;
			case "STARTSWITH":
				result = BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
					(a, b) => a.Value.StartsWith (b, dataComparer.StringComparison),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "ENDSWITH":
				result = BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
					(a, b) => a.Value.EndsWith (b, dataComparer.StringComparison),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "PREFIX":
				result = BinaryExpression<DataString, DataString, DataString>.CreateAutoConvert (
					(a, b) => a.CommonPrefix (b),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "TOSTRINGRADIX":
				result = BinaryExpression<DataInteger, DataInteger, DataString>.CreateAutoConvert (
					(a, b) => Convert.ToString (a.Value, (int)b.Value),
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "FROMSTRINGRADIX":
				result = BinaryExpression<DataString, DataInteger, DataInteger>.CreateAutoConvert (
					(a, b) => Convert.ToInt64 (a.Value, (int)b.Value),
					arg1,
					arg2,
					cultureInfo
				);
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
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive, arg3, cultureInfo);
				break;
			case "REPLACE":
				result = new ReplaceFunction (arg1, arg2, arg3, dataComparer.CaseInsensitive, cultureInfo);
				break;
			case "REPLACEREGEX":
				result = new ReplaceRegexFunction (arg1, arg2, arg3, dataComparer.CaseInsensitive, cultureInfo);
				break;
			case "SUBSTRING":
				result = new SubstringFunction (arg1, arg2, arg3, cultureInfo);
				break;
			case "DATEADD":
				result = BinaryExpression<DataInteger, DataDateTime, DataDateTime>.CreateAutoConvert (
					(a, b) => DatePartHelper.Add ((arg1 as Token<DatePartType>).Value, a.Value, b), arg2, arg3, cultureInfo);
				break;
			case "DATEDIFF":
				result = BinaryExpression<DataDateTime, DataDateTime, DataInteger>.CreateAutoConvert (
					(a, b) => DatePartHelper.Diff ((arg1 as Token<DatePartType>).Value, a.Value, b.Value), arg2, arg3, cultureInfo);
				break;
			case "TOSTRINGRADIX":
				result = TernaryExpression<DataInteger, DataInteger, DataInteger, DataString>.CreateAutoConvert (
					(a, b, c) => Convert.ToString (a.Value, (int)b.Value).PadLeft ((int)c.Value, '0'),
					arg1,
					arg2,
					arg3,
					cultureInfo);
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
				result = new MatchRegexFunction (arg1, arg2, dataComparer.CaseInsensitive, arg3, arg4, cultureInfo);
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
			
			return ConvertExpression.Create (dataType, expr, cultureInfo, format);
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
				result = UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert ((a) => !a, arg, cultureInfo);
				break;
			case "T_PLUS":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => a, arg, cultureInfo);
				else if (arg is Expression<DataFloat>)
					result = UnaryExpression<DataFloat, DataFloat>.CreateAutoConvert ((a) => a, arg, cultureInfo);
				else {
					throw new ParserException (
						string.Format ("Unary operator 'PLUS' cannot be used with datatype {0}",
							arg.GetResultType ().ToString ()),
						operatorTree);
				}
				break;
			case "T_MINUS":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => -a, arg, cultureInfo);
				else if (arg is Expression<DataFloat>)
					result = UnaryExpression<DataFloat, DataFloat>.CreateAutoConvert ((a) => -a, arg, cultureInfo);
				else {
					throw new ParserException (
						string.Format ("Unary operator 'MINUS' cannot be used with datatype {0}",
							arg.GetResultType ().ToString ()),
						operatorTree);
				}
				break;
			case "T_BITWISE_NOT":
				if (arg is Expression<DataInteger>)
					result = UnaryExpression<DataInteger, DataInteger>.CreateAutoConvert ((a) => ~a, arg, cultureInfo);
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
					ParseExpressionBetween (provider, operatorTree),
					cultureInfo);
			} else if (operatorText == "T_IN" || operatorText == "T_ANY" || operatorText == "T_ALL") {
				return ParseExpressionInSomeAnyAll (provider, operatorTree);
			} else if (operatorText == "T_NOTIN") {
				return UnaryExpression<DataBoolean, DataBoolean>.CreateAutoConvert (
					(a) => !a,
					ParseExpressionInSomeAnyAll (provider, operatorTree),
					cultureInfo);
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
					arg2,
					cultureInfo
				);
				break;
			case "T_OR":
				result = BinaryExpression<DataBoolean, DataBoolean, DataBoolean>.CreateAutoConvert (
					(a, b) => a || b,
					arg1,
					arg2,
					cultureInfo
				);
				break;
			case "T_MATCH":
				result = new MatchOperator (arg1, arg2, dataComparer.CaseInsensitive, cultureInfo);
				break;
			case "T_NOTMATCH":
				result = new UnaryExpression<DataBoolean, DataBoolean> (
					(a) => !a,
					new MatchOperator (arg1, arg2, dataComparer.CaseInsensitive, cultureInfo)
				);
				break;
			case "T_LIKE":
				result = new LikeOperator (arg1, arg2, dataComparer.CaseInsensitive, cultureInfo);
				break;
			case "T_NOTLIKE":
				result = new UnaryExpression<DataBoolean, DataBoolean> (
					(a) => !a,
					new LikeOperator (arg1, arg2, dataComparer.CaseInsensitive, cultureInfo)
				);
				break;
			case "T_PLUS":
				{
					if (arg1 is Expression<DataString> || arg2 is Expression<DataString>)
						result = BinaryExpression<DataString, DataString, DataString>.CreateAutoConvert (
							(a, b) => a + b,
							arg1,
							arg2,
							cultureInfo
						);
					else if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
						result = BinaryExpression<DataFloat, DataFloat, DataFloat>.CreateAutoConvert (
							(a, b) => a + b,
							arg1,
							arg2,
							cultureInfo
						);
					else if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
							(a, b) => a + b,
							arg1,
							arg2,
							cultureInfo
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
					if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
						result = BinaryExpression<DataFloat, DataFloat, DataFloat>.CreateAutoConvert (
							(a, b) => a - b,
							arg1,
							arg2,
							cultureInfo
						);
					else if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
							(a, b) => a - b,
							arg1,
							arg2,
							cultureInfo
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
					if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
						result = BinaryExpression<DataFloat, DataFloat, DataFloat>.CreateAutoConvert (
							(a, b) => a / b,
							arg1,
							arg2,
							cultureInfo
						);
					else if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
							(a, b) => a / b,
							arg1,
							arg2,
							cultureInfo
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
					if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
						result = BinaryExpression<DataFloat, DataFloat, DataFloat>.CreateAutoConvert (
							(a, b) => a * b,
							arg1,
							arg2,
							cultureInfo
						);
					else if (arg1 is Expression<DataInteger>)
						result = BinaryExpression<DataInteger, DataInteger, DataInteger>.CreateAutoConvert (
							(a, b) => a * b,
							arg1,
							arg2,
							cultureInfo
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
							arg2,
							cultureInfo
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
							arg2,
							cultureInfo
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
							arg2,
							cultureInfo
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
							arg2,
							cultureInfo
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
						BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetStringComparer (
							operatorText,
							false,
							dataComparer
						),
						arg1, arg2, cultureInfo);
				else if (arg1 is Expression<DataBoolean> || arg2 is Expression<DataBoolean>)
					result = 
						BinaryExpression<DataBoolean, DataBoolean, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetBooleanComparer (
							operatorText,
							false
						),
						arg1, arg2, cultureInfo);
				else if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
					result = 
						BinaryExpression<DataFloat, DataFloat, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetFloatComparer (
							operatorText,
							false
						),
						arg1, arg2, cultureInfo);
				else if (arg1 is Expression<DataInteger>)
					result = 
						BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetIntegerComparer (
							operatorText,
							false
						),
						arg1, arg2, cultureInfo);
				else {
					throw new ParserException (
						string.Format (
							"Binary operator 'EQUAL/NOTEQUAL' cannot be used with datatypes {0} and {1}",
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
						BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetStringComparer (operatorText, false, dataComparer), 
						arg1, arg2, cultureInfo);
				else if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat>)
					result = 
						BinaryExpression<DataFloat, DataFloat, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetFloatComparer (operatorText, false), 
						arg1, arg2, cultureInfo);
				else if (arg1 is Expression<DataInteger>)
					result = 
						BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (
						OperatorHelper.GetIntegerComparer (operatorText, false), 
						arg1, arg2, cultureInfo);
				else {
					throw new ParserException (
						string.Format (
							"Binary operator 'LESS/GREATER/NOTLESS/NOTGREATER' cannot be used with datatypes {0} and {1}",
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
					(a, b, c) => string.Compare (a, b, dataComparer.StringComparison) >= 0
					&& string.Compare (a, c, dataComparer.StringComparison) <= 0,
					arg1,
					arg2,
					arg3,
					cultureInfo);
			else if (arg1 is Expression<DataFloat> || arg2 is Expression<DataFloat> || arg3 is Expression<DataFloat>)
				result = TernaryExpression<DataFloat, DataFloat, DataFloat, DataBoolean>.CreateAutoConvert (
					(a, b, c) => (a >= b) && (a <= c),
					arg1,
					arg2,
					arg3,
					cultureInfo);
			else if (arg1 is Expression<DataInteger>)
				result = TernaryExpression<DataInteger, DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (
					(a, b, c) => (a >= b) && (a <= c),
					arg1,
					arg2,
					arg3,
					cultureInfo);
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
						OperatorHelper.GetStringComparer (op, all, dataComparer),
						cultureInfo);
				else if (arg2 is Expression<DataInteger>)
					result = new AnyListOperator<DataInteger> (
						(Expression<DataInteger>)arg2,
						expressionList,
						OperatorHelper.GetIntegerComparer (op, all),
						cultureInfo);
				else if (arg2 is Expression<DataFloat>)
					result = new AnyListOperator<DataFloat> (
						(Expression<DataFloat>)arg2,
						expressionList,
						OperatorHelper.GetFloatComparer (op, all),
						cultureInfo);
				else
					throw new ParserException (
						string.Format (
							"Binary operator '{0}' cannot be used with datatype {1}",
							inTree.GetChild (0).Text,
							target.Text),
						target
					);
			} else if (target.Text == "T_SELECT") {
				IProvider subProvider = ParseCommandSelect (target);
				if (arg2 is Expression<DataString>)
					result = new AnySubqueryOperator<DataString> (
						(Expression<DataString>)arg2,
						subProvider,
						OperatorHelper.GetStringComparer (op, all, dataComparer)
					);
				else if (arg2 is Expression<DataInteger>)
					result = new AnySubqueryOperator<DataInteger> (
						(Expression<DataInteger>)arg2,
						subProvider,
						OperatorHelper.GetIntegerComparer (op, all)
					);
				else if (arg2 is Expression<DataFloat>)
					result = new AnySubqueryOperator<DataFloat> (
						(Expression<DataFloat>)arg2,
						subProvider,
						OperatorHelper.GetFloatComparer (op, all)
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
				new ColumnProvider (
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
			throw new InvalidOperationException (string.Format ("Column name {0} not found", columnName));
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
			} else if (type == typeof(DataFloat)) {
				return new ColumnExpression<DataFloat> (provider, columnOrdinal);
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
				for (whenNo = 1; expressionTree.GetChild (whenNo).Text == "T_CASE_WHEN"; whenNo++) {
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
							BinaryExpression<DataString, DataString, DataBoolean>.CreateAutoConvert (
							OperatorHelper.GetStringComparer ("T_EQUAL", false, dataComparer),
							source, destination, cultureInfo);
					else if (source is Expression<DataFloat> || destination is Expression<DataFloat>)
						whenItem.Check = 
							BinaryExpression<DataFloat, DataFloat, DataBoolean>.CreateAutoConvert (
							OperatorHelper.GetFloatComparer ("T_EQUAL", false),
							source, destination, cultureInfo);
					else if (source is Expression<DataInteger>)
						whenItem.Check = 
							BinaryExpression<DataInteger, DataInteger, DataBoolean>.CreateAutoConvert (
							OperatorHelper.GetIntegerComparer ("T_EQUAL", false),
							source, destination, cultureInfo);
					else {
						throw new ParserException (
							string.Format (
								"Binary operator 'CASE' cannot be used with datatypes {0} and {1}",
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
				for (whenNo = 0; expressionTree.GetChild (whenNo).Text == "T_CASE_WHEN"; whenNo++) {
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
			
			return new VariableExpression (variableName, type).GetTyped (cultureInfo);
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
	}
}

