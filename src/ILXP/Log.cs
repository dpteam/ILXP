using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ILXP
{
    /// <summary>
    /// Central logging facility. Everything goes through <see cref="Trace"/> so that
    /// the log can be redirected by any standard listener, but the application also
    /// installs its own file listener (and an optional console listener).
    /// </summary>
    internal static class Log
    {
        private const string SourceName = "ILXP";
        private static readonly object SyncRoot = new object();
        private static TraceSource source;
        private static bool initialized;

        public static void Initialize(string logDirectory = null)
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                try
                {
                    source = new TraceSource(SourceName, SourceLevels.All);
                    source.Listeners.Clear();

                    string directory = ResolveLogDirectory(logDirectory);
                    if (directory != null)
                    {
                        string path = Path.Combine(directory, "ILXP.log");
                        try
                        {
                            StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false));
                            writer.AutoFlush = true;
                            TraceListener fileListener = new TextWriterTraceListener(writer);
                            source.Listeners.Add(fileListener);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ILXP: cannot open log file: " + ex.Message);
                        }
                    }

                    // Only mirror to the console when one is actually attached,
                    // otherwise WinExe applications would allocate a hidden window.
                    if (NativeMethods.HasConsoleWindow())
                    {
                        source.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    }

                    Trace.AutoFlush = true;
                    initialized = true;
                    Info("Logger initialized");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ILXP: logger initialization failed: " + ex);
                }
            }
        }

        private static string ResolveLogDirectory(string preferred)
        {
            string[] candidates =
            {
                preferred,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ILXP", "logs"),
                Path.Combine(Path.GetDirectoryName(typeof(Log).Assembly.Location) ?? ".", "logs"),
                Path.Combine(Path.GetTempPath(), "ILXP", "logs")
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }
                try
                {
                    Directory.CreateDirectory(candidate);
                    return candidate;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ILXP: cannot use log directory '" + candidate + "': " + ex.Message);
                }
            }
            return null;
        }

        private static void Write(TraceEventType type, string message, Exception exception)
        {
            Initialize();
            StringBuilder text = new StringBuilder();
            text.Append(message);
            if (exception != null)
            {
                text.AppendLine();
                text.Append(exception);
            }
            try
            {
                if (source != null)
                {
                    source.TraceEvent(type, 0, text.ToString());
                    source.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ILXP: logging failed: " + ex.Message);
            }
        }

        public static void Info(string message)
        {
            Write(TraceEventType.Information, message, null);
        }

        public static void Warn(string message, Exception exception = null)
        {
            Write(TraceEventType.Warning, message, exception);
        }

        public static void Error(string message, Exception exception = null)
        {
            Write(TraceEventType.Error, message, exception);
        }

        public static void Verbose(string message)
        {
            Write(TraceEventType.Verbose, message, null);
        }
    }
}
