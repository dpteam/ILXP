using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ILXP.Interop
{
    /// <summary>
    /// Integration with Microsoft's IL assembler and disassembler. Both tools are
    /// shipped next to the application (native folder) and are also discovered in the
    /// installed .NET Framework / Windows SDK directories.
    /// </summary>
    internal static class IlTools
    {
        private static readonly object SyncRoot = new object();

        public static string IldasmPath
        {
            get { return FindTool("ildasm.exe"); }
        }

        public static string IlasmPath
        {
            get { return FindTool("ilasm.exe"); }
        }

        public static bool IsIldasmAvailable
        {
            get { return IldasmPath != null; }
        }

        public static bool IsIlasmAvailable
        {
            get { return IlasmPath != null; }
        }

        /// <summary>
        /// Disassembles an assembly to IL text. When <paramref name="outputPath"/> is
        /// null the IL is returned as a string (through a temporary file).
        /// </summary>
        public static string Disassemble(string assemblyPath, string outputPath, bool keepBytes = false)
        {
            string tool = IldasmPath;
            if (tool == null)
            {
                throw new FileNotFoundException("ildasm.exe was not found. Place it in the 'native' folder next to ILXP.exe.");
            }
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("assembly not found", assemblyPath);
            }
            assemblyPath = Path.GetFullPath(assemblyPath);

            bool temporary = string.IsNullOrEmpty(outputPath);
            if (temporary)
            {
                outputPath = Path.Combine(Path.GetTempPath(), "ILXP-" + Guid.NewGuid().ToString("N") + ".il");
            }
            outputPath = Path.GetFullPath(outputPath);

            try
            {
                List<string> arguments = new List<string>
                {
                    "/text",
                    "/out=" + Quote(outputPath),
                    Quote(assemblyPath)
                };
                if (keepBytes)
                {
                    arguments.Insert(1, "/bytes");
                }

                ProcessResult result = Run(tool, arguments, 120000);
                if (result.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "ildasm failed with exit code " + result.ExitCode + "." +
                        (string.IsNullOrEmpty(result.StandardError) ? string.Empty : Environment.NewLine + result.StandardError));
                }

                if (temporary)
                {
                    return File.Exists(outputPath) ? File.ReadAllText(outputPath) : result.StandardOutput;
                }
                return outputPath;
            }
            finally
            {
                if (temporary)
                {
                    TryDelete(outputPath);
                }
            }
        }

        /// <summary>Assembles IL text back into an assembly.</summary>
        public static void Assemble(string ilPath, string outputPath, bool isDll, bool is64Bit, bool is32Bit)
        {
            string tool = IlasmPath;
            if (tool == null)
            {
                throw new FileNotFoundException("ilasm.exe was not found. Place it in the 'native' folder next to ILXP.exe.");
            }
            if (!File.Exists(ilPath))
            {
                throw new FileNotFoundException("IL source not found", ilPath);
            }
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("output path is required", "outputPath");
            }
            ilPath = Path.GetFullPath(ilPath);
            outputPath = Path.GetFullPath(outputPath);

            List<string> arguments = new List<string>
            {
                "/quiet",
                "/output=" + Quote(outputPath),
                isDll ? "/dll" : "/exe"
            };
            if (is64Bit)
            {
                arguments.Add("/x64");
            }
            else if (is32Bit)
            {
                arguments.Add("/x86");
            }
            arguments.Add(Quote(ilPath));

            ProcessResult result = Run(tool, arguments, 120000);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "ilasm failed with exit code " + result.ExitCode + "." +
                    (string.IsNullOrEmpty(result.StandardError) ? string.Empty : Environment.NewLine + result.StandardError));
            }
        }

        private static string FindTool(string fileName)
        {
            lock (SyncRoot)
            {
                foreach (string directory in GetSearchDirectories())
                {
                    try
                    {
                        string candidate = Path.Combine(directory, fileName);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Verbose("Unable to probe '" + directory + "': " + ex.Message);
                    }
                }
                return null;
            }
        }

        private static IEnumerable<string> GetSearchDirectories()
        {
            List<string> directories = new List<string>();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            directories.Add(Path.Combine(baseDirectory, "native"));
            directories.Add(baseDirectory);

            try
            {
                directories.Add(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            }
            catch (Exception)
            {
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                directories.Add(Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools"));
                directories.Add(Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools"));
                directories.Add(Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools"));
                directories.Add(Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v7.0A\bin"));
                directories.Add(Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v6.0A\bin"));
            }

            string windows = Environment.GetEnvironmentVariable("WINDIR");
            if (!string.IsNullOrEmpty(windows))
            {
                directories.Add(Path.Combine(windows, "Microsoft.NET", "Framework"));
                directories.Add(Path.Combine(windows, "Microsoft.NET", "Framework64"));
            }
            return directories;
        }

        private static ProcessResult Run(string fileName, IEnumerable<string> arguments, int timeoutMilliseconds)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(fileName) ?? "."
            };
            foreach (string argument in arguments)
            {
                startInfo.Arguments += (startInfo.Arguments.Length == 0 ? string.Empty : " ") + argument;
            }

            Log.Info("Running: " + fileName + " " + startInfo.Arguments);
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                StringBuilder stdout = new StringBuilder();
                StringBuilder stderr = new StringBuilder();
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stdout.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stderr.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    throw new TimeoutException(fileName + " did not finish within " + timeoutMilliseconds + " ms.");
                }
                process.WaitForExit();

                return new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = stdout.ToString(),
                    StandardError = stderr.ToString()
                };
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
            }
        }

        private sealed class ProcessResult
        {
            public int ExitCode;
            public string StandardOutput;
            public string StandardError;
        }
    }
}
