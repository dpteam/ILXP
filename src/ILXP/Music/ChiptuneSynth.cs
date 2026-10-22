using System;
using System.Collections.Generic;

namespace ILXP.Music
{
    /// <summary>
    /// Tiny software synthesizer that renders a <see cref="MidiSequence"/> to a 16-bit
    /// mono PCM WAV image. The sound is deliberately "chiptune": pulse waves with a
    /// short envelope, a triangle for bass notes and noise for percussion. This keeps
    /// the player dependency free (no DirectX, no third-party audio library) and works
    /// on Windows XP.
    /// </summary>
    internal static class ChiptuneSynth
    {
        public const int SampleRate = 22050;
        public const int MaxSeconds = 420;

        private sealed class Voice
        {
            public double Start;
            public double Duration;
            public int Note;
            public int Velocity;
            public int Channel;
            public int Program;
        }

        public static byte[] RenderWav(MidiSequence sequence, float volume)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException("sequence");
            }

            List<Voice> voices = CollectVoices(sequence, out double totalSeconds);
            totalSeconds = Math.Min(totalSeconds + 1.5, MaxSeconds);
            int samples = Math.Max(1, (int)(totalSeconds * SampleRate));
            float[] buffer = new float[samples];

            foreach (Voice voice in voices)
            {
                try
                {
                    RenderVoice(buffer, voice, sequence);
                }
                catch (Exception ex)
                {
                    Log.Verbose("Voice rendering failed: " + ex.Message);
                }
            }

