using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace FxGqlLib
{
	public class ParserException : Exception
	{
		public ParserException (string message, int line, int pos, Exception innerException)
			: base (string.Format ("Parsing failed at line {1}, position {2}. {0}", message, line, pos), innerException)
		{
			Line = line;
			Pos = pos;
		}

		public ParserException (RecognitionException recognitionException)
            : this (recognitionException.Message, recognitionException.Line, recognitionException.CharPositionInLine, recognitionException)
		{
		}

		public ParserException (string message, ITree tree)
            : this (message, tree.Line, tree.CharPositionInLine + 1, null)
		{
		}

		public ParserException (string message, ITree tree, Exception innerException)
            : this (message, tree.Line, tree.CharPositionInLine + 1, innerException)
		{
		}

		public int Line { get; private set; }

		public int Pos { get; private set; }
	}

	public class UnexpectedTokenAntlrException : ParserException
	{
		public UnexpectedTokenAntlrException (string expectedToken, ITree tree)
            : base (string.Format ("Unexpected token. Expected {0}, but found {1}.", expectedToken, tree.Text), tree)
		{
		}

		public UnexpectedTokenAntlrException (ITree tree)
            : base (string.Format ("Unexpected token. Found {0}, but expected another token.", tree.Text), tree)
		{
		}
	}

	public class NotEnoughSubTokensAntlrException : ParserException
	{
		public NotEnoughSubTokensAntlrException (ITree tree)
            : base ("Not enough sub-tokens.", tree)
		{
		}
	}
}