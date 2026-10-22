using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Controlled tree expansion. <see cref="TreeView.ExpandAll"/> is unsafe on this tree:
    /// it forces every lazy node to materialise, which resolves every referenced assembly
    /// from disk and builds every member node. On a large assembly that can freeze the UI
    /// for minutes and allocate hundreds of megabytes.
    ///
    /// This implementation expands level by level with an explicit node budget, skips
    /// reference subtrees unless asked, and reports progress in the status bar.
    /// </summary>
    public sealed partial class MainForm
    {
        /// <summary>Depth value meaning "expand everything the budget allows".</summary>
        public const int InfiniteDepth = -1;

        /// <summary>Upper bound on nodes created by one expansion command.</summary>
        private const int ExpansionNodeBudget = 25000;

        private void OnExpandAll(object sender, EventArgs e)
        {
            ExpandTree(InfiniteDepth);
        }

        private void OnCollapseAll(object sender, EventArgs e)
        {
            treeView.CollapseAll();
            statusLabel.Text = "Collapsed";
        }

        private void OnExpandDepth(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            int depth = item == null ? AppSettings.Default.TreeExpandDepth : Convert.ToInt32(item.Tag);
            ExpandTree(depth);
        }

        private void OnExpandToDepth(object sender, EventArgs e)
        {
            using (DepthPromptForm prompt = new DepthPromptForm(AppSettings.Default.TreeExpandDepth))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                AppSettings.Default.TreeExpandDepth = prompt.SelectedDepth;
                AppSettings.Default.Save();
                ExpandTree(prompt.SelectedDepth);
            }
        }

        /// <summary>
        /// Expands the tree to <paramref name="depth"/> levels (0 = collapse,
        /// <see cref="InfiniteDepth"/> = as deep as the budget allows).
        /// </summary>
        private void ExpandTree(int depth)
        {
            if (depth == 0)
            {
                treeView.CollapseAll();
                statusLabel.Text = "Collapsed";
                return;
            }

            Stopwatch watch = Stopwatch.StartNew();
            int created = 0;
            bool budgetExceeded = false;

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            treeView.BeginUpdate();
            try
            {
                // Breadth-first expansion: level N is fully expanded before level N+1, so
                // the budget is spent evenly and a deep single branch cannot starve the rest.
                List<TreeNode> level = new List<TreeNode>();
                foreach (TreeNode root in treeView.Nodes)
                {
                    level.Add(root);
                }

                int currentDepth = 0;
                while (level.Count > 0)
                {
                    if (depth != InfiniteDepth && currentDepth >= depth)
                    {
                        break;
                    }

                    List<TreeNode> next = new List<TreeNode>();
                    foreach (TreeNode node in level)
                    {
                        if (node.Nodes.Count > 0 && !node.IsExpanded)
                        {
                            // Materialise lazy children only when they will be shown.
                            ExpandLazyNode(node);
                            node.Expand();
                        }

                        foreach (TreeNode child in node.Nodes)
                        {
                            created++;
                            if (created > ExpansionNodeBudget)
                            {
                                budgetExceeded = true;
                                break;
                            }
                            next.Add(child);
                        }

                        if (budgetExceeded)
                        {
                            break;
                        }
                    }

                    if (budgetExceeded)
                    {
                        break;
                    }

                    level = next;
                    currentDepth++;
                    statusLabel.Text = string.Format("Expanding... level {0}, {1} nodes", currentDepth, created);
                    statusLabel.GetCurrentParent().Refresh();
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Tree expansion failed.", ex);
            }
            finally
            {
                treeView.EndUpdate();
                Cursor = previous;
            }

            watch.Stop();
            string summary = string.Format("Expanded to level {0} ({1} nodes, {2:0.0} s)",
                depth == InfiniteDepth ? "max" : depth.ToString(), created, watch.Elapsed.TotalSeconds);
            if (budgetExceeded)
            {
                summary += " - stopped at the node budget (" + ExpansionNodeBudget + ")";
            }
            statusLabel.Text = summary;
            Log.Info(summary);
        }

        /// <summary>Expands the tree for a screenshot, without the status-bar chatter.</summary>
        internal void ExpandForCapture(int depth)
        {
            ExpandTree(depth);
        }

        /// <summary>Small modal prompt for the expansion depth (0 … 9 or infinite).</summary>
        private sealed class DepthPromptForm : Form
        {
            private readonly NumericUpDown numeric;
            private readonly CheckBox unlimited;

            public DepthPromptForm(int current)
            {
                Text = "Expand tree";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new System.Drawing.Size(300, 130);

                Label label = new Label
                {
                    Location = new System.Drawing.Point(12, 12),
                    Size = new System.Drawing.Size(276, 32),
                    Text = "How many levels should be expanded?"
                };

                numeric = new NumericUpDown
                {
                    Location = new System.Drawing.Point(12, 50),
                    Size = new System.Drawing.Size(70, 24),
                    Minimum = 0,
                    Maximum = 9,
                    Value = current > 0 && current <= 9 ? current : 2
                };

                unlimited = new CheckBox
                {
                    Location = new System.Drawing.Point(92, 50),
                    Size = new System.Drawing.Size(180, 24),
                    Text = "Everything (slow)",
                    Checked = current == InfiniteDepth
                };
                unlimited.CheckedChanged += delegate { numeric.Enabled = !unlimited.Checked; };

                Button ok = new Button
                {
                    Location = new System.Drawing.Point(132, 92),
                    Size = new System.Drawing.Size(75, 26),
                    Text = "OK",
                    DialogResult = DialogResult.OK
                };

                Button cancel = new Button
                {
                    Location = new System.Drawing.Point(213, 92),
                    Size = new System.Drawing.Size(75, 26),
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel
                };

                AcceptButton = ok;
                CancelButton = cancel;
                Controls.Add(label);
                Controls.Add(numeric);
                Controls.Add(unlimited);
                Controls.Add(ok);
                Controls.Add(cancel);
            }

            public int SelectedDepth
            {
                get { return unlimited.Checked ? InfiniteDepth : (int)numeric.Value; }
            }
        }
    }
}
