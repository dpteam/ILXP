using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Panel that draws a Windows-visual-styles border, matching the look of
    /// Explorer panes. Only active for <see cref="BorderStyle.FixedSingle"/> when
    /// visual styles are enabled; otherwise the panel behaves like a plain Panel.
    /// </summary>
    public class BorderPanel : Panel
    {
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCPAINT = 0x0085;
        private const int DCX_WINDOW = 0x00000001;
        private const int DCX_CACHE = 0x00000002;
        private const int DCX_CLIPSIBLINGS = 0x00000010;
        private const int DCX_INTERSECTRGN = 0x00000080;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NcCalcSizeParams
        {
            public Rect Client;
            public Rect Before;
            public Rect After;
            public IntPtr LParam;
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (!TryHandleNonClientMessage(this, ref m))
                {
                    base.WndProc(ref m);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Border painting failed.", ex);
                base.WndProc(ref m);
            }
        }

        /// <summary>Handles WM_NCCALCSIZE for a bordered, themed control.</summary>
        public static bool TryHandleNonClientMessage(Panel control, ref Message message)
        {
            if (control == null || control.BorderStyle != BorderStyle.FixedSingle || !Application.RenderWithVisualStyles)
            {
                return false;
            }

            switch (message.Msg)
            {
                case WM_NCCALCSIZE:
                    if (message.WParam == IntPtr.Zero)
                    {
                        Rect rect = (Rect)Marshal.PtrToStructure(message.LParam, typeof(Rect));
                        rect.Bottom++;
                        rect.Top--;
                        rect.Right++;
                        rect.Left--;
                        Marshal.StructureToPtr(rect, message.LParam, false);
                    }
                    else
                    {
                        NcCalcSizeParams parameters = (NcCalcSizeParams)Marshal.PtrToStructure(message.LParam, typeof(NcCalcSizeParams));
                        parameters.Client.Top--;
                        parameters.Client.Bottom++;
                        parameters.Client.Left--;
                        parameters.Client.Right++;
                        Marshal.StructureToPtr(parameters, message.LParam, false);
                    }
                    return true;

                case WM_NCPAINT:
                    return PaintVisualStyleBorder(control, ref message);
            }

            return false;
        }

        private static bool PaintVisualStyleBorder(Panel control, ref Message message)
        {
            Panel panel = control as Panel;
            if (panel == null || !panel.AutoScroll)
            {
                return false;
            }

            IntPtr hdc = IntPtr.Zero;
            try
            {
                if (message.WParam != (IntPtr)1 && message.WParam != IntPtr.Zero)
                {
                    hdc = GetDCEx(message.HWnd, message.WParam, DCX_WINDOW | DCX_CACHE | DCX_CLIPSIBLINGS | DCX_INTERSECTRGN);
                }
                if (hdc == IntPtr.Zero)
                {
                    hdc = GetWindowDC(message.HWnd);
                }
                if (hdc == IntPtr.Zero)
                {
                    return false;
                }

                Rect windowRect;
                if (!GetWindowRect(message.HWnd, out windowRect))
                {
                    return false;
                }

                Rectangle bounds = new Rectangle(0, 0, windowRect.Right - windowRect.Left - 1, windowRect.Bottom - windowRect.Top - 1);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return false;
                }

                using (Graphics graphics = Graphics.FromHdc(hdc))
                {
                    ControlPaint.DrawVisualStyleBorder(graphics, bounds);
                }
                bounds.Inflate(-1, -1);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Visual style border painting failed.", ex);
                return false;
            }
            finally
            {
                if (hdc != IntPtr.Zero)
                {
                    ReleaseDC(message.HWnd, hdc);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, int flags);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
    }
}
