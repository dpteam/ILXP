using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Pdb;

[SuppressIldasm]
[DesignerGenerated]
public sealed class GForm1 : Form
{
	private IContainer icontainer_0;

	[AccessedThroughProperty("MenuStrip1")]
	private MenuStrip menuStrip_0;

	[AccessedThroughProperty("FileToolStripMenuItem")]
	private ToolStripMenuItem toolStripMenuItem_0;

	[AccessedThroughProperty("ViewToolStripMenuItem")]
	private ToolStripMenuItem toolStripMenuItem_1;

	[AccessedThroughProperty("HelpToolStripMenuItem")]
	private ToolStripMenuItem toolStripMenuItem_2;

	[AccessedThroughProperty("ToolStrip1")]
	private ToolStrip m__003C;

	[AccessedThroughProperty("ToolStripButtonLoadFile")]
	private ToolStripButton toolStripButton_0;

	[AccessedThroughProperty("ToolStripComboBoxLanguage")]
	private ToolStripComboBox toolStripComboBox_0;

	[AccessedThroughProperty("StatusStrip1")]
	private StatusStrip m__0024;

	[AccessedThroughProperty("SplitContainer1")]
	private SplitContainer splitContainer_0;

	[AccessedThroughProperty("TreeView1")]
	private TreeView treeView_0;

	[AccessedThroughProperty("Panel1")]
	private Panel panel_0;

	[AccessedThroughProperty("SyntaxBox1")]
	private GClass11 gclass11_0;

	[AccessedThroughProperty("ToolStripButtonRefresh")]
	private ToolStripButton toolStripButton_1;

	[AccessedThroughProperty("ToolStripButtonBack")]
	private ToolStripButton toolStripButton_2;

	[AccessedThroughProperty("ToolStripButtonForward")]
	private ToolStripButton toolStripButton_3;

	[AccessedThroughProperty("ImageBox1")]
	private GClass10 gclass10_0;

	[AccessedThroughProperty("TextBox1")]
	private TextBox textBox_0;

	[AccessedThroughProperty("ContextMenuStrip1")]
	private ContextMenuStrip contextMenuStrip_0;

	[AccessedThroughProperty("MenuItemContextBack")]
	private ToolStripMenuItem toolStripMenuItem_3;

	[AccessedThroughProperty("MenuItemContextForward")]
	private ToolStripMenuItem toolStripMenuItem_4;

	[AccessedThroughProperty("MenuItemContextDivider1")]
	private ToolStripSeparator toolStripSeparator_0;

	[AccessedThroughProperty("MenuItemContextCopy")]
	private ToolStripMenuItem toolStripMenuItem_5;

	[AccessedThroughProperty("MenuItemContextDivider2")]
	private ToolStripSeparator toolStripSeparator_1;

	[AccessedThroughProperty("MenuItemContextSaveAs")]
	private ToolStripMenuItem toolStripMenuItem_6;

	[AccessedThroughProperty("MenuItemFileOpen")]
	private ToolStripMenuItem toolStripMenuItem_7;

	[AccessedThroughProperty("MenuItemHelpAbout")]
	private ToolStripMenuItem toolStripMenuItem_8;

	[AccessedThroughProperty("ImageList1")]
	private ImageList imageList_0;

	[AccessedThroughProperty("ToolStripSeparator2")]
	private ToolStripSeparator toolStripSeparator_2;

	[AccessedThroughProperty("ToolStripSeparator3")]
	private ToolStripSeparator toolStripSeparator_3;

	[AccessedThroughProperty("ToolStripMenuItem1")]
	private ToolStripSeparator toolStripSeparator_4;

	[AccessedThroughProperty("MenuItemFileExit")]
	private ToolStripMenuItem toolStripMenuItem_9;

	[AccessedThroughProperty("MenuItemViewRefresh")]
	private ToolStripMenuItem toolStripMenuItem_10;

	[AccessedThroughProperty("ToolStripMenuItem2")]
	private ToolStripSeparator toolStripSeparator_5;

	[AccessedThroughProperty("MenuItemViewBack")]
	private ToolStripMenuItem toolStripMenuItem_11;

	[AccessedThroughProperty("MenuItemViewForward")]
	private ToolStripMenuItem toolStripMenuItem_12;

	[AccessedThroughProperty("MenuItemContextClose")]
	private ToolStripMenuItem toolStripMenuItem_13;

	[AccessedThroughProperty("ToolStripStatusLabel1")]
	private ToolStripStatusLabel toolStripStatusLabel_0;

	private GClass2 gclass2_0;

	private bool bool_0;

	private GClass7 gclass7_0;

