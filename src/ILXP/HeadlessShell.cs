using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Headless command interpreter. It keeps an assembly loaded between commands, which
    /// makes scripted testing (and batch analysis) fast: no process start, no repeated
    /// metadata parsing.
    ///
    /// Commands (one per line, '#' starts a comment):
    ///   open &lt;path&gt;            load an assembly (replaces the previous one)
    ///   list                     list types
    ///   type &lt;name&gt;             dump a type with the current writer
    ///   method &lt;type&gt; &lt;name&gt;    dump a method body
    ///   accuracy [il|cs|vb]      structural completeness report
    ///   clean &lt;out&gt;             run the cleanup passes and save
    ///   ildasm &lt;out&gt;            write IL to a file
    ///   resources &lt;dir&gt;         extract embedded resources
    ///   lang &lt;il|cs|vb&gt;         select the writer
    ///   dialect &lt;id&gt;            select the output dialect
    ///   info                     print assembly information
    ///   help                     this text
    ///   quit                     exit
    /// </summary>
    internal static class HeadlessShell
    {
        public static int Run(TextReader input, TextWriter output)
        {
            if (input == null)
            {
                input = Console.In;
            }
            if (output == null)
            {
                output = Console.Out;
            }

            AssemblyDefinition assembly = null;
            LanguageWriter writer = new IlWriter();

            output.WriteLine("ILXP headless shell " + AppInfo.VersionText + " ('help' for commands)");

            while (true)
            {
                output.Write("ilxp> ");
                output.Flush();
                string line = input.ReadLine();
                if (line == null)
                {
                    break;
                }

                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                List<string> parts = Tokenize(line);
                string command = parts[0].ToLowerInvariant();
                try
                {
                    switch (command)
                    {
                        case "quit":
                        case "exit":
                            return 0;

                        case "help":
                            PrintHelp(output);
                            break;

                        case "open":
                            if (parts.Count < 2)
                            {
                                output.WriteLine("error: open <path>");
                                break;
                            }
                            assembly = AssemblyLoader.Load(parts[1]).Assembly;
                            output.WriteLine("loaded " + assembly.Name.FullName + " (" + assembly.MainModule.Types.Count + " types)");
                            break;

                        case "list":
                            RequireAssembly(assembly, output);
                            if (assembly == null)
                            {
                                break;
                            }
                            foreach (ModuleDefinition module in assembly.Modules)
                            {
                                output.WriteLine("module " + module.Name);
                                foreach (TypeDefinition type in module.Types)
                                {
                                    output.WriteLine("  " + type.FullName);
                                }
                            }
                            break;

                        case "type":
                            if (assembly == null || parts.Count < 2)
                            {
                                output.WriteLine("error: type <name>");
                                break;
                            }
                            DumpType(assembly, parts[1], writer, output);
                            break;

                        case "method":
                            if (assembly == null || parts.Count < 3)
                            {
                                output.WriteLine("error: method <type> <name>");
                                break;
                            }
                            DumpMethod(assembly, parts[1], parts[2], writer, output);
                            break;

                        case "accuracy":
                        {
                            if (assembly == null)
                            {
                                output.WriteLine("error: open an assembly first");
                                break;
                            }
                            string language = parts.Count > 1 ? parts[1].ToLowerInvariant() : "il";
                            LanguageWriter measure = CreateWriter(language);
                            AccuracyReport report = Accuracy.Measure(assembly, measure);
                            output.WriteLine(report.ToString());
                            break;
                        }

                        case "clean":
                        {
                            if (assembly == null || parts.Count < 2)
                            {
                                output.WriteLine("error: clean <output>");
                                break;
                            }
                            Deobfuscation.DeobfuscationReport report =
                                Deobfuscation.DeobfuscationPipeline.Run(assembly, Deobfuscation.DeobfuscationPipeline.CreateDefault());
                            assembly.Write(parts[1], new WriterParameters { WriteSymbols = false });
                            output.WriteLine("cleaned assembly written to " + parts[1]);
                            foreach (KeyValuePair<string, int> pair in report.Results)
                            {
                                output.WriteLine("  " + pair.Key + ": " + pair.Value);
                            }
                            break;
                        }

                        case "ildasm":
                        {
                            if (assembly == null || parts.Count < 2)
                            {
                                output.WriteLine("error: ildasm <output>");
                                break;
                            }
                            File.WriteAllText(parts[1], Interop.ManagedIlDisassembler.Disassemble(assembly, false), new UTF8Encoding(false));
                            output.WriteLine("IL written to " + parts[1]);
                            break;
                        }

                        case "resources":
                        {
                            if (assembly == null || parts.Count < 2)
                            {
                                output.WriteLine("error: resources <directory>");
                                break;
                            }
                            Directory.CreateDirectory(parts[1]);
                            int count = 0;
                            foreach (Resource resource in assembly.MainModule.Resources)
                            {
                                EmbeddedResource embedded = resource as EmbeddedResource;
                                if (embedded == null)
                                {
                                    continue;
                                }
                                File.WriteAllBytes(Path.Combine(parts[1], Decompiler.SanitizeFileName(resource.Name)), embedded.GetResourceData());
                                count++;
                            }
                            output.WriteLine("extracted " + count + " resources to " + parts[1]);
                            break;
                        }

                        case "lang":
                            if (parts.Count < 2)
                            {
                                output.WriteLine("error: lang <il|cs|vb>");
                                break;
                            }
                            writer = CreateWriter(parts[1].ToLowerInvariant());
                            output.WriteLine("writer: " + writer);
                            break;

                        case "dialect":
                            if (parts.Count < 2)
                            {
                                output.WriteLine("error: dialect <id>");
                                break;
                            }
                            OutputDialect dialect = OutputDialect.FromId(parts[1]);
                            if (dialect == null)
                            {
                                output.WriteLine("error: unknown dialect '" + parts[1] + "'");
                                break;
                            }
                            writer.Dialect = dialect;
                            output.WriteLine("dialect: " + dialect);
                            break;

                        case "info":
                            if (assembly == null)
                            {
                                output.WriteLine("error: open an assembly first");
                                break;
                            }
                            RuntimeProfile profile = RuntimeProfile.Detect(assembly);
                            output.WriteLine("assembly : " + assembly.Name.FullName);
                            output.WriteLine("runtime  : " + profile);
                            output.WriteLine("modules  : " + assembly.Modules.Count);
                            output.WriteLine("types    : " + assembly.MainModule.Types.Count);
                            output.WriteLine("resources: " + assembly.MainModule.Resources.Count);
                            break;

                        default:
                            output.WriteLine("unknown command '" + command + "' (try 'help')");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Headless command '" + line + "' failed.", ex);
                    output.WriteLine("error: " + ex.Message);
                }
            }

            return 0;
        }

        private static void RequireAssembly(AssemblyDefinition assembly, TextWriter output)
        {
            if (assembly == null)
            {
                output.WriteLine("error: open an assembly first");
            }
        }

        private static LanguageWriter CreateWriter(string language)
        {
            switch (language)
            {
                case "cs":
                case "csharp":
                    return new CSharpWriter();
                case "vb":
                    return new VbWriter();
                default:
                    return new IlWriter();
            }
        }

        private static void DumpType(AssemblyDefinition assembly, string name, LanguageWriter writer, TextWriter output)
        {
            TypeDefinition type = FindType(assembly, name);
            if (type == null)
            {
                output.WriteLine("error: type not found: " + name);
                return;
            }
            output.WriteLine(writer.StripMarkup(writer.WriteType(type)));
        }

        private static void DumpMethod(AssemblyDefinition assembly, string typeName, string methodName, LanguageWriter writer, TextWriter output)
        {
            TypeDefinition type = FindType(assembly, typeName);
            if (type == null)
            {
                output.WriteLine("error: type not found: " + typeName);
                return;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    output.WriteLine(writer.StripMarkup(writer.WriteMethod(method)));
                    return;
                }
            }
            output.WriteLine("error: method not found: " + methodName);
        }

        private static TypeDefinition FindType(AssemblyDefinition assembly, string name)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (string.Equals(type.FullName, name, StringComparison.Ordinal) ||
                        string.Equals(type.Name, name, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private static List<string> Tokenize(string line)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (!quoted && char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }
            return parts;
        }

        private static void PrintHelp(TextWriter output)
        {
            output.WriteLine("commands:");
            output.WriteLine("  open <path>            load an assembly");
            output.WriteLine("  list                   list types");
            output.WriteLine("  type <name>            dump a type");
            output.WriteLine("  method <type> <name>   dump a method");
            output.WriteLine("  accuracy [il|cs|vb]    structural completeness");
            output.WriteLine("  clean <out>            run cleanup passes and save");
            output.WriteLine("  ildasm <out>           write IL to a file");
            output.WriteLine("  resources <dir>        extract embedded resources");
            output.WriteLine("  lang <il|cs|vb>        select the writer");
            output.WriteLine("  dialect <id>           select the output dialect");
            output.WriteLine("  info                   assembly information");
            output.WriteLine("  quit                   exit");
        }
    }
}
