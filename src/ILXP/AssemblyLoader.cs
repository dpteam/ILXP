using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>Result of loading an assembly, possibly carved out of a larger blob.</summary>
    internal sealed class LoadedAssembly
    {
        public AssemblyDefinition Assembly;
        public string SourcePath;
        public bool CarvedFromBlob;
        public long BlobOffset;
    }

    /// <summary>
    /// Loads assemblies with several fallbacks. The first attempt is a normal Cecil
    /// read; when that fails (truncated file, memory dump, appended data) the loader
    /// searches the buffer for a valid PE header and retries from that offset.
    /// </summary>
    internal static class AssemblyLoader
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<string> TemporaryFiles = new List<string>();

        public static LoadedAssembly Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("assembly path is required", "path");
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("assembly not found", path);
            }

            Exception firstError;
            try
            {
                return new LoadedAssembly
                {
                    Assembly = AssemblyDefinition.ReadAssembly(path, CreateParameters()),
                    SourcePath = path
                };
            }
            catch (Exception ex)
            {
                firstError = ex;
                Log.Warn("Cecil cannot read '" + path + "' directly; trying to carve a PE image.", ex);
            }

            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to read '" + path + "'.", new AggregateException(firstError, ex));
            }

            List<long> candidates = FindPeHeaders(data);
            foreach (long offset in candidates)
            {
                try
                {
                    string temporary = ExtractToTemporaryFile(data, offset);
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(temporary, CreateParameters());
                    Log.Info("Carved a PE image from '" + path + "' at offset 0x" + offset.ToString("x") + ".");
                    return new LoadedAssembly
                    {
                        Assembly = assembly,
                        SourcePath = path,
                        CarvedFromBlob = true,
                        BlobOffset = offset
                    };
                }
                catch (Exception ex)
                {
                    Log.Verbose("PE candidate at 0x" + offset.ToString("x") + " failed: " + ex.Message);
                }
            }

            throw new InvalidOperationException(
                "Unable to read '" + path + "' as a .NET assembly, and no valid PE image was found in the file.",
                firstError);
        }

        private static ReaderParameters CreateParameters()
        {
            return new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false
            };
        }

        private static List<long> FindPeHeaders(byte[] data)
        {
            List<long> offsets = new List<long>();
            if (data == null || data.Length < 0x40)
            {
                return offsets;
            }

            int limit = Math.Min(data.Length - 0x40, 64 * 1024 * 1024);
            for (int i = 0; i + 0x40 < limit; i++)
            {
                if (data[i] != (byte)'M' || data[i + 1] != (byte)'Z')
                {
                    continue;
                }

                int peOffset = BitConverter.ToInt32(data, i + 0x3C);
                if (peOffset <= 0 || i + peOffset + 0x18 > data.Length)
                {
                    continue;
                }

                int pe = i + peOffset;
                if (data[pe] != (byte)'P' || data[pe + 1] != (byte)'E' || data[pe + 2] != 0 || data[pe + 3] != 0)
                {
                    continue;
                }

                ushort machine = BitConverter.ToUInt16(data, pe + 4);
                if (machine != 0x014C && machine != 0x8664 && machine != 0x01C0 && machine != 0xAA64)
                {
                    continue;
                }

                ushort magic = BitConverter.ToUInt16(data, pe + 24);
                if (magic != 0x010B && magic != 0x020B)
                {
                    continue;
                }

                offsets.Add(i);
                if (offsets.Count >= 16)
                {
                    break;
                }
            }
            return offsets;
        }

        private static string ExtractToTemporaryFile(byte[] data, long offset)
        {
            string directory = Path.Combine(Path.GetTempPath(), "ILXP", "carved");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "carved-" + Guid.NewGuid().ToString("N") + ".dll");
            int length = data.Length - (int)offset;
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.Write(data, (int)offset, length);
            }

            lock (SyncRoot)
            {
                TemporaryFiles.Add(path);
            }
            return path;
        }

        /// <summary>Deletes temporary carved images (called on shutdown).</summary>
        public static void CleanupTemporaryFiles()
        {
            lock (SyncRoot)
            {
                foreach (string path in TemporaryFiles)
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
                TemporaryFiles.Clear();
            }
        }
    }
}
