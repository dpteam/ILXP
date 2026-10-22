using System.Runtime.CompilerServices;
using Mono.Cecil;

[SuppressIldasm]
public sealed class GClass6
{
	private string string_0;

	private ModuleDefinition moduleDefinition_0;

	public GClass6(string Namespace, ModuleDefinition module)
	{
		moduleDefinition_0 = module;
		string_0 = Namespace;
	}

	[SpecialName]
	public string method_0()
	{
		return string_0;
	}

	[SpecialName]
	public ModuleDefinition method_1()
	{
		return moduleDefinition_0;
	}
}
