using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Mono.Cecil;
using ILXP.UI;

namespace ILXP
{
    /// <summary>
    /// Search and clipboard helpers: IL search over the loaded assemblies (Ctrl+Shift+F) and
    /// the "Copy as…" variants of the tree context menu.
    /// </summary>
    public sealed partial class MainForm
    {
        // ------------------------------------------------------------------
        // IL search
        // ------------------------------------------------------------------

        private void OnSearchInIl(object sender, EventArgs e)
        {
            List<AssemblyDefinition> assemblies = GetLoadedAssemblies();
            if (assemblies.Count == 0)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (IlSearchForm form = new IlSearchForm(
                delegate(string query, IlSearch.Options options)
                {
                    List<IlMatch> results = new List<IlMatch>();
                    foreach (AssemblyDefinition assembly in assemblies)
                    {
                        try
                        {
                            results.AddRange(IlSearch.Search(assembly, query, options));
                        }
                        catch (ArgumentException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("IL search failed for '" + assembly.Name.Name + "'.", ex);
                        }

                        if (results.Count >= options.MaxResults)
                        {
                            break;
                        }
                    }
                    return results;
                },
                NavigateToIlMatch,
                string.Empty))
            {
                form.ShowDialog(this);
            }
        }

        /// <summary>Jumps to the method and selects the matching line.</summary>
        private void NavigateToIlMatch(IlMatch match)
        {
            if (match == null)
            {
                return;
            }

            SelectNodeByPath(match.MethodPath);

            // The editor content is refreshed by the selection; the map is rebuilt there.
            if (match.Field != "il" || match.Line <= 0)
            {
                return;
            }

            if (match.Offset > 0)
            {
                GoToIlOffset(match.Offset);
            }
            else
            {
                codeEditor.SelectLine(match.Line);
            }
        }

        private List<AssemblyDefinition> GetLoadedAssemblies()
        {
            List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
            foreach (TreeNode root in treeView.Nodes)
            {
                AssemblyItem item = root.Tag as AssemblyItem;
                if (item != null && item.Assembly != null)
                {
                    assemblies.Add(item.Assembly);
                }
            }
            return assemblies;
        }

        // ------------------------------------------------------------------
        // Copy as…
        // ------------------------------------------------------------------

        private void OnCopyAs(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            if (node == null)
            {
                return;
            }

            ContextMenuStrip menu = new ContextMenuStrip();
            AddCopyItem(menu, "Full name", delegate { return GetNodeFullName(node); });
            AddCopyItem(menu, "Short name", delegate { return node.Text; });

            MethodDefinition method = node.Tag as MethodDefinition;
            if (method != null)
            {
                AddCopyItem(menu, "Signature", delegate { return LanguageWriter.GetMethodSignatureWithReturn(method); });
                AddCopyItem(menu, "IL", delegate { return new IlWriter().StripMarkup(new IlWriter().WriteMethod(method)); });
                AddCopyItem(menu, "C#", delegate { return new CSharpWriter().StripMarkup(new CSharpWriter().WriteMethod(method)); });
                AddCopyItem(menu, "VB", delegate { return new VbWriter().StripMarkup(new VbWriter().WriteMethod(method)); });
            }

            TypeDefinition type = node.Tag as TypeDefinition;
            if (type != null)
            {
                AddCopyItem(menu, "C# declaration", delegate { return new CSharpWriter().StripMarkup(new CSharpWriter().WriteType(type)); });
                AddCopyItem(menu, "IL declaration", delegate { return new IlWriter().StripMarkup(new IlWriter().WriteType(type)); });
            }

            AssemblyItem assembly = node.Tag as AssemblyItem;
            if (assembly != null)
            {
                AddCopyItem(menu, "Assembly references", delegate { return FormatReferences(assembly.Assembly); });
                AddCopyItem(menu, "Target framework", delegate { return RuntimeProfile.Detect(assembly.Assembly).ToString(); });
            }

            menu.Items.Add(new ToolStripSeparator());
            AddCopyItem(menu, "Node path (navigation key)", delegate { return GetSelectedNodePath(); });

            menu.Show(Cursor.Position);
        }

        private static void AddCopyItem(ContextMenuStrip menu, string text, Func<string> getter)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += delegate
            {
                try
                {
                    string value = getter();
                    if (!string.IsNullOrEmpty(value))
                    {
                        Clipboard.SetText(value);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to copy '" + text + "'.", ex);
                }
            };
            menu.Items.Add(item);
        }

        private static string GetNodeFullName(TreeNode node)
        {
            MethodDefinition method = node.Tag as MethodDefinition;
            if (method != null)
            {
                return method.DeclaringType.FullName + "::" + method.Name;
            }

            FieldDefinition field = node.Tag as FieldDefinition;
            if (field != null)
            {
                return field.DeclaringType.FullName + "::" + field.Name;
            }

            PropertyDefinition property = node.Tag as PropertyDefinition;
            if (property != null)
            {
                return property.DeclaringType.FullName + "::" + property.Name;
            }

            EventDefinition @event = node.Tag as EventDefinition;
            if (@event != null)
            {
                return @event.DeclaringType.FullName + "::" + @event.Name;
            }

            TypeDefinition type = node.Tag as TypeDefinition;
            if (type != null)
            {
                return type.FullName;
            }

            ModuleItem module = node.Tag as ModuleItem;
            if (module != null)
            {
                return module.Module.Assembly.Name.Name + " // " + module.Namespace;
            }

            AssemblyItem assembly = node.Tag as AssemblyItem;
            if (assembly != null)
            {
                return assembly.Assembly.Name.FullName;
            }

            return node.Text;
        }

        private static string FormatReferences(AssemblyDefinition assembly)
        {
            StringBuilder text = new StringBuilder();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (seen.Add(reference.FullName))
                    {
                        text.AppendLine(reference.FullName);
                    }
                }
            }
            return text.ToString().TrimEnd();
        }
    }
}
