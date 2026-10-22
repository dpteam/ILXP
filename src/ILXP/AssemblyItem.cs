using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// A loaded assembly together with the path it was loaded from. The path is kept
    /// even when the assembly is later replaced (for example after a refresh), so the
    /// tree can always be restored from disk.
    /// </summary>
    public sealed class AssemblyItem
    {
        public AssemblyItem(string filePath, AssemblyDefinition assembly)
        {
            FilePath = filePath;
            Assembly = assembly;
        }

        public string FilePath { get; private set; }

        public AssemblyDefinition Assembly { get; set; }
    }
}
