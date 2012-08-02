using System;

namespace FxGqlLib
{
	public class FileOptions
	{
		public FileOptions ()
		{
			Provider = ProviderEnum.DontCare;
		}
		
		public Expression<DataString> FileName { get; set; }

		public enum ProviderEnum
		{
			DontCare,
			File,
			Directory,
		}
		public ProviderEnum Provider { get; set; }

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
			default:
				throw new NotSupportedException (string.Format ("Unknown FROM-clause option -Provider='{0}'", Provider));
			}
		}

		public virtual void ValidateFileProviderOptions ()
		{
		}
		public virtual void ValidateDirectoryProviderOptions ()
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

		public string ColumnDelimiter { get; set; }

		public override void ValidateFileProviderOptions ()
		{
		}

		public override void ValidateDirectoryProviderOptions ()
		{
			if (ColumnDelimiter != null)
				throw new NotSupportedException ("FROM-option 'ColumnDelimiter' is not supported when using the DirectoryProvider");
			if (ColumnsRegex != null)
				throw new NotSupportedException ("FROM-option 'ColumnsRegex' is not supported when using the DirectoryProvider");
			if (Heading != GqlEngineState.HeadingEnum.Off)
				throw new NotSupportedException ("FROM-option 'Heading' is not supported when using the DirectoryProvider");
			if (Skip != 0)
				throw new NotSupportedException ("FROM-option 'Skip' is not supported when using the DirectoryProvider");
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
		}

		public override void ValidateDirectoryProviderOptions ()
		{
			throw new NotSupportedException (string.Format ("INTO-clause option -Provider does not support 'Directory' provider"));
		}
	}
	
}