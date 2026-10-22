using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Non-interactive command line mode. It is intentionally small but complete
    /// enough to batch-process assemblies (useful for testing and for damaged files
    /// that the GUI cannot easily inspect).
    /// </summary>
    internal static class CommandLine
    {
        public static int Run(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine();
                PrintUsage();
                return 2;
            }

            if (options.Help || options.Command == null)
            {
                PrintUsage();
                return options.Help ? 0 : 2;
            }

            try
            {
                switch (options.Command)
                {
                    case "version":
                        Console.WriteLine(AppInfo.Title);
                        return 0;
                    case "list":
                        return ListTypes(options.Input);
                    case "decompile":
                        return Decompile(options);
                    case "resources":
                        return ExtractResources(options.Input, options.Output);
                    default:
                        Console.Error.WriteLine("error: unknown command '" + options.Command + "'");
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Command line operation failed.", ex);
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        private static int ListTypes(string path)
        {
            AssemblyDefinition assembly = ReadAssembly(path);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                Console.WriteLine("module " + module.Name);
                foreach (TypeDefinition type in module.Types)
                {
                    Console.WriteLine("  " + type.FullName);
                }
            }
            return 0;
        }

        private static int Decompile(Options options)
        {
            LanguageWriter writer;
            switch (options.Language)
            {
                case "il":
                    writer = new IlWriter();
                    break;
                case "cs":
                case "csharp":
                    writer = new CSharpWriter();
                    break;
                case "vb":
                    writer = new VbWriter();
                    break;
                default:
                    Console.Error.WriteLine("error: unsupported language '" + options.Language + "'");
                    return 2;
            }

            AssemblyDefinition assembly = ReadAssembly(options.Input);
            {
                string outputDirectory = options.Output;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.Combine(
                        Path.GetDirectoryName(Path.GetFullPath(options.Input)) ?? ".",
                        Path.GetFileNameWithoutExtension(options.Input) + "." + options.Language);
                }
                Directory.CreateDirectory(outputDirectory);

                StringBuilder manifest = new StringBuilder();
                manifest.AppendLine("assembly: " + assembly.Name.FullName);
                manifest.AppendLine("file: " + Path.GetFullPath(options.Input));
                manifest.AppendLine("language: " + writer);
                manifest.AppendLine();

                int count = 0;
                foreach (ModuleDefinition module in assembly.Modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        string text = writer.StripMarkup(writer.WriteType(type));
                        string fileName = SanitizeFileName(type.FullName) + "." + options.Language;
                        File.WriteAllText(Path.Combine(outputDirectory, fileName), text, Encoding.UTF8);
                        manifest.AppendLine(fileName);
                        count++;
                    }
                }

                File.WriteAllText(Path.Combine(outputDirectory, "manifest.txt"), manifest.ToString(), Encoding.UTF8);
                Console.WriteLine("decompiled " + count.ToString(CultureInfo.InvariantCulture) + " types into " + outputDirectory);
            }
            return 0;
        }

        private static int ExtractResources(string path, string output)
        {
            if (string.IsNullOrEmpty(output))
            {
                Console.Error.WriteLine("error: --output is required for the resources command");
                return 2;
            }
            Directory.CreateDirectory(output);

            AssemblyDefinition assembly = ReadAssembly(path);
            {
                int count = 0;
                foreach (Resource resource in assembly.MainModule.Resources)
                {
                    EmbeddedResource embedded = resource as EmbeddedResource;
                    if (embedded == null)
                    {
                        continue;
                    }

                    byte[] data = embedded.GetResourceData();
                    string target = Path.Combine(output, SanitizeFileName(resource.Name));
                    File.WriteAllBytes(target, data);
                    Console.WriteLine(resource.Name + " -> " + target);
                    count++;
                }
                Console.WriteLine("extracted " + count.ToString(CultureInfo.InvariantCulture) + " resources");
            }
            return 0;
        }

        private static AssemblyDefinition ReadAssembly(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("--input is required");
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("file not found", path);
            }

            ReaderParameters parameters = new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false
            };

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path, parameters);

            // Symbols are optional: a missing, portable or damaged PDB must never
            // prevent the assembly itself from being read.
            try
            {
                string pdb = Path.ChangeExtension(path, ".pdb");
                if (File.Exists(pdb))
                {
                    Mono.Cecil.Pdb.PdbReaderProvider provider = new Mono.Cecil.Pdb.PdbReaderProvider();
                    assembly.MainModule.ReadSymbols(provider.GetSymbolReader(assembly.MainModule, pdb));
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to read symbols for '" + path + "'.", ex);
            }

            return assembly;
        }

        private static string SanitizeFileName(string name)
        {
            StringBuilder text = new StringBuilder(name.Length);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                text.Append(Array.IndexOf(invalid, c) >= 0 || c == '`' || c == '/' ? '_' : c);
            }
            string result = text.ToString();
            return result.Length > 180 ? result.Substring(0, 180) : result;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(AppInfo.Title);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  ILXP                                     start the GUI");
            Console.WriteLine("  ILXP --version                           print the version");
            Console.WriteLine("  ILXP --list <assembly>                   list types in an assembly");
            Console.WriteLine("  ILXP --decompile <assembly> [options]    write generated source to disk");
            Console.WriteLine("  ILXP --resources <assembly> --output <dir>");
            Console.WriteLine();
            Console.WriteLine("Decompile options:");
            Console.WriteLine("  --language <il|cs|vb>   output language (default: il)");
            Console.WriteLine("  --output <directory>    destination directory");
            Console.WriteLine();
            Console.WriteLine("Run without arguments to start the graphical interface.");
        }

        private sealed class Options
        {
            public string Command { get; private set; }
            public string Input { get; private set; }
            public string Output { get; private set; }
            public string Language { get; private set; }
            public bool Help { get; private set; }

            public static Options Parse(string[] args)
            {
                Options options = new Options { Language = "il" };

                for (int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];
                    switch (argument)
                    {
                        case "-h":
                        case "--help":
                        case "/?":
                            options.Help = true;
                            break;
                        case "--version":
                            options.Command = "version";
                            break;
                        case "--list":
                            options.Command = "list";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--decompile":
                            options.Command = "decompile";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--resources":
                            options.Command = "resources";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--language":
                            options.Language = Next(args, ref i, argument).ToLowerInvariant();
                            break;
                        case "--output":
                        case "-o":
                            options.Output = Next(args, ref i, argument);
                            break;
                        default:
                            if (argument.StartsWith("-", StringComparison.Ordinal))
                            {
                                throw new ArgumentException("unknown option '" + argument + "'");
                            }
                            if (options.Input == null)
                            {
                                options.Input = argument;
                                if (options.Command == null)
                                {
                                    options.Command = "decompile";
                                }
                            }
                            else
                            {
                                throw new ArgumentException("unexpected argument '" + argument + "'");
                            }
                            break;
                    }
                }

                return options;
            }

            private static string Next(string[] args, ref int index, string option)
            {
                if (index + 1 >= args.Length)
                {
                    throw new ArgumentException("missing value for " + option);
                }
                index++;
                return args[index];
            }
        }
    }
}
