using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

[SuppressIldasm]
public class GClass10 : Panel
{
	[SuppressIldasm]
	private struct Struct0
	{
		public int int_0;

		public int int_1;

		public int int_2;

		public int int_3;
	}

	[SuppressIldasm]
	private struct Struct1
	{
		internal Struct0 struct0_0;

		internal Struct0 struct0_1;

		internal Struct0 struct0_2;

		internal Struct2 struct2_0;
	}

	[SuppressIldasm]
	private struct Struct2
	{
		internal IntPtr intptr_0;

		internal IntPtr intptr_1;

		internal int x;

		internal int y;

		internal int int_0;

		internal int int_1;

		internal uint uint_0;
	}

	[DebuggerNonUserCode]
	public GClass10()
	{
	}

	protected override void WndProc(ref Message m)
	{
		try
		{
			if (!smethod_0(this, ref m))
			{
				base.WndProc(ref m);
				smethod_1(this, ref m);
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	public static bool smethod_0(Control control_0, ref Message message_0)
	{
		if (Conversions.ToBoolean(Operators.OrObject(Operators.CompareObjectNotEqual(NewLateBinding.LateGet(control_0, null, Class8.smethod_303(), new object[0], null, null, null), 2, TextCompare: false), !Application.RenderWithVisualStyles)))
		{
			return false;
		}
		checked
		{
			switch (message_0.Msg)
			{
			case 131:
				if (message_0.WParam == (IntPtr)0)
				{
					object lParam = message_0.GetLParam(typeof(Struct0));
					Struct0 struct2 = default;
					Struct0 @struct = ((lParam != null) ? ((Struct0)lParam) : struct2);
					@struct.int_3++;
					@struct.int_1--;
					@struct.int_2++;
					@struct.int_0--;
					Marshal.StructureToPtr(@struct, message_0.LParam, fDeleteOld: false);
				}
				else
				{
					object lParam2 = message_0.GetLParam(typeof(Struct1));
					Struct1 struct4 = default;
					Struct1 struct3 = ((lParam2 != null) ? ((Struct1)lParam2) : struct4);
					struct3.struct0_0.int_1 = struct3.struct0_0.int_1 - 1;
					struct3.struct0_0.int_3 = struct3.struct0_0.int_3 + 1;
					struct3.struct0_0.int_0 = struct3.struct0_0.int_0 - 1;
					struct3.struct0_0.int_2 = struct3.struct0_0.int_2 + 1;
					Marshal.StructureToPtr(struct3, message_0.LParam, fDeleteOld: false);
				}
				return false;
			case 133:
				if (Conversions.ToBoolean((!Conversions.ToBoolean(control_0.GetType().GetProperty(Class8.smethod_304()) != null) || !Conversions.ToBoolean(NewLateBinding.LateGet(control_0, null, Class8.smethod_304(), new object[0], null, null, null))) ? ((object)false) : ((object)true)))
				{
					return false;
				}
				try
				{
					IntPtr zero = IntPtr.Zero;
					if (message_0.WParam.ToInt32() != 1 && !(message_0.WParam == IntPtr.Zero))
					{
						zero = GetDCEx(message_0.HWnd, message_0.WParam, 129);
						if (zero == IntPtr.Zero)
						{
							zero = GetWindowDC(message_0.HWnd);
						}
					}
					else
					{
						zero = GetWindowDC(message_0.HWnd);
					}
					if (zero == IntPtr.Zero)
					{
						return false;
					}
					Graphics graphics = Graphics.FromHdc(zero);
					Struct0 struct0_ = default;
					if (GetWindowRect((int)message_0.HWnd, ref struct0_) == 0)
					{
						return false;
					}
					Rectangle bounds = new Rectangle(0, 0, struct0_.int_2 - struct0_.int_0 - 1, struct0_.int_3 - struct0_.int_1 - 1);
					if ((bounds.Width == 0) | (bounds.Height == 0))
					{
						return false;
					}
					ControlPaint.DrawVisualStyleBorder(graphics, bounds);
					bounds.Inflate(-1, -1);
					ReleaseDC(message_0.HWnd, zero);
					return true;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				break;
			}
			return false;
		}
	}

	public static void smethod_1(Control control_0, ref Message message_0)
	{
		if (Conversions.ToBoolean(Operators.OrObject(Operators.CompareObjectNotEqual(NewLateBinding.LateGet(control_0, null, Class8.smethod_303(), new object[0], null, null, null), 2, TextCompare: false), !Application.RenderWithVisualStyles)))
		{
			return;
		}
		int msg = message_0.Msg;
		if (msg != 133 || Conversions.ToBoolean((Conversions.ToBoolean(control_0.GetType().GetProperty(Class8.smethod_304()) == null) || Conversions.ToBoolean(Operators.NotObject(NewLateBinding.LateGet(control_0, null, Class8.smethod_304(), new object[0], null, null, null)))) ? ((object)true) : ((object)false)))
		{
			return;
		}
		try
		{
			IntPtr zero = IntPtr.Zero;
			if (message_0.WParam.ToInt32() != 1 && !(message_0.WParam == IntPtr.Zero))
			{
				zero = GetDCEx(message_0.HWnd, message_0.WParam, 129);
				if (zero == IntPtr.Zero)
				{
					zero = GetWindowDC(message_0.HWnd);
				}
			}
			else
			{
				zero = GetWindowDC(message_0.HWnd);
			}
			if (zero == IntPtr.Zero)
			{
				return;
			}
			Graphics graphics = Graphics.FromHdc(zero);
			Struct0 struct0_ = default;
			if (GetWindowRect((int)message_0.HWnd, ref struct0_) != 0)
			{
				Rectangle bounds = checked(new Rectangle(0, 0, struct0_.int_2 - struct0_.int_0 - 1, struct0_.int_3 - struct0_.int_1 - 1));
				if (!((bounds.Width == 0) | (bounds.Height == 0)))
				{
					ControlPaint.DrawVisualStyleBorder(graphics, bounds);
					bounds.Inflate(-1, -1);
					ReleaseDC(message_0.HWnd, zero);
				}
			}
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			ProjectData.ClearProjectError();
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	private static extern bool SetWindowPos(IntPtr intptr_0, IntPtr intptr_1, int int_0, int int_1, int int_2, int int_3, int int_4);

	[DllImport("user32")]
	private static extern IntPtr GetWindowDC(IntPtr intptr_0);

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	private static extern IntPtr GetDCEx(IntPtr intptr_0, IntPtr intptr_1, int int_0);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern void ReleaseDC(IntPtr intptr_0, IntPtr intptr_1);

	[DllImport("user32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	private static extern int GetWindowRect(int int_0, ref Struct0 struct0_0);
}
