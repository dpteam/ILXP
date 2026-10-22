using System;
using System.Drawing;
using System.Windows.Forms;

namespace ILXP
{
    partial class MainForm
    {
        private MenuStrip mainMenu;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem openMenuItem;
        private ToolStripSeparator fileSeparator;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem openFolderMenuItem;
        private ToolStripMenuItem recursiveFoldersMenuItem;
        private ToolStripMenuItem exportIlMenuItem;
        private ToolStripMenuItem assembleIlMenuItem;
        private ToolStripMenuItem cleanAssemblyMenuItem;
        private ToolStripMenuItem editMenu;
        private ToolStripMenuItem quickOpenMenuItem;
        private ToolStripMenuItem goToIlMenuItem;
        private ToolStripMenuItem searchIlMenuItem;
        private ToolStripMenuItem copyAsMenuItem;
        private ToolStripMenuItem healthMenuItem;
        private ToolStripMenuItem compareMenuItem;
        private ToolStripMenuItem callGraphMenuItem;
        private ToolStripMenuItem bundleMenuItem;
        private ToolStripMenuItem findMenuItem;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem outputMenu;
        private ToolStripMenuItem expandAllMenuItem;
        private ToolStripMenuItem collapseAllMenuItem;
        private ToolStripMenuItem expandDepthMenuItem;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripTextBox searchBox;
        private ToolStripMenuItem viewMenu;
        private ToolStripMenuItem refreshMenuItem;
        private ToolStripSeparator viewSeparator;
        private ToolStripMenuItem backMenuItem;
        private ToolStripMenuItem forwardMenuItem;
        private ToolStripMenuItem helpMenu;
        private ToolStripMenuItem aboutMenuItem;
        private ToolStrip toolBar;
        private ToolStripButton openButton;
        private ToolStripButton refreshButton;
        private ToolStripSeparator toolbarSeparator1;
        private ToolStripButton backButton;
        private ToolStripButton forwardButton;
        private ToolStripSeparator toolbarSeparator2;
        private ToolStripComboBox languageCombo;
        private StatusStrip statusBar;
        private ToolStripStatusLabel statusLabel;
        private SplitContainer splitContainer;
        private TreeView treeView;
        private Panel contentPanel;
        private CodeEditor codeEditor;
        private BorderPanel imageBox;
        private TextBox textViewer;
        private ContextMenuStrip treeMenu;
        private ToolStripMenuItem contextBack;
        private ToolStripMenuItem contextForward;
        private ToolStripSeparator contextDivider1;
        private ToolStripMenuItem contextCopy;
        private ToolStripSeparator contextDivider2;
        private ToolStripMenuItem contextSaveAs;
        private ToolStripMenuItem contextClose;
        private ImageList treeImages;

