using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

[SuppressIldasm]
public sealed class GClass11 : GClass10
{
	[SuppressIldasm]
	public delegate void _0024(object sender, LinkClickedEventArgs e);

	private GClass15 gclass15_0;

	private GClass13 gclass13_0;

	private int int_0;

	private int int_1;

	private int int_2;

	private int m__003C;

	private StringFormat stringFormat_0;

	private TextFormatFlags textFormatFlags_0;

	private readonly Color color_0;

	private readonly Color color_1;

	private readonly Color color_2;

	private IContainer m__0024;

	[AccessedThroughProperty("ContextMenuStrip1")]
	private ContextMenuStrip contextMenuStrip_0;

	[AccessedThroughProperty("ToolStripMenuItemCopy")]
	private ToolStripMenuItem toolStripMenuItem_0;

	[AccessedThroughProperty("ToolStripMenuItemSelectAll")]
	private ToolStripMenuItem toolStripMenuItem_1;

	private _0024 _0024_0;

	public override Font Font
	{
		get
		{
			return base.Font;
		}
		set
		{
			base.Font = value;
			int_2 = value.Height;
			string text = Class8.smethod_347();
			Size proposedSize = new Size(1000, 100);
			int num = TextRenderer.MeasureText(text, value, proposedSize, textFormatFlags_0).Width;
			string text2 = Class8.smethod_348();
			Size proposedSize2 = new Size(1000, 100);
			this.m__003C = checked(num - TextRenderer.MeasureText(text2, value, proposedSize2, textFormatFlags_0).Width);
		}
	}

