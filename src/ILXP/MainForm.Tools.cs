using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Mono.Cecil;
using ILXP.UI;

namespace ILXP
{
    /// <summary>
    /// Analysis tools that work on whole assemblies: version comparison, preservation
    /// bundle export and the "health" report entry point.
    /// </summary>
    public sealed partial class MainForm
    {
        // ------------------------------------------------------------------
        // Call graph
        // ------------------------------------------------------------------

        private void OnCallGraph(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                CallGraphReport report = CallGraph.Build(item.Assembly);
                string content = "assembly      : " + item.Assembly.Name.FullName + Environment.NewLine +
                                 CallGraph.Format(report, 15);
                using (HealthForm form = new HealthForm(item.Assembly.Name.Name + " (call graph)", content))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to build the call graph.", ex);
                MessageBox.Show(this, ex.Message, "Call graph", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = previous;
            }
        }

        /// <summary>
        /// Exports the selected assembly as a decompiled project: a project file, one source
        /// file per type, AssemblyInfo, resources and a README that states the limitations.
        /// </summary>
        private void OnExportProject(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder for the exported project";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                Cursor previous = Cursor;
                Cursor = Cursors.WaitCursor;
                try
                {
                    ProjectExportResult result = ProjectExporter.Export(item, dialog.SelectedPath,
                        "cs", true, null);
                    MessageBox.Show(this,
                        "Project written to:" + Environment.NewLine + result.OutputDirectory +
                        Environment.NewLine + Environment.NewLine + result.Summary(),
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to export the project.", ex);
                    MessageBox.Show(this, ex.Message, "Export project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = previous;
                }
            }
        }

        // ------------------------------------------------------------------
        // Compare two assemblies
        // ------------------------------------------------------------------

        private void OnCompareAssemblies(object sender, EventArgs e)
        {
            string leftPath = null;
            string rightPath = null;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select the OLD version";
                dialog.Filter = "Assemblies (*.exe;*.dll)|*.exe;*.dll|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                leftPath = dialog.FileName;

                dialog.Title = "Select the NEW version";
                dialog.FileName = string.Empty;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                rightPath = dialog.FileName;
            }

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                AssemblyDefinition left = AssemblyLoader.Load(leftPath).Assembly;
                AssemblyDefinition right = AssemblyLoader.Load(rightPath).Assembly;
                DiffReport report = AssemblyDiff.Compare(left, right);

                string content = AssemblyDiff.Format(report);
                if (!report.HasChanges)
                {
                    content = content.TrimEnd() + Environment.NewLine + Environment.NewLine +
                              "The two assemblies are metadata-identical.";
                }

                using (HealthForm form = new HealthForm(
                    Path.GetFileName(leftPath) + "  ->  " + Path.GetFileName(rightPath), content))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to compare the assemblies.", ex);
                MessageBox.Show(this, ex.Message, "Compare assemblies", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = previous;
            }
        }

        // ------------------------------------------------------------------
        // Preservation bundle
        // ------------------------------------------------------------------

        /// <summary>
        /// Writes a self-describing folder with the original bytes, IL, resources, health
        /// report and a manifest. This is the "data hoarding" export: everything needed to
        /// analyse the assembly later without the original machine.
        /// </summary>
        private void OnExportBundle(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder for the preservation bundle";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                Cursor previous = Cursor;
                Cursor = Cursors.WaitCursor;
                try
                {
                    PreservationBundle.Result result = PreservationBundle.Export(item, dialog.SelectedPath);
                    MessageBox.Show(this,
                        "Bundle written to:" + Environment.NewLine + result.Directory +
                        Environment.NewLine + Environment.NewLine +
                        result.Files + " files, " + result.Bytes / 1024 + " KB",
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to export the preservation bundle.", ex);
                    MessageBox.Show(this, ex.Message, "Export bundle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = previous;
                }
            }
        }
    }
}
