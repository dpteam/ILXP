using System.Collections.Generic;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// A module (usually a namespace group) shown below an assembly in the tree.
    /// </summary>
    public sealed class ModuleItem
    {
        public ModuleItem(string name, ModuleDefinition module)
            : this(name, module, null)
        {
        }

        public ModuleItem(string name, ModuleDefinition module, IList<ModuleDefinition> modules)
        {
            Namespace = name;
            Module = module;

            // A namespace can be spread over several modules of the same assembly; listing
            // them here keeps the tree single-node while still showing every type.
            Modules = modules ?? new List<ModuleDefinition> { module };
        }

        public string Namespace { get; private set; }

        public ModuleDefinition Module { get; private set; }

        /// <summary>All modules that contribute types to this namespace (at least one).</summary>
        public IList<ModuleDefinition> Modules { get; private set; }
    }
}
