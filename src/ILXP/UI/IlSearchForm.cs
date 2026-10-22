using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ILXP.UI
{
    /// <summary>
    /// Search over the rendered IL of every method (Ctrl+Shift+F). Results show the method,
    /// the IL offset and the matching line; double-click or Enter jumps to it.
    /// </summary>
    internal sealed class IlSearchForm : Form
    {
        private readonly TextBox queryBox;
        private readonly CheckBox literalBox;
        private readonly CheckBox ignoreCaseBox;
        private readonly CheckBox fieldsBox;
        private readonly Button searchButton;
        private readonly ListView results;
        private readonly Label status;
        private readonly Func<string, IlSearch.Options, List<IlMatch>> search;
        private readonly Action<IlMatch> navigate;
        private int generation;

        public IlSearchForm(Func<string, IlSearch.Options, List<IlMatch>> searchAction, Action<IlMatch> navigateAction, string initialQuery)
        {
            search = searchAction;
            navigate = navigateAction;

            Text = "Search in IL";
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 460);
            MinimumSize = new Size(600, 320);
            ShowInTaskbar = false;
            KeyPreview = true;

            queryBox = new TextBox
            {
                Location = new Point(8, 8),
                Width = 600,
                Font = new Font("Consolas", 10f),
                Text = initialQuery ?? string.Empty
            };
            queryBox.KeyDown += OnQueryKeyDown;

            searchButton = new Button
            {
                Location = new Point(614, 6),
                Size = new Size(90, 26),
                Text = "&Search"
            };
            searchButton.Click += delegate { RunSearch(); };

            literalBox = new CheckBox
            {
                Location = new Point(712, 9),
                Size = new Size(70, 22),
                Text = "Literal"
            };

            ignoreCaseBox = new CheckBox
            {
                Location = new Point(8, 38),
                Size = new Size(110, 22),
                Text = "Ignore case",
                Checked = true
            };

            fieldsBox = new CheckBox
            {
                Location = new Point(124, 38),
                Size = new Size(140, 22),
                Text = "Fields and types"
            };

            results = new ListView
            {
                Location = new Point(8, 66),
                Size = new Size(804, 366),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = true,
                HideSelection = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            results.Columns.Add("Type", 240);
            results.Columns.Add("Method", 170);
            results.Columns.Add("Offset", 70);
            results.Columns.Add("Line", 480);
            results.DoubleClick += OnResultActivate;
            results.KeyDown += OnResultKeyDown;

            status = new Label
            {
                Location = new Point(8, 438),
                Size = new Size(804, 18),
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Enter a regular expression and press Search."
            };

            Controls.Add(queryBox);
            Controls.Add(searchButton);
            Controls.Add(literalBox);
            Controls.Add(ignoreCaseBox);
            Controls.Add(fieldsBox);
            Controls.Add(results);
            Controls.Add(status);

            KeyDown += OnFormKeyDown;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            queryBox.Focus();
            if (!string.IsNullOrEmpty(queryBox.Text))
            {
                RunSearch();
            }
        }

        private void OnQueryKeyDown(object sender, KeyEventArgs e)        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RunSearch();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                Close();
            }
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                Close();
            }
            else if (e.KeyCode == Keys.F3)
            {
                e.SuppressKeyPress = true;
                RunSearch();
            }
        }

        private void RunSearch()
        {
            string query = queryBox.Text;
            if (query.Length == 0)
            {
                results.Items.Clear();
                status.Text = "Enter a pattern.";
                return;
            }

            IlSearch.Options options = new IlSearch.Options
            {
                IgnoreCase = ignoreCaseBox.Checked,
                UseRegex = !literalBox.Checked,
                SearchFields = fieldsBox.Checked,
                SearchTypes = fieldsBox.Checked,
                MaxResults = 2000
            };

            int token = ++generation;
            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            searchButton.Enabled = false;
            try
            {
                List<IlMatch> matches = search(query, options);
                if (token != generation || IsDisposed)
                {
                    return;
                }

                results.BeginUpdate();
                try
                {
                    results.Items.Clear();
                    foreach (IlMatch match in matches)
                    {
                        ListViewItem item = new ListViewItem(match.TypeName);
                        item.SubItems.Add(match.MethodName);
                        item.SubItems.Add(match.Offset == 0 && match.Field != "il"
                            ? "-"
                            : "IL_" + match.Offset.ToString("x4"));
                        item.SubItems.Add(match.Text == null ? string.Empty : match.Text.Trim());
                        item.Tag = match;
                        results.Items.Add(item);
                    }
                }
                finally
                {
                    results.EndUpdate();
                }

                status.Text = matches.Count == 0
                    ? "No matches."
                    : matches.Count + " match(es) — double-click or press Enter to go to the first one.";
                if (results.Items.Count > 0)
                {
                    results.Items[0].Selected = true;
                    results.Select();
                }
            }
            catch (ArgumentException ex)
            {
                status.Text = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("IL search failed.", ex);
                status.Text = "Search failed: " + ex.Message;
            }
            finally
            {
                Cursor = previous;
                searchButton.Enabled = true;
            }
        }

        private void OnResultActivate(object sender, EventArgs e)
        {
            ActivateSelection();
        }

        private void OnResultKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ActivateSelection();
            }
        }

        private void ActivateSelection()
        {
            if (results.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem item = results.SelectedItems[0];
            IlMatch match = item.Tag as IlMatch;
            if (match == null || navigate == null)
            {
                return;
            }

            try
            {
                navigate(match);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to navigate to the selected match.", ex);
            }
        }
    }
}
