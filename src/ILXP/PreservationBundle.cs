using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Exports a self-describing "preservation bundle": the original bytes, the IL listing,
    /// the generated C# outline, every embedded resource, the health report and a manifest
    /// with SHA-256 hashes.
    ///
    /// The point is long-term preservation: given only this folder, the assembly can be
    /// inspected, decompiled and verified years later, on any machine.
    /// </summary>
    internal static class PreservationBundle
    {
        internal sealed class Result
        {
            public string Directory;
            public int Files;
            public long Bytes;
        }

        public static Result Export(AssemblyItem item, string rootDirectory)
        {
            if (item == null || item.Assembly == null)
            {
                throw new ArgumentException("no assembly to export", "item");
            }

            string name = Decompiler.SanitizeFileName(item.Assembly.Name.Name);
            string directory = Path.Combine(rootDirectory, name + "-" + AppInfo.VersionText);
            int suffix = 1;
            while (Directory.Exists(directory))
            {
                directory = Path.Combine(rootDirectory, name + "-" + AppInfo.VersionText + "-" + suffix);
                suffix++;
            }

            Directory.CreateDirectory(directory);
            Result result = new Result { Directory = directory };
            StringBuilder manifest = new StringBuilder();
            manifest.AppendLine("ILXP preservation bundle");
            manifest.AppendLine("tool      : " + AppInfo.Title);
            manifest.AppendLine("created   : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            manifest.AppendLine("assembly  : " + item.Assembly.Name.FullName);
            manifest.AppendLine("source    : " + item.FilePath);
            manifest.AppendLine();

            // 1. Original bytes, unmodified.
            if (File.Exists(item.FilePath))
            {
                string original = Path.Combine(directory, "original" + Path.GetExtension(item.FilePath));
                File.Copy(item.FilePath, original, true);
                AddFile(manifest, original, directory, ref result);

                string pdb = Path.ChangeExtension(item.FilePath, ".pdb");
                if (File.Exists(pdb))
                {
                    string pdbCopy = Path.Combine(directory, "original.pdb");
                    File.Copy(pdb, pdbCopy, true);
                    AddFile(manifest, pdbCopy, directory, ref result);
                }
            }

            // 2. Health report.
            try
            {
                AssemblyHealthReport health = AssemblyHealth.Analyse(item.Assembly, item.FilePath);
                WriteText(manifest, directory, "health.txt", AssemblyHealth.Format(health), ref result);
            }
            catch (Exception ex)
            {
                Log.Warn("Bundle: health report failed.", ex);
            }

            // 3. IL listing.
            try
            {
                string il = Interop.ManagedIlDisassembler.Disassemble(item.Assembly, false);
                WriteText(manifest, directory, "assembly.il", il, ref result);
            }
            catch (Exception ex)
            {
                Log.Warn("Bundle: IL listing failed.", ex);
            }

            // 4. Generated sources, one folder per language.
            foreach (LanguageWriter writer in new LanguageWriter[] { new IlWriter(), new CSharpWriter(), new VbWriter() })
            {
                try
                {
                    string languageDirectory = Path.Combine(directory, "decompiled-" + Decompiler.GetExtension(writer));
                    Directory.CreateDirectory(languageDirectory);
                    Decompiler.ExportTypes(item.Assembly, writer, languageDirectory);
                    foreach (string file in Directory.GetFiles(languageDirectory))
                    {
                        AddFile(manifest, file, directory, ref result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Bundle: decompilation failed for " + writer + ".", ex);
                }
            }

            // 5. Embedded resources.
            try
            {
                string resourceDirectory = Path.Combine(directory, "resources");
                Directory.CreateDirectory(resourceDirectory);
                foreach (Resource resource in item.Assembly.MainModule.Resources)
                {
                    EmbeddedResource embedded = resource as EmbeddedResource;
                    if (embedded == null)
                    {
                        continue;
                    }

                    string target = Path.Combine(resourceDirectory, Decompiler.SanitizeFileName(resource.Name));
                    File.WriteAllBytes(target, embedded.GetResourceData());
                    AddFile(manifest, target, directory, ref result);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Bundle: resource extraction failed.", ex);
            }

            // 6. Manifest last, so it contains every other file's hash.
            string manifestPath = Path.Combine(directory, "manifest.txt");
            File.WriteAllText(manifestPath, manifest.ToString(), new UTF8Encoding(false));
            result.Files++;
            result.Bytes += new FileInfo(manifestPath).Length;

            Log.Info("Preservation bundle written to " + directory + " (" + result.Files + " files).");
            return result;
        }

        private static void WriteText(StringBuilder manifest, string directory, string fileName,
            string content, ref Result result)
        {
            string path = Path.Combine(directory, fileName);
            File.WriteAllText(path, content ?? string.Empty, new UTF8Encoding(false));
            AddFile(manifest, path, directory, ref result);
        }

        private static void AddFile(StringBuilder manifest, string path, string root, ref Result result)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                manifest.AppendLine(string.Format("{0}  {1,10}  {2}",
                    Sha256(path),
                    info.Length,
                    path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar)));
                result.Files++;
                result.Bytes += info.Length;
            }
            catch (Exception ex)
            {
                manifest.AppendLine("(unreadable)  " + path + "  " + ex.Message);
            }
        }

        private static string Sha256(string path)
        {
            try
            {
                using (SHA256 sha = SHA256.Create())
                using (FileStream stream = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    StringBuilder text = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                    }
                    return text.ToString();
                }
            }
            catch (Exception)
            {
                return new string('0', 64);
            }
        }
    }
}
