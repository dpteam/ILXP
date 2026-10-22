using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace ILXP
{
    public sealed class UiThread
    {
    	public delegate object ObjectFunc(object object_0);

    	public delegate TreeNode TreeNodeFunc(string string_0);

    	public sealed class LoadingOverlay : Control
    	{
    		private const int int_0 = 12;

    		private const int int_1 = 8;

    		private const int int_2 = 5;

    		private static int int_3 = 0;

    		private const int int_4 = 20;

    		private Point[] _003C;

    		public LoadingOverlay()
    		{
    			_003C = new Point[8];
    			Size size = new Size(96, 40);
    			Size = size;
    			SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
    			BackColor = Color.White;
    			ForeColor = Color.Blue;
    			ref Point reference = ref _003C[0];
    			reference = new Point(20, 8);
    			ref Point reference2 = ref _003C[1];
    			reference2 = new Point(28, 12);
    			ref Point reference3 = ref _003C[2];
    			reference3 = new Point(32, 20);
    			ref Point reference4 = ref _003C[3];
    			reference4 = new Point(28, 28);
    			ref Point reference5 = ref _003C[4];
    			reference5 = new Point(20, 32);
    			ref Point reference6 = ref _003C[5];
    			reference6 = new Point(12, 28);
    			ref Point reference7 = ref _003C[6];
    			reference7 = new Point(8, 20);
    			ref Point reference8 = ref _003C[7];
    			reference8 = new Point(12, 12);
    		}

    		protected override void OnPaint(PaintEventArgs e)
    		{
    			base.OnPaint(e);
    			Graphics graphics = e.Graphics;
    			checked
    			{
    				graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1);
    				graphics.SmoothingMode = SmoothingMode.HighQuality;
    				int num = unchecked(int_3 % 8);
    				int num2 = 0;
    				Rectangle rect;
    				do
    				{
    					int num3 = num - num2;
    					if (num3 < 0)
    					{
    						num3 += 8;
    					}
    					num3 = Math.Max(5 - num3, 1);
    					if (num2 > int_3)
    					{
    						num3 = 1;
    					}
    					Brush black = Brushes.Black;
    					rect = new Rectangle(_003C[num2].X - num3, _003C[num2].Y - num3, num3 * 2, num3 * 2);
    					graphics.FillEllipse(black, rect);
    					num2++;
    				}
    				while (num2 <= 7);
    				StringFormat stringFormat = (StringFormat)StringFormat.GenericDefault.Clone();
    				stringFormat.LineAlignment = StringAlignment.Center;
    				string s = "Loading...";
    				Font font = Font;
    				Brush black2 = Brushes.Black;
    				rect = new Rectangle(40, 0, Width - 40, Height);
    				graphics.DrawString(s, font, black2, rect, stringFormat);
    				stringFormat.Dispose();
    				int_3++;
    			}
    		}
    	}

    	private static Thread thread_0;

    	[DebuggerNonUserCode]
    	public UiThread()
    	{
    	}

    	public static void smethod_0(MethodInvoker methodInvoker_0, Control control_0)
    	{
    		IAsyncResult iasyncResult_ = methodInvoker_0.BeginInvoke(null, null);
    		smethod_3(iasyncResult_, control_0);
    	}

    	public static object smethod_1(ObjectFunc gdelegate0_0, object object_0, Control control_0)
    	{
    		IAsyncResult iasyncResult_ = gdelegate0_0.BeginInvoke(RuntimeHelpers.GetObjectValue(object_0), null, null);
    		smethod_3(iasyncResult_, control_0);
    		return gdelegate0_0.EndInvoke(iasyncResult_);
    	}

    	public static TreeNode smethod_2(TreeNodeFunc gdelegate1_0, string string_0, Control control_0)
    	{
    		IAsyncResult iasyncResult_ = gdelegate1_0.BeginInvoke(string_0, null, null);
    		smethod_3(iasyncResult_, control_0);
    		return gdelegate1_0.EndInvoke(iasyncResult_);
    	}

    	private static void smethod_3(IAsyncResult iasyncResult_0, Control control_0)
    	{
    		LoadingOverlay gControl = new LoadingOverlay();
    		Point location = new Point(checked(control_0.ClientSize.Width - gControl.Width) / 2, checked(control_0.ClientSize.Height - gControl.Height) / 2);
    		gControl.Location = location;
    		control_0.Controls.Add(gControl);
    		gControl.BringToFront();
    		gControl.Refresh();
    		while (!iasyncResult_0.IsCompleted)
    		{
    			Thread.Sleep(100);
    			gControl.Refresh();
    		}
    		control_0.Controls.Remove(gControl);
    		gControl.Dispose();
    		control_0.Refresh();
    	}

    	public static void smethod_4(Form form_0, Rectangle rectangle_0)
    	{
    		if (thread_0 == null)
    		{
    			thread_0 = new Thread(smethod_6);
    			thread_0.IsBackground = true;
    			thread_0.Start(rectangle_0);
    		}
    	}

    	public static void smethod_5()
    	{
    		thread_0 = null;
    	}

    	private static void smethod_6(object object_0)
    	{
    		Rectangle rectangle2 = default;
    		Rectangle rectangle = ((object_0 != null) ? ((Rectangle)object_0) : rectangle2);
    		LoadingOverlay gControl = new LoadingOverlay();
    		Form form = new Form();
    		form.FormBorderStyle = FormBorderStyle.None;
    		form.ShowInTaskbar = false;
    		form.StartPosition = FormStartPosition.Manual;
    		form.ShowIcon = false;
    		form.MinimumSize = gControl.Size;
    		form.Size = gControl.Size;
    		checked
    		{
    			Point location = new Point(rectangle.X + unchecked(checked(rectangle.Width - gControl.Width) / 2), rectangle.Y + unchecked(checked(rectangle.Height - gControl.Height) / 2));
    			form.Location = location;
    			form.Controls.Add(gControl);
    			form.TopMost = true;
    			gControl.BringToFront();
    			gControl.Refresh();
    			form.Show();
    			while (thread_0 != null)
    			{
    				Thread.Sleep(100);
    				gControl.Refresh();
    			}
    			form.Close();
    			gControl.Dispose();
    			form.Dispose();
    		}
    	}
    }

}
