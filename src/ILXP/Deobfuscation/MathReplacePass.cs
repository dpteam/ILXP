using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Deobfuscation
{
    /// <summary>
    /// Folds calls to <see cref="System.Math"/> whose arguments are constants, e.g.
    /// <c>Math.Abs(-5)</c> becomes <c>5</c>. Ported from the Unscrambler feature set
    /// and implemented on top of Cecil.
    /// </summary>
    internal sealed class MathReplacePass : IIlPass
    {
        public string Name
        {
            get { return "Math constant folding"; }
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

                replaced += ProcessMethod(method);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                replaced += ProcessType(nested);
            }
            return replaced;
        }

        private static int ProcessMethod(MethodDefinition method)
        {
            int replaced = 0;
            var instructions = method.Body.Instructions;
            HashSet<Instruction> targets = CollectTargets(method.Body);

            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction instruction = instructions[i];
                if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
                {
                    continue;
                }

                MethodReference reference = instruction.Operand as MethodReference;
                if (reference == null || reference.DeclaringType == null ||
                    reference.DeclaringType.FullName != "System.Math" || reference.HasThis)
                {
                    continue;
                }

                int count = reference.Parameters.Count;
                if (count == 0 || i < count)
                {
                    continue;
                }

                object[] arguments = new object[count];
                bool valid = true;
                for (int p = 0; p < count; p++)
                {
                    Instruction load = instructions[i - count + p];
                    if (targets.Contains(load) || !TryGetConstant(load, out arguments[p]))
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid)
                {
                    continue;
                }

                object result;
                if (!TryEvaluate(reference, arguments, out result))
                {
                    continue;
                }

                Instruction replacement;
                if (!TryCreateConstant(result, reference.ReturnType, out replacement))
                {
                    continue;
                }

                for (int p = 0; p < count; p++)
                {
                    Instruction load = instructions[i - count + p];
                    load.OpCode = OpCodes.Nop;
                    load.Operand = null;
                }

                instruction.OpCode = replacement.OpCode;
                instruction.Operand = replacement.Operand;
                replaced++;
            }
            return replaced;
        }

        private static HashSet<Instruction> CollectTargets(Mono.Cecil.Cil.MethodBody body)
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
                Add(targets, handler.TryStart);
                Add(targets, handler.TryEnd);
                Add(targets, handler.HandlerStart);
                Add(targets, handler.HandlerEnd);
                Add(targets, handler.FilterStart);
            }
            return targets;
        }

        private static void Add(HashSet<Instruction> targets, Instruction instruction)
        {
            if (instruction != null)
            {
                targets.Add(instruction);
            }
        }

        private static bool TryGetConstant(Instruction instruction, out object value)
        {
            value = null;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldc_I4_M1:
                    value = -1;
                    return true;
                case Code.Ldc_I4_0:
                    value = 0;
                    return true;
                case Code.Ldc_I4_1:
                    value = 1;
                    return true;
                case Code.Ldc_I4_2:
                    value = 2;
                    return true;
                case Code.Ldc_I4_3:
                    value = 3;
                    return true;
                case Code.Ldc_I4_4:
                    value = 4;
                    return true;
                case Code.Ldc_I4_5:
                    value = 5;
                    return true;
                case Code.Ldc_I4_6:
                    value = 6;
                    return true;
                case Code.Ldc_I4_7:
                    value = 7;
                    return true;
                case Code.Ldc_I4_8:
                    value = 8;
                    return true;
                case Code.Ldc_I4_S:
                    value = (int)(sbyte)instruction.Operand;
                    return true;
                case Code.Ldc_I4:
                    value = (int)instruction.Operand;
                    return true;
                case Code.Ldc_I8:
                    value = (long)instruction.Operand;
                    return true;
                case Code.Ldc_R4:
                    value = (float)instruction.Operand;
                    return true;
                case Code.Ldc_R8:
                    value = (double)instruction.Operand;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryEvaluate(MethodReference reference, object[] arguments, out object result)
        {
            result = null;
            try
            {
                Type[] types = new Type[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    types[i] = arguments[i].GetType();
                }

                MethodInfo method = typeof(Math).GetMethod(reference.Name, BindingFlags.Public | BindingFlags.Static, null, types, null);
                if (method == null)
                {
                    return false;
                }

                result = method.Invoke(null, arguments);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryCreateConstant(object value, TypeReference returnType, out Instruction instruction)
        {
            instruction = null;
            if (value == null || returnType == null)
            {
                return false;
            }

            switch (returnType.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.Char:
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    instruction = Instruction.Create(OpCodes.Ldc_I4, Convert.ToInt32(value));
                    return true;
                case MetadataType.Int64:
                case MetadataType.UInt64:
                    instruction = Instruction.Create(OpCodes.Ldc_I8, Convert.ToInt64(value));
                    return true;
                case MetadataType.Single:
                    instruction = Instruction.Create(OpCodes.Ldc_R4, Convert.ToSingle(value));
                    return true;
                case MetadataType.Double:
                    instruction = Instruction.Create(OpCodes.Ldc_R8, Convert.ToDouble(value));
                    return true;
                default:
                    return false;
            }
        }
    }
}
