using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace FxGqlLib
{
	public class IntoProvider : IProvider
	{
		readonly IProvider provider;
		readonly FileOptionsIntoClause fileOptions;
		readonly string columnDelimiter;

		ProviderRecord record;
		GqlQueryState gqlQueryState;

		public IProvider InnerProvider { get { return provider; } }
		public FileOptionsIntoClause FileOptions { get { return fileOptions; } }

		public IntoProvider (IProvider provider, FileOptionsIntoClause fileOptions)
		{
			this.provider = provider;
			this.fileOptions = fileOptions;
			columnDelimiter = fileOptions.ColumnDelimiter;
			if (columnDelimiter == null) {
				if (fileOptions.Format == FileOptionsIntoClause.FormatEnum.Csv)
					columnDelimiter = ",";
				else
					columnDelimiter = "\t";
			}
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return new ColumnName[] {};
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { };
		}
		
		public Type[] GetNewColumnTypes ()
		{
			return new Type[] { };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			record = new ProviderRecord (this, true);
			record.LineNo = 1;
			this.gqlQueryState = gqlQueryState;
		}

		private string GetNewLine (FileOptionsIntoClause.NewLineEnum lineEnd)
		{
			if (fileOptions.NewLine == FileOptionsIntoClause.NewLineEnum.Dos)
				return "\r\n";
			else if (fileOptions.NewLine == FileOptionsIntoClause.NewLineEnum.Unix)
				return "\n";
			else if (fileOptions.NewLine == FileOptionsIntoClause.NewLineEnum.Mac)
				return "\r";
			else
				return Environment.NewLine;
		}

		public bool GetNextRecord ()
		{
			string fileName = fileOptions.FileName.EvaluateAsData (gqlQueryState).ToDataString ();

			if (fileName.StartsWith ("#"))
				fileName = Path.Combine (gqlQueryState.TempDirectory, fileName);
			else
				fileName = Path.Combine (gqlQueryState.CurrentDirectory, fileName); 

			if (!fileOptions.Overwrite && !fileOptions.Append
				&& File.Exists (fileName))
				throw new InvalidOperationException (
					string.Format ("File '{0}' already exists. Use '-overwrite' or '-append' option to change the existing file.", 
				              fileName)
				);

			if (string.Compare (
				Path.GetExtension (fileName),
				".zip",
				StringComparison.InvariantCultureIgnoreCase
			) == 0) {
				using (FileStream fileStream = new FileStream(fileName, FileMode.Create)) {
					using (AsyncStreamWriter asyncStreamWriter = new AsyncStreamWriter (fileStream)) {
						using (ZipOutputStream zipOutputStream = new ZipOutputStream(fileStream)) {
							zipOutputStream.SetLevel (9);
							ZipEntry zipEntry = new ZipEntry ("output.txt");
							zipEntry.DateTime = DateTime.Now;
							zipOutputStream.PutNextEntry (zipEntry);
						
							DumpProviderToStream (provider, zipOutputStream, this.gqlQueryState,
						                          columnDelimiter, GetNewLine (fileOptions.NewLine), fileOptions.Heading,
							                      fileOptions.Format);
						}
					}
				}
			} else {
				FileMode fileMode;
				if (fileOptions.Append)
					fileMode = FileMode.Append;
				else if (fileOptions.Overwrite)
					fileMode = FileMode.Create;
				else
					fileMode = FileMode.CreateNew;
				using (FileStream outputStream = new FileStream(fileName, fileMode, FileAccess.Write, FileShare.Read)) {
					using (AsyncStreamWriter asyncStreamWriter = new AsyncStreamWriter(outputStream)) {
						DumpProviderToStream (provider, outputStream, this.gqlQueryState,
					                          columnDelimiter, GetNewLine (fileOptions.NewLine), fileOptions.Heading,
						                      fileOptions.Format);
					}
				}
			}

			return false;
		}

		public void Uninitialize ()
		{
			record = null;
		}

		public ProviderRecord Record {
			get {
				return record;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			Uninitialize ();
			provider.Dispose ();
		}
		#endregion

		public static void DumpProviderToStream (IProvider provider, Stream outputStream, GqlQueryState gqlQueryState, 
		                                         string columnDelimiter, string recordDelimiter, GqlEngineState.HeadingEnum heading,
		                                         FileOptionsIntoClause.FormatEnum format)
		{
			using (TextWriter writer = new StreamWriter(outputStream, System.Text.Encoding.GetEncoding (0))) {
				writer.NewLine = recordDelimiter;

				DumpProviderToStream (
					provider,
					writer,
					gqlQueryState,
					columnDelimiter,
					heading,
					0,
					format
				);
			}
		}
		
		public static void DumpProviderToStream (IProvider provider, TextWriter outputWriter, GqlQueryState gqlQueryState, 
		                                         string columnDelimiter, GqlEngineState.HeadingEnum heading, int autoSize, 
		                                         FileOptionsIntoClause.FormatEnum format)
		{
			try {
				provider.Initialize (gqlQueryState);

				List<string[]> list = new List<string[]> ();
				if (heading != GqlEngineState.HeadingEnum.Off) {
					ColumnName[] columnTitles = provider.GetColumnNames ();
					if (columnTitles.Length > 0) {
						string[] columnTitleStrings = columnTitles.Select (p => p.ToStringWithoutBrackets ()).ToArray ();
						list.Add (columnTitleStrings);
						if (heading == GqlEngineState.HeadingEnum.OnWithRule) {
							string[] columnTitlesRule = new string[columnTitles.Length];
							for (int i = 0; i < columnTitles.Length; i++)
								columnTitlesRule [i] = new string ('=', columnTitleStrings [i].ToString ().Length);
							list.Add (columnTitlesRule);
						}
					}
				}

				for (int record = 0; (autoSize == -1 || record < autoSize); record++) {
					try {
						if (!provider.GetNextRecord ())
							break;
					} catch (Exception x) {
						gqlQueryState.Warnings.Add (
						new Exception (string.Format ("Line ignored, {0}", x.Message), x)
						);
						record --;
						continue;
					}
					list.Add (provider.Record.Columns.Select (p => p.ToString ()).ToArray ());
				}

				FormatColumnsFunction formatColumnListFunction;
				if (autoSize == 0) {
					if (format == FileOptionsIntoClause.FormatEnum.Csv)
						formatColumnListFunction = new FormatCsvFunction (columnDelimiter);
					else
						formatColumnListFunction = new FormatColumnListFunction (columnDelimiter);
				} else {
					int[] max = new int[list [0].Length];
					foreach (string[] item in list) {
						for (int col = 0; col < max.Length; col++)
							max [col] = Math.Max (item [col].Length, max [col]);
					}
					Type[] types = provider.GetColumnTypes ();
					for (int col = 0; col < max.Length; col++)
						if (types [col] != typeof(DataString))
							max [col] = -max [col];
					formatColumnListFunction = new FormatColumnListFunction (columnDelimiter, max);
				}

				foreach (var item in list) {
					outputWriter.WriteLine (formatColumnListFunction.Evaluate (item));
				}

				do {
					try {
						if (!provider.GetNextRecord ())
							break;
						outputWriter.WriteLine (formatColumnListFunction.Evaluate (provider.Record.Columns.Select (p => p.ToString ())));
					} catch (InvalidOperationException) {
						throw;
					} catch (Exception x) {
						gqlQueryState.Warnings.Add (
						new Exception (string.Format ("Line ignored, {0}", x.Message), x)
						);
					}
				} while (true);
			} finally {
				provider.Uninitialize ();
			}
		}
		
		/*class ProviderToStream : Stream
		{
			IProvider provider;
			System.Text.Encoding encoding;
			string lineEnd;
			byte[] readBuffer;
			
			public ProviderToStream (IProvider provider, System.Text.Encoding encoding, string lineEnd)
			{
				this.provider = provider;
				this.encoding = encoding;
				this.lineEnd = lineEnd;
			}
			
			#region implemented abstract members of System.IO.Stream
			public override void Flush ()
			{
				throw new NotSupportedException ();
			}

			public override int Read (byte[] buffer, int offset, int count)
			{
				if (readBuffer != null) {
					if (count >= readBuffer.Length) {
						Buffer.BlockCopy (readBuffer, 0, buffer, offset, readBuffer.Length);
						int bytesRead = readBuffer.Length;
						readBuffer = null;
						return bytesRead;
					} else {
						Buffer.BlockCopy (readBuffer, 0, buffer, offset, count);
						byte[] readBuffer2 = new byte[readBuffer.Length - count];
						Buffer.BlockCopy (
							readBuffer,
							count,
							readBuffer2,
							0,
							readBuffer.Length - count
						);
						readBuffer = readBuffer2;
						return count;
					}
				}
				
				if (!provider.GetNextRecord ())
					return 0;
				
				string text = provider.Record.Columns [0].ToString () + lineEnd;
				int maxByteCount = encoding.GetMaxByteCount (text.Length);
				if (maxByteCount <= count) {
					int bytes = encoding.GetBytes (text, 0, text.Length, buffer, offset);
					return bytes;
				}
				
				byte[] buffer2 = encoding.GetBytes (text);
				if (count >= buffer2.Length) {
					Buffer.BlockCopy (buffer2, 0, buffer, offset, buffer2.Length);
					int bytesRead = buffer2.Length;
					readBuffer = null;
					return bytesRead;
				} else {
					Buffer.BlockCopy (buffer2, 0, buffer, offset, count);
					byte[] readBuffer2 = new byte[buffer2.Length - count];
					Buffer.BlockCopy (buffer2, count, readBuffer2, 0, buffer2.Length - count);
					readBuffer = readBuffer2;
					return count;
				}
			}

			public override long Seek (long offset, SeekOrigin origin)
			{
				throw new NotSupportedException ();
			}

			public override void SetLength (long value)
			{
				throw new NotSupportedException ();
			}

			public override void Write (byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException ();
			}

			public override bool CanRead {
				get {
					return true;
				}
			}

			public override bool CanSeek {
				get {
					return false;
				}
			}

			public override bool CanWrite {
				get {
					return false;
				}
			}

			public override long Length {
				get {
					throw new NotSupportedException ();
				}
			}

			public override long Position {
				get {
					throw new NotSupportedException ();
				}
				set {
					throw new NotSupportedException ();
				}
			}
			#endregion
		}*/
	}
}

