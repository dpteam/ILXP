using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILXP
{
    public sealed class LineCollection : List<CodeLine>
    {
    	[DebuggerNonUserCode]
    	public LineCollection()
    	{
    	}

    	public override string ToString()
    	{
    		string text = "";
    		using Enumerator enumerator = GetEnumerator();
    		while (enumerator.MoveNext())
    		{
    			CodeLine current = enumerator.Current;
    			text = text + current.ToString() + Environment.NewLine;
    		}
    		return text;
    	}
    }

}
