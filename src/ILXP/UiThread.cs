using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Helpers for running potentially slow work on the UI thread while a small
    /// "Loading..." overlay is visible, plus a modal overlay window used while an
    /// assembly is being read.
    /// </summary>
    public static class UiThread
    {
        public delegate object ObjectFunc(object argument);

        public delegate TreeNode TreeNodeFunc(string text);

        /// <summary>Small animated spinner with a "Loading..." caption.</summary>
        public sealed class LoadingOverlay : Control
        {
            private const int FrameCount = 8;
            private const int DotCount = 5;
            private const int MaxDotRadius = 5;
            private const int DotSpacing = 20;

            private static int frame;
            private readonly Point[] dotPositions;

            public LoadingOverlay()
            {
                dotPositions = new[]
                {
                    new Point(20, 8),
                    new Point(28, 12),
                    new Point(32, 20),
                    new Point(28, 28),
                    new Point(20, 32),
                    new Point(12, 28),
                    new Point(8, 20),
                    new Point(12, 12)
                };

                Size = new Size(96, 40);
                SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor |
                         ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = Color.White;
                ForeColor = Color.Blue;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics graphics = e.Graphics;
                graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1);
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                int currentFrame = frame % FrameCount;
                for (int i = 0; i < FrameCount; i++)
                {
                    int age = currentFrame - i;
                    if (age < 0)
                    {
                        age += FrameCount;
                    }
                    int radius = Math.Max(DotCount - age, 1);
                    if (i > frame)
                    {
                        radius = 1;
                    }

                    Rectangle dot = new Rectangle(
                        dotPositions[i].X - radius,
                        dotPositions[i].Y - radius,
                        radius * 2,
                        radius * 2);
                    graphics.FillEllipse(Brushes.Black, dot);
                }

                using (StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone())
                {
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString("Loading...", Font, Brushes.Black, new Rectangle(40, 0, Width - 40, Height), format);
                }
                frame++;
            }
        }

        private static Thread modalOverlayThread;

        /// <summary>Runs <paramref name="action"/> on the control's thread, showing the overlay.</summary>
        public static void Run(MethodInvoker action, Control control)
        {
            IAsyncResult result = action.BeginInvoke(null, null);
            WaitWithOverlay(result, control);
        }

        public static object Invoke(ObjectFunc function, object argument, Control control)
        {
            IAsyncResult result = function.BeginInvoke(argument, null, null);
            WaitWithOverlay(result, control);
            return function.EndInvoke(result);
        }

        public static TreeNode Invoke(TreeNodeFunc function, string argument, Control control)
        {
            IAsyncResult result = function.BeginInvoke(argument, null, null);
            WaitWithOverlay(result, control);
            return function.EndInvoke(result);
        }

        private static void WaitWithOverlay(IAsyncResult result, Control control)
        {
            using (LoadingOverlay overlay = new LoadingOverlay())
            {
                overlay.Location = new Point(
                    (control.ClientSize.Width - overlay.Width) / 2,
                    (control.ClientSize.Height - overlay.Height) / 2);
                control.Controls.Add(overlay);
                overlay.BringToFront();
                overlay.Refresh();
                try
                {
                    while (!result.IsCompleted)
                    {
                        Thread.Sleep(100);
                        overlay.Refresh();
                    }
                }
                finally
                {
                    control.Controls.Remove(overlay);
                }
            }
            control.Refresh();
        }

        /// <summary>Shows a top-most loading window centered on <paramref name="bounds"/>.</summary>
        public static void ShowModalOverlay(Form owner, Rectangle bounds)
        {
            if (modalOverlayThread != null)
            {
                return;
            }

            modalOverlayThread = new Thread(ModalOverlayProc);
            modalOverlayThread.IsBackground = true;
            modalOverlayThread.Start(bounds);
        }

        public static void HideModalOverlay()
        {
            modalOverlayThread = null;
        }

        private static void ModalOverlayProc(object state)
        {
            Rectangle bounds = state is Rectangle ? (Rectangle)state : Rectangle.Empty;
            using (LoadingOverlay overlay = new LoadingOverlay())
            using (Form host = new Form())
            {
                host.FormBorderStyle = FormBorderStyle.None;
                host.ShowInTaskbar = false;
                host.StartPosition = FormStartPosition.Manual;
                host.ShowIcon = false;
                host.MinimumSize = overlay.Size;
                host.Size = overlay.Size;
                host.Location = new Point(
                    bounds.X + (bounds.Width - overlay.Width) / 2,
                    bounds.Y + (bounds.Height - overlay.Height) / 2);
                host.Controls.Add(overlay);
                host.TopMost = true;
                overlay.BringToFront();
                overlay.Refresh();
                host.Show();

                while (modalOverlayThread != null)
                {
                    Thread.Sleep(100);
                    overlay.Refresh();
                }

                host.Close();
            }
        }
    }
}
