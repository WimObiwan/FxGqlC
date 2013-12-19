using System;
using System.IO;

namespace FxGqlLib
{
	public class AsyncStreamWriter : Stream
	{
		Stream stream;
		IAsyncResult asyncResult;

		public AsyncStreamWriter (Stream stream)
		{
			this.stream = stream;
		}

		#region implemented abstract members of System.IO.Stream

		public override void Flush ()
		{
			stream.Flush ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			throw new System.NotSupportedException ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new System.NotSupportedException ();
		}

		public override void SetLength (long value)
		{
			throw new System.NotSupportedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			WriteMoreData (buffer, offset, count);
		}

		void WriteMoreData (byte[] buffer, int offset, int count)
		{
			EndWriteMoreData ();
			BeginWriteMoreData (buffer, offset, count);
		}

		void BeginWriteMoreData (byte[] buffer, int offset, int count)
		{
			asyncResult = stream.BeginWrite (buffer, offset, count, null, null);
		}

		void EndWriteMoreData ()
		{
			if (asyncResult != null)
				stream.EndWrite (asyncResult);
		}

		public override bool CanRead {
			get {
				return false;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				throw new System.NotSupportedException ();
			}
		}

		public override long Position {
			get {
				throw new System.NotSupportedException ();
			}
			set {
				throw new System.NotSupportedException ();
			}
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException ();
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return stream.BeginWrite (buffer, offset, count, callback, state);
		}

		public override bool CanTimeout {
			get {
				return stream.CanTimeout;
			}
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			throw new NotSupportedException ();
		}

		public override void Close ()
		{
			stream.Close ();
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			stream.EndWrite (asyncResult);
		}

		public override int ReadByte ()
		{
			throw new NotSupportedException ();
		}

		public override int ReadTimeout {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override int WriteTimeout {
			get {
				return stream.WriteTimeout;
			}
			set {
				stream.WriteTimeout = value;
			}
		}

		public override void WriteByte (byte value)
		{
			stream.Write (new byte[] { value }, 0, 1);
		}

		#endregion

		protected override void Dispose (bool disposing)
		{
			EndWriteMoreData ();
			stream.Dispose ();
			base.Dispose (disposing);
		}
	}
}

