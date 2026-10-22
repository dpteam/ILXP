using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP
{
    /// <summary>Call-graph node statistics for one method.</summary>
    internal sealed class CallNode
    {
        public string Key;
        public string Display;
        public int OutDegree;
        public int InDegree;
        public bool IsEntryPoint;

        public override string ToString()
        {
            return Display;
        }
    }

    /// <summary>Result of the call-graph analysis.</summary>
    internal sealed class CallGraphReport
    {
        public CallGraphReport()
        {
            Nodes = new Dictionary<string, CallNode>(StringComparer.Ordinal);
            Edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            Callers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            UnresolvedCalls = new List<string>();
        }

        public Dictionary<string, CallNode> Nodes { get; private set; }

        /// <summary>Caller key → set of callee keys (only methods inside this assembly).</summary>
        public Dictionary<string, HashSet<string>> Edges { get; private set; }

        /// <summary>Callee key → set of caller keys.</summary>
        public Dictionary<string, HashSet<string>> Callers { get; private set; }

        /// <summary>Calls that leave the assembly (external references), by target name.</summary>
        public List<string> UnresolvedCalls { get; private set; }

        public int InternalCallCount { get; private set; }

        public void AddEdge(string caller, string callee)
        {
            HashSet<string> targets;
            if (!Edges.TryGetValue(caller, out targets))
            {
                targets = new HashSet<string>(StringComparer.Ordinal);
                Edges[caller] = targets;
            }
            if (!targets.Add(callee))
            {
                return;
            }

            InternalCallCount++;

            CallNode node;
            if (Nodes.TryGetValue(caller, out node))
            {
                node.OutDegree = targets.Count;
            }

            HashSet<string> sources;
            if (!Callers.TryGetValue(callee, out sources))
            {
                sources = new HashSet<string>(StringComparer.Ordinal);
                Callers[callee] = sources;
            }
            if (sources.Add(caller) && Nodes.TryGetValue(callee, out node))
            {
                node.InDegree = sources.Count;
            }
        }

        public List<CallNode> TopCallers(int count)
        {
            List<CallNode> list = new List<CallNode>(Nodes.Values);
            list.Sort(delegate(CallNode a, CallNode b) { return b.OutDegree.CompareTo(a.OutDegree); });
            if (list.Count > count)
            {
                list.RemoveRange(count, list.Count - count);
            }
            return list;
        }

        public List<CallNode> TopCallees(int count)
        {
            List<CallNode> list = new List<CallNode>(Nodes.Values);
            list.Sort(delegate(CallNode a, CallNode b) { return b.InDegree.CompareTo(a.InDegree); });
            if (list.Count > count)
            {
                list.RemoveRange(count, list.Count - count);
            }
            return list;
        }

        /// <summary>
        /// Methods with no incoming edge inside the assembly. Entry points, constructors and
        /// event handlers are excluded: they are reached by the runtime or by delegates.
        /// </summary>
        public List<CallNode> FindOrphans()
        {
            List<CallNode> orphans = new List<CallNode>();
            foreach (CallNode node in Nodes.Values)
            {
                if (node.InDegree != 0 || node.IsEntryPoint)
                {
                    continue;
                }

                string key = node.Key;
                if (key.IndexOf("::.ctor", StringComparison.Ordinal) >= 0 ||
                    key.IndexOf("::.cctor", StringComparison.Ordinal) >= 0)
                {
                    continue;
                }

                orphans.Add(node);
            }
            orphans.Sort(delegate(CallNode a, CallNode b)
            {
                return string.Compare(a.Display, b.Display, StringComparison.Ordinal);
            });
            return orphans;
        }

        /// <summary>Finds strongly connected components with more than one member (cycles).</summary>
        public List<List<string>> FindCycles()
        {
            List<List<string>> cycles = new List<List<string>>();
            Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> low = new Dictionary<string, int>(StringComparer.Ordinal);
            Stack<string> stack = new Stack<string>();
            HashSet<string> onStack = new HashSet<string>(StringComparer.Ordinal);
            int counter = 0;

            foreach (string key in Nodes.Keys)
            {
                if (!index.ContainsKey(key))
                {
                    StrongConnect(key, ref counter, index, low, stack, onStack, cycles);
                }
            }

            return cycles;
        }

        private void StrongConnect(string key, ref int counter, Dictionary<string, int> index,
            Dictionary<string, int> low, Stack<string> stack, HashSet<string> onStack, List<List<string>> cycles)
        {
            // Iterative Tarjan to avoid a stack overflow on deep call chains.
            Stack<KeyValuePair<string, int>> work = new Stack<KeyValuePair<string, int>>();
            work.Push(new KeyValuePair<string, int>(key, 0));

            while (work.Count > 0)
            {
                KeyValuePair<string, int> frame = work.Pop();
                string node = frame.Key;
                int childIndex = frame.Value;

                if (childIndex == 0)
                {
                    index[node] = counter;
                    low[node] = counter;
                    counter++;
                    stack.Push(node);
                    onStack.Add(node);
                }

                HashSet<string> targets;
                List<string> children = Edges.TryGetValue(node, out targets)
                    ? new List<string>(targets)
                    : new List<string>();

                bool descended = false;
                for (int i = childIndex; i < children.Count; i++)
                {
                    string child = children[i];
                    if (!Nodes.ContainsKey(child))
                    {
                        continue;
                    }

                    if (!index.ContainsKey(child))
                    {
                        work.Push(new KeyValuePair<string, int>(node, i + 1));
                        work.Push(new KeyValuePair<string, int>(child, 0));
                        descended = true;
                        break;
                    }

                    if (onStack.Contains(child))
                    {
                        low[node] = Math.Min(low[node], index[child]);
                    }
                }

                if (descended)
                {
                    continue;
                }

                if (low[node] == index[node])
                {
                    List<string> component = new List<string>();
                    string member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        component.Add(member);
                    }
                    while (!string.Equals(member, node, StringComparison.Ordinal));

                    if (component.Count > 1)
                    {
                        cycles.Add(component);
                    }
                }

                if (work.Count > 0)
                {
                    KeyValuePair<string, int> parent = work.Peek();
                    string parentNode = parent.Key;
                    low[parentNode] = Math.Min(low[parentNode], low[node]);
                }
            }
        }
    }

    /// <summary>
    /// Builds a call graph for one assembly: nodes are methods defined in the assembly, edges
    /// are resolved calls between them. External calls are counted separately, so the report
    /// stays honest about coverage.
    /// </summary>
    internal static class CallGraph
    {
        public static CallGraphReport Build(AssemblyDefinition assembly)
        {
            CallGraphReport report = new CallGraphReport();

            // 1. Index every method so edges can be resolved by identity.
            Dictionary<string, string> byKey = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    IndexType(type, report, byKey);
                }
            }

            // 2. Walk every body and record edges.
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    CollectEdges(type, report, byKey);
                }
            }

            return report;
        }

        private static void IndexType(TypeDefinition type, CallGraphReport report, Dictionary<string, string> byKey)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                string key = Key(method);
                if (!report.Nodes.ContainsKey(key))
                {
                    report.Nodes[key] = new CallNode
                    {
                        Key = key,
                        Display = Display(method),
                        IsEntryPoint = method.IsConstructor && method.IsStatic && type.Name == "<Module>"
                    };
                    byKey[key] = key;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                IndexType(nested, report, byKey);
            }
        }

        private static void CollectEdges(TypeDefinition type, CallGraphReport report, Dictionary<string, string> byKey)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                string caller = Key(method);
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    MethodReference target = null;
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Call:
                        case Code.Callvirt:
                        case Code.Newobj:
                        case Code.Ldftn:
                        case Code.Ldvirtftn:
                        case Code.Jmp:
                            target = instruction.Operand as MethodReference;
                            break;
                    }

                    if (target == null)
                    {
                        continue;
                    }

                    MethodReference element = target.GetElementMethod();
                    string calleeKey = Key(element);
                    if (byKey.ContainsKey(calleeKey))
                    {
                        report.AddEdge(caller, calleeKey);
                    }
                    else
                    {
                        string display = (element.DeclaringType == null ? "?" : element.DeclaringType.Name) +
                                         "::" + element.Name;
                        if (report.UnresolvedCalls.Count < 400 && !report.UnresolvedCalls.Contains(display))
                        {
                            report.UnresolvedCalls.Add(display);
                        }
                    }
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                CollectEdges(nested, report, byKey);
            }
        }

        internal static string Key(MethodReference method)
        {
            StringBuilder text = new StringBuilder();
            if (method.DeclaringType != null)
            {
                text.Append(method.DeclaringType.FullName);
            }
            text.Append("::").Append(method.Name).Append('(');
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }
                text.Append(AssemblyDiff.NormalizeTypeName(method.Parameters[i].ParameterType.FullName));
            }
            return text.Append(')').ToString();
        }

        private static string Display(MethodDefinition method)
        {
            return method.DeclaringType.FullName + "::" + method.Name +
                   "(" + method.Parameters.Count + ")";
        }

        public static string Format(CallGraphReport report, int top)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("methods       : " + report.Nodes.Count);
            text.AppendLine("internal calls: " + report.InternalCallCount);
            text.AppendLine("external refs : " + report.UnresolvedCalls.Count);

            List<List<string>> cycles = report.FindCycles();
            text.AppendLine("cycles        : " + cycles.Count);
            text.AppendLine();

            text.AppendLine("top callers (fan-out):");
            foreach (CallNode node in report.TopCallers(top))
            {
                if (node.OutDegree == 0)
                {
                    break;
                }
                text.AppendLine(string.Format("  {0,4}  {1}", node.OutDegree, node.Display));
            }
            text.AppendLine();

            text.AppendLine("hottest methods (fan-in):");
            foreach (CallNode node in report.TopCallees(top))
            {
                if (node.InDegree == 0)
                {
                    break;
                }
                text.AppendLine(string.Format("  {0,4}  {1}", node.InDegree, node.Display));
            }
            text.AppendLine();

            List<CallNode> orphans = report.FindOrphans();
            text.AppendLine("orphans (no internal caller): " + orphans.Count);
            for (int i = 0; i < orphans.Count && i < top; i++)
            {
                text.AppendLine("  " + orphans[i].Display);
            }
            if (orphans.Count > top)
            {
                text.AppendLine("  ... " + (orphans.Count - top) + " more");
            }

            if (cycles.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("recursion cycles:");
                for (int i = 0; i < cycles.Count && i < top; i++)
                {
                    text.AppendLine("  " + cycles[i].Count + " methods: " + cycles[i][0] + " ...");
                }
            }

            return text.ToString();
        }
    }
}
