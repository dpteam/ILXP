using System;
using System.Reflection;

namespace ILXP
{
    /// <summary>
    /// Static product information. Replaces the VB <c>My.Application.Info</c> usage
    /// of the original application.
    /// </summary>
    internal static class AppInfo
    {
        public const string ProductName = "ILXP";
        public const string Description = ".NET decompiler and IL analysis tool";

        public static Version Version
        {
            get
            {
                Version version = typeof(AppInfo).Assembly.GetName().Version;
                return version ?? new Version(1, 0, 0, 0);
            }
        }

        public static string VersionText
        {
            get { return Version.ToString(3); }
        }

        public static string Title
        {
            get { return ProductName + " " + VersionText; }
        }

        public static string Copyright
        {
            get
            {
                object[] attributes = typeof(AppInfo).Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length > 0)
                {
                    return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                }
                return string.Empty;
            }
        }
    }
}
