using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ILXP.Music
{
    /// <summary>
    /// One playable tune. The audio pipeline is always text based: a tune is stored as
    /// the MIDI-as-text format, which is parsed and synthesised on demand.
    /// </summary>
    internal sealed class MusicTrack
    {
        public MusicTrack(string id, string displayName, string text, bool builtIn)
        {
            Id = id;
            DisplayName = displayName;
            Text = text;
            IsBuiltIn = builtIn;
        }

        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public string Text { get; private set; }

        public bool IsBuiltIn { get; private set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// Provides the built-in tunes (embedded text files) and imports user MIDI files
    /// through the text format.
    /// </summary>
    internal static class MusicLibrary
    {
        private const string ResourcePrefix = "ILXP.Music.Tracks.";

        private static readonly object SyncRoot = new object();
        private static List<MusicTrack> builtIn;
        private static readonly List<MusicTrack> imported = new List<MusicTrack>();

        /// <summary>Display names for the embedded tunes.</summary>
        private static readonly Dictionary<string, string> DisplayNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "E1M1", "At Doom's Gate" },
                { "TMNT3L2", "TMNT 3 - Level 2" },
                { "EUROPE", "Europe - The Final Countdown" },
                { "BlueMonday", "Blue Monday" },
                { "T2JD", "Terminator 2: Judgment Day" },
                { "Tuborg", "Tuborg" },
                { "AMB1", "Ambient 1" },
                { "AMB2", "Ambient 2" },
                { "SEA", "The Sea" },
                { "FFM", "FF-M" },
                { "HALO", "Halo Ambient" },
                { "sonic_volcano", "Sonic 3D Blast - Volcano Valley Act 2" }
            };

        public static IList<MusicTrack> BuiltIn
        {
            get
            {
                lock (SyncRoot)
                {
                    if (builtIn == null)
                    {
                        builtIn = LoadBuiltIn();
                    }
                    return builtIn;
                }
            }
        }

        public static IList<MusicTrack> Imported
        {
            get
            {
                lock (SyncRoot)
                {
                    return imported.ToArray();
                }
            }
        }

        public static MusicTrack Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (MusicTrack track in BuiltIn)
            {
                if (string.Equals(track.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return track;
                }
            }

            lock (SyncRoot)
            {
                foreach (MusicTrack track in imported)
                {
                    if (string.Equals(track.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        return track;
                    }
                }
            }
            return null;
        }

        /// <summary>Imports a MIDI file by converting it to the text format first.</summary>
        public static MusicTrack ImportMidi(string path)
        {
            MidiSequence sequence = MidiReader.Read(path);
            string text = MidiTextFormat.Format(sequence);

            string name = sequence.Title;
            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(path);
            }

            MusicTrack track = new MusicTrack("file:" + path, name + "  (MIDI)", text, false);
            RegisterImported(track);
            Log.Info("Imported MIDI '" + path + "' (" + sequence.Tracks.Count + " tracks, " + sequence.NoteCount + " notes).");
            return track;
        }

        /// <summary>Imports an existing text tune (the native ILXP music format).</summary>
        public static MusicTrack ImportText(string path)
        {
            string text = File.ReadAllText(path);
            MidiSequence sequence = MidiTextFormat.Parse(text);
            string name = sequence.Title;
            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(path);
            }

            MusicTrack track = new MusicTrack("file:" + path, name + "  (TXT)", text, false);
            RegisterImported(track);
            return track;
        }

        public static void ExportMidi(MusicTrack track, string path)
        {
            MidiSequence sequence = MidiTextFormat.Parse(track.Text);
            MidiWriter.Write(sequence, path);
        }

        public static void ExportText(MusicTrack track, string path)
        {
            File.WriteAllText(path, track.Text);
        }

        private static void RegisterImported(MusicTrack track)
        {
            lock (SyncRoot)
            {
                imported.RemoveAll(delegate(MusicTrack existing)
                {
                    return string.Equals(existing.Id, track.Id, StringComparison.OrdinalIgnoreCase);
                });
                imported.Add(track);
            }
        }

        private static List<MusicTrack> LoadBuiltIn()
        {
            List<MusicTrack> tracks = new List<MusicTrack>();
            Assembly assembly = typeof(MusicLibrary).Assembly;
            string[] names;
            try
            {
                names = assembly.GetManifestResourceNames();
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to enumerate music resources.", ex);
                return tracks;
            }

            Array.Sort(names, StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                if (!name.StartsWith(ResourcePrefix, StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string id = name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - 4);
                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                        {
                            continue;
                        }
                        using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, true))
                        {
                            string text = reader.ReadToEnd();
                            string display;
                            if (!DisplayNames.TryGetValue(id, out display))
                            {
                                display = id;
                            }
                            tracks.Add(new MusicTrack(id, display, text, true));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to load music resource '" + name + "'.", ex);
                }
            }

            // Keep the order used by the original player: a curated list first.
            string[] order =
            {
                "E1M1", "TMNT3L2", "EUROPE", "BlueMonday", "T2JD", "Tuborg",
                "AMB1", "AMB2", "SEA", "FFM", "HALO", "sonic_volcano"
            };
            tracks.Sort(delegate(MusicTrack a, MusicTrack b)
            {
                int indexA = Array.IndexOf(order, a.Id);
                int indexB = Array.IndexOf(order, b.Id);
                if (indexA < 0)
                {
                    indexA = int.MaxValue;
                }
                if (indexB < 0)
                {
                    indexB = int.MaxValue;
                }
                return indexA.CompareTo(indexB);
            });

            Log.Info("Loaded " + tracks.Count + " built-in tune(s).");
            return tracks;
        }
    }
}
