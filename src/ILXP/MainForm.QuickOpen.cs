using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Mono.Cecil;
using ILXP.UI;

namespace ILXP
{
    /// <summary>
    /// Quick Open (Ctrl+P): fuzzy navigation over every type and member of the loaded
    /// assemblies. The index is built lazily and invalidated when the tree changes.
    /// </summary>
    public sealed partial class MainForm
    {
        private List<QuickOpenForm.Entry> quickOpenIndex;
        private bool quickOpenDirty = true;

        private void InvalidateQuickOpenIndex()
        {
            quickOpenDirty = true;
        }

        private List<QuickOpenForm.Entry> GetQuickOpenIndex()
        {
            if (!quickOpenDirty && quickOpenIndex != null)
            {
                return quickOpenIndex;
            }

            List<QuickOpenForm.Entry> entries = new List<QuickOpenForm.Entry>();
            foreach (TreeNode root in treeView.Nodes)
            {
                AssemblyItem item = root.Tag as AssemblyItem;
                if (item == null)
                {
                    continue;
                }

                try
                {
                    entries.AddRange(QuickOpenIndex.Build(item.Assembly));
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to index '" + item.Assembly.Name.Name + "'.", ex);
                }
            }

            quickOpenIndex = entries;
            quickOpenDirty = false;
            return entries;
        }

        private void OnQuickOpen(object sender, EventArgs e)
        {
            List<QuickOpenForm.Entry> index = GetQuickOpenIndex();
            if (index.Count == 0)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (QuickOpenForm form = new QuickOpenForm(index, string.Empty))
            {
                if (form.ShowDialog(this) != DialogResult.OK || form.SelectedEntry == null)
                {
                    return;
                }

                SelectNodeByPath(form.SelectedEntry.Path);
            }
        }
    }
}
