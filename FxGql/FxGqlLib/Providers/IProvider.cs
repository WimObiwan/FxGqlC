using System;

namespace FxGqlLib
{
	public class ProviderRecord
	{
		IProvider provider;

		public ProviderRecord (IProvider provider)
		{
			this.provider = provider;
			
			int columnCount = provider.GetColumnNames ().Length;
			if (columnCount != provider.GetColumnTypes ().Length)
				throw new InvalidOperationException ("Inconsistent column names/types");
		}

		public ProviderRecord (IProvider provider, bool sameOriginalColumns)
			: this (provider)
		{
			int columnCount = provider.GetColumnNames ().Length;
			this.Columns = new IData[columnCount];
			this.NewColumns = new NewData[columnCount];
			Type[] types = provider.GetColumnTypes ();
			for (int i = 0; i < columnCount; i++) {
				this.NewColumns [i].Type = ExpressionBridge.GetNewType (types [i]);
			}

			if (sameOriginalColumns) {
				this.OriginalColumns = this.Columns;
				this.NewOriginalColumns = this.NewColumns;
			} else {
				this.OriginalColumns = new IData[columnCount];
				this.NewOriginalColumns = new NewData[columnCount];
				for (int i = 0; i < columnCount; i++) {
					this.NewOriginalColumns [i].Type = ExpressionBridge.GetNewType (types [i]);
				}
			}
		}
		
		public ColumnName[] ColumnTitles { get { return provider.GetColumnNames (); } }

		public string Source { get; set; }

		public long LineNo { get; set; }

		public long TotalLineNo { get; set; }

		public IData[] Columns { get; set; }
		public NewData[] NewColumns { get; set; }

		public IData[] OriginalColumns { get; set; }
		public NewData[] NewOriginalColumns { get; set; }

		public string GetLine (bool useOriginalColumns)
		{
			// TODO: optimize...
			NewData[] columns;
			if (useOriginalColumns)
				columns = NewOriginalColumns;
			else
				columns = NewColumns;
				
			string column = "";
			for (int i = 0; i < columns.Length; i++) {
				if (i == 0)
					column = columns [i].ToString ();
				else
					column += '\t' + columns [i].ToString ();
			}
				
			return column;
		}
	}

	public class ColumnName : IComparable<ColumnName>
	{
		public ColumnName (string alias, string name)
		{
			Alias = alias;
			Name = name;
		}

		public ColumnName (string name)
			: this(null, name)
		{
		}

		public ColumnName (ColumnName columnName)
			: this(columnName != null ? columnName.Alias : null, 
			       columnName != null ? columnName.Name : null)
		{
		}

		public ColumnName (int ordinal)
			: this(null, string.Format("Column{0}", ordinal + 1))
		{
		}

		public string Alias { get; private set; }
		public string Name { get; private set; }

		public override string ToString ()
		{
			if (Alias != null)
				return string.Format ("[{0}].[{1}]", Alias, Name);
			else
				return string.Format ("[{0}]", Name);
		}

		public string ToStringWithoutBrackets ()
		{
			if (Alias != null)
				return string.Format ("{0}.{1}", Alias, Name);
			else
				return string.Format ("{0}", Name);
		}

		#region IComparable implementation
		public int CompareTo (ColumnName other)
		{
			int result;
			if (Alias != null && other.Alias != null)
				result = string.Compare (Alias, other.Alias, StringComparison.InvariantCultureIgnoreCase);
			else
				result = 0;

			if (result == 0)
				result = string.Compare (Name, other.Name, StringComparison.InvariantCultureIgnoreCase);

			return result;
		}
		#endregion


	}
	
	public interface IProvider : IDisposable
	{
		string[] GetAliases ();

		ColumnName[] GetColumnNames ();

		int GetColumnOrdinal (ColumnName columnName);

		Type[] GetColumnTypes ();

		void Initialize (GqlQueryState gqlQueryState);

		bool GetNextRecord ();

		ProviderRecord Record { get; }

		void Uninitialize ();
	}
}

