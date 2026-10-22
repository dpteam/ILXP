using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Deobfuscation
{
    /// <summary>
    /// Replaces the common obfuscation pattern <c>Type.EmptyTypes.Length</c> with the
    /// constant 0.
    /// </summary>
    internal sealed class EmptyTypesPass : IIlPass
    {
        public string Name
        {
            get { return "Type.EmptyTypes folding"; }
        }

        public int Run(AssemblyDefinition assembly)
        {
            int replaced = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    replaced += ProcessType(type);
                }
            }
            return replaced;
        }

        private static int ProcessType(TypeDefinition type)
        {
            int replaced = 0;
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                var instructions = method.Body.Instructions;
                for (int i = 0; i + 1 < instructions.Count; i++)
                {
                    Instruction current = instructions[i];
                    if (current.OpCode != OpCodes.Ldsfld || instructions[i + 1].OpCode != OpCodes.Ldlen)
                    {
                        continue;
                    }

                    FieldReference field = current.Operand as FieldReference;
                    if (field == null || field.Name != "EmptyTypes" || field.DeclaringType == null ||
                        field.DeclaringType.FullName != "System.Type")
                    {
                        continue;
                    }

                    current.OpCode = OpCodes.Ldc_I4_0;
                    current.Operand = null;
                    instructions[i + 1].OpCode = OpCodes.Nop;
                    instructions[i + 1].Operand = null;
                    replaced++;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                replaced += ProcessType(nested);
            }
            return replaced;
        }
    }
}
