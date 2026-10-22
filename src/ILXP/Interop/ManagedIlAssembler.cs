using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace ILXP.Interop
{
    /// <summary>
    /// Managed IL assembler: reads the IL text produced by
    /// <see cref="ManagedIlDisassembler"/> (and the common ILAsm subset) and writes a new
    /// assembly with Cecil. No external <c>ilasm.exe</c> is required.
    ///
    /// Supported: assembly header, extern references, module name, classes with
    /// base type, fields with constants, methods with parameters, locals, maxstack and a
    /// broad instruction subset. Unsupported directives are skipped and reported through
    /// <see cref="LastWarnings"/> instead of failing the whole assembly.
    /// </summary>
    internal static class ManagedIlAssembler
    {
        private sealed class MethodContext
        {
            public MethodDefinition Method;
            public Dictionary<string, Instruction> Labels = new Dictionary<string, Instruction>(StringComparer.OrdinalIgnoreCase);
            public List<KeyValuePair<Instruction, string>> PendingTargets = new List<KeyValuePair<Instruction, string>>();
        }

        public static List<string> LastWarnings { get; private set; }

        public static AssemblyDefinition Assemble(string ilText, string assemblyName, bool isDll)
        {
            if (ilText == null)
            {
                throw new ArgumentNullException("ilText");
            }

            LastWarnings = new List<string>();
            ModuleDefinition module = ModuleDefinition.CreateModule(assemblyName, isDll ? ModuleKind.Dll : ModuleKind.Console);
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
                assemblyName,
                isDll ? ModuleKind.Dll : ModuleKind.Console);

            TypeDefinition currentType = null;
            MethodDefinition currentMethod = null;
            MethodContext methodContext = null;
            int braceDepth = 0;

            string[] lines = ilText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string rawLine in lines)
            {
                string line = StripComment(rawLine).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                // Track braces so nested blocks are understood.
                int open = Count(line, '{');
                int close = Count(line, '}');

                if (line.StartsWith(".assembly", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // handled by the module/assembly identity above
                }
                if (line.StartsWith(".module", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (line.StartsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = CreateType(assembly, line);
                    continue;
                }
                if (line.StartsWith(".field", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        CreateField(currentType, line);
                    }
                    continue;
                }
                if (line.StartsWith(".method", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        currentMethod = CreateMethod(currentType, line);
                        methodContext = currentMethod == null ? null : new MethodContext { Method = currentMethod };
                    }
                    continue;
                }
                if (currentMethod != null)
                {
                    if (line.StartsWith(".maxstack", StringComparison.OrdinalIgnoreCase))
                    {
                        int value;
                        if (int.TryParse(line.Substring(9).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        {
                            currentMethod.Body.MaxStackSize = value;
                        }
                        continue;
                    }
                    if (line.StartsWith(".locals", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseLocals(currentMethod, line);
                        continue;
                    }
                    if (line.StartsWith(".try", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith(".catch", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith(".finally", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith(".filter", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith(".fault", StringComparison.OrdinalIgnoreCase))
                    {
                        LastWarnings.Add("Exception handler directive skipped: " + line);
                        continue;
                    }
                    if (line.StartsWith("IL_", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseInstruction(methodContext, line);
                    }
                }

                braceDepth += open - close;
                if (braceDepth <= 0)
                {
                    if (currentMethod != null)
                    {
                        ResolveBranches(methodContext);
                        currentMethod = null;
                        methodContext = null;
                    }
                    else if (currentType != null && line.IndexOf('}') >= 0)
                    {
                        currentType = null;
                    }
                    braceDepth = 0;
                }
            }

            return assembly;
        }

        public static void AssembleFile(string ilPath, string outputPath, bool isDll)
        {
            string text = File.ReadAllText(ilPath);
            string name = Path.GetFileNameWithoutExtension(outputPath);
            AssemblyDefinition assembly = Assemble(text, name, isDll);
            Sanitize(assembly);
            assembly.Write(outputPath);
        }

        /// <summary>
        /// Guarantees that every instruction has an operand of the kind its opcode
        /// requires. Anything that cannot be satisfied becomes a <c>nop</c>, which keeps
        /// the produced assembly valid no matter how damaged the input IL text was.
        /// </summary>
        private static void Sanitize(AssemblyDefinition assembly)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    SanitizeType(type);
                }
            }
        }

        private static void SanitizeType(TypeDefinition type)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (Instruction instruction in method.Body.Instructions)
                {
                    bool valid;
                    switch (instruction.OpCode.OperandType)
                    {
                        case OperandType.InlineNone:
                            valid = instruction.Operand == null;
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            valid = instruction.Operand is Instruction;
                            break;
                        case OperandType.ShortInlineI:
                            valid = instruction.Operand is sbyte;
                            break;
                        case OperandType.InlineI:
                            valid = instruction.Operand is int;
                            break;
                        case OperandType.InlineI8:
                            valid = instruction.Operand is long;
                            break;
                        case OperandType.ShortInlineR:
                            valid = instruction.Operand is float;
                            break;
                        case OperandType.InlineR:
                            valid = instruction.Operand is double;
                            break;
                        case OperandType.InlineString:
                            valid = instruction.Operand is string;
                            break;
                        case OperandType.InlineMethod:
                            valid = instruction.Operand is MethodReference;
                            break;
                        case OperandType.InlineField:
                            valid = instruction.Operand is FieldReference;
                            break;
                        case OperandType.InlineType:
                        case OperandType.InlineTok:
                            valid = instruction.Operand is TypeReference ||
                                    instruction.Operand is MethodReference ||
                                    instruction.Operand is FieldReference;
                            break;
                        case OperandType.ShortInlineVar:
                        case OperandType.InlineVar:
                            valid = instruction.Operand is VariableDefinition ||
                                    instruction.Operand is ParameterDefinition;
                            break;
                        default:
                            valid = false;
                            break;
                    }

                    if (!valid)
                    {
                        LastWarnings.Add("Instruction sanitized to nop: " + instruction.OpCode.Name);
                        instruction.OpCode = OpCodes.Nop;
                        instruction.Operand = null;
                    }
                }

                if (method.Body.MaxStackSize < 8)
                {
                    method.Body.MaxStackSize = 8;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                SanitizeType(nested);
            }
        }

        private static TypeDefinition CreateType(AssemblyDefinition assembly, string line)
        {
            string rest = line.Substring(".class".Length).Trim();
            string extends = null;
            int extendsIndex = rest.IndexOf(" extends ", StringComparison.OrdinalIgnoreCase);
            if (extendsIndex >= 0)
            {
                extends = rest.Substring(extendsIndex + 9).Trim();
                rest = rest.Substring(0, extendsIndex).Trim();
            }

            bool isInterface = rest.IndexOf("interface", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isAbstract = rest.IndexOf("abstract", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSealed = rest.IndexOf("sealed", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPublic = rest.IndexOf("public", StringComparison.OrdinalIgnoreCase) >= 0;

            string name = ExtractQuoted(rest);
            if (string.IsNullOrEmpty(name))
            {
                int lastSpace = rest.LastIndexOf(' ');
                name = lastSpace >= 0 ? rest.Substring(lastSpace + 1) : rest;
            }

            string namespaceName = string.Empty;
            int dot = name.LastIndexOf('.');
            if (dot > 0)
            {
                namespaceName = name.Substring(0, dot);
                name = name.Substring(dot + 1);
            }

            TypeAttributes attributes = TypeAttributes.Class;
            if (isInterface)
            {
                attributes = TypeAttributes.Interface | TypeAttributes.Abstract;
            }
            else
            {
                if (isAbstract) attributes |= TypeAttributes.Abstract;
                if (isSealed) attributes |= TypeAttributes.Sealed;
            }
            attributes |= isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic;

            TypeDefinition type = new TypeDefinition(namespaceName, name, attributes);
            if (!string.IsNullOrEmpty(extends))
            {
                type.BaseType = ResolveType(assembly.MainModule, extends);
            }
            else if (!isInterface)
            {
                type.BaseType = assembly.MainModule.TypeSystem.Object;
            }
            assembly.MainModule.Types.Add(type);
            return type;
        }

        private static void CreateField(TypeDefinition type, string line)
        {
            string rest = line.Substring(".field".Length).Trim();
            bool isStatic = rest.IndexOf("static", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isInitOnly = rest.IndexOf("initonly", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isLiteral = rest.IndexOf("literal", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPublic = rest.IndexOf("public", StringComparison.OrdinalIgnoreCase) >= 0;

            int equals = rest.IndexOf('=');
            string declaration = equals >= 0 ? rest.Substring(0, equals).Trim() : rest;

            // The declaration is "<modifiers...> <type> <name>": take the last two tokens
            // so that any combination of attribute keywords is ignored.
            int lastSpace = declaration.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                return;
            }
            int typeSpace = declaration.LastIndexOf(' ', lastSpace - 1);

            string typeName = typeSpace > 0 ? declaration.Substring(typeSpace + 1, lastSpace - typeSpace - 1).Trim() : declaration.Substring(0, lastSpace).Trim();
            string fieldName = declaration.Substring(lastSpace + 1).Trim();

            FieldAttributes attributes = isPublic ? FieldAttributes.Public : FieldAttributes.Private;
            if (isStatic) attributes |= FieldAttributes.Static;
            if (isInitOnly) attributes |= FieldAttributes.InitOnly;
            if (isLiteral) attributes |= FieldAttributes.Literal | FieldAttributes.Static;

            FieldDefinition field = new FieldDefinition(fieldName, attributes, ResolveType(type.MainModule(), typeName));
            if (equals >= 0)
            {
                string value = rest.Substring(equals + 1).Trim();
                object constant = ParseConstant(value);
                if (constant != null && IsConstantCompatible(field.FieldType))
                {
                    field.Constant = constant;
                }
            }
            type.Fields.Add(field);
        }

        private static bool IsConstantCompatible(TypeReference type)
        {
            if (type == null)
            {
                return false;
            }

            switch (type.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.Char:
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                case MetadataType.Int64:
                case MetadataType.UInt64:
                case MetadataType.Single:
                case MetadataType.Double:
                case MetadataType.String:
                    return true;
                default:
                    return false;
            }
        }

        private static MethodDefinition CreateMethod(TypeDefinition type, string line)
        {
            string rest = line.Substring(".method".Length).Trim();
            int paren = rest.IndexOf('(');
            int closeParen = paren >= 0 ? rest.IndexOf(')', paren) : -1;
            if (paren < 0 || closeParen < 0)
            {
                return null;
            }

            string beforeParen = rest.Substring(0, paren).Trim();
            string parameters = rest.Substring(paren + 1, closeParen - paren - 1);

            int lastSpace = beforeParen.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                return null;
            }

            string signature = beforeParen.Substring(0, lastSpace).Trim();
            string methodName = beforeParen.Substring(lastSpace + 1).Trim();
            if (methodName.StartsWith("'") && methodName.EndsWith("'"))
            {
                methodName = methodName.Substring(1, methodName.Length - 2);
            }

            int returnSpace = signature.LastIndexOf(' ');
            string returnTypeName = returnSpace >= 0 ? signature.Substring(returnSpace + 1) : signature;

            bool isStatic = signature.IndexOf("static", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPublic = signature.IndexOf("public", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isVirtual = signature.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isAbstract = signature.IndexOf("abstract", StringComparison.OrdinalIgnoreCase) >= 0;

            MethodAttributes attributes = isPublic ? MethodAttributes.Public : MethodAttributes.Private;
            if (isStatic) attributes |= MethodAttributes.Static;
            if (isVirtual) attributes |= MethodAttributes.Virtual;
            if (isAbstract) attributes |= MethodAttributes.Abstract;
            if (methodName == ".ctor" || methodName == ".cctor")
            {
                attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                if (methodName == ".cctor")
                {
                    attributes |= MethodAttributes.Static;
                }
            }

            MethodDefinition method = new MethodDefinition(
                methodName,
                attributes,
                ResolveType(type.MainModule(), returnTypeName));
            method.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;

            if (parameters.Length > 0)
            {
                foreach (string part in SplitTopLevel(parameters))
                {
                    string parameter = part.Trim();
                    if (parameter.Length == 0)
                    {
                        continue;
                    }

                    int space = parameter.LastIndexOf(' ');
                    string parameterType = space > 0 ? parameter.Substring(0, space).Trim() : parameter;
                    string parameterName = space > 0 ? parameter.Substring(space + 1).Trim() : "arg" + method.Parameters.Count;
                    method.Parameters.Add(new ParameterDefinition(parameterName, ParameterAttributes.None, ResolveType(type.MainModule(), parameterType)));
                }
            }

            if (!isAbstract)
            {
                method.Body = new MethodBody(method);
            }
            type.Methods.Add(method);
            return method;
        }

        private static void ParseLocals(MethodDefinition method, string line)
        {
            int paren = line.IndexOf('(');
            int close = line.LastIndexOf(')');
            if (paren < 0 || close <= paren)
            {
                return;
            }

            string content = line.Substring(paren + 1, close - paren - 1);
            foreach (string part in SplitTopLevel(content))
            {
                string declaration = part.Trim();
                if (declaration.Length == 0)
                {
                    continue;
                }

                int space = declaration.LastIndexOf(' ');
                string typeName = space > 0 ? declaration.Substring(0, space).Trim() : declaration;
                string name = space > 0 ? declaration.Substring(space + 1).Trim() : "V_" + method.Body.Variables.Count;

                if (method.Body == null)
                {
                    method.Body = new MethodBody(method);
                }
                method.Body.Variables.Add(new VariableDefinition(name, ResolveType(method.Module, typeName)));
                method.Body.InitLocals = true;
            }
        }

        private static void ParseInstruction(MethodContext context, string line)
        {
            int colon = line.IndexOf(':');
            if (colon < 0)
            {
                return;
            }

            string label = line.Substring(0, colon).Trim();
            string body = line.Substring(colon + 1).Trim();
            if (body.Length == 0)
            {
                return;
            }

            int space = body.IndexOf(' ');
            string opcodeName = space < 0 ? body : body.Substring(0, space);
            string operandText = space < 0 ? string.Empty : body.Substring(space + 1).Trim();

            OpCode opcode = FindOpCode(opcodeName);
            if (opcode.Size == 0 && opcode.Value == 0)
            {
                LastWarnings.Add("Unknown opcode skipped: " + opcodeName);
                return;
            }

            object operand = ParseOperand(context, opcode, operandText);
            if (operand == Unresolved || !IsOperandValid(opcode, operand))
            {
                // Member/type tokens need a full metadata resolver. The instruction is
                // replaced by a NOP so that the rest of the method (and the assembly)
                // stays valid; the original text is kept in the warning list.
                LastWarnings.Add("Instruction downgraded to nop: " + line);
                opcode = OpCodes.Nop;
                operand = null;
            }

            Instruction instruction = Instruction.Create(OpCodes.Nop);
            instruction.OpCode = opcode;
            instruction.Operand = operand;

            context.Method.Body.Instructions.Add(instruction);
            context.Labels[label] = instruction;

            if (operandText.StartsWith("IL_", StringComparison.OrdinalIgnoreCase))
            {
                context.PendingTargets.Add(new KeyValuePair<Instruction, string>(instruction, operandText));
            }
            else if (operandText.IndexOf("IL_", StringComparison.OrdinalIgnoreCase) > 0)
            {
                context.PendingTargets.Add(new KeyValuePair<Instruction, string>(instruction, operandText));
            }
        }

        /// <summary>Sentinel returned by <see cref="ParseOperand"/> for tokens we cannot resolve.</summary>
        private static readonly object Unresolved = new object();

        /// <summary>
        /// Checks that the operand type matches what the opcode expects. Mismatches are
        /// downgraded to <c>nop</c> instead of producing an invalid assembly.
        /// </summary>
        private static bool IsOperandValid(OpCode opcode, object operand)
        {
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    return operand == null;
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    return operand == null; // resolved later
                case OperandType.ShortInlineI:
                    return operand is sbyte;
                case OperandType.InlineI:
                    return operand is int;
                case OperandType.InlineI8:
                    return operand is long;
                case OperandType.ShortInlineR:
                    return operand is float;
                case OperandType.InlineR:
                    return operand is double;
                case OperandType.InlineString:
                    return operand is string;
                case OperandType.InlineMethod:
                    return operand is MethodReference;
                case OperandType.InlineField:
                    return operand is FieldReference;
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    return operand is TypeReference || operand is MethodReference || operand is FieldReference;
                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                    return operand is VariableDefinition || operand is ParameterDefinition;
                default:
                    return false;
            }
        }

        private static object ParseOperand(MethodContext context, OpCode opcode, string text)
        {
            ModuleDefinition moduleOfText = context.Method.Module;
            if (text.Length == 0)
            {
                return null;
            }

            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    return null;

                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    return null; // resolved later

                case OperandType.ShortInlineI:
                    return (sbyte)ParseInt(text);
                case OperandType.InlineI:
                    return ParseInt(text);

                case OperandType.InlineI8:
                {
                    long value;
                    return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0L;
                }

                case OperandType.ShortInlineR:
                {
                    float value;
                    return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0f;
                }

                case OperandType.InlineR:
                {
                    double value;
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0d;
                }

                case OperandType.InlineString:
                    return Unquote(text);

                case OperandType.InlineMethod:
                    return ResolveMethodOperand(moduleOfText, text);
                case OperandType.InlineField:
                    return ResolveFieldOperand(moduleOfText, text);
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    return ResolveTypeOperand(moduleOfText, text);
                case OperandType.InlineSig:
                case OperandType.InlineSwitch:
                    return Unresolved;

                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                {
                    // Local variables: match by name, fall back to the numeric index.
                    string name = text.Trim();
                    System.Collections.Generic.IList<VariableDefinition> variables =
                        context.Method.Body == null ? null : context.Method.Body.Variables;
                    if (variables != null)
                    {
                        foreach (VariableDefinition variable in variables)
                        {
                            if (string.Equals(variable.Name, name, StringComparison.Ordinal))
                            {
                                return variable;
                            }
                        }
                    }
                    int index;
                    if (variables != null &&
                        int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) &&
                        index >= 0 && index < variables.Count)
                    {
                        return variables[index];
                    }
                    return Unresolved;
                }

                default:
                    return null;
            }
        }

        private static void ResolveBranches(MethodContext context)
        {
            if (context == null || context.Method == null || context.Method.Body == null)
            {
                return;
            }

            foreach (KeyValuePair<Instruction, string> pending in context.PendingTargets)
            {
                string text = pending.Value;
                int index = text.IndexOf("IL_", StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    continue;
                }

                int end = index + 3;
                while (end < text.Length && Uri.IsHexDigit(text[end]))
                {
                    end++;
                }

                string label = text.Substring(index, end - index);
                Instruction target;
                if (context.Labels.TryGetValue(label, out target))
                {
                    pending.Key.Operand = target;
                }
                else
                {
                    // Unresolved label: a branch with a null target would break the
                    // metadata writer, so the instruction becomes a nop.
                    LastWarnings.Add("Unresolved branch target " + label + "; instruction downgraded to nop.");
                    pending.Key.OpCode = OpCodes.Nop;
                    pending.Key.Operand = null;
                }
            }
        }

        private static OpCode FindOpCode(string name)
        {
            foreach (FieldInfo field in typeof(OpCodes).GetFields())
            {
                if (field.FieldType != typeof(OpCode))
                {
                    continue;
                }
                OpCode candidate = (OpCode)field.GetValue(null);
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
            return OpCodes.Nop;
        }

        private static TypeReference ResolveType(ModuleDefinition module, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return module.TypeSystem.Object;
            }

            switch (name)
            {
                case "void": return module.TypeSystem.Void;
                case "bool":
                case "boolean": return module.TypeSystem.Boolean;
                case "char": return module.TypeSystem.Char;
                case "int8": return module.TypeSystem.SByte;
                case "uint8": return module.TypeSystem.Byte;
                case "int16": return module.TypeSystem.Int16;
                case "uint16": return module.TypeSystem.UInt16;
                case "int32":
                case "int": return module.TypeSystem.Int32;
                case "uint32":
                case "uint": return module.TypeSystem.UInt32;
                case "int64":
                case "long": return module.TypeSystem.Int64;
                case "uint64":
                case "ulong": return module.TypeSystem.UInt64;
                case "float32":
                case "single": return module.TypeSystem.Single;
                case "float64":
                case "double": return module.TypeSystem.Double;
                case "string": return module.TypeSystem.String;
                case "object": return module.TypeSystem.Object;
            }

            string clean = name.Trim();
            while (clean.EndsWith("&", StringComparison.Ordinal))
            {
                clean = clean.Substring(0, clean.Length - 1);
            }

            bool isArray = clean.EndsWith("[]", StringComparison.Ordinal);
            if (isArray)
            {
                clean = clean.Substring(0, clean.Length - 2);
            }

            int generic = clean.IndexOf('<');
            if (generic > 0)
            {
                clean = clean.Substring(0, generic);
            }

            int dot = clean.LastIndexOf('.');
            string namespaceName = dot > 0 ? clean.Substring(0, dot) : string.Empty;
            string typeName = dot > 0 ? clean.Substring(dot + 1) : clean;

            TypeReference reference = new TypeReference(namespaceName, typeName, module, NormalizeScope(module, module.TypeSystem.Object.Scope));
            if (isArray)
            {
                reference = new ArrayType(reference);
            }
            return reference;
        }

        private static int ParseInt(string text)
        {
            int value;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) ? value : 0;
            }
            return 0;
        }

        private static object ResolveMethodOperand(ModuleDefinition module, string text)
        {
            try
            {
                int paren = text.IndexOf('(');
                string signature = paren >= 0 ? text.Substring(0, paren).Trim() : text.Trim();
                string parameters = paren >= 0 ? text.Substring(paren + 1, text.LastIndexOf(')') - paren - 1) : string.Empty;

                int separator = signature.IndexOf("::", StringComparison.Ordinal);
                if (separator < 0)
                {
                    return Unresolved;
                }

                string typePart = signature.Substring(0, separator).Trim();
                string methodName = signature.Substring(separator + 2).Trim();

                int space = typePart.LastIndexOf(' ');
                string returnTypeName = space > 0 ? typePart.Substring(0, space).Trim() : "void";
                string typeName = space > 0 ? typePart.Substring(space + 1).Trim() : typePart;

                TypeReference declaring = ResolveTypeOperand(module, typeName) as TypeReference;
                if (declaring == null)
                {
                    return Unresolved;
                }

                MethodReference method = new MethodReference(methodName, ResolveType(module, returnTypeName), declaring);
                method.HasThis = methodName != ".cctor";

                foreach (string part in SplitTopLevel(parameters))
                {
                    string parameter = part.Trim();
                    if (parameter.Length > 0)
                    {
                        method.Parameters.Add(new ParameterDefinition(ResolveType(module, parameter)));
                    }
                }
                return method;
            }
            catch (Exception)
            {
                return Unresolved;
            }
        }

        private static object ResolveFieldOperand(ModuleDefinition module, string text)
        {
            try
            {
                int separator = text.IndexOf("::", StringComparison.Ordinal);
                if (separator < 0)
                {
                    return Unresolved;
                }

                TypeReference declaring = ResolveTypeOperand(module, text.Substring(0, separator).Trim()) as TypeReference;
                string fieldName = text.Substring(separator + 2).Trim();
                if (declaring == null || fieldName.Length == 0)
                {
                    return Unresolved;
                }

                return new FieldReference(fieldName, module.TypeSystem.Object, declaring);
            }
            catch (Exception)
            {
                return Unresolved;
            }
        }

        private static object ResolveTypeOperand(ModuleDefinition module, string text)
        {
            try
            {
                text = text.Trim();
                IMetadataScope scope = NormalizeScope(module, module.TypeSystem.Object.Scope);

                if (text.StartsWith("[", StringComparison.Ordinal))
                {
                    int close = text.IndexOf(']');
                    if (close > 0)
                    {
                        string assemblyName = text.Substring(1, close - 1);
                        text = text.Substring(close + 1).Trim();
                        AssemblyNameReference reference = FindOrCreateReference(module, assemblyName);
                        if (reference != null)
                        {
                            scope = reference;
                        }
                    }
                }

                bool isArray = text.EndsWith("[]", StringComparison.Ordinal);
                if (isArray)
                {
                    text = text.Substring(0, text.Length - 2);
                }

                int generic = text.IndexOf('<');
                if (generic > 0)
                {
                    text = text.Substring(0, generic);
                }

                int dot = text.LastIndexOf('.');
                string namespaceName = dot > 0 ? text.Substring(0, dot) : string.Empty;
                string typeName = dot > 0 ? text.Substring(dot + 1) : text;
                if (typeName.Length == 0)
                {
                    return Unresolved;
                }

                TypeReference arrayElement = new TypeReference(namespaceName, typeName, module, NormalizeScope(module, scope));
                return isArray ? (object)new ArrayType(arrayElement) : arrayElement;
            }
            catch (Exception)
            {
                return Unresolved;
            }
        }

        private static IMetadataScope NormalizeScope(ModuleDefinition module, IMetadataScope scope)
        {
            // A module used as a scope means "this module"; external references need an
            // assembly reference instead, otherwise the metadata writer fails.
            if (scope is ModuleDefinition || scope == null)
            {
                return FindOrCreateReference(module, "mscorlib");
            }
            return scope;
        }

        private static AssemblyNameReference FindOrCreateReference(ModuleDefinition module, string name)
        {
            foreach (AssemblyNameReference existing in module.AssemblyReferences)
            {
                if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }

            AssemblyNameReference created = new AssemblyNameReference(name, new Version(4, 0, 0, 0));
            module.AssemblyReferences.Add(created);
            return created;
        }

        private static object ParseConstant(string text)
        {
            if (text.Length == 0)
            {
                return null;
            }
            if (text.StartsWith("'", StringComparison.Ordinal))
            {
                return Unquote(text);
            }
            if (text == "true")
            {
                return true;
            }
            if (text == "false")
            {
                return false;
            }

            int intValue;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
            {
                return intValue;
            }
            return null;
        }

        private static string Unquote(string text)
        {
            if (text.Length >= 2 && text[0] == '\'' && text[text.Length - 1] == '\'')
            {
                text = text.Substring(1, text.Length - 2);
            }
            return text.Replace("\\'", "'").Replace("\\\\", "\\");
        }

        private static string ExtractQuoted(string text)
        {
            int first = text.IndexOf('\'');
            if (first < 0)
            {
                return null;
            }
            int last = text.LastIndexOf('\'');
            if (last <= first)
            {
                return null;
            }
            return text.Substring(first + 1, last - first - 1);
        }

        private static string StripComment(string line)
        {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            if (index < 0)
            {
                return line;
            }
            return line.Substring(0, index);
        }

        private static int Count(string text, char value)
        {
            int count = 0;
            bool inString = false;
            foreach (char c in text)
            {
                if (c == '\'')
                {
                    inString = !inString;
                }
                else if (!inString && c == value)
                {
                    count++;
                }
            }
            return count;
        }

        private static IEnumerable<string> SplitTopLevel(string text)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            int depth = 0;
            bool inString = false;
            foreach (char c in text)
            {
                if (c == '\'')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '<' || c == '(' || c == '[')
                    {
                        depth++;
                    }
                    else if (c == '>' || c == ')' || c == ']')
                    {
                        depth--;
                    }
                    else if (c == ',' && depth == 0)
                    {
                        parts.Add(current.ToString());
                        current.Length = 0;
                        continue;
                    }
                }
                current.Append(c);
            }
            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }
            return parts;
        }

        private static ModuleDefinition MainModule(this TypeDefinition type)
        {
            return type.Module;
        }
    }
}
