using System.Runtime.CompilerServices;
using Mono.Cecil;

[SuppressIldasm]
public sealed class GClass0
{
	private string string_0;

	private AssemblyDefinition assemblyDefinition_0;

	public GClass0(string FileName, AssemblyDefinition assembly)
	{
		string_0 = FileName;
		assemblyDefinition_0 = assembly;
	}

	[SpecialName]
	public string method_0()
	{
		return string_0;
	}

	[SpecialName]
	public AssemblyDefinition method_1()
	{
		return assemblyDefinition_0;
	}

	[SpecialName]
	public void method_2(AssemblyDefinition assemblyDefinition_1)
	{
		assemblyDefinition_0 = assemblyDefinition_1;
	}
}
