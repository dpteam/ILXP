using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
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
        private readonly NavigationHistory navigationHistory;

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
            foreach (string file in files)
            {
                TreeNode node = LoadAssemblyTree(file);
                if (node != null)
                {
                    ReplaceAssemblyNode(node);
                }
            }
            treeView.EndUpdate();
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
        }

        private void ShowTextViewer(string text)
        {
            codeEditor.Visible = false;
            imageBox.Visible = false;
            textViewer.Visible = true;
            textViewer.Text = text;
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
                MessageBox.Show(this, string.Format("Unable to load the file \"{0}\".", path), "Cannot Load File");
                return null;
            }
        }

        private TreeNode BuildAssemblyTree(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            AssemblyDefinition assembly;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(path);
            }
            catch (Exception ex)
            {
                Log.Error("Cecil cannot read '" + path + "'.", ex);
                return null;
            }

            if (assembly == null)
            {
                return null;
            }

            TryReadSymbols(assembly, path);

            TreeNode root = new TreeNode(assembly.Name.Name);
            root.Tag = new AssemblyItem(path, assembly);
            SetImage(root, "0VSObject_Assembly.bmp");

            foreach (ModuleDefinition module in assembly.Modules)
            {
                TreeNode moduleNode = root.Nodes.Add(module.Name);
                SetImage(moduleNode, "1VSObject_Library.bmp");
                moduleNode.Tag = module;

                TreeNode referencesNode = moduleNode.Nodes.Add("References");
                SetImage(referencesNode, "4VSReferences.bmp");
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    TreeNode referenceNode = new TreeNode(string.Format("{0} ({1})", reference.Name, reference.Version));
                    referenceNode.Tag = reference;
                    SetImage(referenceNode, "5VSProject_reference.bmp");
                    referencesNode.Nodes.Add(referenceNode);
                }

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
                TreeNode baseTypesNode = new TreeNode("Base Types");
                SetImage(baseTypesNode, "4VSBaseTypes.bmp");
                if (type.BaseType != null)
                {
                    TreeNode baseTypeNode = new TreeNode(type.BaseType.Name);
                    SetImage(baseTypeNode, "7VSObject_Class.bmp");
                    baseTypesNode.Nodes.Add(baseTypeNode);
                }
                foreach (TypeReference @interface in type.Interfaces)
                {
                    TreeNode interfaceNode = new TreeNode(@interface.Name);
                    SetImage(interfaceNode, "7VSObject_Interface.bmp");
                    baseTypesNode.Nodes.Add(interfaceNode);
                }
                if (baseTypesNode.Nodes.Count > 0)
                {
                    typeNode.Nodes.Add(baseTypesNode);
                }

                foreach (TypeDefinition nested in type.NestedTypes)
                {
                    AddTypeNode(typeNode, nested);
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
                    if (accessorNames.Contains(method.Name))
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
                    typeNode.Nodes.Add(methodNode);
                }

                foreach (PropertyDefinition property in type.Properties)
                {
                    TreeNode propertyNode = new TreeNode(property.Name);
                    SetImage(propertyNode, GetPropertyImageKey(property));
                    propertyNode.Tag = property;
                    AddAccessorNode(propertyNode, property.SetMethod, property);
                    AddAccessorNode(propertyNode, property.GetMethod, property);
                    typeNode.Nodes.Add(propertyNode);
                }

                foreach (EventDefinition @event in type.Events)
                {
                    TreeNode eventNode = new TreeNode(@event.Name);
                    SetImage(eventNode, GetEventImageKey(@event));
                    eventNode.Tag = @event;
                    typeNode.Nodes.Add(eventNode);
                    AddAccessorNode(eventNode, @event.AddMethod, null);
                    AddAccessorNode(eventNode, @event.RemoveMethod, null);
                    foreach (MethodDefinition other in @event.OtherMethods)
                    {
                        AddAccessorNode(eventNode, other, null);
                    }
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
                    typeNode.Nodes.Add(fieldNode);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to build the tree for type '" + type.FullName + "'.", ex);
                typeNode.Tag = ex.Message;
            }
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
