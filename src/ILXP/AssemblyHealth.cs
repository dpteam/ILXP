using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>One reference of the analysed assembly, with its resolution status.</summary>
    internal sealed class ReferenceStatus
    {
        public string Name;
        public string Version;
        public string ResolvedPath;
        public string Problem;

        public bool IsResolved
        {
            get { return ResolvedPath != null && Problem == null; }
        }

        public override string ToString()
        {
            return IsResolved
                ? Name + " " + Version + "  ->  " + ResolvedPath
                : Name + " " + Version + "  [" + (Problem ?? "not found") + "]";
        }
    }

    /// <summary>Everything the health report shows about one assembly.</summary>
    internal sealed class AssemblyHealthReport
    {
        public AssemblyHealthReport()
        {
            References = new List<ReferenceStatus>();
            Findings = new List<string>();
            Warnings = new List<string>();
        }

        public string Path;
        public string FullName;
        public string ModuleName;
        public RuntimeProfile Runtime;
        public PeInspector.PeInfo Pe;
        public FingerprintReport Fingerprint;

        public int TypeCount;
        public int MethodCount;
        public int FieldCount;
        public int ResourceCount;
        public int CustomAttributeCount;
        public int GenericTypeCount;
        public int AsyncMethodCount;
        public int PInvokeCount;

        public List<ReferenceStatus> References { get; private set; }
        public List<string> Findings { get; private set; }
        public List<string> Warnings { get; private set; }

        /// <summary>0..100, higher is healthier.</summary>
        public int HealthScore
        {
            get
            {
                double score = 100;

                // Missing dependencies are the single most damaging problem for analysis.
                foreach (ReferenceStatus reference in References)
                {
                    if (!reference.IsResolved)
                    {
                        score -= 8;
                    }
                }

                if (Pe != null && !string.IsNullOrEmpty(Pe.Error))
                {
                    score -= 30;
                }

                if (Fingerprint != null)
                {
                    score -= Fingerprint.Indicators.ObfuscationScore * 0.45;
                }

                if (Fingerprint != null && !Fingerprint.Indicators.HasPdb)
                {
                    score -= 3;
                }

                if (score < 0)
                {
                    score = 0;
                }
                return (int)Math.Round(score);
            }
        }

        public string Verdict
        {
            get
            {
                int score = HealthScore;
                if (score >= 85) return "healthy";
                if (score >= 65) return "fair";
                if (score >= 40) return "degraded";
                return "poor";
            }
        }
    }

    /// <summary>
    /// Builds the assembly health report: PE facts, metadata counts, dependency resolution,
    /// obfuscation fingerprint and a short list of findings.
    /// </summary>
    internal static class AssemblyHealth
    {
        public static AssemblyHealthReport Analyse(AssemblyDefinition assembly, string path)
        {
            AssemblyHealthReport report = new AssemblyHealthReport
            {
                Path = path,
                FullName = assembly.Name.FullName,
                ModuleName = assembly.MainModule.Name,
                Runtime = RuntimeProfile.Detect(assembly)
            };

            try
            {
                report.Pe = PeInspector.Inspect(path);
            }
            catch (Exception ex)
            {
                report.Warnings.Add("PE inspection failed: " + ex.Message);
            }

            try
            {
                report.Fingerprint = ObfuscatorFingerprint.Analyse(assembly, path);
            }
            catch (Exception ex)
            {
                report.Warnings.Add("Fingerprint failed: " + ex.Message);
            }

            try
            {
                CountMetadata(assembly, report);
            }
            catch (Exception ex)
            {
                report.Warnings.Add("Metadata scan failed: " + ex.Message);
            }

            try
            {
                ResolveReferences(assembly, path, report);
            }
            catch (Exception ex)
            {
                report.Warnings.Add("Reference resolution failed: " + ex.Message);
            }

            BuildFindings(report);
            return report;
        }

        private static void CountMetadata(AssemblyDefinition assembly, AssemblyHealthReport report)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                report.ResourceCount += module.Resources.Count;
                report.CustomAttributeCount += module.CustomAttributes.Count;

                foreach (TypeDefinition type in module.Types)
                {
                    CountType(type, report);
                }
            }
        }

        private static void CountType(TypeDefinition type, AssemblyHealthReport report)
        {
            if (type.Name != "<Module>")
            {
                report.TypeCount++;
                if (type.HasGenericParameters)
                {
                    report.GenericTypeCount++;
                }
                report.CustomAttributeCount += type.CustomAttributes.Count;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                report.MethodCount++;
                report.CustomAttributeCount += method.CustomAttributes.Count;

                if (method.IsPInvokeImpl)
                {
                    report.PInvokeCount++;
                }

                // Async methods are marked with AsyncStateMachineAttribute.
                foreach (CustomAttribute attribute in method.CustomAttributes)
                {
                    if (attribute.AttributeType != null &&
                        attribute.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                    {
                        report.AsyncMethodCount++;
                        break;
                    }
                }
            }

            report.FieldCount += type.Fields.Count;

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                CountType(nested, report);
            }
        }

        private static void ResolveReferences(AssemblyDefinition assembly, string path, AssemblyHealthReport report)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> searchPaths = new List<string>();

            string directory = null;
            try
            {
                directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
            }
            catch (Exception)
            {
            }
            if (!string.IsNullOrEmpty(directory))
            {
                searchPaths.Add(directory);
            }

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (!seen.Add(reference.FullName))
                    {
                        continue;
                    }

                    ReferenceStatus status = new ReferenceStatus
                    {
                        Name = reference.Name,
                        Version = reference.Version == null ? "?" : reference.Version.ToString()
                    };

                    try
                    {
                        status.ResolvedPath = AssemblyResolver.Resolve(reference.FullName, searchPaths);
                    }
                    catch (Exception ex)
                    {
                        status.Problem = ex is System.IO.FileNotFoundException ? "not found" : ex.Message;
                    }

                    report.References.Add(status);
                }
            }
        }

        private static void BuildFindings(AssemblyHealthReport report)
        {
            int missing = 0;
            foreach (ReferenceStatus reference in report.References)
            {
                if (!reference.IsResolved)
                {
                    missing++;
                }
            }

            if (missing == 0 && report.References.Count > 0)
            {
                report.Findings.Add("all " + report.References.Count + " references resolve");
            }
            else if (missing > 0)
            {
                report.Findings.Add(missing + " of " + report.References.Count + " references are missing");
            }

            if (report.Pe != null && report.Pe.IsValid)
            {
                if (!report.Pe.IsManaged)
                {
                    report.Findings.Add("not a managed assembly (no CLI header)");
                }
                if (!report.Pe.IsIlOnly)
                {
                    report.Findings.Add("mixed-mode assembly (contains native code)");
                }
                if (report.Pe.Requires32Bit)
                {
                    report.Findings.Add("32-bit only (CorFlags 32BITREQUIRED)");
                }
                if (report.Pe.Prefers32Bit)
                {
                    report.Findings.Add("32-bit preferred (CorFlags 32BITPREFERRED)");
                }
                if (report.Pe.HasStrongName)
                {
                    report.Findings.Add("strong-name signed");
                }
                if (!string.IsNullOrEmpty(report.Pe.ClrVersion))
                {
                    report.Findings.Add("CLR header " + report.Pe.ClrVersion);
                }

                // Writable + executable section is a strong indicator of self-modifying code
                // or a packed stub.
                foreach (PeSection section in report.Pe.Sections)
                {
                    if (section.IsExecutable && section.IsWritable)
                    {
                        report.Findings.Add("section " + section.Name + " is writable and executable");
                    }
                }
            }

            if (report.Fingerprint != null)
            {
                FingerprintMatch best = report.Fingerprint.Best;
                if (best != null)
                {
                    report.Findings.Add("protector candidate: " + best.Name + " (score " + best.Score + ")");
                }
                report.Findings.Add("obfuscation: " + report.Fingerprint.Indicators.Verdict +
                                    " (" + report.Fingerprint.Indicators.ObfuscationScore + "/100)");
            }

            if (report.AsyncMethodCount > 0)
            {
                report.Findings.Add(report.AsyncMethodCount + " async method(s)");
            }
            if (report.PInvokeCount > 0)
            {
                report.Findings.Add(report.PInvokeCount + " P/Invoke declaration(s)");
            }
            if (report.GenericTypeCount > 0)
            {
                report.Findings.Add(report.GenericTypeCount + " generic type(s)");
            }
        }

        public static string Format(AssemblyHealthReport report)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("assembly      : " + report.FullName);
            text.AppendLine("path          : " + report.Path);
            text.AppendLine("runtime       : " + report.Runtime);
            text.AppendLine(string.Format("health        : {0}/100 ({1})", report.HealthScore, report.Verdict));
            text.AppendLine();

            if (report.Pe != null && report.Pe.IsValid)
            {
                text.AppendLine("PE            : " + report.Pe.Machine + (report.Pe.Is64Bit ? " (PE32+)" : " (PE32)") +
                                ", " + FormatSize(report.Pe.FileSize));
                text.AppendLine(string.Format("entropy       : {0:0.00} bits/byte (whole file)", report.Pe.FileEntropy));
                text.AppendLine("timestamp     : " + FormatTimestamp(report.Pe.Timestamp));
                text.AppendLine("sections      :");
                foreach (PeSection section in report.Pe.Sections)
                {
                    text.AppendLine(string.Format("  {0,-8} {1,10} bytes  entropy {2,5:0.00}  {3}{4}",
                        section.Name, section.RawSize, section.Entropy,
                        section.IsExecutable ? "X" : "-", section.IsWritable ? "W" : "-"));
                }
                text.AppendLine();
            }
            else if (report.Pe != null)
            {
                text.AppendLine("PE            : " + (report.Pe.Error ?? "unavailable"));
                text.AppendLine();
            }

            text.AppendLine("types         : " + report.TypeCount);
            text.AppendLine("methods       : " + report.MethodCount);
            text.AppendLine("fields        : " + report.FieldCount);
            text.AppendLine("resources     : " + report.ResourceCount);
            text.AppendLine("attributes    : " + report.CustomAttributeCount);
            text.AppendLine();

            text.AppendLine("references    : " + (report.References.Count == 0 ? "(none)" : report.References.Count.ToString(CultureInfo.InvariantCulture)));
            foreach (ReferenceStatus reference in report.References)
            {
                text.AppendLine("  " + reference);
            }
            text.AppendLine();

            if (report.Fingerprint != null)
            {
                text.Append(ObfuscatorFingerprint.Format(report.Fingerprint));
                text.AppendLine();
            }

            text.AppendLine("findings      :");
            foreach (string finding in report.Findings)
            {
                text.AppendLine("  - " + finding);
            }
            foreach (string warning in report.Warnings)
            {
                text.AppendLine("  ! " + warning);
            }

            return text.ToString();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return string.Format("{0:0.0} MB", bytes / (1024.0 * 1024.0));
            }
            if (bytes >= 1024)
            {
                return string.Format("{0:0.0} KB", bytes / 1024.0);
            }
            return bytes + " bytes";
        }

        private static string FormatTimestamp(uint timestamp)
        {
            try
            {
                DateTime when = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(timestamp)
                    .ToLocalTime();

                // Deterministic builds (Roslyn /dotnet) replace the timestamp with a content
                // hash, which decodes to a date far in the future or past.
                if (when.Year < 1990 || when.Year > 2038)
                {
                    return "0x" + timestamp.ToString("X8", CultureInfo.InvariantCulture) + "  (deterministic build marker)";
                }

                return when.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "0x" + timestamp.ToString("X8", CultureInfo.InvariantCulture);
            }
        }
    }
}
