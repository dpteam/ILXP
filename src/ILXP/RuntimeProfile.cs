using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Describes the runtime an input assembly was built for. ILXP reads the file format
    /// directly (Mono.Cecil), so assemblies targeting runtimes newer than the one ILXP is
    /// running on can still be decompiled; this profile only affects naming and hints.
    /// </summary>
    public sealed class RuntimeProfile
    {
        private static readonly string[] CoreLibraries =
        {
            "mscorlib", "System.Private.CoreLib", "System.Runtime", "netstandard",
            "System.Core", "System"
        };

        private RuntimeProfile()
        {
            TargetFramework = string.Empty;
            FrameworkName = "unknown";
            CoreLibrary = "mscorlib";
        }

        /// <summary>Value of <c>TargetFrameworkAttribute</c>, e.g. ".NETFramework,Version=v4.0".</summary>
        public string TargetFramework { get; private set; }

        /// <summary>Short human readable name, e.g. ".NET Framework 4.0" or ".NET 8.0".</summary>
        public string FrameworkName { get; private set; }

        /// <summary>Core library the assembly was compiled against.</summary>
        public string CoreLibrary { get; private set; }

        /// <summary>Runtime version string from the metadata header (e.g. "v4.0.30319").</summary>
        public string MetadataRuntime { get; private set; }

        public bool IsModernDotNet
        {
            get
            {
                return string.Equals(CoreLibrary, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                       FrameworkName.StartsWith(".NET ", StringComparison.Ordinal) ||
                       FrameworkName.StartsWith(".NET Core", StringComparison.Ordinal);
            }
        }

        public bool IsNetStandard
        {
            get { return string.Equals(CoreLibrary, "netstandard", StringComparison.OrdinalIgnoreCase); }
        }

        public static RuntimeProfile Detect(AssemblyDefinition assembly)
        {
            RuntimeProfile profile = new RuntimeProfile();
            if (assembly == null)
            {
                return profile;
            }

            try
            {
                profile.MetadataRuntime = assembly.MainModule.Runtime.ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                foreach (CustomAttribute attribute in assembly.CustomAttributes)
                {
                    if (attribute.AttributeType != null &&
                        attribute.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" &&
                        attribute.ConstructorArguments.Count > 0)
                    {
                        profile.TargetFramework = Convert.ToString(attribute.ConstructorArguments[0].Value) ?? string.Empty;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("Unable to read TargetFrameworkAttribute: " + ex.Message);
            }

            profile.CoreLibrary = DetectCoreLibrary(assembly);
            profile.FrameworkName = Describe(profile.TargetFramework, profile.CoreLibrary);
            return profile;
        }

        private static string DetectCoreLibrary(AssemblyDefinition assembly)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (string.Equals(reference.Name, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
                    {
                        return reference.Name;
                    }
                }
            }

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    foreach (string candidate in CoreLibraries)
                    {
                        if (string.Equals(reference.Name, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            return reference.Name;
                        }
                    }
                }
            }
            return "mscorlib";
        }

        private static string Describe(string targetFramework, string coreLibrary)
        {
            if (!string.IsNullOrEmpty(targetFramework))
            {
                int versionIndex = targetFramework.IndexOf("Version=v", StringComparison.OrdinalIgnoreCase);
                string version = versionIndex >= 0 ? targetFramework.Substring(versionIndex + 9).Trim() : string.Empty;

                if (targetFramework.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
                {
                    return ".NET Framework " + version;
                }
                if (targetFramework.StartsWith(".NETStandard", StringComparison.OrdinalIgnoreCase))
                {
                    return ".NET Standard " + version;
                }
                if (targetFramework.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
                {
                    return ".NET " + version;
                }
                return targetFramework;
            }

            if (string.Equals(coreLibrary, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
            {
                return ".NET (modern)";
            }
            if (string.Equals(coreLibrary, "netstandard", StringComparison.OrdinalIgnoreCase))
            {
                return ".NET Standard";
            }
            return ".NET Framework";
        }

        /// <summary>Maps a core-library type name so that the same type is shown consistently.</summary>
        public string NormalizeTypeName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return fullName;
            }

            if (fullName.StartsWith("System.Private.CoreLib", StringComparison.Ordinal))
            {
                return "mscorlib" + fullName.Substring("System.Private.CoreLib".Length);
            }
            return fullName;
        }

        public override string ToString()
        {
            return FrameworkName + " (" + CoreLibrary + ")";
        }

        /// <summary>Known target frameworks for the UI (newest first).</summary>
        public static IList<string> KnownFrameworks
        {
            get
            {
                return new[]
                {
                    ".NET 9.0", ".NET 8.0", ".NET 7.0", ".NET 6.0", ".NET 5.0",
                    ".NET Core 3.1", ".NET Core 3.0", ".NET Core 2.2", ".NET Core 2.0",
                    ".NET Standard 2.1", ".NET Standard 2.0", ".NET Standard 1.6",
                    ".NET Framework 4.8.1", ".NET Framework 4.8", ".NET Framework 4.7.2",
                    ".NET Framework 4.6.2", ".NET Framework 4.5", ".NET Framework 4.0",
                    ".NET Framework 3.5", ".NET Framework 2.0", ".NET Framework 1.1",
                    ".NET Framework 1.0"
                };
            }
        }
    }
}
