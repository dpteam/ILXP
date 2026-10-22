using System;
using System.Collections.Generic;

namespace ILXP.Music
{
    /// <summary>Kind of a MIDI channel event, kept close to the wire format.</summary>
    internal enum MidiEventKind
    {
        NoteOff,
        NoteOn,
        PolyAftertouch,
        ControlChange,
        ProgramChange,
        ChannelAftertouch,
        PitchBend,
        Meta,
        SysEx
    }

    /// <summary>One event inside a MIDI track, with its delta time in ticks.</summary>
    internal sealed class MidiEvent
    {
        public long Delta;
        public MidiEventKind Kind;
        public int Channel;
        public int Data1;
        public int Data2;
        public int MetaType;
        public byte[] Data;

        public static MidiEvent Note(long delta, int channel, int note, int velocity)
        {
            return new MidiEvent
            {
                Delta = delta,
                Kind = velocity > 0 ? MidiEventKind.NoteOn : MidiEventKind.NoteOff,
                Channel = channel,
                Data1 = note,
                Data2 = velocity
            };
        }

        public static MidiEvent Meta(long delta, int type, byte[] data)
        {
            return new MidiEvent { Delta = delta, Kind = MidiEventKind.Meta, MetaType = type, Data = data ?? new byte[0] };
        }

        public static MidiEvent Tempo(long delta, int microsecondsPerQuarter)
        {
            return Meta(delta, 0x51, new[]
            {
                (byte)((microsecondsPerQuarter >> 16) & 0xFF),
                (byte)((microsecondsPerQuarter >> 8) & 0xFF),
                (byte)(microsecondsPerQuarter & 0xFF)
            });
        }

        public static MidiEvent EndOfTrack(long delta)
        {
            return new MidiEvent { Delta = delta, Kind = MidiEventKind.Meta, MetaType = 0x2F, Data = new byte[0] };
        }
    }

    /// <summary>A MIDI track: an ordered list of delta-timed events.</summary>
    internal sealed class MidiTrack
    {
        public MidiTrack()
        {
            Events = new List<MidiEvent>();
        }

        public List<MidiEvent> Events { get; private set; }

        public string Name
        {
            get
            {
                foreach (MidiEvent e in Events)
                {
                    if (e.Kind == MidiEventKind.Meta && e.MetaType == 0x03 && e.Data != null && e.Data.Length > 0)
                    {
                        return MidiTextFormat.DecodeMetaText(e.Data);
                    }
                }
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// A parsed MIDI file. Tempo changes are folded into <see cref="MicrosecondsPerQuarter"/>
    /// (the first tempo event wins for scheduling purposes).
    /// </summary>
    internal sealed class MidiSequence
    {
        public MidiSequence()
        {
            Tracks = new List<MidiTrack>();
            MicrosecondsPerQuarter = 500000;
            TicksPerQuarterNote = 480;
        }

        public int Format { get; set; }

        public int TicksPerQuarterNote { get; set; }

        public int MicrosecondsPerQuarter { get; set; }

        public List<MidiTrack> Tracks { get; private set; }

        /// <summary>Seconds per tick.</summary>
        public double SecondsPerTick
        {
            get { return MicrosecondsPerQuarter / (TicksPerQuarterNote * 1000000.0); }
        }

        public string Title
        {
            get
            {
                foreach (MidiTrack track in Tracks)
                {
                    string name = track.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>Approximate duration in seconds.</summary>
        public double Duration
        {
            get
            {
                long maxTicks = 0;
                foreach (MidiTrack track in Tracks)
                {
                    long total = 0;
                    foreach (MidiEvent e in track.Events)
                    {
                        total += e.Delta;
                    }
                    if (total > maxTicks)
                    {
                        maxTicks = total;
                    }
                }
                return maxTicks * SecondsPerTick;
            }
        }

        public int NoteCount
        {
            get
            {
                int count = 0;
                foreach (MidiTrack track in Tracks)
                {
                    foreach (MidiEvent e in track.Events)
                    {
                        if (e.Kind == MidiEventKind.NoteOn && e.Data2 > 0)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }
    }
}