        private void InitializeComponent()
        {
            this.mainMenu = new MenuStrip();
            this.fileMenu = new ToolStripMenuItem();
            this.openMenuItem = new ToolStripMenuItem();
            this.fileSeparator = new ToolStripSeparator();
            this.exitMenuItem = new ToolStripMenuItem();
            this.openFolderMenuItem = new ToolStripMenuItem();
            this.recursiveFoldersMenuItem = new ToolStripMenuItem();
            this.exportIlMenuItem = new ToolStripMenuItem();
            this.assembleIlMenuItem = new ToolStripMenuItem();
            this.cleanAssemblyMenuItem = new ToolStripMenuItem();
            this.editMenu = new ToolStripMenuItem();
            this.quickOpenMenuItem = new ToolStripMenuItem();
            this.goToIlMenuItem = new ToolStripMenuItem();
            this.searchIlMenuItem = new ToolStripMenuItem();
            this.copyAsMenuItem = new ToolStripMenuItem();
            this.healthMenuItem = new ToolStripMenuItem();
            this.compareMenuItem = new ToolStripMenuItem();
            this.callGraphMenuItem = new ToolStripMenuItem();
            this.bundleMenuItem = new ToolStripMenuItem();
            this.findMenuItem = new ToolStripMenuItem();
            this.copyMenuItem = new ToolStripMenuItem();
            this.outputMenu = new ToolStripMenuItem();
            this.expandAllMenuItem = new ToolStripMenuItem();
            this.collapseAllMenuItem = new ToolStripMenuItem();
            this.expandDepthMenuItem = new ToolStripMenuItem();
            this.settingsMenuItem = new ToolStripMenuItem();
            this.searchBox = new ToolStripTextBox();
            this.viewMenu = new ToolStripMenuItem();
            this.refreshMenuItem = new ToolStripMenuItem();
            this.viewSeparator = new ToolStripSeparator();
            this.backMenuItem = new ToolStripMenuItem();
            this.forwardMenuItem = new ToolStripMenuItem();
            this.helpMenu = new ToolStripMenuItem();
            this.aboutMenuItem = new ToolStripMenuItem();
            this.toolBar = new ToolStrip();
            this.openButton = new ToolStripButton();
            this.refreshButton = new ToolStripButton();
            this.toolbarSeparator1 = new ToolStripSeparator();
            this.backButton = new ToolStripButton();
            this.forwardButton = new ToolStripButton();
            this.toolbarSeparator2 = new ToolStripSeparator();
            this.languageCombo = new ToolStripComboBox();
            this.statusBar = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            this.splitContainer = new SplitContainer();
            this.treeView = new TreeView();
            this.contentPanel = new Panel();
            this.codeEditor = new CodeEditor();
            this.imageBox = new BorderPanel();
            this.textViewer = new TextBox();
            this.treeMenu = new ContextMenuStrip();
            this.contextBack = new ToolStripMenuItem();
            this.contextForward = new ToolStripMenuItem();
            this.contextDivider1 = new ToolStripSeparator();
            this.contextCopy = new ToolStripMenuItem();
            this.contextDivider2 = new ToolStripSeparator();
            this.contextSaveAs = new ToolStripMenuItem();
            this.contextClose = new ToolStripMenuItem();
            this.treeImages = new ImageList();

            this.mainMenu.SuspendLayout();
            this.toolBar.SuspendLayout();
            this.statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.treeMenu.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.SuspendLayout();

            // main menu
            this.mainMenu.Items.AddRange(new ToolStripItem[] { this.fileMenu, this.editMenu, this.viewMenu, this.outputMenu, this.helpMenu });
            this.mainMenu.Location = new Point(0, 0);
            this.mainMenu.Name = "MenuStrip1";
            this.mainMenu.Size = new Size(828, 24);
            this.mainMenu.TabIndex = 0;

            this.fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                this.openMenuItem,
                this.openFolderMenuItem,
                this.recursiveFoldersMenuItem,
                this.exportIlMenuItem,
                this.assembleIlMenuItem,
                this.cleanAssemblyMenuItem,
                this.compareMenuItem,
                this.bundleMenuItem,
                this.fileSeparator,
                this.exitMenuItem
            });
            this.fileMenu.Name = "FileToolStripMenuItem";
            this.fileMenu.Size = new Size(37, 20);
            this.fileMenu.Text = "&File";

            this.openMenuItem.Image = AppResources.FileOpen;
            this.openMenuItem.Name = "MenuItemFileOpen";
            this.openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            this.openMenuItem.Size = new Size(155, 22);
            this.openMenuItem.Text = "&Open...";
            this.openMenuItem.Click += this.OnOpenFile;

            this.openFolderMenuItem.Name = "MenuItemFileOpenFolder";
            this.openFolderMenuItem.Size = new Size(155, 22);
            this.openFolderMenuItem.Text = "Open F&older...";
            this.openFolderMenuItem.Click += this.OnOpenFolder;

