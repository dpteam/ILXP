using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[SuppressIldasm]
public sealed class GClass13 : List<GClass14>
{
	[DebuggerNonUserCode]
	public GClass13()
	{
	}

	public override string ToString()
	{
		string text = "";
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			GClass14 current = enumerator.Current;
			text = text + current.ToString() + Environment.NewLine;
		}
		return text;
	}
}
