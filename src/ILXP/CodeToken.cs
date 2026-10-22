using System.Drawing;
using System.Runtime.CompilerServices;

namespace ILXP
{
    public sealed class CodeToken
    {
    	private string string_0;

    	private Color color_0;

    	private string string_1;

    	private Rectangle rectangle_0;

    	public CodeToken()
    	{
    		color_0 = Color.Black;
    	}

    	public CodeToken(string text)
    	{
    		color_0 = Color.Black;
    		string_0 = text;
    	}

    	[SpecialName]
    	public string method_0()
    	{
    		return string_0;
    	}

    	[SpecialName]
    	public void method_1(string string_2)
    	{
    		string_0 = string_2;
    	}

    	[SpecialName]
    	public Rectangle method_2()
    	{
    		return rectangle_0;
    	}

    	[SpecialName]
    	public void method_3(Rectangle rectangle_1)
    	{
    		rectangle_0 = rectangle_1;
    	}

    	[SpecialName]
    	public string method_4()
    	{
    		return string_1;
    	}

    	[SpecialName]
    	public void method_5(string string_2)
    	{
    		string_1 = string_2;
    	}

    	[SpecialName]
    	public Color method_6()
    	{
    		return color_0;
    	}

    	[SpecialName]
    	public void method_7(Color color_1)
    	{
    		color_0 = color_1;
    	}
    }

}
