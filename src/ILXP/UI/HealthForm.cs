using System;
using System.Drawing;
using System.Windows.Forms;

namespace ILXP.UI
{
    /// <summary>
    /// Read-only viewer for an <see cref="AssemblyHealthReport"/>. Shows the same text the
    /// <c>--health</c> command prints, with a copy button.
    /// </summary>
    internal sealed class HealthForm : Form
    {
        private readonly TextBox text;

        public HealthForm(string title, string content)
        {
            Text = "Assembly health - " + title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(600, 400);
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;

            text = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5f),
                Dock = DockStyle.Fill,
                Text = content ?? string.Empty,
                BackColor = SystemColors.Window
            };

            Panel buttons = new Panel { Dock = DockStyle.Bottom, Height = 38 };
            Button copy = new Button
            {
                Text = "Copy",
                Location = new Point(8, 6),
                Size = new Size(90, 26)
            };
            copy.Click += delegate
            {
                try
                {
                    Clipboard.SetText(text.Text);
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to copy the health report.", ex);
                }
            };

            Button close = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(104, 6),
                Size = new Size(90, 26)
            };

            buttons.Controls.Add(copy);
            buttons.Controls.Add(close);

            Controls.Add(text);
            Controls.Add(buttons);
            AcceptButton = close;
            CancelButton = close;
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }
    }
}
