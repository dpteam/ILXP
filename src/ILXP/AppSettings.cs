using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ILXP
{
    /// <summary>
    /// Small, dependency-free replacement for the VB "My.Settings" object that the
    /// original application used. Values are stored in a plain INI-style file so that
    /// the application keeps working on locked-down machines and on Windows XP.
    /// </summary>
    internal sealed class AppSettings
    {
        private static readonly object SyncRoot = new object();
        private static AppSettings instance;

        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string filePath;

        private AppSettings()
        {
        }

        public static AppSettings Default
        {
            get
            {
                lock (SyncRoot)
                {
                    if (instance == null)
                    {
                        instance = new AppSettings();
                        instance.Load();
                    }
                    return instance;
                }
            }
        }

        public int SelectedLanguage
        {
            get { return GetInt("SelectedLanguage", 0); }
            set { values["SelectedLanguage"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public string LastFile
        {
            get { return GetString("LastFile", string.Empty); }
            set { values["LastFile"] = value ?? string.Empty; }
        }

        public string LastSelectedNodePath
        {
            get { return GetString("LastSelectedNodePath", string.Empty); }
            set { values["LastSelectedNodePath"] = value ?? string.Empty; }
        }

        /// <summary>Whether the About box should start playing music (default: on).</summary>
        public bool MusicEnabled
        {
            get { return GetBool("MusicEnabled", true); }
            set { values["MusicEnabled"] = value ? "1" : "0"; }
        }

        /// <summary>Identifier of the last played tune (a built-in id or "file:&lt;path&gt;").</summary>
        public string MusicTrack
        {
            get { return GetString("MusicTrack", "E1M1"); }
            set { values["MusicTrack"] = value ?? string.Empty; }
        }

        public float MusicVolume
        {
            get
            {
                float result;
                return float.TryParse(GetString("MusicVolume", "0.55"), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                    ? Math.Max(0f, Math.Min(1f, result))
                    : 0.55f;
            }
            set { values["MusicVolume"] = Math.Max(0f, Math.Min(1f, value)).ToString("0.00", CultureInfo.InvariantCulture); }
        }

        /// <summary>Folders are loaded recursively when true.</summary>
        public bool RecursiveFolders
        {
            get { return GetBool("RecursiveFolders", true); }
            set { values["RecursiveFolders"] = value ? "1" : "0"; }
        }

        /// <summary>
        /// Tree expansion depth used by View → Expand: 0 = collapsed, 1..9 = levels,
        /// -1 = everything the node budget allows.
        /// </summary>
        public int TreeExpandDepth
        {
            get { return GetInt("TreeExpandDepth", 2); }
            set { values["TreeExpandDepth"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>When true, expanding a node may load referenced assemblies from disk.</summary>
        public bool LazyReferences
        {
            get { return GetBool("LazyReferences", true); }
            set { values["LazyReferences"] = value ? "1" : "0"; }
        }

        /// <summary>Show the loading overlay while a slow operation runs.</summary>
        public bool ShowLoadingOverlay
        {
            get { return GetBool("ShowLoadingOverlay", true); }
            set { values["ShowLoadingOverlay"] = value ? "1" : "0"; }
        }

        public bool AutoLoadReferences
        {
            get { return GetBool("AutoLoadReferences", false); }
            set { values["AutoLoadReferences"] = value ? "1" : "0"; }
        }

        public string DecompilerTarget
        {
            get { return GetString("DecompilerTarget", "auto"); }
            set { values["DecompilerTarget"] = value ?? "auto"; }
        }

        public string SettingsPath
        {
            get { return filePath; }
        }

        public void Save()
        {
            lock (SyncRoot)
            {
                string path = ResolveSettingsPath();
                if (path == null)
                {
                    return;
                }

                try
                {
                    StringBuilder text = new StringBuilder();
                    text.AppendLine("; ILXP settings");
                    foreach (KeyValuePair<string, string> pair in values)
                    {
                        text.Append(pair.Key).Append('=').AppendLine(pair.Value.Replace("\r", " ").Replace("\n", " "));
                    }
                    File.WriteAllText(path, text.ToString(), Encoding.UTF8);
                    filePath = path;
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to save settings to '" + path + "'.", ex);
                }
            }
        }

        /// <summary>Clears every stored value, so the defaults apply again.</summary>
        public void ResetToDefaults()
        {
            lock (SyncRoot)
            {
                values.Clear();
            }
        }

        private void Load()
        {
            string path = ResolveSettingsPath();
            if (path == null)
            {
                return;
            }

            filePath = path;
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    int separator = trimmed.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }
                    values[trimmed.Substring(0, separator).Trim()] = trimmed.Substring(separator + 1);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to read settings from '" + path + "'.", ex);
            }
        }

        private string ResolveSettingsPath()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ILXP", "settings.ini"),
                Path.Combine(Path.GetDirectoryName(typeof(AppSettings).Assembly.Location) ?? ".", "settings.ini"),
                Path.Combine(Path.GetTempPath(), "ILXP", "settings.ini")
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    string directory = Path.GetDirectoryName(candidate);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return candidate;
                }
                catch (Exception ex)
                {
                    Log.Warn("Settings path unavailable: '" + candidate + "'.", ex);
                }
            }
            return null;
        }

        private string GetString(string key, string fallback)
        {
            string value;
            return values.TryGetValue(key, out value) && value != null ? value : fallback;
        }

        private int GetInt(string key, int fallback)
        {
            int result;
            return int.TryParse(GetString(key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
                ? result
                : fallback;
        }

        private bool GetBool(string key, bool fallback)
        {
            string value = GetString(key, string.Empty);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
