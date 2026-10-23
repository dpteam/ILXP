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
using GenericParameterAttributes = Mono.Cecil.GenericParameterAttributes;
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

            /// <summary>Nested blocks (.try / catch / finally / filter / fault) of the body.</summary>
            public Stack<BlockInfo> Blocks = new Stack<BlockInfo>();

            /// <summary>Block kind of the next "{" line, set by the preceding directive.</summary>
            public BlockKind PendingKind;
            public string PendingCatchType;

            /// <summary>Handlers grouped by their protected region, resolved when the method ends.</summary>
            public List<HandlerGroup> Groups = new List<HandlerGroup>();
            public HandlerGroup CurrentGroup;
            public HandlerRecord PendingFilter;
        }

        private enum BlockKind
        {
            None,
            Try,
            Catch,
            Finally,
            Fault,
            Filter,
            Handler
        }

        private sealed class BlockInfo
        {
            public BlockKind Kind;
            public int StartIndex;
            public string CatchType;

            /// <summary>The protected region this handler clause belongs to.</summary>
            public HandlerGroup Group;
        }

        private sealed class HandlerGroup
        {
            public int TryStart;
            public int TryEnd;
            public List<HandlerRecord> Handlers = new List<HandlerRecord>();
        }

        private sealed class HandlerRecord
        {
            public ExceptionHandlerType Kind;
            public string CatchType;
            public int HandlerStart;
            public int HandlerEnd;
            public int FilterStart;
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

            // Pre-scan: which types does this text define? Knowing them up front makes
            // forward references deterministic.
            DeclaredTypes = new HashSet<string>(StringComparer.Ordinal);
            DeclaredValueTypes = new HashSet<string>(StringComparer.Ordinal);

            // Assembly headers first: pre-declaring types creates references on demand, and
            // those must already know the version and public key token of each assembly.
            foreach (string rawLine in lines)
            {
                string line = StripComment(rawLine).Trim();
                if (line.StartsWith(".assembly", StringComparison.OrdinalIgnoreCase) &&
                    line.IndexOf("extern", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    ParseAssemblyExtern(assembly, line);
                }
            }

            Predeclaring = true;
            foreach (string rawLine in lines)
            {
                string line = StripComment(rawLine).Trim();
                if (!line.StartsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string declared = ExtractQuoted(line);
                if (string.IsNullOrEmpty(declared))
                {
                    continue;
                }

                DeclaredTypes.Add(declared);
                if (line.IndexOf("System.Enum", StringComparison.Ordinal) >= 0 ||
                    line.IndexOf("System.ValueType", StringComparison.Ordinal) >= 0)
                {
                    DeclaredValueTypes.Add(declared);
                }

                // Create the skeleton now, so a reference from any method body (even one that
                // appears earlier in the text) finds a real definition with generic parameters.
                // Base types and interfaces are filled in by the second pass: they can refer
                // to types declared further down, which must exist by then.
                try
                {
                    CreateType(assembly, line);
                }
                catch (Exception ex)
                {
                    LastWarnings.Add("Unable to predeclare " + declared + ": " + ex.Message);
                }
            }

            // Second pass: resolve base types and interfaces now that every type exists.
            foreach (string rawLine in lines)
            {
                string line = StripComment(rawLine).Trim();
                if (!line.StartsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    Predeclaring = false;
                    CreateType(assembly, line);
                }
                catch (Exception ex)
                {
                    LastWarnings.Add("Unable to declare " + line + ": " + ex.Message);
                }
            }
            Predeclaring = false;
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
                        CurrentTypeOwner = currentType;
                        CurrentMethodOwner = null;
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
                if (line.StartsWith(".genericparams", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyGenericParameterNames(currentType, line);
                    continue;
                }
                // Type-level generic metadata. Inside a method body the same directives
                // describe the method's own parameters, so they are left for that branch.
                if (currentMethod == null && line.StartsWith(".constraint", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyGenericConstraint(assembly, currentType, line, false);
                    continue;
                }
                if (currentMethod == null && line.StartsWith(".gpflags", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyGenericFlags(currentType, line, false);
                    continue;
                }
                if (line.StartsWith(".method", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentType != null)
                    {
                        currentMethod = CreateMethod(currentType, line);
                        if (currentMethod == null)
                        {
                            LastWarnings.Add("Method header skipped: " + line);
                        }
                        methodContext = currentMethod == null ? null : new MethodContext { Method = currentMethod };
                        methodDepth = open - close;
                        CurrentMethodOwner = currentMethod;
                        CurrentTypeOwner = currentType;
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

                    if (line.StartsWith(".constraint", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyGenericConstraint(assembly, currentMethod, line, true);
                        continue;
                    }
                    if (line.StartsWith(".gpflags", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyGenericFlags(currentMethod, line, true);
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
                    if (line.StartsWith(".override", StringComparison.OrdinalIgnoreCase))
                    {
                        object target = ResolveMethodOperand(currentMethod.Module,
                            line.Substring(".override".Length).Trim(), OpCodes.Callvirt, currentMethod);
                        MethodReference overridden = target as MethodReference;
                        if (overridden != null)
                        {
                            currentMethod.Overrides.Add(overridden);
                        }
                        else
                        {
                            LastWarnings.Add("Unresolved .override skipped: " + line);
                        }
                        continue;
                    }

                    if (line.StartsWith(".try", StringComparison.OrdinalIgnoreCase))
                    {
                        if (line.IndexOf(" to ", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            LastWarnings.Add("Long-form .try directive skipped: " + line);
                            continue;
                        }

                        methodContext.PendingKind = BlockKind.Try;
                    }
                    else if (line.StartsWith("catch", StringComparison.OrdinalIgnoreCase))
                    {
                        methodContext.PendingKind = BlockKind.Catch;
                        methodContext.PendingCatchType = line.Substring(5).Trim();
                    }
                    else if (line.StartsWith("finally", StringComparison.OrdinalIgnoreCase))
                    {
                        methodContext.PendingKind = BlockKind.Finally;
                    }
                    else if (line.StartsWith("fault", StringComparison.OrdinalIgnoreCase))
                    {
                        methodContext.PendingKind = BlockKind.Fault;
                    }
                    else if (line.StartsWith("filter", StringComparison.OrdinalIgnoreCase))
                    {
                        methodContext.PendingKind = BlockKind.Filter;
                    }

                    if (line.StartsWith("IL_", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseInstruction(methodContext, line);
                    }

                    // Handler blocks are plain brace blocks; their boundaries are instruction
                    // indices, so they are recorded while the body is still being read.
                    for (int opened = 0; opened < open; opened++)
                    {
                        OpenBlock(methodContext);
                    }
                    for (int closed = 0; closed < close; closed++)
                    {
                        CloseBlock(methodContext);
                    }

                    // A method owns its braces: nested .try blocks must not close the
                    // enclosing type, and every method must resolve its own branches.
                    methodDepth += open - close;
                    if (methodDepth <= 0)
                    {
                        MaterializeHandlers(methodContext);
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

            // An existing reference may have been created by a type reference before the
            // .assembly extern line was read (types are pre-declared). It then lacks the
            // version and the public key token, which breaks binding to the real assembly -
            // so the header always wins.
            foreach (AssemblyNameReference existing in assembly.MainModule.AssemblyReferences)
            {
                if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Version = version;
                    if (token != null)
                    {
                        existing.PublicKeyToken = token;
                    }
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
                        // A MethodSpec (generic instantiation such as IsNullOrEmpty<byte>) must
                        // stay a member reference: replacing it with the open definition would
                        // leave !!0 unbound and the JIT rejects the body.
                        if (target is MethodSpecification)
                        {
                            continue;
                        }

                        // Interface calls must stay member references: a callvirt with a
                        // MethodDef token of an interface method is not valid IL.
                        TypeDefinition declaring = target.DeclaringType == null
                            ? null
                            : FindTypeDefinition(module, target.DeclaringType.FullName);
                        if (declaring == null || !declaring.IsInterface)
                        {
                            MethodDefinition definition = FindMethodDefinition(module, target);
                            if (definition != null && !definition.HasGenericParameters)
                            {
                                instruction.Operand = definition;
                            }
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
            MethodDefinition found = FindMethodByShape(declaring, reference.Name, parameters);
            if (Environment.GetEnvironmentVariable("ILXP_TRACE_OVL") == "1" && reference.Name == "SkipWhileIterator")
            {
                Console.WriteLine("  post " + (found == null ? "null" : found.Parameters[1].ParameterType.Name) +
                    " from " + reference.Parameters.Count + " " +
                    (reference.Parameters.Count > 1 ? AssemblyDiff.DescribeTypeKey(reference.Parameters[1].ParameterType) : "-"));
            }
            return found;
        }

        /// <summary>
        /// Puts a one-byte integer operand into the CLR type the opcode requires: an sbyte for
        /// <c>ldc.i4.s</c> and a byte for everything else (e.g. <c>unaligned</c>).
        /// </summary>
        private static object CoerceShortInlineI(Instruction instruction)
        {
            object operand = instruction.Operand;
            if (operand == null)
            {
                return null;
            }

            int value;
            if (operand is sbyte)
            {
                value = (sbyte)operand;
            }
            else if (operand is byte)
            {
                value = (byte)operand;
            }
            else if (operand is int)
            {
                value = (int)operand;
            }
            else
            {
                return null;
            }

            return instruction.OpCode.Code == Code.Ldc_I4_S ? (object)(sbyte)value : (object)(byte)value;
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
                            // "ldc.i4.s" needs an sbyte and every other one-byte operand a
                            // byte; the metadata writer casts to the exact CLR type.
                            instruction.Operand = CoerceShortInlineI(instruction);
                            valid = instruction.Operand is sbyte || instruction.Operand is byte;
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

                    // A nested type of a generic type re-declares the outer generic
                    // parameters, so its arity is the inherited count plus its own.
                    int inherited = parent.GenericParameters.Count;
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

                        int ownArity = Arity(parts[i]);
                        if (isInnermost && !Predeclaring)
                        {
                            TypeReference baseType = string.IsNullOrEmpty(extends)
                                ? (isInterface ? null : assembly.MainModule.TypeSystem.Object)
                                : ResolveType(assembly.MainModule, extends);
                            if (baseType != null)
                            {
                                nested.BaseType = baseType;
                            }
                            ApplyArrayInitSize(nested);
                        }

                        // Generic parameters must exist before the interface list is resolved,
                        // because it can refer to them ("IComparer`1<!0>").
                        ApplyGenericParameters(nested, inherited + ownArity);
                        if (!Predeclaring && isInnermost)
                        {
                            AttachInterfaces(nested, assembly.MainModule, interfaces);
                            ApplyArrayInitSize(nested);
                        }
                        inherited += ownArity;
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

            TypeDefinition type = FindTopLevel(assembly.MainModule, namespaceName, name);
            if (type == null)
            {
                type = new TypeDefinition(namespaceName, name, attributes);
                assembly.MainModule.Types.Add(type);
            }
            else
            {
                type.Attributes = attributes;
            }

            // The base type and interface list can refer to this type's own parameters,
            // so the parameters and the owner context come first.
            ApplyGenericParameters(type, name);
            CurrentTypeOwner = type;

            if (Predeclaring)
            {
                // Skeleton only: base type and interface list need every type to exist.
                return type;
            }

            if (!string.IsNullOrEmpty(extends))
            {
                type.BaseType = ResolveType(assembly.MainModule, extends);
            }
            else if (!isInterface)
            {
                type.BaseType = assembly.MainModule.TypeSystem.Object;
            }
            AttachInterfaces(type, assembly.MainModule, interfaces);
            ApplyArrayInitSize(type);
            return type;
        }

        /// <summary>Recreates the generic parameters implied by the arity in the type name.</summary>
        private static void ApplyGenericParameters(TypeDefinition type, string name)
        {
            ApplyGenericParameters(type, Arity(name));
        }

        /// <summary>
        /// Applies the names listed by a <c>.genericparams</c> directive, so a rebuilt type
        /// keeps the parameter names of the original ("T", "TKey", ...).
        /// </summary>
        private static void ApplyGenericParameterNames(IGenericParameterProvider owner, string line)
        {
            if (owner == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            int open = line.IndexOf('(');
            int close = line.LastIndexOf(')');
            if (open < 0 || close <= open)
            {
                return;
            }

            string[] names = line.Substring(open + 1, close - open - 1).Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                while (owner.GenericParameters.Count <= i)
                {
                    owner.GenericParameters.Add(new GenericParameter(owner));
                }

                // GenericParameter hides the Name setter; the base implementation is used.
                ((TypeReference)owner.GenericParameters[i]).Name = name;
            }
        }

        /// <summary>
        /// Parses <c>.constraint !!1 = [mscorlib]System.IComparable</c> and attaches the
        /// constraint to the addressed generic parameter.
        /// </summary>
        private static void ApplyGenericConstraint(AssemblyDefinition assembly,
            IGenericParameterProvider owner, string line, bool methodScope)
        {
            if (owner == null)
            {
                return;
            }

            string text = line.Substring(".constraint".Length).Trim();
            int equals = text.IndexOf('=');
            if (equals < 0)
            {
                return;
            }

            string target = text.Substring(0, equals).Trim();
            string typeName = text.Substring(equals + 1).Trim();
            int index = GenericParameterIndex(target);
            if (index < 0 || typeName.Length == 0)
            {
                return;
            }

            while (owner.GenericParameters.Count <= index)
            {
                owner.GenericParameters.Add(new GenericParameter(owner));
            }

            ModuleDefinition module = ModuleOf(assembly, owner);
            TypeReference typeOwner = owner as TypeReference;
            MethodDefinition methodOwner = owner as MethodDefinition;

            object resolved = ResolveTypeOperand(module, typeName, methodOwner);
            TypeReference constraint = resolved as TypeReference;
            if (constraint == null || ReferenceEquals(constraint, Unresolved))
            {
                constraint = ResolveTypeWithOwner(module, typeName, typeOwner, methodOwner)
                             ?? ResolveType(module, typeName);
            }

            if (constraint != null)
            {
                // A constraint must be encoded with the right element type: System.ValueType
                // is a value type, and a "class" encoding makes the CLR reject the type.
                owner.GenericParameters[index].Constraints.Add(MarkValueType(constraint));
            }
        }

        /// <summary>Parses <c>.gpflags !0 = reference ctor</c> into generic parameter attributes.</summary>
        private static void ApplyGenericFlags(IGenericParameterProvider owner, string line, bool methodScope)
        {
            if (owner == null)
            {
                return;
            }

            string text = line.Substring(".gpflags".Length).Trim();
            int equals = text.IndexOf('=');
            if (equals < 0)
            {
                return;
            }

            int index = GenericParameterIndex(text.Substring(0, equals).Trim());
            if (index < 0)
            {
                return;
            }

            while (owner.GenericParameters.Count <= index)
            {
                owner.GenericParameters.Add(new GenericParameter(owner));
            }

            GenericParameter parameter = owner.GenericParameters[index];
            GenericParameterAttributes attributes = parameter.Attributes;
            string flags = text.Substring(equals + 1);

            if (HasKeyword(flags, "covariant")) attributes |= GenericParameterAttributes.Covariant;
            if (HasKeyword(flags, "contravariant")) attributes |= GenericParameterAttributes.Contravariant;
            if (HasKeyword(flags, "reference")) attributes |= GenericParameterAttributes.ReferenceTypeConstraint;
            if (HasKeyword(flags, "valuetype")) attributes |= GenericParameterAttributes.NotNullableValueTypeConstraint;
            if (HasKeyword(flags, "ctor")) attributes |= GenericParameterAttributes.DefaultConstructorConstraint;

            parameter.Attributes = attributes;
        }

        /// <summary>Reads the parameter position from "!0" / "!!1".</summary>
        private static int GenericParameterIndex(string text)
        {
            int start = 0;
            while (start < text.Length && text[start] == '!')
            {
                start++;
            }

            int end = start;
            while (end < text.Length && char.IsDigit(text[end]))
            {
                end++;
            }

            int index;
            if (end > start &&
                int.TryParse(text.Substring(start, end - start), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out index))
            {
                return index;
            }
            return -1;
        }

        private static ModuleDefinition ModuleOf(AssemblyDefinition assembly, IGenericParameterProvider owner)
        {
            TypeReference type = owner as TypeReference;
            if (type != null && type.Module != null)
            {
                return type.Module;
            }

            MethodReference method = owner as MethodReference;
            if (method != null && method.Module != null)
            {
                return method.Module;
            }

            return assembly.MainModule;
        }

        /// <summary>
        /// Reads the arity from a name such as "List`1" (0 when absent).</summary>
        private static int Arity(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }

            int tick = name.LastIndexOf('`');
            if (tick < 0 || tick == name.Length - 1)
            {
                return 0;
            }

            int arity;
            return int.TryParse(name.Substring(tick + 1), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out arity) ? arity : 0;
        }

        private static void ApplyGenericParameters(TypeDefinition type, int arity)
        {
            if (type == null || arity <= 0)
            {
                return;
            }

            while (type.GenericParameters.Count < arity)
            {
                type.GenericParameters.Add(new GenericParameter(type));
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
                // keeps all of its entries. The type itself is the owner of "!N".
                TypeReference reference = ResolveTypeWithOwner(module, name, type, null);
                if (reference == null)
                {
                    continue;
                }

                bool exists = false;
                foreach (TypeReference existing in type.Interfaces)
                {
                    if (string.Equals(existing.FullName, reference.FullName, StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    type.Interfaces.Add(reference);
                }
            }
        }

        private static TypeDefinition FindTopLevel(ModuleDefinition module, string namespaceName, string name)
        {
            foreach (TypeDefinition type in module.Types)
            {
                if (string.Equals(type.Namespace, namespaceName, StringComparison.Ordinal) &&
                    string.Equals(type.Name, name, StringComparison.Ordinal))
                {
                    return type;
                }
            }
            return null;
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

            // Marshalling info is written as a suffix: ".field ... name marshal(lpwstr)".
            // It has to be removed before the declaration is split into type and name.
            NativeType? pendingMarshal = null;
            int marshalAt = declaration.LastIndexOf(" marshal(", StringComparison.Ordinal);
            if (marshalAt > 0)
            {
                int marshalClose = declaration.LastIndexOf(')');
                if (marshalClose > marshalAt)
                {
                    string nativeText = declaration.Substring(marshalAt + " marshal(".Length,
                        marshalClose - marshalAt - " marshal(".Length);
                    declaration = declaration.Substring(0, marshalAt).Trim();
                    NativeType native = ParseNativeType(nativeText.Trim());
                    if (native != NativeType.None)
                    {
                        pendingMarshal = native;
                    }
                }
            }

            // Field data may be declared inline: ".field ... <name> at I_0000".
            int at = declaration.LastIndexOf(" at ", StringComparison.Ordinal);
            if (at > 0)
            {
                dataLabel = declaration.Substring(at + 4).Trim();
                declaration = declaration.Substring(0, at).Trim();
            }

            // The declaration is "<modifiers...> <type> <name>": take the last two tokens
            // so that any combination of attribute keywords is ignored.
            string typeText;
            string fieldName;
            string modifiers;
            SplitDeclaration(declaration, out modifiers, out typeText, out fieldName);
            if (string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            FieldAttributes attributes = FieldVisibility(modifiers);
            if (HasKeyword(modifiers, "static")) attributes |= FieldAttributes.Static;
            if (HasKeyword(modifiers, "initonly")) attributes |= FieldAttributes.InitOnly;
            if (HasKeyword(modifiers, "literal")) attributes |= FieldAttributes.Literal | FieldAttributes.Static;
            if (HasKeyword(modifiers, "specialname")) attributes |= FieldAttributes.SpecialName;
            if (HasKeyword(modifiers, "rtspecialname")) attributes |= FieldAttributes.RTSpecialName;
            if (HasKeyword(modifiers, "notserialized")) attributes |= FieldAttributes.NotSerialized;

            FieldDefinition field = new FieldDefinition(fieldName, attributes,
                ResolveTypeWithOwner(type.MainModule(), typeText, type, null));
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
            if (pendingMarshal.HasValue)
            {
                field.MarshalInfo = new MarshalInfo(pendingMarshal.Value);

                // HasFieldMarshal is a real metadata attribute (0x1000), not derived from the
                // FieldMarshal row, so it has to be set explicitly.
                field.Attributes |= FieldAttributes.HasFieldMarshal;
            }
            return field;
        }

        /// <summary>Maps an ILAsm marshalling keyword to <see cref="NativeType"/>.</summary>
        private static NativeType ParseNativeType(string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "bool": return NativeType.Boolean;
                case "int8": return NativeType.I1;
                case "unsigned int8": return NativeType.U1;
                case "int16": return NativeType.I2;
                case "unsigned int16": return NativeType.U2;
                case "int32": return NativeType.I4;
                case "unsigned int32": return NativeType.U4;
                case "int64": return NativeType.I8;
                case "unsigned int64": return NativeType.U8;
                case "float32": return NativeType.R4;
                case "float64": return NativeType.R8;
                case "lpstr": return NativeType.LPStr;
                case "lpwstr": return NativeType.LPWStr;
                case "lptstr": return NativeType.LPTStr;
                case "int": return NativeType.Int;
                case "unsigned int": return NativeType.UInt;
                case "bstr": return NativeType.BStr;
                case "ansi bstr": return NativeType.ANSIBStr;
                case "tbstr": return NativeType.TBStr;
                case "variant bool": return NativeType.VariantBool;
                case "iunknown": return NativeType.IUnknown;
                case "idispatch": return NativeType.IDispatch;
                case "struct": return NativeType.Struct;
                case "interface": return NativeType.IntF;
                case "safearray": return NativeType.SafeArray;
                case "byvalstr": return NativeType.ByValStr;
                case "as any": return NativeType.ASAny;
                case "lpstruct": return NativeType.LPStruct;
                case "func": return NativeType.Func;
                case "currency": return NativeType.Currency;
                case "error": return NativeType.Error;
            }

            // A numeric fallback keeps unusual native types round-tripping.
            int value;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? (NativeType)value
                : NativeType.None;
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
                name, attributes, ResolveTypeWithOwner(type.MainModule(), PropertyTypeText(before), type, null));

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
                            ResolveTypeWithOwner(type.MainModule(), parameterType, type, null)));
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
                name, attributes, ResolveTypeWithOwner(type.MainModule(), PropertyTypeText(before), type, null));
            type.Events.Add(@event);
            return @event;
        }

        /// <summary>
        /// Extracts the type from a directive header such as
        /// <c>specialname instance Dictionary`2&lt;String, Int32&gt;</c>. Splitting on the
        /// last top-level space keeps generic arguments with commas intact.
        /// </summary>
        private static string PropertyTypeText(string before)
        {
            if (string.IsNullOrEmpty(before))
            {
                return string.Empty;
            }

            int space = LastTopLevelSpace(before);
            return space >= 0 ? before.Substring(space + 1).Trim() : before.Trim();
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
        /// Builds a type reference from a dotted namespace and a name that may still contain
        /// nested separators ("List`1/Enumerator"). Nested parts become declaring types, which
        /// is what the metadata requires.
        /// </summary>
        private static TypeReference BuildTypeReference(ModuleDefinition module, IMetadataScope scope,
            string namespaceName, string typeName)
        {
            int slash = typeName.IndexOf('/');
            if (slash < 0)
            {
                // A type of this module must be referenced through its definition: a loose
                // TypeRef would give its generic parameters a TypeRef owner, and the metadata
                // writer then emits MemberRefs that the CLR cannot resolve.
                if (ReferenceEquals(scope, module))
                {
                    TypeDefinition known = FindTopLevel(module, namespaceName, typeName);
                    if (known != null)
                    {
                        return known;
                    }
                }

                return new TypeReference(namespaceName, typeName, module, scope);
            }

            string outerName = namespaceName.Length > 0
                ? namespaceName + "." + typeName.Substring(0, slash)
                : typeName.Substring(0, slash);

            // Prefer the real definition: a nested generic type must be referenced through its
            // definition, otherwise the metadata lacks the type parameters the arguments need.
            TypeReference current = FindTypeDefinition(module, outerName);
            if (current == null)
            {
                current = new TypeReference(namespaceName, typeName.Substring(0, slash), module, scope);
            }

            string[] parts = typeName.Substring(slash + 1).Split('/');
            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                TypeDefinition parentDefinition = current as TypeDefinition;
                TypeDefinition existing = parentDefinition == null ? null : FindNested(parentDefinition, part);
                if (existing != null)
                {
                    current = existing;
                    continue;
                }

                TypeReference nested = new TypeReference(string.Empty, part, module, scope);
                nested.DeclaringType = current;
                current = nested;
            }
            return current;
        }

        /// <summary>
        /// Splits "<visibility/other keywords> <type> <name>" where the type may carry
        /// modreq/modopt groups: "public int32 modreq(IsVolatile) counter". Splitting on the
        /// last space alone would cut the modifier off the type.
        /// </summary>
        private static void SplitDeclaration(string declaration, out string modifiers,
            out string typeText, out string name)
        {
            modifiers = string.Empty;
            typeText = string.Empty;
            name = string.Empty;

            List<string> tokens = SplitTopLevelSpaces(declaration);
            if (tokens.Count == 0)
            {
                return;
            }

            name = tokens[tokens.Count - 1];
            if (tokens.Count < 2)
            {
                return;
            }

            // The type is the last token that is not a modifier group, plus the modifier
            // groups that follow it.
            int typeIndex = -1;
            for (int i = tokens.Count - 2; i >= 0; i--)
            {
                if (!IsModifierToken(tokens[i]))
                {
                    typeIndex = i;
                    break;
                }
            }

            if (typeIndex < 0)
            {
                return;
            }

            StringBuilder type = new StringBuilder(tokens[typeIndex]);
            for (int i = typeIndex + 1; i < tokens.Count - 1; i++)
            {
                if (IsModifierToken(tokens[i]))
                {
                    type.Append(' ').Append(tokens[i]);
                }
            }
            typeText = type.ToString();

            StringBuilder keywords = new StringBuilder();
            for (int i = 0; i < typeIndex; i++)
            {
                if (keywords.Length > 0)
                {
                    keywords.Append(' ');
                }
                keywords.Append(tokens[i]);
            }
            modifiers = keywords.ToString();
        }

        private static bool IsModifierToken(string token)
        {
            return token.StartsWith("modreq(", StringComparison.Ordinal) ||
                   token.StartsWith("modopt(", StringComparison.Ordinal);
        }
        /// <summary>Splits on spaces that are not inside angle brackets.</summary>
        private static List<string> SplitTopLevelSpaces(string text)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return tokens;
            }

            int depth = 0;
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    if (depth > 0)
                    {
                        depth--;
                    }
                }
                else if (c == ' ' && depth == 0)
                {
                    if (i > start)
                    {
                        tokens.Add(text.Substring(start, i - start));
                    }
                    start = i + 1;
                }
            }

            if (start < text.Length)
            {
                tokens.Add(text.Substring(start));
            }
            return tokens;
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
            string genericNames = null;
            int genericDirective = tail.IndexOf("genericparams", StringComparison.OrdinalIgnoreCase);
            if (genericDirective >= 0)
            {
                genericNames = tail.Substring(genericDirective);
                tail = tail.Substring(0, genericDirective).Trim();
            }
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
            bool isStrict = HasKeyword(signature, "strict");

            MethodAttributes attributes = MethodVisibility(signature);
            if (isStatic) attributes |= MethodAttributes.Static;
            if (isVirtual) attributes |= MethodAttributes.Virtual;
            if (isAbstract) attributes |= MethodAttributes.Abstract;
            if (isSpecialName) attributes |= MethodAttributes.SpecialName;
            if (isRuntimeSpecialName) attributes |= MethodAttributes.RTSpecialName;
            if (isHideBySig) attributes |= MethodAttributes.HideBySig;
            if (isNewSlot) attributes |= MethodAttributes.NewSlot;
            if (isFinal) attributes |= MethodAttributes.Final;
            if (isStrict) attributes |= MethodAttributes.CheckAccessOnOverride;
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

            if (genericNames != null)
            {
                ApplyGenericParameterNames(method, genericNames);
            }

            method.ReturnType = ResolveTypeWithOwner(type.MainModule(), returnTypeName, type, method)
                ?? type.MainModule().TypeSystem.Object;

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
                        ResolveTypeWithOwner(type.MainModule(), parameterType, type, method)
                            ?? type.MainModule().TypeSystem.Object));
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
                method.Body.Variables.Add(new VariableDefinition(name,
                    ResolveTypeWithOwner(method.Module, typeName, method.DeclaringType, method)));
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

        /// <summary>Records the start of a "{ ... }" block of the current method body.</summary>
        private static void OpenBlock(MethodContext context)
        {
            BlockKind kind = context.PendingKind;
            string catchType = context.PendingCatchType;
            context.PendingKind = BlockKind.None;
            context.PendingCatchType = null;

            // The handler block of a filter follows the filter block without a keyword.
            if (kind == BlockKind.None && context.PendingFilter != null)
            {
                kind = BlockKind.Handler;
            }

            BlockInfo block = new BlockInfo
            {
                Kind = kind,
                StartIndex = context.Method.Body.Instructions.Count,
                CatchType = catchType,
                Group = context.CurrentGroup
            };
            context.Blocks.Push(block);
        }

        /// <summary>Records the end of a block and turns handler clauses into records.</summary>
        private static void CloseBlock(MethodContext context)
        {
            if (context.Blocks.Count == 0)
            {
                return; // the method's own closing brace
            }

            BlockInfo block = context.Blocks.Pop();
            int endIndex = context.Method.Body.Instructions.Count;

            switch (block.Kind)
            {
                case BlockKind.Try:
                    context.CurrentGroup = new HandlerGroup
                    {
                        TryStart = block.StartIndex,
                        TryEnd = endIndex
                    };
                    context.Groups.Add(context.CurrentGroup);
                    break;

                case BlockKind.Catch:
                case BlockKind.Finally:
                case BlockKind.Fault:
                {
                    HandlerGroup group = block.Group ?? context.CurrentGroup;
                    if (group == null)
                    {
                        LastWarnings.Add("Exception handler without a .try block ignored.");
                        break;
                    }

                    HandlerRecord record = new HandlerRecord
                    {
                        Kind = block.Kind == BlockKind.Catch ? ExceptionHandlerType.Catch
                             : block.Kind == BlockKind.Finally ? ExceptionHandlerType.Finally
                             : ExceptionHandlerType.Fault,
                        CatchType = block.CatchType,
                        HandlerStart = block.StartIndex,
                        HandlerEnd = endIndex
                    };
                    group.Handlers.Add(record);
                    break;
                }

                case BlockKind.Filter:
                {
                    HandlerGroup group = block.Group ?? context.CurrentGroup;
                    if (group == null)
                    {
                        LastWarnings.Add("Filter without a .try block ignored.");
                        break;
                    }

                    HandlerRecord record = new HandlerRecord
                    {
                        Kind = ExceptionHandlerType.Filter,
                        HandlerStart = endIndex,
                        HandlerEnd = endIndex,
                        FilterStart = block.StartIndex
                    };
                    group.Handlers.Add(record);
                    context.PendingFilter = record;
                    break;
                }

                case BlockKind.Handler:
                {
                    if (context.PendingFilter != null)
                    {
                        context.PendingFilter.HandlerStart = block.StartIndex;
                        context.PendingFilter.HandlerEnd = endIndex;
                        context.PendingFilter = null;
                    }
                    break;
                }
            }
        }

        /// <summary>Converts the recorded index ranges into Cecil exception handlers.</summary>
        private static void MaterializeHandlers(MethodContext context)
        {
            IList<Instruction> instructions = context.Method.Body.Instructions;

            foreach (HandlerGroup group in context.Groups)
            {
                Instruction tryStart = InstructionAt(instructions, group.TryStart);
                Instruction tryEnd = InstructionAt(instructions, group.TryEnd);
                if (tryStart == null || tryEnd == null)
                {
                    LastWarnings.Add("Try block without a following instruction ignored.");
                    continue;
                }

                foreach (HandlerRecord record in group.Handlers)
                {
                    Instruction handlerStart = InstructionAt(instructions, record.HandlerStart);
                    Instruction handlerEnd = InstructionAt(instructions, record.HandlerEnd);
                    if (handlerStart == null)
                    {
                        LastWarnings.Add("Empty exception handler ignored.");
                        continue;
                    }

                    ExceptionHandler handler = new ExceptionHandler(record.Kind)
                    {
                        TryStart = tryStart,
                        TryEnd = tryEnd,
                        HandlerStart = handlerStart,
                        HandlerEnd = handlerEnd
                    };

                    if (record.Kind == ExceptionHandlerType.Catch)
                    {
                        handler.CatchType = ResolveType(context.Method.Module, record.CatchType);
                        if (handler.CatchType == null)
                        {
                            LastWarnings.Add("Catch type '" + record.CatchType + "' could not be resolved.");
                            continue;
                        }
                    }
                    else if (record.Kind == ExceptionHandlerType.Filter)
                    {
                        Instruction filterStart = InstructionAt(instructions, record.FilterStart);
                        if (filterStart == null)
                        {
                            LastWarnings.Add("Filter block without a start instruction ignored.");
                            continue;
                        }
                        handler.FilterStart = filterStart;
                    }

                    context.Method.Body.ExceptionHandlers.Add(handler);
                }
            }

            context.Groups.Clear();
        }

        private static Instruction InstructionAt(IList<Instruction> instructions, int index)
        {
            return index >= 0 && index < instructions.Count ? instructions[index] : null;
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
                    return ResolveMethodOperand(moduleOfText, text, opcode, context.Method);
                case OperandType.InlineField:
                    return ResolveFieldOperand(moduleOfText, text, context.Method);
                case OperandType.InlineType:
                    return ResolveTypeOperand(moduleOfText, text, context.Method);
                case OperandType.InlineTok:
                {
                    // ldtoken can name a type, a field or a method.
                    int separator = text.IndexOf("::", StringComparison.Ordinal);
                    if (separator > 0)
                    {
                        int paren = text.IndexOf('(', separator);
                        return paren > separator
                            ? ResolveMethodOperand(moduleOfText, text, opcode, context.Method)
                            : ResolveFieldOperand(moduleOfText, text, context.Method);
                    }
                    return ResolveTypeOperand(moduleOfText, text, context.Method);
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

            // "int32 modreq(IsVolatile)" and "void modopt(CallConvCdecl)": the modifier is
            // part of the signature and must be rebuilt as a modifier type.
            TypeReference modified = TryResolveModifiedType(module, name.Trim());
            if (modified != null)
            {
                return modified;
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
                case "native int":
                case "intptr":
                case "System.IntPtr": return module.TypeSystem.IntPtr;
                case "native uint":
                case "uintptr":
                case "System.UIntPtr": return module.TypeSystem.UIntPtr;
                case "typedref":
                case "System.TypedReference": return module.TypeSystem.TypedReference;
            }

            string clean = name.Trim();

            // A generic parameter without an explicit owner is resolved against the member
            // currently being built (see CurrentTypeOwner / CurrentMethodOwner).
            if (clean.StartsWith("!", StringComparison.Ordinal))
            {
                TypeReference parameter = ResolveTypeWithOwner(module, clean, CurrentTypeOwner, CurrentMethodOwner);
                return parameter ?? module.TypeSystem.Object;
            }

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

            // Generic arguments are reconstructed from the trailing list; a '<' inside a
            // nested type name ("<>c") must not be mistaken for one.
            string genericElement;
            string genericArguments;
            if (TrySplitGeneric(clean, out genericElement, out genericArguments))
            {
                TypeReference genericBase = ResolveType(module, genericElement);
                GenericInstanceType instance = new GenericInstanceType(genericBase);
                foreach (string argument in SplitTopLevel(genericArguments))
                {
                    string text = argument.Trim();
                    if (text.Length > 0)
                    {
                        instance.GenericArguments.Add(ResolveTypeWithOwner(module, text, genericBase, null)
                            ?? module.TypeSystem.Object);
                    }
                }
                return WrapType(instance, ranks, byRef, pointers);
            }

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
                                instance.GenericArguments.Add(ResolveTypeWithOwner(module, text, genericBase, null)
                                    ?? module.TypeSystem.Object);
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

            TypeReference reference = BuildTypeReference(module,
                ScopeFor(module, clean), namespaceName, typeName);
            return WrapType(MarkValueType(reference), ranks, byRef, pointers);
        }

        /// <summary>
        /// Types declared anywhere in the IL text are known before parsing starts, so an
        /// unqualified name that is declared later in the file still binds to this module
        /// instead of being guessed as a mscorlib type.
        /// </summary>
        private static HashSet<string> DeclaredTypes = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Types of this module declared as structs or enums (value types).</summary>
        private static HashSet<string> DeclaredValueTypes = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// True while the first pass creates type skeletons. In that pass base types and
        /// interfaces are skipped, because the types they name may not exist yet.
        /// </summary>
        private static bool Predeclaring;

        /// <summary>Picks the module scope for a type declared in this module, else mscorlib.</summary>
        private static IMetadataScope ScopeFor(ModuleDefinition module, string name)
        {
            if (name != null && DeclaredTypes.Contains(name.Trim()))
            {
                return module;
            }

            int generic = name == null ? -1 : name.IndexOf('<');
            if (generic > 0 && DeclaredTypes.Contains(name.Substring(0, generic).Trim()))
            {
                return module;
            }

            return NormalizeScope(module, module.TypeSystem.Object.Scope);
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

                // A forward reference must not lose the arity: a nested type re-declares the
                // outer parameters, so methods that use "!0" still resolve.
                ApplyGenericParameters(nested, parent.GenericParameters.Count + Arity(part));
                if (DeclaredValueTypes.Contains(nested.FullName) && nested.BaseType == null)
                {
                    // Built the same way as any other reference, so the scope table stays
                    // consistent with the rest of the module.
                    nested.BaseType = ResolveType(parent.Module, "System.ValueType");
                    nested.IsValueType = true;
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

        /// <summary>Applies the suffix of a decorated type ("[]", "[,]", "&amp;", "*") to a base type.</summary>
        private static TypeReference DecorateType(TypeReference element, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
            {
                return element;
            }

            bool byRef = false;
            List<int> ranks = new List<int>();
            int pointers = 0;

            for (int i = 0; i < suffix.Length; i++)
            {
                char c = suffix[i];
                if (c == '&')
                {
                    byRef = true;
                }
                else if (c == '*')
                {
                    pointers++;
                }
                else if (c == '[')
                {
                    int close = suffix.IndexOf(']', i);
                    if (close < 0)
                    {
                        break;
                    }

                    string inner = suffix.Substring(i + 1, close - i - 1).Trim();
                    ranks.Add(inner.Length == 0 ? 1 : inner.Split(',').Length);
                    i = close;
                }
            }

            return WrapType(element, ranks, byRef, pointers);
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

        /// <summary>Declaring type / method of the member currently being built.</summary>
        private static TypeReference CurrentTypeOwner;
        private static MethodDefinition CurrentMethodOwner;

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

            TypeReference modified = TryResolveModifiedType(module, name);
            if (modified != null)
            {
                return modified;
            }

            if (name.StartsWith("!", StringComparison.Ordinal))
            {
                bool methodParameter = name.StartsWith("!!", StringComparison.Ordinal);
                int start = methodParameter ? 2 : 1;
                int end = start;
                while (end < name.Length && char.IsDigit(name[end]))
                {
                    end++;
                }

                int position;
                if (end > start &&
                    int.TryParse(name.Substring(start, end - start), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out position))
                {
                    TypeReference parameter = null;
                    if (methodParameter && methodOwner != null &&
                        position >= 0 && position < methodOwner.GenericParameters.Count)
                    {
                        parameter = methodOwner.GenericParameters[position];
                    }
                    else if (!methodParameter && typeOwner != null)
                    {
                        // Inside a member reference, VAR refers to the generic parameters of
                        // the declaring type definition (not to the substituted arguments),
                        // so resolve against the element type of an instantiation.
                        TypeReference definitionOwner = typeOwner;
                        TypeSpecification specification = definitionOwner as TypeSpecification;
                        while (specification != null)
                        {
                            definitionOwner = specification.ElementType;
                            specification = definitionOwner as TypeSpecification;
                        }

                        TypeDefinition definition = Resolver.TryResolveType(definitionOwner);
                        if (definition == null)
                        {
                            // A type of the module being built.
                            definition = FindTypeDefinition(module, definitionOwner.FullName);
                        }
                        if (definition != null && position >= 0 && position < definition.GenericParameters.Count)
                        {
                            parameter = definition.GenericParameters[position];
                        }
                    }

                    if (parameter != null)
                    {
                        // "!0[]" and "!0&" carry decorations after the parameter index.
                        return DecorateType(parameter, name.Substring(end));
                    }
                }

                // The operand may belong to the caller (a MethodSpec argument such as
                // "Array.IndexOf<!0>"), so retry in the context of the member being built.
                if (CurrentTypeOwner != null || CurrentMethodOwner != null)
                {
                    if (!ReferenceEquals(typeOwner, CurrentTypeOwner) ||
                        !ReferenceEquals(methodOwner, CurrentMethodOwner))
                    {
                        return DecorateType(
                            ResolveTypeWithOwner(module, text, CurrentTypeOwner, CurrentMethodOwner),
                            string.Empty);
                    }
                }

                return null;
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
                            instance.GenericArguments.Add(ResolveTypeWithOwner(module, part, typeOwner, methodOwner)
                                        ?? module.TypeSystem.Object);
                        }
                    }
                    // Anything after the closing '>' is decoration ("List`1<Int32>&").
                    return DecorateType(instance, name.Substring(close + 1));
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

                // A type of this module may still be undefined at this point (forward
                // reference), so look it up in the module being built as well.
                if (!isValueType)
                {
                    ModuleDefinition module = reference.Module;
                    if (module != null && reference.Scope is ModuleDefinition)
                    {
                        TypeDefinition definition = FindTypeDefinition(module, reference.FullName);
                        isValueType = definition != null && definition.IsValueType;
                    }
                }

                // Forward reference to a struct or enum of this module: the type does not
                // exist yet, but the pre-scan already recorded how it is declared.
                if (!isValueType && DeclaredValueTypes.Contains(reference.FullName))
                {
                    isValueType = true;
                }

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

        /// <summary>
        /// Parses "&lt;type&gt; modreq(&lt;modifier&gt;)" / "modopt" into a modifier type.
        /// Returns null when the text carries no modifier.
        /// </summary>
        private static TypeReference TryResolveModifiedType(ModuleDefinition module, string text)
        {
            int modreq = text.IndexOf(" modreq(", StringComparison.Ordinal);
            int modopt = text.IndexOf(" modopt(", StringComparison.Ordinal);

            int index = modreq >= 0 && (modopt < 0 || modreq < modopt) ? modreq : modopt;
            if (index <= 0)
            {
                return null;
            }

            bool required = index == modreq;
            int open = index + (required ? " modreq".Length : " modopt".Length);
            int close = text.IndexOf(')', open);
            if (close < 0)
            {
                return null;
            }

            string elementText = text.Substring(0, index).Trim();
            string modifierText = text.Substring(open + 1, close - open - 1).Trim();
            if (elementText.Length == 0 || modifierText.Length == 0)
            {
                return null;
            }

            TypeReference element = ResolveType(module, elementText);
            TypeReference modifier = ResolveType(module, modifierText);
            if (element == null || modifier == null)
            {
                return null;
            }

            return required
                ? new RequiredModifierType(modifier, element)
                : (TypeReference)new OptionalModifierType(modifier, element);
        }

        /// <summary>
        /// Splits a trailing generic argument list: "List`1&lt;Int32&gt;" or
        /// "Outer`1/&lt;&gt;c&lt;!0&gt;". Only the list that closes at the very end counts,
        /// because a nested type may contain angle brackets in its own name ("&lt;&gt;c").
        /// </summary>
        private static bool TrySplitGeneric(string text, out string element, out string arguments)
        {
            element = text;
            arguments = null;

            if (text == null || text.Length < 3 || text[text.Length - 1] != '>')
            {
                return false;
            }

            int depth = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == '>')
                {
                    depth++;
                }
                else if (text[i] == '<')
                {
                    depth--;
                    if (depth == 0)
                    {
                        if (i == 0)
                        {
                            return false;
                        }

                        string head = text.Substring(0, i).Trim();
                        string tail = text.Substring(i + 1, text.Length - i - 2);
                        if (head.Length == 0 || tail.Length == 0 || head.IndexOf('`') < 0)
                        {
                            return false;
                        }

                        element = head;
                        arguments = tail;
                        return true;
                    }
                }
            }
            return false;
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
                case "native int":
                case "intptr":
                case "System.IntPtr": return module.TypeSystem.IntPtr;
                case "native uint":
                case "uintptr":
                case "System.UIntPtr": return module.TypeSystem.UIntPtr;
                case "typedref":
                case "System.TypedReference": return module.TypeSystem.TypedReference;
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

        private static object ResolveMethodOperand(ModuleDefinition module, string text, OpCode opcode, MethodDefinition owner)
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

                // A generic method instantiation ends the method name ("Select<int32,string>").
                // A '<' followed by more name text belongs to an explicit interface name
                // ("IEnumerable`1<T>.GetEnumerator") and must stay part of the name. A method
                // whose name itself starts with brackets ("<Show>b__0") keeps them too, so the
                // generic argument list is the one that closes at the very end.
                string genericArguments = null;
                int genericOpen = TrailingGenericOpen(methodName);
                if (genericOpen > 0)
                {
                    genericArguments = methodName.Substring(genericOpen + 1, methodName.Length - genericOpen - 2);
                    methodName = methodName.Substring(0, genericOpen).Trim();
                }

                int space = LastTopLevelSpace(typePart);
                string returnTypeName = space > 0 ? typePart.Substring(0, space).Trim() : "void";
                string typeName = space > 0 ? typePart.Substring(space + 1).Trim() : typePart;

                TypeReference declaring = ResolveTypeOperand(module, typeName, owner) as TypeReference;
                if (declaring == null)
                {
                    return Unresolved;
                }

                List<string> parameterTexts = new List<string>();
                foreach (string part in SplitTopLevel(parameters))
                {
                    string parameter = part.Trim();
                    if (parameter.Length > 0)
                    {
                        parameterTexts.Add(parameter);
                    }
                }

                // A scratch member reference gives "!!N" a legal owner while the signature is
                // being resolved. Its arity comes from the parameter list only: the "!!N" of the
                // *declaring* type arguments belong to the enclosing method and must not count,
                // or the reference would claim generic parameters it does not have.
                MethodReference scratch = new MethodReference(methodName, module.TypeSystem.Void, declaring);
                scratch.HasThis = hasThis;
                int scratchArity = Math.Max(MaxMethodGenericParameter(returnTypeName),
                                            MaxMethodGenericParameter(string.Join(", ", parameterTexts.ToArray())));
                for (int i = 0; i <= scratchArity; i++)
                {
                    scratch.GenericParameters.Add(new GenericParameter(scratch));
                }

                List<TypeReference> parameterTypes = new List<TypeReference>();
                foreach (string parameter in parameterTexts)
                {
                    parameterTypes.Add(ResolveTypeWithOwner(module, parameter, declaring, scratch)
                        ?? module.TypeSystem.Object);
                }

                // Prefer the real definition: it carries the correct static/instance flag, the
                // right generic arity, and lets the call site use a MethodDef token.
                MethodReference target = null;
                TypeDefinition definition = declaring as TypeDefinition ?? FindTypeDefinition(module, typeName);

                // A member of a generic instantiation ("Action`2<Object,EventArgs>") cannot be
                // referenced through its open definition; such a call needs a member reference
                // whose declaring type is the instantiation.
                bool instantiated = declaring is GenericInstanceType;

                if (definition != null && !instantiated)
                {
                    target = FindMethodByShape(definition, methodName, parameterTypes, genericArguments);
                }

                // External generic methods: match against the real definition so the
                // signature comes from metadata instead of being reconstructed from text.
                // An instantiated declaring type must keep its own TypeSpec, so the open
                // definition is only reusable when the call is not instantiated.
                if (target == null && genericArguments != null && !instantiated)
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
                            parameterTypes.Count, arity, parameterTypes);
                        if (match != null)
                        {
                            target = module.Import(match);
                        }
                    }
                }

                if (target == null)
                {
                    // No definition available: the scratch reference already carries the
                    // generic parameters the signature needs.
                    scratch.ReturnType = ResolveTypeWithOwner(module, returnTypeName, declaring, scratch)
                                        ?? module.TypeSystem.Object;
                    foreach (TypeReference parameterType in parameterTypes)
                    {
                        scratch.Parameters.Add(new ParameterDefinition(parameterType));
                    }
                    target = scratch;
                }
                if (genericArguments != null)
                {
                    GenericInstanceMethod spec = new GenericInstanceMethod(target);
                    foreach (string part in SplitTopLevel(genericArguments))
                    {
                        string argument = part.Trim();
                        if (argument.Length > 0)
                        {
                            spec.GenericArguments.Add(
                                ResolveTypeWithOwner(module, argument, declaring, CurrentMethodOwner)
                                ?? module.TypeSystem.Object);
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
            int parameterCount, int genericArity, List<TypeReference> parameters)
        {
            MethodDefinition single = null;
            MethodDefinition exact = null;
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

                // Among equally-shaped overloads the parameter types decide; without this an
                // overload taking Func`2 could be picked for a call that passes Func`3.
                if (exact == null && parameters != null && SameShape(method, parameters))
                {
                    exact = method;
                }
            }

            if (exact != null)
            {
                return exact;
            }

            // A name+arity match alone is not enough to pick an overload: two overloads can
            // differ exactly in a parameter type (Func`2 vs Func`3). Guessing here silently
            // rewrites a call to the wrong method, so only accept a lone candidate when there
            // is nothing to compare against.
            if (parameterCount == 0 && parameters == null)
            {
                return matches == 1 ? single : null;
            }
            return null;
        }

        private static bool SameShape(MethodDefinition method, List<TypeReference> parameters)
        {
            if (method.Parameters.Count != parameters.Count)
            {
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                string left = AssemblyDiff.DescribeTypeKey(method.Parameters[i].ParameterType);
                string right = AssemblyDiff.DescribeTypeKey(parameters[i]);
                if (!string.Equals(left, right, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Returns the highest <c>!!N</c> position referenced by a signature text.</summary>
        private static int MaxMethodGenericParameter(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return -1;
            }

            int max = -1;
            int index = 0;
            while ((index = text.IndexOf("!!", index, StringComparison.Ordinal)) >= 0)
            {
                int start = index + 2;
                int end = start;
                while (end < text.Length && char.IsDigit(text[end]))
                {
                    end++;
                }

                int position;
                if (end > start &&
                    int.TryParse(text.Substring(start, end - start), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out position) && position > max)
                {
                    max = position;
                }

                index = end > start ? end : start;
            }
            return max;
        }

        /// <summary>Finds a method by name and normalized parameter types (overload-safe).</summary>
        private static MethodDefinition FindMethodByShape(TypeDefinition type, string name,
            List<TypeReference> parameters)
        {
            return FindMethodByShape(type, name, parameters, null);
        }

        /// <summary>
        /// Returns the index of the '&lt;' that opens the generic argument list at the very end
        /// of a method name, or -1. Names may contain brackets themselves ("&lt;Show&gt;b__0"),
        /// so the matching '<' is found by scanning back from the final '&gt;'.
        /// </summary>
        private static int TrailingGenericOpen(string name)
        {
            if (string.IsNullOrEmpty(name) || name[name.Length - 1] != '>')
            {
                return -1;
            }

            int depth = 0;
            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (name[i] == '>')
                {
                    depth++;
                }
                else if (name[i] == '<')
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

        /// <summary>
        /// Finds a method by name, parameter shape and (optionally) generic arity. Matching on
        /// arity matters for overloads such as <c>Read(item, fn)</c> and
        /// <c>Read(ref item, fn)</c> that differ only in arity.
        /// </summary>
        private static MethodDefinition FindMethodByShape(TypeDefinition type, string name,
            List<TypeReference> parameters, string genericArguments)
        {
            int arity = -1;
            if (genericArguments != null)
            {
                arity = 0;
                foreach (string part in SplitTopLevel(genericArguments))
                {
                    if (part.Trim().Length > 0)
                    {
                        arity++;
                    }
                }
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (!string.Equals(method.Name, name, StringComparison.Ordinal) ||
                    method.Parameters.Count != parameters.Count)
                {
                    continue;
                }

                if (arity >= 0 && method.GenericParameters.Count != arity)
                {
                    continue;
                }

                bool match = true;
                for (int i = 0; i < parameters.Count; i++)
                {
                    // Full shape, not just the short name: "int32[]" and "int32&" are
                    // different overloads of the same method.
                    string left = AssemblyDiff.DescribeTypeKey(method.Parameters[i].ParameterType);
                    string right = AssemblyDiff.DescribeTypeKey(parameters[i]);
                    if (Environment.GetEnvironmentVariable("ILXP_TRACE_OVL") == "1" && name == "SkipWhileIterator")
                    {
                        Console.WriteLine("    cmp " + method.GenericParameters.Count + " [" + left + "] [" + right + "]");
                    }
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

        private static object ResolveFieldOperand(ModuleDefinition module, string text, MethodDefinition owner)
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

                TypeReference declaring = ResolveTypeOperand(module, typeName, owner) as TypeReference;
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
            return ResolveTypeOperand(module, text, null);
        }

        private static object ResolveTypeOperand(ModuleDefinition module, string text, MethodDefinition owner)
        {
            try
            {
                text = text.Trim();


                // A generic parameter (!0 / !!0) belongs to the enclosing type or method.
                if (text.StartsWith("!", StringComparison.Ordinal))
                {
                    TypeReference parameter = ResolveTypeWithOwner(module, text,
                        owner == null ? null : owner.DeclaringType, owner);
                    return parameter == null ? Unresolved : (object)parameter;
                }

                IMetadataScope scope = NormalizeScope(module, module.TypeSystem.Object.Scope);
                bool moduleScope = false;

                if (text.StartsWith("[", StringComparison.Ordinal))
                {
                    int close = text.IndexOf(']');
                    if (close > 0)
                    {
                        string assemblyName = text.Substring(1, close - 1);
                        text = text.Substring(close + 1).Trim();
                        if (string.Equals(assemblyName, ".module", StringComparison.OrdinalIgnoreCase))
                        {
                            // A type defined in this module; the module itself is the scope.
                            moduleScope = true;
                            scope = module;
                        }
                        else
                        {
                            AssemblyNameReference reference = FindOrCreateReference(module, assemblyName);
                            if (reference != null)
                            {
                                scope = reference;
                            }
                        }
                    }
                }

                if (!moduleScope)
                {
                    // An unqualified name that is declared in this module binds here.
                    if (DeclaredTypes.Contains(text))
                    {
                        scope = module;
                    }
                    else
                    {
                        scope = NormalizeScope(module, scope);
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
                // Generic arguments come from the trailing list only: a '<' inside a nested
                // type name ("<>c") must not be read as the start of an argument list.
                string genericElement;
                string genericArguments;
                if (TrySplitGeneric(text, out genericElement, out genericArguments))
                {
                    // The element keeps the scope of the whole name: recursing with the already
                    // stripped text would lose "[System.Core]" and bind to mscorlib.
                    int baseDot = genericElement.LastIndexOf('.');
                    TypeReference genericBase = BuildTypeReference(module, scope,
                        baseDot > 0 ? genericElement.Substring(0, baseDot) : string.Empty,
                        baseDot > 0 ? genericElement.Substring(baseDot + 1) : genericElement);
                    genericBase = MarkValueType(genericBase);
                    if (genericBase != null)
                    {
                        GenericInstanceType instance = new GenericInstanceType(genericBase);
                        foreach (string argument in SplitTopLevel(genericArguments))
                        {
                            string part = argument.Trim();
                            if (part.Length > 0)
                            {
                                instance.GenericArguments.Add(
                                    ResolveTypeWithOwner(module, part, genericBase, owner)
                                        ?? module.TypeSystem.Object);
                            }
                        }
                        return WrapType(instance, ranks, byRef, pointers);
                    }

                    text = genericElement;
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

                TypeReference element = BuildTypeReference(module, scope, namespaceName, typeName);
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
