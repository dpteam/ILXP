using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Two-way navigation between the rendered IL text and IL offsets: click a line to see
    /// its offset, press Ctrl+G to jump to an offset.
    /// </summary>
    public sealed partial class MainForm
    {
        private IlLineMap ilMap = IlLineMap.Parse(string.Empty);

        private void OnEditorSelectionChanged(object sender, EventArgs e)
        {
            UpdateIlStatus();
        }

        /// <summary>Rebuilds the line → offset map from the current document.</summary>
        private void RebuildIlMap()
        {
            ilMap = IlLineMap.Parse(codeEditor.Text);
        }

        /// <summary>Shows the IL offset of the line under the caret in the status bar.</summary>
        private void UpdateIlStatus()
        {
            if (ilMap.IsEmpty || codeEditor.SelectionLength > 0)
            {
                return;
            }

            int line = codeEditor.GetLineIndexAt(codeEditor.SelectionStart);
            int offset;
            if (line >= 0 && ilMap.TryGetOffset(line, out offset))
            {
                statusLabel.Text = string.Format("IL_{0:x4}   (line {1})", offset, line + 1);
            }
        }

        /// <summary>Ctrl+G: jumps to an IL offset (the nearest instruction is selected).</summary>
        private void OnGoToIlOffset(object sender, EventArgs e)
        {
            if (ilMap.IsEmpty)
            {
                MessageBox.Show(this, "The current view has no IL offsets.", AppInfo.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (IlOffsetPromptForm prompt = new IlOffsetPromptForm())
            {
                if (prompt.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                GoToIlOffset(prompt.SelectedOffset);
            }
        }

        /// <summary>Selects the instruction at (or closest before) <paramref name="offset"/>.</summary>
        private bool GoToIlOffset(int offset)
        {
            int line = ilMap.FindLine(offset);
            if (line < 0)
            {
                return false;
            }

            codeEditor.SelectLine(line);
            int actual;
            if (ilMap.TryGetOffset(line, out actual))
            {
                statusLabel.Text = string.Format("IL_{0:x4}   (line {1})", actual, line + 1);
            }
            return true;
        }

        /// <summary>Small prompt for an IL offset; accepts 0x1234, 1234h and 1234.</summary>
        private sealed class IlOffsetPromptForm : Form
        {
            private readonly TextBox input;
            private int selectedOffset = -1;

            public IlOffsetPromptForm()
            {
                Text = "Go to IL offset";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new System.Drawing.Size(300, 110);

                Label label = new Label
                {
                    Location = new System.Drawing.Point(12, 12),
                    Size = new System.Drawing.Size(276, 20),
                    Text = "Offset (hex, e.g. 0x002a):"
                };

                input = new TextBox
                {
                    Location = new System.Drawing.Point(12, 34),
                    Size = new System.Drawing.Size(276, 24)
                };

                Button ok = new Button
                {
                    Location = new System.Drawing.Point(132, 72),
                    Size = new System.Drawing.Size(75, 26),
                    Text = "OK",
                    DialogResult = DialogResult.OK
                };
                Button cancel = new Button
                {
                    Location = new System.Drawing.Point(213, 72),
                    Size = new System.Drawing.Size(75, 26),
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel
                };

                ok.Click += delegate
                {
                    if (!TryParseOffset(input.Text, out selectedOffset))
                    {
                        MessageBox.Show(this, "Enter a hexadecimal offset.", Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                    }
                };

                AcceptButton = ok;
                CancelButton = cancel;
                Controls.Add(label);
                Controls.Add(input);
                Controls.Add(ok);
                Controls.Add(cancel);
            }

            public int SelectedOffset
            {
                get { return selectedOffset; }
            }

            private static bool TryParseOffset(string text, out int offset)
            {
                offset = 0;
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }

                text = text.Trim();
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(2);
                }
                else if (text.EndsWith("h", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(0, text.Length - 1);
                }

                return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
            }
        }
    }
}
