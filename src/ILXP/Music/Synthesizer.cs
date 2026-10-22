using System;
using System.Collections.Generic;
using System.IO;

namespace ILXP.Music
{
    /// <summary>
    /// Software synthesizer for the text MIDI format. Implements the General MIDI program
    /// set (128 instruments) and the percussion map with pulse/triangle/saw/sine/noise
    /// oscillators, ADSR envelopes, a resonant low-pass filter, vibrato, detune, pulse
    /// width modulation and per-channel volume/pan/expression.
    ///
    /// Rendering is streamed to a WAV file in blocks so that long tunes do not have to be
    /// held in memory.
    /// </summary>
    internal static class Synthesizer
    {
        public const int SampleRate = 32000;
        public const int Channels = 2;
        public const int MaxSeconds = 600;

        private const int BlockFrames = 2048;
        private const double TwoPi = Math.PI * 2.0;
        private const int TableSize = 1024;
        private const int DutyBuckets = 32;

        /// <summary>
        /// Precomputed waveform tables: one per shape and duty bucket. A table lookup is
        /// several times faster than evaluating the waveform for every sample, which keeps
        /// long tunes renderable in a few seconds.
        /// </summary>
        private static readonly float[][] WaveTables = BuildWaveTables();

        private static float[][] BuildWaveTables()
        {
            int shapes = 4; // Pulse, Triangle, Saw, Sine (noise is generated separately)
            float[][] tables = new float[shapes * DutyBuckets][];
            for (int shape = 0; shape < shapes; shape++)
            {
                for (int bucket = 0; bucket < DutyBuckets; bucket++)
                {
                    float duty = DutyForBucket(bucket);
                    float[] table = new float[TableSize];
                    for (int i = 0; i < TableSize; i++)
                    {
                        double phase = (double)i / TableSize;
                        switch ((WaveShape)shape)
                        {
                            case WaveShape.Triangle:
                                table[i] = (float)(4.0 * Math.Abs(phase - 0.5) - 1.0);
                                break;
                            case WaveShape.Saw:
                                table[i] = (float)(phase * 2.0 - 1.0);
                                break;
                            case WaveShape.Sine:
                                table[i] = (float)Math.Sin(phase * TwoPi);
                                break;
                            default:
                                table[i] = phase < duty ? 1f : -1f;
                                break;
                        }
                    }
                    tables[shape * DutyBuckets + bucket] = table;
                }
            }
            return tables;
        }

        private static float DutyForBucket(int bucket)
        {
            return 0.05f + (0.9f * bucket) / (DutyBuckets - 1);
        }

        private static int BucketForDuty(float duty)
        {
            int bucket = (int)((duty - 0.05f) / 0.9f * (DutyBuckets - 1) + 0.5f);
            if (bucket < 0)
            {
                return 0;
            }
            return bucket >= DutyBuckets ? DutyBuckets - 1 : bucket;
        }

        private static float TableSample(float[] table, double phase)
        {
            int index = (int)(phase * TableSize);
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= TableSize)
            {
                index = TableSize - 1;
            }
            return table[index];
        }

        /// <summary>A note that has been scheduled for playback.</summary>
        private sealed class ScheduledNote
        {
            public double Start;
            public double End;
            public int Channel;
            public int NoteNumber;
            public int Velocity;
            public int Program;
            public float Pan;
            public float Volume;
            public double BendCents;
            public bool IsDrum;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public static void RenderToWav(MidiSequence sequence, float volume, string path)
        {
            List<ScheduledNote> notes = Schedule(sequence, out double totalSeconds);
            totalSeconds = Math.Min(totalSeconds + 2.0, MaxSeconds);

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                WriteWavHeader(stream, 0);
                int totalFrames = (int)(totalSeconds * SampleRate);
                Render(notes, stream, totalFrames, volume);
                stream.Seek(0, SeekOrigin.Begin);
                WriteWavHeader(stream, totalFrames * Channels * 2);
            }
        }

