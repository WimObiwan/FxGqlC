using System;
using System.Windows.Forms;

namespace FxGqlCWin
{
	public static class FileSelector
	{
		static FileSelector ()
		{
		}

		public static string[] SelectMultipleFileRead (string title, string currentFolder)
		{
			return Select (title, currentFolder, Mode.MultipleFileRead);
		}

		enum Mode
		{
			MultipleFileRead
		}

		private static string[] Select (string title, string currentFolder, Mode mode)
		{
			FileDialog dialog;
			switch (mode) {
			case Mode.MultipleFileRead:
				{
					OpenFileDialog openDialog = new OpenFileDialog ();
					openDialog.Multiselect = true;
					dialog = openDialog;
					break;
				}
			default:
				throw new NotSupportedException ();
			}

			dialog.Title = title;
			dialog.InitialDirectory = currentFolder;
			dialog.AddExtension = true;
			dialog.CheckFileExists = false;
			dialog.CheckPathExists = false;

			string[] filenames = null;
			if (dialog.ShowDialog () == DialogResult.OK)
				filenames = dialog.FileNames;

			return filenames;
		}
	}
}

