using System;
using Gtk;
using GtkUtil;

namespace Visor.Ui {

	class MainClass {
		
		public static void Main (string[] args)
		{
			Application.Init();
			MainWindow win = null;
			
			try {
				if ( args.Length > 0 )
					 win = new MainWindow( args[ 0 ] );
				else win = new MainWindow();
				
				win.Show();
				Application.Run();
			} catch(Exception e) {
				Util.MsgError( win, Core.AppInfo.Name, e.Message );
			}	
		}
		
	}
	
}

