using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ILXP.Interop
{
    /// <summary>
    /// Managed wrapper around BeaEngine 4 (native x86 disassembler library).
    /// The library is optional: when it is missing or the process is 64-bit, all
    /// methods return null/false and the caller can fall back to another engine.
    /// </summary>
    internal static class BeaEngine
    {
        private const string LibraryName = "BeaEngine.dll";
        private const string Export = "_Disasm@4";

        private const int EipOffset = 0;
        private const int VirtualAddressOffset = 8;
        private const int SecurityBlockOffset = 16;
        private const int CompleteInstructionOffset = 20;
        private const int CompleteInstructionLength = 64;
        private const int ArchitectureOffset = 84;
        private const int OptionsOffset = 88;
        private const int StructureSize = 4096;

        private const uint Architecture32 = 0;
        private const uint Architecture64 = 64;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int DisasmDelegate(IntPtr disasm);

        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static bool available;
        private static IntPtr module;
        private static DisasmDelegate disasm;

        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return available;
            }
        }

        private static void EnsureInitialized()
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }
                initialized = true;

                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native", LibraryName);
                    if (File.Exists(path))
                    {
                        module = LoadLibrary(path);
                    }
                    if (module == IntPtr.Zero)
                    {
                        // Fall back to the default search order (application directory,
                        // PATH and the Windows directory).
                        module = LoadLibrary(LibraryName);
                    }
                    if (module == IntPtr.Zero)
                    {
                        Log.Info("BeaEngine: native library not found (this is expected on 64-bit hosts).");
                        return;
                    }

                    IntPtr address = GetProcAddress(module, Export);
                    if (address == IntPtr.Zero)
                    {
                        Log.Warn("BeaEngine: export '" + Export + "' was not found.");
                        return;
                    }

                    disasm = (DisasmDelegate)Marshal.GetDelegateForFunctionPointer(address, typeof(DisasmDelegate));
                    available = true;
                    Log.Info("BeaEngine: loaded from '" + path + "'.");
                }
                catch (Exception ex)
                {
                    Log.Warn("BeaEngine is not available.", ex);
                    available = false;
                }
            }
        }

        /// <summary>
        /// Disassembles a single instruction. Returns null when the engine cannot
        /// decode the bytes or is not available.
        /// </summary>
        public static string Disassemble(byte[] code, int offset, bool is64Bit, ulong virtualAddress)
        {
            EnsureInitialized();
            if (code == null || offset < 0 || offset >= code.Length || !available)
            {
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal(StructureSize);
            GCHandle pinned = default(GCHandle);
            try
            {
                pinned = GCHandle.Alloc(code, GCHandleType.Pinned);
                IntPtr codeAddress = IntPtr.Add(pinned.AddrOfPinnedObject(), offset);

                for (int i = 0; i < StructureSize; i++)
                {
                    Marshal.WriteByte(buffer, i, 0);
                }

                // BeaEngine reads the instruction bytes directly from EIP, so EIP must
                // point at real (pinned) memory, not at an offset inside the buffer.
                Marshal.WriteIntPtr(buffer, EipOffset, codeAddress);
                Marshal.WriteInt64(buffer, VirtualAddressOffset, unchecked((long)virtualAddress));
                Marshal.WriteInt32(buffer, SecurityBlockOffset, 0);
                Marshal.WriteInt32(buffer, ArchitectureOffset, unchecked((int)(is64Bit ? Architecture64 : Architecture32)));
                Marshal.WriteInt64(buffer, OptionsOffset, 0);

                int result = disasm(buffer);
                if (result <= 0)
                {
                    return null;
                }

                byte[] text = new byte[CompleteInstructionLength];
                Marshal.Copy(new IntPtr(buffer.ToInt64() + CompleteInstructionOffset), text, 0, text.Length);
                int length = Array.IndexOf(text, (byte)0);
                if (length < 0)
                {
                    length = text.Length;
                }
                return Encoding.ASCII.GetString(text, 0, length).Trim();
            }
            catch (Exception ex)
            {
                Log.Warn("BeaEngine failed to decode an instruction.", ex);
                return null;
            }
            finally
            {
                if (pinned.IsAllocated)
                {
                    pinned.Free();
                }
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>Disassembles a buffer instruction by instruction.</summary>
        public static string DisassembleBlock(byte[] code, int offset, int length, bool is64Bit, ulong virtualAddress)
        {
            if (code == null)
            {
                return string.Empty;
            }

            int end = Math.Min(code.Length, offset + Math.Max(length, 0));
            StringBuilder text = new StringBuilder();
            int position = offset;
            while (position < end)
            {
                string instruction = Disassemble(code, position, is64Bit, virtualAddress + (ulong)(position - offset));
                if (string.IsNullOrEmpty(instruction))
                {
                    text.AppendFormat("{0:x8}: db {1:x2}", position, code[position]).AppendLine();
                    position++;
                    continue;
                }

                text.AppendFormat("{0:x8}: {1}", position, instruction).AppendLine();
                position += InstructionLength(instruction);
            }
            return text.ToString();
        }

        /// <summary>
        /// Extracts the instruction length from BeaEngine's textual output, which ends
        /// with the encoded bytes in parentheses (for example "mov eax, ebx (8B C3)").
        /// </summary>
        private static int InstructionLength(string instruction)
        {
            int open = instruction.LastIndexOf('(');
            int close = instruction.LastIndexOf(')');
            if (open < 0 || close <= open)
            {
                return 1;
            }

            string bytes = instruction.Substring(open + 1, close - open - 1);
            int count = 0;
            foreach (string part in bytes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length == 2)
                {
                    count++;
                }
            }
            return Math.Max(count, 1);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
    }
}
