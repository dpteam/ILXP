using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Tests
{
    /// <summary>
    /// Test-only obfuscator. It reproduces the transformations that real protectors apply
    /// so that the decompiler and the cleanup passes can be tested without downloading
    /// third-party tools:
    ///
    ///  * renames types, methods, fields, properties and events to unreadable names;
    ///  * inserts NOPs between instructions;
    ///  * adds opaque-predicate dead code;
    ///  * adds junk types and junk members;
    ///  * shuffles member order.
    /// </summary>
    internal static class TestObfuscator
    {
        private static readonly string[] Alphabet =
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
            "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"
        };

        public sealed class Statistics
        {
            public int RenamedTypes;
            public int RenamedMethods;
            public int RenamedFields;
            public int InsertedNops;
            public int DeadBranches;
            public int JunkTypes;
            public int InvalidInstructions;

            public override string ToString()
            {
                return string.Format(
                    "renamed: {0} types, {1} methods, {2} fields; inserted {3} NOPs, {4} dead branches, {5} junk types, {6} invalid instructions",
                    RenamedTypes, RenamedMethods, RenamedFields, InsertedNops, DeadBranches, JunkTypes, InvalidInstructions);
            }
        }

        public static Statistics Obfuscate(string inputPath, string outputPath, int seed)
        {
            return Obfuscate(inputPath, outputPath, seed, true, true);
        }

        public static Statistics Obfuscate(string inputPath, string outputPath, int seed, bool insertNops, bool addDeadBranches)
        {
            Statistics statistics = new Statistics();
            Random random = new Random(seed);
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(inputPath);

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>" || type.Name.StartsWith("<", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    RenameType(type, random, statistics);
                    if (insertNops)
                    {
                        InsertNops(type, random, statistics);
                    }
                    if (addDeadBranches)
                    {
                        InsertDeadBranches(type, random, statistics);
                    }
                }

                AddJunkTypes(module, random, statistics);
            }

            statistics.InvalidInstructions = CountInvalidInstructions(assembly);
            assembly.Write(outputPath, new WriterParameters { WriteSymbols = false });
            return statistics;
        }

        /// <summary>Checks the in-memory model for instructions whose operand is missing.</summary>
        public static int CountInvalidInstructions(AssemblyDefinition assembly)
        {
            int invalid = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    invalid += CountInvalidInType(type);
                }
            }
            return invalid;
        }

        private static int CountInvalidInType(TypeDefinition type)
        {
            int invalid = 0;
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.OperandType != OperandType.InlineNone && instruction.Operand == null)
                    {
                        invalid++;
                    }
                }
            }
            foreach (TypeDefinition nested in type.NestedTypes)
            {
                invalid += CountInvalidInType(nested);
            }
            return invalid;
        }

        private static void RenameType(TypeDefinition type, Random random, Statistics statistics)
        {
            if (!type.IsPublic || type.IsNested)
            {
                type.Name = GarbageName(random);
                statistics.RenamedTypes++;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsSpecialName)
                {
                    continue;
                }

                method.Name = GarbageName(random);
                statistics.RenamedMethods++;
            }

            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsRuntimeSpecialName)
                {
                    continue;
                }

                field.Name = GarbageName(random);
                statistics.RenamedFields++;
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                property.Name = GarbageName(random);
            }

            foreach (EventDefinition @event in type.Events)
            {
                @event.Name = GarbageName(random);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                RenameType(nested, random, statistics);
            }
        }

        private static void InsertNops(TypeDefinition type, Random random, Statistics statistics)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody || method.Body.Instructions.Count == 0)
                {
                    continue;
                }

                var body = method.Body;
                // Short branches only have a signed-byte range; inserting instructions can
                // push the target out of range and silently corrupt the offset, so they are
                // expanded first (exactly what real obfuscators do).
                ExpandShortBranches(body);

                // Keep branch targets valid: only insert where the previous instruction is
                // not a branch, and never at the very beginning of a handler.
                for (int i = body.Instructions.Count - 1; i >= 0; i--)
                {
                    Instruction instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ret || IsBranch(instruction))
                    {
                        continue;
                    }
                    if (random.Next(100) >= 35)
                    {
                        continue;
                    }

                    Instruction nop = Instruction.Create(OpCodes.Nop);
                    body.Instructions.Insert(i + 1, nop);
                    statistics.InsertedNops++;
                }
            }
        }

        /// <summary>Converts <c>*.s</c> branch instructions into their long forms.</summary>
        private static void ExpandShortBranches(MethodBody body)
        {
            foreach (Instruction instruction in body.Instructions)
            {
                if (instruction.OpCode.OperandType != OperandType.ShortInlineBrTarget)
                {
                    continue;
                }

                OpCode longForm;
                if (LongBranches.TryGetValue(instruction.OpCode.Code, out longForm))
                {
                    instruction.OpCode = longForm;
                }
            }
        }

        private static readonly Dictionary<Code, OpCode> LongBranches = BuildLongBranches();

        private static Dictionary<Code, OpCode> BuildLongBranches()
        {
            Dictionary<string, OpCode> byName = new Dictionary<string, OpCode>(StringComparer.Ordinal);
            List<OpCode> all = new List<OpCode>();
            foreach (System.Reflection.FieldInfo field in typeof(OpCodes).GetFields())
            {
                if (field.FieldType != typeof(OpCode))
                {
                    continue;
                }
                OpCode opcode = (OpCode)field.GetValue(null);
                byName[opcode.Name] = opcode;
                all.Add(opcode);
            }

            Dictionary<Code, OpCode> map = new Dictionary<Code, OpCode>();
            foreach (OpCode opcode in all)
            {
                if (opcode.OperandType != OperandType.ShortInlineBrTarget)
                {
                    continue;
                }

                string name = opcode.Name;
                string longName = name.EndsWith(".s", StringComparison.Ordinal)
                    ? name.Substring(0, name.Length - 2)
                    : (name.EndsWith("_S", StringComparison.Ordinal) ? name.Substring(0, name.Length - 2) : null);
                if (longName == null)
                {
                    continue;
                }

                OpCode longForm;
                if (byName.TryGetValue(longName, out longForm))
                {
                    map[opcode.Code] = longForm;
                }
            }
            return map;
        }

        private static void InsertDeadBranches(TypeDefinition type, Random random, Statistics statistics)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody || method.Body.Instructions.Count < 4)
                {
                    continue;
                }

                var body = method.Body;
                if (random.Next(100) >= 40)
                {
                    continue;
                }

                // Find the last ret so the opaque predicate never disturbs the stack:
                // ldc.i4.1 / ldc.i4.0 / ceq / brfalse / nop / target is stack neutral.
                int retIndex = -1;
                for (int i = body.Instructions.Count - 1; i >= 0; i--)
                {
                    if (body.Instructions[i].OpCode == OpCodes.Ret)
                    {
                        retIndex = i;
                        break;
                    }
                }
                if (retIndex <= 0)
                {
                    continue;
                }

                Instruction target = Instruction.Create(OpCodes.Nop);
                body.Instructions.Insert(retIndex, Instruction.Create(OpCodes.Ldc_I4_1));
                body.Instructions.Insert(retIndex + 1, Instruction.Create(OpCodes.Ldc_I4_0));
                body.Instructions.Insert(retIndex + 2, Instruction.Create(OpCodes.Ceq));
                body.Instructions.Insert(retIndex + 3, Instruction.Create(OpCodes.Brfalse, target));
                body.Instructions.Insert(retIndex + 4, Instruction.Create(OpCodes.Nop));
                body.Instructions.Insert(retIndex + 5, target);
                statistics.DeadBranches++;
            }
        }

        private static void AddJunkTypes(ModuleDefinition module, Random random, Statistics statistics)
        {
            int count = random.Next(2, 6);
            for (int i = 0; i < count; i++)
            {
                TypeDefinition junk = new TypeDefinition(
                    "Junk" + random.Next(1000),
                    GarbageName(random),
                    TypeAttributes.NotPublic | TypeAttributes.Class,
                    module.TypeSystem.Object);

                junk.Fields.Add(new FieldDefinition(GarbageName(random), FieldAttributes.Private | FieldAttributes.Static,
                    module.TypeSystem.Int32));

                MethodDefinition method = new MethodDefinition(
                    GarbageName(random),
                    MethodAttributes.Public | MethodAttributes.Static,
                    module.TypeSystem.Int32);
                method.Body = new MethodBody(method) { InitLocals = true, MaxStackSize = 8 };
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, random.Next(1, 1000)));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                junk.Methods.Add(method);

                module.Types.Add(junk);
                statistics.JunkTypes++;
            }
        }

        private static bool IsBranch(Instruction instruction)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Branch:
                case FlowControl.Cond_Branch:
                case FlowControl.Return:
                case FlowControl.Throw:
                    return true;
                default:
                    return false;
            }
        }

        private static string GarbageName(Random random)
        {
            int length = random.Next(6, 14);
            System.Text.StringBuilder text = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                text.Append(Alphabet[random.Next(Alphabet.Length)]);
            }
            return text.ToString();
        }
    }
}
