using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP
{
    /// <summary>A single match found while searching IL text.</summary>
    internal sealed class IlMatch
    {
        public string TypeName;
        public string MethodName;
        public string MethodPath;
        public int Line;
        public int Offset;
        public string Text;
        public string Field;

        public string Display
        {
            get
            {
                return string.Format("{0}::{1}  IL_{2:x4}  {3}",
                    TypeName, MethodName, Offset, Text == null ? string.Empty : Text.Trim());
            }
        }
    }

    /// <summary>
    /// Regex search over the rendered IL of every method in an assembly.
    ///
    /// The search works on the same text the editor shows (produced by <see cref="IlWriter"/>
    /// and stripped of markup), so results line up with what the user sees. When a match
    /// lands on an instruction line, the IL offset is extracted from its <c>L_xxxx:</c> label
    /// so the GUI can jump straight to the line.
    /// </summary>
    internal static class IlSearch
    {
        private static readonly Regex LabelPattern = new Regex(
            @"^\s*L_([0-9a-fA-F]{4,8}):",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public sealed class Options
        {
            public bool IgnoreCase = true;
            public bool UseRegex = true;
            public int MaxResults = 500;
            public bool SearchFields = true;
            public bool SearchTypes = true;
        }

        public static List<IlMatch> Search(AssemblyDefinition assembly, string query, Options options)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            if (options == null)
            {
                options = new Options();
            }

            List<IlMatch> results = new List<IlMatch>();
            if (string.IsNullOrEmpty(query))
            {
                return results;
            }

            Regex pattern;
            try
            {
                RegexOptions regexOptions = RegexOptions.CultureInvariant;
                if (options.IgnoreCase)
                {
                    regexOptions |= RegexOptions.IgnoreCase;
                }
                pattern = options.UseRegex
                    ? new Regex(query, regexOptions)
                    : new Regex(Regex.Escape(query), regexOptions);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Invalid regular expression: " + ex.Message, "query", ex);
            }

            IlWriter writer = new IlWriter();
            string assemblyKey = assembly.Name.FullName;

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }

                    if (options.SearchTypes)
                    {
                        Match typeMatch = pattern.Match(type.FullName);
                        if (typeMatch.Success)
                        {
                            results.Add(new IlMatch
                            {
                                TypeName = type.FullName,
                                MethodName = string.Empty,
                                MethodPath = assemblyKey + "//N" + type.Namespace + "//" + type.Name,
                                Line = 0,
                                Offset = 0,
                                Text = type.FullName,
                                Field = "type"
                            });
                        }
                    }

                    if (options.SearchFields)
                    {
                        foreach (FieldDefinition field in type.Fields)
                        {
                            string line = field.FieldType.FullName + " " + field.Name;
                            if (pattern.IsMatch(line))
                            {
                                results.Add(new IlMatch
                                {
                                    TypeName = type.FullName,
                                    MethodName = field.Name,
                                    MethodPath = assemblyKey + "//N" + type.Namespace + "//" + type.Name + "//F" + field.Name,
                                    Line = 0,
                                    Offset = 0,
                                    Text = line,
                                    Field = "field"
                                });
                            }
                        }
                    }

                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (results.Count >= options.MaxResults)
                        {
                            return results;
                        }

                        string il;
                        try
                        {
                            il = writer.StripMarkup(writer.WriteMethod(method));
                        }
                        catch (Exception ex)
                        {
                            Log.Verbose("IL search skipped '" + type.FullName + "::" + method.Name + "': " + ex.Message);
                            continue;
                        }

                        string methodPath = assemblyKey + "//N" + type.Namespace + "//" + type.Name + "//M" +
                                            LanguageWriter.GetMethodSignature(method);

                        string[] lines = il.Replace("\r\n", "\n").Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            Match match = pattern.Match(lines[i]);
                            if (!match.Success)
                            {
                                continue;
                            }

                            int offset = 0;
                            Match label = LabelPattern.Match(lines[i]);
                            if (label.Success)
                            {
                                int.TryParse(label.Groups[1].Value, NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture, out offset);
                            }

                            results.Add(new IlMatch
                            {
                                TypeName = type.FullName,
                                MethodName = method.Name,
                                MethodPath = methodPath,
                                Line = i,
                                Offset = offset,
                                Text = lines[i],
                                Field = "il"
                            });

                            if (results.Count >= options.MaxResults)
                            {
                                return results;
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>Formats results as plain text (CLI output and clipboard).</summary>
        public static string Format(List<IlMatch> matches)
        {
            StringBuilder text = new StringBuilder();
            foreach (IlMatch match in matches)
            {
                text.Append(match.Display).AppendLine();
            }
            return text.ToString();
        }
    }
}
