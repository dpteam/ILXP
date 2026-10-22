using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Locates referenced assemblies on disk. The original application only probed the
    /// CLR directory and the GAC; this version also probes the application directory,
    /// the directories of all currently opened assemblies and the runtime folder of the
    /// running CLR, and never throws for malformed assembly names.
    /// </summary>
    public static class AssemblyResolver
    {
        public static string ResolveFromTree(string assemblyName, TreeNodeCollection nodes)
        {
            List<string> directories = new List<string>();
            if (nodes != null)
            {
                foreach (TreeNode node in nodes)
                {
                    AssemblyItem item = node.Tag as AssemblyItem;
                    if (item != null && !string.IsNullOrEmpty(item.FilePath))
                    {
                        string directory = Path.GetDirectoryName(item.FilePath);
                        if (!string.IsNullOrEmpty(directory) && !directories.Contains(directory))
                        {
                            directories.Add(directory);
                        }
                    }
                }
            }
            return Resolve(assemblyName, directories);
        }

        public static string Resolve(string assemblyName, List<string> searchPaths)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException("Invalid assembly name", "assemblyName");
            }

            string name = null;
            string publicKeyToken = null;
            string versionText = null;

            foreach (string part in assemblyName.Split(','))
            {
                string trimmed = part.Trim();
                if (name == null)
                {
                    name = trimmed;
                    continue;
                }

                string[] pair = trimmed.Split('=');
                if (pair.Length != 2)
                {
                    continue;
                }

                if (string.Equals(pair[0], "PublicKeyToken", StringComparison.OrdinalIgnoreCase))
                {
                    publicKeyToken = pair[1];
                }
                else if (string.Equals(pair[0], "Version", StringComparison.OrdinalIgnoreCase))
                {
                    versionText = pair[1];
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid assembly name", "assemblyName");
            }

            List<string> runtimeDirectories = new List<string>();
            string runtimeRoot = Path.GetDirectoryName(Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName));
            if (!string.IsNullOrEmpty(runtimeRoot))
            {
                Version version;
                if (Version.TryParse(versionText ?? string.Empty, out version))
                {
                    switch (version.Major)
                    {
                        case 1:
                            runtimeDirectories.Add(Path.Combine(runtimeRoot, version.Build == 3300 ? "v1.0.3705" : "v1.0.5000.0"));
                            break;
                        case 2:
                            runtimeDirectories.Add(Path.Combine(runtimeRoot, "v2.0.50727"));
                            break;
                        case 4:
                            runtimeDirectories.Add(Path.Combine(runtimeRoot, "v4.0.30319"));
                            break;
                    }
                }

                // Fall back to every installed runtime when the version is unknown.
                if (runtimeDirectories.Count == 0 && Directory.Exists(runtimeRoot))
                {
                    try
                    {
                        runtimeDirectories.AddRange(Directory.GetDirectories(runtimeRoot, "v*"));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to enumerate CLR directories.", ex);
                    }
                }
            }

            foreach (string directory in runtimeDirectories)
            {
                string candidate = ProbeDirectory(directory, name);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            foreach (string gacRoot in GetGacRoots())
            {
                string candidate = ProbeGac(gacRoot, name, versionText, publicKeyToken);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            if (searchPaths != null)
            {
                foreach (string directory in searchPaths)
                {
                    string candidate = ProbeDirectory(directory, name);
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }
            }

            throw new FileNotFoundException("Assembly '" + name + "' was not found.", name + ".dll");
        }

        private static IEnumerable<string> GetGacRoots()
        {
            List<string> roots = new List<string>();
            string windows = Environment.GetEnvironmentVariable("WINDIR");
            if (!string.IsNullOrEmpty(windows))
            {
                roots.Add(Path.Combine(windows, "assembly"));
                roots.Add(Path.Combine(Path.Combine(windows, "Microsoft.NET"), "assembly"));
            }

            string runtimeRoot = Path.GetDirectoryName(Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName));
            if (!string.IsNullOrEmpty(runtimeRoot))
            {
                roots.Add(runtimeRoot);
            }
            return roots;
        }

        private static string ProbeGac(string gacRoot, string name, string versionText, string publicKeyToken)
        {
            string[] buckets = { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
            string[] prefixes = { string.Empty, "v4.0_" };

            foreach (string prefix in prefixes)
            {
                foreach (string bucket in buckets)
                {
                    string basePath = Path.Combine(gacRoot, bucket);
                    if (!Directory.Exists(basePath))
                    {
                        continue;
                    }

                    string assemblyDirectory = Path.Combine(basePath, name);
                    if (!Directory.Exists(assemblyDirectory))
                    {
                        continue;
                    }

                    string folderName = prefix + (versionText ?? string.Empty) + "__" + (publicKeyToken ?? string.Empty);
                    string candidate = ProbeDirectory(Path.Combine(assemblyDirectory, folderName), name);
                    if (candidate != null)
                    {
                        return candidate;
                    }

                    // Last resort: scan the version folders of this assembly.
                    try
                    {
                        foreach (string versionDirectory in Directory.GetDirectories(assemblyDirectory))
                        {
                            candidate = ProbeDirectory(versionDirectory, name);
                            if (candidate != null)
                            {
                                return candidate;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to scan GAC directory '" + assemblyDirectory + "'.", ex);
                    }
                }
            }
            return null;
        }

        private static string ProbeDirectory(string directory, string name)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }

            try
            {
                string dll = Path.Combine(directory, name + ".dll");
                if (File.Exists(dll))
                {
                    return dll;
                }

                string exe = Path.Combine(directory, name + ".exe");
                if (File.Exists(exe))
                {
                    return exe;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to probe directory '" + directory + "'.", ex);
            }
            return null;
        }
    }
}
