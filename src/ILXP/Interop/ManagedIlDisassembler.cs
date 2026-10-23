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

            if (type.HasGenericParameters)
            {
                text.Append(pad).Append("  .genericparams (");
                for (int i = 0; i < type.GenericParameters.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(type.GenericParameters[i].Name);
                }
                text.AppendLine(")");
            }

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

            // Nested types come last; a method body that refers to one is handled by the
            // assembler through a forward reference.
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

            if (method.HasGenericParameters)
            {
                text.Append(" genericparams (");
                for (int i = 0; i < method.GenericParameters.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(method.GenericParameters[i].Name);
                }
                text.Append(')');
            }

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

            // Explicit interface implementations and method overrides live in the MethodImpl
            // table; without them interface dispatch fails at runtime.
            foreach (MethodReference @override in method.Overrides)
            {
                text.Append(pad).Append("  .override ").AppendLine(MemberName(@override));
            }
            if (method.Module != null && ReferenceEquals(method.Module.EntryPoint, method))
            {
                text.Append(pad).AppendLine("  .entrypoint");
            }

            for (int i = 0; i < body.Variables.Count; i++)
            {
                VariableDefinition variable = body.Variables[i];
                text.Append(pad).Append("  .locals init (").Append(TypeName(variable.VariableType)).Append(' ').AppendLine(LocalName(variable) + ")");
            }

            Dictionary<Instruction, int> indices = new Dictionary<Instruction, int>(body.Instructions.Count);
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                indices[body.Instructions[i]] = i;
            }

            List<HandlerGroup> groups = BuildHandlerGroups(body, indices);
            WriteRange(text, method, groups, indices, 0, body.Instructions.Count, pad + "  ");

            text.Append(pad).AppendLine("}");
            text.AppendLine();
        }

        /// <summary>
        /// All exception handlers that share one protected region. ILAsm writes such a group as
        /// a single <c>.try</c> block followed by one clause per handler, so the handlers are
        /// grouped here to keep that structure.
        /// </summary>
        private sealed class HandlerGroup
        {
            public int TryStart;
            public int TryEnd;
            public int SpanEnd;
            public List<ExceptionHandler> Handlers = new List<ExceptionHandler>();
        }

        private static List<HandlerGroup> BuildHandlerGroups(MethodBody body,
            Dictionary<Instruction, int> indices)
        {
            List<HandlerGroup> groups = new List<HandlerGroup>();
            if (!body.HasExceptionHandlers)
            {
                return groups;
            }

            foreach (ExceptionHandler handler in body.ExceptionHandlers)
            {
                int tryStart;
                int tryEnd;
                if (!indices.TryGetValue(handler.TryStart, out tryStart) ||
                    !indices.TryGetValue(handler.TryEnd, out tryEnd))
                {
                    Log.Warn("Exception handler with unknown boundaries skipped.");
                    continue;
                }

                HandlerGroup group = null;
                foreach (HandlerGroup candidate in groups)
                {
                    if (candidate.TryStart == tryStart && candidate.TryEnd == tryEnd)
                    {
                        group = candidate;
                        break;
                    }
                }

                if (group == null)
                {
                    group = new HandlerGroup { TryStart = tryStart, TryEnd = tryEnd };
                    groups.Add(group);
                }

                group.Handlers.Add(handler);

                // The construct ends where the last block of the group ends.
                int handlerEnd = indices.ContainsKey(handler.HandlerEnd) ? indices[handler.HandlerEnd] : tryEnd;
                if (handlerEnd > group.SpanEnd)
                {
                    group.SpanEnd = handlerEnd;
                }
            }

            foreach (HandlerGroup group in groups)
            {
                group.Handlers.Sort(delegate(ExceptionHandler a, ExceptionHandler b)
                {
                    int left = indices.ContainsKey(a.HandlerStart) ? indices[a.HandlerStart] : 0;
                    int right = indices.ContainsKey(b.HandlerStart) ? indices[b.HandlerStart] : 0;
                    return left.CompareTo(right);
                });
            }

            groups.Sort(delegate(HandlerGroup a, HandlerGroup b) { return a.TryStart.CompareTo(b.TryStart); });
            return groups;
        }

        /// <summary>True when no other parameter of the same method shares this name.</summary>
        private static bool IsUniqueParameterName(ParameterDefinition parameter)
        {
            IMethodSignature method = parameter.Method;
            if (method == null)
            {
                return true;
            }

            int matches = 0;
            foreach (ParameterDefinition candidate in method.Parameters)
            {
                if (string.Equals(candidate.Name, parameter.Name, StringComparison.Ordinal))
                {
                    matches++;
                    if (matches > 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Name used for a local in the IL text: the slot index plus the PDB name, if any.
        /// The index prefix makes the name unique — compilers reuse names such as "value",
        /// and instructions refer to locals by name.
        /// </summary>
        private static string LocalName(VariableDefinition variable)
        {
            string prefix = "V_" + variable.Index.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(variable.Name))
            {
                return prefix;
            }

            StringBuilder text = new StringBuilder(prefix).Append('_');
            foreach (char c in variable.Name)
            {
                text.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return text.ToString();
        }

        /// <summary>Emits instructions [start, end) and the protected regions nested in them.</summary>
        private static void WriteRange(StringBuilder text, MethodDefinition method, List<HandlerGroup> groups,
            Dictionary<Instruction, int> indices, int start, int end, string pad)
        {
            int cursor = start;
            foreach (HandlerGroup group in DirectChildren(groups, start, end))
            {
                WriteInstructions(text, method, cursor, group.TryStart, pad);
                WriteGroup(text, method, groups, indices, group, pad);
                cursor = group.SpanEnd;
            }
            WriteInstructions(text, method, cursor, end, pad);
        }

        /// <summary>
        /// Groups contained in [start, end) that are not contained in another such group.
        /// Choosing only the outermost ones keeps every construct emitted exactly once.
        /// </summary>
        private static List<HandlerGroup> DirectChildren(List<HandlerGroup> groups, int start, int end)
        {
            List<HandlerGroup> inside = new List<HandlerGroup>();
            foreach (HandlerGroup group in groups)
            {
                if (group.TryStart >= start && group.SpanEnd <= end)
                {
                    inside.Add(group);
                }
            }

            List<HandlerGroup> direct = new List<HandlerGroup>();
            foreach (HandlerGroup group in inside)
            {
                bool nested = false;
                foreach (HandlerGroup other in inside)
                {
                    if (ReferenceEquals(other, group))
                    {
                        continue;
                    }
                    if (other.TryStart <= group.TryStart && other.SpanEnd >= group.SpanEnd)
                    {
                        nested = true;
                        break;
                    }
                }
                if (!nested)
                {
                    direct.Add(group);
                }
            }

            direct.Sort(delegate(HandlerGroup a, HandlerGroup b) { return a.TryStart.CompareTo(b.TryStart); });
            return direct;
        }

        /// <summary>Emits one <c>.try</c> block with its <c>catch</c>/<c>finally</c> clauses.</summary>
        private static void WriteGroup(StringBuilder text, MethodDefinition method, List<HandlerGroup> groups,
            Dictionary<Instruction, int> indices, HandlerGroup group, string pad)
        {
            text.Append(pad).AppendLine(".try");
            text.Append(pad).AppendLine("{");
            WriteRange(text, method, groups, indices, group.TryStart, group.TryEnd, pad + "  ");
            text.Append(pad).AppendLine("}");

            foreach (ExceptionHandler handler in group.Handlers)
            {
                int handlerStart = indices.ContainsKey(handler.HandlerStart) ? indices[handler.HandlerStart] : group.TryEnd;
                int handlerEnd = indices.ContainsKey(handler.HandlerEnd) ? indices[handler.HandlerEnd] : group.TryEnd;

                if (handler.HandlerType == ExceptionHandlerType.Filter)
                {
                    int filterStart = indices.ContainsKey(handler.FilterStart) ? indices[handler.FilterStart] : handlerStart;
                    text.Append(pad).AppendLine("filter");
                    text.Append(pad).AppendLine("{");
                    WriteRange(text, method, groups, indices, filterStart, handlerStart, pad + "  ");
                    text.Append(pad).AppendLine("}");
                }
                else if (handler.HandlerType == ExceptionHandlerType.Catch)
                {
                    text.Append(pad).Append("catch ").AppendLine(TypeName(handler.CatchType));
                }
                else if (handler.HandlerType == ExceptionHandlerType.Finally)
                {
                    text.Append(pad).AppendLine("finally");
                }
                else
                {
                    text.Append(pad).AppendLine("fault");
                }

                text.Append(pad).AppendLine("{");
                WriteRange(text, method, groups, indices, handlerStart, handlerEnd, pad + "  ");
                text.Append(pad).AppendLine("}");
            }
        }

        private static void WriteInstructions(StringBuilder text, MethodDefinition method, int start, int end,
            string pad)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            for (int i = start; i < end && i < instructions.Count; i++)
            {
                Instruction instruction = instructions[i];
                text.Append(pad).Append("IL_").Append(instruction.Offset.ToString("x4", CultureInfo.InvariantCulture)).Append(": ");
                text.Append(OpCodeText(instruction));
                text.AppendLine();
            }
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
                return name + " " + LocalName((VariableDefinition)operand);
            }

            if (operand is ParameterDefinition)
            {
                ParameterDefinition parameter = (ParameterDefinition)operand;
                if (!string.IsNullOrEmpty(parameter.Name) && IsUniqueParameterName(parameter))
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

            // Generic parameters must be written by position (!0 / !!0), not by name: a
            // literal "T" would be read back as a type called T.
            GenericParameter parameter = type as GenericParameter;
            if (parameter != null)
            {
                return (parameter.Type == GenericParameterType.Method ? "!!" : "!") +
                       parameter.Position.ToString(CultureInfo.InvariantCulture);
            }

            switch (type.MetadataType)
            {
                case MetadataType.Void: return "void";                case MetadataType.Boolean: return "bool";
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
            GenericInstanceType generic = type as GenericInstanceType;
            if (generic != null)
            {
                // FullName does not carry scope prefixes for the generic arguments, and an
                // argument that lives in this module must be marked as such, otherwise a
                // forward reference would be resolved against mscorlib.
                StringBuilder text = new StringBuilder(ScopePrefix(generic));
                text.Append(generic.ElementType.FullName).Append('<');
                for (int i = 0; i < generic.GenericArguments.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(TypeName(generic.GenericArguments[i]));
                }
                return text.Append('>').ToString();
            }

            // A nested type of an instantiated generic type ("List<T>/Enumerator") must keep
            // the parent's arguments, written at the end of the name, or the reference would
            // point at the open definition.
            if (type.IsNested && type.DeclaringType != null)
            {
                List<TypeReference> arguments = new List<TypeReference>();
                TypeReference outermost = null;
                TypeReference cursor = type.DeclaringType;
                while (cursor != null)
                {
                    GenericInstanceType instance = cursor as GenericInstanceType;
                    if (instance != null)
                    {
                        if (outermost == null)
                        {
                            outermost = instance.ElementType;
                        }
                        foreach (TypeReference argument in instance.GenericArguments)
                        {
                            arguments.Add(argument);
                        }
                    }
                    cursor = cursor.DeclaringType;
                }

                if (arguments.Count > 0)
                {
                    StringBuilder text = new StringBuilder(ScopePrefix(type));
                    text.Append(outermost == null ? type.DeclaringType.FullName : outermost.FullName);
                    text.Append('/').Append(type.Name).Append('<');
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            text.Append(", ");
                        }
                        text.Append(TypeName(arguments[i]));
                    }
                    return text.Append('>').ToString();
                }
            }

            return ScopePrefix(type) + type.FullName;
        }

        /// <summary>Returns "" for mscorlib, "[asm]" otherwise (including "[.module]").</summary>
        private static string ScopePrefix(TypeReference type)
        {
            IMetadataScope scope = type.Scope;

            // A type defined in this very module is written as "[.module]" so the assembler
            // does not mistake it for a mscorlib type; that matters for forward references,
            // i.e. when a base type or interface is declared further down in the file.
            if (scope is ModuleDefinition)
            {
                return "[.module]";
            }

            AssemblyNameReference reference = scope as AssemblyNameReference;
            if (reference == null || string.Equals(reference.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return "[" + reference.Name + "]";
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
            if (method.IsFinal) parts.Add("final");
            if ((method.Attributes & MethodAttributes.CheckAccessOnOverride) != 0) parts.Add("strict");
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
