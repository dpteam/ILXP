using System;
using System.IO;
using System.Media;
using System.Threading;

namespace ILXP.Music
{
    internal enum MusicState
    {
        Stopped,
        Preparing,
        Playing,
        Failed
    }

    /// <summary>
    /// Plays <see cref="MusicTrack"/> instances. Rendering happens on a worker thread so
    /// the UI never blocks; playback uses <see cref="SoundPlayer"/> on a rendered WAV
    /// image, which works without any additional runtime dependency.
    /// </summary>
    internal static class MusicPlayer
    {
        private static readonly object SyncRoot = new object();
        private static SoundPlayer player;
        private static MemoryStream currentStream;
        private static MusicTrack currentTrack;
        private static MusicState state = MusicState.Stopped;
        private static int generation;
        private static bool muted;
        private static float volume = 0.55f;

        public static event EventHandler StateChanged;

        public static MusicState State
        {
            get { lock (SyncRoot) { return state; } }
        }

        public static MusicTrack CurrentTrack
        {
            get { lock (SyncRoot) { return currentTrack; } }
        }

        public static bool IsMuted
        {
            get { lock (SyncRoot) { return muted; } }
            set
            {
                bool changed;
                lock (SyncRoot)
                {
                    changed = muted != value;
                    muted = value;
                }

                if (!changed)
                {
                    return;
                }

                try
                {
                    if (muted)
                    {
                        SoundPlayer active = player;
                        if (active != null)
                        {
                            active.Stop();
                        }
                        SetState(MusicState.Stopped);
                    }
                    else if (currentStream != null)
                    {
                        StartPlayback();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to change the music mute state.", ex);
                }
            }
        }

        public static float Volume
        {
            get { lock (SyncRoot) { return volume; } }
            set
            {
                float clamped = value < 0f ? 0f : (value > 1f ? 1f : value);
                bool needsRestart;
                lock (SyncRoot)
                {
                    needsRestart = Math.Abs(volume - clamped) > 0.001f;
                    volume = clamped;
                }
                if (needsRestart && CurrentTrack != null && !IsMuted)
                {
                    Play(CurrentTrack);
                }
            }
        }

        /// <summary>Starts (or restarts) a tune. Rendering is asynchronous.</summary>
        public static void Play(MusicTrack track)
        {
            if (track == null)
            {
                return;
            }

            int token;
            lock (SyncRoot)
            {
                token = ++generation;
                currentTrack = track;
                state = MusicState.Preparing;
                DisposePlayer();
            }
            RaiseStateChanged();

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    MidiSequence sequence = MidiTextFormat.Parse(track.Text);
                    byte[] wav = ChiptuneSynth.RenderWav(sequence, volume);

                    lock (SyncRoot)
                    {
                        if (token != generation)
                        {
                            return;
                        }

                        currentStream = new MemoryStream(wav, false);
                        player = new SoundPlayer(currentStream);
                        if (muted)
                        {
                            state = MusicState.Stopped;
                        }
                        else
                        {
                            try
                            {
                                player.PlayLooping();
                                state = MusicState.Playing;
                            }
                            catch (Exception ex)
                            {
                                Log.Warn("Unable to start audio playback.", ex);
                                state = MusicState.Failed;
                            }
                        }
                    }
                    RaiseStateChanged();
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to render tune '" + track.DisplayName + "'.", ex);
                    lock (SyncRoot)
                    {
                        if (token == generation)
                        {
                            state = MusicState.Failed;
                        }
                    }
                    RaiseStateChanged();
                }
            });
        }

        public static void Stop()
        {
            lock (SyncRoot)
            {
                generation++;
                DisposePlayer();
                currentTrack = null;
                currentStream = null;
                state = MusicState.Stopped;
            }
            RaiseStateChanged();
        }

        /// <summary>Stops playback but keeps the current tune (used when a window closes).</summary>
        public static void Pause()
        {
            lock (SyncRoot)
            {
                generation++;
                if (player != null)
                {
                    try
                    {
                        player.Stop();
                    }
                    catch (Exception)
                    {
                    }
                }
                state = MusicState.Stopped;
            }
            RaiseStateChanged();
        }

        private static void StartPlayback()
        {
            lock (SyncRoot)
            {
                if (player == null || currentStream == null || muted)
                {
                    return;
                }
                try
                {
                    currentStream.Position = 0;
                    player.PlayLooping();
                    state = MusicState.Playing;
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to resume audio playback.", ex);
                    state = MusicState.Failed;
                }
            }
            RaiseStateChanged();
        }

        private static void DisposePlayer()
        {
            if (player != null)
            {
                try
                {
                    player.Stop();
                    player.Dispose();
                }
                catch (Exception)
                {
                }
                player = null;
            }

            if (currentStream != null)
            {
                try
                {
                    currentStream.Dispose();
                }
                catch (Exception)
                {
                }
                currentStream = null;
            }
        }

        private static void SetState(MusicState value)
        {
            lock (SyncRoot)
            {
                state = value;
            }
            RaiseStateChanged();
        }

        private static void RaiseStateChanged()
        {
            EventHandler handler = StateChanged;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Warn("Music state handler failed.", ex);
            }
        }
    }
}
