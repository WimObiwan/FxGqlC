using System;

namespace FxGqlLib
{
	public class FileOptions
	{
		public FileOptions ()
		{
			Provider = ProviderEnum.DontCare;
		}
		
		public enum ProviderEnum
		{
			DontCare,
			File,
			Directory,
			Data,
		}
		public ProviderEnum Provider { get; set; }

		public Expression<DataString> FileName { get; set; }

		public string Client { get; set; }

		public string ConnectionString { get; set; }

		public void ValidateProviderOptions ()
		{
			switch (Provider) {
			case ProviderEnum.DontCare:
			case ProviderEnum.File:
				ValidateFileProviderOptions ();
				break;
			case ProviderEnum.Directory:
				ValidateDirectoryProviderOptions ();
				break;
			case ProviderEnum.Data:
				ValidateDataProviderOptions ();
				break;
			default:
				throw new NotSupportedException (string.Format ("Unknown FROM-clause option -Provider='{0}'", Provider));
			}
		}

		public virtual void ValidateFileProviderOptions ()
		{
			if (Client != null)
				throw new NotSupportedException ("FROM-option 'Client' is not supported when using the FileProvider");
			if (ConnectionString != null)
				throw new NotSupportedException ("FROM-option 'ConnectionString' is not supported when using the FileProvider");
		}

		public virtual void ValidateDirectoryProviderOptions ()
		{
			if (Client != null)
				throw new NotSupportedException ("FROM-option 'Client' is not supported when using the DirectoryProvider");
			if (ConnectionString != null)
				throw new NotSupportedException ("FROM-option 'ConnectionString' is not supported when using the DirectoryProvider");
		}

		public virtual void ValidateDataProviderOptions ()
		{
		}
	}

	public class FileOptionsFromClause : FileOptions
	{
		public FileOptionsFromClause ()
		{
			FileOrder = FileOrderEnum.DontCare;
		}

		public bool Recurse { get; set; }

		public string ColumnsRegex { get; set; }
		
		public GqlEngineState.HeadingEnum Heading { get; set; }
		
		public long Skip { get; set; }

		public enum FileOrderEnum
		{
			DontCare,
			Asc,
			Desc,
			FileNameAsc,
			FileNameDesc,
			ModificationTimeAsc,
			ModificationTimeDesc,
		}
		public FileOrderEnum FileOrder { get; set; }

		public enum FormatEnum
		{
			DontCare,
			PlainText,
			Csv,
		}
		public FormatEnum Format { get; set; }

		public string ColumnDelimiter { get; set; }

		public override void ValidateFileProviderOptions ()
		{
			base.ValidateFileProviderOptions ();
		}

		public override void ValidateDirectoryProviderOptions ()
		{
			base.ValidateDirectoryProviderOptions ();
			if (ColumnDelimiter != null)
				throw new NotSupportedException ("FROM-option 'ColumnDelimiter' is not supported when using the DirectoryProvider");
			if (ColumnsRegex != null)
				throw new NotSupportedException ("FROM-option 'ColumnsRegex' is not supported when using the DirectoryProvider");
			if (Heading != GqlEngineState.HeadingEnum.Off)
				throw new NotSupportedException ("FROM-option 'Heading' is not supported when using the DirectoryProvider");
			if (Skip != 0)
				throw new NotSupportedException ("FROM-option 'Skip' is not supported when using the DirectoryProvider");

		}

		public override void ValidateDataProviderOptions ()
		{
			base.ValidateDataProviderOptions ();
			if (ColumnDelimiter != null)
				throw new NotSupportedException ("FROM-option 'ColumnDelimiter' is not supported when using the DataProvider");
			if (ColumnsRegex != null)
				throw new NotSupportedException ("FROM-option 'ColumnsRegex' is not supported when using the DataProvider");
			if (Heading != GqlEngineState.HeadingEnum.Off)
				throw new NotSupportedException ("FROM-option 'Heading' is not supported when using the DataProvider");
			if (Skip != 0)
				throw new NotSupportedException ("FROM-option 'Skip' is not supported when using the DataProvider");
			if (FileOrder != FileOrderEnum.DontCare)
				throw new NotSupportedException ("FROM-option 'Skip' is not supported when using the DataProvider");
			if (Recurse != false)
				throw new NotSupportedException ("FROM-option 'Skip' is not supported when using the DataProvider");
		}
	}

	public class FileOptionsIntoClause : FileOptions
	{
		public FileOptionsIntoClause ()
		{
			NewLine = NewLineEnum.Default;
		}

		public enum NewLineEnum
		{
			Default,
			Unix,
			Dos,
			Mac
		}
		;

		public NewLineEnum NewLine  { get; set; }

		public bool Append { get; set; }

		public bool Overwrite { get; set; }

		public GqlEngineState.HeadingEnum Heading { get; set; }

		public string ColumnDelimiter { get; set; }

		public override void ValidateFileProviderOptions ()
		{
			base.ValidateFileProviderOptions ();
		}

		public override void ValidateDirectoryProviderOptions ()
		{
			base.ValidateDirectoryProviderOptions ();
			throw new NotSupportedException ("INTO-clause option -Provider does not support 'Directory' provider");
		}

		public override void ValidateDataProviderOptions ()
		{
			base.ValidateDataProviderOptions ();
			throw new NotImplementedException ("INTO-clause option -Provider does not support 'Data' provider - yet");
//			if (NewLine != NewLineEnum.Default)
//				throw new NotSupportedException ("INTO-option 'NewLine' is not supported when using the DataProvider");
//			if (Append != false)
//				throw new NotSupportedException ("INTO-option 'Append' is not supported when using the DataProvider");
//			if (Overwrite != false)
//				throw new NotSupportedException ("INTO-option 'Overwrite' is not supported when using the DataProvider");
//			if (Heading != GqlEngineState.HeadingEnum.Off)
//				throw new NotSupportedException ("INTO-option 'Heading' is not supported when using the DataProvider");
//			if (ColumnDelimiter != null)
//				throw new NotSupportedException ("INTO-option 'ColumnDelimiter' is not supported when using the DataProvider");
		}
	}	
}