using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

[StandardModule]
[DebuggerNonUserCode]
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
[CompilerGenerated]
[SuppressIldasm]
[HideModuleName]
internal sealed class Class5
{
	private static ResourceManager resourceManager_0;

	private static CultureInfo cultureInfo_0;

	[SpecialName]
	internal static ResourceManager smethod_0()
	{
		if (object.ReferenceEquals(resourceManager_0, null))
		{
			ResourceManager resourceManager = new ResourceManager(Class8.smethod_291(), typeof(Class5).Assembly);
			resourceManager_0 = resourceManager;
		}
		return resourceManager_0;
	}

	[SpecialName]
	internal static CultureInfo smethod_1()
	{
		return cultureInfo_0;
	}

	[SpecialName]
	internal static void smethod_2(CultureInfo cultureInfo_1)
	{
		cultureInfo_0 = cultureInfo_1;
	}

	[SpecialName]
	internal static Bitmap smethod_3()
	{
		object objectValue = RuntimeHelpers.GetObjectValue(smethod_0().GetObject(Class8.smethod_292(), cultureInfo_0));
		return (Bitmap)objectValue;
	}

	[SpecialName]
	internal static Bitmap smethod_4()
	{
		object objectValue = RuntimeHelpers.GetObjectValue(smethod_0().GetObject(Class8.smethod_293(), cultureInfo_0));
		return (Bitmap)objectValue;
	}

	[SpecialName]
	internal static Bitmap smethod_5()
	{
		object objectValue = RuntimeHelpers.GetObjectValue(smethod_0().GetObject(Class8.smethod_294(), cultureInfo_0));
		return (Bitmap)objectValue;
	}

	[SpecialName]
	internal static Bitmap _003C()
	{
		object objectValue = RuntimeHelpers.GetObjectValue(smethod_0().GetObject(Class8.smethod_295(), cultureInfo_0));
		return (Bitmap)objectValue;
	}

	[SpecialName]
	internal static Bitmap smethod_6()
	{
		object objectValue = RuntimeHelpers.GetObjectValue(smethod_0().GetObject(Class8.smethod_296(), cultureInfo_0));
		return (Bitmap)objectValue;
	}
}
