using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ILXP.Music;

namespace ILXP.UI
{
    /// <summary>
    /// Settings dialog. The layout follows the settings pages of ILSpy and dnSpy: a list of
    /// categories on the left, the options of the selected category on the right.
    ///
    /// Options are declared once (see <see cref="BuildOptions"/>) and rendered on demand, so
    /// adding a setting is a single entry. Values are applied to <see cref="AppSettings"/>
    /// when the dialog is confirmed.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        /// <summary>One option: it renders itself and reports how to apply its value.</summary>
        private sealed class Option
        {
            public string Category;
            public Func<Control> Create;
        }

        private readonly ListBox categories;
        private readonly Panel page;
        private readonly List<Option> options = new List<Option>();
        private readonly List<Action> pendingApplies = new List<Action>();

        public SettingsForm()
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 430);

            categories = new ListBox
            {
                Location = new Point(12, 12),
                Size = new Size(170, 366),
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            categories.SelectedIndexChanged += delegate { RenderCategory(); };

            page = new Panel
            {
                Location = new Point(194, 12),
                Size = new Size(414, 366),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button ok = new Button
            {
                Location = new Point(452, 390),
                Size = new Size(75, 28),
                Text = "OK",
                DialogResult = DialogResult.OK
            };
            Button cancel = new Button
            {
                Location = new Point(533, 390),
                Size = new Size(75, 28),
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };
            Button reset = new Button
            {
                Location = new Point(12, 390),
                Size = new Size(130, 28),
                Text = "Reset to defaults"
            };

            ok.Click += OnOk;
            reset.Click += OnReset;
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(categories);
            Controls.Add(page);
            Controls.Add(ok);
            Controls.Add(cancel);
            Controls.Add(reset);

            BuildOptions();
            foreach (string category in Categories)
            {
                categories.Items.Add(category);
            }
            if (categories.Items.Count > 0)
            {
                categories.SelectedIndex = 0;
            }
        }

        private static readonly string[] Categories =
        {
            "General", "Tree", "Output", "Music", "Advanced"
        };

        // ------------------------------------------------------------------
        // Option declarations
        // ------------------------------------------------------------------

        private void BuildOptions()
        {
            // ---------------- General ----------------
            AddCheck("General", "Recursive folders",
                "Load every subfolder when a folder is opened.",
                delegate { return AppSettings.Default.RecursiveFolders; },
                delegate(bool value) { AppSettings.Default.RecursiveFolders = value; });

            AddCheck("General", "Restore the last session",
                "Reopen the last assembly and select the last node on startup.",
                delegate { return true; },
                delegate(bool value) { /* always on: the session restore is part of the UX */ });

            // ---------------- Tree ----------------
            AddCheck("Tree", "Lazy references",
                "Resolve referenced assemblies only when a reference node is expanded. " +
                "Turning this off makes deep expansion much slower.",
                delegate { return AppSettings.Default.LazyReferences; },
                delegate(bool value) { AppSettings.Default.LazyReferences = value; });

            AddNumber("Tree", "Default expand depth (0-9)",
                "Used by View → Expand to depth. 0 collapses everything, 9 expands all levels.",
                0, 9,
                delegate { return Math.Max(0, Math.Min(9, AppSettings.Default.TreeExpandDepth)); },
                delegate(int value) { AppSettings.Default.TreeExpandDepth = value; });

            AddCheck("Tree", "Show the loading overlay",
                "Display the animated overlay while an assembly is being read.",
                delegate { return AppSettings.Default.ShowLoadingOverlay; },
                delegate(bool value) { AppSettings.Default.ShowLoadingOverlay = value; });

            // ---------------- Output ----------------
            AddChoice("Output", "Default language",
                "Writer used when an assembly is opened.",
                new[] { "IL", "C#", "Visual Basic" },
                delegate
                {
                    int index = AppSettings.Default.SelectedLanguage;
                    return index < 0 || index > 2 ? 0 : index;
                },
                delegate(int index) { AppSettings.Default.SelectedLanguage = index; });

            AddChoice("Output", "Output dialect",
                "Language / toolchain profile for generated source.",
                DialectNames(),
                delegate
                {
                    OutputDialect dialect = OutputDialect.FromId(AppSettings.Default.DecompilerTarget);
                    return dialect == null ? -1 : IndexOfDialect(dialect);
                },
                delegate(int index)
                {
                    OutputDialect dialect = index >= 0 && index < OutputDialect.All.Count
                        ? OutputDialect.All[index]
                        : null;
                    AppSettings.Default.DecompilerTarget = dialect == null ? "auto" : dialect.Id;
                });

            // ---------------- Music ----------------
            AddCheck("Music", "Play music in the About box",
                "Start the selected tune when the About box opens.",
                delegate { return AppSettings.Default.MusicEnabled; },
                delegate(bool value) { AppSettings.Default.MusicEnabled = value; });

            AddTrack("Music", "Default tune",
                "Tune played when the About box opens.");

            AddNumber("Music", "Volume (%)",
                "Playback volume, 0-100.",
                0, 100,
                delegate { return (int)Math.Round(AppSettings.Default.MusicVolume * 100); },
                delegate(int value) { AppSettings.Default.MusicVolume = value / 100f; });

            // ---------------- Advanced ----------------
            AddInfo("Advanced", "Settings file", AppSettings.Default.SettingsPath);
            AddInfo("Advanced", "Log file", "%APPDATA%\\ILXP\\logs\\ILXP.log");
            AddInfo("Advanced", "Import from ILSpy / dnSpy",
                "Planned: ILSpy keeps its settings in %APPDATA%\\ICSharpCode\\ILSpy.xml and " +
                "dnSpy in %APPDATA%\\dnSpy\\*.xml; the importer will map matching options.");
        }

        private static string[] DialectNames()
        {
            List<string> names = new List<string>();
            foreach (OutputDialect dialect in OutputDialect.All)
            {
                names.Add(dialect.DisplayName);
            }
            return names.ToArray();
        }

        private static int IndexOfDialect(OutputDialect dialect)
        {
            for (int i = 0; i < OutputDialect.All.Count; i++)
            {
                if (OutputDialect.All[i].Id == dialect.Id)
                {
                    return i;
                }
            }
            return -1;
        }

        // ------------------------------------------------------------------
        // Declarative helpers
        // ------------------------------------------------------------------

        private void AddCheck(string category, string title, string description,
            Func<bool> get, Action<bool> set)
        {
            options.Add(new Option
            {
                Category = category,
                Create = delegate
                {
                    CheckBox box = new CheckBox
                    {
                        Text = title,
                        Checked = get(),
                        Location = new Point(12, 0),
                        Size = new Size(380, 22)
                    };
                    pendingApplies.Add(delegate { set(box.Checked); });
                    return Wrap(box, description);
                }
            });
        }

        private void AddNumber(string category, string title, string description,
            int minimum, int maximum, Func<int> get, Action<int> set)
        {
            options.Add(new Option
            {
                Category = category,
                Create = delegate
                {
                    Label label = new Label
                    {
                        Text = title,
                        Location = new Point(12, 3),
                        Size = new Size(240, 20)
                    };
                    NumericUpDown numeric = new NumericUpDown
                    {
                        Location = new Point(260, 0),
                        Size = new Size(70, 24),
                        Minimum = minimum,
                        Maximum = maximum,
                        Value = Math.Max(minimum, Math.Min(maximum, get()))
                    };
                    pendingApplies.Add(delegate { set((int)numeric.Value); });

                    Panel container = Wrap(label, description);
                    numeric.Location = new Point(260, 0);
                    container.Controls.Add(numeric);
                    return container;
                }
            });
        }

        private void AddChoice(string category, string title, string description,
            string[] items, Func<int> get, Action<int> set)
        {
            options.Add(new Option
            {
                Category = category,
                Create = delegate
                {
                    Label label = new Label
                    {
                        Text = title,
                        Location = new Point(12, 3),
                        Size = new Size(240, 20)
                    };
                    ComboBox combo = new ComboBox
                    {
                        Location = new Point(260, 0),
                        Size = new Size(140, 24),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    combo.Items.AddRange(items);
                    int index = get();
                    combo.SelectedIndex = index >= 0 && index < combo.Items.Count ? index : 0;
                    pendingApplies.Add(delegate { set(combo.SelectedIndex); });

                    Panel container = Wrap(label, description);
                    container.Controls.Add(combo);
                    return container;
                }
            });
        }

        private void AddTrack(string category, string title, string description)
        {
            options.Add(new Option
            {
                Category = category,
                Create = delegate
                {
                    Label label = new Label
                    {
                        Text = title,
                        Location = new Point(12, 3),
                        Size = new Size(240, 20)
                    };
                    ComboBox combo = new ComboBox
                    {
                        Location = new Point(260, 0),
                        Size = new Size(140, 24),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    foreach (MusicTrack track in MusicLibrary.BuiltIn)
                    {
                        combo.Items.Add(track);
                    }
                    MusicTrack selected = MusicLibrary.Find(AppSettings.Default.MusicTrack);
                    if (selected != null)
                    {
                        combo.SelectedItem = selected;
                    }
                    else if (combo.Items.Count > 0)
                    {
                        combo.SelectedIndex = 0;
                    }

                    pendingApplies.Add(delegate
                    {
                        MusicTrack track = combo.SelectedItem as MusicTrack;
                        if (track != null)
                        {
                            AppSettings.Default.MusicTrack = track.Id;
                        }
                    });

                    Panel container = Wrap(label, description);
                    container.Controls.Add(combo);
                    return container;
                }
            });
        }

        private void AddInfo(string category, string title, string description)
        {
            options.Add(new Option
            {
                Category = category,
                Create = delegate
                {
                    Label label = new Label
                    {
                        Text = title,
                        Location = new Point(12, 0),
                        Size = new Size(380, 20),
                        Font = new Font(Font, FontStyle.Bold)
                    };
                    return Wrap(label, description);
                }
            });
        }

        /// <summary>Builds a fixed-height container with a title control and a description.</summary>
        private Panel Wrap(Control titleControl, string description)
        {
            Panel container = new Panel { Size = new Size(384, 58) };
            titleControl.Location = new Point(0, 0);
            container.Controls.Add(titleControl);

            Label descriptionLabel = new Label
            {
                Text = description,
                Location = new Point(12, 24),
                Size = new Size(360, 32),
                ForeColor = SystemColors.GrayText
            };
            container.Controls.Add(descriptionLabel);
            return container;
        }

        // ------------------------------------------------------------------
        // Rendering and applying
        // ------------------------------------------------------------------

        private void RenderCategory()
        {
            string category = categories.SelectedItem as string;
            page.SuspendLayout();
            try
            {
                page.Controls.Clear();
                pendingApplies.Clear();

                int y = 10;
                foreach (Option option in options)
                {
                    if (option.Category != category)
                    {
                        continue;
                    }

                    Control control = option.Create();
                    control.Location = new Point(10, y);
                    page.Controls.Add(control);
                    y += control.Height + 6;
                }
            }
            finally
            {
                page.ResumeLayout();
            }
        }

        private void OnOk(object sender, EventArgs e)
        {
            foreach (Action apply in pendingApplies)
            {
                try
                {
                    apply();
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to apply a setting.", ex);
                }
            }
            AppSettings.Default.Save();
        }

        private void OnReset(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Reset every setting to its default value?", "Settings",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            AppSettings.Default.ResetToDefaults();
            AppSettings.Default.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
