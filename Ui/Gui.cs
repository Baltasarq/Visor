using System;
using System.Text;
using Gtk;
using System.Collections;
using System.Collections.Generic;
using GtkUtil;

namespace Visor.Ui {

public partial class MainWindow : Gtk.Window {
	
	public const int MinWidth = 630;
	public const int MinHeight = 400;
	public const int MaxTitleLength = 32;
	public const string CfgFileName = ".visor.cfg";
	
	public MainWindow(string fileName) : this()
	{
		this.LoadDocument( fileName );
	}
		
	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		try {
			// Prepare
			this.Build();
			this.BuildMe();
			this.SetTitle();
			this.DeactivateUI();
			this.SetStatus( "Ready" );
			this.Updating = false;
			
			fileName = System.IO.Directory.GetCurrentDirectory();
			this.ReadConfiguration();
		} finally {
			this.Show();
		}
	}

	/// <summary>
	/// Create the contextual menu and make it ready for action
	/// </summary>
	private void BuildPopup()
	{
		// Menus
		this.popup = new Menu();

		// First
		var menuItemGoFirst = new ImageMenuItem( "Go to f_irst" );
		menuItemGoFirst.Image = new Gtk.Image( Stock.GotoFirst, IconSize.Menu );
		menuItemGoFirst.Activated += delegate { this.OnGotoFirst( null, null ); };
		popup.Append( menuItemGoFirst );

		// Forward
		var menuItemGoForward = new ImageMenuItem( "Go _forward" );
		menuItemGoForward.Image = new Gtk.Image( Stock.GoForward, IconSize.Menu );
		menuItemGoForward.Activated += delegate { this.OnGoForward( null, null ); };
		popup.Append( menuItemGoForward );

		// Back
		var menuItemGoBack = new ImageMenuItem( "Go _back" );
		menuItemGoBack.Image = new Gtk.Image( Stock.GoBack, IconSize.Menu );
		menuItemGoBack.Activated += delegate { this.OnGoBackward( null, null ); };
		popup.Append( menuItemGoBack );

		// Last
		var menuItemGoLast = new ImageMenuItem( "Go to _last" );
		menuItemGoLast.Image = new Gtk.Image( Stock.GotoLast, IconSize.Menu );
		menuItemGoLast.Activated += delegate { this.OnGotoLast( null, null ); };
		popup.Append( menuItemGoLast );

		// Close
		var menuItemClose = new ImageMenuItem( "_Close" );
		menuItemClose.Image = new Gtk.Image( Stock.Close, IconSize.Menu );
		menuItemClose.Activated += delegate { this.OnClose( null, null ); };
		popup.Append( menuItemClose );

		// Finish
		popup.ShowAll();
	}

	private void BuildActions()
	{
		this.actOpen = new Gtk.Action( "open", "Open", "open file", Gtk.Stock.Open );
		this.actOpen.Activated += (sender, e) => this.OnOpen( sender, e );

		this.actClose = new Gtk.Action( "close", "Close", "close file", Gtk.Stock.Close );
		this.actClose.Activated += (sender, e) => this.OnClose( sender, e );

		this.actQuit = new Gtk.Action( "quit", "Quit", "quit app", Gtk.Stock.Quit );
		this.actQuit.Activated += (sender, e) => this.OnQuit( sender ,e );

		this.actViewToolbar = new Gtk.Action( "view-toolbar", "View toolbar", "view toolbar", null );
		this.actViewToolbar.Activated += (sender, e) => this.OnViewToolbar( sender, e );

		this.actForward = new Gtk.Action( "forward", "Go forward", "go forward", Gtk.Stock.GoForward );
		this.actForward.Activated += (sender, e) => this.OnGoForward( sender, e );

		this.actBack = new Gtk.Action( "forward", "Go forward", "go forward", Gtk.Stock.GoBack );
		this.actBack.Activated += (sender, e) => this.OnGoBackward( sender, e );

		this.actRewind = new Gtk.Action( "rewind", "Go to first", "go to first", Gtk.Stock.GotoFirst );
		this.actRewind.Activated += (sender, e) => this.OnGotoFirst( sender, e );

		this.actGoToEnd = new Gtk.Action( "go-to-end", "Go to last", "go to last", Gtk.Stock.GotoLast );
		this.actGoToEnd.Activated += (sender, e) => this.OnGotoLast( sender, e );

		this.actGoTo = new Gtk.Action( "go-to", "Go to...", "go to...", Gtk.Stock.Edit );
		this.actGoTo.Activated += (sender, e) => this.OnGoto( sender, e );

		this.actFind = new Gtk.Action( "find", "Find", "find", Gtk.Stock.Find );
		this.actFind.Activated += (sender, e) => this.OnFind( sender, e );

		this.actAbout = new Gtk.Action( "about", "About", "about...", Gtk.Stock.About );
		this.actAbout.Activated += (sender, e) => this.OnAbout( sender ,e );
	}

