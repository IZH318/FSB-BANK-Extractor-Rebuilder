/**
 * @file FSB_BANK_Extractor_Rebuilder_CS_GUI.Designer.cs
 * @brief Auto-generated code for the main GUI form layout and component initialization.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file contains the automated setup for Windows Forms controls.
 * It is managed by the Visual Studio Form Designer.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-13
 */

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    partial class FSB_BANK_Extractor_Rebuilder_CS_GUI
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FSB_BANK_Extractor_Rebuilder_CS_GUI));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.extractCheckedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportCsvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.lblElapsedTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBoxTree = new System.Windows.Forms.GroupBox();
            this.lvSearchResults = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.treeViewContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.playContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.extractContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.copyNameContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyPathContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyGuidContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.treeViewInfo = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.panelSearch = new System.Windows.Forms.Panel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.labelSearch = new System.Windows.Forms.Label();
            this.groupBoxDetails = new System.Windows.Forms.GroupBox();
            this.listViewDetails = new System.Windows.Forms.ListView();
            this.colCategory = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProperty = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelPlayback = new System.Windows.Forms.Panel();
            this.cmbExtractLocation = new System.Windows.Forms.ComboBox();
            this.lblExtractLocation = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.trackSeek = new System.Windows.Forms.TrackBar();
            this.lblTime = new System.Windows.Forms.Label();
            this.lblVol = new System.Windows.Forms.Label();
            this.trackVol = new System.Windows.Forms.TrackBar();
            this.chkAutoPlay = new System.Windows.Forms.CheckBox();
            this.chkLoop = new System.Windows.Forms.CheckBox();
            this.chkVerboseLog = new System.Windows.Forms.CheckBox();
            this.labelOptions = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxTree.SuspendLayout();
            this.treeViewContextMenu.SuspendLayout();
            this.panelSearch.SuspendLayout();
            this.groupBoxDetails.SuspendLayout();
            this.panelPlayback.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackSeek)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackVol)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(784, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileToolStripMenuItem,
            this.openFolderToolStripMenuItem,
            this.toolStripSeparator2,
            this.extractCheckedToolStripMenuItem,
            this.extractAllToolStripMenuItem,
            this.exportCsvToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openFileToolStripMenuItem
            // 
            this.openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            this.openFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.openFileToolStripMenuItem.Text = "&Open File...";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.openFolderToolStripMenuItem.Text = "Open F&older...";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(253, 6);
            // 
            // extractCheckedToolStripMenuItem
            // 
            this.extractCheckedToolStripMenuItem.Name = "extractCheckedToolStripMenuItem";
            this.extractCheckedToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.extractCheckedToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.extractCheckedToolStripMenuItem.Text = "E&xtract Checked...";
            this.extractCheckedToolStripMenuItem.Click += new System.EventHandler(this.extractCheckedToolStripMenuItem_Click);
            // 
            // extractAllToolStripMenuItem
            // 
            this.extractAllToolStripMenuItem.Name = "extractAllToolStripMenuItem";
            this.extractAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.E)));
            this.extractAllToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.extractAllToolStripMenuItem.Text = "Extract &All...";
            this.extractAllToolStripMenuItem.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // exportCsvToolStripMenuItem
            // 
            this.exportCsvToolStripMenuItem.Name = "exportCsvToolStripMenuItem";
            this.exportCsvToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.C)));
            this.exportCsvToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.exportCsvToolStripMenuItem.Text = "Export List to &CSV...";
            this.exportCsvToolStripMenuItem.Click += new System.EventHandler(this.exportCsvToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(253, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllToolStripMenuItem,
            this.collapseAllToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(45, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.expandAllToolStripMenuItem.Text = "&Expand All";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.collapseAllToolStripMenuItem.Text = "&Collapse All";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressBar,
            this.lblElapsedTime});
            this.statusStrip1.Location = new System.Drawing.Point(0, 537);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(784, 24);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(561, 19);
            this.lblStatus.Spring = true;
            this.lblStatus.Text = "Ready.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 18);
            // 
            // lblElapsedTime
            // 
            this.lblElapsedTime.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.lblElapsedTime.Name = "lblElapsedTime";
            this.lblElapsedTime.Size = new System.Drawing.Size(106, 19);
            this.lblElapsedTime.Text = "Elapsed: 00:00.00";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxTree);
            this.splitContainer1.Panel1.Controls.Add(this.panelSearch);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBoxDetails);
            this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.splitContainer1.Size = new System.Drawing.Size(784, 433);
            this.splitContainer1.SplitterDistance = 239;
            this.splitContainer1.TabIndex = 2;
            // 
            // groupBoxTree
            // 
            this.groupBoxTree.Controls.Add(this.lvSearchResults);
            this.groupBoxTree.Controls.Add(this.treeViewInfo);
            this.groupBoxTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxTree.Location = new System.Drawing.Point(5, 40);
            this.groupBoxTree.Name = "groupBoxTree";
            this.groupBoxTree.Size = new System.Drawing.Size(229, 388);
            this.groupBoxTree.TabIndex = 1;
            this.groupBoxTree.TabStop = false;
            this.groupBoxTree.Text = "Structure Explorer";
            // 
            // lvSearchResults
            // 
            this.lvSearchResults.CheckBoxes = true;
            this.lvSearchResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colType,
            this.colPath});
            this.lvSearchResults.ContextMenuStrip = this.treeViewContextMenu;
            this.lvSearchResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSearchResults.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lvSearchResults.FullRowSelect = true;
            this.lvSearchResults.GridLines = true;
            this.lvSearchResults.HideSelection = false;
            this.lvSearchResults.Location = new System.Drawing.Point(3, 17);
            this.lvSearchResults.MultiSelect = false;
            this.lvSearchResults.Name = "lvSearchResults";
            this.lvSearchResults.Size = new System.Drawing.Size(223, 368);
            this.lvSearchResults.TabIndex = 1;
            this.lvSearchResults.UseCompatibleStateImageBehavior = false;
            this.lvSearchResults.View = System.Windows.Forms.View.Details;
            this.lvSearchResults.Visible = false;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 120;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = 50;
            // 
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 100;
            // 
            // treeViewContextMenu
            // 
            this.treeViewContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.playContextMenuItem,
            this.stopContextMenuItem,
            this.toolStripSeparator3,
            this.extractContextMenuItem,
            this.toolStripSeparator4,
            this.copyNameContextMenuItem,
            this.copyPathContextMenuItem,
            this.copyGuidContextMenuItem});
            this.treeViewContextMenu.Name = "treeViewContextMenu";
            this.treeViewContextMenu.Size = new System.Drawing.Size(173, 148);
            // 
            // playContextMenuItem
            // 
            this.playContextMenuItem.Name = "playContextMenuItem";
            this.playContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.playContextMenuItem.Text = "Play / Pause";
            this.playContextMenuItem.Click += new System.EventHandler(this.playContextMenuItem_Click);
            // 
            // stopContextMenuItem
            // 
            this.stopContextMenuItem.Name = "stopContextMenuItem";
            this.stopContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.stopContextMenuItem.Text = "Stop";
            this.stopContextMenuItem.Click += new System.EventHandler(this.stopContextMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(169, 6);
            // 
            // extractContextMenuItem
            // 
            this.extractContextMenuItem.Name = "extractContextMenuItem";
            this.extractContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.extractContextMenuItem.Text = "Extract This Item...";
            this.extractContextMenuItem.Click += new System.EventHandler(this.extractContextMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(169, 6);
            // 
            // copyNameContextMenuItem
            // 
            this.copyNameContextMenuItem.Name = "copyNameContextMenuItem";
            this.copyNameContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.copyNameContextMenuItem.Text = "Copy Name";
            this.copyNameContextMenuItem.Click += new System.EventHandler(this.copyNameContextMenuItem_Click);
            // 
            // copyPathContextMenuItem
            // 
            this.copyPathContextMenuItem.Name = "copyPathContextMenuItem";
            this.copyPathContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.copyPathContextMenuItem.Text = "Copy Path";
            this.copyPathContextMenuItem.Click += new System.EventHandler(this.copyPathContextMenuItem_Click);
            // 
            // copyGuidContextMenuItem
            // 
            this.copyGuidContextMenuItem.Name = "copyGuidContextMenuItem";
            this.copyGuidContextMenuItem.Size = new System.Drawing.Size(172, 22);
            this.copyGuidContextMenuItem.Text = "Copy GUID";
            this.copyGuidContextMenuItem.Click += new System.EventHandler(this.copyGuidContextMenuItem_Click);
            // 
            // treeViewInfo
            // 
            this.treeViewInfo.CheckBoxes = true;
            this.treeViewInfo.ContextMenuStrip = this.treeViewContextMenu;
            this.treeViewInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewInfo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.treeViewInfo.ImageIndex = 0;
            this.treeViewInfo.ImageList = this.imageList1;
            this.treeViewInfo.Location = new System.Drawing.Point(3, 17);
            this.treeViewInfo.Name = "treeViewInfo";
            this.treeViewInfo.SelectedImageIndex = 0;
            this.treeViewInfo.Size = new System.Drawing.Size(223, 368);
            this.treeViewInfo.TabIndex = 0;
            this.treeViewInfo.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewInfo_AfterSelect);
            this.treeViewInfo.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewInfo_NodeMouseClick);
            this.treeViewInfo.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewInfo_NodeMouseDoubleClick);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // panelSearch
            // 
            this.panelSearch.Controls.Add(this.txtSearch);
            this.panelSearch.Controls.Add(this.labelSearch);
            this.panelSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSearch.Location = new System.Drawing.Point(5, 5);
            this.panelSearch.Name = "panelSearch";
            this.panelSearch.Size = new System.Drawing.Size(229, 35);
            this.panelSearch.TabIndex = 0;
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(60, 7);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(164, 21);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // labelSearch
            // 
            this.labelSearch.AutoSize = true;
            this.labelSearch.Location = new System.Drawing.Point(8, 11);
            this.labelSearch.Name = "labelSearch";
            this.labelSearch.Size = new System.Drawing.Size(49, 12);
            this.labelSearch.TabIndex = 0;
            this.labelSearch.Text = "Search:";
            // 
            // groupBoxDetails
            // 
            this.groupBoxDetails.Controls.Add(this.listViewDetails);
            this.groupBoxDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxDetails.Location = new System.Drawing.Point(5, 5);
            this.groupBoxDetails.Name = "groupBoxDetails";
            this.groupBoxDetails.Size = new System.Drawing.Size(531, 423);
            this.groupBoxDetails.TabIndex = 0;
            this.groupBoxDetails.TabStop = false;
            this.groupBoxDetails.Text = "Details";
            // 
            // listViewDetails
            // 
            this.listViewDetails.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colCategory,
            this.colProperty,
            this.colValue});
            this.listViewDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewDetails.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.listViewDetails.FullRowSelect = true;
            this.listViewDetails.GridLines = true;
            this.listViewDetails.HideSelection = false;
            this.listViewDetails.Location = new System.Drawing.Point(3, 17);
            this.listViewDetails.Name = "listViewDetails";
            this.listViewDetails.Size = new System.Drawing.Size(525, 403);
            this.listViewDetails.TabIndex = 0;
            this.listViewDetails.UseCompatibleStateImageBehavior = false;
            this.listViewDetails.View = System.Windows.Forms.View.Details;
            // 
            // colCategory
            // 
            this.colCategory.Text = "Category";
            this.colCategory.Width = 120;
            // 
            // colProperty
            // 
            this.colProperty.Text = "Property";
            this.colProperty.Width = 200;
            // 
            // colValue
            // 
            this.colValue.Text = "Value";
            this.colValue.Width = 300;
            // 
            // panelPlayback
            // 
            this.panelPlayback.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelPlayback.Controls.Add(this.cmbExtractLocation);
            this.panelPlayback.Controls.Add(this.lblExtractLocation);
            this.panelPlayback.Controls.Add(this.btnPlay);
            this.panelPlayback.Controls.Add(this.btnStop);
            this.panelPlayback.Controls.Add(this.trackSeek);
            this.panelPlayback.Controls.Add(this.lblTime);
            this.panelPlayback.Controls.Add(this.lblVol);
            this.panelPlayback.Controls.Add(this.trackVol);
            this.panelPlayback.Controls.Add(this.chkAutoPlay);
            this.panelPlayback.Controls.Add(this.chkLoop);
            this.panelPlayback.Controls.Add(this.chkVerboseLog);
            this.panelPlayback.Controls.Add(this.labelOptions);
            this.panelPlayback.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelPlayback.Location = new System.Drawing.Point(0, 457);
            this.panelPlayback.Name = "panelPlayback";
            this.panelPlayback.Size = new System.Drawing.Size(784, 80);
            this.panelPlayback.TabIndex = 3;
            // 
            // cmbExtractLocation
            // 
            this.cmbExtractLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbExtractLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExtractLocation.FormattingEnabled = true;
            this.cmbExtractLocation.Location = new System.Drawing.Point(622, 50);
            this.cmbExtractLocation.Name = "cmbExtractLocation";
            this.cmbExtractLocation.Size = new System.Drawing.Size(150, 20);
            this.cmbExtractLocation.TabIndex = 11;
            // 
            // lblExtractLocation
            // 
            this.lblExtractLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblExtractLocation.AutoSize = true;
            this.lblExtractLocation.Location = new System.Drawing.Point(549, 54);
            this.lblExtractLocation.Name = "lblExtractLocation";
            this.lblExtractLocation.Size = new System.Drawing.Size(67, 12);
            this.lblExtractLocation.TabIndex = 10;
            this.lblExtractLocation.Text = "Extract To:";
            this.lblExtractLocation.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(12, 12);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(75, 30);
            this.btnPlay.TabIndex = 0;
            this.btnPlay.Text = "Play (▶)";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(93, 12);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 30);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop (■)";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // trackSeek
            // 
            this.trackSeek.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackSeek.AutoSize = false;
            this.trackSeek.Location = new System.Drawing.Point(174, 12);
            this.trackSeek.Maximum = 1000;
            this.trackSeek.Name = "trackSeek";
            this.trackSeek.Size = new System.Drawing.Size(260, 30);
            this.trackSeek.TabIndex = 2;
            this.trackSeek.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackSeek.MouseDown += new System.Windows.Forms.MouseEventHandler(this.trackSeek_MouseDown);
            this.trackSeek.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trackSeek_MouseUp);
            // 
            // lblTime
            // 
            this.lblTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTime.AutoSize = true;
            this.lblTime.Font = new System.Drawing.Font("굴림", 9.75F);
            this.lblTime.Location = new System.Drawing.Point(440, 17);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(135, 13);
            this.lblTime.TabIndex = 3;
            this.lblTime.Text = "00:00.000 / 00:00.000";
            // 
            // lblVol
            // 
            this.lblVol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVol.AutoSize = true;
            this.lblVol.Location = new System.Drawing.Point(580, 18);
            this.lblVol.Name = "lblVol";
            this.lblVol.Size = new System.Drawing.Size(78, 12);
            this.lblVol.TabIndex = 4;
            this.lblVol.Text = "Volume: 70%";
            // 
            // trackVol
            // 
            this.trackVol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.trackVol.AutoSize = false;
            this.trackVol.Location = new System.Drawing.Point(660, 12);
            this.trackVol.Maximum = 100;
            this.trackVol.Name = "trackVol";
            this.trackVol.Size = new System.Drawing.Size(110, 30);
            this.trackVol.TabIndex = 5;
            this.trackVol.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackVol.Value = 70;
            this.trackVol.Scroll += new System.EventHandler(this.trackVol_Scroll);
            // 
            // chkAutoPlay
            // 
            this.chkAutoPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoPlay.AutoSize = true;
            this.chkAutoPlay.Checked = true;
            this.chkAutoPlay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoPlay.Location = new System.Drawing.Point(80, 53);
            this.chkAutoPlay.Name = "chkAutoPlay";
            this.chkAutoPlay.Size = new System.Drawing.Size(137, 16);
            this.chkAutoPlay.TabIndex = 7;
            this.chkAutoPlay.Text = "Auto-Play on Select";
            this.chkAutoPlay.UseVisualStyleBackColor = true;
            // 
            // chkLoop
            // 
            this.chkLoop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkLoop.AutoSize = true;
            this.chkLoop.Location = new System.Drawing.Point(223, 53);
            this.chkLoop.Name = "chkLoop";
            this.chkLoop.Size = new System.Drawing.Size(88, 16);
            this.chkLoop.TabIndex = 8;
            this.chkLoop.Text = "Force Loop";
            this.chkLoop.UseVisualStyleBackColor = true;
            // 
            // chkVerboseLog
            // 
            this.chkVerboseLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkVerboseLog.AutoSize = true;
            this.chkVerboseLog.Location = new System.Drawing.Point(317, 53);
            this.chkVerboseLog.Name = "chkVerboseLog";
            this.chkVerboseLog.Size = new System.Drawing.Size(128, 16);
            this.chkVerboseLog.TabIndex = 9;
            this.chkVerboseLog.Text = "Verbose Log Save";
            this.chkVerboseLog.UseVisualStyleBackColor = true;
            // 
            // labelOptions
            // 
            this.labelOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptions.AutoSize = true;
            this.labelOptions.Font = new System.Drawing.Font("굴림", 9F);
            this.labelOptions.Location = new System.Drawing.Point(13, 54);
            this.labelOptions.Name = "labelOptions";
            this.labelOptions.Size = new System.Drawing.Size(52, 12);
            this.labelOptions.TabIndex = 6;
            this.labelOptions.Text = "Options:";
            // 
            // FSB_BANK_Extractor_Rebuilder_CS_GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panelPlayback);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(750, 480);
            this.Name = "FSB_BANK_Extractor_Rebuilder_CS_GUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FSB/BANK Extractor & Rebuilder (GUI)";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBoxTree.ResumeLayout(false);
            this.treeViewContextMenu.ResumeLayout(false);
            this.panelSearch.ResumeLayout(false);
            this.panelSearch.PerformLayout();
            this.groupBoxDetails.ResumeLayout(false);
            this.panelPlayback.ResumeLayout(false);
            this.panelPlayback.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackSeek)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackVol)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportCsvToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractCheckedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripStatusLabel lblElapsedTime;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBoxTree;
        private System.Windows.Forms.TreeView treeViewInfo;
        private System.Windows.Forms.ListView lvSearchResults;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.GroupBox groupBoxDetails;
        private System.Windows.Forms.ListView listViewDetails;
        private System.Windows.Forms.ColumnHeader colCategory;
        private System.Windows.Forms.ColumnHeader colProperty;
        private System.Windows.Forms.ColumnHeader colValue;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Panel panelPlayback;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.CheckBox chkAutoPlay;
        private System.Windows.Forms.TrackBar trackVol;
        private System.Windows.Forms.Label lblVol;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label labelSearch;
        private System.Windows.Forms.TrackBar trackSeek;
        private System.Windows.Forms.CheckBox chkLoop;
        private System.Windows.Forms.CheckBox chkVerboseLog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAllToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip treeViewContextMenu;
        private System.Windows.Forms.ToolStripMenuItem playContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopContextMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem extractContextMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem copyNameContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyPathContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyGuidContextMenuItem;
        private System.Windows.Forms.Label labelOptions;
        private System.Windows.Forms.ComboBox cmbExtractLocation;
        private System.Windows.Forms.Label lblExtractLocation;
    }
}