using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace ILXP
{
    public sealed class ModuleItem
    {
    	private string string_0;

    	private ModuleDefinition moduleDefinition_0;

    	public ModuleItem(string Namespace, ModuleDefinition module)
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

}
