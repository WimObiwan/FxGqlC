using System;

namespace FxGqlLib
{
	public class ProviderRecord
	{
		//public string Text { get; set; }
		public string Source { get; set; }

		public long LineNo { get; set; }

		public IComparable[] Columns { get; set; }

		public IComparable[] OriginalColumns { get; set; }
	}
	
	public interface IProvider : IDisposable
	{
		void Initialize ();

		bool GetNextRecord ();

		ProviderRecord Record { get; }

		void Uninitialize ();
	}
}

