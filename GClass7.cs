using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;

[SuppressIldasm]
public sealed class GClass7
{
	[DefaultMember("Item")]
	[SuppressIldasm]
	public class GClass8<t>
	{
		private object[] object_0;

		private int int_0;

		private int int_1;

		private int int_2;

		public GClass8()
		{
			int_0 = 0;
			int_1 = -1;
			int_2 = 100;
			object_0 = new object[checked(int_2 - 1 + 1)];
		}

		public GClass8(int Capacity)
		{
			int_0 = 0;
			int_1 = -1;
			int_2 = 100;
			if (Capacity < 1)
			{
				throw new Exception("Capacity must be greater than 0");
			}
			int_2 = Capacity;
			object_0 = new object[checked(int_2 - 1 + 1)];
		}

		[SpecialName]
		public int method_0()
		{
			return int_0;
		}

		[SpecialName]
		public t method_1(int int_3)
		{
			if (int_3 >= int_0)
			{
				throw new Exception("Out of bounds");
			}
			checked
			{
				int_3 = int_1 - int_3;
				if (int_3 < 0)
				{
					int_3 += int_2;
				}
				t result = default;
				return result;
			}
		}

		public void method_2(t gparam_0)
		{
			checked
			{
				int_1++;
				if (int_1 >= int_2)
				{
					int_1 -= int_2;
				}
				if (int_0 < int_2)
				{
					int_0++;
				}
				object_0[int_1] = gparam_0;
			}
		}

		public t method_3()
		{
			if (int_0 <= 0)
			{
				throw new Exception("Stack is empty");
			}
			t result = Conversions.ToGenericParameter<t>(RuntimeHelpers.GetObjectValue(object_0[int_1]));
			object_0[int_1] = null;
			checked
			{
				int_1--;
				if (int_1 < 0)
				{
					int_1 += int_2;
				}
				int_0--;
				return result;
			}
		}

		public void method_4()
		{
			checked
			{
				for (int i = 0; i < int_0; i++)
				{
					object_0[int_1] = null;
					int_1--;
					if (int_1 < 0)
					{
						int_1 = int_2 - 1;
					}
				}
				int_1 = -1;
				int_0 = 0;
			}
		}
	}

	private GClass8<string> gclass8_0;

	private GClass8<string> gclass8_1;

	public GClass7()
	{
		gclass8_0 = new GClass8<string>(300);
		gclass8_1 = new GClass8<string>(300);
	}

	public void method_0()
	{
		gclass8_0.method_4();
		gclass8_1.method_4();
	}

	public void method_1(string string_0)
	{
		if (gclass8_0.method_0() > 0)
		{
			string text = gclass8_0.method_3();
			gclass8_0.method_2(text);
			if (Operators.CompareString(string_0, text, TextCompare: false) == 0)
			{
				return;
			}
		}
		gclass8_0.method_2(string_0);
		gclass8_1.method_4();
	}

	[SpecialName]
	public bool method_2()
	{
		return gclass8_0.method_0() > 1;
	}

	[SpecialName]
	public bool method_3()
	{
		return gclass8_1.method_0() > 0;
	}

	public string method_4()
	{
		gclass8_1.method_2(gclass8_0.method_3());
		string text = gclass8_0.method_3();
		gclass8_0.method_2(text);
		return text;
	}

	public string method_5()
	{
		string text = gclass8_1.method_3();
		gclass8_0.method_2(text);
		return text;
	}
}
