using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ILXP.Tests
{
    /// <summary>
    /// Dependency-free test runner. The project targets .NET Framework 4.0, so it uses a
    /// tiny assertion helper instead of a NuGet test framework; the runner is also usable
    /// from CI and from the headless REPL.
    /// </summary>
    internal static class Program
    {
        private static int failures;
        private static int passed;
        private static readonly List<string> failureMessages = new List<string>();

        private static string ilxpPath;
        private static string workspace;

        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "run")
            {
                return RunAll(args);
            }

            switch (args[0])
            {
                case "obfuscate":
                    return Obfuscate(args);
                case "help":
                case "--help":
                case "-h":
                    PrintUsage();
                    return 0;
                default:
                    Console.Error.WriteLine("unknown command: " + args[0]);
                    PrintUsage();
                    return 2;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("ILXP.Tests – test runner and test obfuscator");
            Console.WriteLine();
            Console.WriteLine("  ILXP.Tests run [--ilxp <path>] [--workspace <dir>]   run all tests");
            Console.WriteLine("  ILXP.Tests obfuscate <input> <output> [seed]          obfuscate an assembly");
            Console.WriteLine("  ILXP.Tests help");
        }

        private static int Obfuscate(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("usage: obfuscate <input> <output> [seed]");
                return 2;
            }

            int seed = args.Length > 3 ? int.Parse(args[3]) : 12345;
            bool nops = !(args.Length > 4 && args[4] == "no-nops");
            bool dead = !(args.Length > 4 && args[4] == "no-dead");
            TestObfuscator.Statistics statistics = TestObfuscator.Obfuscate(args[1], args[2], seed, nops, dead);
            Console.WriteLine(statistics.ToString());
            Console.WriteLine("written to " + args[2]);
            return 0;        }

        private static int RunAll(string[] args)
        {
            workspace = Path.Combine(Path.GetTempPath(), "ilxp-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspace);
            ilxpPath = LocateIlxp(args);

            Console.WriteLine("ILXP test runner");
            Console.WriteLine("  ILXP.exe : " + ilxpPath);
            Console.WriteLine("  workspace: " + workspace);
            Console.WriteLine();

            try
            {
                TestCliBasics();
                TestAccuracy();
                TestManagedRoundTrip();
                TestCleanPasses();
                TestObfuscationRoundTrip();
                TestDamagedInput();
                TestMusicPipeline();
                TestHeadless();
                TestQuickOpen();
                TestVerification();
            }
            finally
            {
                try
                {
                    Directory.Delete(workspace, true);
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("passed: {0}, failed: {1}", passed, failures));
            foreach (string message in failureMessages)
            {
                Console.WriteLine("  FAILED: " + message);
            }
            return failures == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------------
        // Tests
        // ------------------------------------------------------------------

        private static void TestCliBasics()
        {
            Section("CLI basics");

            CommandResult version = RunIlxp("--version");
            Check("version exits with 0", version.ExitCode == 0, version.ToString());
            Check("version prints the product name", version.Output.Contains("ILXP"), version.Output);

            CommandResult list = RunIlxp("--list", ilxpPath);
            Check("--list succeeds", list.ExitCode == 0, list.ToString());
            Check("--list shows MainForm", list.Output.Contains("MainForm"), "output did not mention MainForm");

            CommandResult help = RunIlxp("--help");
            Check("--help succeeds", help.ExitCode == 0, help.ToString());
            Check("--help documents --accuracy", help.Output.Contains("--accuracy"), "no accuracy entry");
        }

        private static void TestAccuracy()
        {
            Section("Decompile accuracy on ILXP.exe");

            foreach (string language in new[] { "il", "cs", "vb" })
            {
                CommandResult result = RunIlxp("--accuracy", ilxpPath, "--language", language);
                string summary = ExtractLine(result.Output, "structural completeness");
                Check(language + ": accuracy runs", result.ExitCode == 0 || summary != null, result.ToString());
                Check(language + ": accuracy >= 95%", summary != null && ParsePercent(summary) >= 95.0, summary ?? "no summary");
                Console.WriteLine("    " + language + ": " + (summary ?? "n/a"));
            }
        }

        private static void TestManagedRoundTrip()
        {
            Section("Managed ILDasm/ILAsm round trip");

            string il = Path.Combine(workspace, "ilxp.il");
            CommandResult disasm = RunIlxp("--ildasm", ilxpPath, "--output", il);
            Check("--ildasm succeeds", disasm.ExitCode == 0, disasm.ToString());
            Check("IL file is large", File.Exists(il) && new FileInfo(il).Length > 100000, "IL output too small");

            string dll = Path.Combine(workspace, "ilxp.rt.dll");
            CommandResult asm = RunIlxp("--ilasm", il, "--output", dll, "--dll");
            Check("--ilasm succeeds", asm.ExitCode == 0, asm.ToString());
            Check("reassembled DLL exists", File.Exists(dll) && new FileInfo(dll).Length > 10000, "DLL too small");

            CommandResult list = RunIlxp("--list", dll);
            Check("reassembled DLL is readable", list.ExitCode == 0 && list.Output.Contains("MainForm"), list.ToString());
        }

        private static void TestCleanPasses()
        {
            Section("Cleanup passes");

            string clean = Path.Combine(workspace, "ilxp.clean.exe");
            CommandResult result = RunIlxp("--clean", ilxpPath, "--output", clean);
            Check("--clean succeeds", result.ExitCode == 0, result.ToString());
            Check("cleaned assembly exists", File.Exists(clean), "no output file");

            CommandResult list = RunIlxp("--list", clean);
            Check("cleaned assembly is readable", list.ExitCode == 0, list.ToString());
        }

        private static void TestObfuscationRoundTrip()
        {
            Section("Obfuscation round trip");

            string obfuscated = Path.Combine(workspace, "ilxp.obfuscated.exe");
            TestObfuscator.Statistics statistics = TestObfuscator.Obfuscate(ilxpPath, obfuscated, 4242);
            Console.WriteLine("    " + statistics);
            Check("obfuscator produced output", File.Exists(obfuscated), "no output");
            Check("obfuscator renamed members", statistics.RenamedMethods > 0, statistics.ToString());
            Check("obfuscator inserted NOPs", statistics.InsertedNops > 0, statistics.ToString());

            CommandResult list = RunIlxp("--list", obfuscated);
            Check("obfuscated assembly still loads", list.ExitCode == 0, list.ToString());

            CommandResult accuracy = RunIlxp("--accuracy", obfuscated, "--language", "il");
            string summary = ExtractLine(accuracy.Output, "structural completeness");
            Check("obfuscated assembly decompiles", summary != null && ParsePercent(summary) >= 95.0, summary ?? accuracy.ToString());

            string cleaned = Path.Combine(workspace, "ilxp.obfuscated.clean.exe");
            CommandResult clean = RunIlxp("--clean", obfuscated, "--output", cleaned);
            Check("--clean works on obfuscated input", clean.ExitCode == 0, clean.ToString());

            long before = CountNops(obfuscated);
            long after = CountNops(cleaned);
            Check("cleanup removed NOPs", after < before, string.Format("before={0} after={1}", before, after));
            Console.WriteLine(string.Format("    NOPs: {0} -> {1}", before, after));
        }

        private static void TestDamagedInput()
        {
            Section("Damaged input handling");

            byte[] original = File.ReadAllBytes(ilxpPath);
            byte[] blob = new byte[original.Length + 4096];
            for (int i = 0; i < 4096; i++)
            {
                blob[i] = (byte)(i * 7);
            }
            Buffer.BlockCopy(original, 0, blob, 4096, original.Length);

            string path = Path.Combine(workspace, "dump.bin");
            File.WriteAllBytes(path, blob);

            CommandResult list = RunIlxp("--list", path);
            Check("carved PE is readable", list.ExitCode == 0 && list.Output.Contains("MainForm"), list.ToString());

            string truncated = Path.Combine(workspace, "truncated.exe");
            byte[] cut = new byte[original.Length / 3];
            Buffer.BlockCopy(original, 0, cut, 0, cut.Length);
            File.WriteAllBytes(truncated, cut);

            CommandResult broken = RunIlxp("--list", truncated);
            Check("truncated input does not crash", !broken.Output.Contains("Unhandled") && !broken.Error.Contains("Unhandled"), broken.ToString());

            string garbage = Path.Combine(workspace, "garbage.bin");
            byte[] noise = new byte[8192];
            Random random = new Random(7);
            random.NextBytes(noise);
            File.WriteAllBytes(garbage, noise);

            CommandResult invalid = RunIlxp("--list", garbage);
            Check("garbage input fails cleanly", invalid.ExitCode != 0, invalid.ToString());
            Check("garbage input reports an error", invalid.Error.Length > 0 || invalid.Output.Length > 0, "no diagnostics");
        }

        private static void TestMusicPipeline()
        {
            Section("Music pipeline");

            string track = FindTrack("T2JD.txt");
            if (track == null)
            {
                Check("music track available", false, "T2JD.txt not found");
                return;
            }

            string midi = Path.Combine(workspace, "t2jd.mid");
            CommandResult toMidi = RunIlxp("--music", track, "--output", midi);
            Check("text -> MIDI", toMidi.ExitCode == 0 && File.Exists(midi), toMidi.ToString());

            string text = Path.Combine(workspace, "t2jd.txt");
            CommandResult toText = RunIlxp("--music", midi, "--output", text);
            Check("MIDI -> text", toText.ExitCode == 0 && File.Exists(text), toText.ToString());

            string wav = Path.Combine(workspace, "t2jd.wav");
            CommandResult toWav = RunIlxp("--music", midi, "--output", wav);
            Check("MIDI -> WAV", toWav.ExitCode == 0 && File.Exists(wav), toWav.ToString());

            if (File.Exists(wav))
            {
                WavInfo info = ReadWav(wav);
                Check("WAV is stereo", info.Channels == 2, "channels=" + info.Channels);
                Check("WAV is not silent", info.Peak > 1000, "peak=" + info.Peak);
                Check("WAV has content", info.Seconds > 5, "seconds=" + info.Seconds);
                Console.WriteLine(string.Format("    wav: {0:n1}s, peak {1}, rms {2}", info.Seconds, info.Peak, info.Rms));
            }
        }

        private static void TestHeadless()
        {
            Section("Headless mode");
            string script = string.Join(Environment.NewLine, new[]
            {
                "open " + ilxpPath,
                "list",
                "accuracy il",
                "quit"
            }) + Environment.NewLine;

            string scriptPath = Path.Combine(workspace, "script.txt");
            File.WriteAllText(scriptPath, script);

            CommandResult result = RunIlxpWithInput(scriptPath, "--headless");
            Check("headless exits cleanly", result.ExitCode == 0, result.ToString());
            Check("headless listed types", result.Output.Contains("MainForm"), result.Output);
            Check("headless ran accuracy", result.Output.Contains("structural completeness"), result.Output);
        }

        private static void TestQuickOpen()
        {
            Section("Quick Open index");

            CommandResult all = RunIlxp("--search", ilxpPath, "waterlogo");
            Check("search runs", all.ExitCode == 0, all.ToString());
            Check("search finds WaterLogo", all.Output.Contains("WaterLogo"), all.Output);

            CommandResult member = RunIlxp("--search", ilxpPath, "selectnodepath");
            Check("search finds a method", member.ExitCode == 0 && member.Output.Contains("SelectNodeByPath"), member.Output);

            CommandResult nothing = RunIlxp("--search", ilxpPath, "zzzznotarealthing");
            Check("search reports no matches", nothing.ExitCode == 1, nothing.ToString());
        }

        private static void TestVerification()
        {
            Section("IL verification");

            CommandResult valid = RunIlxp("--verify", ilxpPath);
            Check("built assembly verifies", valid.ExitCode == 0, valid.Output);

            string obfuscated = Path.Combine(workspace, "verify.obf.exe");
            TestObfuscator.Obfuscate(ilxpPath, obfuscated, 99);
            CommandResult obfuscatedResult = RunIlxp("--verify", obfuscated);
            Check("obfuscated assembly verifies", obfuscatedResult.ExitCode == 0, obfuscatedResult.Output);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private sealed class CommandResult
        {
            public int ExitCode;
            public string Output;
            public string Error;

            public override string ToString()
            {
                StringBuilder text = new StringBuilder();
                text.Append("exit=").Append(ExitCode);
                if (!string.IsNullOrEmpty(Error))
                {
                    text.Append(" stderr=").Append(Error.Trim());
                }
                return text.ToString();
            }
        }

        private sealed class WavInfo
        {
            public int Channels;
            public int Peak;
            public int Rms;
            public double Seconds;
        }

        private static WavInfo ReadWav(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            WavInfo info = new WavInfo { Channels = BitConverter.ToInt16(data, 22) };
            int sampleRate = BitConverter.ToInt32(data, 24);
            int dataBytes = data.Length - 44;
            int frames = dataBytes / (info.Channels * 2);
            info.Seconds = (double)frames / sampleRate;

            long sum = 0;
            int count = 0;
            for (int i = 44; i + 1 < data.Length; i += 2)
            {
                short sample = BitConverter.ToInt16(data, i);
                int absolute = Math.Abs((int)sample);
                if (absolute > info.Peak)
                {
                    info.Peak = absolute;
                }
                sum += (long)sample * sample;
                count++;
            }
            info.Rms = count == 0 ? 0 : (int)Math.Sqrt(sum / (double)count);
            return info;
        }

        private static long CountNops(string path)
        {
            long count = 0;
            Mono.Cecil.AssemblyDefinition assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(path);
            foreach (Mono.Cecil.ModuleDefinition module in assembly.Modules)
            {
                foreach (Mono.Cecil.TypeDefinition type in module.Types)
                {
                    count += CountNopsInType(type);
                }
            }
            return count;
        }

        private static long CountNopsInType(Mono.Cecil.TypeDefinition type)
        {
            long count = 0;
            foreach (Mono.Cecil.MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }
                foreach (Mono.Cecil.Cil.Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Nop)
                    {
                        count++;
                    }
                }
            }
            foreach (Mono.Cecil.TypeDefinition nested in type.NestedTypes)
            {
                count += CountNopsInType(nested);
            }
            return count;
        }

        private static string FindTrack(string fileName)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDirectory, "Music", "Tracks", fileName),
                Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "src", "ILXP", "Music", "Tracks", fileName)
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            return null;
        }

        private static string LocateIlxp(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--ilxp")
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDirectory, "ILXP.exe"),
                Path.Combine(baseDirectory, "..", "..", "..", "..", "src", "ILXP", "bin", "Debug", "net40", "ILXP.exe"),
                Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "src", "ILXP", "bin", "Debug", "net40", "ILXP.exe")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            throw new FileNotFoundException("ILXP.exe not found; pass --ilxp <path>");
        }

        private static CommandResult RunIlxp(params string[] arguments)
        {
            return Run(ilxpPath, arguments, null);
        }

        private static CommandResult RunIlxpWithInput(string inputPath, params string[] arguments)
        {
            return Run(ilxpPath, arguments, inputPath);
        }

        private static CommandResult Run(string executable, string[] arguments, string inputPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = inputPath != null
            };
            foreach (string argument in arguments)
            {
                startInfo.Arguments += (startInfo.Arguments.Length == 0 ? string.Empty : " ") + Quote(argument);
            }

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                StringBuilder stdout = new StringBuilder();
                StringBuilder stderr = new StringBuilder();
                process.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stdout.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stderr.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (inputPath != null)
                {
                    process.StandardInput.Write(File.ReadAllText(inputPath));
                    process.StandardInput.Close();
                }

                if (!process.WaitForExit(300000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    return new CommandResult { ExitCode = -1, Output = stdout.ToString(), Error = "timeout" };
                }
                process.WaitForExit();

                return new CommandResult
                {
                    ExitCode = process.ExitCode,
                    Output = stdout.ToString(),
                    Error = stderr.ToString()
                };
            }
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value + "\"" : value;
        }

        private static string ExtractLine(string text, string contains)
        {
            if (text == null)
            {
                return null;
            }
            foreach (string line in text.Split('\n'))
            {
                if (line.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return line.Trim();
                }
            }
            return null;
        }

        private static double ParsePercent(string line)
        {
            int index = line.LastIndexOf('%');
            if (index < 0)
            {
                return 0;
            }

            int start = index - 1;
            while (start >= 0 && (char.IsDigit(line[start]) || line[start] == '.' || line[start] == ','))
            {
                start--;
            }

            string number = line.Substring(start + 1, index - start - 1).Replace(',', '.');
            double value;
            return double.TryParse(number, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static void Section(string name)
        {
            Console.WriteLine();
            Console.WriteLine("[" + name + "]");
        }

        private static void Check(string name, bool condition, string details)
        {
            if (condition)
            {
                passed++;
                Console.WriteLine("  ok    " + name);
            }
            else
            {
                failures++;
                failureMessages.Add(name + (string.IsNullOrEmpty(details) ? string.Empty : " (" + details + ")"));
                Console.WriteLine("  FAIL  " + name + (string.IsNullOrEmpty(details) ? string.Empty : " -> " + details));
            }
        }
    }
}
