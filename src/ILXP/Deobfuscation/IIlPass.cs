using Mono.Cecil;

namespace ILXP.Deobfuscation
{
    /// <summary>
    /// A single IL rewriting pass. Passes must be conservative: they may only change
    /// code whose semantics are provably equivalent.
    /// </summary>
    internal interface IIlPass
    {
        string Name { get; }

        /// <summary>Runs the pass over the whole assembly and returns the number of changes.</summary>
        int Run(AssemblyDefinition assembly);
    }
}
