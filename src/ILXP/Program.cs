using System;
using System.Threading;
using System.Windows.Forms;

namespace ILXP
{
    internal static class Program
    {
        /// <summary>
        /// Application entry point. Everything that can fail is wrapped so that a
        /// broken input file or a third-party component never takes the whole process
        /// down silently: the error is logged and, when possible, shown to the user.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Log.Initialize();
            Log.Info("ILXP " + AppInfo.VersionText + " starting");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Log.Error("Fatal error while running the main window.", ex);
                ShowFatalError(ex);
            }
            finally
            {
                Log.Info("ILXP shutting down");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("Unhandled UI exception.", e.Exception);
            ShowError(e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled application exception.", e.ExceptionObject as Exception);
        }

        private static void ShowFatalError(Exception exception)
        {
            try
            {
                MessageBox.Show(
                    "ILXP encountered an unrecoverable error and has to close." + Environment.NewLine +
                    Environment.NewLine + exception.Message,
                    AppInfo.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                // Nothing else we can do at this point.
            }
        }

        private static void ShowError(Exception exception)
        {
            try
            {
                MessageBox.Show(
                    exception.Message,
                    AppInfo.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
            }
        }
    }
}
