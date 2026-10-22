using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Strongly typed access to the bitmap resources that are embedded directly into
    /// the executable. Replaces the generated <c>Resources.resx</c> wrapper of the
    /// original application and avoids binary serialization of the resource table.
    /// </summary>
    internal static class AppResources
    {
        private const string Prefix = "ILXP.Resources.";
        private static readonly object SyncRoot = new object();

        private static Bitmap fileOpen;
        private static Bitmap navBack;
        private static Bitmap navForward;
        private static Bitmap refresh;
        private static Bitmap companyLogo;
        private static Icon appIcon;

        public static Bitmap FileOpen
        {
            get { return GetBitmap("FileOpen.png", ref fileOpen); }
        }

        public static Bitmap NavBack
        {
            get { return GetBitmap("NavBack.png", ref navBack); }
        }

        public static Bitmap NavForward
        {
            get { return GetBitmap("NavForward.png", ref navForward); }
        }

        public static Bitmap Refresh
        {
            get { return GetBitmap("Refresh.png", ref refresh); }
        }

        public static Bitmap CompanyLogo
        {
            get { return GetBitmap("CompanyLogo.png", ref companyLogo); }
        }

        public static Icon AppIcon
        {
            get
            {
                lock (SyncRoot)
                {
                    if (appIcon == null)
                    {
                        using (Stream stream = OpenStream("AppIcon.ico"))
                        {
                            if (stream != null)
                            {
                                appIcon = new Icon(stream);
                            }
                        }
                    }
                    return appIcon;
                }
            }
        }

        /// <summary>
        /// Recreates the tree icon image list. The original application persisted the
        /// list through <see cref="ImageListStreamer"/>; we keep the same payload so
        /// that all image keys stay compatible.
        /// </summary>
        public static ImageListStreamer CreateTreeIconStreamer()
        {
            try
            {
                using (Stream stream = OpenStream("TreeIcons.bin"))
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    return (ImageListStreamer)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to load tree icons.", ex);
                return null;
            }
        }

        private static Bitmap GetBitmap(string name, ref Bitmap cache)
        {
            lock (SyncRoot)
            {
                if (cache == null)
                {
                    using (Stream stream = OpenStream(name))
                    {
                        if (stream != null)
                        {
                            // Copy the bitmap so that the underlying stream can be closed.
                            using (Bitmap loaded = new Bitmap(stream))
                            {
                                cache = new Bitmap(loaded);
                            }
                        }
                    }
                }
                return cache;
            }
        }

        private static Stream OpenStream(string name)
        {
            try
            {
                return typeof(AppResources).Assembly.GetManifestResourceStream(Prefix + name);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to open resource '" + name + "'.", ex);
                return null;
            }
        }
    }
}