            this.recursiveFoldersMenuItem.Name = "MenuItemFileRecursiveFolders";
            this.recursiveFoldersMenuItem.Size = new Size(155, 22);
            this.recursiveFoldersMenuItem.Text = "&Recursive folders";
            this.recursiveFoldersMenuItem.CheckOnClick = true;
            this.recursiveFoldersMenuItem.Checked = AppSettings.Default.RecursiveFolders;
            this.recursiveFoldersMenuItem.CheckedChanged += this.OnRecursiveFoldersChanged;

            this.exportIlMenuItem.Name = "MenuItemFileExportIl";
            this.exportIlMenuItem.Size = new Size(155, 22);
            this.exportIlMenuItem.Text = "Export &IL...";
            this.exportIlMenuItem.Click += this.OnExportIl;

            this.assembleIlMenuItem.Name = "MenuItemFileAssembleIl";
            this.assembleIlMenuItem.Size = new Size(155, 22);
            this.assembleIlMenuItem.Text = "&Assemble IL...";
            this.assembleIlMenuItem.Click += this.OnAssembleIl;

            this.cleanAssemblyMenuItem.Name = "MenuItemFileClean";
            this.cleanAssemblyMenuItem.Size = new Size(155, 22);
            this.cleanAssemblyMenuItem.Text = "&Clean assembly...";
            this.cleanAssemblyMenuItem.Click += this.OnCleanAssembly;

