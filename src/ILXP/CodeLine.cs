using System.Runtime.CompilerServices;

namespace ILXP
{
    public sealed class CodeLine
    {
    	private TokenList tokenList_0;

    	private int int_0;

    	private int int_1;

    	public CodeLine()
    	{
    		tokenList_0 = new TokenList();
    	}

    	[SpecialName]
    	public TokenList method_0()
    	{
    		return tokenList_0;
    	}

    	[SpecialName]
    	public int method_1()
    	{
    		return int_0;
    	}

    	[SpecialName]
    	public void method_2(int int_2)
    	{
    		int_0 = int_2;
    	}

    	[SpecialName]
    	public int method_3()
    	{
    		return int_1;
    	}

    	[SpecialName]
    	public void method_4(int int_2)
    	{
    		int_1 = int_2;
    	}
    }

}
