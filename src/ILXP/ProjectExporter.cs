using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>What a project export produced.</summary>
    internal sealed class ProjectExportResult
    {
        public string OutputDirectory;
        public string ProjectPath;
        public string Language;
        public int Types;
        public int SourceFiles;
        public int Resources;
        public int BodyFailures;
        public bool HasBodies;
        public readonly List<string> Warnings = new List<string>();

        public string Summary()
        {
            StringBuilder text = new StringBuilder();
            text.Append(Types.ToString(CultureInfo.InvariantCulture)).Append(" types, ")
                .Append(SourceFiles.ToString(CultureInfo.InvariantCulture)).Append(" source files");
            if (Resources > 0)
            {
                text.Append(", ").Append(Resources.ToString(CultureInfo.InvariantCulture)).Append(" resources");
            }
            if (!HasBodies)
            {
                text.Append(" (signatures only)");
            }
            if (BodyFailures > 0)
            {
                text.Append(", ").Append(BodyFailures.ToString(CultureInfo.InvariantCulture))
                    .Append(" bodies could not be rendered");
            }
            return text.ToString();
        }
    }

    /// <summary>
    /// Exports a decompiled assembly as a ready-to-open project: a project file with the right
    /// target framework and references, one source file per top-level type (wrapped in its
    /// namespace), an AssemblyInfo file, the embedded resources and a README that states what
    /// the export can and cannot promise.
    ///
    /// This is the same idea as "Export to project" in ILSpy/dnSpy: the result is a starting
    /// point for patching and recompiling, not a byte-perfect reconstruction.
    /// </summary>
    internal static class ProjectExporter
    {
        public static LanguageWriter CreateWriter(string language)
        {
            switch (language)
            {
                case "il":
                    return new IlWriter();
                case "vb":
                    return new VbWriter();
                default:
                    return new CSharpWriter();
            }
        }

        /// <summary>File extension used for a language id ("cs", "vb", "il").</summary>
        public static string Extension(string language)
        {
            switch (language)
            {
                case "vb":
                    return ".vb";
                case "il":
                    return ".il";
                default:
                    return ".cs";
            }
        }

        public static ProjectExportResult Export(AssemblyItem item, string outputDirectory,
            string language, bool includeBodies, OutputDialect dialect)
        {
            if (item == null || item.Assembly == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentException("An output directory is required.", "outputDirectory");
            }

            AssemblyDefinition assembly = item.Assembly;
            ProjectExportResult result = new ProjectExportResult
            {
                OutputDirectory = outputDirectory,
                Language = language,
                HasBodies = includeBodies
            };

            Directory.CreateDirectory(outputDirectory);

            LanguageWriter writer = CreateWriter(language);
            if (dialect != null)
            {
                writer.Dialect = dialect;
            }

            bool previous = LanguageWriter.EmitBodies;
            LanguageWriter.EmitBodies = includeBodies;
            try
            {
                ExporterContext context = new ExporterContext
                {
                    Assembly = assembly,
                    Writer = writer,
                    Directory = outputDirectory,
                    Extension = Extension(language),
                    Result = result
                };

                ExportTypes(context);
                ExportAssemblyInfo(context);
                ExportResources(context);
                WriteProjectFile(context);
                WriteReadme(context);
                WriteManifest(context);
            }
            finally
            {
                LanguageWriter.EmitBodies = previous;
            }

            return result;
        }

        private sealed class ExporterContext
        {
            public AssemblyDefinition Assembly;
            public LanguageWriter Writer;
            public string Directory;
            public string Extension;
            public ProjectExportResult Result;
            public readonly List<string> SourceFiles = new List<string>();
        }

        private static void ExportTypes(ExporterContext context)
        {
            foreach (ModuleDefinition module in context.Assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>" || LanguageWriter.IsCompilerGeneratedName(type.Name))
                    {
                        continue;
                    }

                    string text;
                    try
                    {
                        text = context.Writer.StripMarkup(context.Writer.WriteType(type));
                    }
                    catch (Exception ex)
                    {
                        context.Result.BodyFailures++;
                        context.Result.Warnings.Add("type " + type.FullName + ": " + ex.Message);
                        Log.Warn("Unable to render type '" + type.FullName + "'.", ex);
                        continue;
                    }

                    string fileName = SanitizeFileName(type.Name) + context.Extension;
                    string relative = BuildRelativePath(type.Namespace, fileName);
                    string full = Path.Combine(context.Directory, relative);

                    string directory = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(full, WrapInNamespace(type.Namespace, text, context), Encoding.UTF8);
                    context.SourceFiles.Add(relative.Replace('\\', '/'));
                    context.Result.Types++;
                    context.Result.SourceFiles++;
                }
            }
        }

        /// <summary>Places a type file into folders that mirror its namespace.</summary>
        private static string BuildRelativePath(string namespaceName, string fileName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                return fileName;
            }

            string[] parts = namespaceName.Split('.');
            StringBuilder path = new StringBuilder();
            foreach (string part in parts)
            {
                path.Append(SanitizeFileName(part)).Append(Path.DirectorySeparatorChar);
            }
            return path.Append(fileName).ToString();
        }

        private static string WrapInNamespace(string namespaceName, string typeText, ExporterContext context)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("// Decompiled by ILXP from " + context.Assembly.Name.Name +
                            ". Starting point for patching, not a guaranteed rebuild.");
            text.AppendLine();

            if (string.IsNullOrEmpty(namespaceName))
            {
                foreach (string line in typeText.TrimEnd().Split('\n'))
                {
                    AppendIndented(text, line, string.Empty);
                }
                return text.ToString();
            }

            text.Append("namespace ").AppendLine(namespaceName);
            text.AppendLine("{");
            foreach (string line in typeText.TrimEnd().Split('\n'))
            {
                AppendIndented(text, line, "    ");
            }
            text.AppendLine("}");
            return text.ToString();
        }

        /// <summary>
        /// Indents one generated line and puts an opening brace on its own line. The writers
        /// emit <c>class Foo{</c> for compactness; a project should read like normal source.
        /// </summary>
        private static void AppendIndented(StringBuilder text, string line, string indent)
        {
            string trimmed = line.TrimEnd('\r', ' ', '\t');

            if (trimmed.Length > 1 && trimmed[trimmed.Length - 1] == '{')
            {
                string head = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
                if (head.Length > 0)
                {
                    text.Append(indent).AppendLine(head);
                    text.Append(indent).AppendLine("{");
                    return;
                }
            }

            if (trimmed.Length == 0)
            {
                text.AppendLine();
                return;
            }

            text.Append(indent).AppendLine(trimmed);
        }

        private static void ExportAssemblyInfo(ExporterContext context)
        {
            string directory = Path.Combine(context.Directory, "Properties");
            Directory.CreateDirectory(directory);

            AssemblyNameDefinition name = context.Assembly.Name;
            List<string> lines = new List<string>();
            lines.Add("// Assembly attributes recovered from the original metadata.");
            lines.Add("using System.Reflection;");
            lines.Add("using System.Runtime.InteropServices;");
            lines.Add(string.Empty);
            lines.Add("[assembly: AssemblyTitle(" + Quote(AttributeValue(context.Assembly, "AssemblyTitleAttribute") ?? name.Name) + ")]");
            lines.Add("[assembly: AssemblyDescription(" + Quote(AttributeValue(context.Assembly, "AssemblyDescriptionAttribute") ?? string.Empty) + ")]");
            lines.Add("[assembly: AssemblyCompany(" + Quote(AttributeValue(context.Assembly, "AssemblyCompanyAttribute") ?? string.Empty) + ")]");
            lines.Add("[assembly: AssemblyProduct(" + Quote(AttributeValue(context.Assembly, "AssemblyProductAttribute") ?? string.Empty) + ")]");
            lines.Add("[assembly: AssemblyCopyright(" + Quote(AttributeValue(context.Assembly, "AssemblyCopyrightAttribute") ?? string.Empty) + ")]");
            lines.Add("[assembly: AssemblyVersion(" + Quote(name.Version.ToString()) + ")]");
            lines.Add("[assembly: AssemblyFileVersion(" + Quote(name.Version.ToString()) + ")]");

            bool hasGuid = AttributeValue(context.Assembly, "GuidAttribute") != null;
            if (!hasGuid)
            {
                lines.Add("[assembly: ComVisible(false)]");
            }

            File.WriteAllText(Path.Combine(directory, "AssemblyInfo" + context.Extension),
                string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine, Encoding.UTF8);
        }

        private static string AttributeValue(AssemblyDefinition assembly, string attributeName)
        {
            foreach (CustomAttribute attribute in assembly.CustomAttributes)
            {
                if (attribute.ConstructorArguments.Count != 1)
                {
                    continue;
                }
                if (!string.Equals(attribute.AttributeType.Name, attributeName, StringComparison.Ordinal))
                {
                    continue;
                }

                object value = attribute.ConstructorArguments[0].Value;
                return value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            return null;
        }

        private static string Quote(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static void ExportResources(ExporterContext context)
        {
            foreach (ModuleDefinition module in context.Assembly.Modules)
            {
                foreach (Resource resource in module.Resources)
                {
                    EmbeddedResource embedded = resource as EmbeddedResource;
                    if (embedded == null)
                    {
                        context.Result.Warnings.Add("resource " + resource.Name + " is not embedded; skipped");
                        continue;
                    }

                    string directory = Path.Combine(context.Directory, "Resources");
                    Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, SanitizeFileName(resource.Name));
                    File.WriteAllBytes(path, embedded.GetResourceData());
                    context.Result.Resources++;
                }
            }
        }

        /// <summary>Writes the project file with target framework, output type and references.</summary>
        private static void WriteProjectFile(ExporterContext context)
        {
            bool isVb = context.Extension == ".vb";
            string projectName = SanitizeFileName(context.Assembly.Name.Name);
            string extension = isVb ? ".vbproj" : ".csproj";
            bool isLibrary = true;
            foreach (ModuleDefinition module in context.Assembly.Modules)
            {
                if (module.EntryPoint != null)
                {
                    isLibrary = false;
                }
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            text.AppendLine("<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            text.AppendLine("  <PropertyGroup>");
            text.AppendLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
            text.AppendLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
            text.AppendLine("    <ProjectGuid>{" + StableGuid(context.Assembly.Name.FullName) + "}</ProjectGuid>");
            text.AppendLine("    <OutputType>" + (isLibrary ? "Library" : "WinExe") + "</OutputType>");
            if (isVb)
            {
                text.AppendLine("    <RootNamespace>" + (context.Assembly.Name.Name ?? projectName) + "</RootNamespace>");
            }
            text.AppendLine("    <AssemblyName>" + projectName + "</AssemblyName>");
            text.AppendLine("    <TargetFrameworkVersion>" + TargetFrameworkVersion(context.Assembly) + "</TargetFrameworkVersion>");
            text.AppendLine("    <FileAlignment>512</FileAlignment>");
            text.AppendLine("  </PropertyGroup>");
            text.AppendLine("  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">");
            text.AppendLine("    <DebugSymbols>true</DebugSymbols>");
            text.AppendLine("    <DebugType>full</DebugType>");
            text.AppendLine("    <Optimize>false</Optimize>");
            text.AppendLine("    <OutputPath>bin\\Debug\\</OutputPath>");
            text.AppendLine("    <DefineConstants>DEBUG;TRACE</DefineConstants>");
            text.AppendLine("  </PropertyGroup>");
            text.AppendLine("  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \">");
            text.AppendLine("    <DebugType>pdbonly</DebugType>");
            text.AppendLine("    <Optimize>true</Optimize>");
            text.AppendLine("    <OutputPath>bin\\Release\\</OutputPath>");
            text.AppendLine("    <DefineConstants>TRACE</DefineConstants>");
            text.AppendLine("  </PropertyGroup>");

            text.AppendLine("  <ItemGroup>");
            foreach (AssemblyNameReference reference in context.Assembly.MainModule.AssemblyReferences)
            {
                text.Append("    <Reference Include=\"").Append(reference.Name).Append('"');
                string hint = ResolveHintPath(reference);
                if (hint != null)
                {
                    text.AppendLine(">");
                    text.AppendLine("      <HintPath>" + hint + "</HintPath>");
                    text.AppendLine("      <Private>False</Private>");
                    text.AppendLine("    </Reference>");
                }
                else
                {
                    text.AppendLine(" />");
                }
            }
            text.AppendLine("  </ItemGroup>");

            text.AppendLine("  <ItemGroup>");
            foreach (string file in context.SourceFiles)
            {
                text.Append("    <Compile Include=\"").Append(file).AppendLine("\" />");
            }
            text.AppendLine("  </ItemGroup>");

            text.AppendLine("  <ItemGroup>");
            text.AppendLine("    <Compile Include=\"Properties\\AssemblyInfo" + context.Extension + "\" />");
            text.AppendLine("  </ItemGroup>");

            if (context.Result.Resources > 0)
            {
                text.AppendLine("  <ItemGroup>");
                text.AppendLine("    <None Include=\"Resources\\**\\*\" />");
                text.AppendLine("  </ItemGroup>");
            }

            text.AppendLine("  <Import Project=\"$(MSBuildToolsPath)\\Microsoft." + (isVb ? "VisualBasic" : "CSharp") +
                            ".targets\" />");
            text.AppendLine("</Project>");

            context.Result.ProjectPath = Path.Combine(context.Directory, projectName + extension);
            File.WriteAllText(context.Result.ProjectPath, text.ToString(), Encoding.UTF8);
        }

        /// <summary>Best-effort HintPath for a reference, so the project opens pre-bound.</summary>
        private static string ResolveHintPath(AssemblyNameReference reference)
        {
            try
            {
                string path = AssemblyResolver.Resolve(reference.FullName, new List<string>
                {
                    Path.GetDirectoryName(typeof(ProjectExporter).Assembly.Location) ?? "."
                });
                return string.IsNullOrEmpty(path) ? null : path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string TargetFrameworkVersion(AssemblyDefinition assembly)
        {
            foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences)
            {
                if (!string.Equals(reference.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int major = reference.Version.Major;
                int minor = reference.Version.Minor;
                if (major >= 4)
                {
                    return "v4.0";
                }
                if (major == 3)
                {
                    return "v3.5";
                }
                if (major == 2)
                {
                    return "v2.0";
                }
                return "v" + major.ToString(CultureInfo.InvariantCulture) + "." +
                       minor.ToString(CultureInfo.InvariantCulture);
            }
            return "v4.0";
        }

        /// <summary>Deterministic project GUID derived from the assembly identity.</summary>
        private static string StableGuid(string seed)
        {
            if (string.IsNullOrEmpty(seed))
            {
                seed = "ILXP";
            }

            byte[] hash = System.Security.Cryptography.MD5.Create()
                .ComputeHash(Encoding.UTF8.GetBytes(seed));
            return new Guid(hash).ToString("D").ToUpperInvariant();
        }

        private static void WriteReadme(ExporterContext context)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# " + context.Assembly.Name.Name + " (decompiled by ILXP)");
            text.AppendLine();
            text.AppendLine("Source: `" + context.Assembly.Name.FullName + "`");
            text.AppendLine();
            text.AppendLine("Exported: " + context.Result.Summary());
            text.AppendLine();
            text.AppendLine("## How to use");
            text.AppendLine();
            text.AppendLine("1. Open `" + Path.GetFileName(context.Result.ProjectPath) + "` in Visual Studio or MSBuild.");
            text.AppendLine("2. Restore the NuGet packages the original referenced, if any.");
            text.AppendLine("3. Expect compiler errors: this is a starting point, not a clean rebuild.");
            text.AppendLine();
            text.AppendLine("## What is faithful");
            text.AppendLine();
            text.AppendLine("- Type, member and namespace structure, including nested types.");
            text.AppendLine("- Method signatures, modifiers, attributes and generic constraints.");
            text.AppendLine("- Embedded resources under `Resources/`.");
            text.AppendLine("- Assembly identity in `Properties/AssemblyInfo" + context.Extension + "`.");
            text.AppendLine();
            text.AppendLine("## What is not");
            text.AppendLine();
            text.AppendLine("- Statement bodies are reconstructed heuristically; some will not compile.");
            text.AppendLine("- Compiler-generated types (closures, state machines) are skipped, so");
            text.AppendLine("  their bodies are missing from the calling methods.");
            text.AppendLine("- Custom attribute named arguments and security declarations are not rebuilt.");
            text.AppendLine("- The original IL is the authoritative artifact; use `--bundle` to keep it.");
            if (context.Result.Warnings.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("## Export warnings");
                text.AppendLine();
                int shown = 0;
                foreach (string warning in context.Result.Warnings)
                {
                    if (shown++ >= 40)
                    {
                        text.AppendLine("- ... " + (context.Result.Warnings.Count - 40) + " more");
                        break;
                    }
                    text.AppendLine("- " + warning);
                }
            }

            File.WriteAllText(Path.Combine(context.Directory, "README.md"), text.ToString(), Encoding.UTF8);
        }

        private static void WriteManifest(ExporterContext context)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("assembly: " + context.Assembly.Name.FullName);
            text.AppendLine("language: " + context.Result.Language);
            text.AppendLine("bodies: " + (context.Result.HasBodies ? "yes" : "no"));
            text.AppendLine("types: " + context.Result.Types.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("resources: " + context.Result.Resources.ToString(CultureInfo.InvariantCulture));
            text.AppendLine();

            foreach (string file in context.SourceFiles)
            {
                text.AppendLine(file);
            }

            File.WriteAllText(Path.Combine(context.Directory, "manifest.txt"), text.ToString(), Encoding.UTF8);
        }

        internal static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "unnamed";
            }

            StringBuilder text = new StringBuilder(name.Length);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                bool bad = false;
                foreach (char candidate in invalid)
                {
                    if (c == candidate)
                    {
                        bad = true;
                        break;
                    }
                }
                text.Append(bad || c == '/' || c == '\\' || c == ':' ? '_' : c);
            }

            string result = text.ToString().Trim();
            return result.Length == 0 ? "unnamed" : result;
        }
    }
}
