using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILXP.UI;

namespace ILXP
{
    /// <summary>
    /// Builds the Quick Open index (every type and member of an assembly) and performs the
    /// fuzzy matching. Kept separate from the form so that the command line and the tests
    /// can use exactly the same logic.
    /// </summary>
    internal static class QuickOpenIndex
    {
        public static List<QuickOpenForm.Entry> Build(AssemblyDefinition assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            List<QuickOpenForm.Entry> entries = new List<QuickOpenForm.Entry>();
            string assemblyName = assembly.Name.Name;
            string assemblyKey = assembly.Name.FullName;

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }

                    entries.Add(new QuickOpenForm.Entry
                    {
                        Display = assemblyName + "  " + type.FullName,
                        Search = type.FullName + " " + assemblyName,
                        Kind = TypeKind(type),
                        Path = assemblyKey + "//N" + type.Namespace + "//" + type.Name
                    });

                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (LanguageWriter.IsCompilerGeneratedName(method.Name))
                        {
                            continue;
                        }

                        string signature = LanguageWriter.GetMethodSignatureWithReturn(method);
                        entries.Add(new QuickOpenForm.Entry
                        {
                            Display = assemblyName + "  " + type.FullName + " :: " + signature,
                            Search = type.FullName + "." + method.Name + " " + signature + " " + assemblyName,
                            Kind = "method",
                            Path = assemblyKey + "//N" + type.Namespace + "//" + type.Name + "//M" +
                                   LanguageWriter.GetMethodSignature(method)
                        });
                    }

                    AddMembers(entries, assemblyName, assemblyKey, type, type.Fields, "field", "F");
                    AddMembers(entries, assemblyName, assemblyKey, type, type.Properties, "property", "P");
                    AddMembers(entries, assemblyName, assemblyKey, type, type.Events, "event", "E");
                }
            }

            return entries;
        }

        private static void AddMembers<T>(List<QuickOpenForm.Entry> entries, string assemblyName,
            string assemblyKey, TypeDefinition type, IEnumerable<T> members, string kind, string prefix)
            where T : MemberReference
        {
            foreach (T member in members)
            {
                if (LanguageWriter.IsCompilerGeneratedName(member.Name))
                {
                    continue;
                }

                entries.Add(new QuickOpenForm.Entry
                {
                    Display = assemblyName + "  " + type.FullName + " :: " + member.Name,
                    Search = type.FullName + "." + member.Name + " " + assemblyName,
                    Kind = kind,
                    Path = assemblyKey + "//N" + type.Namespace + "//" + type.Name + "//" + prefix + member.Name
                });
            }
        }

        private static string TypeKind(TypeDefinition type)
        {
            if (type.IsEnum)
            {
                return "enum";
            }
            if (type.IsInterface)
            {
                return "interface";
            }
            if (type.IsValueType)
            {
                return "struct";
            }
            if (type.BaseType != null && type.BaseType.FullName.EndsWith("Delegate", StringComparison.Ordinal))
            {
                return "delegate";
            }
            return "class";
        }

        /// <summary>Returns the best matches for a query, most relevant first.</summary>
        public static List<QuickOpenForm.Entry> Search(IEnumerable<QuickOpenForm.Entry> entries, string query, int limit)
        {
            List<KeyValuePair<int, QuickOpenForm.Entry>> scored = new List<KeyValuePair<int, QuickOpenForm.Entry>>();
            foreach (QuickOpenForm.Entry entry in entries)
            {
                int score = QuickOpenForm.FuzzyMatch(entry.Search, query);
                if (score >= 0)
                {
                    scored.Add(new KeyValuePair<int, QuickOpenForm.Entry>(score, entry));
                }
            }

            scored.Sort(delegate(KeyValuePair<int, QuickOpenForm.Entry> a, KeyValuePair<int, QuickOpenForm.Entry> b)
            {
                int byScore = b.Key.CompareTo(a.Key);
                return byScore != 0 ? byScore : string.Compare(a.Value.Display, b.Value.Display, StringComparison.Ordinal);
            });

            List<QuickOpenForm.Entry> results = new List<QuickOpenForm.Entry>();
            for (int i = 0; i < scored.Count && i < limit; i++)
            {
                results.Add(scored[i].Value);
            }
            return results;
        }
    }
}
