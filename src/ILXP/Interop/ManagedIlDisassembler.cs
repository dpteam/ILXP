using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP.Interop
{
    /// <summary>
    /// Managed IL disassembler: turns an assembly into ILAsm-compatible text without
    /// running the Microsoft <c>ildasm.exe</c>. This is the in-house replacement and it
    /// works for any assembly Cecil can read, including assemblies built for newer
    /// runtimes than the one ILXP itself runs on.
    /// </summary>
    internal static class ManagedIlDisassembler
    {
        public static string Disassemble(AssemblyDefinition assembly, bool includeBytes)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine("//  ILXP managed IL disassembler");
            text.AppendLine("//  Assembly: " + assembly.Name.FullName);
            text.AppendLine();

            WriteAssemblyHeader(text, assembly);

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }
                    WriteType(text, type, 0, includeBytes);
                }
            }

            return text.ToString();
        }

        private static void WriteAssemblyHeader(StringBuilder text, AssemblyDefinition assembly)
        {
            AssemblyNameDefinition name = assembly.Name;
            text.AppendLine(".assembly " + Quote(name.Name));
            text.AppendLine("{");
            if (name.Version != null)
            {
                text.AppendLine("  .ver " + name.Version.Major + ":" + name.Version.Minor + ":" + name.Version.Build + ":" + name.Version.Revision);
            }
            text.AppendLine("}");
            text.AppendLine();

            foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences)
            {
                text.AppendLine(".assembly extern " + Quote(reference.Name) + " { .ver " +
                                reference.Version.Major + ":" + reference.Version.Minor + ":" +
                                reference.Version.Build + ":" + reference.Version.Revision + " }");
            }
            if (assembly.MainModule.AssemblyReferences.Count > 0)
            {
                text.AppendLine();
            }

            text.AppendLine(".module " + Quote(assembly.MainModule.Name));
            text.AppendLine();
        }

        private static void WriteType(StringBuilder text, TypeDefinition type, int indent, bool includeBytes)
        {
            string pad = new string(' ', indent);
            text.Append(pad).Append(".class ").Append(TypeAttributesText(type)).Append(' ').Append(Quote(type.FullName));

            if (type.BaseType != null)
            {
                text.Append(" extends ").Append(TypeName(type.BaseType));
            }
            text.AppendLine();
            text.Append(pad).AppendLine("{");

            foreach (FieldDefinition field in type.Fields)
            {
                text.Append(pad).Append("  .field ").Append(FieldAttributesText(field)).Append(' ');
                text.Append(TypeName(field.FieldType)).Append(' ').Append(field.Name);
                if (field.HasConstant && field.Constant != null)
                {
                    text.Append(" = ").Append(ConstantText(field.Constant));
                }
                text.AppendLine();
            }
            if (type.Fields.Count > 0)
            {
                text.AppendLine();
            }

            foreach (MethodDefinition method in type.Methods)
            {
                WriteMethod(text, method, indent + 2, includeBytes);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                WriteType(text, nested, indent + 2, includeBytes);
            }

            text.Append(pad).AppendLine("}");
            text.AppendLine();
        }

        private static void WriteMethod(StringBuilder text, MethodDefinition method, int indent, bool includeBytes)
        {
            string pad = new string(' ', indent);
            text.Append(pad).Append(".method ").Append(MethodAttributesText(method)).Append(' ');
            text.Append(TypeName(method.ReturnType)).Append(' ').Append(Quote(method.Name)).Append('(');

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(TypeName(method.Parameters[i].ParameterType));
                if (!string.IsNullOrEmpty(method.Parameters[i].Name))
                {
                    text.Append(' ').Append(method.Parameters[i].Name);
                }
            }
            text.Append(')');

            if (method.IsAbstract || method.IsPInvokeImpl)
            {
                text.AppendLine();
                return;
            }

            if (!method.HasBody)
            {
                text.AppendLine(" { }");
                return;
            }

            text.AppendLine();
            text.Append(pad).AppendLine("{");

            MethodBody body = method.Body;
            text.Append(pad).Append("  .maxstack ").AppendLine(body.MaxStackSize.ToString(CultureInfo.InvariantCulture));

            foreach (VariableDefinition variable in body.Variables)
            {
                text.Append(pad).Append("  .locals init (").Append(TypeName(variable.VariableType)).Append(' ').AppendLine(variable.Name + ")");
            }

            foreach (Instruction instruction in body.Instructions)
            {
                text.Append(pad).Append("  IL_").Append(instruction.Offset.ToString("x4", CultureInfo.InvariantCulture)).Append(": ");
                text.Append(OpCodeText(instruction));
                text.AppendLine();
            }

            text.Append(pad).AppendLine("}");
            text.AppendLine();
        }

        private static string OpCodeText(Instruction instruction)
        {
            string name = instruction.OpCode.Name;
            object operand = instruction.Operand;
            if (operand == null)
            {
                return name;
            }

            if (operand is Instruction)
            {
                return name + " IL_" + ((Instruction)operand).Offset.ToString("x4", CultureInfo.InvariantCulture);
            }

            if (operand is Instruction[])
            {
                StringBuilder text = new StringBuilder(name).Append('(');
                Instruction[] targets = (Instruction[])operand;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append("IL_").Append(targets[i].Offset.ToString("x4", CultureInfo.InvariantCulture));
                }
                return text.Append(')').ToString();
            }

            if (operand is string)
            {
                return name + " " + Quote((string)operand);
            }

            if (operand is MethodReference)
            {
                return name + " " + MemberName((MethodReference)operand);
            }

            if (operand is FieldReference)
            {
                return name + " " + MemberName((FieldReference)operand);
            }

            if (operand is TypeReference)
            {
                return name + " " + TypeName((TypeReference)operand);
            }

            if (operand is VariableDefinition)
            {
                return name + " " + ((VariableDefinition)operand).Name;
            }

            if (operand is ParameterDefinition)
            {
                return name + " " + ((ParameterDefinition)operand).Name;
            }

            if (operand is float)
            {
                return name + " " + ((float)operand).ToString("R", CultureInfo.InvariantCulture);
            }

            if (operand is double)
            {
                return name + " " + ((double)operand).ToString("R", CultureInfo.InvariantCulture);
            }

            return name + " " + Convert.ToString(operand, CultureInfo.InvariantCulture);
        }

        private static string MemberName(MethodReference method)
        {
            StringBuilder text = new StringBuilder();
            if (method.DeclaringType != null)
            {
                text.Append(TypeName(method.DeclaringType)).Append("::");
            }
            text.Append(method.Name).Append('(');
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(TypeName(method.Parameters[i].ParameterType));
            }
            return text.Append(')').ToString();
        }

        private static string MemberName(FieldReference field)
        {
            return TypeName(field.DeclaringType) + "::" + field.Name;
        }

        private static string TypeName(TypeReference type)
        {
            if (type == null)
            {
                return "void";
            }

            switch (type.MetadataType)
            {
                case MetadataType.Void: return "void";
                case MetadataType.Boolean: return "bool";
                case MetadataType.Char: return "char";
                case MetadataType.SByte: return "int8";
                case MetadataType.Byte: return "uint8";
                case MetadataType.Int16: return "int16";
                case MetadataType.UInt16: return "uint16";
                case MetadataType.Int32: return "int32";
                case MetadataType.UInt32: return "uint32";
                case MetadataType.Int64: return "int64";
                case MetadataType.UInt64: return "uint64";
                case MetadataType.Single: return "float32";
                case MetadataType.Double: return "float64";
                case MetadataType.String: return "string";
                case MetadataType.Object: return "object";
            }

            StringBuilder text = new StringBuilder();
            if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace != "System")
            {
                text.Append(type.Namespace).Append('.');
            }
            text.Append(type.Name.Replace('/', '.'));

            ArrayType array = type as ArrayType;
            if (array != null)
            {
                text.Append('[').Append(new string(',', Math.Max(0, array.Rank - 1))).Append(']');
            }

            GenericInstanceType generic = type as GenericInstanceType;
            if (generic != null)
            {
                text.Append('<');
                for (int i = 0; i < generic.GenericArguments.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(TypeName(generic.GenericArguments[i]));
                }
                text.Append('>');
            }

            return text.ToString();
        }

        private static string TypeAttributesText(TypeDefinition type)
        {
            List<string> parts = new List<string>();
            parts.Add(type.IsInterface ? "interface" : "auto");
            if (type.IsSequentialLayout) parts.Add("sequential");
            if (type.IsExplicitLayout) parts.Add("explicit");
            if (type.IsAbstract) parts.Add("abstract");
            if (type.IsSealed) parts.Add("sealed");
            if (type.IsBeforeFieldInit) parts.Add("beforefieldinit");
            if (type.IsPublic) parts.Add("public");
            else if (type.IsNestedPublic) parts.Add("nested public");
            else if (type.IsNestedPrivate) parts.Add("nested private");
            else if (type.IsNestedFamily) parts.Add("nested family");
            else if (type.IsNestedAssembly) parts.Add("nested assembly");
            else parts.Add("private");
            return "ansi " + string.Join(" ", parts.ToArray());
        }

        private static string FieldAttributesText(FieldDefinition field)
        {
            List<string> parts = new List<string>();
            if (field.IsStatic) parts.Add("static");
            if (field.IsInitOnly) parts.Add("initonly");
            if (field.IsLiteral) parts.Add("literal");
            if (field.IsPublic) parts.Add("public");
            else if (field.IsFamily) parts.Add("family");
            else if (field.IsAssembly) parts.Add("assembly");
            else parts.Add("private");
            return string.Join(" ", parts.ToArray());
        }

        private static string MethodAttributesText(MethodDefinition method)
        {
            List<string> parts = new List<string>();
            if (method.IsStatic) parts.Add("static");
            if (method.IsVirtual) parts.Add("virtual");
            if (method.IsAbstract) parts.Add("abstract");
            if (method.IsNewSlot) parts.Add("newslot");
            if (method.IsReuseSlot) parts.Add("reuseslot");
            if (method.IsHideBySig) parts.Add("hidebysig");
            if (method.IsSpecialName) parts.Add("specialname");
            if (method.IsPInvokeImpl) parts.Add("pinvokeimpl");
            if (method.IsPublic) parts.Add("public");
            else if (method.IsFamily) parts.Add("family");
            else if (method.IsAssembly) parts.Add("assembly");
            else parts.Add("private");
            parts.Add(method.IsConstructor && method.IsStatic ? "cctor" : (method.IsConstructor ? "ctor" : "instance"));
            return string.Join(" ", parts.ToArray());
        }

        private static string ConstantText(object value)
        {
            if (value == null)
            {
                return "nullref";
            }
            if (value is string)
            {
                return Quote((string)value);
            }
            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }
            if (value is char)
            {
                return ((int)(char)value).ToString(CultureInfo.InvariantCulture);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        internal static string Quote(string value)
        {
            if (value == null)
            {
                return "''";
            }

            StringBuilder text = new StringBuilder(value.Length + 2);
            text.Append('\'');
            foreach (char c in value)
            {
                if (c == '\'')
                {
                    text.Append("\\'");
                }
                else if (c < 32)
                {
                    text.Append("\\0").Append(((int)c).ToString("x2"));
                }
                else
                {
                    text.Append(c);
                }
            }
            text.Append('\'');
            return text.ToString();
        }
    }
}
