using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ILXP.UI
{
    /// <summary>
    /// Picture control that behaves like a water surface: clicking (or dragging) on it
    /// creates ripples that distort the image. The effect is a classic height-field
    /// simulation rendered with per-pixel displacement.
    /// </summary>
    internal sealed class WaterLogo : Control
    {
        private const float Damping = 0.994f;
        private const float Displacement = 0.9f;
        private const float Highlight = 0.055f;
        private const float TintStrength = 1.5f;
        private const int FrameInterval = 33;
        private const float RestThreshold = 0.03f;

        private readonly Timer timer;
        private Bitmap sourceBitmap;
        private int[] sourcePixels = new int[0];
        private int[] targetPixels = new int[0];
        private float[] current = new float[0];
        private float[] previous = new float[0];
        private Bitmap renderTarget;
        private int width;
        private int height;
        private float energy;
        private bool dirty = true;
        private int dropCooldown;

        public WaterLogo()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            timer = new Timer { Interval = FrameInterval };
            timer.Tick += OnTick;
        }

        /// <summary>Image shown on the surface.</summary>
        public Image Image
        {
            get { return sourceBitmap; }
            set
            {
                if (sourceBitmap != null)
                {
                    sourceBitmap.Dispose();
                    sourceBitmap = null;
                }

                sourceBitmap = value == null ? null : new Bitmap(value);
                RebuildBuffers();
            }
        }

        public void AddDrop(int x, int y, int strength)
        {
            if (width <= 1 || height <= 1)
            {
                return;
            }

            x = Math.Max(1, Math.Min(width - 2, x));
            y = Math.Max(1, Math.Min(height - 2, y));
            float value = Math.Max(1, strength);

            // A wide, soft splash reads much better than a single spike: it produces a
            // visible ring that travels across the whole surface.
            const int radius = 7;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px <= 0 || py <= 0 || px >= width - 1 || py >= height - 1)
                    {
                        continue;
                    }

                    float distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared > radius * radius)
                    {
                        continue;
                    }

                    float falloff = 1f / (1f + distanceSquared * 0.45f);
                    current[py * width + px] += value * falloff * 2.2f;
                }
            }

            energy = Math.Max(energy, value * 3f);
            EnsureRunning();
        }

        /// <summary>Adds a ripple in the middle of the surface (used for the first frame).</summary>
        public void Splash()
        {
            AddDrop(width / 2, height / 2, 26);
        }

        /// <summary>
        /// Advances the simulation synchronously. Used by tests and by the screenshot
        /// command so that a static image can show the water effect.
        /// </summary>
        public void Advance(int frames)
        {
            if (width <= 1 || height <= 1)
            {
                return;
            }

            for (int i = 0; i < frames; i++)
            {
                Simulate();
            }
            dirty = true;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RebuildBuffers();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (width <= 1 || height <= 1)
            {
                return;
            }

            if (dirty)
            {
                RenderFrame();
                dirty = false;
            }

            if (renderTarget != null)
            {
                e.Graphics.DrawImageUnscaled(renderTarget, 0, 0);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                AddDrop(e.X, e.Y, e.Button == MouseButtons.Left ? 26 : 14);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                // Dragging leaves a trail of smaller drops, but rate limited so that the
                // simulation cannot be flooded.
                if (dropCooldown <= 0)
                {
                    AddDrop(e.X, e.Y, 9);
                    dropCooldown = 3;
                }
                else
                {
                    dropCooldown--;
                }
            }
            else
            {
                dropCooldown = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
                if (renderTarget != null)
                {
                    renderTarget.Dispose();
                }
                if (sourceBitmap != null)
                {
                    sourceBitmap.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void RebuildBuffers()
        {
            width = Math.Max(0, Width);
            height = Math.Max(0, Height);
            if (width <= 1 || height <= 1)
            {
                return;
            }

            current = new float[width * height];
            previous = new float[width * height];
            targetPixels = new int[width * height];
            energy = 0;

            if (renderTarget != null)
            {
                renderTarget.Dispose();
            }
            renderTarget = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            sourcePixels = new int[width * height];
            if (sourceBitmap == null)
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    sourcePixels[i] = Color.White.ToArgb();
                }
            }
            else
            {
                using (Bitmap scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(scaled))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(sourceBitmap, 0, 0, width, height);
                    }
                    CopyPixels(scaled, sourcePixels);
                }
            }

            dirty = true;
            Invalidate();
        }

        private static void CopyPixels(Bitmap bitmap, int[] destination)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* scan = (byte*)data.Scan0;
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int* row = (int*)(scan + y * data.Stride);
                        int offset = y * bitmap.Width;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            destination[offset + x] = row[x];
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private void EnsureRunning()
        {
            if (!timer.Enabled)
            {
                timer.Start();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (width <= 1 || height <= 1)
            {
                timer.Stop();
                return;
            }

            Simulate();
            dirty = true;
            Invalidate();

            if (energy < RestThreshold)
            {
                energy = 0;
                timer.Stop();
            }
        }

        private void Simulate()
        {
            float[] a = current;
            float[] b = previous;
            float total = 0;

            for (int y = 1; y < height - 1; y++)
            {
                int row = y * width;
                for (int x = 1; x < width - 1; x++)
                {
                    int index = row + x;
                    float value = (a[index - 1] + a[index + 1] + a[index - width] + a[index + width]) * 0.5f - b[index];
                    value *= Damping;
                    b[index] = value;
                    total += value < 0 ? -value : value;
                }
            }

            // Swap: "previous" now holds the new heights.
            current = b;
            previous = a;
            energy = total / (width * height);
        }

        private void RenderFrame()
        {
            if (renderTarget == null || sourcePixels.Length == 0)
            {
                return;
            }

            int stride = width;
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                int up = y > 0 ? row - stride : row;
                int down = y < height - 1 ? row + stride : row;

                for (int x = 0; x < width; x++)
                {
                    int index = row + x;
                    int left = x > 0 ? index - 1 : index;
                    int right = x < width - 1 ? index + 1 : index;

                    float height0 = current[index];
                    float gradientX = current[left] - current[right];
                    float gradientY = current[up] - current[down];
                    float laplacian = height0 - (current[up] + current[down] + current[left] + current[right]) * 0.25f;

                    if (gradientX > -0.02f && gradientX < 0.02f &&
                        gradientY > -0.02f && gradientY < 0.02f &&
                        laplacian > -0.02f && laplacian < 0.02f)
                    {
                        targetPixels[index] = sourcePixels[index];
                        continue;
                    }

                    int dx = (int)(gradientX * Displacement);
                    int dy = (int)(gradientY * Displacement);

                    int sx = x + dx;
                    int sy = y + dy;
                    if (sx < 0)
                    {
                        sx = 0;
                    }
                    else if (sx >= width)
                    {
                        sx = width - 1;
                    }
                    if (sy < 0)
                    {
                        sy = 0;
                    }
                    else if (sy >= height)
                    {
                        sy = height - 1;
                    }

                    int color = sourcePixels[sy * stride + sx];

                    // The laplacian is ring shaped around a drop, so using it for both the
                    // shading and the tint produces visible concentric ripples instead of
                    // flat blobs.
                    int light = (int)(laplacian * Highlight * 255f);
                    if (light > 130)
                    {
                        light = 130;
                    }
                    else if (light < -130)
                    {
                        light = -130;
                    }

                    float magnitude = laplacian < 0 ? -laplacian : laplacian;
                    int tint = (int)(magnitude * TintStrength * 16f);
                    if (tint > 150)
                    {
                        tint = 150;
                    }

                    if (light != 0 || tint != 0)
                    {
                        color = Shade(color, light, tint);
                    }

                    targetPixels[index] = color;
                }
            }

            WritePixels(renderTarget, targetPixels);
        }

        private static int Shade(int color, int light, int tint)
        {
            int a = (color >> 24) & 0xFF;
            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;

            // Water tint: pull the pixel towards a light cyan, proportional to the local
            // wave height. This is what makes the ripples obvious on a white background.
            if (tint > 0)
            {
                r += ((80 - r) * tint) / 255;
                g += ((165 - g) * tint) / 255;
                b += ((235 - b) * tint) / 255;
            }

            r += light;
            g += light;
            b += light;

            r = r < 0 ? 0 : (r > 255 ? 255 : r);
            g = g < 0 ? 0 : (g > 255 ? 255 : g);
            b = b < 0 ? 0 : (b > 255 ? 255 : b);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private static void WritePixels(Bitmap bitmap, int[] pixels)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* scan = (byte*)data.Scan0;
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int* row = (int*)(scan + y * data.Stride);
                        int offset = y * bitmap.Width;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            row[x] = pixels[offset + x];
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
