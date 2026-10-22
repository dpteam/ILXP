using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ILXP.UI
{
    /// <summary>
    /// Quick Open palette (Ctrl+P): fuzzy search over every type and member of the loaded
    /// assemblies. It works on a pre-built index, so typing stays instant even for large
    /// assemblies.
    /// </summary>
    internal sealed class QuickOpenForm : Form
    {
        /// <summary>One entry of the index.</summary>
        internal sealed class Entry
        {
            public string Display;
            public string Search;
            public string Kind;
            public string Path;

            public override string ToString()
            {
                return Display;
            }
        }

        private readonly TextBox searchBox;
        private readonly ListBox results;
        private readonly Label hint;
        private readonly List<Entry> entries;
        private readonly List<Entry> filtered = new List<Entry>();
        private bool suppressSelection;

        public QuickOpenForm(IEnumerable<Entry> index, string initialText)
        {
            Text = "Quick Open";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 380);
            KeyPreview = true;
            BackColor = SystemColors.Window;

            entries = new List<Entry>(index);

            searchBox = new TextBox
            {
                Location = new Point(8, 8),
                Width = 624,
                Font = new Font("Segoe UI", 11f)
            };
            searchBox.TextChanged += OnSearchChanged;
            searchBox.KeyDown += OnSearchKeyDown;

            results = new ListBox
            {
                Location = new Point(8, 38),
                Size = new Size(624, 300),
                Font = new Font("Consolas", 9.5f),
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 18
            };
            results.DrawItem += OnDrawItem;
            results.DoubleClick += OnAccept;
            results.KeyDown += OnResultsKeyDown;

            hint = new Label
            {
                Location = new Point(8, 344),
                Size = new Size(624, 28),
                ForeColor = SystemColors.GrayText,
                Text = "Type to filter  ·  Enter to open  ·  Esc to cancel  ·  " + entries.Count + " items"
            };

            Controls.Add(searchBox);
            Controls.Add(results);
            Controls.Add(hint);

            if (!string.IsNullOrEmpty(initialText))
            {
                searchBox.Text = initialText;
                searchBox.SelectionStart = searchBox.TextLength;
            }
            else
            {
                Rebuild();
            }
        }

        public Entry SelectedEntry { get; private set; }

        private void OnSearchChanged(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            string query = searchBox.Text.Trim();
            filtered.Clear();

            if (query.Length == 0)
            {
                int limit = Math.Min(entries.Count, 400);
                for (int i = 0; i < limit; i++)
                {
                    filtered.Add(entries[i]);
                }
            }
            else
            {
                List<KeyValuePair<int, Entry>> scored = new List<KeyValuePair<int, Entry>>();
                foreach (Entry entry in entries)
                {
                    int score = FuzzyMatch(entry.Search, query);
                    if (score >= 0)
                    {
                        scored.Add(new KeyValuePair<int, Entry>(score, entry));
                    }
                }

                scored.Sort(delegate(KeyValuePair<int, Entry> a, KeyValuePair<int, Entry> b)
                {
                    int byScore = b.Key.CompareTo(a.Key);
                    return byScore != 0 ? byScore : string.Compare(a.Value.Display, b.Value.Display, StringComparison.Ordinal);
                });

                int limit = Math.Min(scored.Count, 400);
                for (int i = 0; i < limit; i++)
                {
                    filtered.Add(scored[i].Value);
                }
            }

            suppressSelection = true;
            try
            {
                results.BeginUpdate();
                results.Items.Clear();
                foreach (Entry entry in filtered)
                {
                    results.Items.Add(entry);
                }
                if (results.Items.Count > 0)
                {
                    results.SelectedIndex = 0;
                }
                results.EndUpdate();
            }
            finally
            {
                suppressSelection = false;
            }

            hint.Text = filtered.Count == 0
                ? "No matches"
                : filtered.Count + " match(es)  ·  Enter to open  ·  Esc to cancel";
        }

        /// <summary>
        /// Scores a candidate. Higher is better; -1 means "no match". A match on
        /// consecutive characters at the start of a word ranks above a scattered match.
        /// </summary>
        internal static int FuzzyMatch(string candidate, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return 0;
            }
            if (string.IsNullOrEmpty(candidate))
            {
                return -1;
            }

            int score = 0;
            int candidateIndex = 0;
            int previousMatch = -2;

            foreach (char q in query.ToLowerInvariant())
            {
                bool found = false;
                while (candidateIndex < candidate.Length)
                {
                    char c = char.ToLowerInvariant(candidate[candidateIndex]);
                    if (c == q)
                    {
                        score += 10;
                        if (candidateIndex == previousMatch + 1)
                        {
                            score += 8; // consecutive
                        }
                        if (candidateIndex == 0 || IsBoundary(candidate, candidateIndex))
                        {
                            score += 12; // start of a word / segment
                        }
                        previousMatch = candidateIndex;
                        candidateIndex++;
                        found = true;
                        break;
                    }

                    // Small penalty for skipping characters, larger across segments.
                    score -= candidate[candidateIndex] == '.' ? 3 : 1;
                    candidateIndex++;
                }

                if (!found)
                {
                    return -1;
                }
            }

            // Prefer shorter candidates when the score is otherwise equal.
            return score - candidate.Length / 8;
        }

        private static bool IsBoundary(string text, int index)
        {
            char previous = text[index - 1];
            return previous == '.' || previous == ':' || previous == '(' || previous == ' ';
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                MoveSelection(1);
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                MoveSelection(-1);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Accept();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void OnResultsKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Accept();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Back)
            {
                // Typing continues in the search box even when the list has focus.
                e.SuppressKeyPress = true;
                if (searchBox.TextLength > 0)
                {
                    searchBox.Text = searchBox.Text.Substring(0, searchBox.TextLength - 1);
                    searchBox.SelectionStart = searchBox.TextLength;
                }
                searchBox.Focus();
            }
            else if (char.IsLetterOrDigit((char)e.KeyValue) || e.KeyValue == (int)Keys.Space)
            {
                e.SuppressKeyPress = true;
                searchBox.Text += ((char)e.KeyValue).ToString();
                searchBox.SelectionStart = searchBox.TextLength;
                searchBox.Focus();
            }
        }

        private void MoveSelection(int delta)
        {
            if (results.Items.Count == 0)
            {
                return;
            }

            int index = results.SelectedIndex + delta;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= results.Items.Count)
            {
                index = results.Items.Count - 1;
            }
            results.SelectedIndex = index;
        }

        private void OnAccept(object sender, EventArgs e)
        {
            Accept();
        }

        private void Accept()
        {
            if (suppressSelection)
            {
                return;
            }

            Entry entry = results.SelectedItem as Entry;
            if (entry == null)
            {
                return;
            }

            SelectedEntry = entry;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= results.Items.Count)
            {
                return;
            }

            Entry entry = results.Items[e.Index] as Entry;
            if (entry == null)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color kindColor = selected ? SystemColors.HighlightText : Color.FromArgb(0, 90, 160);
            Color textColor = selected ? SystemColors.HighlightText : SystemColors.WindowText;

            Rectangle bounds = e.Bounds;
            string kind = "[" + entry.Kind + "]";
            TextRenderer.DrawText(e.Graphics, kind, results.Font, bounds, kindColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int offset = TextRenderer.MeasureText(kind + " ", results.Font).Width;
            Rectangle textBounds = new Rectangle(bounds.X + offset, bounds.Y, bounds.Width - offset, bounds.Height);
            TextRenderer.DrawText(e.Graphics, entry.Display, results.Font, textBounds, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            searchBox.Focus();
        }
    }
}