	public override string Text
	{
		get
		{
			return gclass13_0.ToString();
		}
		set
		{
			gclass13_0.Clear();
			checked
			{
				if (!string.IsNullOrEmpty(value))
				{
					value = value.Replace(Environment.NewLine, "\r");
					string[] array = value.Split('\r');
					Color black = Color.Black;
					string string_ = "";
					string[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						string text = array2[i];
						GClass14 gClass = new GClass14();
						text = text.Replace("<<>>", "<<?>>");
						string[] array3 = Regex.Split(text, "(\\<\\<[r|g|b|\\?|\\#].*?\\>\\>)");
						string[] array4 = array3;
						foreach (string text2 in array4)
						{
							if (Operators.CompareString(text2, "<<?>>", TextCompare: false) == 0)
							{
								black = Color.Black;
								string_ = "";
								continue;
							}
							if (text2.StartsWith("<<#"))
							{
								black = Color.Black;
								string_ = text2.Substring(3, text2.Length - 5);
								continue;
							}
							switch (text2)
							{
							case "<<b>>":
								black = color_1;
								string_ = "";
								break;
							case "<<r>>":
								black = color_0;
								string_ = "";
								break;
							case "<<g>>":
								black = color_2;
								string_ = "";
								break;
							default:
							{
								GClass15 gClass2 = new GClass15(text2);
								gClass2.method_7(black);
								gClass2.method_5(string_);
								gClass.method_0().Add(gClass2);
								break;
							}
							}
						}
						gclass13_0.Add(gClass);
					}
				}
				Point autoScrollPosition = new Point(0, 0);
				AutoScrollPosition = autoScrollPosition;
				int_0 = 0;
				int_1 = 0;
				method_6();
				method_7();
				Invalidate();
			}
		}
	}

	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual ContextMenuStrip vmethod_0()
	{
		return contextMenuStrip_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_1(ContextMenuStrip contextMenuStrip_1)
	{
		CancelEventHandler value = method_11;
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
	internal virtual ToolStripMenuItem vmethod_2()
	{
		return toolStripMenuItem_0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	internal virtual void vmethod_3(ToolStripMenuItem toolStripMenuItem_2)
	{
		EventHandler value = method_9;
		if (toolStripMenuItem_0 != null)
		{
			toolStripMenuItem_0.Click -= value;
		}
		toolStripMenuItem_0 = toolStripMenuItem_2;
		if (toolStripMenuItem_0 != null)
		{
			toolStripMenuItem_0.Click += value;
		}
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
	internal virtual void vmethod_5(ToolStripMenuItem toolStripMenuItem_2)
	{
		EventHandler value = method_10;
		if (toolStripMenuItem_1 != null)
		{
			toolStripMenuItem_1.Click -= value;
		}
		toolStripMenuItem_1 = toolStripMenuItem_2;
		if (toolStripMenuItem_1 != null)
		{
			toolStripMenuItem_1.Click += value;
		}
	}

	public GClass11()
	{
		gclass15_0 = null;
		gclass13_0 = new GClass13();
		int_0 = -1;
		int_1 = 0;
		stringFormat_0 = (StringFormat)StringFormat.GenericDefault.Clone();
		textFormatFlags_0 = TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
		color_0 = Color.FromArgb(163, 21, 21);
		color_1 = Color.FromArgb(0, 0, 255);
		color_2 = Color.FromArgb(0, 128, 0);
		method_14();
		BackColor = SystemColors.Window;
		DoubleBuffered = true;
		Font = new Font(Class8.smethod_158(), 8.5f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.UserMouse | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		TabStop = true;
	}

	protected override void OnMouseClick(MouseEventArgs e)
	{
		base.OnMouseClick(e);
		Focus();
	}

	public void method_0(string string_0)
	{
		gclass13_0.Clear();
		if (!string.IsNullOrEmpty(string_0))
		{
			string_0 = string_0.Replace(Environment.NewLine, "\r");
			string[] array = string_0.Split('\r');
			_ = Color.Black;
			string[] array2 = array;
			foreach (string text in array2)
			{
				GClass14 gClass = new GClass14();
				GClass15 gClass2 = new GClass15(text);
				gClass2.method_7(Color.Black);
				gClass2.method_5("");
				gClass.method_0().Add(gClass2);
				gclass13_0.Add(gClass);
			}
		}
		int_0 = 0;
		int_1 = 0;
		method_6();
		method_7();
		Invalidate();
	}

	[SpecialName]
	public int method_1()
	{
		if (int_1 < 0)
		{
			return checked(int_0 + int_1);
		}
		return int_0;
	}

	[SpecialName]
	public int method_2()
	{
		if (int_1 < 0)
		{
			return checked(int_1 * -1);
		}
		return int_1;
	}

	[SpecialName]
	public string _003C()
	{
		string text = "";
		int num = method_1();
		checked
		{
			int num2 = num + method_2();
			int num3 = 0;
			foreach (GClass14 item in gclass13_0)
			{
				if (item.method_1() + item.method_3() < num)
				{
					num3 = item.method_1() + item.method_3();
					continue;
				}
				foreach (GClass15 item2 in item.method_0())
				{
					string text2 = item2.method_0();
					int i = 0;
					for (int length = text2.Length; i < length; i++)
					{
						char c = text2[i];
						if (num3 < num2)
						{
							if (num3 >= num)
							{
								text += Conversions.ToString(c);
							}
							num3++;
							continue;
						}
						return text;
					}
				}
				text += Environment.NewLine;
			}
			return text;
		}
	}

	[SpecialName]
	public GClass15 method_3()
	{
		return gclass15_0;
	}

	public void method_4()
	{
		int_0 = 0;
		int_1 = 0;
		if (gclass13_0.Count > 0)
		{
			int_1 = checked(gclass13_0[gclass13_0.Count - 1].method_1() + gclass13_0[gclass13_0.Count - 1].method_3());
		}
		Invalidate();
	}

	public void _0024()
	{
		if (int_1 == 0)
		{
			return;
		}
		string text = _003C();
		try
		{
			Clipboard.SetText(text, TextDataFormat.Text);
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public void method_5()
	{
		Text = "";
	}

	private void method_6()
	{
		int num = 0;
		checked
		{
			foreach (GClass14 item in gclass13_0)
			{
				item.method_2(num);
				foreach (GClass15 item2 in item.method_0())
				{
					num += item2.method_0().Length;
				}
				item.method_4(num - item.method_1());
			}
		}
	}

	private void method_7()
	{
		checked
		{
			int num = gclass13_0.Count * int_2;
			int num2 = 0;
			foreach (GClass14 item in gclass13_0)
			{
				num2 = Math.Max(num2, item.method_3());
			}
			num2 *= this.m__003C;
			Point autoScrollPosition = new Point(0, 0);
			AutoScrollPosition = autoScrollPosition;
			Size autoScrollMinSize = new Size(num2, num);
			AutoScrollMinSize = autoScrollMinSize;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
		int num = 0;
		int num2 = 0;
		Font font = Font;
		Font font2 = Font;
		Font font3 = new Font(Font, FontStyle.Underline);
		_ = Color.Black;
		_ = SystemColors.HighlightText;
		SolidBrush brush = (SolidBrush)SystemBrushes.Highlight;
		int num3 = method_1();
		checked
		{
			int num4 = num3 + method_2();
			Rectangle clipRectangle = e.ClipRectangle;
			clipRectangle.Offset(AutoScrollPosition.X * -1, AutoScrollPosition.Y * -1);
			clipRectangle.Inflate(2, 2);
			int num5 = 0;
			foreach (GClass14 item in gclass13_0)
			{
				if (num + int_2 >= clipRectangle.Y && num <= clipRectangle.Bottom)
				{
					num5 = item.method_1();
					foreach (GClass15 item2 in item.method_0())
					{
						Font font4 = font;
						if (!string.IsNullOrEmpty(item2.method_4()))
						{
							font4 = ((gclass15_0 != item2) ? font2 : font3);
						}
						string text = item2.method_0();
						int i = 0;
						for (int length = text.Length; i < length; i++)
						{
							char c = text[i];
							Rectangle rectangle = new Rectangle(num2, num, this.m__003C, int_2);
							if (clipRectangle.IntersectsWith(rectangle))
							{
								if (num5 >= num3 && num5 < num4)
								{
									graphics.FillRectangle(brush, rectangle);
									rectangle.Offset(AutoScrollPosition);
									TextRenderer.DrawText(graphics, Conversions.ToString(c), font4, rectangle, SystemColors.HighlightText, SystemColors.Highlight, textFormatFlags_0);
								}
								else
								{
									rectangle.Offset(AutoScrollPosition);
									TextRenderer.DrawText(graphics, Conversions.ToString(c), font4, rectangle, item2.method_6(), textFormatFlags_0);
								}
							}
							num2 += this.m__003C;
							num5++;
						}
					}
					num2 = 0;
				}
				num += int_2;
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		GClass15 gclass15_ = null;
		checked
		{
			Point point_ = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
			int num = method_8(point_, ref gclass15_);
			if (e.Button == MouseButtons.Left && Capture && num - int_0 != int_1)
			{
				int_1 = num - int_0;
				Invalidate();
			}
			if (gclass15_0 != gclass15_)
			{
				Rectangle rc;
				if (gclass15_0 != null)
				{
					rc = gclass15_0.method_2();
					Rectangle rc2 = new Rectangle(rc.X + AutoScrollPosition.X, gclass15_0.method_2().Y + AutoScrollPosition.Y, gclass15_0.method_2().Width, gclass15_0.method_2().Height);
					Invalidate(rc2);
				}
				gclass15_0 = gclass15_;
				if (gclass15_0 != null)
				{
					rc = new Rectangle(gclass15_0.method_2().X + AutoScrollPosition.X, gclass15_0.method_2().Y + AutoScrollPosition.Y, gclass15_0.method_2().Width, gclass15_0.method_2().Height);
					Invalidate(rc);
				}
			}
			if (gclass15_0 != null && !(Cursor == Cursors.Hand))
			{
				Cursor = Cursors.Hand;
			}
			if (gclass15_0 == null && !(Cursor == Cursors.IBeam))
			{
				Cursor = Cursors.IBeam;
			}
			base.OnMouseMove(e);
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (gclass15_0 != null)
		{
			Cursor = Cursors.Default;
			gclass15_0 = null;
			Invalidate();
		}
		base.OnMouseLeave(e);
	}

	private int method_8(Point point_0, ref GClass15 gclass15_1)
	{
		int num = point_0.Y / int_2;
		int num2 = 0;
		checked
		{
			if (num < 0)
			{
				num2 = 0;
			}
			else if (num >= gclass13_0.Count)
			{
				if (gclass13_0.Count > 0)
				{
					num2 = gclass13_0[gclass13_0.Count - 1].method_1();
					foreach (GClass15 item in gclass13_0[gclass13_0.Count - 1].method_0())
					{
						num2 += item.method_0().Length;
					}
				}
			}
			else
			{
				int num3 = 0;
				num2 = gclass13_0[num].method_1();
				foreach (GClass15 item2 in gclass13_0[num].method_0())
				{
					if (point_0.X < num3 || point_0.X >= num3 + item2.method_0().Length * this.m__003C)
					{
						num2 += item2.method_0().Length;
						num3 += item2.method_0().Length * this.m__003C;
						continue;
					}
					if (!string.IsNullOrEmpty(item2.method_4()))
					{
						gclass15_1 = item2;
						GClass15 obj = gclass15_1;
						Rectangle rectangle_ = new Rectangle(num3, num * int_2, item2.method_0().Length * this.m__003C, int_2 + 5);
						obj.method_3(rectangle_);
					}
					string text = item2.method_0();
					int i = 0;
					for (int length = text.Length; i < length; i++)
					{
						_ = text[i];
						if (point_0.X < num3 || point_0.X >= num3 + this.m__003C)
						{
							num2++;
							num3 += this.m__003C;
							continue;
						}
						break;
					}
					break;
				}
			}
			return num2;
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			int_1 = 0;
			Point point = checked(new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y));
			Point point_ = point;
			GClass15 gclass15_ = null;
			int_0 = method_8(point_, ref gclass15_);
			Invalidate();
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		checked
		{
			Point autoScrollPosition = new Point(AutoScrollPosition.X, unchecked(AutoScrollPosition.Y / int_2) * int_2 * -1 + int_2 * e.Delta);
			AutoScrollPosition = autoScrollPosition;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (!Capture)
		{
			return;
		}
		checked
		{
			if (e.Button == MouseButtons.Left)
			{
				GClass15 gclass15_ = null;
				Point point_ = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
				int num = method_8(point_, ref gclass15_);
				if (num - int_0 != int_1)
				{
					int_1 = num - int_0;
					Invalidate();
				}
				if (int_1 == 0 && gclass15_ != null && !string.IsNullOrEmpty(gclass15_.method_4()))
				{
					_0024_0?.Invoke(this, new LinkClickedEventArgs(gclass15_.method_4()));
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				vmethod_0().Show(this, e.Location);
			}
			base.OnMouseUp(e);
		}
	}

	protected override bool IsInputKey(Keys keyData)
	{
		return keyData switch
		{
			Keys.Down => true, 
			Keys.Up => true, 
			Keys.Left => true, 
			Keys.Right => true, 
			Keys.Home => true, 
			Keys.End => true, 
			Keys.Next => true, 
			Keys.Prior => true, 
			_ => base.IsInputKey(keyData), 
		};
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.Control & (e.KeyCode == Keys.C))
		{
			_0024();
		}
		if (e.Control & (e.KeyCode == Keys.A))
		{
			method_4();
		}
		checked
		{
			Point point;
			Point autoScrollPosition;
			if (e.KeyCode == Keys.Down)
			{
				point = AutoScrollPosition;
				int num = point.X;
				autoScrollPosition = AutoScrollPosition;
				Point autoScrollPosition2 = new Point(num, unchecked(autoScrollPosition.Y / int_2) * int_2 * -1 + int_2);
				AutoScrollPosition = autoScrollPosition2;
			}
			if (e.KeyCode == Keys.Up)
			{
				point = AutoScrollPosition;
				autoScrollPosition = new Point(point.X, unchecked(AutoScrollPosition.Y / int_2) * int_2 * -1 - int_2);
				AutoScrollPosition = autoScrollPosition;
			}
			if (e.KeyCode == Keys.Left)
			{
				int num2 = unchecked(AutoScrollPosition.X / this.m__003C) * this.m__003C * -1 - this.m__003C;
				point = AutoScrollPosition;
				autoScrollPosition = new Point(num2, point.Y);
				AutoScrollPosition = autoScrollPosition;
			}
			if (e.KeyCode == Keys.Right)
			{
				int num3 = unchecked(AutoScrollPosition.X / this.m__003C) * this.m__003C * -1 + this.m__003C;
				point = AutoScrollPosition;
				autoScrollPosition = new Point(num3, point.Y);
				AutoScrollPosition = autoScrollPosition;
			}
			if (e.KeyCode == Keys.Home)
			{
				point = (AutoScrollPosition = new Point(AutoScrollPosition.X * -1, 0));
			}
			if (e.KeyCode == Keys.End)
			{
				point = (AutoScrollPosition = new Point(AutoScrollPosition.X * -1, gclass13_0.Count * int_2));
			}
			if (e.KeyCode == Keys.Next)
			{
				autoScrollPosition = (AutoScrollPosition = new Point(AutoScrollPosition.X, unchecked(AutoScrollPosition.Y / int_2) * int_2 * -1 + unchecked(Height / int_2) * int_2));
			}
			if (e.KeyCode == Keys.Prior)
			{
				autoScrollPosition = (AutoScrollPosition = new Point(AutoScrollPosition.X, unchecked(AutoScrollPosition.Y / int_2) * int_2 * -1 - unchecked(Height / int_2) * int_2));
			}
			base.OnKeyDown(e);
		}
	}

	private void method_9(object sender, EventArgs e)
	{
		_0024();
	}

	private void method_10(object sender, EventArgs e)
	{
		method_4();
	}

	private void method_11(object sender, CancelEventArgs e)
	{
		vmethod_2().Enabled = method_2() > 0;
		vmethod_4().Enabled = gclass13_0.Count > 0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	public void method_12(_0024 _0024_1)
	{
		_0024_0 = (_0024)Delegate.Combine(_0024_0, _0024_1);
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	[SpecialName]
	[DebuggerNonUserCode]
	public void method_13(_0024 _0024_1)
	{
		_0024_0 = (_0024)Delegate.Remove(_0024_0, _0024_1);
	}

	private void method_14()
	{
		this.m__0024 = new Container();
		vmethod_1(new ContextMenuStrip(this.m__0024));
		vmethod_5(new ToolStripMenuItem());
		vmethod_3(new ToolStripMenuItem());
		vmethod_0().SuspendLayout();
		SuspendLayout();
		vmethod_0().Items.AddRange(new ToolStripItem[2]
		{
			vmethod_2(),
			vmethod_4()
		});
		vmethod_0().Name = Class8.smethod_215();
		ContextMenuStrip contextMenuStrip = vmethod_0();
		Size size = new Size(153, 70);
		contextMenuStrip.Size = size;
		vmethod_4().Name = Class8.smethod_353();
		ToolStripMenuItem toolStripMenuItem = vmethod_4();
		size = new Size(152, 22);
		toolStripMenuItem.Size = size;
		vmethod_4().Text = Class8.smethod_349();
		vmethod_2().Name = Class8.smethod_350();
		ToolStripMenuItem toolStripMenuItem2 = vmethod_2();
		size = new Size(152, 22);
		toolStripMenuItem2.Size = size;
		vmethod_2().Text = Class8.smethod_351();
		Name = Class8.smethod_352();
		vmethod_0().ResumeLayout(performLayout: false);
		ResumeLayout(performLayout: false);
	}
}
