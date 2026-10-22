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
        private TreeNode CreateReferenceNode(AssemblyNameReference reference)
        {
            TreeNode node = new TreeNode(string.Format("{0} ({1})", reference.Name, reference.Version));
            node.Tag = reference;
            SetImage(node, "5VSProject_reference.bmp");

            string display = reference.FullName;
            node.ToolTipText = display;
            TreeNode placeholder = new TreeNode("...");
            placeholder.Tag = new LazyNode(delegate { return BuildReferenceChildren(reference); });
            node.Nodes.Add(placeholder);
            return node;
        }

        private List<TreeNode> BuildReferenceChildren(AssemblyNameReference reference)
        {
            List<TreeNode> children = new List<TreeNode>();
            string path = null;
            try
            {
                path = AssemblyResolver.ResolveFromTree(reference.FullName, treeView.Nodes);
            }
            catch (Exception ex)
            {
                Log.Info("Reference '" + reference.Name + "' is not available on disk: " + ex.Message);
            }

            if (string.IsNullOrEmpty(path))
            {
                TreeNode missing = new TreeNode("(assembly not found)");
                missing.ForeColor = System.Drawing.Color.Gray;
                children.Add(missing);
                return children;
            }

            AssemblyDefinition assembly = GetCachedAssembly(path);
            if (assembly == null)
            {
                TreeNode failed = new TreeNode("(unable to read " + Path.GetFileName(path) + ")");
                failed.ForeColor = System.Drawing.Color.Firebrick;
                children.Add(failed);
                return children;
            }

            TreeNode header = new TreeNode(Path.GetFileName(path));
            header.ForeColor = System.Drawing.Color.Gray;
            header.Tag = new AssemblyItem(path, assembly);
            SetImage(header, "1VSObject_Library.bmp");
            children.Add(header);

            foreach (ModuleDefinition module in assembly.Modules)
            {
                TreeNode moduleNode = new TreeNode(module.Name);
                SetImage(moduleNode, "1VSObject_Library.bmp");
                moduleNode.Tag = module;
                moduleNode.Nodes.Add(CreateLazyNode("...", "6VSObject_Namespace.bmp",
                    CreateModuleNamespacesFactory(module)));
                children.Add(moduleNode);
            }
            return children;
        }

        private Func<List<TreeNode>> CreateModuleNamespacesFactory(ModuleDefinition module)
        {
            return delegate
            {
                List<TreeNode> list = new List<TreeNode>();
                Dictionary<string, TreeNode> namespaces = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
                foreach (TypeDefinition type in module.Types)
                {
                    TreeNode namespaceNode;
                    if (!namespaces.TryGetValue(type.Namespace, out namespaceNode))
                    {
                        namespaceNode = CreateLazyNode(
                            string.IsNullOrEmpty(type.Namespace) ? "-" : type.Namespace,
                            "6VSObject_Namespace.bmp",
                            CreateNamespaceFactory(type.Namespace, module));
                        namespaceNode.Tag = new ModuleItem(type.Namespace, module);
                        namespaces.Add(type.Namespace, namespaceNode);
                        list.Add(namespaceNode);
                    }
                }
                return list;
            };
        }

        private Func<List<TreeNode>> CreateNamespaceFactory(string namespaceName, ModuleDefinition module)
        {
            return delegate
            {
                List<TreeNode> list = new List<TreeNode>();
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
