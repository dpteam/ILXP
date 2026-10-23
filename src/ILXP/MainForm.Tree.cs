using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Lazy tree nodes and folder loading. The tree stays responsive because children
    /// of references and namespaces are created on demand and because folders are
    /// scanned on a worker thread.
    /// </summary>
    public sealed partial class MainForm
    {
        /// <summary>A node whose children are produced on first expand.</summary>
        private sealed class LazyNode
        {
            public LazyNode(Func<List<TreeNode>> factory)
            {
                Factory = factory;
            }

            public Func<List<TreeNode>> Factory { get; private set; }
        }

        /// <summary>A clickable link to a type that lives in another assembly.</summary>
        /// <summary>
        /// A reference to another assembly. It becomes browsable in place: the same node shows
        /// the assembly code and its namespaces, instead of a metadata-only file node plus a
        /// separate module node for the code.
        /// </summary>
        private sealed class ReferenceItem
        {
            public ReferenceItem(AssemblyNameReference reference)
            {
                Reference = reference;
            }

            public AssemblyNameReference Reference { get; private set; }

            /// <summary>Loaded on first use; null when the file cannot be found.</summary>
            public AssemblyDefinition Assembly { get; set; }

            public string Path { get; set; }

            public bool Tried { get; set; }
        }

        private sealed class TypeLink
        {
            public TypeLink(TypeReference type)
            {
                Type = type;
            }

            public TypeReference Type { get; private set; }
        }

        private readonly Dictionary<string, AssemblyDefinition> assemblyCache =
            new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);

        private int folderLoadGeneration;

        private void OnTreeBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ExpandLazyNode(e.Node);
        }

        /// <summary>Replaces the placeholder child of a lazy node with real children.</summary>
        private void ExpandLazyNode(TreeNode node)
        {
            if (node == null || node.Nodes.Count != 1)
            {
                return;
            }

            LazyNode lazy = node.Nodes[0].Tag as LazyNode;
            if (lazy == null)
            {
                return;
            }

            node.Nodes.Clear();
            treeView.BeginUpdate();
            try
            {
                List<TreeNode> children = lazy.Factory();
                if (children != null && children.Count > 0)
                {
                    node.Nodes.AddRange(children.ToArray());
                }
                else
                {
                    TreeNode empty = new TreeNode("(empty)");
                    empty.ForeColor = System.Drawing.Color.Gray;
                    node.Nodes.Add(empty);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to expand node '" + node.Text + "'.", ex);
                TreeNode failed = new TreeNode("(failed: " + ex.Message + ")");
                failed.ForeColor = System.Drawing.Color.Firebrick;
                node.Nodes.Add(failed);
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        private static TreeNode CreateLazyNode(string text, string imageKey, Func<List<TreeNode>> factory)
        {
            TreeNode node = new TreeNode(text);
            SetImage(node, imageKey);
            TreeNode placeholder = new TreeNode("...");
            placeholder.Tag = new LazyNode(factory);
            node.Nodes.Add(placeholder);
            return node;
        }

        /// <summary>
        /// Creates a node for a reference. Expanding it loads the referenced assembly and
        /// exposes its namespaces.
        /// </summary>
        /// <summary>
        /// Creates a node for a reference. The node itself carries the assembly (code and
        /// metadata), and expanding it lists the namespaces of the referenced assembly.
        /// </summary>
        private TreeNode CreateReferenceNode(AssemblyNameReference reference)
        {
            ReferenceItem item = new ReferenceItem(reference);
            TreeNode node = new TreeNode(string.Format("{0} ({1})", reference.Name, reference.Version));
            node.Tag = item;
            SetImage(node, "5VSProject_reference.bmp");
            node.ToolTipText = reference.FullName;

            TreeNode placeholder = new TreeNode("...");
            placeholder.Tag = new LazyNode(delegate { return BuildReferenceChildren(node, item); });
            node.Nodes.Add(placeholder);
            return node;
        }

        /// <summary>
        /// Loads the referenced assembly and returns its namespaces. One node per reference keeps
        /// code, metadata and namespaces together instead of a metadata-only file node plus a
        /// separate module node.
        /// </summary>
        private List<TreeNode> BuildReferenceChildren(TreeNode node, ReferenceItem item)
        {
            List<TreeNode> children = new List<TreeNode>();

            if (!item.Tried)
            {
                item.Tried = true;
                try
                {
                    item.Path = AssemblyResolver.ResolveFromTree(item.Reference.FullName, treeView.Nodes);
                }
                catch (Exception ex)
                {
                    Log.Info("Reference '" + item.Reference.Name + "' is not available on disk: " + ex.Message);
                }

                if (!string.IsNullOrEmpty(item.Path))
                {
                    item.Assembly = GetCachedAssembly(item.Path);
                }

                if (item.Assembly != null)
                {
                    // The code is available now, so selecting the node shows the assembly.
                    node.ToolTipText = item.Path;
                }
            }

            if (string.IsNullOrEmpty(item.Path))
            {
                TreeNode missing = new TreeNode("(assembly not found)");
                missing.ForeColor = System.Drawing.Color.Gray;
                children.Add(missing);
                return children;
            }

            if (item.Assembly == null)
            {
                TreeNode failed = new TreeNode("(unable to read " + Path.GetFileName(item.Path) + ")");
                failed.ForeColor = System.Drawing.Color.Firebrick;
                children.Add(failed);
                return children;
            }

            return BuildNamespaceNodes(item.Assembly);
        }

        /// <summary>
        /// Groups the types of every module by namespace, so one namespace is one node even when
        /// the assembly is split over several modules.
        /// </summary>
        private List<TreeNode> BuildNamespaceNodes(AssemblyDefinition assembly)
        {
            Dictionary<string, List<ModuleDefinition>> namespaces =
                new Dictionary<string, List<ModuleDefinition>>(StringComparer.Ordinal);
            List<string> order = new List<string>();

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }

                    List<ModuleDefinition> modules;
                    if (!namespaces.TryGetValue(type.Namespace, out modules))
                    {
                        modules = new List<ModuleDefinition>();
                        namespaces.Add(type.Namespace, modules);
                        order.Add(type.Namespace);
                    }
                    if (!modules.Contains(module))
                    {
                        modules.Add(module);
                    }
                }
            }

            List<TreeNode> result = new List<TreeNode>();
            foreach (string namespaceName in order)
            {
                List<ModuleDefinition> modules = namespaces[namespaceName];
                TreeNode namespaceNode = CreateLazyNode(
                    string.IsNullOrEmpty(namespaceName) ? "-" : namespaceName,
                    "6VSObject_Namespace.bmp",
                    CreateNamespaceFactory(namespaceName, modules));
                namespaceNode.Tag = new ModuleItem(namespaceName, modules[0], modules);
                result.Add(namespaceNode);
            }
            return result;
        }

        private Func<List<TreeNode>> CreateNamespaceFactory(string namespaceName,
            IList<ModuleDefinition> modules)
        {
            return delegate
            {
                List<TreeNode> list = new List<TreeNode>();
                foreach (ModuleDefinition module in modules)
                {
                    foreach (TypeDefinition type in module.Types)
                    {
                        if (!string.Equals(type.Namespace, namespaceName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        TreeNode typeNode = new TreeNode(LanguageWriter.GetShortTypeName(type));
                        typeNode.Tag = type;
                        SetImage(typeNode, GetTypeImageKey(type));
                        if (type.IsNotPublic)
                        {
                            typeNode.ForeColor = System.Drawing.Color.Gray;
                        }
                        typeNode.Nodes.Add(CreateLazyNode("...", "8VSObject_Method.bmp",
                            CreateTypeMembersFactory(type)));
                        list.Add(typeNode);
                    }
                }
                return list;
            };
        }

        private Func<List<TreeNode>> CreateTypeMembersFactory(TypeDefinition type)
        {
            return delegate
            {
                try
                {
                    return BuildTypeMembers(type);
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to build members for '" + type.FullName + "'.", ex);
                    List<TreeNode> failed = new List<TreeNode>();
                    TreeNode node = new TreeNode("(failed: " + ex.Message + ")");
                    node.ForeColor = System.Drawing.Color.Firebrick;
                    failed.Add(node);
                    return failed;
                }
            };
        }

        /// <summary>Loads (and caches) an assembly for reference browsing.</summary>
        private AssemblyDefinition GetCachedAssembly(string path)
        {
            string key;
            try
            {
                key = Path.GetFullPath(path);
            }
            catch (Exception)
            {
                key = path;
            }

            AssemblyDefinition assembly;
            if (assemblyCache.TryGetValue(key, out assembly))
            {
                return assembly;
            }

            try
            {
                LoadedAssembly loaded = AssemblyLoader.Load(path);
                assemblyCache[key] = loaded.Assembly;
                return loaded.Assembly;
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to load referenced assembly '" + path + "'.", ex);
                assemblyCache[key] = null;
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Type links (base types / interfaces)
        // ------------------------------------------------------------------

        private static TreeNode CreateTypeLinkNode(TypeReference type, string imageKey)
        {
            string name = LanguageWriter.GetShortTypeName(type);
            TreeNode node = new TreeNode(name);
            node.Tag = new TypeLink(type);
            node.ForeColor = System.Drawing.Color.FromArgb(0, 0, 128);
            node.ToolTipText = type.FullName;
            SetImage(node, imageKey);
            return node;
        }

        private void NavigateToType(TypeReference type)
        {
            if (type == null)
            {
                return;
            }

            string key = LanguageWriter.GetReferenceKey(type);
            SelectNodeByPath(StripLinkMarkers(key));
        }

        internal static string StripLinkMarkers(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }
            if (key.StartsWith("<<#", StringComparison.Ordinal))
            {
                key = key.Substring(3);
            }
            if (key.EndsWith(">>", StringComparison.Ordinal))
            {
                key = key.Substring(0, key.Length - 2);
            }
            return key;
        }

        // ------------------------------------------------------------------
        // Folder loading
        // ------------------------------------------------------------------

        private void OnOpenFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder with .NET assemblies";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                LoadFolder(dialog.SelectedPath, AppSettings.Default.RecursiveFolders);
            }
        }

        /// <summary>
        /// Scans a folder on a worker thread and adds each assembly to the tree. The UI
        /// stays responsive because the tree is updated in batches on the UI thread.
        /// </summary>
        private void LoadFolder(string folder, bool recursive)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            int generation = ++folderLoadGeneration;
            List<string> files = new List<string>();
            statusLabel.Text = "Scanning " + folder + "...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    foreach (string file in Directory.GetFiles(folder, "*.*", option))
                    {
                        if (generation != folderLoadGeneration)
                        {
                            return;
                        }

                        string extension = Path.GetExtension(file).ToLowerInvariant();
                        if (extension == ".dll" || extension == ".exe")
                        {
                            files.Add(file);
                        }
                    }
                    files.Sort(StringComparer.OrdinalIgnoreCase);

                    if (files.Count == 0)
                    {
                        BeginInvoke(new Action(delegate { statusLabel.Text = "No assemblies found in " + folder; }));
                        return;
                    }

                    int index = 0;
                    foreach (string file in files)
                    {
                        if (generation != folderLoadGeneration)
                        {
                            return;
                        }

                        index++;
                        string current = file;
                        int currentIndex = index;
                        int total = files.Count;
                        try
                        {
                            BeginInvoke(new Action(delegate
                            {
                                statusLabel.Text = string.Format("Loading {0} ({1}/{2})...", Path.GetFileName(current), currentIndex, total);
                                TreeNode node = LoadAssemblyTree(current);
                                if (node != null)
                                {
                                    treeView.BeginUpdate();
                                    try
                                    {
                                        AddOrReplaceAssemblyNode(node);
                                    }
                                    finally
                                    {
                                        treeView.EndUpdate();
                                    }
                                }
                            }));
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }

                    BeginInvoke(new Action(delegate
                    {
                        statusLabel.Text = string.Format("Loaded {0} assemblies from {1}", files.Count, folder);
                    }));
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to scan the folder '" + folder + "'.", ex);
                    try
                    {
                        BeginInvoke(new Action(delegate { statusLabel.Text = "Folder scan failed: " + ex.Message; }));
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            });
        }

        private void AddOrReplaceAssemblyNode(TreeNode node)
        {
            for (int i = treeView.Nodes.Count - 1; i >= 0; i--)
            {
                if (string.Equals(treeView.Nodes[i].Text, node.Text, StringComparison.Ordinal))
                {
                    treeView.Nodes[i].Remove();
                }
            }
            treeView.Nodes.Add(node);
            InvalidateQuickOpenIndex();
        }
    }
}
