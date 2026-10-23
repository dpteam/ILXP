using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP
{
    /// <summary>One problem found in a method body.</summary>
    internal sealed class VerificationIssue
    {
        public string Method;
        public int Offset;
        public string Text;

        public override string ToString()
        {
            return Method + " IL_" + Offset.ToString("x4", CultureInfo.InvariantCulture) + ": " + Text;
        }
    }

    /// <summary>Result of verifying one assembly.</summary>
    internal sealed class VerificationReport
    {
        public VerificationReport()
        {
            Issues = new List<VerificationIssue>();
            Definitions = new List<VerificationIssue>();
        }

        /// <summary>Bodies that would not pass the CLR verifier / JIT.</summary>
        public List<VerificationIssue> Issues { get; private set; }

        /// <summary>Bodies whose prose (IL text) is valid but that reference something odd.</summary>
        public List<VerificationIssue> Definitions { get; private set; }

        public int Bodies { get; private set; }

        public int InvalidBodies { get; set; }

        public void Add(string method, int offset, string text)
        {
            Issues.Add(new VerificationIssue { Method = method, Offset = offset, Text = text });
        }

        public void AddDefinition(string method, string text)
        {
            Definitions.Add(new VerificationIssue { Method = method, Offset = 0, Text = text });
        }

        internal void CountBody()
        {
            Bodies++;
        }

        public string Format(int limit)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("bodies checked: " + Bodies);
            text.AppendLine("invalid bodies: " + InvalidBodies);

            int shown = 0;
            foreach (VerificationIssue issue in Issues)
            {
                if (shown++ >= limit)
                {
                    text.AppendLine("  ... " + (Issues.Count - limit) + " more");
                    break;
                }
                text.AppendLine("  ! " + issue);
            }

            shown = 0;
            foreach (VerificationIssue issue in Definitions)
            {
                if (shown++ >= limit)
                {
                    text.AppendLine("  ... " + (Definitions.Count - limit) + " more unresolved references");
                    break;
                }
                text.AppendLine("  ? " + issue);
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// Structural verifier for IL. It follows the evaluation stack through every instruction
    /// and reports bodies that a JIT would reject: stack underflow, stack depth above
    /// <c>.maxstack</c>, a non-empty stack at <c>ret</c>, local/argument indices out of range,
    /// branch targets that do not exist, and invalid exception handler ranges.
    ///
    /// This is deliberately not a full type-flow verifier: it catches the structural mistakes
    /// that an assembler makes (wrong opcode form, lost instruction, wrong stack discipline),
    /// which is exactly what a round-trip test needs. Type mismatches are reported only when
    /// the operand is a member of another type that cannot be resolved at all.
    /// </summary>
    internal static class IlVerifier
    {
        /// <summary>
        /// Set <c>ILXP_TRACE_STACK</c> to a method-name fragment to print the simulated stack
        /// depth of every instruction of the matching bodies (debugging aid).
        /// </summary>
        private static readonly string TraceFilter = Environment.GetEnvironmentVariable("ILXP_TRACE_STACK");

        private static bool TraceStack
        {
            get { return !string.IsNullOrEmpty(TraceFilter); }
        }
        public static VerificationReport Verify(AssemblyDefinition assembly)
        {
            VerificationReport report = new VerificationReport();
            if (assembly == null)
            {
                return report;
            }

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    VerifyType(type, report);
                }
            }
            return report;
        }

        private static void VerifyType(TypeDefinition type, VerificationReport report)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                VerifyMethod(method, report);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                VerifyType(nested, report);
            }
        }

        private static void VerifyMethod(MethodDefinition method, VerificationReport report)
        {
            if (!method.HasBody)
            {
                return;
            }

            MethodBody body = method.Body;
            if (body.Instructions.Count == 0)
            {
                return;
            }

            report.CountBody();
            int before = report.Issues.Count;
            string name = method.FullName;
            if (TraceStack && name.IndexOf(TraceFilter, StringComparison.Ordinal) < 0)
            {
                return;
            }
            if (TraceStack)
            {
                Console.WriteLine("== " + name);
            }

            Dictionary<Instruction, int> offsets = new Dictionary<Instruction, int>();
            int offset = 0;
            foreach (Instruction instruction in body.Instructions)
            {
                offsets[instruction] = offset;
                offset += InstructionSize(instruction);
            }

            // 1. Branch targets and handler boundaries must point into the body.
            foreach (Instruction instruction in body.Instructions)
            {
                Instruction target = instruction.Operand as Instruction;
                if (target != null && !offsets.ContainsKey(target))
                {
                    report.Add(name, offsets[instruction], "branch target is not part of the body");
                }

                Instruction[] targets = instruction.Operand as Instruction[];
                if (targets != null)
                {
                    foreach (Instruction item in targets)
                    {
                        if (!offsets.ContainsKey(item))
                        {
                            report.Add(name, offsets[instruction], "switch target is not part of the body");
                            break;
                        }
                    }
                }
            }

            VerifyHandlers(body, offsets, report, name);

            // 2. Operand ranges for the compact forms.
            foreach (Instruction instruction in body.Instructions)
            {
                int position;
                offsets.TryGetValue(instruction, out position);

                if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                {
                    VariableDefinition variable = instruction.Operand as VariableDefinition;
                    if (variable == null || variable.Index > byte.MaxValue)
                    {
                        report.Add(name, position, instruction.OpCode.Name + " needs a byte-sized local index");
                    }
                }
                else if (instruction.OpCode.OperandType == OperandType.InlineVar)
                {
                    VariableDefinition variable = instruction.Operand as VariableDefinition;
                    if (variable == null || variable.Index > ushort.MaxValue)
                    {
                        report.Add(name, position, instruction.OpCode.Name + " needs a 16-bit local index");
                    }
                }
                else if (instruction.OpCode.OperandType == OperandType.ShortInlineArg)
                {
                    ParameterDefinition parameter = instruction.Operand as ParameterDefinition;
                    if (parameter == null || ParameterIndex(parameter) > byte.MaxValue)
                    {
                        report.Add(name, position, instruction.OpCode.Name + " needs a byte-sized argument index");
                    }
                }

                if (instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    Instruction branch = instruction.Operand as Instruction;
                    int distance;
                    if (branch != null && offsets.TryGetValue(branch, out distance))
                    {
                        int delta = distance - (position + instruction.OpCode.Size + 1);
                        if (delta < sbyte.MinValue || delta > sbyte.MaxValue)
                        {
                            report.Add(name, position, instruction.OpCode.Name +
                                " target is out of range (" + delta.ToString(CultureInfo.InvariantCulture) + ")");
                        }
                    }
                }
            }

            // 3. Evaluation stack simulation.
            int maxDepth = SimulateStack(method, body, offsets, report, name);
            if (maxDepth > body.MaxStackSize)
            {
                report.Add(name, 0, "maxstack is " + body.MaxStackSize.ToString(CultureInfo.InvariantCulture) +
                                     " but the stack reaches " + maxDepth.ToString(CultureInfo.InvariantCulture));
            }

            if (report.Issues.Count > before)
            {
                report.InvalidBodies++;
            }
        }

        private static void VerifyHandlers(MethodBody body, Dictionary<Instruction, int> offsets,
            VerificationReport report, string name)
        {
            foreach (ExceptionHandler handler in body.ExceptionHandlers)
            {
                if (handler.TryStart == null || handler.HandlerStart == null)
                {
                    report.Add(name, 0, "exception handler has no start instruction");
                    continue;
                }

                if (!offsets.ContainsKey(handler.TryStart) || !offsets.ContainsKey(handler.HandlerStart))
                {
                    report.Add(name, 0, "exception handler boundary is not part of the body");
                    continue;
                }

                if (handler.TryEnd != null && !offsets.ContainsKey(handler.TryEnd))
                {
                    report.Add(name, 0, "try end is not part of the body");
                }
                if (handler.HandlerEnd != null && !offsets.ContainsKey(handler.HandlerEnd))
                {
                    report.Add(name, 0, "handler end is not part of the body");
                }

                int tryStart = offsets[handler.TryStart];
                int tryEnd = handler.TryEnd == null ? int.MaxValue : offsets[handler.TryEnd];
                if (tryEnd < tryStart)
                {
                    report.Add(name, tryStart, "try block has a negative length");
                }

                int handlerStart = offsets[handler.HandlerStart];
                int handlerEnd = handler.HandlerEnd == null ? int.MaxValue : offsets[handler.HandlerEnd];
                if (handlerEnd < handlerStart)
                {
                    report.Add(name, handlerStart, "handler block has a negative length");
                }

                if (handler.HandlerType == ExceptionHandlerType.Catch && handler.CatchType == null)
                {
                    report.Add(name, handlerStart, "catch clause without a catch type");
                }

                if (handler.HandlerType == ExceptionHandlerType.Filter &&
                    (handler.FilterStart == null || !offsets.ContainsKey(handler.FilterStart)))
                {
                    report.Add(name, handlerStart, "filter clause without a filter block");
                }
                else if (handler.FilterStart != null)
                {
                    int filterStart = offsets[handler.FilterStart];
                    if (filterStart >= offsets[handler.HandlerStart])
                    {
                        report.Add(name, handlerStart, "filter block does not precede the handler");
                    }
                }
            }
        }

        /// <summary>
        /// Walks the body and tracks the evaluation stack depth. Returns the maximum depth seen.
        /// </summary>
        private static int SimulateStack(MethodDefinition method, MethodBody body,
            Dictionary<Instruction, int> offsets, VerificationReport report, string name)
        {
            int[] depth = new int[body.Instructions.Count];
            bool[] seen = new bool[body.Instructions.Count];
            for (int i = 0; i < depth.Length; i++)
            {
                depth[i] = -1;
            }

            Dictionary<Instruction, int> index = new Dictionary<Instruction, int>(body.Instructions.Count);
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                index[body.Instructions[i]] = i;
            }

            // A catch or filter block starts with the exception object on the stack; a
            // finally or fault block starts empty.
            foreach (ExceptionHandler handler in body.ExceptionHandlers)
            {
                int start;
                if (handler.HandlerStart != null && index.TryGetValue(handler.HandlerStart, out start))
                {
                    // A catch clause and the handler of a filter both receive the exception
                    // object; a finally or fault block starts with an empty stack.
                    bool carriesException = handler.HandlerType == ExceptionHandlerType.Catch ||
                                            handler.HandlerType == ExceptionHandlerType.Filter;
                    if (!seen[start])
                    {
                        depth[start] = carriesException ? 1 : 0;
                        seen[start] = true;
                    }
                }

                if (handler.HandlerType == ExceptionHandlerType.Filter &&
                    handler.FilterStart != null && index.TryGetValue(handler.FilterStart, out start))
                {
                    // A filter receives the exception object on the stack.
                    if (!seen[start])
                    {
                        depth[start] = 1;
                        seen[start] = true;
                    }
                }
            }

            if (depth.Length > 0)
            {
                if (!seen[0])
                {
                    depth[0] = 0;
                    seen[0] = true;
                }
            }

            int maxDepth = 0;
            Stack<int> work = new Stack<int>();
            for (int i = 0; i < depth.Length; i++)
            {
                if (seen[i])
                {
                    work.Push(i);
                }
            }

            while (work.Count > 0)
            {
                int position = work.Pop();
                Instruction instruction = body.Instructions[position];
                int current = depth[position];

                if (current > maxDepth)
                {
                    maxDepth = current;
                }

                int delta;
                int pop;
                int push;
                GetStackEffect(instruction, out pop, out push);

                if (TraceStack)
                {
                    string operand = string.Empty;
                    MethodReference callee = instruction.Operand as MethodReference;
                    if (callee != null)
                    {
                        operand = "  <" + callee.DeclaringType.Name + "::" + callee.Name +
                                  " params=" + callee.Parameters.Count.ToString(CultureInfo.InvariantCulture) +
                                  " hasThis=" + callee.HasThis + " ret=" + callee.ReturnType.Name + ">";
                    }
                    Console.WriteLine("  depth=" + current.ToString(CultureInfo.InvariantCulture) +
                        " " + instruction.OpCode.Name + " pop=" + pop.ToString(CultureInfo.InvariantCulture) +
                        " push=" + push.ToString(CultureInfo.InvariantCulture) + operand);
                }

                // "ret" consumes the return value; the stack must be empty afterwards.
                bool isRet = instruction.OpCode.Code == Code.Ret;
                if (isRet)
                {
                    pop = method.ReturnType != null && method.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
                    push = 0;
                }

                delta = push - pop;

                if (current < pop)
                {
                    report.Add(name, offsets[instruction], "stack underflow: " + instruction.OpCode.Name +
                        " pops " + pop.ToString(CultureInfo.InvariantCulture) +
                        " but the stack holds " + current.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                int result = current + delta;
                if (result < 0)
                {
                    report.Add(name, offsets[instruction], "stack underflow after " + instruction.OpCode.Name);
                    continue;
                }

                // "leave" unwinds the stack before jumping, so the target starts empty.
                if (instruction.OpCode.Code == Code.Leave || instruction.OpCode.Code == Code.Leave_S)
                {
                    result = 0;
                }

                if (result > maxDepth)
                {
                    maxDepth = result;
                }

                // "ret" and "throw" end the basic block; a non-empty stack at ret is invalid.
                if (instruction.OpCode.Code == Code.Ret && result != 0)
                {
                    report.Add(name, offsets[instruction], "stack is not empty at ret (" +
                        result.ToString(CultureInfo.InvariantCulture) + " value(s))");
                }

                foreach (int target in Successors(instruction, index))
                {
                    if (target < 0 || target >= depth.Length)
                    {
                        continue;
                    }

                    if (!seen[target])
                    {
                        depth[target] = result;
                        seen[target] = true;
                        work.Push(target);
                    }
                    else if (depth[target] != result && IsStackSensitiveTarget(body.Instructions[target]))
                    {
                        // Diverging stack depths that meet at one instruction mean the body
                        // cannot be verified (unless the target pops nothing and is a branch).
                        report.Add(name, offsets[body.Instructions[target]],
                            "stack depth differs on entry (" + depth[target].ToString(CultureInfo.InvariantCulture) +
                            " vs " + result.ToString(CultureInfo.InvariantCulture) + ")");
                    }
                }

                // An instruction that ends a block always terminates the walk of that path.
                if (EndsBlock(instruction))
                {
                    continue;
                }

                int next = position + 1;
                if (next < depth.Length)
                {
                    if (!seen[next])
                    {
                        depth[next] = result;
                        seen[next] = true;
                        work.Push(next);
                    }
                    else if (depth[next] != result && IsStackSensitiveTarget(body.Instructions[next]))
                    {
                        report.Add(name, offsets[body.Instructions[next]],
                            "stack depth differs on entry (" + depth[next].ToString(CultureInfo.InvariantCulture) +
                            " vs " + result.ToString(CultureInfo.InvariantCulture) + ")");
                    }
                }
            }

            if (TraceStack)
            {
                DumpDepths(body, depth, seen, offsets);
            }

            return maxDepth;
        }

        /// <summary>Prints the computed depth of every instruction (debugging aid).</summary>
        private static void DumpDepths(MethodBody body, int[] depth, bool[] seen,
            Dictionary<Instruction, int> offsets)
        {
            Console.WriteLine("  --- depths");
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                if (!seen[i])
                {
                    continue;
                }
                Console.WriteLine("  " + offsets[body.Instructions[i]].ToString("x4") + "  d=" +
                    depth[i].ToString(CultureInfo.InvariantCulture) + "  " + body.Instructions[i].OpCode.Name);
            }
        }

        /// <summary>
        /// A target is stack-sensitive when the merge happens in the middle of a sequence.
        /// Reporting it only there avoids noise on plain labels that simply continue a chain.
        /// </summary>
        private static bool IsStackSensitiveTarget(Instruction instruction)
        {
            return true;
        }

        private static IEnumerable<int> Successors(Instruction instruction, Dictionary<Instruction, int> index)
        {
            List<int> targets = new List<int>();

            // Explicit branch target: br / br.cond / leave (short and long forms).
            Instruction branch = instruction.Operand as Instruction;
            if (branch != null &&
                (instruction.OpCode.OperandType == OperandType.InlineBrTarget ||
                 instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget))
            {
                int target;
                if (index.TryGetValue(branch, out target))
                {
                    targets.Add(target);
                }
                return targets;
            }

            Instruction[] table = instruction.Operand as Instruction[];
            if (table != null)
            {
                foreach (Instruction item in table)
                {
                    int target;
                    if (index.TryGetValue(item, out target))
                    {
                        targets.Add(target);
                    }
                }
            }

            // Fall-through is added by the caller, so nothing else is returned here.
            return targets;
        }

        private static bool EndsBlock(Instruction instruction)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Branch:
                case FlowControl.Return:
                case FlowControl.Throw:
                    return true;
                default:
                    return instruction.OpCode.Code == Code.Throw ||
                           instruction.OpCode.Code == Code.Rethrow ||
                           instruction.OpCode.Code == Code.Endfinally ||
                           instruction.OpCode.Code == Code.Endfilter;
            }
        }

        private static int ParameterIndex(ParameterDefinition parameter)
        {
            int index = parameter.Index;
            if (parameter.Method != null && parameter.Method.HasThis)
            {
                index++;
            }
            return index;
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

        /// <summary>Pops and pushes for one instruction (no call-site signature analysis).</summary>
        private static void GetStackEffect(Instruction instruction, out int pop, out int push)
        {
            pop = 0;
            push = 0;

            switch (instruction.OpCode.Code)
            {
                case Code.Nop:
                case Code.Break:
                case Code.Volatile:
                case Code.Tail:
                case Code.Readonly:
                case Code.Unaligned:
                case Code.Constrained:
                case Code.Endfilter:
                    return;

                case Code.Initobj:
                    pop = 1;
                    return;

                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                case Code.Ldarg:
                case Code.Ldarg_S:
                case Code.Ldarga:
                case Code.Ldarga_S:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                case Code.Ldloc:
                case Code.Ldloc_S:
                case Code.Ldloca:
                case Code.Ldloca_S:
                case Code.Ldc_I4_M1:
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                case Code.Ldc_I4_S:
                case Code.Ldc_I4:
                case Code.Ldc_I8:
                case Code.Ldc_R4:
                case Code.Ldc_R8:
                case Code.Ldstr:
                case Code.Ldnull:
                case Code.Ldtoken:
                case Code.Ldsfld:
                case Code.Ldsflda:
                case Code.Ldftn:
                case Code.Arglist:
                case Code.Sizeof:
                    push = 1;
                    return;

                // localloc pops the size and pushes the address of the new block.
                case Code.Localloc:
                    pop = 1;
                    push = 1;
                    return;

                // ldvirtftn consumes the object reference and pushes the method pointer,
                // so the net effect is zero.
                case Code.Ldvirtftn:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Ldfld:
                case Code.Ldflda:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Dup:
                    pop = 1;
                    push = 2;
                    return;

                case Code.Pop:
                    pop = 1;
                    return;

                case Code.Starg:
                case Code.Starg_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Stsfld:
                    pop = 1;
                    return;

                case Code.Stfld:
                    pop = 2;
                    return;

                case Code.Stobj:
                    pop = 2;
                    return;

                case Code.Cpobj:
                    pop = 2;
                    return;

                case Code.Cpblk:
                case Code.Initblk:
                    pop = 3;
                    return;

                case Code.Castclass:
                case Code.Isinst:
                case Code.Unbox:
                case Code.Unbox_Any:
                case Code.Box:
                case Code.Neg:
                case Code.Not:
                case Code.Conv_I:
                case Code.Conv_I1:
                case Code.Conv_I2:
                case Code.Conv_I4:
                case Code.Conv_I8:
                case Code.Conv_Ovf_I:
                case Code.Conv_Ovf_I1:
                case Code.Conv_Ovf_I2:
                case Code.Conv_Ovf_I4:
                case Code.Conv_Ovf_I8:
                case Code.Conv_Ovf_I_Un:
                case Code.Conv_Ovf_I1_Un:
                case Code.Conv_Ovf_I2_Un:
                case Code.Conv_Ovf_I4_Un:
                case Code.Conv_Ovf_I8_Un:
                case Code.Conv_Ovf_U:
                case Code.Conv_Ovf_U1:
                case Code.Conv_Ovf_U2:
                case Code.Conv_Ovf_U4:
                case Code.Conv_Ovf_U8:
                case Code.Conv_Ovf_U_Un:
                case Code.Conv_Ovf_U1_Un:
                case Code.Conv_Ovf_U2_Un:
                case Code.Conv_Ovf_U4_Un:
                case Code.Conv_Ovf_U8_Un:
                case Code.Conv_R_Un:
                case Code.Conv_R4:
                case Code.Conv_R8:
                case Code.Conv_U:
                case Code.Conv_U1:
                case Code.Conv_U2:
                case Code.Conv_U4:
                case Code.Conv_U8:
                case Code.Ldind_I:
                case Code.Ldind_I1:
                case Code.Ldind_I2:
                case Code.Ldind_I4:
                case Code.Ldind_I8:
                case Code.Ldind_R4:
                case Code.Ldind_R8:
                case Code.Ldind_Ref:
                case Code.Ldind_U1:
                case Code.Ldind_U2:
                case Code.Ldind_U4:
                case Code.Ldobj:
                case Code.Ldlen:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Ldelema:
                    pop = 2;
                    push = 1;
                    return;

                case Code.Stind_I:
                case Code.Stind_I1:
                case Code.Stind_I2:
                case Code.Stind_I4:
                case Code.Stind_I8:
                case Code.Stind_R4:
                case Code.Stind_R8:
                case Code.Stind_Ref:
                    pop = 2;
                    return;

                case Code.Add:
                case Code.Sub:
                case Code.Mul:
                case Code.Div:
                case Code.Div_Un:
                case Code.Rem:
                case Code.Rem_Un:
                case Code.And:
                case Code.Or:
                case Code.Xor:
                case Code.Shl:
                case Code.Shr:
                case Code.Shr_Un:
                case Code.Add_Ovf:
                case Code.Add_Ovf_Un:
                case Code.Sub_Ovf:
                case Code.Sub_Ovf_Un:
                case Code.Mul_Ovf:
                case Code.Mul_Ovf_Un:
                case Code.Clt:
                case Code.Clt_Un:
                case Code.Cgt:
                case Code.Cgt_Un:
                case Code.Ceq:
                    pop = 2;
                    push = 1;
                    return;

                case Code.Mkrefany:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Ldelem_Any:
                    pop = 2;
                    push = 1;
                    return;

                case Code.Stelem_Any:
                    pop = 3;
                    return;

                case Code.Refanyval:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Ldelem_I:
                case Code.Ldelem_I1:
                case Code.Ldelem_I2:
                case Code.Ldelem_I4:
                case Code.Ldelem_I8:
                case Code.Ldelem_R4:
                case Code.Ldelem_R8:
                case Code.Ldelem_Ref:
                case Code.Ldelem_U1:
                case Code.Ldelem_U2:
                case Code.Ldelem_U4:
                    pop = 2;
                    push = 1;
                    return;

                case Code.Stelem_I:
                case Code.Stelem_I1:
                case Code.Stelem_I2:
                case Code.Stelem_I4:
                case Code.Stelem_I8:
                case Code.Stelem_R4:
                case Code.Stelem_R8:
                case Code.Stelem_Ref:
                    pop = 3;
                    return;

                case Code.Refanytype:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Ckfinite:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Throw:
                    pop = 1;
                    return;

                case Code.Rethrow:
                    return;

                case Code.Calli:
                    // The callee pops a pointer and its arguments; treat the pointer as one
                    // value and leave the result unknown (assume one push).
                    pop = 1;
                    push = 1;
                    return;

                case Code.Newobj:
                {
                    MethodReference ctor = instruction.Operand as MethodReference;
                    pop = ctor == null ? 0 : ctor.Parameters.Count + (ctor.HasThis ? 0 : 1);
                    push = 1;
                    return;
                }

                case Code.Call:
                case Code.Callvirt:
                {
                    MethodReference callee = instruction.Operand as MethodReference;
                    if (callee == null)
                    {
                        return;
                    }

                    // A generic instance passes its own type arguments, not the signature ones.
                    MethodReference signature = callee.GetElementMethod();
                    pop = signature.Parameters.Count + (signature.HasThis ? 1 : 0);
                    if (callee is GenericInstanceMethod)
                    {
                        pop = signature.Parameters.Count + (signature.HasThis ? 1 : 0);
                    }
                    push = signature.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
                    return;
                }

                case Code.Jmp:
                    pop = 0;
                    return;

                case Code.Ret:
                    return;

                case Code.Leave:
                case Code.Leave_S:
                    // Clears the stack and jumps; the target starts with an empty stack.
                    pop = 0;
                    return;

                case Code.Newarr:
                    pop = 1;
                    push = 1;
                    return;

                case Code.Brfalse:
                case Code.Brfalse_S:
                case Code.Brtrue:
                case Code.Brtrue_S:
                    pop = 1;
                    return;

                case Code.Beq:
                case Code.Beq_S:
                case Code.Bge:
                case Code.Bge_S:
                case Code.Bge_Un:
                case Code.Bge_Un_S:
                case Code.Bgt:
                case Code.Bgt_S:
                case Code.Bgt_Un:
                case Code.Bgt_Un_S:
                case Code.Ble:
                case Code.Ble_S:
                case Code.Ble_Un:
                case Code.Ble_Un_S:
                case Code.Blt:
                case Code.Blt_S:
                case Code.Blt_Un:
                case Code.Blt_Un_S:
                case Code.Bne_Un:
                case Code.Bne_Un_S:
                    pop = 2;
                    return;

                case Code.Switch:
                    pop = 1;
                    return;

                default:
                    // Unknown but harmless (prefixes, future opcodes): assume it neither pops
                    // nor pushes, which keeps the rest of the body verifiable.
                    return;
            }
        }
    }
}
