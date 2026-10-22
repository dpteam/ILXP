using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Shared export logic used by the GUI ("Decompile to folder") and by the command
    /// line. One file per type, plus a manifest that records what was written.
    /// </summary>
    internal static class Decompiler
    {
        public static int ExportTypes(AssemblyDefinition assembly, LanguageWriter writer, string outputDirectory)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentException("output directory is required", "outputDirectory");
            }

            Directory.CreateDirectory(outputDirectory);
            writer.Runtime = RuntimeProfile.Detect(assembly);
            string extension = GetExtension(writer);
            StringBuilder manifest = new StringBuilder();
            manifest.AppendLine("assembly: " + assembly.Name.FullName);
            manifest.AppendLine("writer: " + writer);
            manifest.AppendLine("dialect: " + (writer.Dialect == null ? "default" : writer.Dialect.DisplayName));
            manifest.AppendLine();

            int count = 0;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    string text;
                    try
                    {
                        text = writer.StripMarkup(writer.WriteType(type));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to render type '" + type.FullName + "'.", ex);
                        continue;
                    }

                    string fileName = SanitizeFileName(type.FullName) + "." + extension;
                    string path = Path.Combine(outputDirectory, fileName);
                    string header = writer.Dialect == null
                        ? string.Empty
                        : (extension == "vb" ? "' " : "// ") + "Dialect: " + writer.Dialect.DisplayName +
                          " (" + writer.Dialect.Toolchain + ")" + Environment.NewLine;

                    try
                    {
                        File.WriteAllText(path, header + text, new UTF8Encoding(false));
                        manifest.AppendLine(fileName);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to write '" + path + "'.", ex);
                    }
                }
            }

            File.WriteAllText(Path.Combine(outputDirectory, "manifest.txt"), manifest.ToString(), new UTF8Encoding(false));
            return count;
        }

        public static string GetExtension(LanguageWriter writer)
        {
            if (writer is VbWriter)
            {
                return "vb";
            }
            if (writer is CSharpWriter)
            {
                return "cs";
            }
            return "il";
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "type";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder text = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                text.Append(Array.IndexOf(invalid, c) >= 0 || c == '`' || c == '/' ? '_' : c);
            }
            string result = text.ToString();
            return result.Length > 180 ? result.Substring(0, 180) : result;
        }
    }
}