            return EncodeWav(buffer, volume);
        }

        private static List<Voice> CollectVoices(MidiSequence sequence, out double totalSeconds)
        {
            double secondsPerTick = sequence.SecondsPerTick;
            List<Voice> voices = new List<Voice>();
            long longest = 0;

            foreach (MidiTrack track in sequence.Tracks)
            {
                Dictionary<int, int> programs = new Dictionary<int, int>();
                Dictionary<string, double> active = new Dictionary<string, double>(StringComparer.Ordinal);
                Dictionary<string, int> velocities = new Dictionary<string, int>(StringComparer.Ordinal);
                Dictionary<string, int> noteNumbers = new Dictionary<string, int>(StringComparer.Ordinal);

                double time = 0;
                foreach (MidiEvent e in track.Events)
                {
                    time += e.Delta * secondsPerTick;

                    switch (e.Kind)
                    {
                        case MidiEventKind.ProgramChange:
                            programs[e.Channel] = e.Data1;
                            break;

                        case MidiEventKind.NoteOn:
                        {
                            string key = e.Channel + ":" + e.Data1;
                            // Retriggering a held note closes the previous one first.
                            double start;
                            if (active.TryGetValue(key, out start))
                            {
                                voices.Add(CreateVoice(start, time, e.Data1, velocities[key], e.Channel, Get(programs, e.Channel)));
                            }
                            active[key] = time;
                            velocities[key] = e.Data2;
                            noteNumbers[key] = e.Data1;
                            break;
                        }

                        case MidiEventKind.NoteOff:
                        {
                            string key = e.Channel + ":" + e.Data1;
                            double start;
                            if (active.TryGetValue(key, out start))
                            {
                                voices.Add(CreateVoice(start, time, e.Data1, velocities[key], e.Channel, Get(programs, e.Channel)));
                                active.Remove(key);
                                velocities.Remove(key);
                                noteNumbers.Remove(key);
                            }
                            break;
                        }
                    }
                }

                foreach (KeyValuePair<string, double> pair in active)
                {
                    string[] parts = pair.Key.Split(':');
                    int channel = int.Parse(parts[0]);
                    int note = int.Parse(parts[1]);
                    int velocity;
                    velocities.TryGetValue(pair.Key, out velocity);
                    voices.Add(CreateVoice(pair.Value, time + 0.2, note, velocity == 0 ? 90 : velocity, channel, Get(programs, channel)));
                }

                long trackLength = 0;
                foreach (MidiEvent e in track.Events)
                {
                    trackLength += e.Delta;
                }
                if (trackLength > longest)
                {
                    longest = trackLength;
                }
            }

            voices.Sort(delegate(Voice a, Voice b) { return a.Start.CompareTo(b.Start); });
            totalSeconds = longest * secondsPerTick;
            return voices;
        }

        private static Voice CreateVoice(double start, double end, int note, int velocity, int channel, int program)
        {
            return new Voice
            {
                Start = start,
                Duration = Math.Max(0.03, end - start),
                Note = note,
                Velocity = velocity,
                Channel = channel,
                Program = program
            };
        }

        private static int Get(Dictionary<int, int> map, int key)
        {
            int value;
            return map.TryGetValue(key, out value) ? value : 0;
        }

        private static void RenderVoice(float[] buffer, Voice voice, MidiSequence sequence)
        {
            int start = (int)(voice.Start * SampleRate);
            if (start >= buffer.Length)
            {
                return;
            }

            if (voice.Channel == 9)
            {
                RenderDrum(buffer, voice, start);
                return;
            }

            double frequency = NoteToFrequency(voice.Note);
            if (frequency <= 0 || frequency > 8000)
            {
                return;
            }

            Waveform waveform = ChooseWaveform(voice);
            int length = (int)(voice.Duration * SampleRate);
            int releaseSamples = (int)(0.08 * SampleRate);
            length = Math.Min(length + releaseSamples, buffer.Length - start);
            if (length <= 0)
            {
                return;
            }

            double amplitude = (voice.Velocity / 127.0) * 0.16;
            double phase = 0;
            double phaseStep = frequency / SampleRate;

            int attack = Math.Min((int)(0.005 * SampleRate) + 1, length);
            int decay = (int)(0.12 * SampleRate);
            double sustain = waveform == Waveform.Pulse50 ? 0.75 : 0.55;
            double lowpass = 0;

            for (int i = 0; i < length; i++)
            {
                double envelope;
                if (i < attack)
                {
                    envelope = (double)i / attack;
                }
                else if (i < attack + decay)
                {
                    envelope = 1.0 - (1.0 - sustain) * ((i - attack) / (double)decay);
                }
                else
                {
                    envelope = sustain;
                }

                if (i > length - releaseSamples)
                {
                    envelope *= Math.Max(0.0, (length - i) / (double)releaseSamples);
                }

                double sample;
                switch (waveform)
                {
                    case Waveform.Triangle:
                        sample = 4.0 * Math.Abs(phase - 0.5) - 1.0;
                        break;
                    case Waveform.Pulse125:
                        sample = phase < 0.125 ? 1.0 : -1.0;
                        break;
                    case Waveform.Pulse25:
                        sample = phase < 0.25 ? 1.0 : -1.0;
                        break;
                    default:
                        sample = phase < 0.5 ? 1.0 : -1.0;
                        break;
                }

                // One-pole low-pass keeps the square waves from sounding too harsh.
                lowpass += (sample - lowpass) * 0.35;
                buffer[start + i] += (float)(lowpass * envelope * amplitude);

                phase += phaseStep;
                if (phase >= 1.0)
                {
                    phase -= 1.0;
                }
            }
        }

        private enum Waveform
        {
            Pulse50,
            Pulse25,
            Pulse125,
            Triangle
        }

        private static Waveform ChooseWaveform(Voice voice)
        {
            // Low notes become the bass (triangle), high notes lead (thin pulse),
            // and the program number nudges the duty cycle like the original player.
            if (voice.Note < 48)
            {
                return Waveform.Triangle;
            }
            if (voice.Note > 79)
            {
                return Waveform.Pulse125;
            }
            switch (voice.Program % 8)
            {
                case 0:
                case 1:
                    return Waveform.Pulse50;
                case 2:
                case 3:
                case 4:
                    return Waveform.Pulse25;
                default:
                    return Waveform.Pulse125;
            }
        }

        private static void RenderDrum(float[] buffer, Voice voice, int start)
        {
            double amplitude = (voice.Velocity / 127.0) * 0.30;
            int note = voice.Note;

            if (note == 35 || note == 36)
            {
                // Kick: falling sine.
                int length = Math.Min((int)(0.13 * SampleRate), buffer.Length - start);
                double phase = 0;
                for (int i = 0; i < length; i++)
                {
                    double t = i / (double)length;
                    double frequency = 150 - 110 * t;
                    phase += frequency / SampleRate;
                    double envelope = Math.Exp(-6.0 * t);
                    buffer[start + i] += (float)(Math.Sin(phase * 2 * Math.PI) * envelope * amplitude * 1.4);
                }
                return;
            }

            if (note == 38 || note == 40)
            {
                // Snare: short noise burst with a little body.
                int length = Math.Min((int)(0.12 * SampleRate), buffer.Length - start);
                Random random = new Random(note * 7919);
                double lowpass = 0;
                for (int i = 0; i < length; i++)
                {
                    double t = i / (double)length;
                    double noise = random.NextDouble() * 2 - 1;
                    lowpass += (noise - lowpass) * 0.5;
                    buffer[start + i] += (float)(lowpass * Math.Exp(-9.0 * t) * amplitude);
                }
                return;
            }

            // Hats, cymbals and toms: filtered noise.
            int hatLength = Math.Min((int)((note == 46 ? 0.16 : 0.06) * SampleRate), buffer.Length - start);
            Random hatRandom = new Random(note * 104729);
            double highpass = 0;
            double previous = 0;
            for (int i = 0; i < hatLength; i++)
            {
                double t = i / (double)hatLength;
                double noise = hatRandom.NextDouble() * 2 - 1;
                highpass = noise - previous + 0.7 * highpass;
                previous = noise;
                buffer[start + i] += (float)(highpass * Math.Exp(-14.0 * t) * amplitude * 0.6);
            }
        }

        private static double NoteToFrequency(int note)
        {
            return 440.0 * Math.Pow(2.0, (note - 69) / 12.0);
        }

        private static byte[] EncodeWav(float[] buffer, float volume)
        {
            if (volume <= 0)
            {
                volume = 0.5f;
            }

            int dataLength = buffer.Length * 2;
            byte[] wav = new byte[44 + dataLength];
            int offset = 0;

            WriteAscii(wav, ref offset, "RIFF");
            WriteInt32(wav, ref offset, 36 + dataLength);
            WriteAscii(wav, ref offset, "WAVE");
            WriteAscii(wav, ref offset, "fmt ");
            WriteInt32(wav, ref offset, 16);
            WriteInt16(wav, ref offset, 1);           // PCM
            WriteInt16(wav, ref offset, 1);           // mono
            WriteInt32(wav, ref offset, SampleRate);
            WriteInt32(wav, ref offset, SampleRate * 2);
            WriteInt16(wav, ref offset, 2);
            WriteInt16(wav, ref offset, 16);
            WriteAscii(wav, ref offset, "data");
            WriteInt32(wav, ref offset, dataLength);

            for (int i = 0; i < buffer.Length; i++)
            {
                // Soft limiter: keeps loud passages from clipping harshly.
                float value = (float)Math.Tanh(buffer[i] * volume * 1.15) * 0.95f;
                short sample = (short)(value * 32000);
                wav[offset++] = (byte)(sample & 0xFF);
                wav[offset++] = (byte)((sample >> 8) & 0xFF);
            }

            return wav;
        }

        private static void WriteAscii(byte[] buffer, ref int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                buffer[offset++] = (byte)text[i];
            }
        }

        private static void WriteInt32(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteInt16(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
        }
    }
}
