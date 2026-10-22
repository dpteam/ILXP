using System;
using System.Runtime.InteropServices;

namespace ILXP
{
    /// <summary>
    /// Native interop helpers used by the UI layer. Kept in one place so that the
    /// P/Invoke surface of the application stays auditable.
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static bool HasConsoleWindow()
        {
            try
            {
                return GetConsoleWindow() != IntPtr.Zero;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
