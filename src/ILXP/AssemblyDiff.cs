using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP
{
    internal enum ChangeKind
    {
        Added,
        Removed,
        Changed,
        Unchanged
    }

    /// <summary>One difference between two versions of a member or type.</summary>
    internal sealed class DiffEntry
    {
        public string Kind;
        public string FullName;
        public ChangeKind Change;
        public string Detail;

        public override string ToString()
        {
            string mark;
            switch (Change)
            {
                case ChangeKind.Added: mark = "+"; break;
                case ChangeKind.Removed: mark = "-"; break;
                case ChangeKind.Changed: mark = "~"; break;
                default: mark = "="; break;
            }
            return string.Format("{0} [{1,-8}] {2}{3}", mark, Kind, FullName,
                string.IsNullOrEmpty(Detail) ? string.Empty : "   " + Detail);
        }
    }

    /// <summary>Result of comparing two assemblies.</summary>
    internal sealed class DiffReport
    {
        public DiffReport()
        {
            Entries = new List<DiffEntry>();
        }

        public string LeftName;
        public string RightName;
        public List<DiffEntry> Entries { get; private set; }

        public int Count(ChangeKind kind)
        {
            int count = 0;
            foreach (DiffEntry entry in Entries)
            {
                if (entry.Change == kind)
                {
                    count++;
                }
            }
            return count;
        }

        public bool HasChanges
        {
            get { return Count(ChangeKind.Added) + Count(ChangeKind.Removed) + Count(ChangeKind.Changed) > 0; }
        }
    }

    /// <summary>
    /// Compares two assemblies by metadata identity and IL hash. Useful for "what changed
    /// between 1.2 and 1.3" archaeology, and for verifying that the cleanup passes only
    /// touched what they claim to touch.
    ///
    /// Members are matched by name + signature; a member whose body hash differs is reported
    /// as changed, which keeps the output stable when only instruction offsets shift.
    /// </summary>
    internal static class AssemblyDiff
    {
        public static DiffReport Compare(AssemblyDefinition left, AssemblyDefinition right)
        {
            DiffReport report = new DiffReport
            {
                LeftName = left.Name.FullName,
                RightName = right.Name.FullName
            };

            Dictionary<string, string> leftTypes = IndexTypes(left);
            Dictionary<string, string> rightTypes = IndexTypes(right);

            foreach (KeyValuePair<string, string> pair in leftTypes)
            {
                string rightHash;
                if (!rightTypes.TryGetValue(pair.Key, out rightHash))
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = "type",
                        FullName = pair.Key,
                        Change = ChangeKind.Removed
                    });
                }
                else if (rightHash != pair.Value)
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = "type",
                        FullName = pair.Key,
                        Change = ChangeKind.Changed,
                        Detail = "metadata: " + pair.Value + " -> " + rightHash
                    });
                }
            }

            foreach (KeyValuePair<string, string> pair in rightTypes)
            {
                if (!leftTypes.ContainsKey(pair.Key))
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = "type",
                        FullName = pair.Key,
                        Change = ChangeKind.Added
                    });
                }
            }

            Dictionary<string, string> leftMembers = IndexMembers(left);
            Dictionary<string, string> rightMembers = IndexMembers(right);

            foreach (KeyValuePair<string, string> pair in leftMembers)
            {
                string rightHash;
                if (!rightMembers.TryGetValue(pair.Key, out rightHash))
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = MemberKind(pair.Key),
                        FullName = pair.Key,
                        Change = ChangeKind.Removed
                    });
                }
                else if (rightHash != pair.Value)
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = MemberKind(pair.Key),
                        FullName = pair.Key,
                        Change = ChangeKind.Changed,
                        Detail = DescribeChange(pair.Key, pair.Value, rightHash)
                    });
                }
            }

            foreach (KeyValuePair<string, string> pair in rightMembers)
            {
                if (!leftMembers.ContainsKey(pair.Key))
                {
                    report.Entries.Add(new DiffEntry
                    {
                        Kind = MemberKind(pair.Key),
                        FullName = pair.Key,
                        Change = ChangeKind.Added
                    });
                }
            }

            report.Entries.Sort(delegate(DiffEntry a, DiffEntry b)
            {
                int byChange = a.Change.CompareTo(b.Change);
                return byChange != 0 ? byChange : string.Compare(a.FullName, b.FullName, StringComparison.Ordinal);
            });
            return report;
        }

        /// <summary>
        /// Normalizes a type name for comparison. Scope prefixes ("[mscorlib]"), generic
        /// arguments, array suffixes and generic arity markers are dropped, so the same
        /// semantic type compares equal even when one side was rebuilt from IL text.
        /// </summary>
        internal static string NormalizeTypeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            string result = name.Trim();

            // Assembly scope prefix: "[mscorlib]System.Object".
            if (result.StartsWith("[", StringComparison.Ordinal))
            {
                int scopeEnd = result.IndexOf(']');
                if (scopeEnd > 0)
                {
                    result = result.Substring(scopeEnd + 1).Trim();
                }
            }

            // Generic arguments follow the arity marker ("List`1<...>"); a '<' without an
            // arity marker is part of the name itself ("<PrivateImplementationDetails>").
            int tick = result.IndexOf('`');
            if (tick > 0)
            {
                int angle = result.IndexOf('<', tick);
                result = angle > tick ? result.Substring(0, angle) : result;
            }

            // Arrays, pointers and by-references are decoration, not identity.
            bool trimmed = true;
            while (trimmed)
            {
                trimmed = false;
                string[] suffixes = { "[]", "[,]", "&", "*" };
                foreach (string suffix in suffixes)
                {
                    if (result.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        result = result.Substring(0, result.Length - suffix.Length).Trim();
                        trimmed = true;
                    }
                }
            }

            tick = result.IndexOf('`');
            if (tick > 0)
            {
                result = result.Substring(0, tick);
            }

            // A trailing generic argument list ("Enumerator<T>") is not part of the name.
            if (result.Length > 1 && result[result.Length - 1] == '>')
            {
                int angle = result.LastIndexOf('<');
                if (angle > 0)
                {
                    result = result.Substring(0, angle);
                }
            }

            // Compare by the short name: the IL text writes primitive keywords ("object")
            // while metadata carries the full name ("System.Object"), and rebuilt type
            // references may omit the namespace. For a fidelity metric the short name is the
            // stable identity.
            int separator = result.LastIndexOfAny(new[] { '.', '/' });
            if (separator >= 0 && separator < result.Length - 1)
            {
                result = result.Substring(separator + 1);
            }

            // Primitive keywords ("string") and metadata names ("String") must compare
            // equal; type names that differ only in case do not exist in practice.
            return result.ToLowerInvariant();
        }

        private static string MemberKind(string key)
        {
            int index = key.IndexOf("::", StringComparison.Ordinal);
            string tail = index >= 0 ? key.Substring(index + 2) : key;
            if (tail.StartsWith("field ", StringComparison.Ordinal)) return "field";
            if (tail.StartsWith("property ", StringComparison.Ordinal)) return "property";
            if (tail.StartsWith("event ", StringComparison.Ordinal)) return "event";
            return "method";
        }

        private static string DescribeChange(string key, string left, string right)
        {
            string kind = MemberKind(key);
            if (kind == "field" || kind == "property" || kind == "event")
            {
                return kind + " metadata: " + left + " -> " + right;
            }

            // The hashes are content hashes, so the only useful detail is whether one side
            // was empty (abstract/extern) - otherwise just say the body differs.
            if (left == "empty" && right != "empty")
            {
                return "body added";
            }
            if (left != "empty" && right == "empty")
            {
                return "body removed";
            }
            return "body differs";
        }

        private static Dictionary<string, string> IndexTypes(AssemblyDefinition assembly)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    IndexType(type, map);
                }
            }
            return map;
        }

        private static void IndexType(TypeDefinition type, Dictionary<string, string> map)
        {
            if (type.Name == "<Module>")
            {
                return;
            }

            map[type.FullName] = DescribeType(type);
            foreach (TypeDefinition nested in type.NestedTypes)
            {
                IndexType(nested, map);
            }
        }

        private static string DescribeType(TypeDefinition type)
        {
            StringBuilder text = new StringBuilder();
            text.Append(type.Attributes).Append('|');
            text.Append(type.BaseType == null ? string.Empty : NormalizeTypeName(type.BaseType.FullName)).Append('|');
            foreach (TypeReference @interface in type.Interfaces)
            {
                text.Append(NormalizeTypeName(@interface.FullName)).Append(',');
            }
            return text.ToString();
        }

        private static Dictionary<string, string> IndexMembers(AssemblyDefinition assembly)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    IndexMembers(type, map);
                }
            }
            return map;
        }

        private static void IndexMembers(TypeDefinition type, Dictionary<string, string> map)
        {
            if (type.Name == "<Module>")
            {
                return;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                string key = type.FullName + "::" + method.Name + "(" + ParameterList(method) + ")";
                map[key] = method.HasBody ? HashBody(method.Body) : "empty";
            }

            foreach (FieldDefinition field in type.Fields)
            {
                string key = type.FullName + "::field " + field.Name;
                map[key] = NormalizeTypeName(field.FieldType.FullName) + "|" + field.Attributes;
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                string key = type.FullName + "::property " + property.Name;
                map[key] = NormalizeTypeName(property.PropertyType.FullName);
            }

            foreach (EventDefinition @event in type.Events)
            {
                string key = type.FullName + "::event " + @event.Name;
                map[key] = NormalizeTypeName(@event.EventType.FullName);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                IndexMembers(nested, map);
            }
        }

        private static string ParameterList(MethodDefinition method)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }
                text.Append(NormalizeTypeName(method.Parameters[i].ParameterType.FullName));            }
            return text.ToString();
        }

        /// <summary>
        /// Hashes the instruction stream by opcode and operand identity, ignoring offsets and
        /// branch targets (which shift when a single instruction is inserted).
        /// </summary>
        private static string HashBody(MethodBody body)
        {
            return Hash(BodyText(body));
        }

        /// <summary>
        /// The exact text a body is hashed from; useful when debugging a diff. Exception
        /// handlers are part of the body: without them a method that catches exceptions is
        /// not equivalent, so their shape is included in the hash.
        /// </summary>
        internal static string BodyText(MethodBody body)
        {
            StringBuilder text = new StringBuilder();
            foreach (Instruction instruction in body.Instructions)
            {
                text.Append(instruction.OpCode.Code);
                if (instruction.Operand != null)
                {
                    text.Append(':').Append(DescribeOperand(instruction.Operand));
                }
                text.Append(';');
            }

            if (body.HasExceptionHandlers)
            {
                Dictionary<Instruction, int> indices = new Dictionary<Instruction, int>(body.Instructions.Count);
                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    indices[body.Instructions[i]] = i;
                }

                text.Append("|handlers:");

                // Handlers that guard the same region form a group whose order is semantic
                // (first match wins). Nested groups are reordered by the IL text form, so
                // groups are compared by their protected region instead of by table order.
                List<KeyValuePair<long, List<ExceptionHandler>>> groups =
                    new List<KeyValuePair<long, List<ExceptionHandler>>>();
                foreach (ExceptionHandler handler in body.ExceptionHandlers)
                {
                    int tryStart = Index(indices, handler.TryStart);
                    int tryEnd = Index(indices, handler.TryEnd);
                    long key = ((long)tryStart << 32) | (uint)tryEnd;

                    List<ExceptionHandler> group = null;
                    foreach (KeyValuePair<long, List<ExceptionHandler>> candidate in groups)
                    {
                        if (candidate.Key == key)
                        {
                            group = candidate.Value;
                            break;
                        }
                    }
                    if (group == null)
                    {
                        group = new List<ExceptionHandler>();
                        groups.Add(new KeyValuePair<long, List<ExceptionHandler>>(key, group));
                    }
                    group.Add(handler);
                }

                groups.Sort(delegate(KeyValuePair<long, List<ExceptionHandler>> a,
                    KeyValuePair<long, List<ExceptionHandler>> b) { return a.Key.CompareTo(b.Key); });

                foreach (KeyValuePair<long, List<ExceptionHandler>> pair in groups)
                {
                    text.Append('{');
                    foreach (ExceptionHandler handler in pair.Value)
                    {
                        text.Append(handler.HandlerType).Append('@');
                        text.Append(Index(indices, handler.TryStart)).Append('-')
                            .Append(Index(indices, handler.TryEnd)).Append('>')
                            .Append(Index(indices, handler.HandlerStart)).Append('-')
                            .Append(Index(indices, handler.HandlerEnd));
                        if (handler.CatchType != null)
                        {
                            text.Append('/').Append(NormalizeTypeName(handler.CatchType.FullName));
                        }
                        if (handler.FilterStart != null)
                        {
                            text.Append("/filter").Append(Index(indices, handler.FilterStart));
                        }
                        text.Append(';');
                    }
                    text.Append('}');
                }
            }

            return text.ToString();
        }

        private static int Index(Dictionary<Instruction, int> indices, Instruction instruction)
        {
            int index;
            return instruction != null && indices.TryGetValue(instruction, out index) ? index : -1;
        }

        private static string DescribeOperand(object operand)
        {
            if (operand == null)
            {
                return "null";
            }
            if (operand is Instruction)
            {
                return "target";
            }
            if (operand is Instruction[])
            {
                return "targets";
            }
            if (operand is MethodReference)
            {
                MethodReference method = (MethodReference)operand;
                StringBuilder text = new StringBuilder();
                text.Append(NormalizeTypeName(method.DeclaringType == null ? string.Empty : method.DeclaringType.FullName));
                text.Append("::").Append(method.Name).Append('(');
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(',');
                    }
                    text.Append(NormalizeTypeName(method.Parameters[i].ParameterType.FullName));
                }
                text.Append(')');
                return text.ToString();
            }
            if (operand is FieldReference)
            {
                FieldReference field = (FieldReference)operand;
                return NormalizeTypeName(field.DeclaringType == null ? string.Empty : field.DeclaringType.FullName) +
                       "::" + field.Name;
            }
            if (operand is TypeReference)
            {
                return NormalizeTypeName(((TypeReference)operand).FullName);
            }
            if (operand is VariableDefinition)
            {
                return "var" + ((VariableDefinition)operand).Index.ToString(CultureInfo.InvariantCulture);
            }
            if (operand is ParameterDefinition)
            {
                return "arg" + ((ParameterDefinition)operand).Index.ToString(CultureInfo.InvariantCulture);
            }
            return Convert.ToString(operand, CultureInfo.InvariantCulture);
        }

        /// <summary>FNV-1a, stable across runs and platforms.</summary>
        internal static string Hash(string text)
        {
            ulong hash = 14695981039346656037UL;
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    hash = (hash ^ text[i]) * 1099511628211UL;
                }
            }
            return hash.ToString("x16");
        }

        public static string Format(DiffReport report)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("left  : " + report.LeftName);
            text.AppendLine("right : " + report.RightName);
            text.AppendLine(string.Format("added {0}, removed {1}, changed {2}",
                report.Count(ChangeKind.Added), report.Count(ChangeKind.Removed), report.Count(ChangeKind.Changed)));
            text.AppendLine();

            foreach (DiffEntry entry in report.Entries)
            {
                text.AppendLine(entry.ToString());
            }
            return text.ToString();
        }
    }
}
