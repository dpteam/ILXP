using System;

namespace ILXP.Music
{
    /// <summary>Oscillator shape used by an instrument.</summary>
    internal enum WaveShape
    {
        Pulse,
        Triangle,
        Saw,
        Sine,
        Noise
    }

    /// <summary>
    /// Description of a single instrument. The values follow the General MIDI program
    /// families: the oscillator, envelope and filter are chosen so that a piano does not
    /// sound like a flute and a bass does not sound like a lead.
    /// </summary>
    internal sealed class InstrumentDefinition
    {
        public InstrumentDefinition(string name)
        {
            Name = name;
            Shape = WaveShape.Pulse;
            Duty = 0.5f;
            Attack = 0.004f;
            Decay = 0.15f;
            Sustain = 0.7f;
            Release = 0.10f;
            Cutoff = 6000f;
            Resonance = 0.7f;
            FilterEnvelope = 0f;
            VibratoRate = 0f;
            VibratoDepth = 0f;
            Detune = 0f;
            OctaveShift = 0;
            Gain = 0.20f;
            PulseWidthModulation = 0f;
            SecondShape = (WaveShape)(-1);
            SecondMix = 0f;
            SecondOctave = 0;
        }

        public string Name { get; private set; }

        public WaveShape Shape { get; set; }

        /// <summary>Duty cycle for <see cref="WaveShape.Pulse"/> (0.125 … 0.5).</summary>
        public float Duty { get; set; }

        public float Attack { get; set; }

        public float Decay { get; set; }

        public float Sustain { get; set; }

        public float Release { get; set; }

        /// <summary>Low-pass cutoff in Hz.</summary>
        public float Cutoff { get; set; }

        /// <summary>Filter resonance, 0.5 … 8.</summary>
        public float Resonance { get; set; }

        /// <summary>Extra cutoff in Hz contributed by the envelope at its peak.</summary>
        public float FilterEnvelope { get; set; }

        public float VibratoRate { get; set; }

        /// <summary>Vibrato depth in cents.</summary>
        public float VibratoDepth { get; set; }

        /// <summary>Detune of the second oscillator in cents.</summary>
        public float Detune { get; set; }

        public int OctaveShift { get; set; }

        public float Gain { get; set; }

        /// <summary>Pulse-width modulation depth (0 … 0.3), used by synth leads and pads.</summary>
        public float PulseWidthModulation { get; set; }

        /// <summary>Optional secondary oscillator shape (mixed at <see cref="SecondMix"/>).</summary>
        public WaveShape SecondShape { get; set; }

        public float SecondMix { get; set; }

        public int SecondOctave { get; set; }

        public bool HasSecondShape
        {
            get { return SecondMix > 0f && (int)SecondShape >= 0; }
        }
    }

    /// <summary>A percussion voice definition (channel 10 in MIDI numbering: channel 9).</summary>
    internal sealed class PercussionDefinition
    {
        public string Name;
        public WaveShape Shape = WaveShape.Sine;
        public float Duty = 0.5f;
        public float StartFrequency = 150f;
        public float EndFrequency = 40f;
        public float Decay = 0.12f;
        public float NoiseMix;
        public float NoiseCutoff = 6000f;
        public float NoiseHighpass;
        public float Gain = 0.35f;
        public float Length = 0.15f;
    }

    /// <summary>
    /// General MIDI program map. Each of the 128 programs resolves to an instrument with
    /// a distinct timbre; the chiptune character comes from pulse/triangle oscillators and
    /// a resonant low-pass filter rather than from a sample library.
    /// </summary>
    internal static class InstrumentMap
    {
        private static readonly InstrumentDefinition[] Programs = BuildPrograms();
        private static readonly PercussionDefinition[] Drums = BuildDrums();

        public static InstrumentDefinition Get(int program)
        {
            if (program < 0)
            {
                program = 0;
            }
            return Programs[program % Programs.Length];
        }

        public static PercussionDefinition GetDrum(int note)
        {
            if (note < 0 || note >= Drums.Length || Drums[note] == null)
            {
                return Drums[0];
            }
            return Drums[note];
        }

        public static string GetProgramName(int program)
        {
            return Get(program).Name;
        }