            this.fileSeparator.Name = "ToolStripMenuItem1";
            this.fileSeparator.Size = new Size(152, 6);
            this.exitMenuItem.Name = "MenuItemFileExit";
            this.exitMenuItem.Size = new Size(155, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += this.OnExit;

            this.viewMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                this.refreshMenuItem,
                this.expandAllMenuItem,
                this.expandDepthMenuItem,
                this.collapseAllMenuItem,
                this.viewSeparator,
                this.backMenuItem,
                this.forwardMenuItem,
                new ToolStripSeparator(),
                this.settingsMenuItem
            });
            this.viewMenu.DropDownOpening += this.OnViewMenuOpening;
            this.viewMenu.DropDownClosed += this.OnViewMenuClosed;
            this.viewMenu.Name = "ViewToolStripMenuItem";
            this.viewMenu.Size = new Size(44, 20);
            this.viewMenu.Text = "&View";

            this.refreshMenuItem.Image = AppResources.Refresh;
            this.refreshMenuItem.Name = "MenuItemViewRefresh";
            this.refreshMenuItem.ShortcutKeys = Keys.F5;
            this.refreshMenuItem.Size = new Size(175, 22);
            this.refreshMenuItem.Text = "&Refresh";
            this.refreshMenuItem.Click += this.OnRefreshTree;

            this.viewSeparator.Name = "ToolStripMenuItem2";
            this.viewSeparator.Size = new Size(172, 6);

            this.backMenuItem.Image = AppResources.NavBack;
            this.backMenuItem.Name = "MenuItemViewBack";
            this.backMenuItem.ShortcutKeys = Keys.Alt | Keys.Left;
            this.backMenuItem.Size = new Size(175, 22);
            this.backMenuItem.Text = "&Back";
            this.backMenuItem.Click += this.OnGoBack;

            this.forwardMenuItem.Image = AppResources.NavForward;
            this.forwardMenuItem.Name = "MenuItemViewForward";
            this.forwardMenuItem.ShortcutKeys = Keys.Alt | Keys.Right;
            this.forwardMenuItem.Size = new Size(175, 22);
            this.forwardMenuItem.Text = "&Forward";
            this.forwardMenuItem.Click += this.OnGoForward;

            this.helpMenu.DropDownItems.AddRange(new ToolStripItem[] { this.aboutMenuItem });            this.helpMenu.Name = "HelpToolStripMenuItem";
            this.helpMenu.Size = new Size(44, 20);
            this.helpMenu.Text = "&Help";

            this.aboutMenuItem.Name = "MenuItemHelpAbout";
            this.aboutMenuItem.Size = new Size(107, 22);
            this.aboutMenuItem.Text = "&About";
            this.aboutMenuItem.Click += this.OnAbout;

            // edit menu
            this.editMenu.DropDownItems.AddRange(new ToolStripItem[] { this.quickOpenMenuItem, this.goToIlMenuItem, this.searchIlMenuItem, this.findMenuItem, this.copyMenuItem, this.copyAsMenuItem, this.healthMenuItem, this.callGraphMenuItem });
            this.editMenu.Name = "EditToolStripMenuItem";
            this.editMenu.Size = new Size(39, 20);
            this.editMenu.Text = "&Edit";

            this.quickOpenMenuItem.Name = "MenuItemEditQuickOpen";
            this.quickOpenMenuItem.ShortcutKeys = Keys.Control | Keys.P;
            this.quickOpenMenuItem.Size = new Size(160, 22);
            this.quickOpenMenuItem.Text = "&Quick Open...";
            this.quickOpenMenuItem.Click += this.OnQuickOpen;

            this.goToIlMenuItem.Name = "MenuItemEditGoToIl";
            this.goToIlMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            this.goToIlMenuItem.Size = new Size(160, 22);
            this.goToIlMenuItem.Text = "Go to &IL offset...";
            this.goToIlMenuItem.Click += this.OnGoToIlOffset;

            this.searchIlMenuItem.Name = "MenuItemEditSearchIl";
            this.searchIlMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F;
            this.searchIlMenuItem.Size = new Size(160, 22);
            this.searchIlMenuItem.Text = "Search in &IL...";
            this.searchIlMenuItem.Click += this.OnSearchInIl;

            this.copyAsMenuItem.Name = "MenuItemEditCopyAs";
            this.copyAsMenuItem.Size = new Size(160, 22);
            this.copyAsMenuItem.Text = "Copy &as...";
            this.copyAsMenuItem.Click += this.OnCopyAs;

            this.healthMenuItem.Name = "MenuItemEditHealth";
            this.healthMenuItem.Size = new Size(160, 22);
            this.healthMenuItem.Text = "Assembly &health...";
            this.healthMenuItem.Click += this.OnAssemblyHealth;

            this.compareMenuItem.Name = "MenuItemFileCompare";
            this.compareMenuItem.Size = new Size(155, 22);
            this.compareMenuItem.Text = "&Compare versions...";
            this.compareMenuItem.Click += this.OnCompareAssemblies;

            this.callGraphMenuItem.Name = "MenuItemEditCallGraph";
            this.callGraphMenuItem.Size = new Size(160, 22);
            this.callGraphMenuItem.Text = "Call &graph...";
            this.callGraphMenuItem.Click += this.OnCallGraph;

            this.bundleMenuItem.Name = "MenuItemFileBundle";
            this.bundleMenuItem.Size = new Size(155, 22);
            this.bundleMenuItem.Text = "Export preservation &bundle...";
            this.bundleMenuItem.Click += this.OnExportBundle;

            this.findMenuItem.Name = "MenuItemEditFind";
            this.findMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            this.findMenuItem.Size = new Size(160, 22);
            this.findMenuItem.Text = "&Find in tree";
            this.findMenuItem.Click += this.OnFindInTree;

            this.copyMenuItem.Name = "MenuItemEditCopy";
            this.copyMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.C;
            this.copyMenuItem.Size = new Size(160, 22);
            this.copyMenuItem.Text = "&Copy name";
            this.copyMenuItem.Click += this.OnCopyNodeText;

            // output menu
            this.outputMenu.Name = "OutputToolStripMenuItem";
            this.outputMenu.Size = new Size(57, 20);
            this.outputMenu.Text = "&Output";
            this.outputMenu.DropDownOpening += this.OnOutputMenuOpening;

            this.expandAllMenuItem.Name = "MenuItemViewExpandAll";
            this.expandAllMenuItem.Size = new Size(180, 22);
            this.expandAllMenuItem.Text = "&Expand all (deep)";
            this.expandAllMenuItem.ToolTipText = "Expands every level; may take a while on large assemblies";
            this.expandAllMenuItem.Click += this.OnExpandAll;

            this.expandDepthMenuItem.Name = "MenuItemViewExpandDepth";
            this.expandDepthMenuItem.Size = new Size(180, 22);
            this.expandDepthMenuItem.Text = "Expand to &depth...";
            this.expandDepthMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            this.expandDepthMenuItem.Click += this.OnExpandToDepth;

            this.collapseAllMenuItem.Name = "MenuItemViewCollapseAll";
            this.collapseAllMenuItem.Size = new Size(180, 22);
            this.collapseAllMenuItem.Text = "&Collapse all";
            this.collapseAllMenuItem.Click += this.OnCollapseAll;

            this.settingsMenuItem.Name = "MenuItemViewSettings";
            this.settingsMenuItem.Size = new Size(180, 22);
            this.settingsMenuItem.Text = "&Settings...";
            this.settingsMenuItem.Click += this.OnOpenSettings;

            // search box in the toolbar
            searchBox.Name = "ToolStripSearchBox";
            this.searchBox.Size = new Size(160, 25);
            this.searchBox.ToolTipText = "Find in tree (Enter for next match, Ctrl+P for Quick Open)";
            this.searchBox.KeyDown += this.OnSearchBoxKeyDown;
            this.searchBox.TextChanged += this.OnSearchBoxTextChanged;

            // toolbar
            this.toolBar.Items.AddRange(new ToolStripItem[]
            {
                this.openButton,
                this.refreshButton,
                this.toolbarSeparator1,
                this.backButton,
                this.forwardButton,
                this.toolbarSeparator2,
                this.languageCombo,
                this.searchBox
            });
            this.toolBar.Location = new Point(0, 24);
            this.toolBar.Name = "ToolStrip1";
            this.toolBar.Size = new Size(828, 25);
            this.toolBar.TabIndex = 1;

            this.openButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.openButton.Image = AppResources.FileOpen;
            this.openButton.ImageTransparentColor = Color.Magenta;
            this.openButton.Name = "ToolStripButtonLoadFile";
            this.openButton.Size = new Size(23, 22);
            this.openButton.Text = "Open...";
            this.openButton.ToolTipText = "Open...\r\nShortcut Key: Ctrl+O";
            this.openButton.Click += this.OnOpenFile;

            this.refreshButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.refreshButton.Enabled = false;
            this.refreshButton.Image = AppResources.Refresh;
            this.refreshButton.ImageTransparentColor = Color.Magenta;
            this.refreshButton.Name = "ToolStripButtonRefresh";
            this.refreshButton.Size = new Size(23, 22);
            this.refreshButton.Text = "Refresh";
            this.refreshButton.ToolTipText = "Refresh\r\nShortcut Key: F5";
            this.refreshButton.Click += this.OnRefreshTree;

            this.toolbarSeparator1.Name = "ToolStripSeparator2";
            this.toolbarSeparator1.Size = new Size(6, 25);

            this.backButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.backButton.Enabled = false;
            this.backButton.Image = AppResources.NavBack;
            this.backButton.ImageTransparentColor = Color.Magenta;
            this.backButton.Name = "ToolStripButtonBack";
            this.backButton.Size = new Size(23, 22);
            this.backButton.Text = "Back";
            this.backButton.ToolTipText = "Back\r\nShortcut Key: Alt+Left";
            this.backButton.Click += this.OnGoBack;

            this.forwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.forwardButton.Enabled = false;
            this.forwardButton.Image = AppResources.NavForward;
            this.forwardButton.ImageTransparentColor = Color.Magenta;
            this.forwardButton.Name = "ToolStripButtonForward";
            this.forwardButton.Size = new Size(23, 22);
            this.forwardButton.Text = "Forward";
            this.forwardButton.ToolTipText = "Forward\r\nShortcut Key: Alt+Right";
            this.forwardButton.Click += this.OnGoForward;

            this.toolbarSeparator2.Name = "ToolStripSeparator3";
            this.toolbarSeparator2.Size = new Size(6, 25);

            this.languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.languageCombo.FlatStyle = FlatStyle.Standard;
            this.languageCombo.Name = "ToolStripComboBoxLanguage";
            this.languageCombo.Size = new Size(121, 25);
            this.languageCombo.SelectedIndexChanged += this.OnLanguageChanged;

            // status bar
            this.statusBar.Items.AddRange(new ToolStripItem[] { this.statusLabel });
            this.statusBar.Location = new Point(0, 517);
            this.statusBar.Name = "StatusStrip1";
            this.statusBar.Size = new Size(828, 22);
            this.statusBar.TabIndex = 2;

            this.statusLabel.Name = "ToolStripStatusLabel1";
            this.statusLabel.Size = new Size(813, 17);
            this.statusLabel.Spring = true;
            this.statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            // split container
            this.splitContainer.Dock = DockStyle.Fill;
            this.splitContainer.FixedPanel = FixedPanel.Panel1;
            this.splitContainer.Location = new Point(0, 49);
            this.splitContainer.Name = "SplitContainer1";
            this.splitContainer.Panel1.Controls.Add(this.treeView);
            this.splitContainer.Panel2.Controls.Add(this.contentPanel);
            this.splitContainer.Size = new Size(828, 468);
            this.splitContainer.SplitterDistance = 371;
            this.splitContainer.TabIndex = 3;

            // tree
            this.treeView.AllowDrop = true;
            this.treeView.ContextMenuStrip = this.treeMenu;
            this.treeView.Dock = DockStyle.Fill;
            this.treeView.HideSelection = false;
            this.treeView.ImageKey = "0VSObject_Assembly.bmp";
            this.treeView.ImageList = this.treeImages;
            this.treeView.Location = new Point(0, 0);
            this.treeView.Name = "TreeView1";
            this.treeView.SelectedImageKey = "0VSObject_Assembly.bmp";
            this.treeView.Size = new Size(371, 468);
            this.treeView.TabIndex = 0;
            this.treeView.DragEnter += this.OnTreeDragEnter;
            this.treeView.DragDrop += this.OnTreeDragDrop;
            this.treeView.AfterSelect += this.OnTreeAfterSelect;
            this.treeView.NodeMouseClick += this.OnTreeMouseClick;

            // tree context menu
            this.treeMenu.Items.AddRange(new ToolStripItem[]
            {
                this.contextBack,
                this.contextForward,
                this.contextDivider1,
                this.contextCopy,
                this.contextDivider2,
                this.contextSaveAs,
                this.contextClose
            });
            this.treeMenu.Name = "ContextMenuStrip1";
            this.treeMenu.Size = new Size(124, 126);
            this.treeMenu.Opening += this.OnTreeMenuOpening;

            this.contextBack.Image = AppResources.NavBack;
            this.contextBack.Name = "MenuItemContextBack";
            this.contextBack.ShortcutKeyDisplayString = "";
            this.contextBack.Size = new Size(123, 22);
            this.contextBack.Text = "Back";
            this.contextBack.Click += this.OnGoBack;

            this.contextForward.Image = AppResources.NavForward;
            this.contextForward.Name = "MenuItemContextForward";
            this.contextForward.ShortcutKeyDisplayString = "";
            this.contextForward.Size = new Size(123, 22);
            this.contextForward.Text = "Forward";
            this.contextForward.Click += this.OnGoForward;

            this.contextDivider1.Name = "MenuItemContextDivider1";
            this.contextDivider1.Size = new Size(120, 6);

            this.contextCopy.Name = "MenuItemContextCopy";
            this.contextCopy.Size = new Size(123, 22);
            this.contextCopy.Text = "Copy";
            this.contextCopy.Click += this.OnCopyNodeText;

            this.contextDivider2.Name = "MenuItemContextDivider2";
            this.contextDivider2.Size = new Size(120, 6);

            this.contextSaveAs.Name = "MenuItemContextSaveAs";
            this.contextSaveAs.Size = new Size(123, 22);
            this.contextSaveAs.Text = "Save As...";
            this.contextSaveAs.Click += this.OnSaveResourceAs;

            this.contextClose.Name = "MenuItemContextClose";
            this.contextClose.Size = new Size(123, 22);
            this.contextClose.Text = "Close";
            this.contextClose.Click += this.OnCloseAssembly;

            // content panel
            this.contentPanel.BackColor = SystemColors.Window;
            this.contentPanel.Controls.Add(this.codeEditor);
            this.contentPanel.Controls.Add(this.imageBox);
            this.contentPanel.Controls.Add(this.textViewer);
            this.contentPanel.Dock = DockStyle.Fill;
            this.contentPanel.Location = new Point(0, 0);
            this.contentPanel.Name = "Panel1";
            this.contentPanel.Padding = new Padding(1);
            this.contentPanel.Size = new Size(453, 468);
            this.contentPanel.TabIndex = 0;
            this.contentPanel.Paint += this.OnContentPanelPaint;

            this.textViewer.BackColor = SystemColors.Info;
            this.textViewer.BorderStyle = BorderStyle.None;
            this.textViewer.Dock = DockStyle.Fill;
            this.textViewer.Font = new Font("Courier New", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.textViewer.HideSelection = false;
            this.textViewer.Location = new Point(1, 1);
            this.textViewer.Multiline = true;
            this.textViewer.Name = "TextBox1";
            this.textViewer.ReadOnly = true;
            this.textViewer.ScrollBars = ScrollBars.Both;
            this.textViewer.Size = new Size(451, 466);
            this.textViewer.TabIndex = 2;
            this.textViewer.Visible = false;
            this.textViewer.WordWrap = false;

            this.codeEditor.BackColor = SystemColors.Info;
            this.codeEditor.Dock = DockStyle.Fill;
            this.codeEditor.Location = new Point(1, 1);
            this.codeEditor.Name = "SyntaxBox1";
            this.codeEditor.Size = new Size(451, 466);
            this.codeEditor.TabIndex = 1;
            this.codeEditor.TabStop = true;
            this.codeEditor.LinkClicked += this.OnEditorLinkClicked;
            this.codeEditor.SelectionChanged += this.OnEditorSelectionChanged;

            this.imageBox.BackColor = SystemColors.Info;
            this.imageBox.BackgroundImageLayout = ImageLayout.Center;
            this.imageBox.Dock = DockStyle.Fill;
            this.imageBox.Location = new Point(1, 1);
            this.imageBox.Name = "ImageBox1";
            this.imageBox.Size = new Size(451, 466);
            this.imageBox.TabIndex = 4;
            this.imageBox.Visible = false;

            // tree images (the payload is the original ImageListStreamer)
            ImageListStreamer streamer = AppResources.CreateTreeIconStreamer();
            if (streamer != null)
            {
                this.treeImages.ImageStream = streamer;
            }
            this.treeImages.TransparentColor = Color.Magenta;
            string[] imageKeys =
            {
                "0VSObject_Assembly.bmp", "1VSObject_Library.bmp", "2VSResources.bmp", "2VSResources_Open.bmp",
                "3VSProject_imagefile.bmp", "3VSProject_resourcefile.bmp", "3VSProject_xml.bmp", "4VSBaseTypes.bmp",
                "4VSReferences.bmp", "5VSProject_reference.bmp", "6VSObject_Namespace.bmp", "7VSObject_Class.bmp",
                "7VSObject_Class_Friend.bmp", "7VSObject_Class_Private.bmp", "7VSObject_Class_Protected.bmp",
                "7VSObject_Class_Sealed.bmp", "7VSObject_Delegate.bmp", "7VSObject_Delegate_Friend.bmp",
                "7VSObject_Delegate_Private.bmp", "7VSObject_Delegate_Protected.bmp", "7VSObject_Delegate_Sealed.bmp",
                "7VSObject_Enum.bmp", "7VSObject_Enum_Friend.bmp", "7VSObject_Enum_Protected.bmp",
                "7VSObject_Enum_Sealed.bmp", "7VSObject_Interface.bmp", "7VSObject_Interface_Friend.bmp",
                "7VSObject_Interface_Private.bmp", "7VSObject_Interface_Protected.bmp", "7VSObject_Interface_Sealed.bmp",
                "7VSObject_Module.bmp", "7VSObject_Module_Friend.bmp", "7VSObject_Module_Private.bmp",
                "7VSObject_Module_Protected.bmp", "7VSObject_Module_Sealed.bmp", "7VSObject_Structure.bmp",
                "7VSObject_Structure_Friend.bmp", "7VSObject_Structure_Private.bmp", "7VSObject_Structure_Protected.bmp",
                "7VSObject_Structure_Sealed.bmp", "7VSObject_ValueType.bmp", "7VSObject_ValueType_Friend.bmp",
                "7VSObject_ValueType_Private.bmp", "7VSObject_ValueType_Protected.bmp", "7VSObject_ValueType_Sealed.bmp",
                "8VSObject_EnumItem.bmp", "8VSObject_EnumItem_Friend.bmp", "8VSObject_EnumItem_Private.bmp",
                "8VSObject_EnumItem_Protected.bmp", "8VSObject_EnumItem_Sealed.bmp", "8VSObject_EnumPrivate.bmp",
                "8VSObject_Method.bmp", "8VSObject_Method_Friend.bmp", "8VSObject_Method_Private.bmp",
                "8VSObject_Method_Protected.bmp", "8VSObject_Method_Sealed.bmp", "8VSObject_MethodOverload.bmp",
                "8VSObject_MethodOverload_Friend.bmp", "8VSObject_MethodOverload_Private.bmp",
                "8VSObject_MethodOverload_Protected.bmp", "8VSObject_MethodOverload_Sealed.bmp",
                "9VSObject_Properties.bmp", "9VSObject_Properties_Friend.bmp", "9VSObject_Properties_Private.bmp",
                "9VSObject_Properties_Protected.bmp", "9VSObject_Properties_Sealed.bmp", "10VSObject_Event.bmp",
                "10VSObject_Event_Friend.bmp", "10VSObject_Event_Private.bmp", "10VSObject_Event_Protected.bmp",
                "10VSObject_Event_Sealed.bmp", "11VSObject_Constant.bmp", "11VSObject_Constant_Friend.bmp",
                "11VSObject_Constant_Private.bmp", "11VSObject_Constant_Protected.bmp", "11VSObject_Constant_Sealed.bmp",
                "11VSObject_Field.bmp", "11VSObject_Field_Friend.bmp", "11VSObject_Field_Private.bmp",
                "11VSObject_Field_Protected.bmp", "11VSObject_Field_Sealed.bmp"
            };
            for (int i = 0; i < imageKeys.Length && i < this.treeImages.Images.Count; i++)
            {
                this.treeImages.Images.SetKeyName(i, imageKeys[i]);
            }

            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(828, 539);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.toolBar);
            this.Controls.Add(this.mainMenu);
            this.Icon = AppResources.AppIcon;
            this.MainMenuStrip = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "ILXP <Version>";

            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.toolBar.ResumeLayout(false);
            this.toolBar.PerformLayout();
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.splitContainer).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.treeMenu.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.contentPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
