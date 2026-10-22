using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

[SuppressIldasm]
[DesignerGenerated]
public sealed class GForm0 : Form
{
	private IContainer icontainer_0;

	[AccessedThroughProperty("ButtonClose")]
	private Button button_0;

	[AccessedThroughProperty("PictureBox1")]
	private PictureBox pictureBox_0;

	[AccessedThroughProperty("Label1")]
	private Label label_0;

	[AccessedThroughProperty("LabelInfo")]
	private Label label_1;

	[AccessedThroughProperty("FlowLayoutPanel1")]
	private FlowLayoutPanel m__003C;

	[AccessedThroughProperty("LabelCopyright")]
	private Label label_2;

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
		ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(GForm0));
		vmethod_1(new Button());
		vmethod_5(new Label());
		vmethod_7(new Label());
		vmethod_3(new PictureBox());
		vmethod_8(new FlowLayoutPanel());
		vmethod_10(new Label());
		((ISupportInitialize)vmethod_2()).BeginInit();
		_003C().SuspendLayout();
		SuspendLayout();
		vmethod_0().Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		vmethod_0().DialogResult = DialogResult.Cancel;
		Button button = vmethod_0();
		Point location = new Point(375, 270);
		button.Location = location;
		vmethod_0().Name = "ButtonClose";
		Button button2 = vmethod_0();
		Size size = new Size(75, 23);
		button2.Size = size;
		vmethod_0().TabIndex = 0;
		vmethod_0().Text = "Close";
		vmethod_0().UseVisualStyleBackColor = true;
		vmethod_4().Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		vmethod_4().AutoSize = true;
		Label label = vmethod_4();
		location = new Point(9, 212);
		label.Location = location;
		vmethod_4().Name = "Label1";
		Label label2 = vmethod_4();
		size = new Size(427, 52);
		label2.Size = size;
		vmethod_4().TabIndex = 2;
		vmethod_4().Text = componentResourceManager.GetString("Label1.Text");
		vmethod_6().AutoSize = true;
		Label label3 = vmethod_6();
		location = new Point(3, 0);
		label3.Location = location;
		vmethod_6().Name = "LabelInfo";
		Label label4 = vmethod_6();
		size = new Size(140, 13);
		label4.Size = size;
		vmethod_6().TabIndex = 3;
		vmethod_6().Text = "<ProductName> (<Version>)";
		vmethod_2().BackColor = Color.White;
		vmethod_2().Dock = DockStyle.Top;
		vmethod_2().Image = Class5.smethod_6();
		PictureBox pictureBox = vmethod_2();
		location = new Point(0, 0);
		pictureBox.Location = location;
		PictureBox pictureBox2 = vmethod_2();
		Padding margin = new Padding(0);
		pictureBox2.Margin = margin;
		vmethod_2().Name = "PictureBox1";
		PictureBox pictureBox3 = vmethod_2();
		size = new Size(459, 128);
		pictureBox3.Size = size;
		vmethod_2().TabIndex = 1;
		vmethod_2().TabStop = false;
		_003C().Controls.Add(vmethod_6());
		_003C().Controls.Add(vmethod_9());
		_003C().FlowDirection = FlowDirection.TopDown;
		FlowLayoutPanel flowLayoutPanel = _003C();
		location = new Point(8, 137);
		flowLayoutPanel.Location = location;
		_003C().Name = "FlowLayoutPanel1";
		FlowLayoutPanel flowLayoutPanel2 = _003C();
		size = new Size(439, 67);
		flowLayoutPanel2.Size = size;
		_003C().TabIndex = 4;
		vmethod_9().AutoSize = true;
		Label label5 = vmethod_9();
		location = new Point(3, 13);
		label5.Location = location;
		vmethod_9().Name = "LabelCopyright";
		Label label6 = vmethod_9();
		size = new Size(68, 13);
		label6.Size = size;
		vmethod_9().TabIndex = 4;
		vmethod_9().Text = "<CopyRight>";
		AcceptButton = vmethod_0();
		SizeF sizeF = new SizeF(6f, 13f);
		AutoScaleDimensions = sizeF;
		AutoScaleMode = AutoScaleMode.Font;
		AutoSize = true;
		CancelButton = vmethod_0();
		size = new Size(459, 302);
		ClientSize = size;
		Controls.Add(_003C());
		Controls.Add(vmethod_4());
		Controls.Add(vmethod_2());
		Controls.Add(vmethod_0());
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "AboutDialog";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "About";
		((ISupportInitialize)vmethod_2()).EndInit();
		_003C().ResumeLayout(performLayout: false);
		_003C().PerformLayout();
		ResumeLayout(performLayout: false);
		PerformLayout();
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual Button vmethod_0()
	{
		return button_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_1(Button button_1)
	{
		button_0 = button_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual PictureBox vmethod_2()
	{
		return pictureBox_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_3(PictureBox pictureBox_1)
	{
		pictureBox_0 = pictureBox_1;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual Label vmethod_4()
	{
		return label_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_5(Label label_3)
	{
		label_0 = label_3;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual Label vmethod_6()
	{
		return label_1;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_7(Label label_3)
	{
		label_1 = label_3;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual FlowLayoutPanel _003C()
	{
		return this.m__003C;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_8(FlowLayoutPanel flowLayoutPanel_0)
	{
		this.m__003C = flowLayoutPanel_0;
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual Label vmethod_9()
	{
		return label_2;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_10(Label label_3)
	{
		label_2 = label_3;
	}

	public GForm0()
	{
		method_0();
		vmethod_6().Text = vmethod_6().Text.Replace("<ProductName>", Class1.smethod_1().Info.ProductName).Replace("<Version>", Class1.smethod_1().Info.Version.ToString(3)).Replace("<CopyRight>", Class1.smethod_1().Info.Copyright);
		vmethod_6().Font = new Font(vmethod_6().Font, FontStyle.Bold);
		vmethod_9().Text = vmethod_9().Text.Replace("<ProductName>", Class1.smethod_1().Info.ProductName).Replace("<Version>", Class1.smethod_1().Info.Version.ToString(3)).Replace("<CopyRight>", Class1.smethod_1().Info.Copyright);
	}
}