        /// <summary>
        /// Renders a tune to a cached file. The cache key covers the track text, the
        /// volume and the synthesizer revision, so repeated playback is instant.
        /// </summary>
        public static string RenderToCache(string trackId, string trackText, float volume)
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ILXP", "cache");
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to create the audio cache directory.", ex);
                directory = Path.GetTempPath();
            }

            string key = CacheKey(trackId, trackText, volume);
            string path = Path.Combine(directory, key + ".wav");
            if (File.Exists(path) && new FileInfo(path).Length > 1024)
            {
                return path;
            }

            string temporary = path + ".tmp";
            RenderToWav(MidiTextFormat.Parse(trackText), volume, temporary);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(temporary, path);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to move the rendered tune into the cache.", ex);
                return temporary;
            }
            return path;
        }

        private static string CacheKey(string trackId, string trackText, float volume)
        {
            // FNV-1a over the text plus the volume and a version salt.
            ulong hash = 14695981039346656037UL;
            string salt = "synth-v2|" + (trackId ?? string.Empty) + "|" + volume.ToString("0.00") + "|";
            for (int i = 0; i < salt.Length; i++)
            {
                hash = (hash ^ salt[i]) * 1099511628211UL;
            }
            if (trackText != null)
            {
                for (int i = 0; i < trackText.Length; i++)
                {
                    hash = (hash ^ trackText[i]) * 1099511628211UL;
                }
            }
            return "tune-" + hash.ToString("x16");
        }

        /// <summary>Renders to an in-memory WAV image (used for short previews and tests).</summary>
        public static byte[] RenderWav(MidiSequence sequence, float volume)
        {
            string temporary = Path.Combine(Path.GetTempPath(), "ILXP-" + Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                RenderToWav(sequence, volume, temporary);
                return File.ReadAllBytes(temporary);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                    {
                        File.Delete(temporary);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        // ------------------------------------------------------------------
        // Event scheduling
        // ------------------------------------------------------------------

        private sealed class ChannelState
        {
            public int Program;
            public float Volume = 1f;
            public float Expression = 1f;
            public float Pan = 0.5f;
            public double BendCents;
            public bool Sustain;
        }

        private static List<ScheduledNote> Schedule(MidiSequence sequence, out double totalSeconds)
        {
            double[] secondsPerTick = BuildTempoMap(sequence, out long lastTick);
            List<ScheduledNote> notes = new List<ScheduledNote>();

            foreach (MidiTrack track in sequence.Tracks)
            {
                ChannelState[] channels = new ChannelState[16];
                for (int i = 0; i < channels.Length; i++)
                {
                    channels[i] = new ChannelState();
                }

                // Active notes per channel/note pair; a retrigger closes the previous one.
                Dictionary<int, ScheduledNote> active = new Dictionary<int, ScheduledNote>(256);
                Dictionary<int, double> sustained = new Dictionary<int, double>(64);

                long tick = 0;
                foreach (MidiEvent e in track.Events)
                {
                    tick += e.Delta;
                    double time = TickToSeconds(secondsPerTick, tick);
                    ChannelState state = e.Channel >= 0 && e.Channel < 16 ? channels[e.Channel] : channels[0];

                    switch (e.Kind)
                    {
                        case MidiEventKind.ProgramChange:
                            state.Program = e.Data1;
                            break;

                        case MidiEventKind.ControlChange:
                            switch (e.Data1)
                            {
                                case 7:
                                    state.Volume = e.Data2 / 127f;
                                    break;
                                case 10:
                                    state.Pan = e.Data2 / 127f;
                                    break;
                                case 11:
                                    state.Expression = e.Data2 / 127f;
                                    break;
                                case 64:
                                    bool on = e.Data2 >= 64;
                                    if (state.Sustain && !on)
                                    {
                                        // Release everything that was held by the pedal.
                                        List<int> keys = new List<int>(sustained.Keys);
                                        foreach (int key in keys)
                                        {
                                            if (key / 128 == e.Channel)
                                            {
                                                CloseNote(active, key, sustained[key]);
                                                sustained.Remove(key);
                                            }
                                        }
                                    }
                                    state.Sustain = on;
                                    break;
                            }
                            break;

                        case MidiEventKind.PitchBend:
                        {
                            int value = e.Data1 | (e.Data2 << 7);
                            state.BendCents = (value - 8192) / 8192.0 * 200.0;
                            break;
                        }

                        case MidiEventKind.NoteOn:
                        {
                            int key = e.Channel * 128 + e.Data1;
                            ScheduledNote previous;
                            if (active.TryGetValue(key, out previous))
                            {
                                previous.End = Math.Max(previous.Start + 0.02, time);
                                active.Remove(key);
                            }

                            ScheduledNote note = new ScheduledNote
                            {
                                Start = time,
                                End = double.MaxValue,
                                Channel = e.Channel,
                                NoteNumber = e.Data1,
                                Velocity = e.Data2,
                                Program = state.Program,
                                Pan = state.Pan,
                                Volume = state.Volume * state.Expression,
                                BendCents = state.BendCents,
                                IsDrum = e.Channel == 9
                            };
                            active[key] = note;
                            notes.Add(note);
                            break;
                        }

                        case MidiEventKind.NoteOff:
                        {
                            int key = e.Channel * 128 + e.Data1;
                            if (state.Sustain)
                            {
                                sustained[key] = time;
                            }
                            else
                            {
                                CloseNote(active, key, time);
                            }
                            break;
                        }
                    }
                }

                double trackEnd = TickToSeconds(secondsPerTick, tick);
                foreach (KeyValuePair<int, ScheduledNote> pair in active)
                {
                    if (pair.Value.End == double.MaxValue)
                    {
                        pair.Value.End = trackEnd + 0.25;
                    }
                }
                active.Clear();
            }

            // Notes that never received a note-off end shortly after the last event.
            double end = TickToSeconds(secondsPerTick, lastTick) + 1.0;
            foreach (ScheduledNote note in notes)
            {
                if (note.End == double.MaxValue)
                {
                    note.End = end;
                }
                if (note.End - note.Start < 0.02)
                {
                    note.End = note.Start + 0.02;
                }
            }

            notes.Sort(delegate(ScheduledNote a, ScheduledNote b) { return a.Start.CompareTo(b.Start); });
            totalSeconds = end;
            return notes;
        }

        private static void CloseNote(Dictionary<int, ScheduledNote> active, int key, double time)
        {
            ScheduledNote note;
            if (active.TryGetValue(key, out note))
            {
                note.End = Math.Max(note.Start + 0.02, time);
                active.Remove(key);
            }
        }

        /// <summary>Builds a tick → seconds lookup from all tempo events.</summary>
        private static double[] BuildTempoMap(MidiSequence sequence, out long lastTick)
        {
            List<KeyValuePair<long, int>> tempoEvents = new List<KeyValuePair<long, int>>();
            lastTick = 0;

            foreach (MidiTrack track in sequence.Tracks)
            {
                long tick = 0;
                foreach (MidiEvent e in track.Events)
                {
                    tick += e.Delta;
                    if (e.Kind == MidiEventKind.Meta && e.MetaType == 0x51 && e.Data != null && e.Data.Length == 3)
                    {
                        int tempo = (e.Data[0] << 16) | (e.Data[1] << 8) | e.Data[2];
                        if (tempo > 0)
                        {
                            tempoEvents.Add(new KeyValuePair<long, int>(tick, tempo));
                        }
                    }
                }
                if (tick > lastTick)
                {
                    lastTick = tick;
                }
            }

            tempoEvents.Sort(delegate(KeyValuePair<long, int> a, KeyValuePair<long, int> b) { return a.Key.CompareTo(b.Key); });

            int ticksPerQuarter = Math.Max(1, sequence.TicksPerQuarterNote);
            int segments = Math.Max(1, tempoEvents.Count + 1);
            double[] secondsPerTick = new double[segments];

            int currentTempo = sequence.MicrosecondsPerQuarter > 0 ? sequence.MicrosecondsPerQuarter : 500000;
            for (int i = 0; i < segments; i++)
            {
                if (i < tempoEvents.Count)
                {
                    currentTempo = tempoEvents[i].Value;
                }
                secondsPerTick[i] = currentTempo / (ticksPerQuarter * 1000000.0);
            }

            // Segment boundaries: segment 0 starts at tick 0, segment i starts at the
            // i-th tempo change (i-1 in the event list).
            tempoBoundaries = new long[segments];
            tempoBoundaries[0] = 0;
            for (int i = 1; i < segments; i++)
            {
                tempoBoundaries[i] = tempoEvents[i - 1].Key;
            }
            return secondsPerTick;
        }

        [ThreadStatic]
        private static long[] tempoBoundaries;

        private static double TickToSeconds(double[] secondsPerTick, long tick)
        {
            long[] boundaries = tempoBoundaries;
            if (boundaries == null || boundaries.Length == 0 || secondsPerTick == null)
            {
                return 0;
            }

            double total = 0;
            for (int i = 0; i < boundaries.Length; i++)
            {
                long segmentStart = boundaries[i];
                if (tick <= segmentStart)
                {
                    break;
                }

                long segmentEnd = i + 1 < boundaries.Length ? boundaries[i + 1] : tick;
                long span = Math.Min(tick, segmentEnd) - segmentStart;
                if (span > 0)
                {
                    total += span * secondsPerTick[i];
                }
                if (tick <= segmentEnd)
                {
                    break;
                }
            }
            return total;
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        private static void Render(List<ScheduledNote> notes, Stream stream, int totalFrames, float volume)
        {
            float[] left = new float[BlockFrames];
            float[] right = new float[BlockFrames];
            byte[] pcm = new byte[BlockFrames * Channels * 2];
            Random noise = new Random(12345);
            List<ScheduledNote> active = new List<ScheduledNote>(64);

            int nextNote = 0;
            int frame = 0;
            while (frame < totalFrames)
            {
                int frames = Math.Min(BlockFrames, totalFrames - frame);
                Array.Clear(left, 0, frames);
                Array.Clear(right, 0, frames);

                double blockStart = (double)frame / SampleRate;
                double blockEnd = (double)(frame + frames) / SampleRate;

                // Add newly starting notes and drop finished ones.
                while (nextNote < notes.Count && notes[nextNote].Start < blockEnd)
                {
                    active.Add(notes[nextNote]);
                    nextNote++;
                }
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    if (active[i].End + 1.5 < blockStart)
                    {
                        active.RemoveAt(i);
                    }
                }

                for (int i = 0; i < active.Count; i++)
                {
                    try
                    {
                        RenderNote(active[i], left, right, frames, blockStart, noise);
                    }
                    catch (Exception ex)
                    {
                        Log.Verbose("Note rendering failed: " + ex.Message);
                    }
                }

                // Mix to 16-bit PCM with a cheap cubic soft limiter.
                for (int i = 0; i < frames; i++)
                {
                    float l = SoftClip(left[i] * volume * 1.1f) * 0.92f;
                    float r = SoftClip(right[i] * volume * 1.1f) * 0.92f;
                    short sl = (short)(l * 32000);
                    short sr = (short)(r * 32000);
                    int offset = i * 4;
                    pcm[offset] = (byte)(sl & 0xFF);
                    pcm[offset + 1] = (byte)((sl >> 8) & 0xFF);
                    pcm[offset + 2] = (byte)(sr & 0xFF);
                    pcm[offset + 3] = (byte)((sr >> 8) & 0xFF);
                }
                stream.Write(pcm, 0, frames * 4);

                frame += frames;
            }
        }

        private static void RenderNote(ScheduledNote note, float[] left, float[] right, int frames, double blockStart, Random noise)
        {
            double noteEnd = note.End;
            if (noteEnd <= blockStart)
            {
                return;
            }

            float velocity = note.Velocity / 127f;
            float panLeft = (float)Math.Sqrt(1.0 - note.Pan);
            float panRight = (float)Math.Sqrt(note.Pan);
            float amplitude = velocity * note.Volume * 0.32f * panLeft;
            float amplitudeRight = velocity * note.Volume * 0.32f * panRight;

            if (note.IsDrum)
            {
                RenderDrum(note, left, right, frames, blockStart, amplitude, amplitudeRight, noise);
                return;
            }

            InstrumentDefinition instrument = InstrumentMap.Get(note.Program);
            double frequency = Frequency(note.NoteNumber) * Math.Pow(2.0, note.BendCents / 1200.0)
                               * Math.Pow(2.0, instrument.OctaveShift);
            if (frequency < 8 || frequency > SampleRate * 0.45)
            {
                return;
            }

            double sustain = Math.Max(0.05, instrument.Sustain);
            double attack = Math.Max(0.001, instrument.Attack);
            double decay = Math.Max(0.01, instrument.Decay);
            double release = Math.Max(0.02, instrument.Release);

            double duration = noteEnd - note.Start;
            double totalLength = duration + release;
            if (totalLength <= 0)
            {
                return;
            }

            // Oscillator state, kept in the local scope for speed.
            double phase1 = 0;
            double phase2 = 0;
            double vibratoPhase = 0;
            double pwmPhase = 0;
            double detuneRatio = Math.Pow(2.0, instrument.Detune / 1200.0);

            // State-variable filter.
            double low = 0;
            double band = 0;
            double filterQ = 1.0 / Math.Max(0.5, instrument.Resonance);
            double baseStep = frequency / SampleRate;
            double secondRatio = instrument.HasSecondShape
                ? Math.Pow(2.0, instrument.SecondOctave)
                : detuneRatio;

            // Expensive functions (sin/pow) are evaluated once per control block and the
            // inner loop only does arithmetic.
            const int ControlBlock = 32;
            for (int block = 0; block < frames; block += ControlBlock)
            {
                int count = Math.Min(ControlBlock, frames - block);
                double blockTime = blockStart + (double)block / SampleRate;
                double localBlock = blockTime - note.Start;

                // Vibrato for this control block.
                double vibrato = 1.0;
                if (instrument.VibratoDepth > 0)
                {
                    vibratoPhase = TwoPi * instrument.VibratoRate * blockTime;
                    vibrato = Math.Pow(2.0, Math.Sin(vibratoPhase) * instrument.VibratoDepth / 1200.0);
                }

                // Pulse width for this control block.
                float duty = instrument.Duty;
                if (instrument.PulseWidthModulation > 0)
                {
                    pwmPhase = TwoPi * 0.6 * blockTime;
                    duty += (float)(Math.Sin(pwmPhase) * instrument.PulseWidthModulation);
                    if (duty < 0.05f)
                    {
                        duty = 0.05f;
                    }
                    else if (duty > 0.95f)
                    {
                        duty = 0.95f;
                    }
                }

                // Envelope value at the start of the block, for the filter cutoff.
                double envelopeAtBlock = EvaluateEnvelope(localBlock, attack, decay, sustain, duration, release);

                double cutoff = instrument.Cutoff + instrument.FilterEnvelope * envelopeAtBlock;
                if (cutoff > SampleRate * 0.45)
                {
                    cutoff = SampleRate * 0.45;
                }
                if (cutoff < 60)
                {
                    cutoff = 60;
                }
                double filterF = 2.0 * Math.Sin(Math.PI * cutoff / SampleRate);
                if (filterF > 0.99)
                {
                    filterF = 0.99;
                }

                double step1 = baseStep * vibrato;
                double step2 = step1 * secondRatio;
                double invRate = 1.0 / SampleRate;

                float[] table1 = WaveTables[(int)instrument.Shape * DutyBuckets + BucketForDuty(duty)];
                float[] table2 = instrument.HasSecondShape
                    ? WaveTables[(int)instrument.SecondShape * DutyBuckets + BucketForDuty(duty)]
                    : table1;

                double local = blockStart + (double)block / SampleRate - note.Start;
                for (int j = 0; j < count; j++, local += invRate)
                {
                    int i = block + j;
                    if (local < 0)
                    {
                        continue;
                    }
                    if (local > totalLength)
                    {
                        block = frames;
                        break;
                    }

                    double envelope;
                    if (local < attack)
                    {
                        envelope = local / attack;
                    }
                    else if (local < attack + decay)
                    {
                        envelope = 1.0 + (sustain - 1.0) * ((local - attack) / decay);
                    }
                    else if (local < duration)
                    {
                        envelope = sustain;
                    }
                    else
                    {
                        double k = (local - duration) / release;
                        if (k >= 1.0)
                        {
                            block = frames;
                            break;
                        }
                        envelope = sustain * (1.0 - k);
                    }
                    if (envelope <= 0)
                    {
                        continue;
                    }

                    phase1 += step1;
                    if (phase1 >= 1.0)
                    {
                        phase1 -= 1.0;
                    }
                    float sample = TableSample(table1, phase1);

                    if (instrument.Detune != 0)
                    {
                        phase2 += step2;
                        if (phase2 >= 1.0)
                        {
                            phase2 -= 1.0;
                        }
                        sample = sample * 0.6f + TableSample(table1, phase2) * 0.4f;
                    }
                    else if (instrument.HasSecondShape)
                    {
                        phase2 += step2;
                        if (phase2 >= 1.0)
                        {
                            phase2 -= 1.0;
                        }
                        sample = sample * (1.0f - instrument.SecondMix) +
                                 TableSample(table2, phase2) * instrument.SecondMix;
                    }

                    // Resonant low-pass filter.
                    low += filterF * band;
                    double high = sample - low - filterQ * band;
                    band += filterF * high;
                    sample = (float)low;

                    float value = sample * (float)envelope * instrument.Gain * 3.2f;
                    left[i] += value * amplitude * 3f;
                    right[i] += value * amplitudeRight * 3f;
                }
            }
        }

        private static double EvaluateEnvelope(double local, double attack, double decay, double sustain, double duration, double release)
        {
            if (local < 0)
            {
                return 0;
            }
            if (local < attack)
            {
                return local / attack;
            }
            if (local < attack + decay)
            {
                return 1.0 + (sustain - 1.0) * ((local - attack) / decay);
            }
            if (local < duration)
            {
                return sustain;
            }
            double k = (local - duration) / release;
            return k >= 1.0 ? 0.0 : sustain * (1.0 - k);
        }

        private static void RenderDrum(ScheduledNote note, float[] left, float[] right, int frames,
            double blockStart, float amplitude, float amplitudeRight, Random noise)
        {
            PercussionDefinition drum = InstrumentMap.GetDrum(note.NoteNumber);
            double start = note.Start;
            double length = Math.Max(0.02, drum.Length);

            double phase = 0;
            double lowpass = 0;
            double previousNoise = 0;
            double highpass = 0;

            for (int i = 0; i < frames; i++)
            {
                double t = blockStart + (double)i / SampleRate;
                double local = t - start;
                if (local < 0)
                {
                    continue;
                }
                if (local > length)
                {
                    break;
                }

                double k = local / length;
                double envelope = Math.Exp(-local / Math.Max(0.01, drum.Decay) * 3.0);
                if (k > 0.85)
                {
                    envelope *= (1.0 - k) / 0.15;
                }

                double frequency = drum.StartFrequency * Math.Pow(drum.EndFrequency / drum.StartFrequency, k);
                phase += frequency / SampleRate;
                if (phase >= 1.0)
                {
                    phase -= 1.0;
                }
                double tone = Waveform(drum.Shape, phase, drum.Duty);

                double sample = tone;
                if (drum.NoiseMix > 0)
                {
                    double white = noise.NextDouble() * 2.0 - 1.0;
                    lowpass += (white - lowpass) * 0.6;
                    double filtered = lowpass;
                    if (drum.NoiseHighpass > 0)
                    {
                        highpass = filtered - previousNoise + 0.85 * highpass;
                        previousNoise = filtered;
                        filtered = highpass;
                    }
                    sample = tone * (1.0 - drum.NoiseMix) + filtered * drum.NoiseMix;
                }

                float value = (float)(sample * envelope * drum.Gain * 2.6);
                left[i] += value * amplitude * 3f;
                right[i] += value * amplitudeRight * 3f;
            }
        }

        private static float SoftClip(float value)
        {
            if (value > 1.5f)
            {
                return 0.98f;
            }
            if (value < -1.5f)
            {
                return -0.98f;
            }
            return value - (value * value * value) / 6.75f;
        }

        private static double Waveform(WaveShape shape, double phase, float duty)
        {
            switch (shape)
            {
                case WaveShape.Triangle:
                    return 4.0 * Math.Abs(phase - 0.5) - 1.0;
                case WaveShape.Saw:
                    return phase * 2.0 - 1.0;
                case WaveShape.Sine:
                    return Math.Sin(phase * TwoPi);
                case WaveShape.Noise:
                    return 0.0;
                default:
                    return phase < duty ? 1.0 : -1.0;
            }
        }

        private static double Frequency(int note)
        {
            return 440.0 * Math.Pow(2.0, (note - 69) / 12.0);
        }

        private static void WriteWavHeader(Stream stream, int dataLength)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(36 + dataLength);
            writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)Channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * Channels * 2);
            writer.Write((short)(Channels * 2));
            writer.Write((short)16);
            writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            writer.Write(dataLength);
            writer.Flush();
        }
    }
}
