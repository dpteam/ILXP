using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ILXP
{
    /// <summary>One PE section as seen by the health report.</summary>
    internal sealed class PeSection
    {
        public string Name;
        public uint VirtualAddress;
        public uint VirtualSize;
        public uint RawOffset;
        public uint RawSize;
        public uint Characteristics;
        public double Entropy;

        public bool IsExecutable
        {
            get { return (Characteristics & 0x20000000) != 0; }
        }

        public bool IsWritable
        {
            get { return (Characteristics & 0x80000000) != 0; }
        }

        public double FilledRatio
        {
            get { return VirtualSize == 0 ? 0 : Math.Min(1.0, (double)RawSize / VirtualSize); }
        }
    }

    /// <summary>
    /// Minimal PE reader used by the health report. It only reads what the analysis needs
    /// (headers, sections, entropy, CLR flags) and never throws for damaged files: every
    /// value degrades to a safe default.
    /// </summary>
    internal static class PeInspector
    {
        public sealed class PeInfo
        {
            public bool IsValid;
            public bool IsManaged;
            public string Machine = "unknown";
            public ushort MachineValue;
            public bool Is64Bit;
            public uint Timestamp;
            public uint ImageBase;
            public uint EntryPoint;
            public ushort Subsystem;
            public uint Characteristics;
            public bool HasStrongName;
            public bool IsIlOnly = true;
            public bool Requires32Bit;
            public bool Prefers32Bit;
            public string ClrVersion;
            public List<PeSection> Sections = new List<PeSection>();
            public double FileEntropy;
            public long FileSize;
            public string Error;
        }

        public static PeInfo Inspect(string path)
        {
            PeInfo info = new PeInfo();
            byte[] data;
            try
            {
                FileInfo file = new FileInfo(path);
                info.FileSize = file.Exists ? file.Length : 0;
                data = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
                return info;
            }

            return Inspect(data);
        }

        public static PeInfo Inspect(byte[] data)
        {
            PeInfo info = new PeInfo { FileSize = data == null ? 0 : data.Length };
            if (data == null || data.Length < 0x100)
            {
                info.Error = "file is too small to be a PE image";
                return info;
            }

            try
            {
                info.FileEntropy = Entropy(data, 0, data.Length);

                int pe = BitConverter.ToInt32(data, 0x3C);
                if (pe <= 0 || pe + 0x100 > data.Length)
                {
                    info.Error = "invalid DOS header";
                    return info;
                }
                if (data[pe] != 'P' || data[pe + 1] != 'E')
                {
                    info.Error = "missing PE signature";
                    return info;
                }

                info.IsValid = true;
                info.MachineValue = BitConverter.ToUInt16(data, pe + 4);
                info.Machine = MachineName(info.MachineValue);
                ushort sectionCount = BitConverter.ToUInt16(data, pe + 6);
                info.Timestamp = BitConverter.ToUInt32(data, pe + 8);
                ushort optionalSize = BitConverter.ToUInt16(data, pe + 20);
                info.Characteristics = BitConverter.ToUInt16(data, pe + 22);

                int optional = pe + 24;
                ushort magic = BitConverter.ToUInt16(data, optional);
                info.Is64Bit = magic == 0x20B;
                info.EntryPoint = BitConverter.ToUInt32(data, optional + 16);
                info.ImageBase = info.Is64Bit
                    ? (uint)BitConverter.ToUInt64(data, optional + 24)
                    : BitConverter.ToUInt32(data, optional + 28);
                info.Subsystem = BitConverter.ToUInt16(data, optional + 68);

                int dataDirectories = optional + (info.Is64Bit ? 112 : 96);
                uint cliRva = 0;
                if (dataDirectories + 15 * 8 <= data.Length)
                {
                    cliRva = BitConverter.ToUInt32(data, dataDirectories + 14 * 8);
                }
                info.IsManaged = cliRva != 0;

                int sectionTable = optional + optionalSize;
                for (int i = 0; i < sectionCount; i++)
                {
                    int offset = sectionTable + i * 40;
                    if (offset + 40 > data.Length)
                    {
                        break;
                    }

                    PeSection section = new PeSection
                    {
                        Name = ReadName(data, offset),
                        VirtualSize = BitConverter.ToUInt32(data, offset + 8),
                        VirtualAddress = BitConverter.ToUInt32(data, offset + 12),
                        RawSize = BitConverter.ToUInt32(data, offset + 16),
                        RawOffset = BitConverter.ToUInt32(data, offset + 20),
                        Characteristics = BitConverter.ToUInt32(data, offset + 36)
                    };

                    if (section.RawOffset + section.RawSize <= data.Length && section.RawSize > 0)
                    {
                        section.Entropy = Entropy(data, (int)section.RawOffset, (int)section.RawSize);
                    }
                    info.Sections.Add(section);
                }

                if (cliRva != 0)
                {
                    ReadClrFlags(info, data, cliRva);
                }
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }

            return info;
        }

        private static void ReadClrFlags(PeInfo info, byte[] data, uint cliRva)
        {
            int offset = RvaToOffset(info, cliRva);
            if (offset < 0 || offset + 24 > data.Length)
            {
                return;
            }

            // IMAGE_COR20_HEADER: cb, MajorRuntimeVersion, MinorRuntimeVersion, MetaData,
            // Flags, EntryPointToken, Resources, StrongNameSignature, ...
            ushort major = BitConverter.ToUInt16(data, offset + 4);
            ushort minor = BitConverter.ToUInt16(data, offset + 6);
            info.ClrVersion = "v" + major + "." + minor;

            uint flags = BitConverter.ToUInt32(data, offset + 16);
            info.IsIlOnly = (flags & 0x1) != 0;
            info.Requires32Bit = (flags & 0x2) != 0;
            info.Prefers32Bit = (flags & 0x20000) != 0;

            uint strongNameRva = BitConverter.ToUInt32(data, offset + 32);
            uint strongNameSize = BitConverter.ToUInt32(data, offset + 36);
            info.HasStrongName = strongNameRva != 0 && strongNameSize != 0;
        }

        private static int RvaToOffset(PeInfo info, uint rva)
        {
            foreach (PeSection section in info.Sections)
            {
                uint size = Math.Max(section.VirtualSize, section.RawSize);
                if (rva >= section.VirtualAddress && rva < section.VirtualAddress + size)
                {
                    return (int)(section.RawOffset + (rva - section.VirtualAddress));
                }
            }
            return -1;
        }

        private static string ReadName(byte[] data, int offset)
        {
            StringBuilder text = new StringBuilder(8);
            for (int i = 0; i < 8; i++)
            {
                byte value = data[offset + i];
                if (value == 0)
                {
                    break;
                }
                text.Append((char)value);
            }
            return text.ToString();
        }

        /// <summary>Shannon entropy in bits per byte (0..8).</summary>
        public static double Entropy(byte[] data, int offset, int length)
        {
            if (data == null || length <= 0 || offset < 0 || offset + length > data.Length)
            {
                return 0;
            }

            long[] counts = new long[256];
            for (int i = 0; i < length; i++)
            {
                counts[data[offset + i]]++;
            }

            double entropy = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0)
                {
                    continue;
                }
                double probability = (double)counts[i] / length;
                entropy -= probability * Math.Log(probability, 2);
            }
            return entropy;
        }

        public static string MachineName(ushort machine)
        {
            switch (machine)
            {
                case 0x014C: return "i386";
                case 0x8664: return "amd64";
                case 0x01C0: return "arm";
                case 0xAA64: return "arm64";
                case 0x0200: return "ia64";
                default: return "0x" + machine.ToString("X4", CultureInfo.InvariantCulture);
            }
        }
    }
}
