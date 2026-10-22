using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ILXP.Deobfuscation;
using ILXP.Interop;
using Mono.Cecil;
using Mono.Cecil.Pdb;

namespace ILXP
{
    /// <summary>
    /// Main window: assembly tree on the left, generated source on the right.
    /// </summary>
    public sealed partial class MainForm : Form
    {
        private LanguageWriter writer;
        private bool suppressHistory;
        private string searchText = string.Empty;
        private readonly NavigationHistory navigationHistory;

        /// <summary>Set by automation (screenshots, tests) to avoid modal startup dialogs.</summary>
        internal bool SuppressStartupDialogs { get; set; }

        /// <summary>
        /// Loads an assembly and selects a type, without any dialog. Used by the
        /// <c>--screenshot</c> command so that documentation images show real content.
        /// </summary>
        internal bool LoadForCapture(string assemblyPath, string typeName)
        {
            try
            {
                // Synchronous on purpose: the capture runs on the UI thread without a
                // message loop, so the asynchronous loader (and its overlay) must be skipped.
                TreeNode node = BuildAssemblyTree(assemblyPath);
                if (node == null)
                {
                    return false;
                }

                treeView.BeginUpdate();
                try
                {
                    AddOrReplaceAssemblyNode(node);
                    node.Expand();
                    if (node.Nodes.Count > 0)
                    {
                        node.Nodes[0].Expand();
                    }
                }
                finally
                {
                    treeView.EndUpdate();
                }

                TreeNode selected = node;
                if (!string.IsNullOrEmpty(typeName))
                {
                    // "Type" or "Type::member" (member may be a method, field, property or event).
                    string typePart = typeName;
                    string memberPart = null;
                    int separator = typeName.IndexOf("::", StringComparison.Ordinal);
                    if (separator > 0)
                    {
                        typePart = typeName.Substring(0, separator);
                        memberPart = typeName.Substring(separator + 2);
                    }

                    TreeNode typeNode = FindTypeNode(node, typePart);
                    if (typeNode != null)
                    {
                        selected = typeNode;
                        if (!string.IsNullOrEmpty(memberPart))
                        {
                            selected = FindMemberNodeByTag(typeNode, memberPart) ?? typeNode;
                        }
                    }
                }

                TreeNode parent = selected.Parent;
                while (parent != null)
                {
                    parent.Expand();
                    parent = parent.Parent;
                }

                treeView.SelectedNode = selected;
                selected.EnsureVisible();
                treeView.Focus();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to prepare the window for capture.", ex);
                return false;
            }
        }

