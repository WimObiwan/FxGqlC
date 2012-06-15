using System;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace FxGqlLib
{
	public class IntoProvider : IProvider
	{
		IProvider provider;
		FileOptions fileOptions;
		ProviderRecord record;
		GqlQueryState gqlQueryState;
		
		public IntoProvider (IProvider provider, FileOptions fileOptions)
		{
			this.provider = provider;
			this.fileOptions = fileOptions;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return new string[] {};
		}

		public int GetColumnOrdinal(string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes()
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

		private string GetNewLine (FileOptions.NewLineEnum lineEnd)
		{
			if (fileOptions.NewLine == FileOptions.NewLineEnum.Dos)
				return "\r\n";
			else if (fileOptions.NewLine == FileOptions.NewLineEnum.Unix)
				return "\n";
			else if (fileOptions.NewLine == FileOptions.NewLineEnum.Mac)
				return "\r";
			else
				return Environment.NewLine;
		}

		public bool GetNextRecord ()
		{
			if (!fileOptions.Overwrite && !fileOptions.Append
			    && File.Exists(fileOptions.FileName))
				throw new InvalidOperationException(
					string.Format("File '{0}' already exists. Use '-overwrite' or '-append' option to change the existing file.", 
				              fileOptions.FileName));

			if (string.Compare (Path.GetExtension (fileOptions.FileName), ".zip", StringComparison.InvariantCultureIgnoreCase) == 0) {
				using (FileStream fileStream = new FileStream(fileOptions.FileName, FileMode.Create)) {
					using (ZipOutputStream zipOutputStream = new ZipOutputStream(fileStream)) {
						zipOutputStream.SetLevel (9);
						ZipEntry zipEntry = new ZipEntry ("output.txt");
						zipEntry.DateTime = DateTime.Now;
						zipOutputStream.PutNextEntry (zipEntry);
						
						
						using (SelectProvider provider = new SelectProvider (
							new List<IExpression> () { new FormatColumnListFunction ("\t") },
							this.provider)) {

							provider.Initialize (this.gqlQueryState);

							using (ProviderToStream stream = new ProviderToStream(provider, System.Text.Encoding.GetEncoding (0), GetNewLine(fileOptions.NewLine))) {
								using (BufferedStream bufferedStream = new BufferedStream(stream)) {		
									byte[] buffer = new byte[4096];
									int size;
									while ((size = stream.Read (buffer, 0, buffer.Length)) > 0) {
										zipOutputStream.Write (buffer, 0, size);
									}
								}
							}
//							while (provider.GetNextRecord()) {
//								streamWriter.WriteLine (provider.Record.Columns [0].ToString ());
//								//byte[] buffer = System.Text.Encoding.GetEncoding (0).GetBytes (provider.Record.Columns [0].ToString ());
//								//zipOutputStream.Write (buffer, 0, buffer.Length);
//							}

							provider.Uninitialize ();
						}
						zipOutputStream.CloseEntry ();
						zipOutputStream.IsStreamOwner = false;
						zipOutputStream.Finish ();
						zipOutputStream.Flush ();
						zipOutputStream.Close ();
					}
				}
			} else {
				using (StreamWriter outputStream = new StreamWriter(fileOptions.FileName, fileOptions.Append)) {
					outputStream.NewLine = GetNewLine (fileOptions.NewLine);
					using (SelectProvider provider = new SelectProvider (
						new List<IExpression> () { new FormatColumnListFunction ("\t") },
						this.provider)) {
	
						provider.Initialize (this.gqlQueryState);

						while (provider.GetNextRecord()) {
							outputStream.WriteLine (provider.Record.Columns [0].ToString ());
						}

						provider.Uninitialize ();
					}
				}
			}

//			using (FileStream fileStream = new FileStream(fileOptions.FileName, FileMode.Create)) {
//				using (SelectProvider provider = new SelectProvider (
//						new List<IExpression> () { new FormatColumnListFunction ("\t") },
//						this.provider)) {
//	
//					provider.Initialize ();
//
//					using (ProviderToStream stream = new ProviderToStream(provider, System.Text.Encoding.GetEncoding (0), GetNewLine(fileOptions.NewLine))) {
//						byte[] buffer = new byte[10];
//						int size;
//						while ((size = stream.Read (buffer, 0, buffer.Length)) > 0) {
//							fileStream.Write (buffer, 0, size);
//						}
//					}
//					
//					provider.Uninitialize ();
//				}
//			}
			
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
		
		
		class ProviderToStream : Stream
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
						Buffer.BlockCopy (readBuffer, count, readBuffer2, 0, readBuffer.Length - count);
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
		}
	}
}

