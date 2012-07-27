using System;
using System.IO;

namespace FxGqlW
{
	public class OutputTextWriter : TextWriter
	{
		IOutputWriter outputWriter;

		public OutputTextWriter (IOutputWriter outputWriter)
		{
			this.outputWriter = outputWriter;
		}

		#region implemented abstract members of System.IO.TextWriter
		public override System.Text.Encoding Encoding {
			get {
				throw new System.NotImplementedException ();
			}
		}
		#endregion

		public override void WriteLine (string value)
		{
			outputWriter.WriteLine (value);
		}
	}
}

