using System;
using System.IO;

namespace FxGqlLib
{
	public class AsyncStreamReader : Stream
	{
		Stream stream;
		byte[] memoryBuffer;
		int memoryBufferPos;
		int memoryBufferLen;
		byte[] readBuffer;
		IAsyncResult asyncResult;
		bool endOfStream;

		public AsyncStreamReader (Stream stream, int blockSize)
		{
			this.stream = stream;
			this.memoryBuffer = new byte[blockSize];
			this.memoryBufferPos = 0;
			this.memoryBufferLen = 0;
			this.readBuffer = new byte[blockSize];
			this.asyncResult = null;
			this.endOfStream = false;
		}

		#region implemented abstract members of System.IO.Stream
		public override void Flush ()
		{
			throw new NotSupportedException ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int totalLen = 0;
			do {
				if (memoryBufferPos == memoryBufferLen) {
					if (endOfStream)
						return totalLen;
					ReadMoreData ();
					if (memoryBufferPos == memoryBufferLen)
						return totalLen;
				}

				int len;
				bool readMore;
				if (memoryBufferLen - memoryBufferPos > count) {
					readMore = false;
					len = count;
				} else {
					readMore = true;
					len = memoryBufferLen - memoryBufferPos;
				}
				Buffer.BlockCopy (memoryBuffer, memoryBufferPos, buffer, offset, len);
				totalLen += len;
				memoryBufferPos += len;
				if (!readMore)
					return totalLen;
				offset += len;
				count -= len;
			} while (true);
		}

		void ReadMoreData ()
		{
			bool result = EndReadMoreData ();
			if (!endOfStream)
				BeginReadMoreData ();
			if (!result) {
				EndReadMoreData ();
				BeginReadMoreData ();
			}
		}

		void BeginReadMoreData ()
		{
			asyncResult = stream.BeginRead (readBuffer, 0, readBuffer.Length, null, null);
		}

		bool EndReadMoreData ()
		{
			if (asyncResult != null) {
				memoryBufferLen = stream.EndRead (asyncResult);
				asyncResult = null;
				if (memoryBufferLen == 0)
					endOfStream = true;

				byte[] swapBuffer = memoryBuffer;
				memoryBuffer = readBuffer;
				readBuffer = swapBuffer;
				memoryBufferPos = 0;
				return true;
			} else {
				return false;
			}
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
			throw new System.NotSupportedException ();
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
				return stream.Length;
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
			return stream.BeginRead (buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException ();
		}

		public override bool CanTimeout {
			get {
				return stream.CanTimeout;
			}
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			return stream.EndRead (asyncResult);
		}

		public override void Close ()
		{
			stream.Close ();
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			throw new NotSupportedException ();
		}

		public override int ReadByte ()
		{
			if (memoryBufferPos == memoryBufferLen) {
				if (endOfStream)
					return -1;
				ReadMoreData ();
				if (memoryBufferPos == memoryBufferLen)
					return -1;
			}

			memoryBufferPos++;
			return memoryBuffer [memoryBufferPos];
		}

		public override int ReadTimeout {
			get {
				return stream.ReadTimeout;
			}
			set {
				stream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override void WriteByte (byte value)
		{
			throw new NotSupportedException ();
		}
		#endregion

		protected override void Dispose (bool disposing)
		{
			EndReadMoreData ();
			stream.Dispose ();
			base.Dispose (disposing);
		}

	}
}