	private void BuildFileChunkViewer()
	{
		this.tvFile.Hide();

		try {
			// Create liststore (16 columns + index column + text column)
			var types = new System.Type[ Core.Document.Columns + 2 ];

			for(int typeNumber = 0; typeNumber < types.Length; ++typeNumber) {
				types[ typeNumber ] = typeof( string );
			}
			Gtk.ListStore listStore = new Gtk.ListStore( types );
			this.tvFile.Model = listStore;

			// Delete existing columns
			while( this.tvFile.Columns.Length > 0 ) {
				this.tvFile.RemoveColumn( this.tvFile.Columns[ 0 ] );
			}

			// Create indexes column
			var column = new Gtk.TreeViewColumn();
			var cell = new Gtk.CellRendererText();
			column.Title = "#";
			column.PackStart( cell, true );
			cell.Editable = false;
			cell.Foreground = "black";
			cell.Background = "light gray";
			column.AddAttribute( cell, "text", 0 );
			this.tvFile.AppendColumn( column );

			// Create columns belonging to the document
			for(int columnNumber = 0; columnNumber < Core.Document.Columns; ++columnNumber) {
				column = new Gtk.TreeViewColumn();
				cell = new Gtk.CellRendererText();
				column.Title = columnNumber.ToString( "X" );
				column.PackStart( cell, true );
				cell.Editable = false;
				column.AddAttribute( cell, "text", columnNumber + 1 );
				this.tvFile.AppendColumn( column );
			}

			// Create text representation column
			column = new Gtk.TreeViewColumn();
			cell = new Gtk.CellRendererText();
			column.Title = "txt";
			column.PackStart( cell, true );
			cell.Editable = false;
			cell.Foreground = "black";
			cell.Background = "gray";
			column.AddAttribute( cell, "text", Core.Document.Columns +1 );
			this.tvFile.AppendColumn( column );
		} catch(Exception e) {
			Util.MsgError( null, Core.AppInfo.Name, "Error building view: '" + e.Message + '\'' );
		}

		this.tvFile.EnableGridLines = TreeViewGridLines.Both;
		this.tvFile.SetCursor(
			new TreePath( new int[]{ 0 } ),
			this.tvFile.Columns[ 0 ],
			false
		);
		this.tvFile.Show();
	}

