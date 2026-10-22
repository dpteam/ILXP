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
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

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

        /// <summary>
        /// Ensures that console output from a WinExe process is visible when the
        /// application was started from a command prompt.
        /// </summary>
        public static bool AttachParentConsole()
        {
            try
            {
                if (HasConsoleWindow())
                {
                    return true;
                }
                return AttachConsole(AttachParentProcess);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void DetachConsole()
        {
            try
            {
                FreeConsole();
            }
            catch (Exception)
            {
            }
        }
    }
}
