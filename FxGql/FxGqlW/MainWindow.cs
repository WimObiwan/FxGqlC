using System;
using Gtk;
using FxGqlW;
using FxGqlLib;

public partial class MainWindow: Gtk.Window
{
	GqlEngine gqlEngine;
	ConsoleWidget consoleWidget;

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		gqlEngine = new GqlEngine ();

		consoleWidget = new ConsoleWidget ();
		consoleWidget.Execute += HandleExecute;
		Add (consoleWidget);

		Build ();
	}

	void HandleExecute (object sender, ExecuteArgs e)
	{
		try {
			gqlEngine.OutputStream = new OutputTextWriter (consoleWidget);
			gqlEngine.Execute (e.Command);
		} catch (Exception x) {
			consoleWidget.WriteErrorLine (x.Message);
		}
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		gqlEngine.Dispose ();
		Application.Quit ();
		a.RetVal = true;
	}
}
