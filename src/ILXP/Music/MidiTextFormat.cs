using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ILXP.Music
{
    /// <summary>
    /// Text representation of a MIDI file, compatible with the format used by the
    /// original music player that shipped with this project:
    ///
    /// <code>
    /// Format: 1
    /// Tracks: 2
    /// TicksPerQuarterNote: 480
    ///
    /// [Track]
    /// 0   Meta      TrackName   "Piano"
    /// 0   NoteOn    Ch:0  N:60  V:100
    /// 240 NoteOff   Ch:0  N:60
    /// </code>
    ///
    /// The format is intentionally line oriented and lossy-friendly: unknown meta
    /// events are preserved as hex so that a MIDI -> text -> MIDI round trip keeps the
    /// original bytes.
    /// </summary>
    internal static class MidiTextFormat
    {
        private static readonly Regex EventPattern = new Regex(
            @"^(?<delta>\d+)\s+(?<kind>[A-Za-z]+)\s+(?<args>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PairPattern = new Regex(
            @"(?<key>[A-Za-z0-9_]+):(?<value>-?\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static MidiSequence Parse(string text)
        {
            MidiSequence sequence = new MidiSequence();
            if (string.IsNullOrEmpty(text))
            {
                return sequence;
            }

            MidiTrack current = null;
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                {
                    continue;
                }

                if (line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase))
                {
                    sequence.Format = ParseInt(line.Substring(7));
                    continue;
                }
                if (line.StartsWith("Tracks:", StringComparison.OrdinalIgnoreCase))
                {
                    // Informational only: the actual track count comes from [Track] markers.
                    continue;
                }
                if (line.StartsWith("TicksPerQuarterNote:", StringComparison.OrdinalIgnoreCase))
                {
                    int ticks = ParseInt(line.Substring(20));
                    if (ticks > 0)
                    {
                        sequence.TicksPerQuarterNote = ticks;
                    }
                    continue;
                }
                if (line.Equals("[Track]", StringComparison.OrdinalIgnoreCase))
                {
                    current = new MidiTrack();
                    sequence.Tracks.Add(current);
                    continue;
                }
                if (current == null)
                {
                    continue;
                }

                MidiEvent evt = ParseEvent(line);
                if (evt != null)
                {
                    current.Events.Add(evt);
                }
            }

            ApplyTempo(sequence);
            return sequence;
        }

        public static string Format(MidiSequence sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException("sequence");
            }

            StringBuilder text = new StringBuilder();
            text.Append("Format: ").Append(sequence.Format).AppendLine();
            text.Append("Tracks: ").Append(sequence.Tracks.Count).AppendLine();
            text.Append("TicksPerQuarterNote: ").Append(sequence.TicksPerQuarterNote).AppendLine();

            foreach (MidiTrack track in sequence.Tracks)
            {
                text.AppendLine();
                text.AppendLine("[Track]");
                foreach (MidiEvent e in track.Events)
                {
                    FormatEvent(text, e);
                }
            }
            return text.ToString();
        }

        private static void FormatEvent(StringBuilder text, MidiEvent e)
        {
            text.Append(e.Delta.ToString(CultureInfo.InvariantCulture)).Append('\t');

            switch (e.Kind)
            {
                case MidiEventKind.Meta:
                    text.Append("Meta\t").Append(GetMetaName(e.MetaType)).Append('\t');
                    if (IsTextMeta(e.MetaType) && e.Data != null)
                    {
                        text.Append('"').Append(EncodeMetaText(e.Data)).Append('"');
                    }
                    else if (e.Data != null)
                    {
                        text.Append(ToHex(e.Data));
                    }
                    break;

                case MidiEventKind.SysEx:
                    text.Append("SysEx\t").Append("F0\t").Append(ToHex(e.Data));
                    break;

                case MidiEventKind.NoteOff:
                    text.Append("NoteOff\tCh:").Append(e.Channel).Append("\tN:").Append(e.Data1).Append("\tV:").Append(e.Data2);
                    break;

                case MidiEventKind.NoteOn:
                    text.Append("NoteOn\tCh:").Append(e.Channel).Append("\tN:").Append(e.Data1).Append("\tV:").Append(e.Data2);
                    break;

                case MidiEventKind.PolyAftertouch:
                    text.Append("PolyAft\tCh:").Append(e.Channel).Append("\tN:").Append(e.Data1).Append("\tV:").Append(e.Data2);
                    break;

                case MidiEventKind.ControlChange:
                    text.Append("CC\tCh:").Append(e.Channel).Append("\tNum:").Append(e.Data1).Append("\tVal:").Append(e.Data2);
                    break;

                case MidiEventKind.ProgramChange:
                    text.Append("PC\tCh:").Append(e.Channel).Append("\tProg:").Append(e.Data1);
                    break;

                case MidiEventKind.ChannelAftertouch:
                    text.Append("ChAft\tCh:").Append(e.Channel).Append("\tVal:").Append(e.Data1);
                    break;

                case MidiEventKind.PitchBend:
                    text.Append("PitchBend\tCh:").Append(e.Channel).Append("\tVal:").Append(e.Data1 | (e.Data2 << 7));
                    break;
            }

            text.AppendLine();
        }

        private static MidiEvent ParseEvent(string line)
        {
            Match match = EventPattern.Match(line);
            if (!match.Success)
            {
                return null;
            }

            long delta;
            if (!long.TryParse(match.Groups["delta"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out delta))
            {
                return null;
            }

            string kind = match.Groups["kind"].Value;
            string args = match.Groups["args"].Value.Trim();

            if (string.Equals(kind, "Meta", StringComparison.OrdinalIgnoreCase))
            {
                int split = args.IndexOfAny(new[] { ' ', '\t' });
                string name = split < 0 ? args : args.Substring(0, split);
                string payload = split < 0 ? string.Empty : args.Substring(split + 1).Trim();
                return MidiEvent.Meta(delta, GetMetaType(name), ParsePayload(payload));            }

            if (string.Equals(kind, "SysEx", StringComparison.OrdinalIgnoreCase))
            {
                int split = args.IndexOfAny(new[] { ' ', '\t' });
                string payload = split < 0 ? string.Empty : args.Substring(split + 1).Trim();
                return new MidiEvent { Delta = delta, Kind = MidiEventKind.SysEx, Data = FromHex(payload) };
            }

            Dictionary<string, int> values = ParsePairs(args);
            int channel = Get(values, "Ch");
            switch (kind.ToLowerInvariant())
            {
                case "noteon":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.NoteOn, Channel = channel, Data1 = Get(values, "N"), Data2 = Get(values, "V") };
                case "noteoff":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.NoteOff, Channel = channel, Data1 = Get(values, "N"), Data2 = Get(values, "V") };
                case "polyaft":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.PolyAftertouch, Channel = channel, Data1 = Get(values, "N"), Data2 = Get(values, "V") };
                case "cc":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.ControlChange, Channel = channel, Data1 = Get(values, "Num"), Data2 = Get(values, "Val") };
                case "pc":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.ProgramChange, Channel = channel, Data1 = Get(values, "Prog") };
                case "chaft":
                    return new MidiEvent { Delta = delta, Kind = MidiEventKind.ChannelAftertouch, Channel = channel, Data1 = Get(values, "Val") };
                case "pitchbend":
                {
                    int bend = Get(values, "Val");
                    return new MidiEvent
                    {
                        Delta = delta,
                        Kind = MidiEventKind.PitchBend,
                        Channel = channel,
                        Data1 = bend & 0x7F,
                        Data2 = (bend >> 7) & 0x7F
                    };
                }
                default:
                    return null;
            }
        }

        private static void ApplyTempo(MidiSequence sequence)
        {
            foreach (MidiTrack track in sequence.Tracks)
            {
                foreach (MidiEvent e in track.Events)
                {
                    if (e.Kind == MidiEventKind.Meta && e.MetaType == 0x51 && e.Data != null && e.Data.Length == 3)
                    {
                        sequence.MicrosecondsPerQuarter = (e.Data[0] << 16) | (e.Data[1] << 8) | e.Data[2];
                        return;
                    }
                }
            }
        }

        private static byte[] ParsePayload(string payload)
        {
            if (payload.Length == 0)
            {
                return new byte[0];
            }

            if (payload.Length >= 2 && payload[0] == '"' && payload[payload.Length - 1] == '"')
            {
                string text = payload.Substring(1, payload.Length - 2)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\\", "\\");
                return Encoding.UTF8.GetBytes(text);
            }

            return FromHex(payload);
        }

        internal static string DecodeMetaText(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(data).TrimEnd('\0');
            }
            catch (Exception)
            {
                try
                {
                    return Encoding.Default.GetString(data).TrimEnd('\0');
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        private static string EncodeMetaText(byte[] data)
        {
            string text = DecodeMetaText(data);
            return text.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static int ParseInt(string value)
        {
            int result;
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private static Dictionary<string, int> ParsePairs(string args)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in PairPattern.Matches(args))
            {
                int value;
                if (int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    values[match.Groups["key"].Value] = value;
                }
            }
            return values;
        }

        private static int Get(Dictionary<string, int> values, string key)
        {
            int value;
            return values.TryGetValue(key, out value) ? value : 0;
        }

        internal static string ToHex(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }
            StringBuilder text = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                text.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        internal static byte[] FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return new byte[0];
            }

            hex = hex.Replace(" ", string.Empty).Replace("-", string.Empty);
            if ((hex.Length & 1) == 1)
            {
                hex = "0" + hex;
            }

            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                byte value;
                if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                {
                    return new byte[0];
                }
                data[i] = value;
            }
            return data;
        }

        internal static bool IsTextMeta(int type)
        {
            return type >= 0x01 && type <= 0x07;
        }

        internal static string GetMetaName(int type)
        {
            switch (type)
            {
                case 0x00: return "SequenceNumber";
                case 0x01: return "Text";
                case 0x02: return "Copyright";
                case 0x03: return "TrackName";
                case 0x04: return "Instrument";
                case 0x05: return "Lyric";
                case 0x06: return "Marker";
                case 0x07: return "CuePoint";
                case 0x20: return "ChannelPrefix";
                case 0x21: return "MidiPort";
                case 0x2F: return "EndOfTrack";
                case 0x51: return "Tempo";
                case 0x54: return "SMPTEOffset";
                case 0x58: return "TimeSignature";
                case 0x59: return "KeySignature";
                case 0x7F: return "SequencerSpecific";
                default: return "0x" + type.ToString("X2", CultureInfo.InvariantCulture);
            }
        }

        internal static int GetMetaType(string name)
        {
            switch (name)
            {
                case "SequenceNumber": return 0x00;
                case "Text": return 0x01;
                case "Copyright": return 0x02;
                case "TrackName": return 0x03;
                case "Instrument": return 0x04;
                case "Lyric": return 0x05;
                case "Marker": return 0x06;
                case "CuePoint": return 0x07;
                case "ChannelPrefix": return 0x20;
                case "MidiPort": return 0x21;
                case "EndOfTrack": return 0x2F;
                case "Tempo": return 0x51;
                case "SMPTEOffset": return 0x54;
                case "TimeSignature": return 0x58;
                case "KeySignature": return 0x59;
                case "SequencerSpecific": return 0x7F;
                default:
                    if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        int value;
                        if (int.TryParse(name.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                        {
                            return value;
                        }
                    }
                    return 0x7F;
            }
        }
    }
}
