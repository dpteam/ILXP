using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Deobfuscation
{
    /// <summary>
    /// Removes NOP instructions that are not referenced by branches or exception
    /// handlers. This is the classic cleanup for obfuscated assemblies that insert
    /// random NOPs between every real instruction.
    /// </summary>
    internal sealed class NopRemoverPass : IIlPass
    {
        public string Name
        {
            get { return "NOP remover"; }
        }

        public int Run(AssemblyDefinition assembly)
        {
            int removed = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    removed += ProcessType(type);
                }
            }
            return removed;
        }

        private static int ProcessType(TypeDefinition type)
        {
            int removed = 0;
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                MethodBody body = method.Body;
                HashSet<Instruction> targets = CollectTargets(body);
                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    Instruction instruction = body.Instructions[i];
                    if (instruction.OpCode != OpCodes.Nop || targets.Contains(instruction))
                    {
                        continue;
                    }

                    body.Instructions.RemoveAt(i);
                    i--;
                    removed++;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                removed += ProcessType(nested);
            }
            return removed;
        }

        private static HashSet<Instruction> CollectTargets(MethodBody body)
        {
            HashSet<Instruction> targets = new HashSet<Instruction>();
            foreach (Instruction instruction in body.Instructions)
            {
                Instruction target = instruction.Operand as Instruction;
                if (target != null)
                {
                    targets.Add(target);
                }

                Instruction[] array = instruction.Operand as Instruction[];
                if (array != null)
                {
                    foreach (Instruction item in array)
                    {
                        if (item != null)
                        {
                            targets.Add(item);
                        }
                    }
                }
            }

            foreach (ExceptionHandler handler in body.ExceptionHandlers)
            {
                AddIfNotNull(targets, handler.TryStart);
                AddIfNotNull(targets, handler.TryEnd);
                AddIfNotNull(targets, handler.HandlerStart);
                AddIfNotNull(targets, handler.HandlerEnd);
                AddIfNotNull(targets, handler.FilterStart);
            }
            return targets;
        }

        private static void AddIfNotNull(HashSet<Instruction> targets, Instruction instruction)
        {
            if (instruction != null)
            {
                targets.Add(instruction);
            }
        }
    }
}
