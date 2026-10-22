using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Read-only code viewer with syntax coloring, link navigation, selection and
    /// copy support. Text is supplied with lightweight markup produced by the
    /// <see cref="LanguageWriter"/> implementations:
    /// <c>&lt;&lt;b&gt;&gt;</c> keyword, <c>&lt;&lt;r&gt;&gt;</c> string, <c>&lt;&lt;g&gt;&gt;</c>
    /// comment, <c>&lt;&lt;#target&gt;&gt;</c> link, <c>&lt;&lt;&gt;&gt;</c> reset.
    /// </summary>
    public sealed class CodeEditor : BorderPanel
    {
        private const string MarkupPattern = "(\\<\\<[r|g|b|\\?|\\#].*?\\>\\>)";

        private CodeToken hoveredToken;
        private LineCollection lines;
        private int selectionStart;
        private int selectionLength;
        private int lineHeight;
        private int characterWidth;
        private readonly TextFormatFlags textFormatFlags;
        private readonly Color stringColor = Color.FromArgb(163, 21, 21);
        private readonly Color keywordColor = Color.FromArgb(0, 0, 255);
        private readonly Color commentColor = Color.FromArgb(0, 128, 0);

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem selectAllMenuItem;

        /// <summary>Raised when the user clicks a link token.</summary>
        public event EventHandler<LinkClickedEventArgs> LinkClicked;

        /// <summary>Raised when the caret or the selection changes (mouse or keyboard).</summary>
        public event EventHandler SelectionChanged;

        /// <summary>Number of rendered lines.</summary>
        public int LineCount
        {
            get { return lines.Count; }
        }

        /// <summary>Plain text of a rendered line, or an empty string.</summary>
        public string GetLineText(int index)
        {
            if (index < 0 || index >= lines.Count)
            {
                return string.Empty;
            }

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (CodeToken token in lines[index].Tokens)
            {
                text.Append(token.Text);
            }
            return text.ToString();
        }

        /// <summary>Index of the line that contains the given character offset.</summary>
        public int GetLineIndexAt(int charOffset)
        {
            if (lines.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                CodeLine line = lines[i];
                if (charOffset < line.Offset + line.Length)
                {
                    return i;
                }
            }
            return lines.Count - 1;
        }

        /// <summary>Selects a whole line and scrolls it into view.</summary>
        public void SelectLine(int index)
        {
            if (index < 0 || index >= lines.Count)
            {
                return;
            }

            CodeLine line = lines[index];
            selectionStart = line.Offset;
            selectionLength = line.Length;
            ScrollToLine(index);
            Invalidate();
            RaiseSelectionChanged();
        }

        /// <summary>Scrolls so that the requested line is visible.</summary>
        public void ScrollToLine(int index)
        {
            if (index < 0)
            {
                return;
            }

            int y = index * lineHeight;
            int visibleTop = -AutoScrollPosition.Y;
            int visibleBottom = visibleTop + ClientSize.Height - lineHeight;

            if (y < visibleTop || y > visibleBottom)
            {
                AutoScrollPosition = new Point(AutoScrollPosition.X, Math.Max(0, y - ClientSize.Height / 2));
            }
        }

        private void RaiseSelectionChanged()
        {
            EventHandler handler = SelectionChanged;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Warn("Selection handler failed.", ex);
            }
        }

        public CodeEditor()
        {
            lines = new LineCollection();
            selectionStart = -1;
            textFormatFlags = TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix |
                              TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;

            InitializeContextMenu();
            BackColor = SystemColors.Window;
            DoubleBuffered = true;
            Font = new Font("Courier New", 8.5f);
            SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.UserMouse |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            TabStop = true;
        }

        public override string Text
        {
            get { return lines.ToString(); }
            set { ParseMarkup(value); }
        }

        public int SelectionStart
        {
            get { return selectionLength < 0 ? selectionStart + selectionLength : selectionStart; }
        }

        public int SelectionLength
        {
            get { return selectionLength < 0 ? -selectionLength : selectionLength; }
        }

        public CodeToken HoveredToken
        {
            get { return hoveredToken; }
        }

        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                if (value == null)
                {
                    return;
                }
                lineHeight = value.Height;
                characterWidth = TextRenderer.MeasureText("xxxxxxxxxxxxxxxxxxxx", value,
                    new Size(1000, 100), textFormatFlags).Width -
                    TextRenderer.MeasureText("xxxxxxxxxxxxxxxxxxx", value,
                    new Size(1000, 100), textFormatFlags).Width;
            }
        }

        /// <summary>Clears the document.</summary>
        public void Clear()
        {
            Text = string.Empty;
        }

        /// <summary>Replaces the document with plain text (no markup).</summary>
        public void SetPlainText(string text)
        {
            lines.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                foreach (string lineText in text.Replace(Environment.NewLine, "\r").Split('\r'))
                {
                    CodeLine line = new CodeLine();
                    line.Tokens.Add(new CodeToken(lineText) { Color = Color.Black, Link = string.Empty });
                    lines.Add(line);
                }
            }
            selectionStart = 0;
            selectionLength = 0;
            RecalculateOffsets();
            UpdateScrollSize();
            Invalidate();
        }

        private void ParseMarkup(string value)
        {
            lines.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                Color color = Color.Black;
                string link = string.Empty;
                foreach (string lineText in value.Replace(Environment.NewLine, "\r").Split('\r'))
                {
                    CodeLine line = new CodeLine();
                    string normalized = lineText.Replace("<<>>", "<<?>>");
                    foreach (string part in Regex.Split(normalized, MarkupPattern))
                    {
                        switch (part)
                        {
                            case "<<?>>":
                                color = Color.Black;
                                link = string.Empty;
                                continue;
                            case "<<b>>":
                                color = keywordColor;
                                link = string.Empty;
                                continue;
                            case "<<r>>":
                                color = stringColor;
                                link = string.Empty;
                                continue;
                            case "<<g>>":
                                color = commentColor;
                                link = string.Empty;
                                continue;
                        }

                        if (part.StartsWith("<<#", StringComparison.Ordinal))
                        {
                            color = Color.Black;
                            link = part.Substring(3, part.Length - 5);
                            continue;
                        }

                        line.Tokens.Add(new CodeToken(part) { Color = color, Link = link });
                    }
                    lines.Add(line);
                }
            }

            AutoScrollPosition = new Point(0, 0);
            selectionStart = 0;
            selectionLength = 0;
            RecalculateOffsets();
            UpdateScrollSize();
            Invalidate();
        }

        private void RecalculateOffsets()
        {
            int offset = 0;
            foreach (CodeLine line in lines)
            {
                line.Offset = offset;
                foreach (CodeToken token in line.Tokens)
                {
                    offset += token.Text.Length;
                }
                line.Length = offset - line.Offset;
            }
        }

        private void UpdateScrollSize()
        {
            int maxLength = 0;
            foreach (CodeLine line in lines)
            {
                maxLength = Math.Max(maxLength, line.Length);
            }

            AutoScrollPosition = new Point(0, 0);
            AutoScrollMinSize = new Size(maxLength * characterWidth, lines.Count * lineHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            int y = 0;
            int x = 0;
            int firstSelected = SelectionStart;
            int lastSelected = firstSelected + SelectionLength;
            using (Font underlinedFont = new Font(Font, FontStyle.Underline))
            {
                SolidBrush highlight = (SolidBrush)SystemBrushes.Highlight;
                Rectangle clip = e.ClipRectangle;
                clip.Offset(-AutoScrollPosition.X, -AutoScrollPosition.Y);
                clip.Inflate(2, 2);

                int position = 0;
                foreach (CodeLine line in lines)
                {
                    if (y + lineHeight >= clip.Y && y <= clip.Bottom)
                    {
                        position = line.Offset;
                        foreach (CodeToken token in line.Tokens)
                        {
                            Font tokenFont = Font;
                            if (!string.IsNullOrEmpty(token.Link))
                            {
                                tokenFont = hoveredToken == token ? underlinedFont : Font;
                            }

                            string text = token.Text;
                            for (int i = 0; i < text.Length; i++)
                            {
                                Rectangle cell = new Rectangle(x, y, characterWidth, lineHeight);
                                if (clip.IntersectsWith(cell))
                                {
                                    if (position >= firstSelected && position < lastSelected)
                                    {
                                        graphics.FillRectangle(highlight, cell);
                                        cell.Offset(AutoScrollPosition);
                                        TextRenderer.DrawText(graphics, text[i].ToString(), tokenFont, cell,
                                            SystemColors.HighlightText, SystemColors.Highlight, textFormatFlags);
                                    }
                                    else
                                    {
                                        cell.Offset(AutoScrollPosition);
                                        TextRenderer.DrawText(graphics, text[i].ToString(), tokenFont, cell,
                                            token.Color, textFormatFlags);
                                    }
                                }
                                x += characterWidth;
                                position++;
                            }
                        }
                        x = 0;
                    }
                    y += lineHeight;
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            CodeToken token = null;
            Point position = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
            int caret = HitTest(position, ref token);

            if (e.Button == MouseButtons.Left && Capture && caret - selectionStart != selectionLength)
            {
                selectionLength = caret - selectionStart;
                Invalidate();
            }

            if (hoveredToken != token)
            {
                if (hoveredToken != null)
                {
                    Invalidate(ToScreen(hoveredToken.Bounds));
                }
                hoveredToken = token;
                if (hoveredToken != null)
                {
                    Invalidate(ToScreen(hoveredToken.Bounds));
                }
            }

            if (hoveredToken != null && Cursor != Cursors.Hand)
            {
                Cursor = Cursors.Hand;
            }
            else if (hoveredToken == null && Cursor != Cursors.IBeam)
            {
                Cursor = Cursors.IBeam;
            }

            base.OnMouseMove(e);
        }

        private Rectangle ToScreen(Rectangle bounds)
        {
            return new Rectangle(bounds.X + AutoScrollPosition.X, bounds.Y + AutoScrollPosition.Y, bounds.Width, bounds.Height);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hoveredToken != null)
            {
                Cursor = Cursors.Default;
                hoveredToken = null;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        private int HitTest(Point position, ref CodeToken hit)
        {
            int lineIndex = position.Y / lineHeight;
            if (lineIndex < 0)
            {
                return 0;
            }

            if (lineIndex >= lines.Count)
            {
                int end = 0;
                if (lines.Count > 0)
                {
                    CodeLine last = lines[lines.Count - 1];
                    end = last.Offset;
                    foreach (CodeToken token in last.Tokens)
                    {
                        end += token.Text.Length;
                    }
                }
                return end;
            }

            CodeLine currentLine = lines[lineIndex];
            int lineStart = currentLine.Offset;
            int x = 0;
            foreach (CodeToken token in currentLine.Tokens)
            {
                if (position.X >= x && position.X < x + token.Text.Length * characterWidth)
                {
                    if (!string.IsNullOrEmpty(token.Link))
                    {
                        hit = token;
                        token.Bounds = new Rectangle(x, lineIndex * lineHeight, token.Text.Length * characterWidth, lineHeight + 5);
                    }

                    string text = token.Text;
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (position.X >= x && position.X < x + characterWidth)
                        {
                            return lineStart + i;
                        }
                        lineStart++;
                        x += characterWidth;
                    }
                    return lineStart;
                }

                lineStart += token.Text.Length;
                x += token.Text.Length * characterWidth;
            }
            return lineStart;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                selectionLength = 0;
                Point position = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
                CodeToken token = null;
                selectionStart = HitTest(position, ref token);
                Capture = true;
                Invalidate();
                RaiseSelectionChanged();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            AutoScrollPosition = new Point(AutoScrollPosition.X,
                (AutoScrollPosition.Y / lineHeight) * lineHeight * -1 + lineHeight * e.Delta);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!Capture)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                CodeToken token = null;
                Point position = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
                int caret = HitTest(position, ref token);
                if (caret - selectionStart != selectionLength)
                {
                    selectionLength = caret - selectionStart;
                    Invalidate();
                }

                if (selectionLength == 0 && token != null && !string.IsNullOrEmpty(token.Link))
                {
                    EventHandler<LinkClickedEventArgs> handler = LinkClicked;
                    if (handler != null)
                    {
                        handler(this, new LinkClickedEventArgs(token.Link));
                    }
                }
                RaiseSelectionChanged();
            }
            else if (e.Button == MouseButtons.Right)
            {
                contextMenu.Show(this, e.Location);
            }
            base.OnMouseUp(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.Next:
                case Keys.Prior:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelection();
            }
            if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAll();
            }

            switch (e.KeyCode)
            {
                case Keys.Down:
                    AutoScrollPosition = new Point(AutoScrollPosition.X,
                        (AutoScrollPosition.Y / lineHeight) * lineHeight * -1 + lineHeight);
                    break;
                case Keys.Up:
                    AutoScrollPosition = new Point(AutoScrollPosition.X,
                        (AutoScrollPosition.Y / lineHeight) * lineHeight * -1 - lineHeight);
                    break;
                case Keys.Left:
                    AutoScrollPosition = new Point(
                        (AutoScrollPosition.X / characterWidth) * characterWidth * -1 - characterWidth,
                        AutoScrollPosition.Y);
                    break;
                case Keys.Right:
                    AutoScrollPosition = new Point(
                        (AutoScrollPosition.X / characterWidth) * characterWidth * -1 + characterWidth,
                        AutoScrollPosition.Y);
                    break;
                case Keys.Home:
                    AutoScrollPosition = new Point(AutoScrollPosition.X * -1, 0);
                    break;
                case Keys.End:
                    AutoScrollPosition = new Point(AutoScrollPosition.X * -1, lines.Count * lineHeight);
                    break;
                case Keys.Next:
                    AutoScrollPosition = new Point(AutoScrollPosition.X,
                        (AutoScrollPosition.Y / lineHeight) * lineHeight * -1 + (Height / lineHeight) * lineHeight);
                    break;
                case Keys.Prior:
                    AutoScrollPosition = new Point(AutoScrollPosition.X,
                        (AutoScrollPosition.Y / lineHeight) * lineHeight * -1 - (Height / lineHeight) * lineHeight);
                    break;
            }
            base.OnKeyDown(e);
            RaiseSelectionChanged();
        }

        public string GetSelectedText()
        {
            string text = string.Empty;
            int start = SelectionStart;
            int end = start + SelectionLength;
            int position = 0;

            foreach (CodeLine line in lines)
            {
                if (line.Offset + line.Length < start)
                {
                    position = line.Offset + line.Length;
                    continue;
                }

                foreach (CodeToken token in line.Tokens)
                {
                    for (int i = 0; i < token.Text.Length; i++)
                    {
                        if (position >= start)
                        {
                            if (position < end)
                            {
                                text += token.Text[i];
                            }
                            else
                            {
                                return text;
                            }
                        }
                        position++;
                    }
                }
                text += Environment.NewLine;
            }
            return text;
        }

        public void SelectAll()
        {
            selectionStart = 0;
            selectionLength = 0;
            if (lines.Count > 0)
            {
                CodeLine last = lines[lines.Count - 1];
                selectionLength = last.Offset + last.Length;
            }
            Invalidate();
        }

        public void CopySelection()
        {
            if (selectionLength == 0)
            {
                return;
            }

            try
            {
                Clipboard.SetText(GetSelectedText(), TextDataFormat.Text);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to copy the selection to the clipboard.", ex);
            }
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            copyMenuItem = new ToolStripMenuItem();
            selectAllMenuItem = new ToolStripMenuItem();
            contextMenu.SuspendLayout();
            SuspendLayout();

            contextMenu.Items.AddRange(new ToolStripItem[] { copyMenuItem, selectAllMenuItem });
            contextMenu.Name = "ContextMenuStrip1";
            contextMenu.Size = new Size(120, 48);
            contextMenu.Opening += OnContextMenuOpening;

            copyMenuItem.Name = "MenuItemCopy";
            copyMenuItem.Size = new Size(119, 22);
            copyMenuItem.Text = "&Copy";
            copyMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            copyMenuItem.Click += delegate { CopySelection(); };

            selectAllMenuItem.Name = "MenuItemSelectAll";
            selectAllMenuItem.Size = new Size(119, 22);
            selectAllMenuItem.Text = "&Select All";
            selectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
            selectAllMenuItem.Click += delegate { SelectAll(); };

            contextMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void OnContextMenuOpening(object sender, CancelEventArgs e)
        {
            copyMenuItem.Enabled = SelectionLength > 0;
            selectAllMenuItem.Enabled = lines.Count > 0;
        }
    }
}
