using System;
using System.IO;

namespace FxGqlLib
{
	public class OldCharBuffer
	{
		public OldCharBuffer ()
			: this(32 * 1024)
		{
		}

		public OldCharBuffer (int bufferSize)
		{
		}

		public string GetNextLine (StreamReader reader)
		{
			return reader.ReadLine ();
		}
	}

	public class CharArrayReader : IDisposable
	{
		StreamReader reader;
		char[] buffer;
		int dataLen;
		int dataPos;
		bool endOfData;

		public CharArrayReader (StreamReader reader)
			: this(reader, 64)
		{
		}

		public CharArrayReader (StreamReader reader, int bufferSize)
		{
			this.reader = reader;
			buffer = new char[bufferSize];
			dataLen = 0;
			dataPos = 0;
			endOfData = false;
		}

		void ReadMoreData (int start)
		{
			if (start == 0 && dataLen == buffer.Length) {
				Array.Resize (ref buffer, buffer.Length * 2);
			}

			int remainder = dataLen - start;
			if (remainder > 0 && start != 0)
				Array.Copy (buffer, start, buffer, 0, remainder);

			int len = reader.ReadBlock (buffer, remainder, buffer.Length - remainder);
			dataLen = remainder + len;
			dataPos -= start;

			endOfData = len == 0;
		}

		void ReadLine (out int start, out int len)
		{
			start = dataPos;
			len = -1;
			char lastEol = char.MinValue;
			for (;; dataPos++) {
				if (dataPos >= dataLen) {
					if (endOfData) {
						if (len == -1) {
							len = dataPos - start;
							if (len == 0)
								start = -1;
						}
						return;
					}
					ReadMoreData (start);
					start = 0;
					if (dataPos >= dataLen) {
						if (len == -1) {
							len = dataPos - start;
							if (len == 0)
								start = -1;
						}
						return;
					}
				}

				char ch = buffer [dataPos];
				if (lastEol != char.MinValue) {
					if ((lastEol == '\u000D' && ch == '\u000A') // CRLF
						|| (lastEol == '\u000A' && ch == '\u000D')) // LFCR
						dataPos ++;
					return;
				}
				if (ch == '\u000D' || ch == '\u000A' /*|| ch == '\u2028' || ch == '\u2029' || ch == '\u0003'*/) {
					len = dataPos - start;
					if (ch == '\u000D' || ch == '\u000A') {
						lastEol = ch;
						continue;
					}
					dataPos++;
					return;
				}
			}
		}

		public void ReadLine (out char[] buffer, out int start, out int len)
		{
			ReadLine (out start, out len);
			buffer = this.buffer;
		}

		public string ReadLine ()
		{
			int start, len;
			ReadLine (out start, out len);
			if (start == -1)
				return null;
			else
				return new string (buffer, start, len);

//			start = pos;
//			do {
//				if (pos >= len) {
//					start = 0;
//					if (!Fill(reader)) {
//						len = pos;
//						return;
//					}
//				}
//				char lastCh = 0;
//				for (; pos < len; ++pos) {
//					if (lastCh == '\u000D' || lastCh == '\u000A') {
//
//					char ch = buffer[pos];
//					if (ch == '\u000D' || ch == '\u000A') {
//						if (pos + 1 < len) {
//							char ch2 = buffer[pos + 1];
//					}
//					|| ch == '\u2028' || ch == '\u2029') {
//					if ( || ch == '\u2028' || ch == '\u2029') {
//						len = pos - start;
//						if (ch == 
//					}
//			}
		}

		public void Close ()
		{
			if (reader != null) {
				reader.Close ();
				reader = null;
			}
			buffer = null;
			dataLen = 0;
			dataPos = 0;
			endOfData = true;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Close ();
		}
		#endregion

	}
}

