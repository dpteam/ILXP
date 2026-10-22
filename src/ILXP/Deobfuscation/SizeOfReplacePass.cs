using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Deobfuscation
{
    /// <summary>
    /// Replaces <c>sizeof(T)</c> with the constant size for primitive and
    /// blittable value types. Types whose layout cannot be determined safely are
    /// left untouched.
    /// </summary>
    internal sealed class SizeOfReplacePass : IIlPass
    {
        public string Name
        {
            get { return "sizeof() replacement"; }
        }

        public int Run(AssemblyDefinition assembly)
        {
            int replaced = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                bool is32Bit = module.Architecture == TargetArchitecture.I386;
                foreach (TypeDefinition type in module.Types)
                {
                    replaced += ProcessType(type, is32Bit);
                }
            }
            return replaced;
        }

        private static int ProcessType(TypeDefinition type, bool is32Bit)
        {
            int replaced = 0;
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode != OpCodes.Sizeof)
                    {
                        continue;
                    }

                    TypeReference operand = instruction.Operand as TypeReference;
                    int size;
                    if (operand != null && TryGetSize(operand, is32Bit, out size))
                    {
                        instruction.OpCode = OpCodes.Ldc_I4;
                        instruction.Operand = size;
                        replaced++;
                    }
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                replaced += ProcessType(nested, is32Bit);
            }
            return replaced;
        }

        private static bool TryGetSize(TypeReference type, bool is32Bit, out int size)
        {
            size = 0;
            switch (type.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.SByte:
                case MetadataType.Byte:
                    size = 1;
                    return true;
                case MetadataType.Char:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                    size = 2;
                    return true;
                case MetadataType.Int32:
                case MetadataType.UInt32:
                case MetadataType.Single:
                    size = 4;
                    return true;
                case MetadataType.Int64:
                case MetadataType.UInt64:
                case MetadataType.Double:
                    size = 8;
                    return true;
                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    size = is32Bit ? 4 : 8;
                    return true;
                case MetadataType.ValueType:
                    return TryGetValueTypeSize(type, is32Bit, out size);
                default:
                    return false;
            }
        }

        private static bool TryGetValueTypeSize(TypeReference type, bool is32Bit, out int size)
        {
            size = 0;
            try
            {
                TypeDefinition definition = type.Resolve();
                if (definition == null || definition.Fields.Count == 0)
                {
                    return false;
                }

                int total = 0;
                foreach (FieldDefinition field in definition.Fields)
                {
                    if (field.IsStatic)
                    {
                        continue;
                    }

                    int fieldSize;
                    if (!TryGetSize(field.FieldType, is32Bit, out fieldSize))
                    {
                        return false;
                    }
                    total += fieldSize;
                }
                size = total;
                return total > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