	private void BuildMenu()
	{
		var accelGroup = new Gtk.AccelGroup();

		// File
		this.miFile = new Gtk.MenuItem( "_File" );
		this.mFile = new Gtk.Menu();
		this.miFile.Submenu = this.mFile;

		var opOpen = this.actOpen.CreateMenuItem();
		opOpen.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.o, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		var opQuit = this.actQuit.CreateMenuItem();
		opQuit.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.q, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		miFile.Add( opOpen );
		miFile.Add( this.actClose.CreateMenuItem() );
		miFile.Add( new Gtk.SeparatorMenuItem() );
		miFile.Add( opQuit );

		// View
		this.miView = new Gtk.MenuItem( "_View" );
		this.mView = new Gtk.Menu();
		this.miView.Submenu = this.mView;

		var opViewToolbar = this.actViewToolbar.CreateMenuItem();

		var opForward = this.actForward.CreateMenuItem();
		opForward.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
				Gdk.Key.Page_Down, Gdk.ModifierType.None, Gtk.AccelFlags.Visible) );

		var opBackward = this.actBack.CreateMenuItem();
		opForward.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.Page_Up, Gdk.ModifierType.None, Gtk.AccelFlags.Visible) );

		var opRewind = this.actRewind.CreateMenuItem();
		opRewind.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.Page_Up, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		var opGotoLast = this.actGoToEnd.CreateMenuItem();
		opGotoLast.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.Page_Down, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		var opGo = this.actGoTo.CreateMenuItem();
		opGo.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.g, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		var opFind = this.actFind.CreateMenuItem();
		opFind.AddAccelerator( "activate", accelGroup, new Gtk.AccelKey(
			Gdk.Key.f, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible) );

		this.mView.Add( opViewToolbar );
		this.mView.Add( opForward );
		this.mView.Add( opBackward );
		this.mView.Add( opRewind );
		this.mView.Add( opGotoLast );
		this.mView.Add( opGo );
		this.mView.Add( new Gtk.SeparatorMenuItem() );
		this.mView.Add( opFind );

		// Help
		this.miHelp = new Gtk.MenuItem( "_Help" );
		this.mHelp = new Gtk.Menu();
		this.miHelp.Submenu = this.mHelp;
		this.mHelp.Add( this.actAbout.CreateMenuItem() );

		// Menu bar
		this.mbMenuBar = new Gtk.MenuBar();
		this.mbMenuBar.Add( miFile );
		this.mbMenuBar.Add( miView );
		this.mbMenuBar.Add( miHelp );
		this.AddAccelGroup( accelGroup );
	}

	private void BuildMe()
	{
		this.BuildActions();
		this.BuildMenu();
		this.BuildPopup();
		this.BuildFileChunkViewer();

		this.SetGeometryHints(
			this,
			new Gdk.Geometry() {
				MinWidth = MainWindow.MinHeight,
				MinHeight = MainWindow.MinWidth
			},
			Gdk.WindowHints.MinSize
		);
		this.SetDefaultSize( MainWindow.MinHeight, MainWindow.MinWidth );
		this.ShowAll();

		// Set fonts
		this.txtFile.ModifyFont( Pango.FontDescription.FromString("Courier 10") );
		this.tvFile.ModifyFont( Pango.FontDescription.FromString("Courier 10") );

		// Events
		this.ButtonReleaseEvent += OnClick;
	}
		
	private void SetTitle()
	{
		this.SetTitle( "" );
	}
		
	private void SetTitle(string fileName)
	{
		string title = Core.AppInfo.Name;
			
		if ( fileName.Length > 0 ) {
			title = fileName;
			
			if ( fileName.Length > MaxTitleLength ) {
				title = "..." + fileName.Substring( fileName.Length - MaxTitleLength, MaxTitleLength );
			}

			title += " - " + Core.AppInfo.Name;
		}

		this.Title = title;
	}
		
	private void OnClick(object obj, Gtk.ButtonReleaseEventArgs args)
	{
		if ( args.Event.Button == 3 ) {
			this.popup.Popup();
		}
	}

	private void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Quit();
		a.RetVal = true;
	}
	
	private void Quit()
	{
		this.WriteConfiguration();
		Application.Quit();
	}

	public void DeactivateUI()
	{
		this.PrepareIde( false );
	}
		
	public void ActivateUI()
	{
		this.PrepareIde( true );
	}
		
	public void PrepareIde(bool active)
	{
		// UI parts
		this.frFile1.Visible = active;
		this.lblPosition.Visible = active;
		this.nbView.Page = 0;
		this.hbIndicators.Visible = active;
		this.hbPanelGo.Hide();
			
		// Actions
		this.closeAction.Sensitive = active;
		this.goForwardAction.Sensitive = active;
		this.goBackAction.Sensitive = active;
		this.gotoFirstAction.Sensitive = active;
		this.gotoLastAction.Sensitive = active;
		this.positionAction.Sensitive = active;
		this.findAction.Sensitive = active;
	}
		
	public static void PrepareFileTxtView(Gtk.TreeView tvTable)
	{
		try {
			Gtk.ListStore listStore = new Gtk.ListStore( new System.Type[]{ typeof(string) } );
			tvTable.Model = listStore;
				
			// Delete existing columns
			while( tvTable.Columns.Length > 0 ) {
				tvTable.RemoveColumn( tvTable.Columns[ 0 ] );
			}
				
			// Create data column
			var column = new Gtk.TreeViewColumn();
			var cell = new Gtk.CellRendererText();
			column.Title = "txt";
			column.PackStart( cell, true );
			cell.Editable = false;
			cell.Foreground = "black";
			cell.Background = "white";
			column.AddAttribute( cell, "text", 0 );
	 		tvTable.AppendColumn( column );
		} catch(Exception e) {
			Util.MsgError( null, Core.AppInfo.Name, "Error building view: '" + e.Message + '\'' );
		}
		
		tvTable.EnableGridLines = TreeViewGridLines.Horizontal;
		tvTable.SetCursor(
				new TreePath( new int[]{ 0 } ),
				tvTable.Columns[ 0 ],
				false
		);
		tvTable.Show();
	}
		
	/// <summary>
	///  Completely updates the view of the document on window
	/// </summary>
	private void ShowData()
	{
		// Avoid race conditions
		while( this.Updating );
		
		this.Updating = true;
		this.SetStatus( "Showing..." );
			
		this.ShowBinData();
		this.ShowTextualData();
		this.UpdateView();
			
		this.SetStatus();
		this.Updating = false;
	}
	
	/// <summary>
	/// Updates the info of position and other displayed
	/// </summary>
	private void UpdateView()
	{
		// Position labels
		this.lblPosition.Text = " | "
			+ ( this.Document.FrameBufferNumber +1 ).ToString( "D5" )
			+ " / "
			+ this.Document.TotalFrameBuffersNumber.ToString( "D5" )
		;
			
		// Sidebar
		this.scrlSideBar.Value = this.Document.Position;
		this.scrlSideBar.TooltipText = this.Document.Position.ToString();
	}
		
	/// <summary>
	/// Updates the hex info on window
	/// </summary>
	private void ShowBinData()
	{
		Gtk.TreeView tvTable = this.tvFile;
		
		// Clear
		( (Gtk.ListStore) tvTable.Model ).Clear();

		// Insert data
		var row = new List<string>();
		for (int i = 0; i < this.Document.Data.Length; ++i) {
			// Row data
			row.Clear();
			row.AddRange( this.Document.Data[ i ] );
				
			// Index data
			long pos = this.Document.Position + ( i * Core.Document.Columns );
			row.Insert( 0, pos.ToString( "x6" ) );
				
			// Textual data
			var colBuilder = new StringBuilder();
			colBuilder.Capacity = Core.Document.Columns;
			int dataPos = i * Core.Document.Columns;
			for(int j = dataPos; j < ( dataPos + Core.Document.Columns); ++j) {
				char ch = (char) this.Document.RawData[ j ];
				if ( Core.Document.IsStrictlyPrintable( ch ) ) {
						colBuilder.Append( ch );
				} else 	colBuilder.Append( '.' );
			}
			row.Add( colBuilder.ToString() );
			

			( (Gtk.ListStore) tvTable.Model ).AppendValues( row.ToArray() );
		}
		
		frFrame1Label.Markup = "<b>" + System.IO.Path.GetFileName( this.Document.Path ) + "</b>";
	}

	/// <summary>
	/// Updates the "text only" window.
	/// </summary>
	private void ShowTextualData()
	{
		StringBuilder buffer = new StringBuilder();
		buffer.EnsureCapacity( this.Document.RawData.Length );
			
		foreach( var b in this.Document.RawData) {
			if ( Core.Document.IsPrintable( (char) b ) )
					buffer.Append( (char) b );
			else  	buffer.Append( '.' );
		}

		this.txtFile.Buffer.Text = buffer.ToString();
	}
	
	/// <summary>
	/// Shows the About dialog
	/// </summary>
	private void OnAbout(object sender, System.EventArgs e)
	{
		var about = new Gtk.AboutDialog();
		String[] authors = { Core.AppInfo.Author };
		
		about.ProgramName = Core.AppInfo.Name;
		about.Version = Core.AppInfo.Version;
		about.Authors = authors;
		about.Comments = Core.AppInfo.Comments;
		about.License = Core.AppInfo.License;
		about.Copyright = "(c) " + authors[ 0 ]
			            + " " + Convert.ToString( DateTime.Now.Year )
		;
		about.Website = Core.AppInfo.Website;
		
		about.Logo = this.Icon;
		
		about.Parent = this;
		about.TransientFor = this;
		about.SetPosition( WindowPosition.CenterOnParent );
	    about.Run();
	    about.Destroy();
	}
		
	private void OnViewToolbar(object sender, System.EventArgs e)
	{
		this.tbToolbar.Visible = this.ViewToolbarAction.Active;
	}
	
	private void OnQuit(object sender, System.EventArgs e)
	{
		Quit();
	}

	private void OnOpen(object sender, System.EventArgs e)
	{
		if ( Util.DlgOpen( "Open file", Core.AppInfo.Name, this, ref fileName, "*" ) )
		{
			this.LoadDocument( fileName );
		}
	}
		
	private void LoadDocument(string fileName)
	{
		this.SetStatus( "Opening..." );
			
		// Load document
		this.document = new Core.Document( fileName );
		this.ActivateUI();
		this.ShowData();
		this.SetTitle( fileName );
			
		// Prepare
		this.PrepareSideBar();
		this.PrepareFilePositionSelector();
			
		this.SetStatus();
	}
		
	/// <summary>
	/// Configures the sidebar
	/// </summary>
	private void PrepareSideBar()
	{
		this.lblMaxPos.LabelProp = "<b>" + this.Document.FileLength.ToString() + "</b>";
		this.scrlSideBar.Adjustment.Lower = 0;
		this.scrlSideBar.Adjustment.Upper = this.Document.FileLength;
		this.scrlSideBar.Adjustment.PageIncrement = this.Document.BufferSize;
		this.scrlSideBar.Adjustment.Value = 0;
		this.scrlSideBar.TooltipText = "0";
	}
	
	/// <summary>
	/// Configures the spin button used to go to a specific position
	/// </summary>
	private void PrepareFilePositionSelector()
	{
		this.sbFilePosition.Adjustment.Upper = this.Document.FileLength;
		this.sbFilePosition.Adjustment.Lower = 0;
		this.sbFilePosition.Value = this.Document.Position;
	}
	
	/// <summary>
	/// When closing...
	/// </summary>
	private void OnClose(object sender, System.EventArgs e)
	{
		this.document = null;
		this.DeactivateUI();
	}

	/// <summary>
	/// Eliminates the last status set
	/// </summary>
	public void SetStatus()
	{
		this.stStatusBar.Pop( 0 );
		Util.UpdateUI();
	}
		
	/// <summary>
	/// Sets a new status
	/// </summary>
	/// <param name="msg">
	/// A <see cref="System.String"/>
	/// </param>
	public void SetStatus(string msg)
	{
		this.stStatusBar.Push( 0, msg );
	}
		
	/// <summary>
	/// Writes the configuration to the config file
	/// </summary>
	public void WriteConfiguration()
	{
		SetStatus( "Saving configuration..." );
		
		Util.WriteConfiguration( this, this.CfgFile, new string[]{ "" } );
		
		SetStatus();
		return;
	}
	
	/// <summary>
	/// Reads the configuration from the config file
	/// </summary>
	public void ReadConfiguration()
	{
		SetStatus( "Loading configuration..." );

		Util.ReadConfiguration( this, this.CfgFile );

		SetStatus();
	}
	
	/// <summary>
	/// Warns that there is no open document
	/// </summary>
	private void WarnNoDocument()
	{
		Util.MsgError( this, Core.AppInfo.Name, "No document" );
	}

	/// <summary>
	/// Move a page forward
	/// </summary>
	private void OnGoForward(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			this.Document.Advance();
			this.ShowData();
		}
		else this.WarnNoDocument();
	}

	/// <summary>
	/// Move a page backwards
	/// </summary>
	private void OnGoBackward(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			this.Document.Recede();
			this.ShowData();
		}
		else this.WarnNoDocument();
	}
	
	/// <summary>
	/// Go to the first page of the document
	/// </summary>
	private void OnGotoFirst(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			this.Document.Position = 0;
			this.ShowData();
		}
		else this.WarnNoDocument();
	}
	
	/// <summary>
	/// Go to the last page of the document
	/// </summary>
	/// <param name="sender">
	private void OnGotoLast(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			this.Document.FrameBufferNumber = this.Document.TotalFrameBuffersNumber -1;
			this.ShowData();
		}
		else this.WarnNoDocument();
	}

	/// <summary>
	/// Go to a specific place
	/// </summary>
	private void OnGoto(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			hbPanelGo.Show();
		} else WarnNoDocument();
	}

	/// <summary>
	/// Update the sidebar's position
	/// </summary>
	private void OnSidebarUpdated(object sender, System.EventArgs e)
	{
		if ( !this.Updating ) {
			this.Document.FrameBufferNumber = (long) this.scrlSideBar.Value / this.Document.BufferSize;
			this.ShowData();
		}
	}
	
	/// <summary>
	/// Hide the go panel
	/// </summary>
	private void OnGoPanelClose(object sender, System.EventArgs e)
	{
		this.hbPanelGo.Hide();
		this.sbFilePosition.HasFocus = true;
	}
		
	/// <summary>
	/// Hide the find panel
	/// </summary>
	private void OnFindPanelClose(object sender, System.EventArgs e)
	{
		hbPanelFind.Hide();
	}
	
	/// <summary>
	/// When the user changes the file position selector...
	/// </summary>
	private void OnFilePositionChanged(object sender, System.EventArgs e)
	{
		if ( this.Document != null ) {
			this.Document.Position = (long) this.sbFilePosition.Value;
			this.ShowData();
		} else WarnNoDocument();
	}
	
	/// <summary>
	/// When the find option is activated...
	/// </summary>
	private void OnFind(object sender, System.EventArgs e)
	{
		this.hbPanelFind.Show();
		this.edFind.HasFocus = true;
	}

	/// <summary>
	/// When the find button is clicked...
	/// </summary>
	private void OnFindClicked(object sender, System.EventArgs e)
	{
		int opt = this.cbType.Active;
		
		if ( this.Document != null ) {
			// Chose value
			if ( opt < 0
			  || opt > ( (int) Core.Document.ValueType.Dec ) )
			{
				opt = 0;
			}
				
			this.SetStatus( "Searching..." );
			long position = this.Document.Find( edFind.Text, (Core.Document.ValueType) opt );

			if ( position > -1 ) {
				this.Document.Position = position;
				this.ShowData();
			}
			else Util.MsgError( this, Core.AppInfo.Name, "Not found" );
			this.SetStatus();

		} else this.WarnNoDocument();
	}

	/// <summary>
	/// Determines whether the GUI is updating the data view
	/// </summary>
	private bool Updating {
		get; set;
	}

	public Core.Document Document {
		get { return this.document; }
	}

	/// <summary>
	/// Gets the config file's absolute path.
	/// </summary>
	/// <value>The cfg file path, as a string.</value>
	public string CfgFile
	{
		get {
			if ( cfgFile.Trim().Length == 0 ) {
				this.cfgFile = Util.GetCfgCompletePath( CfgFileName );
			}

			return this.cfgFile;
		}
	}

	private string cfgFile = "";
	private static string fileName = "";
	private Core.Document document = null;
	private Menu popup = null;

	private Gtk.Action actOpen;
	private Gtk.Action actClose;
	private Gtk.Action actQuit;
	private Gtk.Action actViewToolbar;
	private Gtk.Action actForward;
	private Gtk.Action actBack;
	private Gtk.Action actRewind;
	private Gtk.Action actGoToEnd;
	private Gtk.Action actGoTo;
	private Gtk.Action actFind;
	private Gtk.Action actAbout;

	private Gtk.MenuBar mbMenuBar;
	private Gtk.Menu mFile;
	private Gtk.Menu mView;
	private Gtk.Menu mHelp;
	private Gtk.MenuItem miFile;
	private Gtk.MenuItem miView;
	private Gtk.MenuItem miHelp;
}

}
