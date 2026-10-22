using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// A module (usually a namespace group) shown below an assembly in the tree.
    /// </summary>
    public sealed class ModuleItem
    {
        public ModuleItem(string name, ModuleDefinition module)
        {
            Namespace = name;
            Module = module;
        }

        public string Namespace { get; private set; }

        public ModuleDefinition Module { get; private set; }
    }
}
