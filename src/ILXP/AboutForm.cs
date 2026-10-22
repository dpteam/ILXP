using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ILXP.Music;
using ILXP.UI;

namespace ILXP
{
    /// <summary>
    /// About box. It also hosts the little music player: the tune starts when the box
    /// opens, stops when it closes, and the logo is a water surface you can poke.
    /// </summary>
    public sealed class AboutForm : Form
    {
        private readonly WaterLogo waterLogo;
        private readonly Label productLabel;
        private readonly Label versionLabel;
        private readonly Label infoLabel;
        private readonly Label copyrightLabel;
        private readonly Label hintLabel;
        private readonly Button musicButton;
        private readonly ComboBox trackCombo;
        private readonly Button anotherMusicButton;
        private readonly Button closeButton;
        private readonly ContextMenuStrip musicMenu;
        private readonly ToolTip toolTip;

        private bool suppressTrackChange;
        private bool musicRequested;

        /// <summary>
        /// Adds a few ripples and advances the water simulation. Used by the
        /// <c>--screenshot</c> command to capture the effect without user input.
        /// </summary>
        internal void SimulateWaterForCapture()
        {
            waterLogo.AddDrop(waterLogo.Width / 3, waterLogo.Height / 2, 30);
            waterLogo.AddDrop(waterLogo.Width * 2 / 3, waterLogo.Height / 3, 22);
            waterLogo.AddDrop(waterLogo.Width / 2, waterLogo.Height * 2 / 3, 26);
            waterLogo.Advance(14);
        }

        public AboutForm()
        {
            Text = "About " + AppInfo.ProductName;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 384);
            BackColor = SystemColors.Window;
            KeyPreview = true;

            waterLogo = new WaterLogo
            {
                Location = new Point(0, 0),
                Size = new Size(540, 150),
                Image = AppResources.CompanyLogo,
                Cursor = Cursors.Hand
            };
            waterLogo.MouseEnter += delegate { toolTip.SetToolTip(waterLogo, "Click the water..."); };

            productLabel = new Label
            {
                Location = new Point(16, 160),
                AutoSize = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Text = AppInfo.ProductName
            };

            versionLabel = new Label
            {
                Location = new Point(20, 190),
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Version " + AppInfo.VersionText + "  \u00B7  " + AppInfo.Description
            };

            infoLabel = new Label
            {
                Location = new Point(16, 216),
                Size = new Size(508, 34),
                Text = "Warning: This computer program is protected by copyright law and international treaties."
            };

            copyrightLabel = new Label
            {
                Location = new Point(16, 254),
                Size = new Size(508, 18),
                ForeColor = SystemColors.GrayText,
                Text = AppInfo.Copyright
            };

            hintLabel = new Label
            {
                Location = new Point(16, 276),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = SystemColors.GrayText,
                Text = "Tip: the logo is water. Try clicking it."
            };

            musicButton = new Button
            {
                Location = new Point(16, 306),
                Size = new Size(96, 26),
                FlatStyle = FlatStyle.System,
                Text = "Music"
            };
            musicButton.Click += OnMusicToggle;

