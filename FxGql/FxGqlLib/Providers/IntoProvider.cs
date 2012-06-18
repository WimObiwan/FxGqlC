using System;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace FxGqlLib
{
	public class IntoProvider : IProvider
	{
		IProvider provider;
		FileOptionsIntoClause fileOptions;
		ProviderRecord record;
		GqlQueryState gqlQueryState;
		
		public IntoProvider (IProvider provider, FileOptionsIntoClause fileOptions)
		{
			this.provider = provider;
			this.fileOptions = fileOptions;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return new string[] {};
		}

		public int GetColumnOrdinal (string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			record = new ProviderRecord ();
			record.Columns = new IComparable[] { };
			record.OriginalColumns = new IComparable[] { };
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
			if (!fileOptions.Overwrite && !fileOptions.Append
				&& File.Exists (fileOptions.FileName))
				throw new InvalidOperationException (
					string.Format ("File '{0}' already exists. Use '-overwrite' or '-append' option to change the existing file.", 
				              fileOptions.FileName)
				);

			if (string.Compare (
				Path.GetExtension (fileOptions.FileName),
				".zip",
				StringComparison.InvariantCultureIgnoreCase
			) == 0) {
				using (FileStream fileStream = new FileStream(fileOptions.FileName, FileMode.Create)) {
					using (ZipOutputStream zipOutputStream = new ZipOutputStream(fileStream)) {
						zipOutputStream.SetLevel (9);
						ZipEntry zipEntry = new ZipEntry ("output.txt");
						zipEntry.DateTime = DateTime.Now;
						zipOutputStream.PutNextEntry (zipEntry);
						
						DumpProviderToStream (provider, zipOutputStream, this.gqlQueryState,
						                      "\t", GetNewLine (fileOptions.NewLine));
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
				using (FileStream outputStream = new FileStream(fileOptions.FileName, fileMode, FileAccess.Write, FileShare.None)) {
					DumpProviderToStream (provider, outputStream, this.gqlQueryState,
					                      "\t", GetNewLine (fileOptions.NewLine));
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

		public static void DumpProviderToStream (IProvider provider, Stream outputStream, GqlQueryState gqlQueryState, string columnDelimiter, string recordDelimiter)
		{
			using (TextWriter writer = new StreamWriter(outputStream, System.Text.Encoding.GetEncoding (0))) {
				writer.NewLine = recordDelimiter;

				DumpProviderToStream (
					provider,
					writer,
					gqlQueryState,
					columnDelimiter,
					GqlEngineState.HeadingEnum.Off
				);
			}
		}
		
		public static void DumpProviderToStream (IProvider provider, TextWriter outputWriter, GqlQueryState gqlQueryState, 
		                                         string columnDelimiter, GqlEngineState.HeadingEnum heading)
		{
			using (SelectProvider selectProvider = new SelectProvider (
								new List<IExpression> () { new FormatColumnListFunction (columnDelimiter) },
								provider)) {
	
				selectProvider.Initialize (gqlQueryState);

				if (selectProvider.GetNextRecord ()) {
					if (heading != GqlEngineState.HeadingEnum.Off) {
						FormatColumnListFunction formatColumnListFunction = new FormatColumnListFunction (columnDelimiter);
						string[] columnTitles = provider.GetColumnTitles ();
						outputWriter.WriteLine (formatColumnListFunction.Evaluate (columnTitles));
						if (heading == GqlEngineState.HeadingEnum.OnWithRule) {
							string[] columnTitlesRule = new string[columnTitles.Length];
							for (int i = 0; i < columnTitles.Length; i++)
								columnTitlesRule [i] = new string ('=', columnTitles [i].Length);
							outputWriter.WriteLine (formatColumnListFunction.Evaluate (columnTitlesRule));
						}
					}

					do {
						outputWriter.WriteLine (selectProvider.Record.Columns [0].ToString ());
					} while (selectProvider.GetNextRecord());
				}

				selectProvider.Uninitialize ();
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

