using System;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public class DirectoryProvider : IProvider
	{
		readonly static ColumnName[] columnNames = new ColumnName[] {
				new ColumnName ("FullName"),
				new ColumnName ("Name"),
				new ColumnName ("Extension"),
				new ColumnName ("Length"),
				new ColumnName ("CreationTime"),
				new ColumnName ("LastWriteTime"),
				new ColumnName ("LastAccessTime"),
				new ColumnName ("Attributes"),
			};
		readonly static Type[] columnTypes = new Type[] {
			typeof(DataString),
			typeof(DataString),
			typeof(DataString),
			typeof(DataInteger),
			typeof(DataDateTime),
			typeof(DataDateTime),
			typeof(DataDateTime),
			typeof(DataString),
		};
		readonly static Type[] newColumnTypes = new Type[] {
			typeof(string),
			typeof(string),
			typeof(string),
			typeof(int),
			typeof(DateTime),
			typeof(DateTime),
			typeof(DateTime),
			typeof(string),
		};
		
		readonly FileOptionsFromClause fileOptions;
		readonly StringComparer stringComparer;

		string[] files;
		ProviderRecord record;

		public DirectoryProvider (FileOptionsFromClause fileOptions, StringComparer stringComparer)
		{
			this.fileOptions = fileOptions;
			this.stringComparer = stringComparer;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			files = null;
		}
		#endregion

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return columnNames;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return Array.FindIndex (columnNames, a => a.CompareTo (columnName) == 0);
		}

		public Type[] GetColumnTypes ()
		{
			return columnTypes;
		}
		
		public Type[] GetNewColumnTypes ()
		{
			return newColumnTypes;
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			string fileName = fileOptions.FileName.Evaluate (gqlQueryState);
			string path = Path.GetDirectoryName (fileName);
			string searchPattern = Path.GetFileName (fileName);
			SearchOption searchOption;
			if (fileOptions.Recurse)
				searchOption = SearchOption.AllDirectories;
			else
				searchOption = SearchOption.TopDirectoryOnly;
			
			path = Path.Combine (gqlQueryState.CurrentDirectory, path); 
			files = Directory.GetFiles (path + Path.DirectorySeparatorChar, searchPattern, searchOption);

			if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Asc 
				|| fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.FileNameAsc)
				files = files.Select (p => new FileInfo (p)).OrderBy (p => p.Name, stringComparer).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Desc
				|| fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.FileNameDesc)
				files = files.Select (p => new FileInfo (p)).OrderByDescending (p => p.Name, stringComparer).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.ModificationTimeAsc)
				files = files.Select (p => new FileInfo (p)).OrderBy (p => p.LastWriteTime).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.ModificationTimeDesc)
				files = files.Select (p => new FileInfo (p)).OrderByDescending (p => p.LastWriteTime).Select (p => p.FullName).ToArray ();

			record = new ProviderRecord (this, true);
			record.LineNo = 0;
			record.TotalLineNo = 0;
			record.Source = "DirectoryProvider";
		}

		public bool GetNextRecord ()
		{
			if (record.LineNo >= files.Length)
				return false;
		
			FileInfo fi = new FileInfo (files [record.LineNo]);
			record.NewColumns [0].String = fi.FullName;
			record.NewColumns [1].String = fi.Name;
			record.NewColumns [2].String = fi.Extension;
			record.NewColumns [3].Integer = fi.Length;
			record.NewColumns [4].DateTime = fi.CreationTime;
			record.NewColumns [5].DateTime = fi.LastWriteTime;
			record.NewColumns [6].DateTime = fi.LastAccessTime;
			record.NewColumns [7].String = fi.Attributes.ToString ();
			record.Columns [0] = new DataString (fi.FullName);
			record.Columns [1] = new DataString (fi.Name);
			record.Columns [2] = new DataString (fi.Extension);
			record.Columns [3] = new DataInteger (fi.Length);
			record.Columns [4] = new DataDateTime (fi.CreationTime);
			record.Columns [5] = new DataDateTime (fi.LastWriteTime);
			record.Columns [6] = new DataDateTime (fi.LastAccessTime);
			record.Columns [7] = new DataString (fi.Attributes.ToString ());

			record.LineNo++;
			record.TotalLineNo = record.LineNo;

			return true;
		}

		public void Uninitialize ()
		{
			files = null;
		}

		public ProviderRecord Record {
			get {
				return record;
			}
		}
		#endregion

	}
}