        private static InstrumentDefinition[] BuildPrograms()
        {
            InstrumentDefinition[] map = new InstrumentDefinition[128];

            // 0-7 Piano
            map[0] = Piano("Acoustic Grand Piano", 0.30f, 0.02f, 0.35f, 0.45f, 4200f);
            map[1] = Piano("Bright Acoustic Piano", 0.32f, 0.01f, 0.30f, 0.40f, 6500f);
            map[2] = Piano("Electric Grand Piano", 0.28f, 0.01f, 0.28f, 0.35f, 5200f);
            map[3] = Piano("Honky-tonk Piano", 0.30f, 0.01f, 0.32f, 0.42f, 4800f);
            map[4] = Bell("Electric Piano 1", 0.22f, 0.9f, 5200f);
            map[5] = Bell("Electric Piano 2", 0.22f, 1.0f, 6000f);
            map[6] = Bell("Harpsichord", 0.24f, 0.55f, 7200f);
            map[7] = Bell("Clavinet", 0.24f, 0.45f, 6800f);

            // 8-15 Chromatic percussion
            map[8] = Bell("Celesta", 0.20f, 0.75f, 9000f);
            map[9] = Bell("Glockenspiel", 0.18f, 0.55f, 12000f);
            map[10] = Bell("Music Box", 0.17f, 0.65f, 10000f);
            map[11] = Bell("Vibraphone", 0.18f, 1.20f, 7000f);
            map[12] = Bell("Marimba", 0.22f, 0.35f, 4200f);
            map[13] = Bell("Xylophone", 0.20f, 0.30f, 8000f);
            map[14] = Bell("Tubular Bells", 0.20f, 1.60f, 5200f);
            map[15] = Bell("Dulcimer", 0.22f, 0.70f, 6000f);

            // 16-23 Organ
            map[16] = Organ("Drawbar Organ", 0.5f, 0.30f, 3200f);
            map[17] = Organ("Percussive Organ", 0.25f, 0.26f, 4200f);
            map[18] = Organ("Rock Organ", 0.5f, 0.30f, 5200f);
            map[19] = Organ("Church Organ", 0.5f, 0.26f, 2600f);
            map[20] = Organ("Reed Organ", 0.25f, 0.24f, 2400f);
            map[21] = Accordion("Accordion", 0.5f, 0.24f, 3600f);
            map[22] = Accordion("Harmonica", 0.25f, 0.22f, 4200f);
            map[23] = Accordion("Tango Accordion", 0.25f, 0.24f, 3800f);

            // 24-31 Guitar
            map[24] = Plucked("Acoustic Guitar (nylon)", 0.26f, 0.28f, 3800f, 0.30f);
            map[25] = Plucked("Acoustic Guitar (steel)", 0.26f, 0.24f, 5200f, 0.35f);
            map[26] = Plucked("Electric Guitar (jazz)", 0.24f, 0.30f, 3200f, 0.25f);
            map[27] = Plucked("Electric Guitar (clean)", 0.26f, 0.26f, 4600f, 0.30f);
            map[28] = Plucked("Electric Guitar (muted)", 0.20f, 0.10f, 2600f, 0.10f);
            map[29] = Plucked("Overdriven Guitar", 0.24f, 0.30f, 6000f, 0.55f);
            map[30] = Plucked("Distortion Guitar", 0.24f, 0.34f, 7200f, 0.65f);
            map[31] = Plucked("Guitar Harmonics", 0.18f, 0.60f, 9000f, 0.30f);

            // 32-39 Bass
            map[32] = Bass("Acoustic Bass", 0.30f, 0.35f, 2200f);
            map[33] = Bass("Electric Bass (finger)", 0.32f, 0.28f, 2600f);
            map[34] = Bass("Electric Bass (pick)", 0.32f, 0.22f, 3200f);
            map[35] = Bass("Fretless Bass", 0.30f, 0.40f, 2000f);
            map[36] = Bass("Slap Bass 1", 0.30f, 0.16f, 3600f);
            map[37] = Bass("Slap Bass 2", 0.30f, 0.14f, 4200f);
            map[38] = Bass("Synth Bass 1", 0.32f, 0.20f, 3000f);
            map[39] = Bass("Synth Bass 2", 0.30f, 0.26f, 2400f);

            // 40-47 Strings
            map[40] = Strings("Violin", 0.22f, 0.10f, 4200f, 5.5f, 0.5f);
            map[41] = Strings("Viola", 0.22f, 0.12f, 3600f, 5.0f, 0.5f);
            map[42] = Strings("Cello", 0.22f, 0.14f, 3000f, 4.5f, 0.5f);
            map[43] = Strings("Contrabass", 0.22f, 0.16f, 2200f, 4.0f, 0.5f);
            map[44] = Strings("Tremolo Strings", 0.20f, 0.12f, 3800f, 6.0f, 1.4f);
            map[45] = Strings("Pizzicato Strings", 0.24f, 0.05f, 4200f, 3.5f, 0.2f);
            map[46] = Strings("Orchestral Harp", 0.22f, 0.30f, 5200f, 3.0f, 0.3f);
            map[47] = Strings("Timpani", 0.26f, 0.40f, 1800f, 3.0f, 0.0f);

            // 48-55 Ensemble
            map[48] = Ensemble("String Ensemble 1", 0.20f, 0.12f, 4000f, 7f, 0.6f);
            map[49] = Ensemble("String Ensemble 2", 0.20f, 0.10f, 4400f, 7f, 0.6f);
            map[50] = Ensemble("Synth Strings 1", 0.18f, 0.18f, 3800f, 6f, 1.0f);
            map[51] = Ensemble("Synth Strings 2", 0.18f, 0.20f, 3400f, 6f, 1.2f);
            map[52] = Choir("Choir Aahs", 0.16f, 0.14f, 3000f);
            map[53] = Choir("Voice Oohs", 0.16f, 0.16f, 2600f);
            map[54] = Choir("Synth Voice", 0.16f, 0.18f, 3400f);
            map[55] = Brass("Orchestra Hit", 0.24f, 0.10f, 6000f);

            // 56-63 Brass
            map[56] = Brass("Trumpet", 0.24f, 0.06f, 5200f);
            map[57] = Brass("Trombone", 0.24f, 0.08f, 4200f);
            map[58] = Brass("Tuba", 0.24f, 0.10f, 3000f);
            map[59] = Brass("Muted Trumpet", 0.22f, 0.05f, 6200f);
            map[60] = Brass("French Horn", 0.22f, 0.10f, 3600f);
            map[61] = Brass("Brass Section", 0.22f, 0.07f, 5000f);
            map[62] = Brass("Synth Brass 1", 0.22f, 0.08f, 5600f);
            map[63] = Brass("Synth Brass 2", 0.22f, 0.10f, 4600f);

            // 64-71 Reed
            map[64] = Reed("Soprano Sax", 0.22f, 0.06f, 4800f);
            map[65] = Reed("Alto Sax", 0.22f, 0.07f, 4200f);
            map[66] = Reed("Tenor Sax", 0.22f, 0.08f, 3600f);
            map[67] = Reed("Baritone Sax", 0.22f, 0.09f, 3000f);
            map[68] = Reed("Oboe", 0.22f, 0.05f, 5200f);
            map[69] = Reed("English Horn", 0.22f, 0.06f, 4400f);
            map[70] = Reed("Bassoon", 0.22f, 0.08f, 3000f);
            map[71] = Reed("Clarinet", 0.22f, 0.05f, 4000f);

            // 72-79 Pipe
            map[72] = Pipe("Piccolo", 0.18f, 0.05f, 9000f);
            map[73] = Pipe("Flute", 0.18f, 0.06f, 7000f);
            map[74] = Pipe("Recorder", 0.18f, 0.05f, 6500f);
            map[75] = Pipe("Pan Flute", 0.18f, 0.06f, 6000f);
            map[76] = Pipe("Blown Bottle", 0.16f, 0.10f, 4000f);
            map[77] = Pipe("Shakuhachi", 0.18f, 0.08f, 5200f);
            map[78] = Pipe("Whistle", 0.16f, 0.05f, 8000f);
            map[79] = Pipe("Ocarina", 0.16f, 0.06f, 6000f);

            // 80-87 Synth lead
            map[80] = SynthLead("Lead 1 (square)", 0.5f, 0.26f);
            map[81] = SynthLead("Lead 2 (sawtooth)", 0.25f, 0.26f);
            map[82] = SynthLead("Lead 3 (calliope)", 0.125f, 0.24f);
            map[83] = SynthLead("Lead 4 (chiff)", 0.25f, 0.22f);
            map[84] = SynthLead("Lead 5 (charang)", 0.5f, 0.24f);
            map[85] = SynthLead("Lead 6 (voice)", 0.125f, 0.22f);
            map[86] = SynthLead("Lead 7 (fifths)", 0.25f, 0.24f);
            map[87] = SynthLead("Lead 8 (bass+lead)", 0.5f, 0.26f);

            // 88-95 Synth pad
            map[88] = SynthPad("Pad 1 (new age)", 0.25f, 0.18f);
            map[89] = SynthPad("Pad 2 (warm)", 0.5f, 0.16f);
            map[90] = SynthPad("Pad 3 (polysynth)", 0.25f, 0.18f);
            map[91] = SynthPad("Pad 4 (choir)", 0.125f, 0.16f);
            map[92] = SynthPad("Pad 5 (bowed)", 0.5f, 0.14f);
            map[93] = SynthPad("Pad 6 (metallic)", 0.125f, 0.16f);
            map[94] = SynthPad("Pad 7 (halo)", 0.25f, 0.14f);
            map[95] = SynthPad("Pad 8 (sweep)", 0.5f, 0.15f);

            // 96-103 Synth effects
            map[96] = SynthEffect("FX 1 (rain)", 0.125f);
            map[97] = SynthEffect("FX 2 (soundtrack)", 0.25f);
            map[98] = SynthEffect("FX 3 (crystal)", 0.5f);
            map[99] = SynthEffect("FX 4 (atmosphere)", 0.25f);
            map[100] = SynthEffect("FX 5 (brightness)", 0.125f);
            map[101] = SynthEffect("FX 6 (goblins)", 0.5f);
            map[102] = SynthEffect("FX 7 (echoes)", 0.25f);
            map[103] = SynthEffect("FX 8 (sci-fi)", 0.125f);

            // 104-111 Ethnic
            map[104] = Plucked("Sitar", 0.22f, 0.35f, 5200f, 0.40f);
            map[105] = Plucked("Banjo", 0.22f, 0.18f, 6000f, 0.35f);
            map[106] = Plucked("Shamisen", 0.22f, 0.16f, 5200f, 0.35f);
            map[107] = Plucked("Koto", 0.22f, 0.24f, 4800f, 0.35f);
            map[108] = Bell("Kalimba", 0.20f, 0.30f, 6000f);
            map[109] = Pipe("Bagpipe", 0.20f, 0.06f, 4200f);
            map[110] = Strings("Fiddle", 0.22f, 0.08f, 4200f, 5.0f, 0.6f);
            map[111] = Reed("Shanai", 0.22f, 0.06f, 5200f);

            // 112-119 Percussive
            map[112] = Bell("Tinkle Bell", 0.18f, 0.50f, 11000f);
            map[113] = Bell("Agogo", 0.20f, 0.30f, 7000f);
            map[114] = Bell("Steel Drums", 0.22f, 0.40f, 6000f);
            map[115] = Plucked("Woodblock", 0.20f, 0.06f, 3200f, 0.20f);
            map[116] = Plucked("Taiko Drum", 0.26f, 0.18f, 1600f, 0.10f);
            map[117] = Plucked("Melodic Tom", 0.24f, 0.14f, 2200f, 0.15f);
            map[118] = Plucked("Synth Drum", 0.24f, 0.12f, 3000f, 0.20f);
            map[119] = Plucked("Reverse Cymbal", 0.20f, 0.60f, 8000f, 0.20f);

            // 120-127 Sound effects
            map[120] = SynthEffect("Guitar Fret Noise", 0.25f);
            map[121] = SynthEffect("Breath Noise", 0.125f);
            map[122] = SynthEffect("Seashore", 0.25f);
            map[123] = SynthEffect("Bird Tweet", 0.125f);
            map[124] = SynthEffect("Telephone Ring", 0.5f);
            map[125] = SynthEffect("Helicopter", 0.25f);
            map[126] = SynthEffect("Applause", 0.125f);
            map[127] = SynthEffect("Gunshot", 0.5f);

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i] == null)
                {
                    map[i] = Piano("Program " + i, 0.25f, 0.05f, 0.30f, 0.40f, 5000f);
                }
            }
            return map;
        }

        // ------------------------------------------------------------------
        // Family builders
        // ------------------------------------------------------------------

        private static InstrumentDefinition Piano(string name, float gain, float decay, float sustain, float release, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = 0.25f,
                Attack = 0.002f,
                Decay = decay,
                Sustain = sustain,
                Release = release,
                Cutoff = cutoff,
                Resonance = 1.1f,
                FilterEnvelope = cutoff * 0.6f,
                Gain = gain,
                SecondShape = WaveShape.Triangle,
                SecondMix = 0.35f
            };
        }

        private static InstrumentDefinition Bell(string name, float gain, float decay, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Sine,
                Attack = 0.001f,
                Decay = decay,
                Sustain = 0.05f,
                Release = decay * 0.8f,
                Cutoff = cutoff,
                Resonance = 2.0f,
                FilterEnvelope = cutoff * 0.8f,
                Gain = gain,
                SecondShape = WaveShape.Sine,
                SecondMix = 0.45f,
                SecondOctave = 1
            };
        }

        private static InstrumentDefinition Organ(string name, float duty, float gain, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = duty,
                Attack = 0.012f,
                Decay = 0.05f,
                Sustain = 0.92f,
                Release = 0.06f,
                Cutoff = cutoff,
                Resonance = 0.8f,
                Gain = gain,
                SecondShape = WaveShape.Sine,
                SecondMix = 0.30f,
                SecondOctave = 1
            };
        }

        private static InstrumentDefinition Accordion(string name, float duty, float gain, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = duty,
                Attack = 0.02f,
                Decay = 0.10f,
                Sustain = 0.85f,
                Release = 0.08f,
                Cutoff = cutoff,
                Resonance = 1.4f,
                VibratoRate = 5.5f,
                VibratoDepth = 8f,
                Gain = gain,
                SecondShape = WaveShape.Saw,
                SecondMix = 0.22f
            };
        }

        private static InstrumentDefinition Plucked(string name, float gain, float decay, float cutoff, float resonance)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Saw,
                Attack = 0.002f,
                Decay = decay,
                Sustain = 0.08f,
                Release = decay * 0.7f,
                Cutoff = cutoff,
                Resonance = 2.0f + resonance * 3f,
                FilterEnvelope = cutoff * 1.2f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.25f
            };
        }

        private static InstrumentDefinition Bass(string name, float gain, float decay, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Triangle,
                Attack = 0.004f,
                Decay = decay,
                Sustain = 0.55f,
                Release = 0.07f,
                Cutoff = cutoff,
                Resonance = 1.6f,
                FilterEnvelope = cutoff * 0.9f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.30f,
                SecondOctave = 1
            };
        }

        private static InstrumentDefinition Strings(string name, float gain, float attack, float cutoff, float resonance, float vibrato)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Saw,
                Attack = attack,
                Decay = 0.25f,
                Sustain = 0.85f,
                Release = 0.18f,
                Cutoff = cutoff,
                Resonance = resonance,
                FilterEnvelope = cutoff * 0.35f,
                VibratoRate = 5.2f,
                VibratoDepth = vibrato * 6f,
                Detune = 7f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.18f
            };
        }

        private static InstrumentDefinition Ensemble(string name, float gain, float attack, float cutoff, float resonance, float vibrato)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Saw,
                Attack = attack,
                Decay = 0.30f,
                Sustain = 0.88f,
                Release = 0.22f,
                Cutoff = cutoff,
                Resonance = resonance,
                FilterEnvelope = cutoff * 0.30f,
                VibratoRate = 4.8f,
                VibratoDepth = vibrato * 5f,
                Detune = 12f,
                Gain = gain,
                SecondShape = WaveShape.Saw,
                SecondMix = 0.35f
            };
        }

        private static InstrumentDefinition Choir(string name, float gain, float attack, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Triangle,
                Attack = attack,
                Decay = 0.35f,
                Sustain = 0.9f,
                Release = 0.25f,
                Cutoff = cutoff,
                Resonance = 3.0f,
                FilterEnvelope = cutoff * 0.4f,
                VibratoRate = 4.4f,
                VibratoDepth = 14f,
                Detune = 9f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.20f
            };
        }

        private static InstrumentDefinition Brass(string name, float gain, float attack, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Saw,
                Attack = attack,
                Decay = 0.12f,
                Sustain = 0.82f,
                Release = 0.09f,
                Cutoff = cutoff,
                Resonance = 2.2f,
                FilterEnvelope = cutoff * 0.8f,
                VibratoRate = 5.6f,
                VibratoDepth = 5f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.28f
            };
        }

        private static InstrumentDefinition Reed(string name, float gain, float attack, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = 0.25f,
                Attack = attack,
                Decay = 0.10f,
                Sustain = 0.85f,
                Release = 0.08f,
                Cutoff = cutoff,
                Resonance = 3.2f,
                FilterEnvelope = cutoff * 0.5f,
                VibratoRate = 5.0f,
                VibratoDepth = 9f,
                Gain = gain,
                SecondShape = WaveShape.Saw,
                SecondMix = 0.20f
            };
        }

        private static InstrumentDefinition Pipe(string name, float gain, float attack, float cutoff)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Sine,
                Attack = attack,
                Decay = 0.08f,
                Sustain = 0.88f,
                Release = 0.09f,
                Cutoff = cutoff,
                Resonance = 1.2f,
                VibratoRate = 5.8f,
                VibratoDepth = 11f,
                Gain = gain,
                SecondShape = WaveShape.Pulse,
                SecondMix = 0.16f,
                SecondOctave = 1
            };
        }

        private static InstrumentDefinition SynthLead(string name, float duty, float gain)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = duty,
                Attack = 0.003f,
                Decay = 0.10f,
                Sustain = 0.72f,
                Release = 0.08f,
                Cutoff = 9000f,
                Resonance = 2.6f,
                FilterEnvelope = 4000f,
                VibratoRate = 6.0f,
                VibratoDepth = 6f,
                PulseWidthModulation = 0.10f,
                Gain = gain,
                SecondShape = WaveShape.Saw,
                SecondMix = 0.25f
            };
        }

        private static InstrumentDefinition SynthPad(string name, float duty, float gain)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = duty,
                Attack = 0.25f,
                Decay = 0.40f,
                Sustain = 0.85f,
                Release = 0.45f,
                Cutoff = 3600f,
                Resonance = 2.0f,
                FilterEnvelope = 2200f,
                VibratoRate = 3.6f,
                VibratoDepth = 6f,
                Detune = 11f,
                PulseWidthModulation = 0.18f,
                Gain = gain,
                SecondShape = WaveShape.Triangle,
                SecondMix = 0.40f
            };
        }

        private static InstrumentDefinition SynthEffect(string name, float duty)
        {
            return new InstrumentDefinition(name)
            {
                Shape = WaveShape.Pulse,
                Duty = duty,
                Attack = 0.05f,
                Decay = 0.60f,
                Sustain = 0.55f,
                Release = 0.50f,
                Cutoff = 5200f,
                Resonance = 4.0f,
                FilterEnvelope = 3200f,
                VibratoRate = 7.0f,
                VibratoDepth = 20f,
                Detune = 15f,
                PulseWidthModulation = 0.22f,
                Gain = 0.15f,
                SecondShape = WaveShape.Noise,
                SecondMix = 0.08f
            };
        }

        // ------------------------------------------------------------------
        // Drum kit (GM percussion map)
        // ------------------------------------------------------------------

        private static PercussionDefinition[] BuildDrums()
        {
            PercussionDefinition[] drums = new PercussionDefinition[128];
            PercussionDefinition fallback = new PercussionDefinition
            {
                Name = "Percussion",
                Shape = WaveShape.Noise,
                Length = 0.08f,
                Decay = 0.08f,
                Gain = 0.22f,
                NoiseMix = 1f,
                NoiseCutoff = 6000f
            };
            for (int i = 0; i < drums.Length; i++)
            {
                drums[i] = fallback;
            }

            drums[35] = Kick("Acoustic Bass Drum", 0.40f, 0.14f);
            drums[36] = Kick("Bass Drum 1", 0.42f, 0.13f);
            drums[37] = new PercussionDefinition { Name = "Side Stick", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 3200f, NoiseHighpass = 900f, Decay = 0.03f, Length = 0.04f, Gain = 0.20f };
            drums[38] = Snare("Acoustic Snare", 0.34f, 0.14f);
            drums[39] = new PercussionDefinition { Name = "Hand Clap", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 4200f, NoiseHighpass = 1200f, Decay = 0.10f, Length = 0.12f, Gain = 0.26f };
            drums[40] = Snare("Electric Snare", 0.34f, 0.12f);
            drums[41] = Tom("Low Floor Tom", 90f, 55f, 0.24f, 0.28f);
            drums[42] = Hat("Closed Hi-Hat", 0.05f, 0.10f, 8000f);
            drums[43] = Tom("High Floor Tom", 130f, 80f, 0.22f, 0.26f);
            drums[44] = Hat("Pedal Hi-Hat", 0.04f, 0.09f, 7000f);
            drums[45] = Tom("Low Tom", 160f, 95f, 0.20f, 0.24f);
            drums[46] = Hat("Open Hi-Hat", 0.18f, 0.16f, 8000f);
            drums[47] = Tom("Low-Mid Tom", 190f, 110f, 0.19f, 0.23f);
            drums[48] = Tom("Hi-Mid Tom", 220f, 130f, 0.18f, 0.22f);
            drums[49] = Cymbal("Crash Cymbal 1", 0.30f, 0.40f, 7000f);
            drums[50] = Tom("High Tom", 260f, 150f, 0.17f, 0.21f);
            drums[51] = Cymbal("Ride Cymbal 1", 0.22f, 0.35f, 9000f);
            drums[52] = Cymbal("Chinese Cymbal", 0.24f, 0.45f, 8000f);
            drums[53] = Cymbal("Ride Bell", 0.18f, 0.30f, 10000f);
            drums[54] = new PercussionDefinition { Name = "Tambourine", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 9000f, NoiseHighpass = 4000f, Decay = 0.12f, Length = 0.14f, Gain = 0.20f };
            drums[55] = Cymbal("Splash Cymbal", 0.26f, 0.30f, 9000f);
            drums[56] = new PercussionDefinition { Name = "Cowbell", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 540f, EndFrequency = 540f, Decay = 0.12f, Length = 0.14f, Gain = 0.20f };
            drums[57] = Cymbal("Crash Cymbal 2", 0.30f, 0.42f, 7000f);
            drums[58] = new PercussionDefinition { Name = "Vibraslap", Shape = WaveShape.Pulse, Duty = 0.25f, StartFrequency = 320f, EndFrequency = 260f, Decay = 0.20f, Length = 0.22f, Gain = 0.18f };
            drums[59] = Cymbal("Ride Cymbal 2", 0.22f, 0.36f, 9000f);
            drums[60] = new PercussionDefinition { Name = "Hi Bongo", Shape = WaveShape.Sine, StartFrequency = 480f, EndFrequency = 300f, Decay = 0.10f, Length = 0.12f, Gain = 0.24f };
            drums[61] = new PercussionDefinition { Name = "Low Bongo", Shape = WaveShape.Sine, StartFrequency = 360f, EndFrequency = 230f, Decay = 0.11f, Length = 0.13f, Gain = 0.24f };
            drums[62] = new PercussionDefinition { Name = "Mute Hi Conga", Shape = WaveShape.Sine, StartFrequency = 420f, EndFrequency = 280f, Decay = 0.09f, Length = 0.10f, Gain = 0.24f };
            drums[63] = new PercussionDefinition { Name = "Open Hi Conga", Shape = WaveShape.Sine, StartFrequency = 420f, EndFrequency = 300f, Decay = 0.16f, Length = 0.18f, Gain = 0.26f };
            drums[64] = new PercussionDefinition { Name = "Low Conga", Shape = WaveShape.Sine, StartFrequency = 300f, EndFrequency = 200f, Decay = 0.18f, Length = 0.20f, Gain = 0.26f };
            drums[65] = Tom("High Timbale", 320f, 190f, 0.18f, 0.22f);
            drums[66] = Tom("Low Timbale", 240f, 150f, 0.20f, 0.24f);
            drums[67] = new PercussionDefinition { Name = "High Agogo", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 800f, EndFrequency = 800f, Decay = 0.10f, Length = 0.12f, Gain = 0.18f };
            drums[68] = new PercussionDefinition { Name = "Low Agogo", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 600f, EndFrequency = 600f, Decay = 0.11f, Length = 0.13f, Gain = 0.18f };
            drums[69] = new PercussionDefinition { Name = "Cabasa", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 11000f, NoiseHighpass = 5000f, Decay = 0.07f, Length = 0.09f, Gain = 0.18f };
            drums[70] = new PercussionDefinition { Name = "Maracas", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 10000f, NoiseHighpass = 4500f, Decay = 0.06f, Length = 0.08f, Gain = 0.18f };
            drums[71] = new PercussionDefinition { Name = "Short Whistle", Shape = WaveShape.Sine, StartFrequency = 1400f, EndFrequency = 1800f, Decay = 0.14f, Length = 0.16f, Gain = 0.16f };
            drums[72] = new PercussionDefinition { Name = "Long Whistle", Shape = WaveShape.Sine, StartFrequency = 1500f, EndFrequency = 1900f, Decay = 0.30f, Length = 0.34f, Gain = 0.16f };
            drums[73] = new PercussionDefinition { Name = "Short Guiro", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 7000f, NoiseHighpass = 2000f, Decay = 0.10f, Length = 0.12f, Gain = 0.16f };
            drums[74] = new PercussionDefinition { Name = "Long Guiro", Shape = WaveShape.Noise, NoiseMix = 1f, NoiseCutoff = 7000f, NoiseHighpass = 2000f, Decay = 0.22f, Length = 0.26f, Gain = 0.16f };
            drums[75] = new PercussionDefinition { Name = "Claves", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 1200f, EndFrequency = 1200f, Decay = 0.05f, Length = 0.06f, Gain = 0.20f };
            drums[76] = new PercussionDefinition { Name = "Hi Wood Block", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 900f, EndFrequency = 900f, Decay = 0.05f, Length = 0.06f, Gain = 0.20f };
            drums[77] = new PercussionDefinition { Name = "Low Wood Block", Shape = WaveShape.Pulse, Duty = 0.5f, StartFrequency = 600f, EndFrequency = 600f, Decay = 0.06f, Length = 0.07f, Gain = 0.20f };
            drums[78] = new PercussionDefinition { Name = "Mute Cuica", Shape = WaveShape.Triangle, StartFrequency = 300f, EndFrequency = 420f, Decay = 0.12f, Length = 0.14f, Gain = 0.18f };
            drums[79] = new PercussionDefinition { Name = "Open Cuica", Shape = WaveShape.Triangle, StartFrequency = 300f, EndFrequency = 520f, Decay = 0.22f, Length = 0.24f, Gain = 0.18f };
            drums[80] = new PercussionDefinition { Name = "Mute Triangle", Shape = WaveShape.Sine, StartFrequency = 2400f, EndFrequency = 2400f, Decay = 0.10f, Length = 0.12f, Gain = 0.14f };
            drums[81] = new PercussionDefinition { Name = "Open Triangle", Shape = WaveShape.Sine, StartFrequency = 2400f, EndFrequency = 2400f, Decay = 0.40f, Length = 0.44f, Gain = 0.14f };
            return drums;
        }

        private static PercussionDefinition Kick(string name, float gain, float decay)
        {
            return new PercussionDefinition
            {
                Name = name,
                Shape = WaveShape.Sine,
                StartFrequency = 150f,
                EndFrequency = 42f,
                Decay = decay,
                Length = decay * 1.1f,
                Gain = gain,
                NoiseMix = 0.08f,
                NoiseCutoff = 2200f
            };
        }

        private static PercussionDefinition Snare(string name, float gain, float decay)
        {
            return new PercussionDefinition
            {
                Name = name,
                Shape = WaveShape.Triangle,
                StartFrequency = 220f,
                EndFrequency = 150f,
                Decay = decay,
                Length = decay * 1.2f,
                Gain = gain,
                NoiseMix = 0.75f,
                NoiseCutoff = 6500f,
                NoiseHighpass = 700f
            };
        }

        private static PercussionDefinition Tom(string name, float start, float end, float decay, float length)
        {
            return new PercussionDefinition
            {
                Name = name,
                Shape = WaveShape.Sine,
                StartFrequency = start,
                EndFrequency = end,
                Decay = decay,
                Length = length,
                Gain = 0.30f,
                NoiseMix = 0.12f,
                NoiseCutoff = 3200f
            };
        }

        private static PercussionDefinition Hat(string name, float decay, float length, float cutoff)
        {
            return new PercussionDefinition
            {
                Name = name,
                Shape = WaveShape.Noise,
                NoiseMix = 1f,
                NoiseCutoff = cutoff,
                NoiseHighpass = 4000f,
                Decay = decay,
                Length = length,
                Gain = 0.16f
            };
        }

        private static PercussionDefinition Cymbal(string name, float decay, float length, float cutoff)
        {
            return new PercussionDefinition
            {
                Name = name,
                Shape = WaveShape.Noise,
                NoiseMix = 1f,
                NoiseCutoff = cutoff,
                NoiseHighpass = 2500f,
                Decay = decay,
                Length = length,
                Gain = 0.17f
            };
        }
    }
}
