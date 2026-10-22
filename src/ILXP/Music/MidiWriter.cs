using System;
using System.IO;
using System.Text;

namespace ILXP.Music
{
    /// <summary>Writes Standard MIDI Files. Running status is not used: every event carries its status byte.</summary>
    internal static class MidiWriter
    {
        public static void Write(MidiSequence sequence, string path)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException("sequence");
            }
            File.WriteAllBytes(path, Write(sequence));
        }

        public static byte[] Write(MidiSequence sequence)
        {
            using (MemoryStream output = new MemoryStream())
            {
                output.Write(Encoding.ASCII.GetBytes("MThd"), 0, 4);
                WriteInt32BigEndian(output, 6);
                WriteInt16BigEndian(output, sequence.Format);
                WriteInt16BigEndian(output, sequence.Tracks.Count);
                WriteInt16BigEndian(output, sequence.TicksPerQuarterNote);

                foreach (MidiTrack track in sequence.Tracks)
                {
                    byte[] data = WriteTrack(track);
                    output.Write(Encoding.ASCII.GetBytes("MTrk"), 0, 4);
                    WriteInt32BigEndian(output, data.Length);
                    output.Write(data, 0, data.Length);
                }

                return output.ToArray();
            }
        }

        private static byte[] WriteTrack(MidiTrack track)
        {
            using (MemoryStream output = new MemoryStream())
            {
                bool hasEnd = false;
                foreach (MidiEvent e in track.Events)
                {
                    WriteVarInt(output, e.Delta < 0 ? 0 : e.Delta);
                    switch (e.Kind)
                    {
                        case MidiEventKind.Meta:
                            output.WriteByte(0xFF);
                            output.WriteByte((byte)e.MetaType);
                            byte[] data = e.Data ?? new byte[0];
                            WriteVarInt(output, data.Length);
                            output.Write(data, 0, data.Length);
                            if (e.MetaType == 0x2F)
                            {
                                hasEnd = true;
                            }
                            break;

                        case MidiEventKind.SysEx:
                        {
                            output.WriteByte(0xF0);
                            byte[] sysex = e.Data ?? new byte[0];
                            WriteVarInt(output, sysex.Length);
                            output.Write(sysex, 0, sysex.Length);
                            break;
                        }

                        case MidiEventKind.NoteOff:
                            output.WriteByte((byte)(0x80 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            output.WriteByte((byte)Clamp7(e.Data2));
                            break;

                        case MidiEventKind.NoteOn:
                            output.WriteByte((byte)(0x90 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            output.WriteByte((byte)Clamp7(e.Data2));
                            break;

                        case MidiEventKind.PolyAftertouch:
                            output.WriteByte((byte)(0xA0 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            output.WriteByte((byte)Clamp7(e.Data2));
                            break;

                        case MidiEventKind.ControlChange:
                            output.WriteByte((byte)(0xB0 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            output.WriteByte((byte)Clamp7(e.Data2));
                            break;

                        case MidiEventKind.ProgramChange:
                            output.WriteByte((byte)(0xC0 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            break;

                        case MidiEventKind.ChannelAftertouch:
                            output.WriteByte((byte)(0xD0 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            break;

                        case MidiEventKind.PitchBend:
                            output.WriteByte((byte)(0xE0 | ClampChannel(e.Channel)));
                            output.WriteByte((byte)Clamp7(e.Data1));
                            output.WriteByte((byte)Clamp7(e.Data2));
                            break;
                    }
                }

                if (!hasEnd)
                {
                    WriteVarInt(output, 0);
                    output.WriteByte(0xFF);
                    output.WriteByte(0x2F);
                    output.WriteByte(0x00);
                }

                return output.ToArray();
            }
        }

        private static int ClampChannel(int channel)
        {
            return channel < 0 ? 0 : (channel > 15 ? 15 : channel);
        }

        private static int Clamp7(int value)
        {
            return value < 0 ? 0 : (value > 127 ? 127 : value);
        }

        internal static void WriteVarInt(Stream stream, long value)
        {
            if (value < 0)
            {
                value = 0;
            }

            byte[] buffer = new byte[8];
            int index = buffer.Length - 1;
            buffer[index] = (byte)(value & 0x7F);
            value >>= 7;
            while (value > 0 && index > 0)
            {
                buffer[--index] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }
            stream.Write(buffer, index, buffer.Length - index);
        }

        private static void WriteInt32BigEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        private static void WriteInt16BigEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }
    }
}
