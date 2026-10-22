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
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using EventAttributes = Mono.Cecil.EventAttributes;
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
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
                assemblyName,
                isDll ? ModuleKind.Dll : ModuleKind.Console);
            TypeDefinition currentType = null;
            MethodDefinition currentMethod = null;
            MethodContext methodContext = null;
            PropertyDefinition currentProperty = null;
            EventDefinition currentEvent = null;
            Stack<TypeDefinition> typeStack = new Stack<TypeDefinition>();
            List<KeyValuePair<FieldDefinition, string>> pendingFieldData =
                new List<KeyValuePair<FieldDefinition, string>>();
            Dictionary<string, byte[]> dataBlobs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            int typeDepth = 0;
            int methodDepth = 0;

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
                    if (line.IndexOf("extern", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        ParseAssemblyExtern(assembly, line);
                    }
                    continue; // the assembly identity itself comes from the parameters
                }
                if (line.StartsWith(".module", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (line.StartsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = CreateType(assembly, line);
                    if (currentType != null)
                    {
                        typeStack.Push(currentType);
                        typeDepth = open - close;
                    }
                    continue;
                }
                if (line.StartsWith(".field", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        string dataLabel;
                        FieldDefinition field = CreateField(currentType, line, out dataLabel);
                        if (field != null && !string.IsNullOrEmpty(dataLabel))
                        {
                            pendingFieldData.Add(new KeyValuePair<FieldDefinition, string>(field, dataLabel));
                        }
                    }
                    continue;
                }
                if (line.StartsWith(".data", StringComparison.OrdinalIgnoreCase))
                {
                    ParseData(line, dataBlobs);
                    continue;
                }
                if (line.StartsWith(".method", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        currentMethod = CreateMethod(currentType, line);
                        methodContext = currentMethod == null ? null : new MethodContext { Method = currentMethod };
                        methodDepth = open - close;
                    }
                    continue;
                }
                if (line.StartsWith(".property", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        currentProperty = CreateProperty(currentType, line);
                    }
                    continue;
                }
                if (line.StartsWith(".event", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        currentEvent = CreateEvent(currentType, line);
                    }
                    continue;
                }
                if (currentProperty != null || currentEvent != null)
                {
                    // Inside a .property/.event block only the accessor directives matter.
                    // The braces belong to this block and must not close the enclosing type.
                    if (line.StartsWith(".get", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProperty.GetMethod = FindMethod(currentType, ExtractQuoted(line));
                    }
                    else if (line.StartsWith(".set", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProperty.SetMethod = FindMethod(currentType, ExtractQuoted(line));
                    }
                    else if (line.StartsWith(".other", StringComparison.OrdinalIgnoreCase))
                    {
                        MethodDefinition other = FindMethod(currentType, ExtractQuoted(line));
                        if (other != null)
                        {
                            if (currentProperty != null)
                            {
                                currentProperty.OtherMethods.Add(other);
                            }
                            else
                            {
                                currentEvent.OtherMethods.Add(other);
                            }
                        }
                    }
                    else if (line.StartsWith(".addon", StringComparison.OrdinalIgnoreCase))
                    {
                        currentEvent.AddMethod = FindMethod(currentType, ExtractQuoted(line));
                    }
                    else if (line.StartsWith(".removeon", StringComparison.OrdinalIgnoreCase))
                    {
                        currentEvent.RemoveMethod = FindMethod(currentType, ExtractQuoted(line));
                    }
                    else if (line.StartsWith(".fire", StringComparison.OrdinalIgnoreCase))
                    {
                        currentEvent.InvokeMethod = FindMethod(currentType, ExtractQuoted(line));
                    }

                    if (line.IndexOf('}') >= 0)
                    {
                        currentProperty = null;
                        currentEvent = null;
                    }
                    continue;
                }
                if (currentMethod != null)
                {
                    if (currentMethod.Body == null)
                    {
                        // A native/runtime method has no IL; ignore a stray body rather than
                        // failing the whole assembly (damaged input must stay loadable).
                        if (line.StartsWith("IL_", StringComparison.OrdinalIgnoreCase))
                        {
                            LastWarnings.Add("Instruction in a body-less method skipped: " + line);
                        }
                        methodDepth += open - close;
                        if (methodDepth <= 0)
                        {
                            currentMethod = null;
                            methodContext = null;
                            methodDepth = 0;
                        }
                        continue;
                    }

                    if (line.StartsWith(".maxstack", StringComparison.OrdinalIgnoreCase))
                    {
                        int value;
                        if (int.TryParse(line.Substring(9).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        {
                            currentMethod.Body.MaxStackSize = value;
                        }
                        continue;
                    }
                    if (line.StartsWith(".entrypoint", StringComparison.OrdinalIgnoreCase))
                    {
                        assembly.MainModule.EntryPoint = currentMethod;
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

                    // A method owns its braces: nested .try blocks must not close the
                    // enclosing type, and every method must resolve its own branches.
                    methodDepth += open - close;
                    if (methodDepth <= 0)
                    {
                        ResolveBranches(methodContext);
                        currentMethod = null;
                        methodContext = null;
                        methodDepth = 0;
                    }
                    continue;
                }

                typeDepth += open - close;
                if (typeDepth <= 0 && line.IndexOf('}') >= 0)
                {
                    if (typeStack.Count > 0)
                    {
                        typeStack.Pop();
                    }
                    currentType = typeStack.Count > 0 ? typeStack.Peek() : null;
                    typeDepth = 0;
                }
            }
            foreach (KeyValuePair<FieldDefinition, string> pending in pendingFieldData)
            {
                byte[] bytes;
                if (dataBlobs.TryGetValue(pending.Value, out bytes))
                {
                    pending.Key.InitialValue = bytes;
                    pending.Key.Attributes |= FieldAttributes.HasFieldRVA;
                }
                else
                {
                    LastWarnings.Add("Field data " + pending.Value + " was not found for " + pending.Key.Name);
                }
            }

            return assembly;
        }

        /// <summary>
        /// Parses <c>.data I_0000 = (01 02 ff ...)</c> into raw bytes. The blob is attached to
        /// the field that refers to it with <c>at I_0000</c>.
        /// </summary>
        private static void ParseData(string line, Dictionary<string, byte[]> blobs)
        {
            int equals = line.IndexOf('=');
            if (equals < 0)
            {
                return;
            }

            string header = line.Substring(".data".Length, equals - ".data".Length).Trim();
            string label = LastToken(header);
            if (label.Length == 0)
            {
                return;
            }

            int open = line.IndexOf('(', equals);
            int close = line.LastIndexOf(')');
            if (open < 0 || close <= open)
            {
                return;
            }

            string content = line.Substring(open + 1, close - open - 1);
            List<byte> bytes = new List<byte>();
            foreach (string token in content.Split(new[] { ' ', '\t', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                byte value;
                if (byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                {
                    bytes.Add(value);
                }
            }

            blobs[label] = bytes.ToArray();
        }

        /// <summary>Parses an <c>.assembly extern</c> line, keeping version and public key token.</summary>
        private static void ParseAssemblyExtern(AssemblyDefinition assembly, string line)
        {
            string name;
            string before;
            string after;
            if (!SplitQuoted(line, out before, out name, out after) || name.Length == 0)
            {
                return;
            }

            Version version = new Version(0, 0, 0, 0);
            int ver = after.IndexOf(".ver ", StringComparison.OrdinalIgnoreCase);
            if (ver >= 0)
            {
                int end = after.IndexOf(' ', ver + 5);
                if (end < 0)
                {
                    end = after.IndexOf('}', ver + 5);
                }
                string text = (end > 0 ? after.Substring(ver + 5, end - ver - 5) : after.Substring(ver + 5)).Trim();
                string[] numbers = text.Split('.', ':');
                if (numbers.Length >= 2)
                {
                    int major, minor, build = 0, revision = 0;
                    int.TryParse(numbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out major);
                    int.TryParse(numbers[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor);
                    if (numbers.Length > 2)
                    {
                        int.TryParse(numbers[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out build);
                    }
                    if (numbers.Length > 3)
                    {
                        int.TryParse(numbers[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out revision);
                    }
                    version = new Version(major, minor, build, revision);
                }
            }

            byte[] token = null;
            int key = after.IndexOf(".publickeytoken", StringComparison.OrdinalIgnoreCase);
            if (key >= 0)
            {
                int open = after.IndexOf('(', key);
                int close = open >= 0 ? after.IndexOf(')', open) : -1;
                if (open >= 0 && close > open)
                {
                    List<byte> bytes = new List<byte>();
                    foreach (string part in after.Substring(open + 1, close - open - 1).Split(
                                 new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        byte value;
                        if (byte.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                        {
                            bytes.Add(value);
                        }
                    }
                    token = bytes.ToArray();
                }
            }

            foreach (AssemblyNameReference existing in assembly.MainModule.AssemblyReferences)
            {
                if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            AssemblyNameReference reference = new AssemblyNameReference(name, version);
            if (token != null)
            {
                reference.PublicKeyToken = token;
            }
            assembly.MainModule.AssemblyReferences.Add(reference);
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
                    ResolveMemberOperands(type, module);
                    SanitizeType(type);
                }
            }
        }

        /// <summary>
        /// Re-points member references at their real definitions. Methods are emitted before
        /// the types they use, so a call to a type that appears later in the IL text starts
        /// out as a loose member reference; without this pass the reference would carry the
        /// wrong signature (for example an object-typed field) and fail to load.
        /// </summary>
        private static void ResolveMemberOperands(TypeDefinition type, ModuleDefinition module)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (Instruction instruction in method.Body.Instructions)
                {
                    FieldReference field = instruction.Operand as FieldReference;
                    if (field != null && !(field is FieldDefinition))
                    {
                        FieldDefinition definition = FindFieldDefinition(module, field);
                        if (definition != null)
                        {
                            instruction.Operand = definition;
                        }
                        continue;
                    }

                    MethodReference target = instruction.Operand as MethodReference;
                    if (target != null && !(target is MethodDefinition))
                    {
                        MethodDefinition definition = FindMethodDefinition(module, target);
                        if (definition != null)
                        {
                            instruction.Operand = definition;
                        }
                    }
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                ResolveMemberOperands(nested, module);
            }
        }

        private static FieldDefinition FindFieldDefinition(ModuleDefinition module, FieldReference reference)
        {
            TypeDefinition declaring = reference.DeclaringType == null
                ? null
                : FindTypeDefinition(module, reference.DeclaringType.FullName);
            if (declaring == null)
            {
                return null;
            }

            foreach (FieldDefinition field in declaring.Fields)
            {
                if (string.Equals(field.Name, reference.Name, StringComparison.Ordinal))
                {
                    return field;
                }
            }
            return null;
        }

        private static MethodDefinition FindMethodDefinition(ModuleDefinition module, MethodReference reference)
        {
            TypeDefinition declaring = reference.DeclaringType == null
                ? null
                : FindTypeDefinition(module, reference.DeclaringType.FullName);
            if (declaring == null)
            {
                return null;
            }

            List<TypeReference> parameters = new List<TypeReference>();
            foreach (ParameterDefinition parameter in reference.Parameters)
            {
                parameters.Add(parameter.ParameterType);
            }
            return FindMethodByShape(declaring, reference.Name, parameters);
        }

        /// <summary>
        /// Promotes <c>*.s</c> branches to their long form only when the real distance to the
        /// target does not fit into a signed byte. Offsets are recomputed after every pass,
        /// because promoting one branch shifts everything after it.
        /// </summary>
        private static void ExpandOutOfRangeBranches(MethodBody body)
        {
            for (int pass = 0; pass < 8; pass++)
            {
                Dictionary<Instruction, int> offsets = ComputeOffsets(body);
                bool changed = false;

                foreach (Instruction instruction in body.Instructions)
                {
                    if (instruction.OpCode.OperandType != OperandType.ShortInlineBrTarget)
                    {
                        continue;
                    }

                    Instruction target = instruction.Operand as Instruction;
                    if (target == null || !offsets.ContainsKey(target) || !offsets.ContainsKey(instruction))
                    {
                        continue;
                    }

                    int from = offsets[instruction] + instruction.OpCode.Size + 1;
                    int delta = offsets[target] - from;
                    if (delta >= sbyte.MinValue && delta <= sbyte.MaxValue)
                    {
                        continue;
                    }

                    string name = instruction.OpCode.Name;
                    if (!name.EndsWith(".s", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    OpCode longForm;
                    if (TryGetOpCode(name.Substring(0, name.Length - 2), out longForm))
                    {
                        instruction.OpCode = longForm;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return;
                }
            }
        }

        private static Dictionary<Instruction, int> ComputeOffsets(MethodBody body)
        {
            Dictionary<Instruction, int> offsets = new Dictionary<Instruction, int>();
            int offset = 0;
            foreach (Instruction instruction in body.Instructions)
            {
                offsets[instruction] = offset;
                offset += InstructionSize(instruction);
            }
            return offsets;
        }

        private static int InstructionSize(Instruction instruction)
        {
            int size = instruction.OpCode.Size;
            switch (instruction.OpCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineArg:
                    size += 1;
                    break;
                case OperandType.InlineVar:
                case OperandType.InlineArg:
                    size += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    size += 8;
                    break;
                case OperandType.InlineSwitch:
                    Instruction[] targets = instruction.Operand as Instruction[];
                    size += 4 + (targets == null ? 0 : targets.Length * 4);
                    break;
                default:
                    size += 4;
                    break;
            }
            return size;
        }

        private static bool TryGetOpCode(string name, out OpCode opcode)
        {
            foreach (FieldInfo field in typeof(OpCodes).GetFields())
            {
                if (field.FieldType != typeof(OpCode))
                {
                    continue;
                }

                OpCode candidate = (OpCode)field.GetValue(null);
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    opcode = candidate;
                    return true;
                }
            }
            opcode = OpCodes.Nop;
            return false;
        }

        private static void SanitizeType(TypeDefinition type)        {
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
                        case OperandType.InlineSwitch:
                            valid = instruction.Operand is Instruction[];
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
                        case OperandType.ShortInlineArg:
                        case OperandType.InlineArg:
                            valid = instruction.Operand is ParameterDefinition;
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

                // Short branches must fit into a signed byte; if the layout does not allow
                // it, promote them to their long form instead of writing a corrupted offset.
                ExpandOutOfRangeBranches(method.Body);

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

            string interfaces = null;
            int implementsIndex = extends == null
                ? rest.IndexOf(" implements ", StringComparison.OrdinalIgnoreCase)
                : extends.IndexOf(" implements ", StringComparison.OrdinalIgnoreCase);
            if (implementsIndex >= 0)
            {
                string source = extends == null ? rest : extends;
                interfaces = source.Substring(implementsIndex + 12).Trim();
                if (extends == null)
                {
                    rest = rest.Substring(0, implementsIndex).Trim();
                }
                else
                {
                    extends = extends.Substring(0, implementsIndex).Trim();
                }
            }

            // Keyword checks must only look at the header, never at the quoted type name
            // (a class called 'IInterface' must not become an interface).
            string header = rest;
            int quote = header.IndexOf('\'');
            if (quote >= 0)
            {
                header = header.Substring(0, quote);
            }

            bool isInterface = header.IndexOf("interface", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isAbstract = header.IndexOf("abstract", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSealed = header.IndexOf("sealed", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPublic = header.IndexOf("public", StringComparison.OrdinalIgnoreCase) >= 0;

            string name = ExtractQuoted(rest);
            if (string.IsNullOrEmpty(name))
            {
                int lastSpace = rest.LastIndexOf(' ');
                name = lastSpace >= 0 ? rest.Substring(lastSpace + 1) : rest;
            }

            bool isNested = header.IndexOf("nested", StringComparison.OrdinalIgnoreCase) >= 0;

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

            if (HasKeyword(header, "sequential")) attributes |= TypeAttributes.SequentialLayout;
            if (HasKeyword(header, "explicit")) attributes |= TypeAttributes.ExplicitLayout;
            if (HasKeyword(header, "unicode")) attributes |= TypeAttributes.UnicodeClass;
            if (HasKeyword(header, "autoclass")) attributes |= TypeAttributes.AutoClass;
            if (HasKeyword(header, "beforefieldinit")) attributes |= TypeAttributes.BeforeFieldInit;
            if (HasKeyword(header, "serializable")) attributes |= TypeAttributes.Serializable;
            if (HasKeyword(header, "import")) attributes |= TypeAttributes.Import;
            if (HasKeyword(header, "hassecurity")) attributes |= TypeAttributes.HasSecurity;
            if (HasKeyword(header, "specialname")) attributes |= TypeAttributes.SpecialName;
            if (HasKeyword(header, "rtspecialname")) attributes |= TypeAttributes.RTSpecialName;

            if (isNested)
            {
                attributes |= NestedVisibility(header);
            }
            else
            {
                attributes |= isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic;
            }

            // Nested types are written as "Outer/Inner/Innermost": create or reuse the
            // chain so the metadata keeps the real nesting relationship.
            int slash = name.IndexOf('/');
            if (slash > 0)
            {
                TypeDefinition parent = FindType(assembly, name.Substring(0, slash));
                if (parent != null)
                {
                    string[] parts = name.Substring(slash + 1).Split('/');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        bool isInnermost = i == parts.Length - 1;
                        if (parts[i].Length == 0)
                        {
                            continue;
                        }
                        TypeDefinition nested = FindNested(parent, parts[i]);
                        if (nested == null)
                        {
                            nested = new TypeDefinition(string.Empty, parts[i],
                                isInnermost ? attributes : TypeAttributes.NestedPublic);
                            parent.NestedTypes.Add(nested);
                        }
                        else if (isInnermost)
                        {
                            // A stub created by a forward reference: fill in the real details.
                            nested.Attributes = attributes;
                        }

                        if (isInnermost)
                        {
                            {
                                TypeReference baseType = string.IsNullOrEmpty(extends)
                                    ? (isInterface ? null : assembly.MainModule.TypeSystem.Object)
                                    : ResolveType(assembly.MainModule, extends);
                                if (baseType != null)
                                {
                                    nested.BaseType = baseType;
                                }
                            }
                            AttachInterfaces(nested, assembly.MainModule, interfaces);
                            ApplyArrayInitSize(nested);
                            ApplyGenericParameters(nested, parts[i]);
                        }
                        parent = nested;
                    }
                    return parent;
                }
            }

            string namespaceName = string.Empty;
            int dot = name.LastIndexOf('.');
            if (dot > 0)
            {
                namespaceName = name.Substring(0, dot);
                name = name.Substring(dot + 1);
            }

            TypeDefinition type = new TypeDefinition(namespaceName, name, attributes);
            if (!string.IsNullOrEmpty(extends))
            {
                type.BaseType = ResolveType(assembly.MainModule, extends);
            }
            else if (!isInterface)
            {
                type.BaseType = assembly.MainModule.TypeSystem.Object;
            }
            AttachInterfaces(type, assembly.MainModule, interfaces);
            assembly.MainModule.Types.Add(type);
            ApplyArrayInitSize(type);
            ApplyGenericParameters(type, name);
            return type;
        }

        /// <summary>Recreates the generic parameters implied by the arity in the type name.</summary>
        private static void ApplyGenericParameters(TypeDefinition type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name) || type.HasGenericParameters)
            {
                return;
            }

            int tick = name.LastIndexOf('`');
            if (tick < 0 || tick == name.Length - 1)
            {
                return;
            }

            int arity;
            if (int.TryParse(name.Substring(tick + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out arity) &&
                arity > 0)
            {
                for (int i = 0; i < arity; i++)
                {
                    type.GenericParameters.Add(new GenericParameter(type));
                }
            }
        }

        /// <summary>
        /// Roslyn represents statically initialised arrays as structs named
        /// <c>__StaticArrayInitTypeSize=N</c> whose ClassLayout records the byte size.
        /// The size is not part of the IL text, so it is restored from the name.
        /// </summary>
        private static void ApplyArrayInitSize(TypeDefinition type)
        {
            const string prefix = "__StaticArrayInitTypeSize=";
            if (type == null || type.Name == null || !type.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            int size;
            if (int.TryParse(type.Name.Substring(prefix.Length), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out size) && size > 0)
            {
                type.PackingSize = 1;
                type.ClassSize = size;
            }
        }

        /// <summary>Adds the <c>implements</c> list; generic arguments are preserved in order.</summary>
        private static void AttachInterfaces(TypeDefinition type, ModuleDefinition module, string interfaces)
        {
            if (string.IsNullOrEmpty(interfaces))
            {
                return;
            }

            foreach (string part in SplitTopLevel(interfaces))
            {
                string name = part.Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                // Interface references keep their shape, including generic arguments, so
                // an assembly that implements the same interface for several type arguments
                // keeps all of its entries.
                type.Interfaces.Add(ResolveType(module, name));
            }
        }

        /// <summary>Field visibility keyword to attribute, mirroring the IL text.</summary>
        private static FieldAttributes FieldVisibility(string modifiers)
        {
            if (HasKeyword(modifiers, "famorassem")) return FieldAttributes.FamORAssem;
            if (HasKeyword(modifiers, "famandassem")) return FieldAttributes.FamANDAssem;
            if (HasKeyword(modifiers, "family")) return FieldAttributes.Family;
            if (HasKeyword(modifiers, "assembly")) return FieldAttributes.Assembly;
            if (HasKeyword(modifiers, "public")) return FieldAttributes.Public;
            return FieldAttributes.Private;
        }

        private static TypeAttributes NestedVisibility(string rest)        {
            if (HasKeyword(rest, "nested public")) return TypeAttributes.NestedPublic;
            if (HasKeyword(rest, "nested family")) return TypeAttributes.NestedFamily;
            if (HasKeyword(rest, "nested famorassem")) return TypeAttributes.NestedFamORAssem;
            if (HasKeyword(rest, "nested famandassem")) return TypeAttributes.NestedFamANDAssem;
            if (HasKeyword(rest, "nested assembly")) return TypeAttributes.NestedAssembly;
            return TypeAttributes.NestedPrivate;
        }

        private static TypeDefinition FindType(AssemblyDefinition assembly, string fullName)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    TypeDefinition found = FindType(type, fullName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private static TypeDefinition FindType(TypeDefinition type, string fullName)
        {
            if (string.Equals(type.FullName, fullName, StringComparison.Ordinal))
            {
                return type;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                TypeDefinition found = FindType(nested, fullName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static TypeDefinition FindNested(TypeDefinition parent, string name)
        {
            foreach (TypeDefinition nested in parent.NestedTypes)
            {
                if (string.Equals(nested.Name, name, StringComparison.Ordinal))
                {
                    return nested;
                }
            }
            return null;
        }

        private static FieldDefinition CreateField(TypeDefinition type, string line, out string dataLabel)
        {
            dataLabel = null;
            string rest = line.Substring(".field".Length).Trim();

            // A constant assignment is written as " = value". A bare '=' also appears in
            // type names such as "__StaticArrayInitTypeSize=16", so require the spaces.
            int equals = rest.IndexOf(" = ", StringComparison.Ordinal);
            string declaration = equals >= 0 ? rest.Substring(0, equals).Trim() : rest;

            // Field data may be declared inline: ".field ... <name> at I_0000".
            int at = declaration.LastIndexOf(" at ", StringComparison.Ordinal);
            if (at > 0)
            {
                dataLabel = declaration.Substring(at + 4).Trim();
                declaration = declaration.Substring(0, at).Trim();
            }

            // The declaration is "<modifiers...> <type> <name>": take the last two tokens
            // so that any combination of attribute keywords is ignored.
            int lastSpace = declaration.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                return null;
            }

            string fieldName = declaration.Substring(lastSpace + 1).Trim();
            string beforeName = declaration.Substring(0, lastSpace).Trim();
            int typeSpace = LastTopLevelSpace(beforeName);

            string typeName = typeSpace > 0 ? beforeName.Substring(typeSpace + 1).Trim() : beforeName;

            // Only the modifier part may be searched for keywords: the field name itself
            // can be "Assembly" or "Static".
            string modifiers = typeSpace > 0 ? beforeName.Substring(0, typeSpace).Trim() : string.Empty;

            FieldAttributes attributes = FieldVisibility(modifiers);
            if (HasKeyword(modifiers, "static")) attributes |= FieldAttributes.Static;
            if (HasKeyword(modifiers, "initonly")) attributes |= FieldAttributes.InitOnly;
            if (HasKeyword(modifiers, "literal")) attributes |= FieldAttributes.Literal | FieldAttributes.Static;
            if (HasKeyword(modifiers, "specialname")) attributes |= FieldAttributes.SpecialName;
            if (HasKeyword(modifiers, "rtspecialname")) attributes |= FieldAttributes.RTSpecialName;

            FieldDefinition field = new FieldDefinition(fieldName, attributes, ResolveType(type.MainModule(), typeName));
            if (equals >= 0)
            {
                string value = rest.Substring(equals + 3).Trim();
                object constant = ParseConstant(value);
                if (constant != null && IsConstantCompatible(field.FieldType))
                {
                    field.Constant = CoerceConstant(constant, field.FieldType);
                    field.Attributes |= FieldAttributes.HasDefault;
                }
            }
            type.Fields.Add(field);
            return field;
        }

        /// <summary>
        /// Parses a <c>.property</c> header. Accessors are attached later, when the
        /// <c>.get</c>/<c>.set</c> directives inside the block are read.
        /// </summary>
        private static PropertyDefinition CreateProperty(TypeDefinition type, string line)
        {
            string rest = line.Substring(".property".Length).Trim();
            string before;
            string name;
            string after;
            if (!SplitQuoted(rest, out before, out name, out after))
            {
                return null;
            }

            PropertyAttributes attributes = PropertyAttributes.None;
            if (HasKeyword(before, "specialname"))
            {
                attributes |= PropertyAttributes.SpecialName;
            }
            if (HasKeyword(before, "rtspecialname"))
            {
                attributes |= PropertyAttributes.RTSpecialName;
            }
            if (HasKeyword(before, "hasdefault"))
            {
                attributes |= PropertyAttributes.HasDefault;
            }

            PropertyDefinition property = new PropertyDefinition(
                name, attributes, ResolveType(type.MainModule(), LastToken(before)));

            // Indexers carry their parameter types in parentheses after the name.
            int paren = after.IndexOf('(');
            int close = paren >= 0 ? after.IndexOf(')', paren) : -1;
            if (paren >= 0 && close > paren)
            {
                foreach (string part in SplitTopLevel(after.Substring(paren + 1, close - paren - 1)))
                {
                    string parameterType = LastToken(part.Trim());
                    if (parameterType.Length > 0)
                    {
                        property.Parameters.Add(new ParameterDefinition(
                            ResolveType(type.MainModule(), parameterType)));
                    }
                }
            }

            type.Properties.Add(property);
            return property;
        }

        /// <summary>Parses an <c>.event</c> header; accessors are attached from the block.</summary>
        private static EventDefinition CreateEvent(TypeDefinition type, string line)
        {
            string rest = line.Substring(".event".Length).Trim();
            string before;
            string name;
            string after;
            if (!SplitQuoted(rest, out before, out name, out after))
            {
                return null;
            }

            EventAttributes attributes = EventAttributes.None;
            if (HasKeyword(before, "specialname"))
            {
                attributes |= EventAttributes.SpecialName;
            }
            if (HasKeyword(before, "rtspecialname"))
            {
                attributes |= EventAttributes.RTSpecialName;
            }

            EventDefinition @event = new EventDefinition(
                name, attributes, ResolveType(type.MainModule(), LastToken(before)));
            type.Events.Add(@event);
            return @event;
        }

        /// <summary>Finds a method of the type by name; accessor directives reference names.</summary>
        private static MethodDefinition FindMethod(TypeDefinition type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (string.Equals(method.Name, name, StringComparison.Ordinal))
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>Splits <c>prefix 'name' suffix</c> around the quoted name.</summary>
        private static bool SplitQuoted(string text, out string before, out string name, out string after)
        {
            before = null;
            name = null;
            after = string.Empty;

            int first = text.IndexOf('\'');
            if (first < 0)
            {
                return false;
            }

            int second = text.IndexOf('\'', first + 1);
            if (second < 0)
            {
                return false;
            }

            before = text.Substring(0, first).Trim();
            name = text.Substring(first + 1, second - first - 1).Replace("\\'", "'");
            after = text.Substring(second + 1).Trim();
            return true;
        }

        /// <summary>
        /// Matches a whole word (or phrase) so that a field named "Assembly" is not mistaken
        /// for the "assembly" visibility keyword.
        /// </summary>
        private static bool HasKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            {
                return false;
            }

            int index = 0;
            while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                int end = index + keyword.Length;
                bool left = index == 0 || !IsIdentifierChar(text[index - 1]);
                bool right = end >= text.Length || !IsIdentifierChar(text[end]);
                if (left && right)
                {
                    return true;
                }
                index = end;
            }
            return false;
        }

        private static bool IsIdentifierChar(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        /// <summary>
        /// Returns the index of the last space outside angle brackets. Type names can contain
        /// spaces inside generic arguments ("Dictionary`2&lt;String, Int32&gt;"), so a plain
        /// LastIndexOf(' ') would split inside the type.
        /// </summary>
        private static int LastTopLevelSpace(string text)
        {
            int depth = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == '>')
                {
                    depth++;
                }
                else if (c == '<')
                {
                    if (depth > 0)
                    {
                        depth--;
                    }
                }
                else if (c == ' ' && depth == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Returns the last whitespace-separated token, i.e. the type in a declaration.</summary>
        private static string LastToken(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int space = text.LastIndexOf(' ');
            return space >= 0 ? text.Substring(space + 1).Trim() : text.Trim();
        }

        /// <summary>Converts a parsed constant to the exact CLR type the field expects.</summary>
        private static object CoerceConstant(object constant, TypeReference type)
        {
            if (constant == null || type == null)
            {
                return constant;
            }

            MetadataType metadataType = type.MetadataType;
            try
            {
                TypeDefinition definition = type.Resolve();
                if (definition != null && definition.IsEnum)
                {
                    FieldDefinition underlying = null;
                    foreach (FieldDefinition field in definition.Fields)
                    {
                        if (field.Name == "value__")
                        {
                            underlying = field;
                            break;
                        }
                    }
                    if (underlying != null)
                    {
                        return CoerceConstant(constant, underlying.FieldType);
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to the metadata type based conversion.
            }

            try
            {
                switch (metadataType)
                {
                    case MetadataType.Single:
                        return Convert.ToSingle(constant, CultureInfo.InvariantCulture);
                    case MetadataType.Double:
                        return Convert.ToDouble(constant, CultureInfo.InvariantCulture);
                    case MetadataType.Int64:
                        return Convert.ToInt64(constant, CultureInfo.InvariantCulture);
                    case MetadataType.UInt64:
                        return Convert.ToUInt64(constant, CultureInfo.InvariantCulture);
                    case MetadataType.Int16:
                        return Convert.ToInt16(constant, CultureInfo.InvariantCulture);
                    case MetadataType.UInt16:
                        return Convert.ToUInt16(constant, CultureInfo.InvariantCulture);
                    case MetadataType.Byte:
                        return Convert.ToByte(constant, CultureInfo.InvariantCulture);
                    case MetadataType.SByte:
                        return Convert.ToSByte(constant, CultureInfo.InvariantCulture);
                    case MetadataType.Char:
                        return Convert.ToChar(Convert.ToInt32(constant, CultureInfo.InvariantCulture));
                    default:
                        return constant;
                }
            }
            catch (Exception)
            {
                return constant;
            }
        }

        private static bool IsConstantCompatible(TypeReference type)
        {
            if (type == null)
            {
                return false;
            }

            try
            {
                // Enum constants are stored as their underlying integer type.
                TypeDefinition definition = type.Resolve();
                if (definition != null && definition.IsEnum)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // An unresolvable reference simply means "not a known enum".
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

            // The pinvokeimpl clause has its own parentheses: skip past it before looking
            // for the parameter list. Only the header is searched, because a method may
            // legitimately be named "get_IsPInvokeImpl".
            string header = rest;
            int nameQuote = rest.IndexOf('\'');
            if (nameQuote > 0)
            {
                header = rest.Substring(0, nameQuote);
            }

            int paren = rest.IndexOf('(');
            int pinvoke = header.IndexOf("pinvokeimpl", StringComparison.OrdinalIgnoreCase);
            if (pinvoke >= 0 && paren > pinvoke)
            {
                int pinvokeClose = rest.IndexOf(')', pinvoke);
                if (pinvokeClose > paren)
                {
                    paren = rest.IndexOf('(', pinvokeClose);
                }
            }

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
            string tail = rest.Substring(closeParen + 1).Trim();
            string methodName = beforeParen.Substring(lastSpace + 1).Trim();
            if (methodName.StartsWith("'") && methodName.EndsWith("'"))
            {
                methodName = methodName.Substring(1, methodName.Length - 2);
            }

            int returnSpace = LastTopLevelSpace(signature);
            string returnTypeName = returnSpace >= 0 ? signature.Substring(returnSpace + 1) : signature;

            bool isStatic = HasKeyword(signature, "static");
            bool isPublic = HasKeyword(signature, "public");
            bool isVirtual = HasKeyword(signature, "virtual");
            bool isAbstract = HasKeyword(signature, "abstract");
            bool isSpecialName = HasKeyword(signature, "specialname");
            bool isRuntimeSpecialName = HasKeyword(signature, "rtspecialname");
            bool isHideBySig = HasKeyword(signature, "hidebysig");
            bool isNewSlot = HasKeyword(signature, "newslot");
            bool isFinal = HasKeyword(signature, "final");

            MethodAttributes attributes = MethodVisibility(signature);
            if (isStatic) attributes |= MethodAttributes.Static;
            if (isVirtual) attributes |= MethodAttributes.Virtual;
            if (isAbstract) attributes |= MethodAttributes.Abstract;
            if (isSpecialName) attributes |= MethodAttributes.SpecialName;
            if (isRuntimeSpecialName) attributes |= MethodAttributes.RTSpecialName;
            if (isHideBySig) attributes |= MethodAttributes.HideBySig;
            if (isNewSlot) attributes |= MethodAttributes.NewSlot;
            if (isFinal) attributes |= MethodAttributes.Final;
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
                type.MainModule().TypeSystem.Void);
            method.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;

            // Generic method parameters must exist before the parameter types are resolved,
            // because a signature can reference them (!!0).
            if (parameters.Length > 0)
            {
                int maxGeneric = -1;
                foreach (string part in SplitTopLevel(parameters))
                {
                    int space = LastTopLevelSpace(part);
                    string typeName = space > 0 ? part.Substring(0, space).Trim() : part.Trim();
                    if (typeName.StartsWith("!!", StringComparison.Ordinal))
                    {
                        int position;
                        if (int.TryParse(typeName.Substring(2), NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out position) && position > maxGeneric)
                        {
                            maxGeneric = position;
                        }
                    }
                }

                for (int i = 0; i <= maxGeneric; i++)
                {
                    method.GenericParameters.Add(new GenericParameter(method));
                }
            }

            method.ReturnType = ResolveTypeWithOwner(type.MainModule(), returnTypeName, type, method);

            if (parameters.Length > 0)
            {
                foreach (string part in SplitTopLevel(parameters))
                {
                    string parameter = part.Trim();
                    if (parameter.Length == 0)
                    {
                        continue;
                    }

                    int space = LastTopLevelSpace(parameter);
                    string parameterType = space > 0 ? parameter.Substring(0, space).Trim() : parameter;
                    string parameterName = space > 0 ? parameter.Substring(space + 1).Trim() : "arg" + method.Parameters.Count;
                    method.Parameters.Add(new ParameterDefinition(parameterName, ParameterAttributes.None,
                        ResolveTypeWithOwner(type.MainModule(), parameterType, type, method)));
                }
            }

            bool isPInvoke = HasKeyword(signature, "pinvokeimpl");
            bool isRuntime = HasKeyword(tail, "runtime") || HasKeyword(signature, "internalcall");
            if (isPInvoke)
            {
                method.ImplAttributes = MethodImplAttributes.PreserveSig;
                AttachPInvokeInfo(method, type.MainModule(), signature);
            }
            else if (isRuntime || HasKeyword(signature, "internalcall"))
            {
                method.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }

            if (!isAbstract && !isPInvoke && !isRuntime)
            {
                method.Body = new MethodBody(method);
            }
            type.Methods.Add(method);
            return method;
        }

        /// <summary>
        /// Reads <c>pinvokeimpl("kernel32.dll" winapi)</c> and attaches the native module and
        /// calling convention, so P/Invoke declarations survive the round trip.
        /// </summary>
        private static void AttachPInvokeInfo(MethodDefinition method, ModuleDefinition module, string signature)
        {
            int start = signature.IndexOf("pinvokeimpl", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return;
            }

            int quote = signature.IndexOf('"', start);
            int close = quote >= 0 ? signature.IndexOf('"', quote + 1) : -1;
            if (quote < 0 || close <= quote)
            {
                return;
            }

            string dll = signature.Substring(quote + 1, close - quote - 1);
            string options = close + 1 < signature.Length ? signature.Substring(close + 1) : string.Empty;

            PInvokeAttributes attributes = PInvokeAttributes.CallConvWinapi;
            if (HasKeyword(options, "cdecl")) attributes = PInvokeAttributes.CallConvCdecl;
            else if (HasKeyword(options, "stdcall")) attributes = PInvokeAttributes.CallConvStdCall;
            else if (HasKeyword(options, "thiscall")) attributes = PInvokeAttributes.CallConvThiscall;
            else if (HasKeyword(options, "fastcall")) attributes = PInvokeAttributes.CallConvFastcall;
            if (HasKeyword(options, "noilgen")) attributes |= PInvokeAttributes.NoMangle;

            ModuleReference reference = null;
            foreach (ModuleReference existing in module.ModuleReferences)
            {
                if (string.Equals(existing.Name, dll, StringComparison.OrdinalIgnoreCase))
                {
                    reference = existing;
                    break;
                }
            }
            if (reference == null)
            {
                reference = new ModuleReference(dll);
                module.ModuleReferences.Add(reference);
            }

            method.PInvokeInfo = new PInvokeInfo(attributes, method.Name, reference);
        }

        /// <summary>Method visibility keyword to attribute.</summary>
        private static MethodAttributes MethodVisibility(string modifiers)
        {
            if (HasKeyword(modifiers, "famorassem")) return MethodAttributes.FamORAssem;
            if (HasKeyword(modifiers, "famandassem")) return MethodAttributes.FamANDAssem;
            if (HasKeyword(modifiers, "family")) return MethodAttributes.Family;
            if (HasKeyword(modifiers, "assembly")) return MethodAttributes.Assembly;
            if (HasKeyword(modifiers, "public")) return MethodAttributes.Public;
            return MethodAttributes.Private;
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

            if (opcode.OperandType == OperandType.ShortInlineBrTarget ||
                opcode.OperandType == OperandType.InlineBrTarget ||
                opcode.OperandType == OperandType.InlineSwitch)
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
                case OperandType.InlineSwitch:
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
                case OperandType.ShortInlineArg:
                case OperandType.InlineArg:
                    return operand is ParameterDefinition;
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
                    return ResolveMethodOperand(moduleOfText, text, opcode);
                case OperandType.InlineField:
                    return ResolveFieldOperand(moduleOfText, text);
                case OperandType.InlineType:
                    return ResolveTypeOperand(moduleOfText, text);
                case OperandType.InlineTok:
                {
                    // ldtoken can name a type, a field or a method.
                    int separator = text.IndexOf("::", StringComparison.Ordinal);
                    if (separator > 0)
                    {
                        int paren = text.IndexOf('(', separator);
                        return paren > separator
                            ? ResolveMethodOperand(moduleOfText, text, opcode)
                            : ResolveFieldOperand(moduleOfText, text);
                    }
                    return ResolveTypeOperand(moduleOfText, text);
                }
                case OperandType.InlineSig:
                    return Unresolved;
                case OperandType.InlineSwitch:
                    // Resolved in ResolveBranches once every label is known.
                    return text;

                case OperandType.ShortInlineArg:
                case OperandType.InlineArg:
                    return ResolveParameterOperand(context.Method, text);

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
                    return null;            }
        }

        /// <summary>Resolves an argument operand by name, or by its IL argument index.</summary>
        private static object ResolveParameterOperand(MethodDefinition method, string text)
        {
            if (method == null || method.Body == null)
            {
                return Unresolved;
            }

            string name = text.Trim();
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
                {
                    return parameter;
                }
            }

            int index;
            if (!int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
            {
                return Unresolved;
            }

            // IL argument indices count "this" first for instance methods.
            int parameterIndex = method.HasThis ? index - 1 : index;
            if (parameterIndex >= 0 && parameterIndex < method.Parameters.Count)
            {
                return method.Parameters[parameterIndex];
            }
            return Unresolved;
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

                if (pending.Key.OpCode.Code == Code.Switch)
                {
                    Instruction[] targets = ResolveSwitchTargets(context, text);
                    if (targets == null)
                    {
                        LastWarnings.Add("Unresolved switch targets; instruction downgraded to nop.");
                        pending.Key.OpCode = OpCodes.Nop;
                        pending.Key.Operand = null;
                    }
                    else
                    {
                        pending.Key.Operand = targets;
                    }
                    continue;
                }

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

        /// <summary>Resolves every label in a <c>switch (IL_x, IL_y, ...)</c> operand.</summary>
        private static Instruction[] ResolveSwitchTargets(MethodContext context, string text)
        {
            List<Instruction> targets = new List<Instruction>();
            int position = 0;
            while (true)
            {
                int index = text.IndexOf("IL_", position, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                int end = index + 3;
                while (end < text.Length && Uri.IsHexDigit(text[end]))
                {
                    end++;
                }

                Instruction target;
                if (!context.Labels.TryGetValue(text.Substring(index, end - index), out target))
                {
                    return null;
                }

                targets.Add(target);
                position = end;
            }

            return targets.Count == 0 ? null : targets.ToArray();
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

            // "[Assembly]Namespace.Type" carries an explicit assembly scope.
            string trimmedName = name.Trim();
            if (trimmedName.StartsWith("[", StringComparison.Ordinal))
            {
                TypeReference scoped = ResolveTypeOperand(module, trimmedName) as TypeReference;
                if (scoped != null)
                {
                    return scoped;
                }
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

            // "&" marks a by-reference type, "*" a pointer; "[,]" and "[]" may nest.
            bool byRef = false;
            int pointers = 0;
            bool stripped = true;
            while (stripped)
            {
                stripped = false;
                if (clean.EndsWith("&", StringComparison.Ordinal))
                {
                    byRef = true;
                    clean = clean.Substring(0, clean.Length - 1).Trim();
                    stripped = true;
                }
                else if (clean.EndsWith("*", StringComparison.Ordinal))
                {
                    pointers++;
                    clean = clean.Substring(0, clean.Length - 1).Trim();
                    stripped = true;
                }
            }

            List<int> ranks = new List<int>();
            while (clean.EndsWith("]", StringComparison.Ordinal))
            {
                int open = clean.LastIndexOf('[');
                if (open < 0)
                {
                    break;
                }

                string inner = clean.Substring(open + 1, clean.Length - open - 2).Trim();
                if (inner.Length != 0 && inner.Replace(",", string.Empty).Trim().Length != 0)
                {
                    break; // generic argument list, not an array suffix
                }

                ranks.Add(inner.Length == 0 ? 1 : inner.Split(',').Length);
                clean = clean.Substring(0, open).Trim();
            }

            // Generic arguments are not reconstructed: keep the arity name ("List`1").
            int tick = clean.IndexOf('`');
            if (tick > 0)
            {
                int angle = clean.IndexOf('<', tick);
                if (angle > tick)
                {
                    int close = MatchAngle(clean, angle);
                    if (close > angle)
                    {
                        TypeReference genericBase = ResolveType(module, clean.Substring(0, angle).Trim());
                        GenericInstanceType instance = new GenericInstanceType(genericBase);
                        foreach (string argument in SplitTopLevel(clean.Substring(angle + 1, close - angle - 1)))
                        {
                            string text = argument.Trim();
                            if (text.Length > 0)
                            {
                                instance.GenericArguments.Add(ResolveTypeWithOwner(module, text, genericBase));
                            }
                        }
                        return WrapType(instance, ranks, byRef, pointers);
                    }

                    clean = clean.Substring(0, angle).Trim();
                }
            }

            TypeReference primitive = ResolvePrimitiveType(module, clean);
            if (primitive != null)
            {
                return WrapType(primitive, ranks, byRef, pointers);
            }

            // A type that already exists in the module (including nested types such as
            // "<>c") must be referenced by definition, not by a fresh name-based reference.
            TypeDefinition known = FindTypeDefinition(module, clean);
            if (known != null)
            {
                return WrapType(known, ranks, byRef, pointers);
            }

            // A nested type that is referenced before its .class block is emitted: create
            // the chain now so the reference points at the real definition.
            if (clean.IndexOf('/') > 0)
            {
                TypeDefinition created = EnsureNestedType(module, clean);
                if (created != null)
                {
                    return WrapType(created, ranks, byRef, pointers);
                }
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
            return WrapType(MarkValueType(reference), ranks, byRef, pointers);
        }

        /// <summary>
        /// Finds or creates the chain for a nested type name ("Outer/Inner/Innermost") so
        /// that references emitted before the type's own .class block resolve correctly.
        /// </summary>
        private static TypeDefinition EnsureNestedType(ModuleDefinition module, string fullName)
        {
            int slash = fullName.IndexOf('/');
            if (slash <= 0)
            {
                return null;
            }

            string outerName = fullName.Substring(0, slash);
            TypeDefinition parent = FindTypeDefinition(module, outerName);
            if (parent == null)
            {
                return null;
            }

            string[] parts = fullName.Substring(slash + 1).Split('/');
            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }
                TypeDefinition nested = FindNested(parent, part);
                if (nested == null)
                {
                    nested = new TypeDefinition(string.Empty, part, TypeAttributes.NestedPublic);
                    parent.NestedTypes.Add(nested);
                }
                parent = nested;
            }
            return parent;
        }

        private static TypeDefinition FindTypeDefinition(ModuleDefinition module, string fullName)        {
            foreach (TypeDefinition type in module.Types)
            {
                TypeDefinition found = FindTypeDefinition(type, fullName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static TypeDefinition FindTypeDefinition(TypeDefinition type, string fullName)
        {
            if (string.Equals(type.FullName, fullName, StringComparison.Ordinal) ||
                string.Equals(type.FullName.Replace('/', '.'), fullName.Replace('/', '.'), StringComparison.Ordinal))
            {
                return type;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                TypeDefinition found = FindTypeDefinition(nested, fullName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static TypeReference WrapType(TypeReference element, List<int> ranks, bool byRef)
        {
            return WrapType(element, ranks, byRef, 0);
        }

        /// <summary>Returns the index of the '>' that closes the '&lt;' at <paramref name="open"/>.</summary>
        private static int MatchAngle(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '<')
                {
                    depth++;
                }
                else if (text[i] == '>')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>Resolver used for signature checks (value types) while assembling.</summary>
        private static readonly CecilAssemblyResolver Resolver = new CecilAssemblyResolver();

        /// <summary>
        /// Copies the value-type flag from the resolved definition. Metadata signatures encode
        /// enums and structs differently, so a reference that claims "class" for an enum
        /// cannot be resolved by the runtime.
        /// </summary>
        private static readonly Dictionary<string, bool> ValueTypeCache =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Resolves a type that may be a generic parameter (!0 / !!0) of the owner type or
        /// owner method, including generic instances that use such parameters.
        /// </summary>
        private static TypeReference ResolveTypeWithOwner(ModuleDefinition module, string text,
            TypeReference typeOwner, MethodReference methodOwner)
        {
            string name = text.Trim();
            if (name.StartsWith("!", StringComparison.Ordinal))
            {
                bool methodParameter = name.StartsWith("!!", StringComparison.Ordinal);
                string number = methodParameter ? name.Substring(2) : name.Substring(1);
                int position;
                if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out position))
                {
                    if (methodParameter && methodOwner != null &&
                        position >= 0 && position < methodOwner.GenericParameters.Count)
                    {
                        return methodOwner.GenericParameters[position];
                    }

                    if (!methodParameter && typeOwner != null)
                    {
                        TypeDefinition definition = Resolver.TryResolveType(typeOwner);
                        if (definition != null && position >= 0 && position < definition.GenericParameters.Count)
                        {
                            return definition.GenericParameters[position];
                        }
                    }
                }

                return ResolveType(module, text);
            }

            int tick = name.IndexOf('`');
            int angle = tick > 0 ? name.IndexOf('<', tick) : -1;
            if (angle > tick)
            {
                int close = MatchAngle(name, angle);
                if (close > angle)
                {
                    TypeReference baseType = ResolveType(module, name.Substring(0, angle).Trim());
                    GenericInstanceType instance = new GenericInstanceType(baseType);
                    foreach (string argument in SplitTopLevel(name.Substring(angle + 1, close - angle - 1)))
                    {
                        string part = argument.Trim();
                        if (part.Length > 0)
                        {
                            instance.GenericArguments.Add(ResolveTypeWithOwner(module, part, baseType, methodOwner));
                        }
                    }
                    return instance;
                }
            }

            return ResolveType(module, text);
        }

        private static TypeReference ResolveTypeWithOwner(ModuleDefinition module, string text, TypeReference owner)
        {
            return ResolveTypeWithOwner(module, text, owner, null);
        }

        private static TypeReference MarkValueType(TypeReference reference)
        {
            if (reference == null || reference.IsValueType)
            {
                return reference;
            }

            bool isValueType;
            if (!ValueTypeCache.TryGetValue(reference.FullName, out isValueType))
            {
                isValueType = Resolver.TryIsValueType(reference);
                ValueTypeCache[reference.FullName] = isValueType;
            }

            if (isValueType)
            {
                reference.IsValueType = true;
            }
            return reference;
        }

        private static TypeReference WrapType(TypeReference element, List<int> ranks, bool byRef, int pointers)
        {
            TypeReference current = element;
            for (int i = ranks.Count - 1; i >= 0; i--)
            {
                current = ranks[i] == 1 ? new ArrayType(current) : new ArrayType(current, ranks[i]);
            }
            for (int i = 0; i < pointers; i++)
            {
                current = new PointerType(current);
            }
            return byRef ? new ByReferenceType(current) : current;
        }

        private static TypeReference ResolvePrimitiveType(ModuleDefinition module, string name)
        {
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
                default: return null;
            }
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

        private static object ResolveMethodOperand(ModuleDefinition module, string text, OpCode opcode)
        {
            try
            {
                int paren = text.IndexOf('(');
                string signature = paren >= 0 ? text.Substring(0, paren).Trim() : text.Trim();
                string parameters = paren >= 0 ? text.Substring(paren + 1, text.LastIndexOf(')') - paren - 1) : string.Empty;

                // "instance " marks a call with a hidden this argument.
                bool hasThis = false;
                if (signature.StartsWith("instance ", StringComparison.OrdinalIgnoreCase))
                {
                    hasThis = true;
                    signature = signature.Substring(9).Trim();
                }

                int separator = signature.IndexOf("::", StringComparison.Ordinal);
                if (separator < 0)
                {
                    return Unresolved;
                }

                string typePart = signature.Substring(0, separator).Trim();
                string methodName = signature.Substring(separator + 2).Trim();

                // A generic method instantiation is written as Name<Arg1,Arg2>.
                string genericArguments = null;
                int genericOpen = methodName.IndexOf('<');
                if (genericOpen > 0)
                {
                    int genericClose = MatchAngle(methodName, genericOpen);
                    if (genericClose > genericOpen)
                    {
                        genericArguments = methodName.Substring(genericOpen + 1, genericClose - genericOpen - 1);
                        methodName = methodName.Substring(0, genericOpen).Trim();
                    }
                }

                int space = LastTopLevelSpace(typePart);
                string returnTypeName = space > 0 ? typePart.Substring(0, space).Trim() : "void";
                string typeName = space > 0 ? typePart.Substring(space + 1).Trim() : typePart;

                TypeReference declaring = ResolveTypeOperand(module, typeName) as TypeReference;
                if (declaring == null)
                {
                    return Unresolved;
                }

                MethodReference method = new MethodReference(methodName, module.TypeSystem.Void, declaring);
                method.HasThis = hasThis;

                // Generic method parameters must exist before the signature is resolved,
                // because the signature can reference them (!!0, !!1).
                List<string> parameterTexts = new List<string>();
                int maxMethodParameter = -1;
                foreach (string part in SplitTopLevel(parameters))
                {
                    string parameter = part.Trim();
                    if (parameter.Length == 0)
                    {
                        continue;
                    }

                    parameterTexts.Add(parameter);
                    if (parameter.StartsWith("!!", StringComparison.Ordinal))
                    {
                        int position;
                        if (int.TryParse(parameter.Substring(2), NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out position) && position > maxMethodParameter)
                        {
                            maxMethodParameter = position;
                        }
                    }
                }

                for (int i = 0; i <= maxMethodParameter; i++)
                {
                    method.GenericParameters.Add(new GenericParameter(method));
                }

                method.ReturnType = ResolveTypeWithOwner(module, returnTypeName, declaring, method);

                List<TypeReference> parameterTypes = new List<TypeReference>();
                foreach (string parameter in parameterTexts)
                {
                    parameterTypes.Add(ResolveTypeWithOwner(module, parameter, declaring, method));
                }

                // Prefer the real definition: it carries the correct static/instance flag and
                // lets the call site reference a MethodDef token instead of a MemberRef.
                MethodReference target = null;
                TypeDefinition definition = declaring as TypeDefinition ?? FindTypeDefinition(module, typeName);
                if (definition != null && maxMethodParameter < 0)
                {
                    target = FindMethodByShape(definition, methodName, parameterTypes);
                }

                // External generic methods: match the member against the real definition so the
                // signature comes from metadata instead of being reconstructed from text.
                if (target == null && genericArguments != null)
                {
                    TypeDefinition external = Resolver.TryResolveType(declaring);
                    if (external != null)
                    {
                        int arity = 0;
                        foreach (string part in SplitTopLevel(genericArguments))
                        {
                            if (part.Trim().Length > 0)
                            {
                                arity++;
                            }
                        }
                        MethodDefinition match = FindMethodByApproximateShape(external, methodName,
                            parameterTypes.Count, arity);
                        if (match != null)
                        {
                            // References into another module must go through ImportReference.
                            target = module.Import(match);
                        }
                    }
                }

                if (target == null)
                {
                    foreach (TypeReference parameterType in parameterTypes)
                    {
                        method.Parameters.Add(new ParameterDefinition(parameterType));
                    }
                    target = method;
                }

                if (genericArguments != null)
                {
                    GenericInstanceMethod spec = new GenericInstanceMethod(target);
                    foreach (string part in SplitTopLevel(genericArguments))
                    {
                        string argument = part.Trim();
                        if (argument.Length > 0)
                        {
                            spec.GenericArguments.Add(ResolveType(module, argument));
                        }
                    }
                    return spec;
                }

                return target;
            }
            catch (Exception)
            {
                return Unresolved;
            }
        }

        /// <summary>
        /// Finds a method by name, parameter count and generic arity; returns null when the
        /// name is ambiguous, because guessing an overload would produce a broken reference.
        /// </summary>
        private static MethodDefinition FindMethodByApproximateShape(TypeDefinition type, string name,
            int parameterCount, int genericArity)
        {
            MethodDefinition single = null;
            int matches = 0;
            foreach (MethodDefinition method in type.Methods)
            {
                if (!string.Equals(method.Name, name, StringComparison.Ordinal) ||
                    method.Parameters.Count != parameterCount ||
                    method.GenericParameters.Count != genericArity)
                {
                    continue;
                }

                single = method;
                matches++;
            }
            return matches == 1 ? single : null;
        }

        /// <summary>Finds a method by name and normalized parameter types (overload-safe).</summary>
        private static MethodDefinition FindMethodByShape(TypeDefinition type, string name,
            List<TypeReference> parameters)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!string.Equals(method.Name, name, StringComparison.Ordinal) ||
                    method.Parameters.Count != parameters.Count)
                {
                    continue;
                }

                bool match = true;
                for (int i = 0; i < parameters.Count; i++)
                {
                    string left = AssemblyDiff.NormalizeTypeName(method.Parameters[i].ParameterType.FullName);
                    string right = AssemblyDiff.NormalizeTypeName(parameters[i].FullName);
                    if (!string.Equals(left, right, StringComparison.Ordinal))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return method;
                }
            }
            return null;
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

                string typeText = text.Substring(0, separator).Trim();

                // The field type may be written in front: "<fieldType> <owner>::<name>".
                int space = LastTopLevelSpace(typeText);
                string fieldTypeName = space > 0 ? typeText.Substring(0, space).Trim() : null;
                string typeName = space > 0 ? typeText.Substring(space + 1).Trim() : typeText;

                TypeReference declaring = ResolveTypeOperand(module, typeName) as TypeReference;
                string fieldName = text.Substring(separator + 2).Trim();
                if (declaring == null || fieldName.Length == 0)
                {
                    return Unresolved;
                }

                // A field defined in this module must be referenced by its definition,
                // otherwise the member reference carries the wrong field type.
                TypeDefinition definition = declaring as TypeDefinition ?? FindTypeDefinition(module, typeName);
                if (definition != null)
                {
                    foreach (FieldDefinition field in definition.Fields)
                    {
                        if (string.Equals(field.Name, fieldName, StringComparison.Ordinal))
                        {
                            return field;
                        }
                    }
                }

                TypeReference fieldType = string.IsNullOrEmpty(fieldTypeName)
                    ? module.TypeSystem.Object
                    : ResolveTypeWithOwner(module, fieldTypeName, declaring, null);
                return new FieldReference(fieldName, fieldType, declaring);
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

                bool byRef = false;
                int pointers = 0;
                bool stripped = true;
                while (stripped)
                {
                    stripped = false;
                    if (text.EndsWith("&", StringComparison.Ordinal))
                    {
                        byRef = true;
                        text = text.Substring(0, text.Length - 1).Trim();
                        stripped = true;
                    }
                    else if (text.EndsWith("*", StringComparison.Ordinal))
                    {
                        pointers++;
                        text = text.Substring(0, text.Length - 1).Trim();
                        stripped = true;
                    }
                }

                List<int> ranks = new List<int>();
                while (text.EndsWith("]", StringComparison.Ordinal))
                {
                    int open = text.LastIndexOf('[');
                    if (open < 0)
                    {
                        break;
                    }

                    string inner = text.Substring(open + 1, text.Length - open - 2).Trim();
                    if (inner.Length != 0 && inner.Replace(",", string.Empty).Trim().Length != 0)
                    {
                        break;
                    }

                    ranks.Add(inner.Length == 0 ? 1 : inner.Split(',').Length);
                    text = text.Substring(0, open).Trim();
                }

                // Generic arguments follow the arity marker ("List`1<...>"); a '<' without
                // one is part of the name ("<>c"). Rebuild the instance so generic
                // parameters keep their arguments.
                int tick = text.IndexOf('`');
                if (tick > 0)
                {
                    int angle = text.IndexOf('<', tick);
                    if (angle > tick)
                    {
                        int close = MatchAngle(text, angle);
                        if (close > angle)
                        {
                            TypeReference genericBase = ResolveTypeOperand(module, text.Substring(0, angle).Trim()) as TypeReference;
                            if (genericBase != null)
                            {
                                GenericInstanceType instance = new GenericInstanceType(genericBase);
                                foreach (string argument in SplitTopLevel(text.Substring(angle + 1, close - angle - 1)))
                                {
                                    string part = argument.Trim();
                                    if (part.Length > 0)
                                    {
                                        instance.GenericArguments.Add(ResolveType(module, part));
                                    }
                                }
                                return WrapType(instance, ranks, byRef, pointers);
                            }
                        }

                        text = text.Substring(0, angle).Trim();
                    }
                }

                TypeReference primitive = ResolvePrimitiveType(module, text);
                if (primitive != null)
                {
                    return WrapType(primitive, ranks, byRef, pointers);
                }

                TypeDefinition known = FindTypeDefinition(module, text);
                if (known != null)
                {
                    return WrapType(known, ranks, byRef, pointers);
                }

                if (text.IndexOf('/') > 0)
                {
                    TypeDefinition created = EnsureNestedType(module, text);
                    if (created != null)
                    {
                        return WrapType(created, ranks, byRef, pointers);
                    }
                }

                int dot = text.LastIndexOf('.');
                string namespaceName = dot > 0 ? text.Substring(0, dot) : string.Empty;
                string typeName = dot > 0 ? text.Substring(dot + 1) : text;
                if (typeName.Length == 0)
                {
                    return Unresolved;
                }

                TypeReference element = new TypeReference(namespaceName, typeName, module, NormalizeScope(module, scope));
                return WrapType(MarkValueType(element), ranks, byRef, pointers);
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

            long longValue;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            {
                return longValue;
            }

            double doubleValue;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
            {
                return doubleValue;
            }
            return null;
        }

        private static string Unquote(string text)
        {
            if (text.Length >= 2 && text[0] == '\'' && text[text.Length - 1] == '\'')
            {
                text = text.Substring(1, text.Length - 2);
            }
            return Unescape(text);
        }

        /// <summary>Decodes the escapes that <c>Quote</c> produces: <c>\'</c>, <c>\\</c> and <c>\0xx</c>.</summary>
        private static string Unescape(string text)
        {
            StringBuilder result = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c != '\\' || i + 1 >= text.Length)
                {
                    result.Append(c);
                    continue;
                }

                char next = text[i + 1];
                if (next == '\'' || next == '\\')
                {
                    result.Append(next);
                    i++;
                }
                else if (next == '0' && i + 3 < text.Length)
                {
                    int code;
                    if (int.TryParse(text.Substring(i + 2, 2), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture, out code))
                    {
                        result.Append((char)code);
                        i += 3;
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
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
            return Unescape(text.Substring(first + 1, last - first - 1));
        }

        private static string StripComment(string line)
        {
            // "//" inside a quoted literal is part of the string, not a comment.
            bool inString = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inString && c == '\\' && i + 1 < line.Length)
                {
                    i++;
                    continue;
                }
                if (c == '\'')
                {
                    inString = !inString;
                    continue;
                }
                if (!inString && c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    return line.Substring(0, i);
                }
            }
            return line;
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
