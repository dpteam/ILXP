using System;
using System.IO;
using System.Text;

namespace ILXP.Music
{
    /// <summary>Reads Standard MIDI Files (format 0, 1 and 2).</summary>
    internal static class MidiReader
    {
        public static MidiSequence Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                return Read(stream);
            }
        }

        public static MidiSequence Read(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data, false))
            {
                return Read(stream);
            }
        }

        public static MidiSequence Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            MidiSequence sequence = new MidiSequence();
            byte[] header = new byte[4];
            if (!ReadExactly(stream, header, 4) || Encoding.ASCII.GetString(header) != "MThd")
            {
                throw new InvalidDataException("Not a MIDI file: missing 'MThd'.");
            }

            int headerLength = ReadInt32BigEndian(stream);
            byte[] headerData = new byte[headerLength];
            if (!ReadExactly(stream, headerData, headerLength) || headerLength < 6)
            {
                throw new InvalidDataException("Truncated MIDI header.");
            }

            sequence.Format = (headerData[0] << 8) | headerData[1];
            int trackCount = (headerData[2] << 8) | headerData[3];
            int division = (headerData[4] << 8) | headerData[5];
            if ((division & 0x8000) == 0 && division > 0)
            {
                sequence.TicksPerQuarterNote = division;
            }

            for (int i = 0; i < trackCount && stream.Position < stream.Length; i++)
            {
                byte[] chunkId = new byte[4];
                if (!ReadExactly(stream, chunkId, 4))
                {
                    break;
                }

                int chunkLength = ReadInt32BigEndian(stream);
                if (chunkLength < 0 || stream.Position + chunkLength > stream.Length)
                {
                    Log.Warn("Truncated MIDI chunk '" + Encoding.ASCII.GetString(chunkId) + "'.");
                    break;
                }

                byte[] chunk = new byte[chunkLength];
                ReadExactly(stream, chunk, chunkLength);

                if (Encoding.ASCII.GetString(chunkId) != "MTrk")
                {
                    Log.Verbose("Skipping MIDI chunk '" + Encoding.ASCII.GetString(chunkId) + "'.");
                    continue;
                }

                sequence.Tracks.Add(ReadTrack(chunk));
            }

            if (sequence.Tracks.Count == 0)
            {
                throw new InvalidDataException("The MIDI file contains no tracks.");
            }

            ApplyFirstTempo(sequence);
            return sequence;
        }

        private static MidiTrack ReadTrack(byte[] chunk)
        {
            MidiTrack track = new MidiTrack();
            int position = 0;
            int runningStatus = 0;

            while (position < chunk.Length)
            {
                long delta = ReadVarInt(chunk, ref position);
                if (position >= chunk.Length)
                {
                    break;
                }

                int status = chunk[position];
                if (status >= 0x80)
                {
                    position++;
                }
                else
                {
                    status = runningStatus;
                    if (status == 0)
                    {
                        break;
                    }
                }

                if (status < 0xF0)
                {
                    runningStatus = status;
                }

                int command = status & 0xF0;
                int channel = status & 0x0F;

                if (status == 0xFF)
                {
                    if (position >= chunk.Length)
                    {
                        break;
                    }
                    int type = chunk[position++];
                    int length = (int)ReadVarInt(chunk, ref position);
                    byte[] data = ReadSlice(chunk, ref position, length);
                    track.Events.Add(new MidiEvent
                    {
                        Delta = delta,
                        Kind = MidiEventKind.Meta,
                        MetaType = type,
                        Data = data
                    });
                    if (type == 0x2F)
                    {
                        break;
                    }
                    continue;
                }

                if (status == 0xF0 || status == 0xF7)
                {
                    int length = (int)ReadVarInt(chunk, ref position);
                    byte[] data = ReadSlice(chunk, ref position, length);
                    track.Events.Add(new MidiEvent { Delta = delta, Kind = MidiEventKind.SysEx, Data = data });
                    continue;
                }

                switch (command)
                {
                    case 0x80:
                    case 0x90:
                    case 0xA0:
                    case 0xB0:
                    case 0xE0:
                    {
                        if (position + 2 > chunk.Length)
                        {
                            return track;
                        }
                        int data1 = chunk[position++];
                        int data2 = chunk[position++];
                        MidiEventKind kind = command == 0x80 ? MidiEventKind.NoteOff
                            : command == 0x90 ? (data2 > 0 ? MidiEventKind.NoteOn : MidiEventKind.NoteOff)
                            : command == 0xA0 ? MidiEventKind.PolyAftertouch
                            : command == 0xB0 ? MidiEventKind.ControlChange
                            : MidiEventKind.PitchBend;
                        track.Events.Add(new MidiEvent
                        {
                            Delta = delta,
                            Kind = kind,
                            Channel = channel,
                            Data1 = data1,
                            Data2 = data2
                        });
                        break;
                    }

                    case 0xC0:
                    case 0xD0:
                    {
                        if (position >= chunk.Length)
                        {
                            return track;
                        }
                        int data1 = chunk[position++];
                        track.Events.Add(new MidiEvent
                        {
                            Delta = delta,
                            Kind = command == 0xC0 ? MidiEventKind.ProgramChange : MidiEventKind.ChannelAftertouch,
                            Channel = channel,
                            Data1 = data1
                        });
                        break;
                    }

                    default:
                        // Unknown status: stop parsing this track instead of losing sync.
                        return track;
                }
            }

            return track;
        }

        private static void ApplyFirstTempo(MidiSequence sequence)
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

        private static byte[] ReadSlice(byte[] data, ref int position, int length)
        {
            if (length <= 0)
            {
                return new byte[0];
            }
            if (position + length > data.Length)
            {
                length = Math.Max(0, data.Length - position);
            }

            byte[] slice = new byte[length];
            Buffer.BlockCopy(data, position, slice, 0, length);
            position += length;
            return slice;
        }

        internal static long ReadVarInt(byte[] data, ref int position)
        {
            long value = 0;
            int guard = 0;
            while (position < data.Length && guard++ < 8)
            {
                byte b = data[position++];
                value = (value << 7) | (uint)(b & 0x7F);
                if ((b & 0x80) == 0)
                {
                    break;
                }
            }
            return value;
        }

        private static int ReadInt32BigEndian(Stream stream)
        {
            byte[] buffer = new byte[4];
            if (!ReadExactly(stream, buffer, 4))
            {
                return 0;
            }
            return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        }

        private static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int read = 0;
            while (read < count)
            {
                int chunk = stream.Read(buffer, read, count - read);
                if (chunk <= 0)
                {
                    return false;
                }
                read += chunk;
            }
            return true;
        }
    }
}
