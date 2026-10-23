using System;
using System.Threading;
using System.Windows.Forms;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Code preview with cancellation. Selecting a type, namespace or assembly decompiles it in
    /// the background and shows the result as soon as it is ready (like ILSpy/dnSpy), so the
    /// tree never freezes on a large type. While the work runs, a bar offers Cancel: pressing it
    /// throws the pending result away and puts the previous preview back.
    /// </summary>
    public sealed partial class MainForm
    {
        private int previewGeneration;
        private bool previewRunning;
        private string previewSnapshot = string.Empty;

        /// <summary>
        /// Starts a background decompilation for <paramref name="node"/>. Returns false when the
        /// node is cheap enough to render synchronously (methods, fields, resources, ...).
        /// </summary>
        private bool TryDeferPreview(TreeNode node, object value)
        {
            Func<LanguageWriter, string> render = null;
            string label = node.Text;

            TypeDefinition type = value as TypeDefinition;
            if (type != null)
            {
                render = delegate(LanguageWriter preview)
                {
                    preview.EmitBodies = true;
                    return preview.WriteType(type);
                };
            }

            if (render == null)
            {
                ModuleItem moduleItem = value as ModuleItem;
                if (moduleItem != null)
                {
                    render = delegate(LanguageWriter preview)
                    {
                        preview.EmitBodies = true;
                        return preview.WriteNamespace(moduleItem);
                    };
                }
            }

            if (render == null)
            {
                AssemblyItem assemblyItem = value as AssemblyItem;
                if (assemblyItem != null)
                {
                    render = delegate(LanguageWriter preview)
                    {
                        preview.Runtime = RuntimeProfile.Detect(assemblyItem.Assembly);
                        preview.EmitBodies = true;
                        return preview.WriteAssembly(assemblyItem);
                    };
                }
            }

            if (render == null)
            {
                ReferenceItem referenceItem = value as ReferenceItem;
                if (referenceItem != null && referenceItem.Assembly != null)
                {
                    AssemblyItem referenced = new AssemblyItem(referenceItem.Path, referenceItem.Assembly);
                    render = delegate(LanguageWriter preview)
                    {
                        preview.Runtime = RuntimeProfile.Detect(referenced.Assembly);
                        preview.EmitBodies = true;
                        return preview.WriteAssembly(referenced);
                    };
                }
            }

            if (render == null)
            {
                return false;
            }

            StartPreview(label, render);
            return true;
        }

        /// <summary>Runs the renderer on a worker thread and applies the result on the UI thread.</summary>
        private void StartPreview(string label, Func<LanguageWriter, string> render)
        {
            int generation = ++previewGeneration;

            if (!previewRunning)
            {
                // Remember what is on screen right now: Cancel puts exactly this back.
                previewSnapshot = codeEditor.Text;
            }
            previewRunning = true;

            ShowPreviewBar("Decompiling " + label + " ...");
            statusLabel.Text = "Decompiling " + label + "...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                string text = null;
                string error = null;
                try
                {
                    LanguageWriter preview = CreatePreviewWriter();
                    text = preview.StripMarkup(render(preview));
                }
                catch (Exception ex)
                {
                    Log.Warn("Preview of '" + label + "' failed.", ex);
                    error = ex.Message;
                }

                if (generation != previewGeneration)
                {
                    return; // superseded or cancelled while rendering
                }

                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        if (generation != previewGeneration || IsDisposed)
                        {
                            return;
                        }

                        previewRunning = false;
                        HidePreviewBar();

                        if (error != null)
                        {
                            codeEditor.Visible = true;
                            textViewer.Visible = false;
                            imageBox.Visible = false;
                            codeEditor.SetPlainText("// preview failed: " + error);
                            statusLabel.Text = "Preview failed";
                            return;
                        }

                        codeEditor.Visible = true;
                        textViewer.Visible = false;
                        imageBox.Visible = false;
                        codeEditor.Text = text;
                        previewSnapshot = text;
                        RebuildIlMap();
                        statusLabel.Text = "Decompiled " + label;
                    }));
                }
                catch (Exception ex)
                {
                    // The form can be closed while the worker is still running.
                    Log.Info("Preview result dropped: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// Cancels the running preview and restores the document that was visible before it
        /// started, which is what the Cancel button in the preview bar does.
        /// </summary>
        private void CancelPreview()
        {
            if (!previewRunning && !previewBar.Visible)
            {
                return;
            }

            previewGeneration++;
            previewRunning = false;
            HidePreviewBar();

            codeEditor.Visible = true;
            textViewer.Visible = false;
            imageBox.Visible = false;
            codeEditor.Text = previewSnapshot ?? string.Empty;
            RebuildIlMap();
            statusLabel.Text = "Preview cancelled";
        }

        private void OnPreviewCancel(object sender, EventArgs e)
        {
            CancelPreview();
        }

        /// <summary>
        /// A writer of the same language for the worker thread: writers are cheap and must not be
        /// shared with the UI thread while a body is being rendered.
        /// </summary>
        private LanguageWriter CreatePreviewWriter()
        {
            LanguageWriter source = writer;
            LanguageWriter copy;

            if (source is IlWriter)
            {
                copy = new IlWriter();
            }
            else if (source is VbWriter)
            {
                copy = new VbWriter();
            }
            else
            {
                copy = new CSharpWriter();
            }

            copy.Dialect = source == null ? null : source.Dialect;
            copy.Runtime = source == null ? null : source.Runtime;
            return copy;
        }

        private void ShowPreviewBar(string text)
        {
            previewLabel.Text = text;
            previewBar.Visible = true;
            previewBar.BringToFront();
        }

        private void HidePreviewBar()
        {
            previewBar.Visible = false;
        }

        /// <summary>
        /// Waits for a running preview to finish (used by screenshots and tests, which have no
        /// message loop of their own). Returns false when the wait timed out.
        /// </summary>
        internal bool WaitForPreview(int timeoutMilliseconds)
        {
            int waited = 0;
            while (previewRunning && waited < timeoutMilliseconds)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
                waited += 20;
            }
            return !previewRunning;
        }

        /// <summary>
        /// Selects a type by name and returns the document text before the selection. The
        /// preview is left running: callers decide whether to wait for it or cancel it.
        /// </summary>
        internal string SelectForCheck(string typeName)
        {
            string before = codeEditor.Text;
            TreeNode root = treeView.Nodes.Count == 0 ? null : treeView.Nodes[0];
            TreeNode node = root == null ? null : FindTypeNode(root, typeName);
            if (node != null)
            {
                treeView.SelectedNode = node;
            }
            return before;
        }

        /// <summary>The document currently shown in the code pane.</summary>
        internal string CurrentPreviewText()
        {
            return codeEditor.Text;
        }

        /// <summary>Invokes the Cancel button path without a click (used by the self check).</summary>
        internal void CancelPreviewForCheck()
        {
            CancelPreview();
        }

        /// <summary>Invokes the Del/C​lose path without a key press (used by the self check).</summary>
        internal string CheckCloseForCheck()
        {
            if (treeView.Nodes.Count == 0)
            {
                return "no assembly node to close";
            }

            CloseCurrentAssembly();
            if (treeView.Nodes.Count != 0)
            {
                return "closing left " + treeView.Nodes.Count + " node(s)";
            }
            return null;
        }

        /// <summary>
        /// Verifies the shape of the reference tree: a reference node expands straight into
        /// namespaces, with no intermediate file or module node. Returns an error text or null.
        /// </summary>
        internal string CheckReferenceNodeShape()
        {
            if (treeView.Nodes.Count == 0)
            {
                return "no assembly loaded";
            }

            TreeNode references = null;
            foreach (TreeNode child in treeView.Nodes[0].Nodes)
            {
                if (child.Tag == null && string.Equals(child.Text, "References", StringComparison.Ordinal))
                {
                    references = child;
                    break;
                }
            }
            if (references == null)
            {
                return "no References node";
            }

            ExpandLazyNode(references);
            if (references.Nodes.Count == 0)
            {
                return "References node is empty";
            }

            TreeNode reference = references.Nodes[0];
            if (!(reference.Tag is ReferenceItem))
            {
                return "reference node does not carry a ReferenceItem";
            }

            ExpandLazyNode(reference);
            if (reference.Nodes.Count == 0)
            {
                return "reference node expanded to nothing";
            }

            foreach (TreeNode child in reference.Nodes)
            {
                if (child.Tag is AssemblyItem)
                {
                    return "reference exposes a separate file node (" + child.Text + ")";
                }
                if (child.Tag is ModuleDefinition)
                {
                    return "reference exposes a separate module node (" + child.Text + ")";
                }
                if (!(child.Tag is ModuleItem))
                {
                    return "reference child is not a namespace (" + child.Text + ")";
                }
            }
            return null;
        }
    }
}
