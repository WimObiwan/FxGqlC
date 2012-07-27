using System;
using Gtk;
using Gdk;

namespace FxGqlW
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ConsoleWidget : Bin, IOutputWriter
	{
		public delegate void ExecuteHandler (object sender,ExecuteArgs e);
		public event ExecuteHandler Execute;

		TextBuffer commandBuffer;
		TextTag commandTag;
		TextTag exceptionTag;
		TextBuffer outputBuffer;
		Button executeButton;
		TextView command;

		Color outputColorBackground = new Color (1, 36, 86);
		Color outputColorForeground = new Color (238, 237, 240);
		Color commandColorForeground = new Color (119, 237, 120);
		Color exceptionColorForeground = new Color (255, 0, 0);

		public ConsoleWidget ()
		{
			this.Build ();

			VBox vbox = new VBox ();
			this.Add (vbox);

			outputBuffer = new TextBuffer (null);
			commandTag = new TextTag ("command");
			commandTag.ForegroundGdk = commandColorForeground;
			commandTag.Weight = Pango.Weight.Bold;
			outputBuffer.TagTable.Add (commandTag);
			exceptionTag = new TextTag ("exception");
			exceptionTag.ForegroundGdk = exceptionColorForeground;
			exceptionTag.Weight = Pango.Weight.Ultrabold;
			outputBuffer.TagTable.Add (exceptionTag);

			TextView output = new TextView (outputBuffer);
			var font = new Pango.FontDescription ();
			font.Family = "Monospace";
			output.Editable = false;
			output.ModifyFont (font);
			output.ModifyBase (StateType.Normal, outputColorBackground);
			output.ModifyText (StateType.Normal, outputColorForeground);
			output.WrapMode = WrapMode.None;
			vbox.Add (output);
			vbox.SetChildPacking (output, true, true, 0, PackType.Start);

			HBox hbox = new HBox ();
			vbox.Add (hbox);
			vbox.SetChildPacking (hbox, false, false, 2, PackType.End);
			vbox.FocusChild = hbox;

			commandBuffer = new TextBuffer (null);
			command = new TextView (commandBuffer);
			command.ModifyFont (font);
			command.GrabFocus ();
			//commandBuffer.InsertText
			command.KeyReleaseEvent += HandleKeyReleaseEvent;
			hbox.Add (command);
			hbox.SetChildPacking (command, true, true, 2, PackType.Start);
			hbox.FocusChild = command;

			executeButton = new Button ();
			executeButton.Label = "Execute";
			executeButton.Pressed += delegate(object sender, EventArgs e) {
				OnExecute ();
			};
			hbox.Add (executeButton);
			hbox.SetChildPacking (executeButton, false, false, 2, PackType.End);
		}

		void HandleKeyReleaseEvent (object o, KeyReleaseEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return)
			if (args.Event.State == ModifierType.None)
				executeButton.Press ();
		}

		protected virtual void OnExecute ()
		{
			if (Execute != null) {
				string commandLine = commandBuffer.Text.TrimEnd ();
				commandBuffer.Clear ();
				AppendOutputBuffer (commandLine, TextType.Command);
				ExecuteArgs args = new ExecuteArgs ();
				args.Command = commandLine;
				Execute (this, args);
				command.GrabFocus ();
			}
		}

		enum TextType
		{
			Normal,
			Command,
			Exception
		}

		void AppendOutputBuffer (string text, TextType textType)
		{
			TextIter textIter = outputBuffer.EndIter;
			TextTag tag;
			if (textType == TextType.Command)
				tag = commandTag;
			else if (textType == TextType.Exception)
				tag = exceptionTag;
			else
				tag = null;

			if (tag != null)
				outputBuffer.InsertWithTags (ref textIter, text, tag);
			else
				outputBuffer.Insert (ref textIter, text);
			outputBuffer.Insert (ref textIter, Environment.NewLine);
		}

		#region IOutputWriter implementation
		public void WriteLine (string text)
		{
			AppendOutputBuffer (text, TextType.Normal);
		}

		public void WriteErrorLine (string text)
		{
			AppendOutputBuffer (text, TextType.Exception);
		}
		#endregion


	}

	public class ExecuteArgs : EventArgs
	{
		public string Command { get; set; }
	}
}

