using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ILXP.Deobfuscation;
using ILXP.Interop;
using ILXP.Music;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
                    case "ildasm":
                        return DisassembleIl(options);
                    case "ilasm":
                        return AssembleIl(options);
                    case "disasm-x86":
                        return DisassembleX86(options);
                    case "clean":
                        return Clean(options);
                    case "tree":
                        return DumpTree(options);
                    case "accuracy":
                        return MeasureAccuracy(options);
                    case "verify":
                        return Verify(options);
                    case "headless":
                        return HeadlessShell.Run(Console.In, Console.Out);
                    case "music":
                        return MusicCommand(options);
                    case "screenshot":
                        return Screenshot(options);
                    case "render-water":
                        return RenderWater(options);
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

            OutputDialect dialect = OutputDialect.FromId(options.Dialect);
            if (dialect != null)
            {
                writer.Dialect = dialect;
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
                int bodyFailures = 0;
                foreach (ModuleDefinition module in assembly.Modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        string text = writer.StripMarkup(writer.WriteType(type));
                        if (options.Bodies)
                        {
                            StringBuilder withBodies = new StringBuilder(text);
                            foreach (MethodDefinition method in type.Methods)
                            {
                                if (!method.HasBody || LanguageWriter.IsCompilerGeneratedName(method.Name))
                                {
                                    continue;
                                }
                                try
                                {
                                    withBodies.AppendLine(writer.StripMarkup(writer.WriteMethod(method)));
                                }
                                catch (Exception ex)
                                {
                                    bodyFailures++;
                                    Log.Warn("Unable to render body of '" + type.FullName + "::" + method.Name + "'.", ex);
                                }
                            }
                            text = withBodies.ToString();
                        }

                        string fileName = SanitizeFileName(type.FullName) + "." + options.Language;
                        string header = writer.Dialect == null
                            ? string.Empty
                            : (options.Language == "vb" ? "' " : "// ") + "Dialect: " + writer.Dialect.DisplayName +
                              " (" + writer.Dialect.Toolchain + ")" + Environment.NewLine;
                        File.WriteAllText(Path.Combine(outputDirectory, fileName), header + text, Encoding.UTF8);
                        manifest.AppendLine(fileName);
                        count++;
                    }
                }

                File.WriteAllText(Path.Combine(outputDirectory, "manifest.txt"), manifest.ToString(), Encoding.UTF8);
                Console.WriteLine("decompiled " + count.ToString(CultureInfo.InvariantCulture) + " types into " + outputDirectory);
                if (bodyFailures > 0)
                {
                    Console.WriteLine("method bodies that could not be rendered: " + bodyFailures.ToString(CultureInfo.InvariantCulture));
                    Log.Warn("Decompile finished with " + bodyFailures + " method body failure(s).");
                }
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

            LoadedAssembly loaded = AssemblyLoader.Load(path);
            AssemblyDefinition assembly = loaded.Assembly;
            if (loaded.CarvedFromBlob)
            {
                Console.WriteLine("carved PE image from " + path + " at offset 0x" + loaded.BlobOffset.ToString("x"));
            }
            else
            {
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
            }

            return assembly;
        }

        private static int DisassembleIl(Options options)
        {
            if (options.UseNativeTools)
            {
                string text = IlTools.Disassemble(options.Input, options.Output, options.KeepBytes);
                if (string.IsNullOrEmpty(options.Output))
                {
                    Console.Write(text);
                }
                else
                {
                    Console.WriteLine("IL written to " + options.Output);
                }
                return 0;
            }

            // Default: the managed disassembler, which needs no external executable.
            AssemblyDefinition assembly = ReadAssembly(options.Input);
            string il = ManagedIlDisassembler.Disassemble(assembly, options.KeepBytes);
            if (string.IsNullOrEmpty(options.Output))
            {
                Console.Write(il);
            }
            else
            {
                File.WriteAllText(options.Output, il, new UTF8Encoding(false));
                Console.WriteLine("IL written to " + options.Output);
            }
            return 0;
        }

        private static int AssembleIl(Options options)
        {
            string output = options.Output;
            if (string.IsNullOrEmpty(output))
            {
                output = Path.ChangeExtension(options.Input, options.IsDll ? ".dll" : ".exe");
            }

            if (options.UseNativeTools)
            {
                IlTools.Assemble(options.Input, output, options.IsDll, options.Is64Bit, options.Is32Bit);
            }
            else
            {
                ManagedIlAssembler.AssembleFile(options.Input, output, options.IsDll);
                if (ManagedIlAssembler.LastWarnings != null)
                {
                    foreach (string warning in ManagedIlAssembler.LastWarnings)
                    {
                        Console.Error.WriteLine("warning: " + warning);
                    }
                }
            }

            Console.WriteLine("assembly written to " + output);
            return 0;
        }

        private static int Screenshot(Options options)
        {
            if (string.IsNullOrEmpty(options.Output))
            {
                Console.Error.WriteLine("error: --output <file.png> is required");
                return 2;
            }

            using (Form form = CreateWindowForCapture(options.Window))
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(0, 0);
                form.Show();
                for (int i = 0; i < 40; i++)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(40);
                }

                AboutForm about = form as AboutForm;
                if (about != null)
                {
                    about.SimulateWaterForCapture();
                    for (int i = 0; i < 10; i++)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(30);
                    }
                }

                using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                    bitmap.Save(options.Output, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            }

            Console.WriteLine("screenshot written to " + options.Output);
            return 0;
        }

        private static int RenderWater(Options options)
        {
            if (string.IsNullOrEmpty(options.Output))
            {
                Console.Error.WriteLine("error: --output <file.png> is required");
                return 2;
            }

            int width = options.Width > 0 ? options.Width : 540;
            int height = options.Height > 0 ? options.Height : 150;

            using (UI.WaterLogo water = new UI.WaterLogo())
            {
                water.Size = new Size(width, height);
                water.Image = AppResources.CompanyLogo;
                water.AddDrop(width / 3, height / 2, 30);
                water.AddDrop(width * 2 / 3, height / 3, 22);
                water.AddDrop(width / 2, height * 2 / 3, 26);
                water.Advance(options.Frames > 0 ? options.Frames : 14);

                using (Bitmap bitmap = new Bitmap(width, height))
                {
                    water.DrawToBitmap(bitmap, new Rectangle(0, 0, width, height));
                    bitmap.Save(options.Output, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            Console.WriteLine("water rendered to " + options.Output);
            return 0;
        }

        private static Form CreateWindowForCapture(string window)
        {
            if (string.Equals(window, "main", StringComparison.OrdinalIgnoreCase))
            {
                MainForm main = new MainForm();
                main.SuppressStartupDialogs = true;
                return main;
            }
            return new AboutForm();
        }

        private static int MusicCommand(Options options)
        {
            string input = options.Input;
            if (string.IsNullOrEmpty(input) || !File.Exists(input))
            {
                Console.Error.WriteLine("error: input file not found");
                return 2;
            }

            bool isMidi = input.EndsWith(".mid", StringComparison.OrdinalIgnoreCase) ||
                          input.EndsWith(".midi", StringComparison.OrdinalIgnoreCase);

            MidiSequence sequence;
            if (isMidi)
            {
                sequence = MidiReader.Read(input);
            }
            else
            {
                sequence = MidiTextFormat.Parse(File.ReadAllText(input));
            }

            Console.WriteLine("format      : " + sequence.Format);
            Console.WriteLine("tracks      : " + sequence.Tracks.Count);
            Console.WriteLine("ticks/qn    : " + sequence.TicksPerQuarterNote);
            Console.WriteLine("tempo       : " + sequence.MicrosecondsPerQuarter + " us/qn");
            Console.WriteLine("notes       : " + sequence.NoteCount);
            Console.WriteLine("duration    : " + TimeSpan.FromSeconds(sequence.Duration));

            string output = options.Output;
            if (!string.IsNullOrEmpty(output))
            {
                string extension = Path.GetExtension(output).ToLowerInvariant();
                if (extension == ".mid" || extension == ".midi")
                {
                    MidiWriter.Write(sequence, output);
                }
                else if (extension == ".wav")
                {
                    Synthesizer.RenderToWav(sequence, options.MusicVolume, output);
                }
                else
                {
                    File.WriteAllText(output, MidiTextFormat.Format(sequence));
                }
                Console.WriteLine("written to  : " + output);
            }
            return 0;
        }

        /// <summary>
        /// Checks that every instruction has an operand matching its opcode and that branch
        /// targets point inside the method body. Useful before writing an assembly back to
        /// disk (and for diagnosing damaged input).
        /// </summary>
        private static int Verify(Options options)
        {
            AssemblyDefinition assembly = ReadAssembly(options.Input);
            int methods = 0;
            int instructions = 0;
            int badOperands = 0;
            int badTargets = 0;
            List<string> problems = new List<string>();

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    VerifyType(type, ref methods, ref instructions, ref badOperands, ref badTargets, problems);
                }
            }

            Console.WriteLine("assembly    : " + assembly.Name.FullName);
            Console.WriteLine("methods     : " + methods);
            Console.WriteLine("instructions: " + instructions);
            Console.WriteLine("bad operands: " + badOperands);
            Console.WriteLine("bad targets : " + badTargets);

            if (problems.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("problems:");
                for (int i = 0; i < problems.Count && i < 40; i++)
                {
                    Console.WriteLine("  " + problems[i]);
                }
                if (problems.Count > 40)
                {
                    Console.WriteLine("  ... " + (problems.Count - 40) + " more");
                }
            }

            return badOperands == 0 && badTargets == 0 ? 0 : 1;
        }

        private static void VerifyType(TypeDefinition type, ref int methods, ref int instructions,
            ref int badOperands, ref int badTargets, List<string> problems)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                methods++;
                HashSet<Instruction> body = new HashSet<Instruction>(method.Body.Instructions);
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    instructions++;
                    OperandType operandType = instruction.OpCode.OperandType;
                    if (operandType != OperandType.InlineNone && instruction.Operand == null)
                    {
                        badOperands++;
                        if (problems.Count < 200)
                        {
                            problems.Add(type.FullName + "::" + method.Name + " IL_" +
                                         instruction.Offset.ToString("x4") + " " + instruction.OpCode.Name + " has a null operand");
                        }
                        continue;
                    }

                    Instruction target = instruction.Operand as Instruction;
                    if (target != null && !body.Contains(target))
                    {
                        badTargets++;
                        if (problems.Count < 200)
                        {
                            problems.Add(type.FullName + "::" + method.Name + " IL_" +
                                         instruction.Offset.ToString("x4") + " branches outside the body");
                        }
                    }

                    Instruction[] targets = instruction.Operand as Instruction[];
                    if (targets != null)
                    {
                        foreach (Instruction item in targets)
                        {
                            if (item == null || !body.Contains(item))
                            {
                                badTargets++;
                                break;
                            }
                        }
                    }
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                VerifyType(nested, ref methods, ref instructions, ref badOperands, ref badTargets, problems);
            }
        }

        /// <summary>Prints the structural completeness report for an assembly.</summary>
        private static int MeasureAccuracy(Options options)
        {
            AssemblyDefinition assembly = ReadAssembly(options.Input);

            LanguageWriter writer;
            switch (options.Language)
            {
                case "cs":
                case "csharp":
                    writer = new CSharpWriter();
                    break;
                case "vb":
                    writer = new VbWriter();
                    break;
                default:
                    writer = new IlWriter();
                    break;
            }

            if (!string.IsNullOrEmpty(options.Dialect))
            {
                OutputDialect dialect = OutputDialect.FromId(options.Dialect);
                if (dialect != null)
                {
                    writer.Dialect = dialect;
                }
            }

            AccuracyReport report = Accuracy.Measure(assembly, writer);
            Console.Write(report.ToString());

            if (options.VerboseOutput && report.Missing.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("missing members:");
                foreach (string item in report.Missing)
                {
                    Console.WriteLine("  " + item);
                }
            }

            return report.Percent >= 99.0 ? 0 : 1;
        }

        private static int DumpTree(Options options)
        {
            AssemblyDefinition assembly = ReadAssembly(options.Input);
            StringBuilder text = new StringBuilder();

            foreach (ModuleDefinition module in assembly.Modules)
            {
                text.AppendLine(module.Name);
                text.AppendLine("  References:");

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (!seen.Add(reference.FullName))
                    {
                        continue;
                    }

                    string resolved = null;
                    try
                    {
                        resolved = AssemblyResolver.Resolve(reference.FullName, null);
                    }
                    catch (Exception)
                    {
                    }

                    text.Append("    ").Append(reference.Name).Append(' ').Append(reference.Version);
                    text.Append(resolved == null ? "   [not found]" : "   -> " + resolved);
                    text.AppendLine();

                    if (resolved != null && options.Deep)
                    {
                        try
                        {
                            LoadedAssembly loaded = AssemblyLoader.Load(resolved);
                            foreach (TypeDefinition type in loaded.Assembly.MainModule.Types)
                            {
                                if (!string.IsNullOrEmpty(type.Namespace) && type.Name != "<Module>")
                                {
                                    text.Append("      ").AppendLine(type.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            text.Append("      (cannot read: ").Append(ex.Message).AppendLine(")");
                        }
                    }
                }

                text.AppendLine("  Namespaces:");
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }
                    text.Append("    ").Append(type.FullName).Append("  (").Append(type.Methods.Count).Append(" methods, ")
                        .Append(type.Fields.Count).Append(" fields)").AppendLine();
                }
            }

            Console.Write(text.ToString());
            return 0;
        }

        private static int Clean(Options options)
        {
            AssemblyDefinition assembly = ReadAssembly(options.Input);
            DeobfuscationReport report = DeobfuscationPipeline.Run(assembly, DeobfuscationPipeline.CreateDefault());

            string output = options.Output;
            if (string.IsNullOrEmpty(output))
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(options.Input)) ?? ".";
                output = Path.Combine(directory, Path.GetFileNameWithoutExtension(options.Input) + ".cleaned" + Path.GetExtension(options.Input));
            }

            WriterParameters parameters = new WriterParameters { WriteSymbols = false };
            assembly.Write(output, parameters);

            Console.WriteLine("cleaned assembly written to " + output);
            foreach (System.Collections.Generic.KeyValuePair<string, int> pair in report.Results)
            {
                Console.WriteLine("  " + pair.Key + ": " + pair.Value);
            }
            return 0;
        }

        private static int DisassembleX86(Options options)
        {
            byte[] data = File.ReadAllBytes(options.Input);
            int offset = options.Offset;
            int length = options.Length > 0 ? options.Length : data.Length - offset;

            // The managed decoder is always used; the native BeaEngine library is only a
            // fallback for byte sequences the managed decoder does not recognise.
            string text = ManagedDisassembler.DisassembleBlock(data, offset, length, options.Is64Bit, (ulong)Math.Max(offset, 0));
            if (!string.IsNullOrEmpty(options.Output))
            {
                File.WriteAllText(options.Output, text);
                Console.WriteLine("disassembly written to " + options.Output);
            }
            else
            {
                Console.Write(text);
            }
            return 0;
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
            Console.WriteLine("  ILXP --tree <assembly> [--deep]          list references/namespaces (with resolution)");
            Console.WriteLine("  ILXP --accuracy <assembly> [--language il|cs|vb] [--verbose]");
            Console.WriteLine("                                           structural completeness report");
            Console.WriteLine("  ILXP --headless                          command shell on stdin (no window)");
            Console.WriteLine("  ILXP --decompile <assembly> [options]    write generated source to disk");
            Console.WriteLine("  ILXP --resources <assembly> --output <dir>");
            Console.WriteLine("  ILXP --ildasm <assembly> [--output <file.il>] [--bytes]");
            Console.WriteLine("  ILXP --ilasm <file.il> [--output <assembly>] [--dll] [--x86|--x64]");
            Console.WriteLine("  ILXP --disasm-x86 <file> [--offset <n>] [--length <n>] [--x64]");
            Console.WriteLine("  ILXP --clean <assembly> [--output <assembly>]   run IL cleanup passes");
            Console.WriteLine("  ILXP --music <file.mid|file.txt> [--output <out.mid|out.txt|out.wav>]");
            Console.WriteLine("  ILXP --screenshot <out.png> [--window about|main]");
            Console.WriteLine();
            Console.WriteLine("Decompile options:");
            Console.WriteLine("  --language <il|cs|vb>   output language (default: il)");
            Console.WriteLine("  --dialect <id>          output dialect: cs1 cs2 cs3 cs4 cs5 cs6 cs7 cs8 cs9 sd vb");
            Console.WriteLine("  --output <directory>    destination directory");
            Console.WriteLine("  --bodies                also render method bodies (C#/VB)");
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
            public bool KeepBytes { get; private set; }
            public bool IsDll { get; private set; }
            public bool Is64Bit { get; private set; }
            public bool Is32Bit { get; private set; }
            public bool Bodies { get; private set; }
            public int Offset { get; private set; }
            public int Length { get; private set; }
            public float MusicVolume { get; private set; }
            public string Window { get; private set; }
            public string Dialect { get; private set; }
            public bool Deep { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public int Frames { get; private set; }
            public bool VerboseOutput { get; private set; }
            public bool UseNativeTools { get; private set; }

            public static Options Parse(string[] args)
            {
                Options options = new Options { Language = "il", MusicVolume = 0.55f };

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
                        case "--ildasm":
                            options.Command = "ildasm";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--ilasm":
                            options.Command = "ilasm";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--disasm-x86":
                            options.Command = "disasm-x86";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--clean":
                            options.Command = "clean";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--music":
                            options.Command = "music";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--screenshot":
                            options.Command = "screenshot";
                            options.Output = Next(args, ref i, argument);
                            break;
                        case "--render-water":
                            options.Command = "render-water";
                            options.Output = Next(args, ref i, argument);
                            break;
                        case "--width":
                            options.Width = int.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--height":
                            options.Height = int.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--frames":
                            options.Frames = int.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--window":
                            options.Window = Next(args, ref i, argument);
                            break;
                        case "--volume":
                            options.MusicVolume = float.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--bytes":
                            options.KeepBytes = true;
                            break;
                        case "--tree":
                            options.Command = "tree";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--accuracy":
                            options.Command = "accuracy";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--verify":
                            options.Command = "verify";
                            options.Input = Next(args, ref i, argument);
                            break;
                        case "--headless":
                            options.Command = "headless";
                            break;
                        case "--verbose":
                            options.VerboseOutput = true;
                            break;
                        case "--deep":
                            options.Deep = true;
                            break;
                        case "--native":
                            options.UseNativeTools = true;
                            break;
                        case "--bodies":
                            options.Bodies = true;
                            break;
                        case "--dll":
                            options.IsDll = true;
                            break;
                        case "--x64":
                            options.Is64Bit = true;
                            break;
                        case "--x86":
                            options.Is32Bit = true;
                            break;
                        case "--offset":
                            options.Offset = int.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--length":
                            options.Length = int.Parse(Next(args, ref i, argument), CultureInfo.InvariantCulture);
                            break;
                        case "--language":
                            options.Language = Next(args, ref i, argument).ToLowerInvariant();
                            break;
                        case "--dialect":
                            options.Dialect = Next(args, ref i, argument);
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