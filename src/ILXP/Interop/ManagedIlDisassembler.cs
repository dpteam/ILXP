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

            List<FieldData> data = new List<FieldData>();
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }
                    WriteType(text, type, 0, includeBytes, data);
                }
            }

            WriteFieldData(text, data);
            return text.ToString();
        }

        private sealed class FieldData
        {
            public string Label;
            public byte[] Bytes;
        }

        /// <summary>
        /// Emits the raw bytes of fields with an RVA (statically initialised arrays). ILAsm
        /// declares them as <c>.data I_xxxx = (...)</c> and refers to them with <c>at</c>.
        /// </summary>
        private static void WriteFieldData(StringBuilder text, List<FieldData> data)
        {
            if (data.Count == 0)
            {
                return;
            }

            foreach (FieldData item in data)
            {
                text.Append(".data ").Append(item.Label).Append(" = (");
                for (int i = 0; i < item.Bytes.Length; i++)
                {
                    if (i > 0)
                    {
                        text.Append(' ');
                    }
                    text.Append(item.Bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                text.AppendLine(")");
            }
            text.AppendLine();
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
                text.Append(".assembly extern ").Append(Quote(reference.Name)).Append(" { .ver ")
                    .Append(reference.Version.Major).Append(':').Append(reference.Version.Minor).Append(':')
                    .Append(reference.Version.Build).Append(':').Append(reference.Version.Revision);
                if (reference.PublicKeyToken != null && reference.PublicKeyToken.Length > 0)
                {
                    text.Append(" .publickeytoken = (");
                    for (int i = 0; i < reference.PublicKeyToken.Length; i++)
                    {
                        if (i > 0)
                        {
                            text.Append(' ');
                        }
                        text.Append(reference.PublicKeyToken[i].ToString("x2", CultureInfo.InvariantCulture));
                    }
                    text.Append(')');
                }
                text.AppendLine(" }");
            }
            if (assembly.MainModule.AssemblyReferences.Count > 0)
            {
                text.AppendLine();
            }

            text.AppendLine(".module " + Quote(assembly.MainModule.Name));
            text.AppendLine();
        }

        private static void WriteType(StringBuilder text, TypeDefinition type, int indent, bool includeBytes,
            List<FieldData> data)
        {
            string pad = new string(' ', indent);
            text.Append(pad).Append(".class ").Append(TypeAttributesText(type)).Append(' ').Append(Quote(type.FullName));

            if (type.BaseType != null)
            {
                text.Append(" extends ").Append(TypeName(type.BaseType));
            }

            if (type.HasInterfaces)
            {
                text.Append(" implements ");
                for (int i = 0; i < type.Interfaces.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(TypeName(type.Interfaces[i]));
                }
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
                else if (field.InitialValue != null && field.InitialValue.Length > 0)
                {
                    string label = "I_" + data.Count.ToString("x4", CultureInfo.InvariantCulture);
                    data.Add(new FieldData { Label = label, Bytes = field.InitialValue });
                    text.Append(" at ").Append(label);
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

            foreach (PropertyDefinition property in type.Properties)
            {
                WriteProperty(text, property, indent + 2);
            }

            foreach (EventDefinition @event in type.Events)
            {
                WriteEvent(text, @event, indent + 2);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                WriteType(text, nested, indent + 2, includeBytes, data);
            }

            text.Append(pad).AppendLine("}");
            text.AppendLine();
        }

        /// <summary>
        /// Emits a <c>.property</c> directive. Accessor methods are already emitted as normal
        /// methods, so only the metadata link has to be described here.
        /// </summary>
        private static void WriteProperty(StringBuilder text, PropertyDefinition property, int indent)
        {
            string pad = new string(' ', indent);
            text.Append(pad).Append(".property ").Append(PropertyAttributesText(property)).Append(' ');
            text.Append(TypeName(property.PropertyType)).Append(' ').Append(Quote(property.Name)).Append('(');

            for (int i = 0; i < property.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(TypeName(property.Parameters[i].ParameterType));
            }
            text.AppendLine(")");

            text.Append(pad).AppendLine("{");
            if (property.GetMethod != null)
            {
                text.Append(pad).Append("  .get ").AppendLine(Quote(property.GetMethod.Name));
            }
            if (property.SetMethod != null)
            {
                text.Append(pad).Append("  .set ").AppendLine(Quote(property.SetMethod.Name));
            }
            foreach (MethodDefinition other in property.OtherMethods)
            {
                text.Append(pad).Append("  .other ").AppendLine(Quote(other.Name));
            }
            text.Append(pad).AppendLine("}");
        }

        /// <summary>Emits an <c>.event</c> directive.</summary>
        private static void WriteEvent(StringBuilder text, EventDefinition @event, int indent)
        {
            string pad = new string(' ', indent);
            text.Append(pad).Append(".event ").Append(EventAttributesText(@event)).Append(' ');
            text.Append(TypeName(@event.EventType)).Append(' ').Append(Quote(@event.Name)).AppendLine();

            text.Append(pad).AppendLine("{");
            if (@event.AddMethod != null)
            {
                text.Append(pad).Append("  .addon ").AppendLine(Quote(@event.AddMethod.Name));
            }
            if (@event.RemoveMethod != null)
            {
                text.Append(pad).Append("  .removeon ").AppendLine(Quote(@event.RemoveMethod.Name));
            }
            if (@event.InvokeMethod != null)
            {
                text.Append(pad).Append("  .fire ").AppendLine(Quote(@event.InvokeMethod.Name));
            }
            foreach (MethodDefinition other in @event.OtherMethods)
            {
                text.Append(pad).Append("  .other ").AppendLine(Quote(other.Name));
            }
            text.Append(pad).AppendLine("}");
        }

        private static string PropertyAttributesText(PropertyDefinition property)
        {
            List<string> parts = new List<string>();
            if (property.HasThis)
            {
                parts.Add("instance");
            }
            if (property.IsSpecialName)
            {
                parts.Add("specialname");
            }
            if (property.IsRuntimeSpecialName)
            {
                parts.Add("rtspecialname");
            }
            if (!property.HasThis)
            {
                parts.Add("static");
            }
            return string.Join(" ", parts.ToArray());
        }

        private static string EventAttributesText(EventDefinition @event)
        {
            List<string> parts = new List<string>();
            parts.Add("specialname");
            parts.Add("rtspecialname");
            return string.Join(" ", parts.ToArray());
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

            if ((method.ImplAttributes & MethodImplAttributes.Runtime) != 0)
            {
                // Delegate methods and other runtime-provided implementations have no IL.
                text.AppendLine(" runtime managed");
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
            if (method.Module != null && ReferenceEquals(method.Module.EntryPoint, method))
            {
                text.Append(pad).AppendLine("  .entrypoint");
            }

            for (int i = 0; i < body.Variables.Count; i++)
            {
                VariableDefinition variable = body.Variables[i];
                // PDB names are optional; a synthetic name keeps the IL text self-consistent,
                // because instructions refer to locals by name.
                string name = string.IsNullOrEmpty(variable.Name) ? "V_" + i : variable.Name;
                text.Append(pad).Append("  .locals init (").Append(TypeName(variable.VariableType)).Append(' ').AppendLine(name + ")");
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
                StringBuilder text = new StringBuilder(name).Append(" (");
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
                VariableDefinition variable = (VariableDefinition)operand;
                return name + " " + (string.IsNullOrEmpty(variable.Name) ? "V_" + variable.Index : variable.Name);
            }

            if (operand is ParameterDefinition)
            {
                ParameterDefinition parameter = (ParameterDefinition)operand;
                if (!string.IsNullOrEmpty(parameter.Name))
                {
                    return name + " " + parameter.Name;
                }

                // IL argument indices count "this" first for instance methods.
                int index = parameter.Index;
                if (parameter.Method != null && parameter.Method.HasThis)
                {
                    index++;
                }
                return name + " " + index.ToString(CultureInfo.InvariantCulture);
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
            if (method.HasThis)
            {
                text.Append("instance ");
            }
            text.Append(TypeName(method.ReturnType)).Append(' ');
            if (method.DeclaringType != null)
            {
                text.Append(TypeName(method.DeclaringType)).Append("::");
            }
            text.Append(method.Name);

            GenericInstanceMethod instance = method as GenericInstanceMethod;
            if (instance != null && instance.GenericArguments.Count > 0)
            {
                text.Append('<');
                for (int i = 0; i < instance.GenericArguments.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(TypeName(instance.GenericArguments[i]));
                }
                text.Append('>');
            }

            text.Append('(');
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
            return TypeName(field.FieldType) + " " + TypeName(field.DeclaringType) + "::" + field.Name;
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

            ArrayType array = type as ArrayType;
            if (array != null)
            {
                return TypeName(array.ElementType) + "[" + new string(',', Math.Max(0, array.Rank - 1)) + "]";
            }

            ByReferenceType byReference = type as ByReferenceType;
            if (byReference != null)
            {
                return TypeName(byReference.ElementType) + "&";
            }

            PointerType pointer = type as PointerType;
            if (pointer != null)
            {
                return TypeName(pointer.ElementType) + "*";
            }

            // FullName keeps the namespace ("System.ValueType") and the nested separator
            // ("Outer/Inner"), both of which are required for the text to reassemble.
            return ScopedName(type);
        }

        /// <summary>
        /// Prefixes the name with <c>[assembly]</c> when the type lives in another assembly,
        /// so the assembler resolves it against the right reference.
        /// </summary>
        private static string ScopedName(TypeReference type)
        {
            string fullName = type.FullName;
            IMetadataScope scope = type.Scope;
            AssemblyNameReference reference = scope as AssemblyNameReference;
            if (reference == null || string.Equals(reference.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                return fullName;
            }

            return "[" + reference.Name + "]" + fullName;
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
            if (type.IsSerializable) parts.Add("serializable");
            if (type.IsAutoClass) parts.Add("autoclass");
            else if (type.IsUnicodeClass) parts.Add("unicode");
            if (type.IsImport) parts.Add("import");
            if (type.HasSecurity) parts.Add("hassecurity");
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
            if (field.IsSpecialName) parts.Add("specialname");
            if (field.IsRuntimeSpecialName) parts.Add("rtspecialname");
            if (field.IsPublic) parts.Add("public");
            else if (field.IsFamily) parts.Add("family");
            else if (field.IsAssembly) parts.Add("assembly");
            else if (field.IsFamilyOrAssembly) parts.Add("famorassem");
            else if (field.IsFamilyAndAssembly) parts.Add("famandassem");
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
            if (method.IsPInvokeImpl)
            {
                parts.Add(PInvokeText(method));
            }
            if (method.IsPublic) parts.Add("public");
            else if (method.IsFamily) parts.Add("family");
            else if (method.IsAssembly) parts.Add("assembly");
            else parts.Add("private");
            parts.Add(method.IsConstructor && method.IsStatic ? "cctor" : (method.IsConstructor ? "ctor" : "instance"));
            return string.Join(" ", parts.ToArray());
        }

        /// <summary>Renders the <c>pinvokeimpl("dll" winapi)</c> clause.</summary>
        private static string PInvokeText(MethodDefinition method)
        {
            if (!method.HasPInvokeInfo || method.PInvokeInfo == null)
            {
                return "pinvokeimpl";
            }

            PInvokeInfo info = method.PInvokeInfo;
            StringBuilder text = new StringBuilder("pinvokeimpl(\"");
            text.Append(info.Module == null ? string.Empty : info.Module.Name).Append('"');

            switch (info.Attributes & PInvokeAttributes.CallConvMask)
            {
                case PInvokeAttributes.CallConvCdecl: text.Append(" cdecl"); break;
                case PInvokeAttributes.CallConvStdCall: text.Append(" stdcall"); break;
                case PInvokeAttributes.CallConvThiscall: text.Append(" thiscall"); break;
                case PInvokeAttributes.CallConvFastcall: text.Append(" fastcall"); break;
                default: text.Append(" winapi"); break;
            }

            if ((info.Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi)
            {
                text.Append(" charset=ansi");
            }
            else if ((info.Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode)
            {
                text.Append(" charset=unicode");
            }
            else if ((info.Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAuto)
            {
                text.Append(" charset=auto");
            }

            if ((info.Attributes & PInvokeAttributes.NoMangle) != 0)
            {
                text.Append(" nomangle");
            }
            if ((info.Attributes & PInvokeAttributes.SupportsLastError) != 0)
            {
                text.Append(" lasterr");
            }

            return text.Append(')').ToString();
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
                else if (c == '\\')
                {
                    text.Append("\\\\");
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