        /// <summary>Finds a member node by name inside a type (expanding lazy children).</summary>
        private TreeNode FindMemberNodeByTag(TreeNode typeNode, string memberName)
        {
            ExpandLazyNode(typeNode);
            foreach (TreeNode child in typeNode.Nodes)
            {
                if (child.Tag is MethodDefinition && string.Equals(((MethodDefinition)child.Tag).Name, memberName, StringComparison.Ordinal))
                {
                    return child;
                }
                if (child.Tag is FieldDefinition && string.Equals(((FieldDefinition)child.Tag).Name, memberName, StringComparison.Ordinal))
                {
                    return child;
                }
                if (child.Tag is PropertyDefinition && string.Equals(((PropertyDefinition)child.Tag).Name, memberName, StringComparison.Ordinal))
                {
                    return child;
                }
                if (child.Tag is EventDefinition && string.Equals(((EventDefinition)child.Tag).Name, memberName, StringComparison.Ordinal))
                {
                    return child;
                }

                TreeNode nested = FindMemberNodeByTag(child, memberName);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        /// <summary>Finds a type node by full or short name, expanding only matching namespaces.</summary>
        private TreeNode FindTypeNode(TreeNode node, string typeName)
        {
            if (node.Tag is TypeDefinition)
            {
                TypeDefinition type = (TypeDefinition)node.Tag;
                if (string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(type.Name, typeName, StringComparison.Ordinal) ||
                    string.Equals(LanguageWriter.GetShortTypeName(type), typeName, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            // Only descend into namespaces that can actually contain the requested type.
            // Expanding everything would load every reference and build every member,
            // which looks like a hang on large assemblies.
            string expectedNamespace = null;
            int lastDot = typeName.LastIndexOf('.');
            if (lastDot > 0)
            {
                expectedNamespace = typeName.Substring(0, lastDot);
            }

            ExpandLazyNode(node);
            foreach (TreeNode child in node.Nodes)
            {
                ModuleItem module = child.Tag as ModuleItem;
                if (module != null && expectedNamespace != null &&
                    !string.Equals(module.Namespace, expectedNamespace, StringComparison.Ordinal))
                {
                    continue;
                }

                AssemblyNameReference reference = child.Tag as AssemblyNameReference;
                if (reference != null)
                {
                    continue; // references are never expanded while searching
                }

                TreeNode found = FindTypeNode(child, typeName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public MainForm()
        {
            navigationHistory = new NavigationHistory();
            ApplySystemDialogFont();
            InitializeComponent();

            Text = Text.Replace("<Version>", AppInfo.VersionText);

            languageCombo.Items.Add(new IlWriter());
            languageCombo.Items.Add(new CSharpWriter());
            languageCombo.Items.Add(new VbWriter());
            languageCombo.SelectedIndex = Math.Min(Math.Max(AppSettings.Default.SelectedLanguage, 0), languageCombo.Items.Count - 1);
            writer = languageCombo.SelectedItem as LanguageWriter;
            treeView.TreeViewNodeSorter = new TreeNodeComparer();
            treeView.BeforeExpand += OnTreeBeforeExpand;
            KeyPreview = true;
            KeyDown += OnMainKeyDown;

            Shown += OnShown;
        }

        /// <summary>
        /// The original application switched the global WinForms default font to the
        /// system dialog font so that the UI matches the current Windows version.
        /// </summary>
        private static void ApplySystemDialogFont()
        {
            try
            {
                FieldInfo field = typeof(Control).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic);
                if (field != null)
                {
                    Font font = new Font(SystemFonts.DialogFont.Name, Control.DefaultFont.SizeInPoints);
                    field.SetValue(null, font);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to apply the system dialog font.", ex);
            }
        }

        private void OnShown(object sender, EventArgs e)
        {
            if (SuppressStartupDialogs)
            {
                return;
            }

            Application.DoEvents();

            string lastPath = AppSettings.Default.LastSelectedNodePath;
            if (File.Exists(AppSettings.Default.LastFile))
            {
                TreeNode node = LoadAssemblyTree(AppSettings.Default.LastFile);
                if (node != null)
                {
                    treeView.BeginUpdate();
                    treeView.Nodes.Add(node);
                    treeView.SelectedNode = node;
                    node.Expand();
                    if (node.Nodes.Count > 0)
                    {
                        node.Nodes[0].Expand();
                    }
                    if (!string.IsNullOrEmpty(lastPath))
                    {
                        SelectNodeByPath(lastPath);
                    }
                    treeView.EndUpdate();
                }
            }
            else
            {
                OpenFile();
            }
        }

        private void OnOpenFile(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void OpenFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Assemblies (*.exe;*.dll)|*.exe;*.dll|All Files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                treeView.BeginUpdate();
                TreeNode node = LoadAssemblyTree(dialog.FileName);
                if (node != null)
                {
                    ReplaceAssemblyNode(node);
                }
                treeView.EndUpdate();
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnExportIl(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "IL files (*.il)|*.il|All Files (*.*)|*.*";
                    dialog.FileName = Path.GetFileNameWithoutExtension(item.FilePath) + ".il";
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    IlTools.Disassemble(item.FilePath, dialog.FileName);
                    MessageBox.Show(this, "IL written to " + dialog.FileName, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to export IL.", ex);
                MessageBox.Show(this, ex.Message, "IL Disassembler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnAssembleIl(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog input = new OpenFileDialog())
                {
                    input.Filter = "IL files (*.il)|*.il|All Files (*.*)|*.*";
                    if (input.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    using (SaveFileDialog output = new SaveFileDialog())
                    {
                        output.Filter = "Assembly (*.dll)|*.dll|Executable (*.exe)|*.exe";
                        output.FileName = Path.GetFileNameWithoutExtension(input.FileName) + ".dll";
                        if (output.ShowDialog(this) != DialogResult.OK)
                        {
                            return;
                        }

                        bool isDll = string.Equals(Path.GetExtension(output.FileName), ".dll", StringComparison.OrdinalIgnoreCase);
                        IlTools.Assemble(input.FileName, output.FileName, isDll, false, false);
                        MessageBox.Show(this, "Assembly written to " + output.FileName, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to assemble IL.", ex);
                MessageBox.Show(this, ex.Message, "IL Assembler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRecursiveFoldersChanged(object sender, EventArgs e)
        {
            AppSettings.Default.RecursiveFolders = recursiveFoldersMenuItem.Checked;
            AppSettings.Default.Save();
        }

        private void OnCleanAssembly(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the cleaned assembly";
                dialog.Filter = "Assembly (*.dll)|*.dll|Executable (*.exe)|*.exe";
                dialog.FileName = Path.GetFileNameWithoutExtension(item.FilePath) + ".cleaned" + Path.GetExtension(item.FilePath);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    AssemblyDefinition assembly = item.Assembly;
                    DeobfuscationReport report = DeobfuscationPipeline.Run(assembly, DeobfuscationPipeline.CreateDefault());
                    assembly.Write(dialog.FileName, new WriterParameters { WriteSymbols = false });

                    System.Text.StringBuilder text = new System.Text.StringBuilder();
                    text.AppendLine("Cleaned assembly written to:");
                    text.AppendLine(dialog.FileName);
                    text.AppendLine();
                    foreach (System.Collections.Generic.KeyValuePair<string, int> pair in report.Results)
                    {
                        text.AppendLine(pair.Key + ": " + pair.Value);
                    }
                    MessageBox.Show(this, text.ToString(), "Clean assembly", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to clean the assembly.", ex);
                    MessageBox.Show(this, ex.Message, "Clean assembly", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnOpenSettings(object sender, EventArgs e)
        {
            using (UI.SettingsForm form = new UI.SettingsForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            // Apply the settings that affect the live window.
            if (writer != null)
            {
                OutputDialect dialect = OutputDialect.FromId(AppSettings.Default.DecompilerTarget);
                if (dialect != null)
                {
                    writer.Dialect = dialect;
                }
            }

            languageCombo.SelectedIndex = Math.Min(Math.Max(AppSettings.Default.SelectedLanguage, 0),
                Math.Max(0, languageCombo.Items.Count - 1));
            recursiveFoldersMenuItem.Checked = AppSettings.Default.RecursiveFolders;

            if (treeView.SelectedNode != null)
            {
                DisplayNode(treeView.SelectedNode);
            }
            statusLabel.Text = "Settings saved";
        }

        private void OnFindInTree(object sender, EventArgs e)
        {
            searchBox.Focus();
            searchBox.SelectAll();
        }

        private void OnMainKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.P)
            {
                e.SuppressKeyPress = true;
                OnQuickOpen(sender, EventArgs.Empty);
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                e.SuppressKeyPress = true;
                OnGoToIlOffset(sender, EventArgs.Empty);
            }
        }

        private void OnSearchBoxTextChanged(object sender, EventArgs e)
        {
            searchText = searchBox.Text;
        }

        private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FindNext();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                searchBox.Text = string.Empty;
                treeView.Focus();
            }
        }

        /// <summary>Selects the next node whose text contains the search text.</summary>
        private void FindNext()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return;
            }

            TreeNode start = treeView.SelectedNode;
            TreeNode found = FindNextNode(start, searchText, true);
            if (found == null && start != null)
            {
                // Wrap around to the beginning.
                found = FindNextNode(null, searchText, true);
            }

            if (found == null)
            {
                statusLabel.Text = "Not found: " + searchText;
                return;
            }

            found.EnsureVisible();
            treeView.SelectedNode = found;
            statusLabel.Text = "Found: " + found.Text;
        }

        private static TreeNode FindNextNode(TreeNode from, string text, bool afterFrom)
        {
            // Depth-first walk with a simple "seen the starting node" flag.
            bool started = !afterFrom || from == null;
            return Walk(from == null ? null : from.TreeView.Nodes, text, ref started, from);
        }

        private static TreeNode Walk(TreeNodeCollection nodes, string text, ref bool started, TreeNode from)
        {
            if (nodes == null)
            {
                return null;
            }

            foreach (TreeNode node in nodes)
            {
                if (!started)
                {
                    if (node == from)
                    {
                        started = true;
                    }
                }
                else if (node.Text.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    return node;
                }

                TreeNode found = Walk(node.Nodes, text, ref started, from);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void OnOutputMenuOpening(object sender, EventArgs e)
        {
            outputMenu.DropDownItems.Clear();

            foreach (OutputDialect dialect in OutputDialect.All)
            {
                OutputDialect current = dialect;
                ToolStripMenuItem item = new ToolStripMenuItem(dialect.DisplayName + "  (" + dialect.Toolchain + ")");
                item.Checked = IsDialectSelected(current);
                item.Tag = current;
                item.Click += delegate
                {
                    ApplyDialect(current);
                };
                outputMenu.DropDownItems.Add(item);
            }

            outputMenu.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem decompile = new ToolStripMenuItem("Decompile to folder...");
            decompile.Click += OnDecompileToFolder;
            outputMenu.DropDownItems.Add(decompile);
        }

        private bool IsDialectSelected(OutputDialect dialect)
        {
            string id = AppSettings.Default.DecompilerTarget;
            if (string.Equals(id, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // "auto" means: whatever the language combo implies.
                if (dialect == OutputDialect.Il) return writer is IlWriter;
                if (dialect == OutputDialect.VisualBasic) return writer is VbWriter;
                if (dialect == OutputDialect.CSharp4) return writer is CSharpWriter;
                return false;
            }
            return string.Equals(dialect.Id, id, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyDialect(OutputDialect dialect)
        {
            if (dialect == null)
            {
                return;
            }

            if (dialect.IsCSharp)
            {
                languageCombo.SelectedItem = FindWriterItem(typeof(CSharpWriter));
            }
            else if (dialect == OutputDialect.VisualBasic)
            {
                languageCombo.SelectedItem = FindWriterItem(typeof(VbWriter));
            }
            else
            {
                languageCombo.SelectedItem = FindWriterItem(typeof(IlWriter));
            }

            if (writer != null)
            {
                writer.Dialect = dialect;
            }

            AppSettings.Default.DecompilerTarget = dialect.Id;
            AppSettings.Default.Save();
            statusLabel.Text = "Output: " + dialect.DisplayName;

            if (treeView.SelectedNode != null)
            {
                DisplayNode(treeView.SelectedNode);
            }
        }

        private object FindWriterItem(Type writerType)
        {
            foreach (object item in languageCombo.Items)
            {
                if (writerType.IsInstanceOfType(item))
                {
                    return item;
                }
            }
            return null;
        }

        private void OnDecompileToFolder(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            while (node != null && node.Parent != null)
            {
                node = node.Parent;
            }

            AssemblyItem item = node == null ? null : node.Tag as AssemblyItem;
            if (item == null)
            {
                MessageBox.Show(this, "Open an assembly first.", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the output folder";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    int count = Decompiler.ExportTypes(item.Assembly, writer, dialog.SelectedPath);
                    MessageBox.Show(this, "Wrote " + count + " files to " + dialog.SelectedPath,
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to decompile the assembly.", ex);
                    MessageBox.Show(this, ex.Message, "Decompile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            using (AboutForm form = new AboutForm())
            {
                form.ShowDialog(this);
            }
        }

        private void OnRefreshTree(object sender, EventArgs e)
        {
            RefreshAssemblies();
        }

        private void OnGoBack(object sender, EventArgs e)
        {
            GoBack();
        }

        private void OnGoForward(object sender, EventArgs e)
        {
            GoForward();
        }

        private void OnCloseAssembly(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null)
            {
                return;
            }

            treeView.SelectedNode.Remove();
            InvalidateQuickOpenIndex();
            navigationHistory.Clear();
            backButton.Enabled = navigationHistory.CanGoBack;
            backMenuItem.Enabled = navigationHistory.CanGoBack;
            forwardButton.Enabled = navigationHistory.CanGoForward;
            forwardMenuItem.Enabled = navigationHistory.CanGoForward;
            codeEditor.Visible = true;
            textViewer.Visible = false;
            imageBox.Visible = false;
            codeEditor.Clear();
        }

        private void OnCopyNodeText(object sender, EventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                Clipboard.SetText(treeView.SelectedNode.Text);
            }
        }

        private void OnSaveResourceAs(object sender, EventArgs e)
        {
            EmbeddedResource resource = treeView.SelectedNode == null ? null : treeView.SelectedNode.Tag as EmbeddedResource;
            if (resource == null)
            {
                return;
            }

            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.OverwritePrompt = true;
                    dialog.Filter = "All Files (*.*)|*.*";
                    dialog.FileName = treeView.SelectedNode.Text;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        File.WriteAllBytes(dialog.FileName, resource.GetResourceData());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to save the resource.", ex);
                MessageBox.Show(this, ex.Message, "Unable to save file.", MessageBoxButtons.OK);
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            AppSettings.Default.SelectedLanguage = languageCombo.SelectedIndex;
            writer = languageCombo.SelectedItem as LanguageWriter;
            if (treeView.SelectedNode != null)
            {
                DisplayNode(treeView.SelectedNode);
            }
        }

        private void OnViewMenuOpening(object sender, EventArgs e)
        {
            refreshMenuItem.Enabled = treeView.Nodes.Count > 0;
            backMenuItem.Enabled = navigationHistory.CanGoBack;
            forwardMenuItem.Enabled = navigationHistory.CanGoForward;
        }

        private void OnViewMenuClosed(object sender, EventArgs e)
        {
            refreshMenuItem.Enabled = true;
            backMenuItem.Enabled = true;
            forwardMenuItem.Enabled = true;
        }

        private void OnTreeMenuOpening(object sender, CancelEventArgs e)
        {
            if (treeView.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            bool isResource = treeView.SelectedNode.Tag is Resource;
            bool isAssembly = treeView.SelectedNode.Tag is AssemblyItem;
            contextBack.Enabled = navigationHistory.CanGoBack;
            contextForward.Enabled = navigationHistory.CanGoForward;
            contextDivider2.Visible = isResource || isAssembly;
            contextSaveAs.Visible = isResource;
            contextClose.Visible = isAssembly;
        }

        private void OnTreeDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void OnTreeDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            treeView.BeginUpdate();
            try
            {
                foreach (string path in files)
                {
                    if (Directory.Exists(path))
                    {
                        // Folders are scanned asynchronously; see LoadFolder.
                        LoadFolder(path, AppSettings.Default.RecursiveFolders);
                        continue;
                    }

                    TreeNode node = LoadAssemblyTree(path);
                    if (node != null)
                    {
                        AddOrReplaceAssemblyNode(node);
                    }
                }
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        private void OnTreeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (treeView.SelectedNode != e.Node)
            {
                treeView.SelectedNode = e.Node;
            }
        }

        private void OnTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            TypeLink link = e.Node.Tag as TypeLink;
            if (link != null)
            {
                NavigateToType(link.Type);
                return;
            }

            UpdateStatusAndHistory();
            DisplayNode(e.Node);
        }

        private void OnEditorLinkClicked(object sender, LinkClickedEventArgs e)
        {
            SelectNodeByPath(e.LinkText);
        }

        private void OnContentPanelPaint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawVisualStyleBorder(e.Graphics, new Rectangle(0, 0, contentPanel.Width, contentPanel.Height));
        }

        private void DisplayNode(TreeNode node)
        {
            object value = node.Tag;
            EmbeddedResource resource = value as EmbeddedResource;
            if (resource != null)
            {
                byte[] data = resource.GetResourceData();
                switch (FileContentInspector.Detect(resource.Name, data))
                {
                    case FileContentKind.Binary:
                        ShowTextViewer(FileContentInspector.FormatHexDump(data));
                        break;
                    case FileContentKind.Text:
                    case FileContentKind.Xml:
                        ShowTextViewer(FileContentInspector.DecodeText(data));
                        break;
                    case FileContentKind.Image:
                        codeEditor.Visible = false;
                        textViewer.Visible = false;
                        imageBox.Visible = true;
                        imageBox.BackgroundImage = FileContentInspector.TryLoadImage(data);
                        break;
                    case FileContentKind.ResX:
                        ShowTextViewer(FileContentInspector.FormatResourceTable(data));
                        break;
                }
                return;
            }

            codeEditor.Visible = true;
            textViewer.Visible = false;
            imageBox.Visible = false;

            if (value == null)
            {
                codeEditor.Clear();
            }
            else if (value is string)
            {
                codeEditor.Text = (string)value;
            }
            else if (value is MethodDefinition)
            {
                codeEditor.Text = writer.WriteMethod((MethodDefinition)value);
            }
            else if (value is FieldDefinition)
            {
                codeEditor.Text = writer.WriteField((FieldDefinition)value);
            }
            else if (value is PropertyDefinition)
            {
                codeEditor.Text = writer.WriteProperty((PropertyDefinition)value);
            }
            else if (value is AssemblyItem)
            {
                writer.Runtime = RuntimeProfile.Detect(((AssemblyItem)value).Assembly);
                codeEditor.Text = writer.WriteAssembly((AssemblyItem)value);
            }
            else if (value is ModuleDefinition)
            {
                codeEditor.Text = writer.WriteModule((ModuleDefinition)value);
            }
            else if (value is EventDefinition)
            {
                codeEditor.Text = writer.WriteEvent((EventDefinition)value);
            }
            else if (value is TypeDefinition)
            {
                codeEditor.Text = writer.WriteType((TypeDefinition)value);
            }
            else if (value is ModuleItem)
            {
                codeEditor.Text = writer.WriteNamespace((ModuleItem)value);
            }
            else
            {
                codeEditor.Clear();
            }

            // The IL map is derived from the rendered text, so it is rebuilt after every
            // document change (cheap: one regex pass).
            RebuildIlMap();
        }

        private void ShowTextViewer(string text)
        {
            codeEditor.Visible = false;
            imageBox.Visible = false;
            textViewer.Visible = true;
            textViewer.Text = text;
            ilMap = IlLineMap.Parse(string.Empty);
        }

        private void UpdateStatusAndHistory()
        {
            TreeNode node = treeView.SelectedNode;
            object value = node == null ? null : node.Tag;

            MethodDefinition method = value as MethodDefinition;
            FieldDefinition field = value as FieldDefinition;
            PropertyDefinition property = value as PropertyDefinition;
            EventDefinition eventDefinition = value as EventDefinition;
            TypeDefinition type = value as TypeDefinition;
            ModuleItem module = value as ModuleItem;

            if (method != null)
            {
                statusLabel.Text = method.DeclaringType.FullName + "::" + method.Name;
            }
            else if (field != null)
            {
                statusLabel.Text = field.DeclaringType.FullName + "::" + field.Name;
            }
            else if (property != null)
            {
                statusLabel.Text = property.DeclaringType.FullName + "::" + property.Name;
            }
            else if (eventDefinition != null)
            {
                statusLabel.Text = eventDefinition.DeclaringType.FullName + "::" + eventDefinition.Name;
            }
            else if (type != null)
            {
                statusLabel.Text = type.FullName;
            }
            else if (module != null)
            {
                statusLabel.Text = module.Namespace;
            }
            else
            {
                statusLabel.Text = string.Empty;
            }

            string path = GetSelectedNodePath();
            if (!suppressHistory && !string.IsNullOrEmpty(path))
            {
                navigationHistory.Navigate(path);
            }
            backButton.Enabled = navigationHistory.CanGoBack;
            backMenuItem.Enabled = navigationHistory.CanGoBack;
            forwardButton.Enabled = navigationHistory.CanGoForward;
            forwardMenuItem.Enabled = navigationHistory.CanGoForward;

            AppSettings.Default.LastSelectedNodePath = path;
            TreeNode root = node;
            while (root != null && root.Parent != null)
            {
                root = root.Parent;
            }
            if (root != null)
            {
                AssemblyItem item = root.Tag as AssemblyItem;
                if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                {
                    AppSettings.Default.LastFile = item.FilePath;
                }
            }
            AppSettings.Default.Save();
        }

        private void GoBack()
        {
            if (navigationHistory.CanGoBack)
            {
                suppressHistory = true;
                SelectNodeByPath(navigationHistory.Back());
                suppressHistory = false;
            }
        }

        private void GoForward()
        {
            if (navigationHistory.CanGoForward)
            {
                suppressHistory = true;
                SelectNodeByPath(navigationHistory.Forward());
                suppressHistory = false;
            }
        }

        private void RefreshAssemblies()
        {
            string path = GetSelectedNodePath();
            StringCollection files = new StringCollection();
            codeEditor.Clear();
            foreach (TreeNode node in treeView.Nodes)
            {
                AssemblyItem item = node.Tag as AssemblyItem;
                if (item != null)
                {
                    files.Add(item.FilePath);
                }
            }

            treeView.Nodes.Clear();
            Refresh();
            treeView.BeginUpdate();
            foreach (string file in files)
            {
                TreeNode node = LoadAssemblyTree(file);
                if (node != null)
                {
                    treeView.Nodes.Add(node);
                }
            }
            treeView.EndUpdate();
            SelectNodeByPath(path);
        }

        private TreeNode LoadAssemblyTree(string path)
        {
            try
            {
                return UiThread.Invoke(BuildAssemblyTree, path, this);
            }
            catch (Exception ex)
            {
                Log.Error("Unable to load '" + path + "'.", ex);
                if (!SuppressStartupDialogs)
                {
                    MessageBox.Show(this, string.Format("Unable to load the file \"{0}\".", path), "Cannot Load File");
                }
                return null;
            }
        }

        private TreeNode BuildAssemblyTree(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            LoadedAssembly loaded;
            try
            {
                loaded = AssemblyLoader.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error("Cannot read '" + path + "'.", ex);
                return null;
            }

            AssemblyDefinition assembly = loaded.Assembly;
            if (assembly == null)
            {
                return null;
            }

            if (!loaded.CarvedFromBlob)
            {
                TryReadSymbols(assembly, path);
            }

            TreeNode root = new TreeNode(assembly.Name.Name);
            if (loaded.CarvedFromBlob)
            {
                root.Text += " (carved)";
                root.ToolTipText = "PE image carved from " + loaded.SourcePath + " at offset 0x" + loaded.BlobOffset.ToString("x");
            }
            root.Tag = new AssemblyItem(path, assembly);
            SetImage(root, "0VSObject_Assembly.bmp");

            RuntimeProfile profile = RuntimeProfile.Detect(assembly);
            root.ToolTipText = string.IsNullOrEmpty(root.ToolTipText)
                ? profile.ToString()
                : root.ToolTipText + Environment.NewLine + profile.ToString();
            root.Text += "  [" + profile.FrameworkName + "]";

            TreeNode assemblyReferences = CreateLazyNode("References", "4VSReferences.bmp", delegate
            {
                return BuildAssemblyReferenceNodes(assembly);
            });
            root.Nodes.Add(assemblyReferences);

            foreach (ModuleDefinition module in assembly.Modules)
            {
                TreeNode moduleNode = root.Nodes.Add(module.Name);
                SetImage(moduleNode, "1VSObject_Library.bmp");
                moduleNode.Tag = module;

                TreeNode referencesNode = CreateLazyNode("References", "4VSReferences.bmp", delegate
                {
                    List<TreeNode> list = new List<TreeNode>();
                    foreach (AssemblyNameReference reference in module.AssemblyReferences)
                    {
                        list.Add(CreateReferenceNode(reference));
                    }
                    return list;
                });
                moduleNode.Nodes.Add(referencesNode);

                Dictionary<string, TreeNode> namespaces = new Dictionary<string, TreeNode>();
                foreach (TypeDefinition type in module.Types)
                {
                    TreeNode namespaceNode;
                    if (!namespaces.TryGetValue(type.Namespace, out namespaceNode))
                    {
                        namespaceNode = new TreeNode(string.IsNullOrEmpty(type.Namespace) ? "-" : type.Namespace);
                        namespaceNode.Tag = new ModuleItem(type.Namespace, module);
                        SetImage(namespaceNode, "6VSObject_Namespace.bmp");
                        moduleNode.Nodes.Add(namespaceNode);
                        namespaces.Add(type.Namespace, namespaceNode);
                    }
                    AddTypeNode(namespaceNode, type);
                }
            }

            TreeNode resourcesNode = root.Nodes.Add("Resources");
            SetImage(resourcesNode, "2VSResources.bmp");
            resourcesNode.SelectedImageKey = "2VSResources_Open.bmp";
            foreach (Resource resource in assembly.MainModule.Resources)
            {
                EmbeddedResource embedded = resource as EmbeddedResource;
                if (embedded == null)
                {
                    continue;
                }

                TreeNode resourceNode = new TreeNode(resource.Name);
                resourceNode.Tag = resource;
                resourcesNode.Nodes.Add(resourceNode);
                switch (FileContentInspector.Detect(resource.Name, embedded.GetResourceData()))
                {
                    case FileContentKind.Xml:
                        SetImage(resourceNode, "3VSProject_xml.bmp");
                        break;
                    case FileContentKind.Image:
                        SetImage(resourceNode, "3VSProject_imagefile.bmp");
                        break;
                    default:
                        SetImage(resourceNode, "3VSProject_resourcefile.bmp");
                        break;
                }
            }

            return root;
        }

        /// <summary>Builds the union of all references of an assembly (deduplicated).</summary>
        private List<TreeNode> BuildAssemblyReferenceNodes(AssemblyDefinition assembly)
        {
            List<TreeNode> list = new List<TreeNode>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (!seen.Add(reference.FullName))
                    {
                        continue;
                    }
                    list.Add(CreateReferenceNode(reference));
                }
            }
            if (list.Count == 0)
            {
                TreeNode none = new TreeNode("(no references)");
                none.ForeColor = Color.Gray;
                list.Add(none);
            }
            return list;
        }

        private static void TryReadSymbols(AssemblyDefinition assembly, string path)
        {
            try
            {
                string pdbPath = Path.ChangeExtension(path, ".pdb");
                if (!File.Exists(pdbPath))
                {
                    return;
                }

                PdbReaderProvider provider = new PdbReaderProvider();
                assembly.MainModule.ReadSymbols(provider.GetSymbolReader(assembly.MainModule, pdbPath));
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to read symbols for '" + path + "'.", ex);
            }
        }

        private static void SetImage(TreeNode node, string key)
        {
            node.ImageKey = key;
            node.SelectedImageKey = key;
        }

        private void AddTypeNode(TreeNode parent, TypeDefinition type)
        {
            TreeNode typeNode = new TreeNode(LanguageWriter.GetShortTypeName(type));
            SetImage(typeNode, GetTypeImageKey(type));
            if (type.IsNotPublic)
            {
                typeNode.ForeColor = Color.Gray;
            }
            typeNode.Tag = type;
            parent.Nodes.Add(typeNode);

            try
            {
                List<TreeNode> members = BuildTypeMembers(type);
                if (members.Count > 0)
                {
                    typeNode.Nodes.AddRange(members.ToArray());
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to build the tree for type '" + type.FullName + "'.", ex);
                typeNode.Tag = ex.Message;
            }
        }

        /// <summary>
        /// Builds the child nodes of a type: base types (clickable), nested types,
        /// methods, properties, events and fields.
        /// </summary>
        private List<TreeNode> BuildTypeMembers(TypeDefinition type)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            TreeNode baseTypesNode = new TreeNode("Base Types");
            SetImage(baseTypesNode, "4VSBaseTypes.bmp");
            if (type.BaseType != null)
            {
                baseTypesNode.Nodes.Add(CreateTypeLinkNode(type.BaseType, "7VSObject_Class.bmp"));
            }
            foreach (TypeReference @interface in type.Interfaces)
            {
                baseTypesNode.Nodes.Add(CreateTypeLinkNode(@interface, "7VSObject_Interface.bmp"));
            }
            if (baseTypesNode.Nodes.Count > 0)
            {
                nodes.Add(baseTypesNode);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                TreeNode nestedNode = new TreeNode(LanguageWriter.GetShortTypeName(nested));
                SetImage(nestedNode, GetTypeImageKey(nested));
                if (nested.IsNotPublic)
                {
                    nestedNode.ForeColor = Color.Gray;
                }
                nestedNode.Tag = nested;
                nestedNode.Nodes.Add(CreateLazyNode("...", "8VSObject_Method.bmp",
                    CreateTypeMembersFactory(nested)));
                nodes.Add(nestedNode);
            }

            List<string> accessorNames = new List<string>();
            foreach (PropertyDefinition property in type.Properties)
            {
                if (property.SetMethod != null)
                {
                    accessorNames.Add(property.SetMethod.Name);
                }
                if (property.GetMethod != null)
                {
                    accessorNames.Add(property.GetMethod.Name);
                }
            }
            foreach (EventDefinition @event in type.Events)
            {
                if (@event.AddMethod != null)
                {
                    accessorNames.Add(@event.AddMethod.Name);
                }
                if (@event.RemoveMethod != null)
                {
                    accessorNames.Add(@event.RemoveMethod.Name);
                }
                foreach (MethodDefinition other in @event.OtherMethods)
                {
                    if (other != null)
                    {
                        accessorNames.Add(other.Name);
                    }
                }
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (accessorNames.Contains(method.Name) || LanguageWriter.IsCompilerGeneratedName(method.Name))
                {
                    continue;
                }

                TreeNode methodNode = new TreeNode(LanguageWriter.GetMethodSignatureWithReturn(method));
                SetImage(methodNode, GetMethodImageKey(method));
                if (!method.IsPublic)
                {
                    methodNode.ForeColor = Color.Gray;
                }
                methodNode.Tag = method;
                nodes.Add(methodNode);
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                TreeNode propertyNode = new TreeNode(property.Name);
                SetImage(propertyNode, GetPropertyImageKey(property));
                propertyNode.Tag = property;
                AddAccessorNode(propertyNode, property.SetMethod, property);
                AddAccessorNode(propertyNode, property.GetMethod, property);
                nodes.Add(propertyNode);
            }

            foreach (EventDefinition @event in type.Events)
            {
                TreeNode eventNode = new TreeNode(@event.Name);
                SetImage(eventNode, GetEventImageKey(@event));
                eventNode.Tag = @event;
                AddAccessorNode(eventNode, @event.AddMethod, null);
                AddAccessorNode(eventNode, @event.RemoveMethod, null);
                foreach (MethodDefinition other in @event.OtherMethods)
                {
                    AddAccessorNode(eventNode, other, null);
                }
                nodes.Add(eventNode);
            }

            foreach (FieldDefinition field in type.Fields)
            {
                TreeNode fieldNode = new TreeNode(field.Name);
                SetImage(fieldNode, GetFieldImageKey(field));
                fieldNode.Tag = field;
                if (!field.IsPublic)
                {
                    fieldNode.ForeColor = Color.Gray;
                }
                nodes.Add(fieldNode);
            }

            return nodes;
        }

        private static void AddAccessorNode(TreeNode parent, MethodDefinition method, PropertyDefinition owner)
        {
            if (method == null)
            {
                return;
            }

            TreeNode node = new TreeNode(LanguageWriter.GetMethodSignatureWithReturn(method));
            node.Tag = method;
            SetImage(node, GetMethodImageKey(method));
            if (!method.IsPublic)
            {
                node.ForeColor = Color.Gray;
                if (owner != null)
                {
                    parent.ForeColor = Color.Gray;
                }
            }
            parent.Nodes.Add(node);
        }

        private static string GetPropertyImageKey(PropertyDefinition property)
        {
            MethodDefinition method = property.GetMethod ?? property.SetMethod;
            if (method == null)
            {
                return "9VSObject_Properties.bmp";
            }
            if (method.IsAssembly)
            {
                return "9VSObject_Properties_Friend.bmp";
            }
            if (method.IsFamily)
            {
                return "9VSObject_Properties_Protected.bmp";
            }
            if (method.IsPrivate)
            {
                return "9VSObject_Properties_Private.bmp";
            }
            return "9VSObject_Properties.bmp";
        }

        private static string GetMethodImageKey(MethodDefinition method)
        {
            if (method.IsReuseSlot && method.IsAssembly)
            {
                return "8VSObject_MethodOverload_Friend.bmp";
            }
            if (method.IsReuseSlot && method.IsFamily)
            {
                return "8VSObject_MethodOverload_Protected.bmp";
            }
            if (method.IsReuseSlot && method.IsPrivate)
            {
                return "8VSObject_MethodOverload_Private.bmp";
            }
            if (method.IsReuseSlot)
            {
                return "8VSObject_MethodOverload.bmp";
            }
            if (method.IsAssembly)
            {
                return "8VSObject_Method_Friend.bmp";
            }
            if (method.IsFamily)
            {
                return "8VSObject_Method_Protected.bmp";
            }
            if (method.IsPrivate)
            {
                return "8VSObject_Method_Private.bmp";
            }
            return "8VSObject_Method.bmp";
        }

        private static string GetFieldImageKey(FieldDefinition field)
        {
            bool constant = field.HasConstant;
            if (field.IsAssembly)
            {
                return constant ? "11VSObject_Constant_Friend.bmp" : "11VSObject_Field_Friend.bmp";
            }
            if (field.IsFamily)
            {
                return constant ? "11VSObject_Constant_Protected.bmp" : "11VSObject_Field_Protected.bmp";
            }
            if (field.IsPrivate)
            {
                return constant ? "11VSObject_Constant_Private.bmp" : "11VSObject_Field_Private.bmp";
            }
            return constant ? "11VSObject_Constant.bmp" : "11VSObject_Field.bmp";
        }

        private static string GetEventImageKey(EventDefinition @event)
        {
            MethodDefinition method = @event.AddMethod ?? @event.RemoveMethod ?? @event.InvokeMethod;
            if (method == null)
            {
                return "10VSObject_Event.bmp";
            }
            if (method.IsAssembly)
            {
                return "10VSObject_Event_Friend.bmp";
            }
            if (method.IsFamily)
            {
                return "10VSObject_Event_Protected.bmp";
            }
            if (method.IsPrivate)
            {
                return "10VSObject_Event_Private.bmp";
            }
            return "10VSObject_Event.bmp";
        }

        private static string GetTypeImageKey(TypeDefinition type)
        {
            string group;
            if (type.BaseType != null && type.BaseType.FullName.EndsWith("Delegate", StringComparison.Ordinal))
            {
                group = "Delegate";
            }
            else if (type.IsEnum)
            {
                group = "Enum";
            }
            else if (type.IsInterface)
            {
                group = "Interface";
            }
            else if (type.IsSequentialLayout)
            {
                group = "Structure";
            }
            else if (type.IsValueType)
            {
                group = "ValueType";
            }
            else if (type.IsClass)
            {
                group = type.IsSealed ? "Structure" : "Class";
            }
            else
            {
                group = "Module";
            }

            if (type.IsSealed && group != "Structure")
            {
                return "7VSObject_" + group + "_Sealed.bmp";
            }
            if (type.IsNestedFamily)
            {
                return "7VSObject_" + group + "_Protected.bmp";
            }
            if (type.IsNestedAssembly)
            {
                return "7VSObject_" + group + "_Friend.bmp";
            }
            if (type.IsNotPublic)
            {
                return "7VSObject_" + group + "_Private.bmp";
            }
            return "7VSObject_" + group + ".bmp";
        }

        private void ReplaceAssemblyNode(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            for (int i = treeView.Nodes.Count - 1; i >= 0; i--)
            {
                if (string.Equals(treeView.Nodes[i].Text, node.Text, StringComparison.Ordinal))
                {
                    treeView.Nodes[i].Remove();
                }
            }

            treeView.CollapseAll();
            treeView.Nodes.Add(node);
            treeView.SelectedNode = node;
            InvalidateQuickOpenIndex();
            node.Expand();
            if (node.Nodes.Count > 0)
            {
                node.Nodes[0].Expand();
            }
        }

        public string GetSelectedNodePath()
        {
            TreeNode node = treeView.SelectedNode;
            if (node == null)
            {
                return string.Empty;
            }

            List<object> chain = new List<object>();
            while (node != null)
            {
                if (node.Tag != null)
                {
                    chain.Insert(0, node.Tag);
                }
                node = node.Parent;
            }

            string path = string.Empty;
            foreach (object value in chain)
            {
                AssemblyItem assembly = value as AssemblyItem;
                if (assembly != null)
                {
                    path = assembly.Assembly.Name.FullName;
                    break;
                }
            }

            foreach (object value in chain)
            {
                Resource resource = value as Resource;
                if (resource != null)
                {
                    path += "//R" + resource.Name;
                    break;
                }
            }
            foreach (object value in chain)
            {
                ModuleItem module = value as ModuleItem;
                if (module != null)
                {
                    path += "//N" + module.Namespace;
                    break;
                }
            }
            foreach (object value in chain)
            {
                TypeDefinition type = value as TypeDefinition;
                if (type != null)
                {
                    path += "//" + type.Name;
                    break;
                }
            }
            foreach (object value in chain)
            {
                MethodDefinition method = value as MethodDefinition;
                if (method != null)
                {
                    return path + "//M" + LanguageWriter.GetMethodSignature(method);
                }
            }
            foreach (object value in chain)
            {
                PropertyDefinition property = value as PropertyDefinition;
                if (property != null)
                {
                    return path + "//P" + property.Name;
                }
            }
            foreach (object value in chain)
            {
                FieldDefinition field = value as FieldDefinition;
                if (field != null)
                {
                    return path + "//F" + field.Name;
                }
            }
            foreach (object value in chain)
            {
                EventDefinition @event = value as EventDefinition;
                if (@event != null)
                {
                    return path + "//E" + @event.Name;
                }
            }
            return path;
        }

        public void SelectNodeByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string[] parts = path.Split(new[] { "//" }, StringSplitOptions.None);
            if (parts.Length == 0)
            {
                return;
            }

            TreeNode node = null;
            foreach (TreeNode candidate in treeView.Nodes)
            {
                AssemblyItem item = candidate.Tag as AssemblyItem;
                if (item != null && string.Equals(item.Assembly.Name.FullName, parts[0], StringComparison.Ordinal))
                {
                    node = candidate;
                    break;
                }
            }

            if (node == null)
            {
                string file = GetAssemblyPath(parts[0]);
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }

                node = LoadAssemblyTree(file);
                if (node == null)
                {
                    return;
                }
                ReplaceAssemblyNode(node);
            }

            if (parts.Length > 1 && parts[1].StartsWith("R", StringComparison.Ordinal))
            {
                string resourceName = parts[1].Substring(1);
                foreach (TreeNode group in node.Nodes)
                {
                    if (!string.Equals(group.Text, "Resources", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    foreach (TreeNode resourceNode in group.Nodes)
                    {
                        Resource resource = resourceNode.Tag as Resource;
                        if (resource != null && string.Equals(resource.Name, resourceName, StringComparison.Ordinal))
                        {
                            treeView.SelectedNode = resourceNode;
                            resourceNode.EnsureVisible();
                            return;
                        }
                    }
                    return;
                }
            }

            if (parts.Length > 1 && parts[1].StartsWith("N", StringComparison.Ordinal))
            {
                string namespaceName = parts[1].Substring(1);
                bool found = false;
                foreach (TreeNode moduleNode in node.Nodes)
                {
                    if (string.Equals(moduleNode.Text, "Resources", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    foreach (TreeNode namespaceNode in moduleNode.Nodes)
                    {
                        ModuleItem module = namespaceNode.Tag as ModuleItem;
                        if (module != null && string.Equals(module.Namespace, namespaceName, StringComparison.Ordinal))
                        {
                            node = namespaceNode;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    return;
                }

                if (parts.Length > 2)
                {
                    found = false;
                    foreach (TreeNode typeNode in node.Nodes)
                    {
                        TypeDefinition type = typeNode.Tag as TypeDefinition;
                        if (type != null && string.Equals(type.Name.Replace("<", string.Empty).Replace(">", string.Empty), parts[2], StringComparison.Ordinal))
                        {
                            node = typeNode;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return;
                    }
                }

                if (parts.Length > 3)
                {
                    string selector = parts[3];
                    char kind = selector.Length > 0 ? selector[0] : ' ';
                    string expected = selector.Length > 0 ? selector.Substring(1) : string.Empty;
                    TreeNode match = FindMemberNode(node, kind, expected);
                    if (match == null)
                    {
                        return;
                    }
                    node = match;
                }
            }

            treeView.SelectedNode = node;
            node.EnsureVisible();
        }

        private static TreeNode FindMemberNode(TreeNode parent, char kind, string expected)
        {
            foreach (TreeNode child in parent.Nodes)
            {
                MethodDefinition method = child.Tag as MethodDefinition;
                if (method != null)
                {
                    if (kind == 'M' && string.Equals(LanguageWriter.GetMethodSignature(method), expected, StringComparison.Ordinal))
                    {
                        return child;
                    }
                    continue;
                }

                PropertyDefinition property = child.Tag as PropertyDefinition;
                if (property != null)
                {
                    if (kind == 'P' && string.Equals(property.Name, expected, StringComparison.Ordinal))
                    {
                        return child;
                    }

                    // Members may be nested below their property/event node.
                    TreeNode nested = FindMemberNode(child, kind, expected);
                    if (nested != null)
                    {
                        return nested;
                    }
                    continue;
                }

                if (kind == 'F')
                {
                    FieldDefinition field = child.Tag as FieldDefinition;
                    if (field != null && string.Equals(field.Name, expected, StringComparison.Ordinal))
                    {
                        return child;
                    }
                }
                else if (kind == 'E')
                {
                    EventDefinition @event = child.Tag as EventDefinition;
                    if (@event != null && string.Equals(@event.Name, expected, StringComparison.Ordinal))
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private string GetAssemblyPath(string assemblyName)
        {
            string path = null;
            try
            {
                path = AssemblyResolver.ResolveFromTree(assemblyName, treeView.Nodes);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to resolve '" + assemblyName + "'.", ex);
            }

            if (string.IsNullOrEmpty(path))
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "Locate " + assemblyName;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        path = dialog.FileName;
                    }
                }
            }
            return path;
        }
    }
}
