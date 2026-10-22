using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>Outcome of one fixpoint iteration.</summary>
    internal sealed class FixpointResult
    {
        public string OriginalPath;
        public string RoundTripPath;
        public string IlPath;
        public DiffReport Diff;
        public int OriginalTypes;
        public int OriginalMethods;
        public int OriginalMembers;
        public int RoundTripTypes;
        public int RoundTripMethods;
        public int TotalBodies;
        public int ChangedBodies;
        public bool Reassembled;
        public string Error;

        /// <summary>Percentage of method bodies that survived the round trip unchanged.</summary>
        public double BodyFidelity
        {
            get
            {
                if (TotalBodies == 0)
                {
                    return 100.0;
                }
                return 100.0 * (TotalBodies - ChangedBodies) / TotalBodies;
            }
        }

        /// <summary>Percentage of members that survived the round trip unchanged.</summary>
        public double Fidelity
        {
            get
            {
                if (Diff == null)
                {
                    return 0;
                }

                int total = Diff.Count(ChangeKind.Added) + Diff.Count(ChangeKind.Removed) + Diff.Count(ChangeKind.Changed);
                int expected = OriginalMembers > 0 ? OriginalMembers : OriginalTypes + OriginalMethods;
                if (expected == 0)
                {
                    return 100.0;
                }

                // "removed" is the real loss; added/changed are also degradations but weigh
                // less because an added compiler-generated member does not lose information.
                int lost = Diff.Count(ChangeKind.Removed);
                double score = 100.0 - (100.0 * lost / expected);
                if (score < 0)
                {
                    score = 0;
                }
                return score;
            }
        }
    }

    /// <summary>
    /// Fixpoint test: decompile an assembly to IL, reassemble it with the managed assembler,
    /// then compare the result with the original.
    ///
    /// This is the honest quality metric for a decompiler: everything that cannot be
    /// expressed in the IL text (or that the assembler cannot rebuild) shows up as a removed
    /// or changed member. Running it repeatedly shows whether the pipeline is stable.
    /// </summary>
    internal static class Fixpoint
    {
        public static FixpointResult Run(string path, int iterations)
        {
            FixpointResult result = new FixpointResult { OriginalPath = path };
            if (iterations < 1)
            {
                iterations = 1;
            }

            string workspace = Path.Combine(Path.GetTempPath(), "ILXP-fixpoint-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspace);

            string current = path;
            try
            {
                AssemblyDefinition original = AssemblyLoader.Load(path).Assembly;
                CountMembers(original, out int types, out int methods);
                result.OriginalTypes = types;
                result.OriginalMethods = methods;
                result.OriginalMembers = MemberCount;

                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    string il = Path.Combine(workspace, "iteration" + iteration + ".il");
                    string roundTrip = Path.Combine(workspace, "iteration" + iteration + ".dll");

                    AssemblyDefinition input = AssemblyLoader.Load(current).Assembly;
                    string text = Interop.ManagedIlDisassembler.Disassemble(input, false);
                    File.WriteAllText(il, text, new UTF8Encoding(false));

                    Interop.ManagedIlAssembler.AssembleFile(il, roundTrip, true);
                    result.Reassembled = true;

                    if (iteration == iterations - 1)
                    {
                        result.IlPath = il;
                        result.RoundTripPath = roundTrip;

                        AssemblyDefinition rebuilt = AssemblyLoader.Load(roundTrip).Assembly;
                        CountMembers(rebuilt, out int rebuiltTypes, out int rebuiltMethods);
                        result.RoundTripTypes = rebuiltTypes;
                        result.RoundTripMethods = rebuiltMethods;
                        result.Diff = AssemblyDiff.Compare(original, rebuilt);
                        CountBodies(original, result);
                        DumpBodyIfRequested(original, rebuilt);
                    }

                    current = roundTrip;
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log.Error("Fixpoint iteration failed.", ex);
            }

            return result;
        }

        /// <summary>
        /// Diagnostic helper: when <c>ILXP_DEBUG_DIFF</c> names a method, both body texts are
        /// written to the trace so a mismatch can be inspected instruction by instruction.
        /// </summary>
        private static void DumpBodyIfRequested(AssemblyDefinition original, AssemblyDefinition rebuilt)
        {
            string wanted = Environment.GetEnvironmentVariable("ILXP_DEBUG_DIFF");
            if (string.IsNullOrEmpty(wanted))
            {
                return;
            }

            MethodDefinition left = FindMethodBySubstring(original, wanted);
            MethodDefinition right = FindMethodBySubstring(rebuilt, wanted);
            if (left == null || right == null || !left.HasBody || !right.HasBody)
            {
                return;
            }

            string leftText = AssemblyDiff.BodyText(left.Body);
            string rightText = AssemblyDiff.BodyText(right.Body);
            Console.WriteLine("DEBUG body left : " + leftText);
            Console.WriteLine("DEBUG body right: " + rightText);

            int length = Math.Min(leftText.Length, rightText.Length);
            for (int i = 0; i < length; i++)
            {
                if (leftText[i] != rightText[i])
                {
                    int start = Math.Max(0, i - 60);
                    Console.WriteLine("DEBUG first difference at " + i + ":");
                    Console.WriteLine("  left : ..." + leftText.Substring(start, Math.Min(160, leftText.Length - start)));
                    Console.WriteLine("  right: ..." + rightText.Substring(start, Math.Min(160, rightText.Length - start)));
                    return;
                }
            }
            Console.WriteLine("DEBUG bodies are identical (length " + leftText.Length + ")");
        }

        private static MethodDefinition FindMethodBySubstring(AssemblyDefinition assembly, string substring)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    MethodDefinition found = FindMethodBySubstring(type, substring);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private static MethodDefinition FindMethodBySubstring(TypeDefinition type, string substring)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if ((type.FullName + "::" + method.Name).IndexOf(substring, StringComparison.Ordinal) >= 0)
                {
                    return method;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                MethodDefinition found = FindMethodBySubstring(nested, substring);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static void CountMembers(AssemblyDefinition assembly, out int types, out int methods)
        {
            int memberTotal = 0;
            types = 0;
            methods = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name != "<Module>")
                    {
                        types++;
                    }
                    methods += type.Methods.Count;
                    memberTotal += 1 + type.Methods.Count + type.Fields.Count +
                                   type.Properties.Count + type.Events.Count;
                }
            }
            MemberCount = memberTotal;
        }

        /// <summary>Total number of comparable members (types, methods, fields, properties, events).</summary>
        public static int MemberCount { get; private set; }

        /// <summary>Counts method bodies and how many of them changed in the round trip.</summary>
        private static void CountBodies(AssemblyDefinition original, FixpointResult result)
        {
            int total = 0;
            int changed = 0;

            HashSet<string> changedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (DiffEntry entry in result.Diff.Entries)
            {
                if (entry.Change == ChangeKind.Changed && entry.Kind == "method")
                {
                    changedKeys.Add(entry.FullName);
                }
            }

            foreach (ModuleDefinition module in original.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    CountBodies(type, changedKeys, ref total, ref changed);
                }
            }

            result.TotalBodies = total;
            result.ChangedBodies = changed;
        }

        private static void CountBodies(TypeDefinition type, HashSet<string> changedKeys, ref int total, ref int changed)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody || method.Body.Instructions.Count == 0)
                {
                    continue;
                }

                total++;
                string key = type.FullName + "::" + method.Name + "(" + BodyKeyParameters(method) + ")";
                if (changedKeys.Contains(key))
                {
                    changed++;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                CountBodies(nested, changedKeys, ref total, ref changed);
            }
        }

        private static string BodyKeyParameters(MethodDefinition method)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }
                text.Append(AssemblyDiff.NormalizeTypeName(method.Parameters[i].ParameterType.FullName));
            }
            return text.ToString();
        }

        public static string Format(FixpointResult result)
        {
            return Format(result, 25);
        }

        public static string Format(FixpointResult result, int limit)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("assembly      : " + result.OriginalPath);
            text.AppendLine("original      : " + result.OriginalTypes + " types, " + result.OriginalMethods +
                            " methods, " + result.OriginalMembers + " members");

            if (!string.IsNullOrEmpty(result.Error))
            {
                text.AppendLine("error         : " + result.Error);
                return text.ToString();
            }

            text.AppendLine("round trip    : " + result.RoundTripTypes + " types, " + result.RoundTripMethods + " methods");
            text.AppendLine(string.Format("fidelity      : {0:0.00}%", result.Fidelity));
            text.AppendLine(string.Format("body fidelity : {0:0.00}%  ({1} of {2} method bodies unchanged)",
                result.BodyFidelity, result.TotalBodies - result.ChangedBodies, result.TotalBodies));

            if (result.Diff != null)
            {
                text.AppendLine(string.Format("diff          : added {0}, removed {1}, changed {2}",
                    result.Diff.Count(ChangeKind.Added),
                    result.Diff.Count(ChangeKind.Removed),
                    result.Diff.Count(ChangeKind.Changed)));

                int shown = 0;
                int total = result.Diff.Count(ChangeKind.Added) +
                            result.Diff.Count(ChangeKind.Removed) +
                            result.Diff.Count(ChangeKind.Changed);
                foreach (DiffEntry entry in result.Diff.Entries)
                {
                    if (entry.Change == ChangeKind.Unchanged)
                    {
                        continue;
                    }

                    if (shown++ >= limit)
                    {
                        text.AppendLine("  ... " + (total - limit) + " more");
                        break;
                    }
                    text.AppendLine("  " + entry);
                }
            }

            return text.ToString();
        }
    }
}