            trackCombo = new ComboBox
            {
                Location = new Point(118, 307),
                Size = new Size(248, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            trackCombo.SelectedIndexChanged += OnTrackChanged;

            anotherMusicButton = new Button
            {
                Location = new Point(372, 306),
                Size = new Size(152, 26),
                FlatStyle = FlatStyle.System,
                Text = "Another music..."
            };
            anotherMusicButton.Click += OnAnotherMusic;

            closeButton = new Button
            {
                Location = new Point(449, 344),
                Size = new Size(75, 26),
                FlatStyle = FlatStyle.System,
                Text = "Close",
                DialogResult = DialogResult.OK
            };

            musicMenu = new ContextMenuStrip();
            musicMenu.Opening += OnMusicMenuOpening;

            toolTip = new ToolTip();

            AcceptButton = closeButton;
            CancelButton = closeButton;
            Controls.Add(waterLogo);
            Controls.Add(productLabel);
            Controls.Add(versionLabel);
            Controls.Add(infoLabel);
            Controls.Add(copyrightLabel);
            Controls.Add(hintLabel);
            Controls.Add(musicButton);
            Controls.Add(trackCombo);
            Controls.Add(anotherMusicButton);
            Controls.Add(closeButton);

            Shown += OnShown;
            FormClosed += OnFormClosed;
            KeyDown += OnKeyDown;
        }

        private void OnShown(object sender, EventArgs e)
        {
            PopulateTracks();
            MusicPlayer.StateChanged += OnMusicStateChanged;
            musicRequested = AppSettings.Default.MusicEnabled;
            MusicPlayer.Volume = AppSettings.Default.MusicVolume;

            MusicTrack track = MusicLibrary.Find(AppSettings.Default.MusicTrack);
            if (track == null)
            {
                track = MusicLibrary.BuiltIn.Count > 0 ? MusicLibrary.BuiltIn[0] : null;
            }
            if (track != null)
            {
                SelectTrack(track);
            }

            UpdateMusicButton();
            waterLogo.Splash();
            waterLogo.Focus();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            MusicPlayer.StateChanged -= OnMusicStateChanged;
            MusicPlayer.Pause();
            AppSettings.Default.Save();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void PopulateTracks()
        {
            suppressTrackChange = true;
            try
            {
                trackCombo.Items.Clear();
                foreach (MusicTrack track in MusicLibrary.BuiltIn)
                {
                    trackCombo.Items.Add(track);
                }
                foreach (MusicTrack track in MusicLibrary.Imported)
                {
                    trackCombo.Items.Add(track);
                }
            }
            finally
            {
                suppressTrackChange = false;
            }
        }

        private void SelectTrack(MusicTrack track)
        {
            suppressTrackChange = true;
            try
            {
                trackCombo.SelectedItem = track;
                if (trackCombo.SelectedIndex < 0)
                {
                    for (int i = 0; i < trackCombo.Items.Count; i++)
                    {
                        MusicTrack candidate = trackCombo.Items[i] as MusicTrack;
                        if (candidate != null && string.Equals(candidate.Id, track.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            trackCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            finally
            {
                suppressTrackChange = false;
            }

            AppSettings.Default.MusicTrack = track.Id;
            if (musicRequested)
            {
                MusicPlayer.IsMuted = false;
                MusicPlayer.Play(track);
            }
        }

        private void OnTrackChanged(object sender, EventArgs e)
        {
            if (suppressTrackChange)
            {
                return;
            }

            MusicTrack track = trackCombo.SelectedItem as MusicTrack;
            if (track == null)
            {
                return;
            }

            musicRequested = true;
            MusicPlayer.IsMuted = false;
            AppSettings.Default.MusicEnabled = true;
            SelectTrack(track);
            UpdateMusicButton();
        }

        private void OnMusicToggle(object sender, EventArgs e)
        {
            musicRequested = !musicRequested;
            AppSettings.Default.MusicEnabled = musicRequested;

            if (!musicRequested)
            {
                MusicPlayer.IsMuted = true;
            }
            else
            {
                MusicPlayer.IsMuted = false;
                MusicTrack track = trackCombo.SelectedItem as MusicTrack;
                if (track != null && MusicPlayer.CurrentTrack == null)
                {
                    MusicPlayer.Play(track);
                }
            }
            UpdateMusicButton();
        }

        private void OnMusicStateChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new EventHandler(OnMusicStateChanged), sender, e);
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            UpdateMusicButton();
        }

        private void UpdateMusicButton()
        {
            bool playing = musicRequested && !MusicPlayer.IsMuted && MusicPlayer.State != MusicState.Failed;
            musicButton.Text = playing ? "\u266A  Music: on" : "\u266A  Music: off";
            musicButton.Size = new Size(96, 26);
            anotherMusicButton.Enabled = true;

            switch (MusicPlayer.State)
            {
                case MusicState.Preparing:
                    toolTip.SetToolTip(musicButton, "Rendering the tune...");
                    break;
                case MusicState.Failed:
                    toolTip.SetToolTip(musicButton, "Audio playback failed (no output device?)");
                    break;
                default:
                    toolTip.SetToolTip(musicButton, playing ? "Click to mute" : "Click to play");
                    break;
            }
        }

        private void OnAnotherMusic(object sender, EventArgs e)
        {
            musicMenu.Items.Clear();

            foreach (MusicTrack track in MusicLibrary.BuiltIn)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(track.DisplayName);
                item.Tag = track;
                item.Checked = trackCombo.SelectedItem == track;
                item.Click += OnMenuTrackClick;
                musicMenu.Items.Add(item);
            }

            if (MusicLibrary.Imported.Count > 0)
            {
                musicMenu.Items.Add(new ToolStripSeparator());
                foreach (MusicTrack track in MusicLibrary.Imported)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(track.DisplayName);
                    item.Tag = track;
                    item.Checked = trackCombo.SelectedItem == track;
                    item.Click += OnMenuTrackClick;
                    musicMenu.Items.Add(item);
                }
            }

            musicMenu.Items.Add(new ToolStripSeparator());
            musicMenu.Items.Add(new ToolStripMenuItem("Load MIDI file...", null, OnLoadMidi));
            musicMenu.Items.Add(new ToolStripMenuItem("Load music TXT...", null, OnLoadMusicText));
            musicMenu.Items.Add(new ToolStripMenuItem("Save current tune as MIDI...", null, OnSaveMidi));

            musicMenu.Show(anotherMusicButton, new Point(0, anotherMusicButton.Height));
        }

        private void OnMusicMenuOpening(object sender, CancelEventArgs e)
        {
            if (MusicLibrary.BuiltIn.Count == 0 && MusicLibrary.Imported.Count == 0)
            {
                e.Cancel = true;
            }
        }

        private void OnMenuTrackClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            MusicTrack track = item == null ? null : item.Tag as MusicTrack;
            if (track == null)
            {
                return;
            }

            musicRequested = true;
            MusicPlayer.IsMuted = false;
            AppSettings.Default.MusicEnabled = true;
            SelectTrack(track);
            UpdateMusicButton();
        }

        private void OnLoadMidi(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Load a MIDI file";
                dialog.Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    MusicTrack track = MusicLibrary.ImportMidi(dialog.FileName);
                    PopulateTracks();
                    musicRequested = true;
                    MusicPlayer.IsMuted = false;
                    AppSettings.Default.MusicEnabled = true;
                    SelectTrack(track);
                    UpdateMusicButton();
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to load the MIDI file.", ex);
                    MessageBox.Show(this, ex.Message, "MIDI import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void OnLoadMusicText(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Load a music text file";
                dialog.Filter = "Music text (*.txt)|*.txt|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    MusicTrack track = MusicLibrary.ImportText(dialog.FileName);
                    PopulateTracks();
                    musicRequested = true;
                    MusicPlayer.IsMuted = false;
                    AppSettings.Default.MusicEnabled = true;
                    SelectTrack(track);
                    UpdateMusicButton();
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to load the music text file.", ex);
                    MessageBox.Show(this, ex.Message, "Music import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void OnSaveMidi(object sender, EventArgs e)
        {
            MusicTrack track = trackCombo.SelectedItem as MusicTrack;
            if (track == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the tune as MIDI";
                dialog.Filter = "MIDI files (*.mid)|*.mid";
                dialog.FileName = SanitizeFileName(track.DisplayName) + ".mid";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    MusicLibrary.ExportMidi(track, dialog.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to export the tune.", ex);
                    MessageBox.Show(this, ex.Message, "MIDI export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "tune";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            System.Text.StringBuilder text = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                text.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return text.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip.Dispose();
                musicMenu.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
