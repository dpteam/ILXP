using System;
using System.IO;
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
    /// Plays <see cref="MusicTrack"/> instances. Rendering happens on a worker thread (the
    /// synthesizer streams into a temporary WAV file) and playback uses
    /// <see cref="System.Media.SoundPlayer"/>, which needs no additional runtime
    /// dependency and works on Windows XP.
    /// </summary>
    internal static class MusicPlayer
    {
        private static readonly object SyncRoot = new object();
        private static System.Media.SoundPlayer player;
        private static string currentFile;
        private static MusicTrack currentTrack;
        private static MusicState state = MusicState.Stopped;
        private static int generation;
        private static bool muted;
        private static float volume = 0.55f;

        private static bool ownsFile;

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
                        StopPlayback();
                        SetState(MusicState.Stopped);
                    }
                    else if (currentFile != null)
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
            float currentVolume;
            lock (SyncRoot)
            {
                token = ++generation;
                currentTrack = track;
                currentVolume = volume;
                state = MusicState.Preparing;
                StopPlayback();
            }
            RaiseStateChanged();

            ThreadPool.QueueUserWorkItem(delegate
            {
                string file = null;
                try
                {
                    file = Synthesizer.RenderToCache(track.Id, track.Text, currentVolume);

                    lock (SyncRoot)
                    {
                        if (token != generation)
                        {
                            return;
                        }

                        currentFile = file;
                        ownsFile = false;
                        player = new System.Media.SoundPlayer(file);
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
                    if (file != null)
                    {
                        TryDelete(file);
                    }
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
                StopPlayback();
                currentTrack = null;
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
                StopPlayback();
                state = MusicState.Stopped;
            }
            RaiseStateChanged();
        }

        private static void StartPlayback()
        {
            lock (SyncRoot)
            {
                if (currentFile == null || muted)
                {
                    return;
                }
                try
                {
                    if (player == null)
                    {
                        player = new System.Media.SoundPlayer(currentFile);
                    }
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

        private static void StopPlayback()
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

            if (currentFile != null)
            {
                if (ownsFile)
                {
                    TryDelete(currentFile);
                }
                currentFile = null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
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