	[DebuggerNonUserCode]
	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && icontainer_0 != null)
			{
				icontainer_0.Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	[DebuggerStepThrough]
	private void method_0()
	{
		icontainer_0 = new Container();
		ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(GForm1));
		vmethod_1(new MenuStrip());
		vmethod_3(new ToolStripMenuItem());
		vmethod_46(new ToolStripMenuItem());
		vmethod_55(new ToolStripSeparator());
		vmethod_57(new ToolStripMenuItem());
		vmethod_5(new ToolStripMenuItem());
		vmethod_59(new ToolStripMenuItem());
		_003C(new ToolStripSeparator());
		vmethod_62(new ToolStripMenuItem());
		vmethod_64(new ToolStripMenuItem());
		vmethod_7(new ToolStripMenuItem());
		_0024(new ToolStripMenuItem());
		vmethod_8(new ToolStrip());
		vmethod_10(new ToolStripButton());
		vmethod_23(new ToolStripButton());
		vmethod_51(new ToolStripSeparator());
		vmethod_25(new ToolStripButton());
		vmethod_27(new ToolStripButton());
		vmethod_53(new ToolStripSeparator());
		vmethod_12(new ToolStripComboBox());
		vmethod_13(new StatusStrip());
		vmethod_68(new ToolStripStatusLabel());
		vmethod_15(new SplitContainer());
		vmethod_17(new TreeView());
		vmethod_33(new ContextMenuStrip(icontainer_0));
		vmethod_35(new ToolStripMenuItem());
		vmethod_37(new ToolStripMenuItem());
		vmethod_39(new ToolStripSeparator());
		_003C(new ToolStripMenuItem());
		vmethod_42(new ToolStripSeparator());
		vmethod_44(new ToolStripMenuItem());
		vmethod_66(new ToolStripMenuItem());
		vmethod_49(new ImageList(icontainer_0));
		vmethod_19(new Panel());
		vmethod_31(new TextBox());
		vmethod_21(new GClass11());
		vmethod_29(new GClass10());
		vmethod_0().SuspendLayout();
		_003C().SuspendLayout();
		_0024().SuspendLayout();
		vmethod_14().Panel1.SuspendLayout();
		vmethod_14().Panel2.SuspendLayout();
		vmethod_14().SuspendLayout();
		vmethod_32().SuspendLayout();
		vmethod_18().SuspendLayout();
		SuspendLayout();
		vmethod_0().Items.AddRange(new ToolStripItem[3]
		{
			vmethod_2(),
			vmethod_4(),
			vmethod_6()
		});
		MenuStrip menuStrip = vmethod_0();
		Point location = new Point(0, 0);
		menuStrip.Location = location;
		vmethod_0().Name = "MenuStrip1";
		MenuStrip menuStrip2 = vmethod_0();
		Size size = new Size(828, 24);
		menuStrip2.Size = size;
		vmethod_0().TabIndex = 0;
		vmethod_0().Text = "MenuStrip1";
		vmethod_2().DropDownItems.AddRange(new ToolStripItem[3]
		{
			vmethod_45(),
			vmethod_54(),
			vmethod_56()
		});
		vmethod_2().Name = "FileToolStripMenuItem";
		ToolStripMenuItem toolStripMenuItem = vmethod_2();
		size = new Size(37, 20);
		toolStripMenuItem.Size = size;
		vmethod_2().Text = "&File";
		vmethod_45().Image = (Image)componentResourceManager.GetObject("MenuItemFileOpen.Image");
		vmethod_45().Name = "MenuItemFileOpen";
		vmethod_45().ShortcutKeys = Keys.O | Keys.Control;
		ToolStripMenuItem toolStripMenuItem2 = vmethod_45();
		size = new Size(155, 22);
		toolStripMenuItem2.Size = size;
		vmethod_45().Text = "&Open...";
		vmethod_54().Name = "ToolStripMenuItem1";
		ToolStripSeparator toolStripSeparator = vmethod_54();
		size = new Size(152, 6);
		toolStripSeparator.Size = size;
		vmethod_56().Name = "MenuItemFileExit";
		ToolStripMenuItem toolStripMenuItem3 = vmethod_56();
		size = new Size(155, 22);
		toolStripMenuItem3.Size = size;
		vmethod_56().Text = "E&xit";
		vmethod_4().DropDownItems.AddRange(new ToolStripItem[4]
		{
			vmethod_58(),
			vmethod_60(),
			vmethod_61(),
			vmethod_63()
		});
		vmethod_4().Name = "ViewToolStripMenuItem";
		ToolStripMenuItem toolStripMenuItem4 = vmethod_4();
		size = new Size(44, 20);
		toolStripMenuItem4.Size = size;
		vmethod_4().Text = "&View";
		vmethod_58().Image = (Image)componentResourceManager.GetObject("MenuItemViewRefresh.Image");
		vmethod_58().Name = "MenuItemViewRefresh";
		vmethod_58().ShortcutKeys = Keys.F5;
		ToolStripMenuItem toolStripMenuItem5 = vmethod_58();
		size = new Size(175, 22);
		toolStripMenuItem5.Size = size;
		vmethod_58().Text = "&Refresh";
		vmethod_60().Name = "ToolStripMenuItem2";
		ToolStripSeparator toolStripSeparator2 = vmethod_60();
		size = new Size(172, 6);
		toolStripSeparator2.Size = size;
		vmethod_61().Image = (Image)componentResourceManager.GetObject("MenuItemViewBack.Image");
		vmethod_61().Name = "MenuItemViewBack";
		vmethod_61().ShortcutKeys = Keys.Left | Keys.Alt;
		ToolStripMenuItem toolStripMenuItem6 = vmethod_61();
		size = new Size(175, 22);
		toolStripMenuItem6.Size = size;
		vmethod_61().Text = "&Back";
		vmethod_63().Image = (Image)componentResourceManager.GetObject("MenuItemViewForward.Image");
		vmethod_63().Name = "MenuItemViewForward";
		vmethod_63().ShortcutKeys = Keys.Right | Keys.Alt;
		ToolStripMenuItem toolStripMenuItem7 = vmethod_63();
		size = new Size(175, 22);
		toolStripMenuItem7.Size = size;
		vmethod_63().Text = "&Forward";
		vmethod_6().DropDownItems.AddRange(new ToolStripItem[1] { vmethod_47() });
		vmethod_6().Name = "HelpToolStripMenuItem";
		ToolStripMenuItem toolStripMenuItem8 = vmethod_6();
		size = new Size(44, 20);
		toolStripMenuItem8.Size = size;
		vmethod_6().Text = "&Help";
		vmethod_47().Name = "MenuItemHelpAbout";
		ToolStripMenuItem toolStripMenuItem9 = vmethod_47();
		size = new Size(107, 22);
		toolStripMenuItem9.Size = size;
		vmethod_47().Text = "&About";
		_003C().Items.AddRange(new ToolStripItem[7]
		{
			vmethod_9(),
			vmethod_22(),
			vmethod_50(),
			vmethod_24(),
			vmethod_26(),
			vmethod_52(),
			vmethod_11()
		});
		ToolStrip toolStrip = _003C();
		location = new Point(0, 24);
		toolStrip.Location = location;
		_003C().Name = "ToolStrip1";
		ToolStrip toolStrip2 = _003C();
		size = new Size(828, 25);
		toolStrip2.Size = size;
		_003C().TabIndex = 1;
		_003C().Text = "ToolStrip1";
		vmethod_9().DisplayStyle = ToolStripItemDisplayStyle.Image;
		vmethod_9().Image = (Image)componentResourceManager.GetObject("ToolStripButtonLoadFile.Image");
		vmethod_9().ImageTransparentColor = Color.Magenta;
		vmethod_9().Name = "ToolStripButtonLoadFile";
		ToolStripButton toolStripButton = vmethod_9();
		size = new Size(23, 22);
		toolStripButton.Size = size;
		vmethod_9().Text = "Open...";
		vmethod_9().ToolTipText = "Open...\r\nShortcut Key: Ctrl+O";
		vmethod_22().DisplayStyle = ToolStripItemDisplayStyle.Image;
		vmethod_22().Enabled = false;
		vmethod_22().Image = (Image)componentResourceManager.GetObject("ToolStripButtonRefresh.Image");
		vmethod_22().ImageTransparentColor = Color.Magenta;
		vmethod_22().Name = "ToolStripButtonRefresh";
		ToolStripButton toolStripButton2 = vmethod_22();
		size = new Size(23, 22);
		toolStripButton2.Size = size;
		vmethod_22().Text = "Refresh";
		vmethod_22().ToolTipText = "Refresh\r\nShortcut Key: F5";
		vmethod_50().Name = "ToolStripSeparator2";
		ToolStripSeparator toolStripSeparator3 = vmethod_50();
		size = new Size(6, 25);
		toolStripSeparator3.Size = size;
		vmethod_24().DisplayStyle = ToolStripItemDisplayStyle.Image;
		vmethod_24().Enabled = false;
		vmethod_24().Image = (Image)componentResourceManager.GetObject("ToolStripButtonBack.Image");
		vmethod_24().ImageTransparentColor = Color.Magenta;
		vmethod_24().Name = "ToolStripButtonBack";
		ToolStripButton toolStripButton3 = vmethod_24();
		size = new Size(23, 22);
		toolStripButton3.Size = size;
		vmethod_24().Text = "Back";
		vmethod_24().ToolTipText = "Back\r\nShortcut Key: Alt+Left";
		vmethod_26().DisplayStyle = ToolStripItemDisplayStyle.Image;
		vmethod_26().Enabled = false;
		vmethod_26().Image = (Image)componentResourceManager.GetObject("ToolStripButtonForward.Image");
		vmethod_26().ImageTransparentColor = Color.Magenta;
		vmethod_26().Name = "ToolStripButtonForward";
		ToolStripButton toolStripButton4 = vmethod_26();
		size = new Size(23, 22);
		toolStripButton4.Size = size;
		vmethod_26().Text = "Forward";
		vmethod_26().ToolTipText = "Forward\r\nShortcut Key: Alt+Right";
		vmethod_52().Name = "ToolStripSeparator3";
		ToolStripSeparator toolStripSeparator4 = vmethod_52();
		size = new Size(6, 25);
		toolStripSeparator4.Size = size;
		vmethod_11().DropDownStyle = ComboBoxStyle.DropDownList;
		vmethod_11().FlatStyle = FlatStyle.Standard;
		vmethod_11().Name = "ToolStripComboBoxLanguage";
		ToolStripComboBox toolStripComboBox = vmethod_11();
		size = new Size(121, 25);
		toolStripComboBox.Size = size;
		_0024().Items.AddRange(new ToolStripItem[1] { vmethod_67() });
		StatusStrip statusStrip = _0024();
		location = new Point(0, 517);
		statusStrip.Location = location;
		_0024().Name = "StatusStrip1";
		StatusStrip statusStrip2 = _0024();
		size = new Size(828, 22);
		statusStrip2.Size = size;
		_0024().TabIndex = 2;
		_0024().Text = "StatusStrip1";
		vmethod_67().Name = "ToolStripStatusLabel1";
		ToolStripStatusLabel toolStripStatusLabel = vmethod_67();
		size = new Size(813, 17);
		toolStripStatusLabel.Size = size;
		vmethod_67().Spring = true;
		vmethod_67().TextAlign = ContentAlignment.MiddleLeft;
		vmethod_14().Dock = DockStyle.Fill;
		vmethod_14().FixedPanel = FixedPanel.Panel1;
		SplitContainer splitContainer = vmethod_14();
		location = new Point(0, 49);
		splitContainer.Location = location;
		vmethod_14().Name = "SplitContainer1";
		vmethod_14().Panel1.Controls.Add(vmethod_16());
		vmethod_14().Panel2.Controls.Add(vmethod_18());
		SplitContainer splitContainer2 = vmethod_14();
		size = new Size(828, 468);
		splitContainer2.Size = size;
		vmethod_14().SplitterDistance = 371;
		vmethod_14().TabIndex = 3;
		vmethod_14().TabStop = false;
		vmethod_16().AllowDrop = true;
		vmethod_16().ContextMenuStrip = vmethod_32();
		vmethod_16().Dock = DockStyle.Fill;
		vmethod_16().HideSelection = false;
		vmethod_16().ImageKey = "0VSObject_Assembly.bmp";
		vmethod_16().ImageList = vmethod_48();
		TreeView treeView = vmethod_16();
		location = new Point(0, 0);
		treeView.Location = location;
		vmethod_16().Name = "TreeView1";
		vmethod_16().SelectedImageKey = "0VSObject_Assembly.bmp";
		TreeView treeView2 = vmethod_16();
		size = new Size(371, 468);
		treeView2.Size = size;
		vmethod_16().TabIndex = 0;
		vmethod_32().Items.AddRange(new ToolStripItem[7]
		{
			vmethod_34(),
			vmethod_36(),
			vmethod_38(),
			vmethod_40(),
			vmethod_41(),
			vmethod_43(),
			vmethod_65()
		});
		vmethod_32().Name = "ContextMenuStrip1";
		ContextMenuStrip contextMenuStrip = vmethod_32();
		size = new Size(124, 126);
		contextMenuStrip.Size = size;
		vmethod_34().Image = (Image)componentResourceManager.GetObject("MenuItemContextBack.Image");
		vmethod_34().Name = "MenuItemContextBack";
		vmethod_34().ShortcutKeyDisplayString = "";
		ToolStripMenuItem toolStripMenuItem10 = vmethod_34();
		size = new Size(123, 22);
		toolStripMenuItem10.Size = size;
		vmethod_34().Text = "Back";
		vmethod_36().Image = (Image)componentResourceManager.GetObject("MenuItemContextForward.Image");
		vmethod_36().Name = "MenuItemContextForward";
		vmethod_36().ShortcutKeyDisplayString = "";
		ToolStripMenuItem toolStripMenuItem11 = vmethod_36();
		size = new Size(123, 22);
		toolStripMenuItem11.Size = size;
		vmethod_36().Text = "Forward";
		vmethod_38().Name = "MenuItemContextDivider1";
		ToolStripSeparator toolStripSeparator5 = vmethod_38();
		size = new Size(120, 6);
		toolStripSeparator5.Size = size;
		vmethod_40().Name = "MenuItemContextCopy";
		ToolStripMenuItem toolStripMenuItem12 = vmethod_40();
		size = new Size(123, 22);
		toolStripMenuItem12.Size = size;
		vmethod_40().Text = "Copy";
		vmethod_41().Name = "MenuItemContextDivider2";
		ToolStripSeparator toolStripSeparator6 = vmethod_41();
		size = new Size(120, 6);
		toolStripSeparator6.Size = size;
		vmethod_43().Name = "MenuItemContextSaveAs";
		ToolStripMenuItem toolStripMenuItem13 = vmethod_43();
		size = new Size(123, 22);
		toolStripMenuItem13.Size = size;
		vmethod_43().Text = "Save As...";
		vmethod_65().Name = "MenuItemContextClose";
		ToolStripMenuItem toolStripMenuItem14 = vmethod_65();
		size = new Size(123, 22);
		toolStripMenuItem14.Size = size;
		vmethod_65().Text = "Close";
		vmethod_48().ImageStream = (ImageListStreamer)componentResourceManager.GetObject("ImageList1.ImageStream");
		vmethod_48().TransparentColor = Color.Magenta;
		vmethod_48().Images.SetKeyName(0, "0VSObject_Assembly.bmp");
		vmethod_48().Images.SetKeyName(1, "1VSObject_Library.bmp");
		vmethod_48().Images.SetKeyName(2, "2VSResources.bmp");
		vmethod_48().Images.SetKeyName(3, "2VSResources_Open.bmp");
		vmethod_48().Images.SetKeyName(4, "3VSProject_imagefile.bmp");
		vmethod_48().Images.SetKeyName(5, "3VSProject_resourcefile.bmp");
		vmethod_48().Images.SetKeyName(6, "3VSProject_xml.bmp");
		vmethod_48().Images.SetKeyName(7, "4VSBaseTypes.bmp");
		vmethod_48().Images.SetKeyName(8, "4VSReferences.bmp");
		vmethod_48().Images.SetKeyName(9, "5VSProject_reference.bmp");
		vmethod_48().Images.SetKeyName(10, "6VSObject_Namespace.bmp");
		vmethod_48().Images.SetKeyName(11, "7VSObject_Class.bmp");
		vmethod_48().Images.SetKeyName(12, "7VSObject_Class_Friend.bmp");
		vmethod_48().Images.SetKeyName(13, "7VSObject_Class_Private.bmp");
		vmethod_48().Images.SetKeyName(14, "7VSObject_Class_Protected.bmp");
		vmethod_48().Images.SetKeyName(15, "7VSObject_Class_Sealed.bmp");
		vmethod_48().Images.SetKeyName(16, "7VSObject_Delegate.bmp");
		vmethod_48().Images.SetKeyName(17, "7VSObject_Delegate_Friend.bmp");
		vmethod_48().Images.SetKeyName(18, "7VSObject_Delegate_Private.bmp");
		vmethod_48().Images.SetKeyName(19, "7VSObject_Delegate_Protected.bmp");
		vmethod_48().Images.SetKeyName(20, "7VSObject_Delegate_Sealed.bmp");
		vmethod_48().Images.SetKeyName(21, "7VSObject_Enum.bmp");
		vmethod_48().Images.SetKeyName(22, "7VSObject_Enum_Friend.bmp");
		vmethod_48().Images.SetKeyName(23, "7VSObject_Enum_Protected.bmp");
		vmethod_48().Images.SetKeyName(24, "7VSObject_Enum_Sealed.bmp");
		vmethod_48().Images.SetKeyName(25, "7VSObject_Interface.bmp");
		vmethod_48().Images.SetKeyName(26, "7VSObject_Interface_Friend.bmp");
		vmethod_48().Images.SetKeyName(27, "7VSObject_Interface_Private.bmp");
		vmethod_48().Images.SetKeyName(28, "7VSObject_Interface_Protected.bmp");
		vmethod_48().Images.SetKeyName(29, "7VSObject_Interface_Sealed.bmp");
		vmethod_48().Images.SetKeyName(30, "7VSObject_Module.bmp");
		vmethod_48().Images.SetKeyName(31, "7VSObject_Module_Friend.bmp");
		vmethod_48().Images.SetKeyName(32, "7VSObject_Module_Private.bmp");
		vmethod_48().Images.SetKeyName(33, "7VSObject_Module_Protected.bmp");
		vmethod_48().Images.SetKeyName(34, "7VSObject_Module_Sealed.bmp");
		vmethod_48().Images.SetKeyName(35, "7VSObject_Structure.bmp");
		vmethod_48().Images.SetKeyName(36, "7VSObject_Structure_Friend.bmp");
		vmethod_48().Images.SetKeyName(37, "7VSObject_Structure_Private.bmp");
		vmethod_48().Images.SetKeyName(38, "7VSObject_Structure_Protected.bmp");
		vmethod_48().Images.SetKeyName(39, "7VSObject_Structure_Sealed.bmp");
		vmethod_48().Images.SetKeyName(40, "7VSObject_ValueType.bmp");
		vmethod_48().Images.SetKeyName(41, "7VSObject_ValueType_Friend.bmp");
		vmethod_48().Images.SetKeyName(42, "7VSObject_ValueType_Private.bmp");
		vmethod_48().Images.SetKeyName(43, "7VSObject_ValueType_Protected.bmp");
		vmethod_48().Images.SetKeyName(44, "7VSObject_ValueType_Sealed.bmp");
		vmethod_48().Images.SetKeyName(45, "8VSObject_EnumItem.bmp");
		vmethod_48().Images.SetKeyName(46, "8VSObject_EnumItem_Friend.bmp");
		vmethod_48().Images.SetKeyName(47, "8VSObject_EnumItem_Private.bmp");
		vmethod_48().Images.SetKeyName(48, "8VSObject_EnumItem_Protected.bmp");
		vmethod_48().Images.SetKeyName(49, "8VSObject_EnumItem_Sealed.bmp");
		vmethod_48().Images.SetKeyName(50, "8VSObject_EnumPrivate.bmp");
		vmethod_48().Images.SetKeyName(51, "8VSObject_Method.bmp");
		vmethod_48().Images.SetKeyName(52, "8VSObject_Method_Friend.bmp");
		vmethod_48().Images.SetKeyName(53, "8VSObject_Method_Private.bmp");
		vmethod_48().Images.SetKeyName(54, "8VSObject_Method_Protected.bmp");
		vmethod_48().Images.SetKeyName(55, "8VSObject_Method_Sealed.bmp");
		vmethod_48().Images.SetKeyName(56, "8VSObject_MethodOverload.bmp");
		vmethod_48().Images.SetKeyName(57, "8VSObject_MethodOverload_Friend.bmp");
		vmethod_48().Images.SetKeyName(58, "8VSObject_MethodOverload_Private.bmp");
		vmethod_48().Images.SetKeyName(59, "8VSObject_MethodOverload_Protected.bmp");
		vmethod_48().Images.SetKeyName(60, "8VSObject_MethodOverload_Sealed.bmp");
		vmethod_48().Images.SetKeyName(61, "9VSObject_Properties.bmp");
		vmethod_48().Images.SetKeyName(62, "9VSObject_Properties_Friend.bmp");
		vmethod_48().Images.SetKeyName(63, "9VSObject_Properties_Private.bmp");
		vmethod_48().Images.SetKeyName(64, "9VSObject_Properties_Protected.bmp");
		vmethod_48().Images.SetKeyName(65, "9VSObject_Properties_Sealed.bmp");
		vmethod_48().Images.SetKeyName(66, "10VSObject_Event.bmp");
		vmethod_48().Images.SetKeyName(67, "10VSObject_Event_Friend.bmp");
		vmethod_48().Images.SetKeyName(68, "10VSObject_Event_Private.bmp");
		vmethod_48().Images.SetKeyName(69, "10VSObject_Event_Protected.bmp");
		vmethod_48().Images.SetKeyName(70, "10VSObject_Event_Sealed.bmp");
		vmethod_48().Images.SetKeyName(71, "11VSObject_Constant.bmp");
		vmethod_48().Images.SetKeyName(72, "11VSObject_Constant_Friend.bmp");
		vmethod_48().Images.SetKeyName(73, "11VSObject_Constant_Private.bmp");
		vmethod_48().Images.SetKeyName(74, "11VSObject_Constant_Protected.bmp");
		vmethod_48().Images.SetKeyName(75, "11VSObject_Constant_Sealed.bmp");
		vmethod_48().Images.SetKeyName(76, "11VSObject_Field.bmp");
		vmethod_48().Images.SetKeyName(77, "11VSObject_Field_Friend.bmp");
		vmethod_48().Images.SetKeyName(78, "11VSObject_Field_Private.bmp");
		vmethod_48().Images.SetKeyName(79, "11VSObject_Field_Protected.bmp");
		vmethod_48().Images.SetKeyName(80, "11VSObject_Field_Sealed.bmp");
		vmethod_18().BackColor = SystemColors.Window;
		vmethod_18().Controls.Add(vmethod_20());
		vmethod_18().Controls.Add(vmethod_28());
		vmethod_18().Controls.Add(vmethod_30());
		vmethod_18().Dock = DockStyle.Fill;
		Panel panel = vmethod_18();
		location = new Point(0, 0);
		panel.Location = location;
		vmethod_18().Name = "Panel1";
		Panel panel2 = vmethod_18();
		Padding padding = new Padding(1);
		panel2.Padding = padding;
		Panel panel3 = vmethod_18();
		size = new Size(453, 468);
		panel3.Size = size;
		vmethod_18().TabIndex = 0;
		vmethod_30().BackColor = SystemColors.Info;
		vmethod_30().BorderStyle = BorderStyle.None;
		vmethod_30().Dock = DockStyle.Fill;
		vmethod_30().Font = new Font("Courier New", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
		vmethod_30().HideSelection = false;
		TextBox textBox = vmethod_30();
		location = new Point(1, 1);
		textBox.Location = location;
		vmethod_30().Multiline = true;
		vmethod_30().Name = "TextBox1";
		vmethod_30().ReadOnly = true;
		vmethod_30().ScrollBars = ScrollBars.Both;
		TextBox textBox2 = vmethod_30();
		size = new Size(451, 466);
		textBox2.Size = size;
		vmethod_30().TabIndex = 2;
		vmethod_30().Visible = false;
		vmethod_30().WordWrap = false;
		vmethod_20().BackColor = SystemColors.Info;
		vmethod_20().Dock = DockStyle.Fill;
		GClass11 gClass = vmethod_20();
		location = new Point(1, 1);
		gClass.Location = location;
		vmethod_20().Name = "SyntaxBox1";
		GClass11 gClass2 = vmethod_20();
		size = new Size(451, 466);
		gClass2.Size = size;
		vmethod_20().TabIndex = 1;
		vmethod_20().TabStop = true;
		vmethod_28().BackColor = SystemColors.Info;
		vmethod_28().BackgroundImageLayout = ImageLayout.Center;
		vmethod_28().Dock = DockStyle.Fill;
		GClass10 gClass3 = vmethod_28();
		location = new Point(1, 1);
		gClass3.Location = location;
		vmethod_28().Name = "ImageBox1";
		GClass10 gClass4 = vmethod_28();
		size = new Size(451, 466);
		gClass4.Size = size;
		vmethod_28().TabIndex = 4;
		vmethod_28().Visible = false;
		SizeF sizeF = new SizeF(6f, 13f);
		AutoScaleDimensions = sizeF;
		AutoScaleMode = AutoScaleMode.Font;
		size = new Size(828, 539);
		ClientSize = size;
		Controls.Add(vmethod_14());
		Controls.Add(_0024());
		Controls.Add(_003C());
		Controls.Add(vmethod_0());
		Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
		MainMenuStrip = vmethod_0();
		Name = "MainForm";
		Text = "CodeReflect <Version>";
		vmethod_0().ResumeLayout(performLayout: false);
		vmethod_0().PerformLayout();
		_003C().ResumeLayout(performLayout: false);
		_003C().PerformLayout();
		_0024().ResumeLayout(performLayout: false);
		_0024().PerformLayout();
		vmethod_14().Panel1.ResumeLayout(performLayout: false);
		vmethod_14().Panel2.ResumeLayout(performLayout: false);
		vmethod_14().ResumeLayout(performLayout: false);
		vmethod_32().ResumeLayout(performLayout: false);
		vmethod_18().ResumeLayout(performLayout: false);
		vmethod_18().PerformLayout();
		ResumeLayout(performLayout: false);
		PerformLayout();
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual MenuStrip vmethod_0()
	{
		return menuStrip_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_1(MenuStrip menuStrip_1)
	{
		menuStrip_0 = menuStrip_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_2()
	{
		return toolStripMenuItem_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_3(ToolStripMenuItem toolStripMenuItem_14)
	{
		toolStripMenuItem_0 = toolStripMenuItem_14;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_4()
	{
		return toolStripMenuItem_1;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_5(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_38;
		EventHandler value2 = method_37;
		if (toolStripMenuItem_1 != null)
		{
			toolStripMenuItem_1.DropDownOpening -= value;
			toolStripMenuItem_1.DropDownClosed -= value2;
		}
		toolStripMenuItem_1 = toolStripMenuItem_14;
		if (toolStripMenuItem_1 != null)
		{
			toolStripMenuItem_1.DropDownOpening += value;
			toolStripMenuItem_1.DropDownClosed += value2;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_6()
	{
		return toolStripMenuItem_2;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_7(ToolStripMenuItem toolStripMenuItem_14)
	{
		toolStripMenuItem_2 = toolStripMenuItem_14;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStrip _003C()
	{
		return this.m__003C;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_8(ToolStrip toolStrip_0)
	{
		this.m__003C = toolStrip_0;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripButton vmethod_9()
	{
		return toolStripButton_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_10(ToolStripButton toolStripButton_4)
	{
		EventHandler value = method_22;
		if (toolStripButton_0 != null)
		{
			toolStripButton_0.Click -= value;
		}
		toolStripButton_0 = toolStripButton_4;
		if (toolStripButton_0 != null)
		{
			toolStripButton_0.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripComboBox vmethod_11()
	{
		return toolStripComboBox_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_12(ToolStripComboBox toolStripComboBox_1)
	{
		EventHandler value = (object sender, EventArgs e) =>
		{
			Class6.smethod_0().SelectedLanguage = vmethod_11().SelectedIndex;
			gclass2_0 = (GClass2)vmethod_11().SelectedItem;
			method_16(vmethod_16(), new TreeViewEventArgs(vmethod_16().SelectedNode));
		};
		if (toolStripComboBox_0 != null)
		{
			toolStripComboBox_0.SelectedIndexChanged -= value;
		}
		toolStripComboBox_0 = toolStripComboBox_1;
		if (toolStripComboBox_0 != null)
		{
			toolStripComboBox_0.SelectedIndexChanged += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual StatusStrip _0024()
	{
		return this.m__0024;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_13(StatusStrip statusStrip_0)
	{
		this.m__0024 = statusStrip_0;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual SplitContainer vmethod_14()
	{
		return splitContainer_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_15(SplitContainer splitContainer_1)
	{
		splitContainer_0 = splitContainer_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual TreeView vmethod_16()
	{
		return treeView_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_17(TreeView treeView_1)
	{
		DragEventHandler value = method_18;
		DragEventHandler value2 = method_17;
		TreeViewEventHandler value3 = method_16;
		TreeNodeMouseClickEventHandler value4 = method_19;
		if (treeView_0 != null)
		{
			treeView_0.DragEnter -= value;
			treeView_0.DragDrop -= value2;
			treeView_0.AfterSelect -= value3;
			treeView_0.NodeMouseClick -= value4;
		}
		treeView_0 = treeView_1;
		if (treeView_0 != null)
		{
			treeView_0.DragEnter += value;
			treeView_0.DragDrop += value2;
			treeView_0.AfterSelect += value3;
			treeView_0.NodeMouseClick += value4;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual Panel vmethod_18()
	{
		return panel_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_19(Panel panel_1)
	{
		PaintEventHandler value = method_21;
		if (panel_0 != null)
		{
			panel_0.Paint -= value;
		}
		panel_0 = panel_1;
		if (panel_0 != null)
		{
			panel_0.Paint += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual GClass11 vmethod_20()
	{
		return gclass11_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_21(GClass11 gclass11_1)
	{
		GClass11._0024 obj = method_20;
		if (gclass11_0 != null)
		{
			gclass11_0.method_13(obj);
		}
		gclass11_0 = gclass11_1;
		if (gclass11_0 != null)
		{
			gclass11_0.method_12(obj);
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripButton vmethod_22()
	{
		return toolStripButton_1;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_23(ToolStripButton toolStripButton_4)
	{
		EventHandler value = method_23;
		if (toolStripButton_1 != null)
		{
			toolStripButton_1.Click -= value;
		}
		toolStripButton_1 = toolStripButton_4;
		if (toolStripButton_1 != null)
		{
			toolStripButton_1.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripButton vmethod_24()
	{
		return toolStripButton_2;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_25(ToolStripButton toolStripButton_4)
	{
		EventHandler value = method_24;
		if (toolStripButton_2 != null)
		{
			toolStripButton_2.Click -= value;
		}
		toolStripButton_2 = toolStripButton_4;
		if (toolStripButton_2 != null)
		{
			toolStripButton_2.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripButton vmethod_26()
	{
		return toolStripButton_3;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_27(ToolStripButton toolStripButton_4)
	{
		EventHandler value = method_25;
		if (toolStripButton_3 != null)
		{
			toolStripButton_3.Click -= value;
		}
		toolStripButton_3 = toolStripButton_4;
		if (toolStripButton_3 != null)
		{
			toolStripButton_3.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual GClass10 vmethod_28()
	{
		return gclass10_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_29(GClass10 gclass10_1)
	{
		gclass10_0 = gclass10_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual TextBox vmethod_30()
	{
		return textBox_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_31(TextBox textBox_1)
	{
		textBox_0 = textBox_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ContextMenuStrip vmethod_32()
	{
		return contextMenuStrip_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_33(ContextMenuStrip contextMenuStrip_1)
	{
		CancelEventHandler value = method_26;
		if (contextMenuStrip_0 != null)
		{
			contextMenuStrip_0.Opening -= value;
		}
		contextMenuStrip_0 = contextMenuStrip_1;
		if (contextMenuStrip_0 != null)
		{
			contextMenuStrip_0.Opening += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_34()
	{
		return toolStripMenuItem_3;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_35(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_27;
		if (toolStripMenuItem_3 != null)
		{
			toolStripMenuItem_3.Click -= value;
		}
		toolStripMenuItem_3 = toolStripMenuItem_14;
		if (toolStripMenuItem_3 != null)
		{
			toolStripMenuItem_3.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_36()
	{
		return toolStripMenuItem_4;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_37(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_28;
		if (toolStripMenuItem_4 != null)
		{
			toolStripMenuItem_4.Click -= value;
		}
		toolStripMenuItem_4 = toolStripMenuItem_14;
		if (toolStripMenuItem_4 != null)
		{
			toolStripMenuItem_4.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_38()
	{
		return toolStripSeparator_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_39(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_0 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_40()
	{
		return toolStripMenuItem_5;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void _003C(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = (object sender, EventArgs e) =>
		{
			Class1.smethod_0().Clipboard.SetText(vmethod_16().SelectedNode.Text);
		};
		if (toolStripMenuItem_5 != null)
		{
			toolStripMenuItem_5.Click -= value;
		}
		toolStripMenuItem_5 = toolStripMenuItem_14;
		if (toolStripMenuItem_5 != null)
		{
			toolStripMenuItem_5.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_41()
	{
		return toolStripSeparator_1;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_42(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_1 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_43()
	{
		return toolStripMenuItem_6;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_44(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_29;
		if (toolStripMenuItem_6 != null)
		{
			toolStripMenuItem_6.Click -= value;
		}
		toolStripMenuItem_6 = toolStripMenuItem_14;
		if (toolStripMenuItem_6 != null)
		{
			toolStripMenuItem_6.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_45()
	{
		return toolStripMenuItem_7;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_46(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_32;
		if (toolStripMenuItem_7 != null)
		{
			toolStripMenuItem_7.Click -= value;
		}
		toolStripMenuItem_7 = toolStripMenuItem_14;
		if (toolStripMenuItem_7 != null)
		{
			toolStripMenuItem_7.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_47()
	{
		return toolStripMenuItem_8;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void _0024(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_31;
		if (toolStripMenuItem_8 != null)
		{
			toolStripMenuItem_8.Click -= value;
		}
		toolStripMenuItem_8 = toolStripMenuItem_14;
		if (toolStripMenuItem_8 != null)
		{
			toolStripMenuItem_8.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ImageList vmethod_48()
	{
		return imageList_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_49(ImageList imageList_1)
	{
		imageList_0 = imageList_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_50()
	{
		return toolStripSeparator_2;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_51(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_2 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_52()
	{
		return toolStripSeparator_3;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_53(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_3 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_54()
	{
		return toolStripSeparator_4;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_55(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_4 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_56()
	{
		return toolStripMenuItem_9;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_57(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_33;
		if (toolStripMenuItem_9 != null)
		{
			toolStripMenuItem_9.Click -= value;
		}
		toolStripMenuItem_9 = toolStripMenuItem_14;
		if (toolStripMenuItem_9 != null)
		{
			toolStripMenuItem_9.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_58()
	{
		return toolStripMenuItem_10;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_59(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_34;
		if (toolStripMenuItem_10 != null)
		{
			toolStripMenuItem_10.Click -= value;
		}
		toolStripMenuItem_10 = toolStripMenuItem_14;
		if (toolStripMenuItem_10 != null)
		{
			toolStripMenuItem_10.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripSeparator vmethod_60()
	{
		return toolStripSeparator_5;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void _003C(ToolStripSeparator toolStripSeparator_6)
	{
		toolStripSeparator_5 = toolStripSeparator_6;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_61()
	{
		return toolStripMenuItem_11;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_62(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_35;
		if (toolStripMenuItem_11 != null)
		{
			toolStripMenuItem_11.Click -= value;
		}
		toolStripMenuItem_11 = toolStripMenuItem_14;
		if (toolStripMenuItem_11 != null)
		{
			toolStripMenuItem_11.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_63()
	{
		return toolStripMenuItem_12;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_64(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_36;
		if (toolStripMenuItem_12 != null)
		{
			toolStripMenuItem_12.Click -= value;
		}
		toolStripMenuItem_12 = toolStripMenuItem_14;
		if (toolStripMenuItem_12 != null)
		{
			toolStripMenuItem_12.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripMenuItem vmethod_65()
	{
		return toolStripMenuItem_13;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_66(ToolStripMenuItem toolStripMenuItem_14)
	{
		EventHandler value = method_30;
		if (toolStripMenuItem_13 != null)
		{
			toolStripMenuItem_13.Click -= value;
		}
		toolStripMenuItem_13 = toolStripMenuItem_14;
		if (toolStripMenuItem_13 != null)
		{
			toolStripMenuItem_13.Click += value;
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ToolStripStatusLabel vmethod_67()
	{
		return toolStripStatusLabel_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_68(ToolStripStatusLabel toolStripStatusLabel_1)
	{
		toolStripStatusLabel_0 = toolStripStatusLabel_1;
	}

	public GForm1()
	{
		Shown += GForm1_Shown;
		bool_0 = false;
		gclass7_0 = new GClass7();
		Type typeFromHandle = typeof(Control);
		FieldInfo field = typeFromHandle.GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic);
		Font font = Control.DefaultFont;
		Font value = new Font(SystemFonts.DialogFont.Name, font.SizeInPoints);
		field.SetValue(null, value);
		method_0();
		Text = Text.Replace("<Version>", Class1.smethod_1().Info.Version.ToString(3));
		vmethod_11().Items.Add(new GClass4());
		vmethod_11().Items.Add(new GClass3());
		vmethod_11().Items.Add(new GClass5());
		vmethod_11().SelectedIndex = Class6.smethod_0().SelectedLanguage;
		vmethod_16().TreeViewNodeSorter = new GClass9();
	}

	private void GForm1_Shown(object sender, EventArgs e)
	{
		Application.DoEvents();
		TreeNode treeNode = null;
		string lastSelectedNodePath = Class6.smethod_0().LastSelectedNodePath;
		if (File.Exists(Class6.smethod_0().LastFile))
		{
			treeNode = method_8(Class6.smethod_0().LastFile);
			if (treeNode != null)
			{
				vmethod_16().BeginUpdate();
				vmethod_16().Nodes.Add(treeNode);
				vmethod_16().SelectedNode = vmethod_16().Nodes[0];
				vmethod_16().Nodes[0].Expand();
				if (vmethod_16().Nodes[0].Nodes.Count > 0)
				{
					vmethod_16().Nodes[0].Nodes[0].Expand();
				}
				if (!string.IsNullOrEmpty(lastSelectedNodePath))
				{
					method_12(lastSelectedNodePath);
				}
				vmethod_16().EndUpdate();
			}
		}
		else
		{
			method_22(null, null);
		}
	}

	private TreeNode method_1(string string_0)
	{
		string text = GClass1.smethod_0(string_0, vmethod_16().Nodes);
		if (string.IsNullOrEmpty(text))
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				text = openFileDialog.FileName;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		TreeNode treeNode = method_8(text);
		vmethod_16().BeginUpdate();
		if (treeNode != null)
		{
			method_10(treeNode);
		}
		vmethod_16().EndUpdate();
		return treeNode;
	}

	private string method_2(PropertyDefinition propertyDefinition_0)
	{
		MethodDefinition methodDefinition = propertyDefinition_0.GetMethod;
		if (methodDefinition == null)
		{
			methodDefinition = propertyDefinition_0.SetMethod;
		}
		if (methodDefinition.IsAssembly)
		{
			return "9VSObject_Properties_Friend.bmp";
		}
		if (methodDefinition.IsFamily)
		{
			return "9VSObject_Properties_Protected.bmp";
		}
		if (methodDefinition.IsPrivate)
		{
			return "9VSObject_Properties_Private.bmp";
		}
		return "9VSObject_Properties.bmp";
	}

	private string method_3(MethodDefinition methodDefinition_0)
	{
		if (methodDefinition_0.IsReuseSlot & methodDefinition_0.IsAssembly)
		{
			return "8VSObject_MethodOverload_Friend.bmp";
		}
		if (methodDefinition_0.IsReuseSlot & methodDefinition_0.IsFamily)
		{
			return "8VSObject_MethodOverload_Protected.bmp";
		}
		if (methodDefinition_0.IsReuseSlot & methodDefinition_0.IsPrivate)
		{
			return "8VSObject_MethodOverload_Private.bmp";
		}
		if (methodDefinition_0.IsReuseSlot)
		{
			return "8VSObject_MethodOverload.bmp";
		}
		if (methodDefinition_0.IsAssembly)
		{
			return "8VSObject_Method_Friend.bmp";
		}
		if (methodDefinition_0.IsFamily)
		{
			return "8VSObject_Method_Protected.bmp";
		}
		if (methodDefinition_0.IsPrivate)
		{
			return "8VSObject_Method_Private.bmp";
		}
		return "8VSObject_Method.bmp";
	}

	private string method_4(FieldDefinition fieldDefinition_0)
	{
		if (fieldDefinition_0.HasConstant)
		{
			if (fieldDefinition_0.IsAssembly)
			{
				return "11VSObject_Constant_Friend.bmp";
			}
			if (fieldDefinition_0.IsFamily)
			{
				return "11VSObject_Constant_Protected.bmp";
			}
			if (fieldDefinition_0.IsPrivate)
			{
				return "11VSObject_Constant_Private.bmp";
			}
			return "11VSObject_Constant.bmp";
		}
		if (fieldDefinition_0.IsAssembly)
		{
			return "11VSObject_Field_Friend.bmp";
		}
		if (fieldDefinition_0.IsFamily)
		{
			return "11VSObject_Field_Protected.bmp";
		}
		if (fieldDefinition_0.IsPrivate)
		{
			return "11VSObject_Field_Private.bmp";
		}
		return "11VSObject_Field.bmp";
	}

	private string method_5(EventDefinition eventDefinition_0)
	{
		MethodDefinition methodDefinition = eventDefinition_0.AddMethod;
		if (methodDefinition == null)
		{
			methodDefinition = eventDefinition_0.RemoveMethod;
		}
		if (methodDefinition == null)
		{
			methodDefinition = eventDefinition_0.InvokeMethod;
		}
		if (methodDefinition.IsAssembly)
		{
			return "10VSObject_Event_Friend.bmp";
		}
		if (methodDefinition.IsFamily)
		{
			return "10VSObject_Event_Protected.bmp";
		}
		if (methodDefinition.IsPrivate)
		{
			return "10VSObject_Event_Private.bmp";
		}
		return "10VSObject_Event.bmp";
	}

	private string method_6(TypeDefinition typeDefinition_0)
	{
		if (typeDefinition_0.BaseType != null && typeDefinition_0.BaseType.FullName.EndsWith("Delegate"))
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_Delegate_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_Delegate_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_Delegate_Friend.bmp";
			}
			if (typeDefinition_0.IsNotPublic)
			{
				return "7VSObject_Delegate_Private.bmp";
			}
			return "7VSObject_Delegate.bmp";
		}
		if (typeDefinition_0.IsEnum)
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_Enum_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_Enum_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_Enum_Friend.bmp";
			}
			return "7VSObject_Enum.bmp";
		}
		if (typeDefinition_0.IsInterface)
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_Interface_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_Interface_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_Interface_Friend.bmp";
			}
			if (typeDefinition_0.IsNotPublic)
			{
				return "7VSObject_Interface_Private.bmp";
			}
			return "7VSObject_Interface.bmp";
		}
		if (typeDefinition_0.IsSequentialLayout)
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_Structure_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_Structure_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_Structure_Friend.bmp";
			}
			if (typeDefinition_0.IsNotPublic)
			{
				return "7VSObject_Structure_Private.bmp";
			}
			return "7VSObject_Structure.bmp";
		}
		if (typeDefinition_0.IsValueType)
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_ValueType_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_ValueType_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_ValueType_Friend.bmp";
			}
			if (typeDefinition_0.IsNotPublic)
			{
				return "7VSObject_ValueType_Private.bmp";
			}
			return "7VSObject_ValueType.bmp";
		}
		if (typeDefinition_0.IsClass)
		{
			if (typeDefinition_0.IsSealed)
			{
				return "7VSObject_Structure_Sealed.bmp";
			}
			if (typeDefinition_0.IsNestedFamily)
			{
				return "7VSObject_Class_Protected.bmp";
			}
			if (typeDefinition_0.IsNestedAssembly)
			{
				return "7VSObject_Class_Friend.bmp";
			}
			if (typeDefinition_0.IsNotPublic)
			{
				return "7VSObject_Class_Private.bmp";
			}
			return "7VSObject_Class.bmp";
		}
		if (typeDefinition_0.IsSealed)
		{
			return "7VSObject_Module_Sealed.bmp";
		}
		if (typeDefinition_0.IsNestedFamily)
		{
			return "7VSObject_Module_Protected.bmp";
		}
		if (typeDefinition_0.IsNestedAssembly)
		{
			return "7VSObject_Module_Friend.bmp";
		}
		if (typeDefinition_0.IsNotPublic)
		{
			return "7VSObject_Module_Private.bmp";
		}
		return "7VSObject_Module.bmp";
	}

	private void method_7(TreeNode treeNode_0, TypeDefinition typeDefinition_0)
	{
		TreeNode treeNode = new TreeNode(GClass2.smethod_3(typeDefinition_0));
		treeNode.ImageKey = method_6(typeDefinition_0);
		treeNode.SelectedImageKey = treeNode.ImageKey;
		if (typeDefinition_0.IsNotPublic)
		{
			treeNode.ForeColor = Color.Gray;
		}
		treeNode.Tag = typeDefinition_0;
		treeNode_0.Nodes.Add(treeNode);
		try
		{
			TreeNode treeNode2 = new TreeNode("Base Types");
			treeNode2.ImageKey = "4VSBaseTypes.bmp";
			treeNode2.SelectedImageKey = "4VSBaseTypes.bmp";
			if (typeDefinition_0.BaseType != null)
			{
				TreeNode treeNode3 = new TreeNode(typeDefinition_0.BaseType.Name);
				treeNode3.ImageKey = "7VSObject_Class.bmp";
				treeNode3.SelectedImageKey = "7VSObject_Class.bmp";
				treeNode2.Nodes.Add(treeNode3);
			}
			foreach (TypeReference @interface in typeDefinition_0.Interfaces)
			{
				TreeNode treeNode4 = new TreeNode(@interface.Name);
				treeNode4.ImageKey = "7VSObject_Interface.bmp";
				treeNode4.SelectedImageKey = "7VSObject_Interface.bmp";
				treeNode2.Nodes.Add(treeNode4);
			}
			if (treeNode2.Nodes.Count > 0)
			{
				treeNode.Nodes.Add(treeNode2);
			}
			foreach (TypeDefinition nestedType in typeDefinition_0.NestedTypes)
			{
				method_7(treeNode, nestedType);
			}
			List<string> list = new List<string>();
			foreach (PropertyDefinition property in typeDefinition_0.Properties)
			{
				if (property.SetMethod != null)
				{
					list.Add(property.SetMethod.Name);
				}
				if (property.GetMethod != null)
				{
					list.Add(property.GetMethod.Name);
				}
			}
			foreach (EventDefinition @event in typeDefinition_0.Events)
			{
				if (@event.AddMethod != null)
				{
					list.Add(@event.AddMethod.Name);
				}
				if (@event.RemoveMethod != null)
				{
					list.Add(@event.RemoveMethod.Name);
				}
				foreach (MethodDefinition otherMethod in @event.OtherMethods)
				{
					if (otherMethod != null)
					{
						list.Add(otherMethod.Name);
					}
				}
			}
			foreach (MethodDefinition method in typeDefinition_0.Methods)
			{
				if (!list.Contains(method.Name))
				{
					TreeNode treeNode5 = new TreeNode(GClass2.smethod_5(method));
					treeNode5.ImageKey = method_3(method);
					treeNode5.SelectedImageKey = treeNode5.ImageKey;
					if (!method.IsPublic)
					{
						treeNode5.ForeColor = Color.Gray;
					}
					treeNode5.Tag = method;
					treeNode.Nodes.Add(treeNode5);
				}
			}
			foreach (PropertyDefinition property2 in typeDefinition_0.Properties)
			{
				TreeNode treeNode6 = new TreeNode(property2.Name);
				treeNode6.ImageKey = method_2(property2);
				treeNode6.SelectedImageKey = treeNode6.ImageKey;
				treeNode6.Tag = property2;
				if (property2.SetMethod != null)
				{
					TreeNode treeNode7 = new TreeNode(GClass2.smethod_5(property2.SetMethod));
					treeNode7.Tag = property2.SetMethod;
					treeNode7.ImageKey = method_3(property2.SetMethod);
					treeNode7.SelectedImageKey = treeNode7.ImageKey;
					if (!property2.SetMethod.IsPublic)
					{
						treeNode7.ForeColor = Color.Gray;
					}
					if (!property2.SetMethod.IsPublic)
					{
						treeNode6.ForeColor = Color.Gray;
					}
					treeNode6.Nodes.Add(treeNode7);
				}
				if (property2.GetMethod != null)
				{
					TreeNode treeNode8 = new TreeNode(GClass2.smethod_5(property2.GetMethod));
					treeNode8.Tag = property2.GetMethod;
					treeNode8.ImageKey = method_3(property2.GetMethod);
					treeNode8.SelectedImageKey = treeNode8.ImageKey;
					if (!property2.GetMethod.IsPublic)
					{
						treeNode8.ForeColor = Color.Gray;
					}
					if (!property2.GetMethod.IsPublic)
					{
						treeNode6.ForeColor = Color.Gray;
					}
					treeNode6.Nodes.Add(treeNode8);
				}
				treeNode.Nodes.Add(treeNode6);
			}
			foreach (EventDefinition event2 in typeDefinition_0.Events)
			{
				TreeNode treeNode9 = new TreeNode(event2.Name);
				treeNode9.ImageKey = method_5(event2);
				treeNode9.SelectedImageKey = treeNode9.ImageKey;
				treeNode9.Tag = event2;
				treeNode.Nodes.Add(treeNode9);
				if (event2.AddMethod != null)
				{
					TreeNode treeNode10 = new TreeNode(GClass2.smethod_5(event2.AddMethod));
					treeNode10.Tag = event2.AddMethod;
					treeNode10.ImageKey = method_3(event2.AddMethod);
					treeNode10.SelectedImageKey = treeNode10.ImageKey;
					if (!event2.AddMethod.IsPublic)
					{
						treeNode10.ForeColor = Color.Gray;
					}
					if (!event2.AddMethod.IsPublic)
					{
						treeNode9.ForeColor = Color.Gray;
					}
					treeNode9.Nodes.Add(treeNode10);
				}
				if (event2.RemoveMethod != null)
				{
					TreeNode treeNode11 = new TreeNode(GClass2.smethod_5(event2.RemoveMethod));
					treeNode11.Tag = event2.RemoveMethod;
					treeNode11.ImageKey = method_3(event2.RemoveMethod);
					treeNode11.SelectedImageKey = treeNode11.ImageKey;
					if (!event2.RemoveMethod.IsPublic)
					{
						treeNode11.ForeColor = Color.Gray;
					}
					if (!event2.RemoveMethod.IsPublic)
					{
						treeNode9.ForeColor = Color.Gray;
					}
					treeNode9.Nodes.Add(treeNode11);
				}
				foreach (MethodDefinition otherMethod2 in event2.OtherMethods)
				{
					if (otherMethod2 != null)
					{
						TreeNode treeNode12 = new TreeNode(GClass2.smethod_5(otherMethod2));
						treeNode12.Tag = otherMethod2;
						treeNode12.ImageKey = method_3(otherMethod2);
						treeNode12.SelectedImageKey = treeNode12.ImageKey;
						if (!otherMethod2.IsPublic)
						{
							treeNode12.ForeColor = Color.Gray;
						}
						if (!otherMethod2.IsPublic)
						{
							treeNode9.ForeColor = Color.Gray;
						}
						treeNode9.Nodes.Add(treeNode12);
					}
				}
			}
			foreach (FieldDefinition field in typeDefinition_0.Fields)
			{
				TreeNode treeNode13 = new TreeNode(field.Name);
				treeNode13.ImageKey = method_4(field);
				treeNode13.SelectedImageKey = treeNode13.ImageKey;
				treeNode13.Tag = field;
				if (!field.IsPublic)
				{
					treeNode13.ForeColor = Color.Gray;
				}
				treeNode.Nodes.Add(treeNode13);
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			treeNode.Tag = ex2.Message;
			ProjectData.ClearProjectError();
		}
	}

	private TreeNode method_8(string string_0)
	{
		TreeNode treeNode = null;
		try
		{
			treeNode = GClass16.smethod_2(method_9, string_0, this);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			MessageBox.Show(this, string.Format("Unable to load the file \"{0}\".", string_0), "Cannot Load File");
			ProjectData.ClearProjectError();
		}
		if (treeNode != null)
		{
			vmethod_22().Enabled = true;
		}
		return treeNode;
	}

	private TreeNode method_9(string string_0)
	{
		if (!File.Exists(string_0))
		{
			return null;
		}
		AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(string_0);
		try
		{
			if (File.Exists(Path.ChangeExtension(string_0, ".pdb")))
			{
				PdbReaderProvider pdbReaderProvider = new PdbReaderProvider();
				assemblyDefinition.MainModule.ReadSymbols(pdbReaderProvider.GetSymbolReader(assemblyDefinition.MainModule, Path.ChangeExtension(string_0, ".pdb")));
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
		TreeNode treeNode = new TreeNode(assemblyDefinition.Name.Name);
		treeNode.Tag = new GClass0(string_0, assemblyDefinition);
		treeNode.ImageKey = "0VSObject_Assembly.bmp";
		treeNode.SelectedImageKey = "0VSObject_Assembly.bmp";
		foreach (ModuleDefinition module in assemblyDefinition.Modules)
		{
			TreeNode treeNode2 = treeNode.Nodes.Add(module.Name);
			Dictionary<string, TreeNode> dictionary = new Dictionary<string, TreeNode>();
			treeNode2.ImageKey = "1VSObject_Library.bmp";
			treeNode2.SelectedImageKey = "1VSObject_Library.bmp";
			treeNode2.Tag = module;
			TreeNode treeNode3 = treeNode2.Nodes.Add("References");
			treeNode3.ImageKey = "4VSReferences.bmp";
			treeNode3.SelectedImageKey = "4VSReferences.bmp";
			foreach (AssemblyNameReference assemblyReference in module.AssemblyReferences)
			{
				TreeNode treeNode4 = new TreeNode(string.Format("{0} ({1})", assemblyReference.Name, assemblyReference.Version.ToString()));
				treeNode4.Tag = assemblyReference;
				treeNode4.ImageKey = "5VSProject_reference.bmp";
				treeNode4.SelectedImageKey = "5VSProject_reference.bmp";
				treeNode3.Nodes.Add(treeNode4);
			}
			foreach (TypeDefinition type in module.Types)
			{
				if (!dictionary.ContainsKey(type.Namespace))
				{
					TreeNode treeNode5 = new TreeNode(type.Namespace);
					treeNode5.Tag = new GClass6(type.Namespace, module);
					if (Operators.CompareString(treeNode5.Text, "", TextCompare: false) == 0)
					{
						treeNode5.Text = "-";
					}
					treeNode5.ImageKey = "6VSObject_Namespace.bmp";
					treeNode5.SelectedImageKey = "6VSObject_Namespace.bmp";
					treeNode2.Nodes.Add(treeNode5);
					dictionary.Add(type.Namespace, treeNode5);
				}
				method_7(dictionary[type.Namespace], type);
			}
		}
		TreeNode treeNode6 = treeNode.Nodes.Add("Resources");
		treeNode6.ImageKey = "2VSResources.bmp";
		treeNode6.SelectedImageKey = "2VSResources_Open.bmp";
		foreach (Resource resource in assemblyDefinition.MainModule.Resources)
		{
			if (resource.ResourceType == ResourceType.Embedded)
			{
				TreeNode treeNode7 = new TreeNode(resource.Name);
				treeNode7.Tag = resource;
				treeNode6.Nodes.Add(treeNode7);
				switch (Class7.smethod_0(resource.Name, ((EmbeddedResource)resource).GetResourceData()))
				{
				default:
					treeNode7.ImageKey = "3VSProject_resourcefile.bmp";
					treeNode7.SelectedImageKey = "3VSProject_resourcefile.bmp";
					break;
				case Class7.Enum0.Xml:
					treeNode7.ImageKey = "3VSProject_xml.bmp";
					treeNode7.SelectedImageKey = "3VSProject_xml.bmp";
					break;
				case Class7.Enum0.Image:
					treeNode7.ImageKey = "3VSProject_imagefile.bmp";
					treeNode7.SelectedImageKey = "3VSProject_imagefile.bmp";
					break;
				}
			}
		}
		return treeNode;
	}

	private void method_10(TreeNode treeNode_0)
	{
		if (treeNode_0 == null)
		{
			return;
		}
		checked
		{
			for (int i = vmethod_16().Nodes.Count - 1; i >= 0; i += -1)
			{
				TreeNode treeNode = vmethod_16().Nodes[i];
				if (Operators.CompareString(treeNode.Text, treeNode_0.Text, TextCompare: false) == 0)
				{
					treeNode.Remove();
				}
			}
			vmethod_16().CollapseAll();
			vmethod_16().Nodes.Add(treeNode_0);
			vmethod_16().SelectedNode = treeNode_0;
			treeNode_0.Expand();
			if (treeNode_0.Nodes.Count > 0)
			{
				treeNode_0.Nodes[0].Expand();
			}
		}
	}

	[SpecialName]
	public string method_11()
	{
		string text = "";
		TreeNode selectedNode = vmethod_16().SelectedNode;
		if (selectedNode == null)
		{
			return "";
		}
		ArrayList arrayList = new ArrayList();
		while (selectedNode != null)
		{
			if (selectedNode.Tag != null)
			{
				arrayList.Insert(0, RuntimeHelpers.GetObjectValue(selectedNode.Tag));
			}
			selectedNode = selectedNode.Parent;
		}
		foreach (object item in arrayList)
		{
			object objectValue = RuntimeHelpers.GetObjectValue(item);
			if (objectValue is GClass0)
			{
				text = ((GClass0)objectValue).method_1().Name.FullName;
				break;
			}
		}
		foreach (object item2 in arrayList)
		{
			object objectValue2 = RuntimeHelpers.GetObjectValue(item2);
			if (objectValue2 is Resource)
			{
				text = text + "//R" + ((Resource)objectValue2).Name;
				break;
			}
		}
		foreach (object item3 in arrayList)
		{
			object objectValue3 = RuntimeHelpers.GetObjectValue(item3);
			if (objectValue3 is GClass6)
			{
				text = text + "//N" + ((GClass6)objectValue3).method_0();
				break;
			}
		}
		foreach (object item4 in arrayList)
		{
			object objectValue4 = RuntimeHelpers.GetObjectValue(item4);
			if (objectValue4 is TypeDefinition)
			{
				text = text + "//" + ((TypeDefinition)objectValue4).Name;
				break;
			}
		}
		foreach (object item5 in arrayList)
		{
			object objectValue5 = RuntimeHelpers.GetObjectValue(item5);
			if (objectValue5 is MethodDefinition)
			{
				text = text + "//M" + GClass2.smethod_4((MethodDefinition)objectValue5);
				return text;
			}
		}
		foreach (object item6 in arrayList)
		{
			object objectValue6 = RuntimeHelpers.GetObjectValue(item6);
			if (objectValue6 is PropertyDefinition)
			{
				text = text + "//P" + ((PropertyDefinition)objectValue6).Name;
				return text;
			}
		}
		foreach (object item7 in arrayList)
		{
			object objectValue7 = RuntimeHelpers.GetObjectValue(item7);
			if (objectValue7 is FieldDefinition)
			{
				text = text + "//F" + ((FieldDefinition)objectValue7).Name;
				return text;
			}
		}
		foreach (object item8 in arrayList)
		{
			object objectValue8 = RuntimeHelpers.GetObjectValue(item8);
			if (objectValue8 is EventDefinition)
			{
				return text + "//E" + ((EventDefinition)objectValue8).Name;
			}
		}
		return text;
	}

	[SpecialName]
	public void method_12(string string_0)
	{
		if (string.IsNullOrEmpty(string_0))
		{
			return;
		}
		string[] array = Regex.Split(string_0, "\\/\\/");
		TreeNode treeNode = null;
		if (array.Length > 0)
		{
			bool flag = false;
			foreach (TreeNode node in vmethod_16().Nodes)
			{
				if (Operators.CompareString(((GClass0)node.Tag).method_1().Name.FullName, array[0], TextCompare: false) == 0)
				{
					treeNode = node;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				treeNode = method_1(array[0]);
			}
		}
		if (treeNode == null)
		{
			return;
		}
		if (array.Length > 1 && array[1].StartsWith("R"))
		{
			foreach (TreeNode node2 in treeNode.Nodes)
			{
				bool flag2 = false;
				if (Operators.CompareString(node2.Text, "Resources", TextCompare: false) != 0)
				{
					continue;
				}
				foreach (TreeNode node3 in node2.Nodes)
				{
					if (Operators.CompareString(((Resource)node3.Tag).Name, array[1].Substring(1), TextCompare: false) == 0)
					{
						treeNode = node3;
						flag2 = true;
						break;
					}
				}
				if (flag2)
				{
					continue;
				}
				return;
			}
		}
		if (array.Length > 1 && array[1].StartsWith("N"))
		{
			bool flag3 = false;
			foreach (TreeNode node4 in treeNode.Nodes)
			{
				if (Operators.CompareString(node4.Text, "Resources", TextCompare: false) == 0)
				{
					continue;
				}
				foreach (TreeNode node5 in node4.Nodes)
				{
					if (node5.Tag != null && Operators.CompareString(((GClass6)node5.Tag).method_0(), array[1].Substring(1), TextCompare: false) == 0)
					{
						treeNode = node5;
						flag3 = true;
						break;
					}
				}
			}
			if (!flag3)
			{
				return;
			}
			if (array.Length > 2)
			{
				flag3 = false;
				foreach (TreeNode node6 in treeNode.Nodes)
				{
					if (Operators.CompareString(((TypeDefinition)node6.Tag).Name.Replace("<", "").Replace(">", ""), array[2], TextCompare: false) == 0)
					{
						treeNode = node6;
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					return;
				}
			}
			if (array.Length > 3 && array[3].StartsWith("M"))
			{
				flag3 = false;
				foreach (TreeNode node7 in treeNode.Nodes)
				{
					if (!(node7.Tag is MethodDefinition) || Operators.CompareString(GClass2.smethod_4((MethodDefinition)node7.Tag), array[3].Substring(1), TextCompare: false) != 0)
					{
						if (!(node7.Tag is PropertyDefinition))
						{
							continue;
						}
						foreach (TreeNode node8 in node7.Nodes)
						{
							if (node8.Tag is MethodDefinition && Operators.CompareString(GClass2.smethod_4((MethodDefinition)node8.Tag), array[3].Substring(1), TextCompare: false) == 0)
							{
								treeNode = node8;
								flag3 = true;
								break;
							}
						}
						if (flag3)
						{
							break;
						}
						continue;
					}
					treeNode = node7;
					flag3 = true;
					break;
				}
				if (!flag3)
				{
					return;
				}
			}
			if (array.Length > 3 && array[3].StartsWith("P"))
			{
				flag3 = false;
				foreach (TreeNode node9 in treeNode.Nodes)
				{
					if (node9.Tag is PropertyDefinition && Operators.CompareString(((PropertyDefinition)node9.Tag).Name, array[3].Substring(1), TextCompare: false) == 0)
					{
						treeNode = node9;
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					return;
				}
			}
			if (array.Length > 3 && array[3].StartsWith("F"))
			{
				flag3 = false;
				foreach (TreeNode node10 in treeNode.Nodes)
				{
					if (node10.Tag is FieldDefinition && Operators.CompareString(((FieldDefinition)node10.Tag).Name, array[3].Substring(1), TextCompare: false) == 0)
					{
						treeNode = node10;
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					return;
				}
			}
			if (array.Length > 3 && array[3].StartsWith("E"))
			{
				flag3 = false;
				foreach (TreeNode node11 in treeNode.Nodes)
				{
					if (node11.Tag is EventDefinition && Operators.CompareString(((EventDefinition)node11.Tag).Name, array[3].Substring(1), TextCompare: false) == 0)
					{
						treeNode = node11;
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					return;
				}
			}
		}
		if (treeNode != null)
		{
			vmethod_16().SelectedNode = treeNode;
			treeNode.EnsureVisible();
		}
	}

	private void method_13()
	{
		if (gclass7_0.method_3())
		{
			bool_0 = true;
			method_12(gclass7_0.method_5());
			bool_0 = false;
		}
	}

	private void method_14()
	{
		if (gclass7_0.method_2())
		{
			bool_0 = true;
			method_12(gclass7_0.method_4());
			bool_0 = false;
		}
	}

	private void method_15()
	{
		object obj = null;
		try
		{
			object objectValue = RuntimeHelpers.GetObjectValue(vmethod_16().SelectedNode.Tag);
			if (objectValue is MethodDefinition)
			{
				vmethod_67().Text = string.Format("{0}::{1}", ((MethodDefinition)objectValue).DeclaringType.FullName, ((MethodDefinition)objectValue).Name);
			}
			else if (objectValue is FieldDefinition)
			{
				vmethod_67().Text = string.Format("{0}::{1}", ((FieldDefinition)objectValue).DeclaringType.FullName, ((FieldDefinition)objectValue).Name);
			}
			else if (objectValue is PropertyDefinition)
			{
				vmethod_67().Text = string.Format("{0}::{1}", ((PropertyDefinition)objectValue).DeclaringType.FullName, ((PropertyDefinition)objectValue).Name);
			}
			else if (objectValue is EventDefinition)
			{
				vmethod_67().Text = string.Format("{0}::{1}", ((EventDefinition)objectValue).DeclaringType.FullName, ((EventDefinition)objectValue).Name);
			}
			else if (objectValue is TypeDefinition)
			{
				vmethod_67().Text = string.Format("{0}", ((TypeDefinition)objectValue).FullName);
			}
			else if (objectValue is GClass6)
			{
				vmethod_67().Text = string.Format("{0}", ((GClass6)objectValue).method_0());
			}
			else
			{
				vmethod_67().Text = "";
			}
		}
		finally
		{
			if (obj != null)
			{
				((IDisposable)obj).Dispose();
			}
		}
		string text = method_11();
		if (!bool_0 && !string.IsNullOrEmpty(text))
		{
			gclass7_0.method_1(text);
		}
		vmethod_24().Enabled = gclass7_0.method_2();
		vmethod_61().Enabled = gclass7_0.method_2();
		vmethod_26().Enabled = gclass7_0.method_3();
		vmethod_63().Enabled = gclass7_0.method_3();
		Class6.smethod_0().LastSelectedNodePath = text;
		TreeNode selectedNode = vmethod_16().SelectedNode;
		while (selectedNode != null && selectedNode.Parent != null)
		{
			selectedNode = selectedNode.Parent;
		}
		if (selectedNode != null && !string.IsNullOrEmpty(((GClass0)selectedNode.Tag).method_0()) && File.Exists(((GClass0)selectedNode.Tag).method_0()))
		{
			Class6.smethod_0().LastFile = ((GClass0)selectedNode.Tag).method_0();
		}
		Class6.smethod_0().Save();
	}

	private void method_16(object sender, TreeViewEventArgs e)
	{
		if (e.Node == null)
		{
			return;
		}
		method_15();
		object objectValue = RuntimeHelpers.GetObjectValue(e.Node.Tag);
		if (objectValue is EmbeddedResource)
		{
			EmbeddedResource embeddedResource = (EmbeddedResource)objectValue;
			switch (Class7.smethod_0(embeddedResource.Name, embeddedResource.GetResourceData()))
			{
			case Class7.Enum0.Binary:
				vmethod_30().Visible = true;
				vmethod_20().Visible = false;
				vmethod_28().Visible = false;
				vmethod_30().Text = Class7.smethod_4(embeddedResource.GetResourceData());
				break;
			case Class7.Enum0.Text:
			case Class7.Enum0.Xml:
				vmethod_30().Visible = true;
				vmethod_20().Visible = false;
				vmethod_28().Visible = false;
				vmethod_30().Text = Class7.smethod_3(embeddedResource.GetResourceData());
				break;
			case Class7.Enum0.Image:
				vmethod_28().Visible = true;
				vmethod_20().Visible = false;
				vmethod_30().Visible = false;
				vmethod_28().BackgroundImage = Class7.smethod_5(embeddedResource.GetResourceData());
				break;
			case Class7.Enum0.ResX:
				vmethod_30().Visible = true;
				vmethod_20().Visible = false;
				vmethod_28().Visible = false;
				vmethod_30().Text = Class7._003C(embeddedResource.GetResourceData());
				break;
			}
		}
		else
		{
			vmethod_20().Visible = true;
			vmethod_30().Visible = false;
			vmethod_28().Visible = false;
			if (objectValue == null)
			{
				vmethod_20().method_5();
			}
			else if (objectValue is string)
			{
				vmethod_20().Text = Conversions.ToString(objectValue);
			}
			else if (objectValue is MethodDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_4((MethodDefinition)objectValue);
			}
			else if (objectValue is FieldDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_6((FieldDefinition)objectValue);
			}
			else if (objectValue is PropertyDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_5((PropertyDefinition)objectValue);
			}
			else if (objectValue is GClass0)
			{
				vmethod_20().Text = gclass2_0.vmethod_0((GClass0)objectValue);
			}
			else if (objectValue is ModuleDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_3((ModuleDefinition)objectValue);
			}
			else if (objectValue is EventDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_7((EventDefinition)objectValue);
			}
			else if (objectValue is TypeDefinition)
			{
				vmethod_20().Text = gclass2_0.vmethod_2((TypeDefinition)objectValue);
			}
			else if (objectValue is GClass6)
			{
				vmethod_20().Text = gclass2_0.vmethod_1((GClass6)objectValue);
			}
			else
			{
				vmethod_20().method_5();
			}
		}
	}

	private void method_17(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
			TreeNode treeNode = null;
			vmethod_16().BeginUpdate();
			string[] array2 = array;
			foreach (string string_ in array2)
			{
				treeNode = method_8(string_);
				method_10(treeNode);
			}
			vmethod_16().EndUpdate();
		}
	}

	private void method_18(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effect = DragDropEffects.All;
		}
	}

	private void method_19(object sender, TreeNodeMouseClickEventArgs e)
	{
		if (vmethod_16().SelectedNode != e.Node)
		{
			vmethod_16().SelectedNode = e.Node;
		}
	}

	private void method_20(object sender, LinkClickedEventArgs e)
	{
		method_12(e.LinkText);
	}

	private void method_21(object sender, PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Rectangle bounds = new Rectangle(0, 0, vmethod_18().Width, vmethod_18().Height);
		ControlPaint.DrawVisualStyleBorder(graphics, bounds);
	}

	private void method_22(object sender, EventArgs e)
	{
		method_32(null, null);
	}

	private void method_23(object sender, EventArgs e)
	{
		method_34(null, null);
	}

	private void method_24(object sender, EventArgs e)
	{
		method_14();
	}

	private void method_25(object sender, EventArgs e)
	{
		method_13();
	}

	private void _003C(object sender, EventArgs e)
	{
		Class6.smethod_0().SelectedLanguage = vmethod_11().SelectedIndex;
		gclass2_0 = (GClass2)vmethod_11().SelectedItem;
		method_16(vmethod_16(), new TreeViewEventArgs(vmethod_16().SelectedNode));
	}

	private void method_26(object sender, CancelEventArgs e)
	{
		if (vmethod_16().SelectedNode == null)
		{
			e.Cancel = true;
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (vmethod_16().SelectedNode.Tag is Resource)
		{
			flag = true;
		}
		if (vmethod_16().SelectedNode.Tag is GClass0)
		{
			flag2 = true;
		}
		vmethod_34().Enabled = gclass7_0.method_2();
		vmethod_36().Enabled = gclass7_0.method_3();
		vmethod_41().Visible = flag | flag2;
		vmethod_43().Visible = flag;
		vmethod_65().Visible = flag2;
	}

	private void method_27(object sender, EventArgs e)
	{
		method_14();
	}

	private void method_28(object sender, EventArgs e)
	{
		method_13();
	}

	private void _0024(object sender, EventArgs e)
	{
		Class1.smethod_0().Clipboard.SetText(vmethod_16().SelectedNode.Text);
	}

	private void method_29(object sender, EventArgs e)
	{
		try
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.OverwritePrompt = true;
			saveFileDialog.Filter = "All Files (*.*)|*.*";
			saveFileDialog.FileName = vmethod_16().SelectedNode.Text;
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
				BinaryWriter binaryWriter = new BinaryWriter(fileStream);
				binaryWriter.Write(((EmbeddedResource)vmethod_16().SelectedNode.Tag).GetResourceData());
				binaryWriter.Close();
				fileStream.Close();
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			MessageBox.Show(this, ex2.Message, "Unable to save file.", MessageBoxButtons.OK);
			ProjectData.ClearProjectError();
		}
	}

	private void method_30(object sender, EventArgs e)
	{
		vmethod_16().SelectedNode.Remove();
		gclass7_0.method_0();
		vmethod_24().Enabled = gclass7_0.method_2();
		vmethod_61().Enabled = gclass7_0.method_2();
		vmethod_26().Enabled = gclass7_0.method_3();
		vmethod_63().Enabled = gclass7_0.method_3();
		vmethod_20().Visible = true;
		vmethod_30().Visible = false;
		vmethod_28().Visible = false;
		vmethod_20().method_5();
	}

	private void method_31(object sender, EventArgs e)
	{
		GForm0 gForm = new GForm0();
		gForm.ShowDialog();
	}

	private void method_32(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			TreeNode treeNode = null;
			vmethod_16().BeginUpdate();
			treeNode = method_8(openFileDialog.FileName);
			method_10(treeNode);
			vmethod_16().EndUpdate();
		}
	}

	private void method_33(object sender, EventArgs e)
	{
		Application.Exit();
	}

	private void method_34(object sender, EventArgs e)
	{
		string string_ = method_11();
		StringCollection stringCollection = new StringCollection();
		vmethod_20().method_5();
		foreach (TreeNode node in vmethod_16().Nodes)
		{
			stringCollection.Add(((GClass0)node.Tag).method_0());
		}
		vmethod_16().Nodes.Clear();
		Refresh();
		vmethod_16().BeginUpdate();
		StringEnumerator enumerator2 = stringCollection.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			string current = enumerator2.Current;
			vmethod_16().Nodes.Add(method_8(current));
		}
		vmethod_16().EndUpdate();
		method_12(string_);
	}

	private void method_35(object sender, EventArgs e)
	{
		method_14();
	}

	private void method_36(object sender, EventArgs e)
	{
		method_13();
	}

	private void method_37(object sender, EventArgs e)
	{
		vmethod_58().Enabled = true;
		vmethod_61().Enabled = true;
		vmethod_63().Enabled = true;
	}

	private void method_38(object sender, EventArgs e)
	{
		vmethod_58().Enabled = vmethod_16().Nodes.Count > 0;
		vmethod_61().Enabled = gclass7_0.method_2();
		vmethod_63().Enabled = gclass7_0.method_3();
	}
}
