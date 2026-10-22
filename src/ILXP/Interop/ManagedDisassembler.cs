using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ILXP.Interop
{
    /// <summary>
    /// Managed x86/x64 instruction length decoder and formatter. This is the in-house
    /// replacement for BeaEngine: it does not need a native DLL, works on 64-bit hosts
    /// and never crashes on malformed input (unknown bytes are reported as <c>db</c>).
    ///
    /// The decoder resolves prefixes, the ModRM/SIB bytes and the immediate size, which
    /// is everything required to walk a code stream reliably. Mnemonics are provided for
    /// the common integer/control-flow subset; rarer opcodes are shown by opcode number
    /// together with the decoded operands, so the listing stays useful for analysis.
    /// </summary>
    internal static class ManagedDisassembler
    {
        private sealed class Instruction
        {
            public int Length;
            public string Mnemonic;
            public string Operands;
            public bool IsInvalid;
        }

        private static readonly string[] Registers32 =
        {
            "eax", "ecx", "edx", "ebx", "esp", "ebp", "esi", "edi"
        };

        private static readonly string[] Registers16 =
        {
            "ax", "cx", "dx", "bx", "sp", "bp", "si", "di"
        };

        private static readonly string[] Registers8 =
        {
            "al", "cl", "dl", "bl", "ah", "ch", "dh", "bh"
        };

        private static readonly string[] Registers64 =
        {
            "rax", "rcx", "rdx", "rbx", "rsp", "rbp", "rsi", "rdi",
            "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15"
        };

        /// <summary>Formats a block of bytes as a disassembly listing.</summary>
        public static string DisassembleBlock(byte[] code, int offset, int length, bool is64Bit, ulong virtualAddress)
        {
            if (code == null)
            {
                return string.Empty;
            }

            int start = Math.Max(0, offset);
            int end = Math.Min(code.Length, start + Math.Max(0, length));
            StringBuilder text = new StringBuilder();
            int position = start;

            while (position < end)
            {
                int consumed;
                string line = Decode(code, position, end, is64Bit, virtualAddress + (ulong)(position - start), out consumed);
                text.Append((virtualAddress + (ulong)(position - start)).ToString("x8")).Append(": ");
                text.Append(line).AppendLine();
                position += consumed > 0 ? consumed : 1;
            }
            return text.ToString();
        }

        /// <summary>Decodes one instruction, returning its textual form.</summary>
        public static string Decode(byte[] code, int offset, int limit, bool is64Bit, ulong address, out int length)
        {
            Instruction instruction = DecodeInternal(code, offset, limit, is64Bit, address);
            length = instruction.Length;
            if (instruction.IsInvalid)
            {
                return "db " + code[offset].ToString("x2");
            }
            return string.IsNullOrEmpty(instruction.Operands)
                ? instruction.Mnemonic
                : instruction.Mnemonic + " " + instruction.Operands;
        }

        private static Instruction DecodeInternal(byte[] code, int offset, int limit, bool is64Bit, ulong address)
        {
            Instruction result = new Instruction { Length = 1 };
            int position = offset;

            bool operand16 = false;
            bool address32 = is64Bit;   // 64-bit mode defaults to 64-bit addressing
            bool rexW = false;
            int rexR = 0, rexX = 0, rexB = 0;
            bool lockPrefix = false;
            bool repPrefix = false;
            string segmentPrefix = null;

            // ---- prefixes -------------------------------------------------------
            while (position < limit)
            {
                byte prefix = code[position];
                if (prefix == 0x66) { operand16 = true; position++; continue; }
                if (prefix == 0x67) { address32 = !is64Bit; position++; continue; }
                if (prefix == 0xF0) { lockPrefix = true; position++; continue; }
                if (prefix == 0xF2 || prefix == 0xF3) { repPrefix = true; position++; continue; }
                if (prefix == 0x2E) { segmentPrefix = "cs"; position++; continue; }
                if (prefix == 0x36) { segmentPrefix = "ss"; position++; continue; }
                if (prefix == 0x3E) { segmentPrefix = "ds"; position++; continue; }
                if (prefix == 0x26) { segmentPrefix = "es"; position++; continue; }
                if (prefix == 0x64) { segmentPrefix = "fs"; position++; continue; }
                if (prefix == 0x65) { segmentPrefix = "gs"; position++; continue; }
                if (is64Bit && prefix >= 0x40 && prefix <= 0x4F)
                {
                    rexW = (prefix & 0x08) != 0;
                    rexR = (prefix & 0x04) != 0 ? 1 : 0;
                    rexX = (prefix & 0x02) != 0 ? 1 : 0;
                    rexB = (prefix & 0x01) != 0 ? 1 : 0;
                    position++;
                    continue;
                }
                break;
            }

            if (position >= limit)
            {
                result.IsInvalid = true;
                result.Length = Math.Max(1, position - offset);
                return result;
            }

            byte opcode = code[position++];
            string mnemonic;
            string operands = string.Empty;
            int immediateSize = 0;
            bool hasModRm = false;
            bool isGroup1Arithmetic = false;
            int arithmeticIndex = 0;

            // ---- one-byte opcode map -------------------------------------------
            if (opcode >= 0x40 && opcode <= 0x4F && !is64Bit)
            {
                mnemonic = "inc";
                operands = Registers32[opcode & 7];
            }
            else if (opcode >= 0x48 && opcode <= 0x4F && !is64Bit)
            {
                mnemonic = "dec";
                operands = Registers32[opcode & 7];
            }
            else if (opcode >= 0x50 && opcode <= 0x57)
            {
                mnemonic = "push";
                operands = Register(code, opcode & 7, rexB, is64Bit, operand16, rexW);
            }
            else if (opcode >= 0x58 && opcode <= 0x5F)
            {
                mnemonic = "pop";
                operands = Register(code, opcode & 7, rexB, is64Bit, operand16, rexW);
            }
            else if (opcode >= 0x70 && opcode <= 0x7F)
            {
                mnemonic = ConditionalMnemonic(opcode & 0x0F);
                immediateSize = 1;
                operands = string.Empty;
            }
            else if (opcode >= 0x90 && opcode <= 0x97)
            {
                mnemonic = opcode == 0x90 ? "nop" : "xchg";
                operands = opcode == 0x90 ? string.Empty : Register(code, opcode & 7, rexB, is64Bit, operand16, rexW);
            }
            else if (opcode >= 0xB0 && opcode <= 0xB7)
            {
                mnemonic = "mov";
                operands = Register8(code, opcode & 7, rexB);
                immediateSize = 1;
            }
            else if (opcode >= 0xB8 && opcode <= 0xBF)
            {
                mnemonic = "mov";
                operands = Register(code, opcode & 7, rexB, is64Bit, operand16, rexW);
                immediateSize = rexW ? 8 : (operand16 ? 2 : 4);
            }
            else
            {
                switch (opcode)
                {
                    case 0x00: case 0x01: case 0x02: case 0x03:
                    case 0x08: case 0x09: case 0x0A: case 0x0B:
                    case 0x10: case 0x11: case 0x12: case 0x13:
                    case 0x18: case 0x19: case 0x1A: case 0x1B:
                    case 0x20: case 0x21: case 0x22: case 0x23:
                    case 0x28: case 0x29: case 0x2A: case 0x2B:
                    case 0x30: case 0x31: case 0x32: case 0x33:
                    case 0x38: case 0x39: case 0x3A: case 0x3B:
                        isGroup1Arithmetic = true;
                        arithmeticIndex = opcode >> 3;
                        mnemonic = ArithmeticMnemonic(arithmeticIndex);
                        hasModRm = true;
                        break;

                    case 0x04: case 0x0C: case 0x14: case 0x1C:
                    case 0x24: case 0x2C: case 0x34: case 0x3C:
                        mnemonic = ArithmeticMnemonic(opcode >> 3);
                        operands = "al";
                        immediateSize = 1;
                        break;

                    case 0x05: case 0x0D: case 0x15: case 0x1D:
                    case 0x25: case 0x2D: case 0x35: case 0x3D:
                        mnemonic = ArithmeticMnemonic(opcode >> 3);
                        operands = operand16 ? "ax" : "eax";
                        immediateSize = operand16 ? 2 : 4;
                        break;

                    case 0x06: mnemonic = "push"; operands = "es"; break;
                    case 0x07: mnemonic = "pop"; operands = "es"; break;
                    case 0x0E: mnemonic = "push"; operands = "cs"; break;
                    case 0x16: mnemonic = "push"; operands = "ss"; break;
                    case 0x17: mnemonic = "pop"; operands = "ss"; break;
                    case 0x1E: mnemonic = "push"; operands = "ds"; break;
                    case 0x1F: mnemonic = "pop"; operands = "ds"; break;
                    case 0x27: mnemonic = "daa"; break;
                    case 0x2F: mnemonic = "das"; break;
                    case 0x37: mnemonic = "aaa"; break;
                    case 0x3F: mnemonic = "aas"; break;

                    case 0x60: mnemonic = is64Bit ? "db" : "pushad"; break;
                    case 0x61: mnemonic = is64Bit ? "db" : "popad"; break;
                    case 0x62: mnemonic = is64Bit ? "db" : "bound"; hasModRm = true; break;
                    case 0x63: mnemonic = is64Bit ? "movsxd" : "arpl"; hasModRm = true; break;
                    case 0x68: mnemonic = "push"; immediateSize = operand16 ? 2 : 4; break;
                    case 0x69: mnemonic = "imul"; hasModRm = true; immediateSize = operand16 ? 2 : 4; break;
                    case 0x6A: mnemonic = "push"; immediateSize = 1; break;
                    case 0x6B: mnemonic = "imul"; hasModRm = true; immediateSize = 1; break;
                    case 0x6C: mnemonic = "insb"; break;
                    case 0x6D: mnemonic = "insd"; break;
                    case 0x6E: mnemonic = "outsb"; break;
                    case 0x6F: mnemonic = "outsd"; break;

                    case 0x80: mnemonic = "group1"; hasModRm = true; immediateSize = 1; isGroup1Arithmetic = true; break;
                    case 0x81: mnemonic = "group1"; hasModRm = true; immediateSize = operand16 ? 2 : 4; isGroup1Arithmetic = true; break;
                    case 0x82: mnemonic = "group1"; hasModRm = true; immediateSize = 1; isGroup1Arithmetic = true; break;
                    case 0x83: mnemonic = "group1"; hasModRm = true; immediateSize = 1; isGroup1Arithmetic = true; break;

                    case 0x84: mnemonic = "test"; hasModRm = true; break;
                    case 0x85: mnemonic = "test"; hasModRm = true; break;
                    case 0x86: mnemonic = "xchg"; hasModRm = true; break;
                    case 0x87: mnemonic = "xchg"; hasModRm = true; break;
                    case 0x88: mnemonic = "mov"; hasModRm = true; break;
                    case 0x89: mnemonic = "mov"; hasModRm = true; break;
                    case 0x8A: mnemonic = "mov"; hasModRm = true; break;
                    case 0x8B: mnemonic = "mov"; hasModRm = true; break;
                    case 0x8C: mnemonic = "mov"; hasModRm = true; break;
                    case 0x8D: mnemonic = "lea"; hasModRm = true; break;
                    case 0x8E: mnemonic = "mov"; hasModRm = true; break;
                    case 0x8F: mnemonic = "pop"; hasModRm = true; break;

                    case 0x98: mnemonic = operand16 ? "cbw" : (rexW ? "cdqe" : "cwde"); break;
                    case 0x99: mnemonic = operand16 ? "cwd" : (rexW ? "cqo" : "cdq"); break;
                    case 0x9A: mnemonic = "callf"; immediateSize = operand16 ? 5 : 7; break;
                    case 0x9B: mnemonic = "wait"; break;
                    case 0x9C: mnemonic = is64Bit ? "pushfq" : (operand16 ? "pushf" : "pushfd"); break;
                    case 0x9D: mnemonic = is64Bit ? "popfq" : (operand16 ? "popf" : "popfd"); break;
                    case 0x9E: mnemonic = "sahf"; break;
                    case 0x9F: mnemonic = "lahf"; break;

                    case 0xA0: case 0xA1: case 0xA2: case 0xA3:
                        mnemonic = (opcode == 0xA0 || opcode == 0xA1) ? "mov" : "mov";
                        immediateSize = address32 ? 8 : 4;
                        break;
                    case 0xA4: mnemonic = "movsb"; break;
                    case 0xA5: mnemonic = operand16 ? "movsw" : "movsd"; break;
                    case 0xA6: mnemonic = "cmpsb"; break;
                    case 0xA7: mnemonic = operand16 ? "cmpsw" : "cmpsd"; break;
                    case 0xA8: mnemonic = "test"; operands = "al"; immediateSize = 1; break;
                    case 0xA9: mnemonic = "test"; operands = operand16 ? "ax" : "eax"; immediateSize = operand16 ? 2 : 4; break;
                    case 0xAA: mnemonic = "stosb"; break;
                    case 0xAB: mnemonic = operand16 ? "stosw" : "stosd"; break;
                    case 0xAC: mnemonic = "lodsb"; break;
                    case 0xAD: mnemonic = operand16 ? "lodsw" : "lodsd"; break;
                    case 0xAE: mnemonic = "scasb"; break;
                    case 0xAF: mnemonic = operand16 ? "scasw" : "scasd"; break;

                    case 0xC0: mnemonic = "group2"; hasModRm = true; immediateSize = 1; break;
                    case 0xC1: mnemonic = "group2"; hasModRm = true; immediateSize = 1; break;
                    case 0xC2: mnemonic = "ret"; immediateSize = 2; break;
                    case 0xC3: mnemonic = "ret"; break;
                    case 0xC4: mnemonic = is64Bit ? "vex3" : "les"; hasModRm = true; break;
                    case 0xC5: mnemonic = is64Bit ? "vex2" : "lds"; hasModRm = true; break;
                    case 0xC6: mnemonic = "mov"; hasModRm = true; immediateSize = 1; break;
                    case 0xC7: mnemonic = "mov"; hasModRm = true; immediateSize = operand16 ? 2 : 4; break;
                    case 0xC8: mnemonic = "enter"; immediateSize = 3; break;
                    case 0xC9: mnemonic = "leave"; break;
                    case 0xCA: mnemonic = "retf"; immediateSize = 2; break;
                    case 0xCB: mnemonic = "retf"; break;
                    case 0xCC: mnemonic = "int3"; break;
                    case 0xCD: mnemonic = "int"; immediateSize = 1; break;
                    case 0xCE: mnemonic = "into"; break;
                    case 0xCF: mnemonic = is64Bit ? "iretq" : (operand16 ? "iretw" : "iretd"); break;

                    case 0xD0: mnemonic = "group2"; hasModRm = true; break;
                    case 0xD1: mnemonic = "group2"; hasModRm = true; break;
                    case 0xD2: mnemonic = "group2"; hasModRm = true; break;
                    case 0xD3: mnemonic = "group2"; hasModRm = true; break;
                    case 0xD4: mnemonic = "aam"; immediateSize = 1; break;
                    case 0xD5: mnemonic = "aad"; immediateSize = 1; break;
                    case 0xD6: mnemonic = "salc"; break;
                    case 0xD7: mnemonic = "xlatb"; break;
                    case 0xD8: case 0xD9: case 0xDA: case 0xDB:
                    case 0xDC: case 0xDD: case 0xDE: case 0xDF:
                        mnemonic = "fpu"; hasModRm = true;
                        break;

                    case 0xE0: mnemonic = "loopne"; immediateSize = 1; break;
                    case 0xE1: mnemonic = "loope"; immediateSize = 1; break;
                    case 0xE2: mnemonic = "loop"; immediateSize = 1; break;
                    case 0xE3: mnemonic = "jecxz"; immediateSize = 1; break;
                    case 0xE4: mnemonic = "in"; operands = "al"; immediateSize = 1; break;
                    case 0xE5: mnemonic = "in"; operands = "eax"; immediateSize = 1; break;
                    case 0xE6: mnemonic = "out"; operands = "al"; immediateSize = 1; break;
                    case 0xE7: mnemonic = "out"; operands = "eax"; immediateSize = 1; break;
                    case 0xE8: mnemonic = "call"; immediateSize = operand16 ? 2 : 4; break;
                    case 0xE9: mnemonic = "jmp"; immediateSize = operand16 ? 2 : 4; break;
                    case 0xEA: mnemonic = "jmpf"; immediateSize = operand16 ? 4 : 6; break;
                    case 0xEB: mnemonic = "jmp"; immediateSize = 1; break;
                    case 0xEC: mnemonic = "in"; operands = "al, dx"; break;
                    case 0xED: mnemonic = "in"; operands = "eax, dx"; break;
                    case 0xEE: mnemonic = "out"; operands = "dx, al"; break;
                    case 0xEF: mnemonic = "out"; operands = "dx, eax"; break;

                    case 0xF1: mnemonic = "int1"; break;
                    case 0xF4: mnemonic = "hlt"; break;
                    case 0xF5: mnemonic = "cmc"; break;
                    case 0xF6: mnemonic = "group3"; hasModRm = true; break;
                    case 0xF7: mnemonic = "group3"; hasModRm = true; break;
                    case 0xF8: mnemonic = "clc"; break;
                    case 0xF9: mnemonic = "stc"; break;
                    case 0xFA: mnemonic = "cli"; break;
                    case 0xFB: mnemonic = "sti"; break;
                    case 0xFC: mnemonic = "cld"; break;
                    case 0xFD: mnemonic = "std"; break;
                    case 0xFE: mnemonic = "group4"; hasModRm = true; break;
                    case 0xFF: mnemonic = "group5"; hasModRm = true; break;

                    case 0x0F:
                        return DecodeTwoByte(code, position, limit, is64Bit, address, offset, result, operand16, rexW, rexR, rexB);

                    case 0x26: case 0x2E: case 0x36: case 0x3E:
                    case 0x64: case 0x65: case 0x66: case 0x67:
                    case 0xF0: case 0xF2: case 0xF3:
                        // A second prefix where an opcode was expected.
                        result.IsInvalid = true;
                        result.Length = Math.Max(1, position - offset);
                        return result;

                    default:
                        result.IsInvalid = true;
                        result.Length = Math.Max(1, position - offset);
                        return result;
                }
            }

            // ---- ModRM, SIB and operands ---------------------------------------
            if (hasModRm)
            {
                if (position >= limit)
                {
                    result.IsInvalid = true;
                    result.Length = Math.Max(1, position - offset);
                    return result;
                }

                byte modRm = code[position++];
                int mod = (modRm >> 6) & 3;
                int reg = ((modRm >> 3) & 7) + rexR * 8;
                int rm = (modRm & 7) + rexB * 8;

                string rmText;
                int sibDisplacement = 0;
                if (mod == 3)
                {
                    rmText = is64Bit ? Registers64[rm & 15] : (operand16 ? Registers16[rm & 7] : Registers32[rm & 7]);
                }
                else
                {
                    bool ripRelative = false;
                    string baseText = null;
                    string indexText = null;
                    int scale = 1;

                    if ((rm & 7) == 4)
                    {
                        // SIB byte.
                        if (position >= limit)
                        {
                            result.IsInvalid = true;
                            result.Length = Math.Max(1, position - offset);
                            return result;
                        }
                        byte sib = code[position++];
                        int sibScale = 1 << ((sib >> 6) & 3);
                        int sibIndex = ((sib >> 3) & 7) + rexX * 8;
                        int sibBase = (sib & 7) + rexB * 8;
                        scale = sibScale;

                        if (sibIndex != 4)
                        {
                            indexText = (is64Bit ? Registers64[sibIndex & 15] : Registers32[sibIndex & 7]) + "*" + scale;
                        }

                        if ((sib & 7) == 5 && mod == 0)
                        {
                            baseText = null;
                            sibDisplacement = 4;
                        }
                        else
                        {
                            baseText = is64Bit ? Registers64[sibBase & 15] : Registers32[sibBase & 7];
                        }
                    }
                    else if ((rm & 7) == 5 && mod == 0)
                    {
                        ripRelative = is64Bit;
                        sibDisplacement = 4;
                    }
                    else
                    {
                        baseText = is64Bit ? Registers64[rm & 15] : Registers32[rm & 7];
                    }

                    if (mod == 1)
                    {
                        sibDisplacement = 1;
                    }
                    else if (mod == 2)
                    {
                        sibDisplacement = 4;
                    }

                    int displacement = 0;
                    if (sibDisplacement > 0)
                    {
                        if (position + sibDisplacement > limit)
                        {
                            result.IsInvalid = true;
                            result.Length = Math.Max(1, position - offset);
                            return result;
                        }

                        if (sibDisplacement == 1)
                        {
                            displacement = (sbyte)code[position];
                        }
                        else if (sibDisplacement == 4)
                        {
                            displacement = BitConverter.ToInt32(code, position);
                        }
                        else
                        {
                            displacement = BitConverter.ToUInt16(code, position);
                        }
                        position += sibDisplacement;
                    }

                    if (ripRelative)
                    {
                        rmText = "rip" + FormatDisplacement(displacement);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(is64Bit ? "qword ptr " : (operand16 ? "word ptr " : "dword ptr "));
                        if (!string.IsNullOrEmpty(segmentPrefix))
                        {
                            sb.Append(segmentPrefix).Append(':');
                        }
                        sb.Append('[');
                        bool hasTerm = false;
                        if (!string.IsNullOrEmpty(baseText))
                        {
                            sb.Append(baseText);
                            hasTerm = true;
                        }
                        if (!string.IsNullOrEmpty(indexText))
                        {
                            if (hasTerm)
                            {
                                sb.Append('+');
                            }
                            sb.Append(indexText);
                            hasTerm = true;
                        }
                        if (sibDisplacement > 0 && displacement != 0)
                        {
                            if (hasTerm && displacement > 0)
                            {
                                sb.Append('+');
                            }
                            sb.Append(displacement.ToString(CultureInfo.InvariantCulture));
                        }
                        else if (!hasTerm)
                        {
                            sb.Append('0');
                        }
                        sb.Append(']');
                        rmText = sb.ToString();
                    }
                }

                string regText = is64Bit ? Registers64[reg & 15] : (operand16 ? Registers16[reg & 7] : Registers32[reg & 7]);

                if (isGroup1Arithmetic)
                {
                    mnemonic = ArithmeticMnemonic((modRm >> 3) & 7);
                    operands = ((modRm >> 3) & 7) < 2 ? regText + ", " + rmText : rmText + ", " + regText;
                }
                else if (opcode == 0x8D)
                {
                    operands = regText + ", " + rmText;
                }
                else if (opcode == 0x8F)
                {
                    operands = rmText;
                }
                else if (opcode == 0xFE || opcode == 0xFF)
                {
                    int groupIndex = (modRm >> 3) & 7;
                    if (opcode == 0xFE)
                    {
                        mnemonic = groupIndex == 0 ? "inc" : (groupIndex == 1 ? "dec" : "group4");
                        operands = rmText;
                    }
                    else
                    {
                        switch (groupIndex)
                        {
                            case 0: mnemonic = "inc"; operands = rmText; break;
                            case 1: mnemonic = "dec"; operands = rmText; break;
                            case 2: mnemonic = "call"; operands = rmText; break;
                            case 3: mnemonic = "callf"; operands = rmText; break;
                            case 4: mnemonic = "jmp"; operands = rmText; break;
                            case 5: mnemonic = "jmpf"; operands = rmText; break;
                            case 6: mnemonic = "push"; operands = rmText; break;
                            default: mnemonic = "invalid"; operands = rmText; break;
                        }
                    }
                }
                else if (opcode == 0xF6 || opcode == 0xF7)
                {
                    int groupIndex = (modRm >> 3) & 7;
                    switch (groupIndex)
                    {
                        case 0: mnemonic = "test"; operands = rmText; immediateSize = opcode == 0xF6 ? 1 : (operand16 ? 2 : 4); break;
                        case 2: mnemonic = "not"; operands = rmText; break;
                        case 3: mnemonic = "neg"; operands = rmText; break;
                        case 4: mnemonic = "mul"; operands = rmText; break;
                        case 5: mnemonic = "imul"; operands = rmText; break;
                        case 6: mnemonic = "div"; operands = rmText; break;
                        case 7: mnemonic = "idiv"; operands = rmText; break;
                        default: mnemonic = "test"; operands = rmText; break;
                    }
                }
                else if (opcode == 0xC0 || opcode == 0xC1 || opcode == 0xD0 || opcode == 0xD1 || opcode == 0xD2 || opcode == 0xD3)
                {
                    int groupIndex = (modRm >> 3) & 7;
                    string[] shiftNames = { "rol", "ror", "rcl", "rcr", "shl", "shr", "sal", "sar" };
                    mnemonic = shiftNames[groupIndex];
                    operands = rmText;
                }
                else if (opcode == 0x88 || opcode == 0x89 || opcode == 0x8A || opcode == 0x8B ||
                         opcode == 0x84 || opcode == 0x85 || opcode == 0x86 || opcode == 0x87)
                {
                    bool registerDestination = (opcode & 0x02) != 0;
                    operands = registerDestination ? regText + ", " + rmText : rmText + ", " + regText;
                }
                else
                {
                    operands = regText + ", " + rmText;
                }
            }

            // ---- immediates -----------------------------------------------------
            if (immediateSize > 0)
            {
                if (position + immediateSize > limit)
                {
                    result.IsInvalid = true;
                    result.Length = Math.Max(1, position - offset);
                    return result;
                }

                long immediate;
                if (immediateSize == 1)
                {
                    immediate = (sbyte)code[position];
                }
                else if (immediateSize == 2)
                {
                    immediate = BitConverter.ToInt16(code, position);
                }
                else if (immediateSize == 4)
                {
                    immediate = BitConverter.ToInt32(code, position);
                }
                else
                {
                    immediate = BitConverter.ToInt64(code, position);
                }
                position += immediateSize;

                string immediateText = FormatImmediate(immediate, immediateSize);
                if (opcode >= 0x70 && opcode <= 0x7F || opcode == 0xE0 || opcode == 0xE1 || opcode == 0xE2 || opcode == 0xE3 || opcode == 0xEB || opcode == 0xE9 || opcode == 0xE8)
                {
                    // Branch targets are shown as absolute addresses.
                    ulong target = address + (ulong)(position - offset) + (ulong)immediate;
                    operands = (operands.Length > 0 ? operands + ", " : string.Empty) + "0x" + target.ToString("x");
                }
                else
                {
                    operands = (operands.Length > 0 ? operands + ", " : string.Empty) + immediateText;
                }
            }

            if (lockPrefix)
            {
                mnemonic = "lock " + mnemonic;
            }
            else if (repPrefix)
            {
                mnemonic = "rep " + mnemonic;
            }

            result.Mnemonic = mnemonic;
            result.Operands = operands;
            result.Length = Math.Max(1, position - offset);
            return result;
        }

        private static Instruction DecodeTwoByte(byte[] code, int position, int limit, bool is64Bit, ulong address, int offset, Instruction result, bool operand16, bool rexW, int rexR, int rexB)
        {
            if (position >= limit)
            {
                result.IsInvalid = true;
                result.Length = Math.Max(1, position - offset);
                return result;
            }

            byte opcode = code[position++];
            string mnemonic;
            int immediateSize = 0;
            bool hasModRm = true;

            if (opcode >= 0x80 && opcode <= 0x8F)
            {
                mnemonic = "jcc";
                immediateSize = operand16 ? 2 : 4;
                hasModRm = false;
            }
            else if (opcode >= 0x90 && opcode <= 0x9F)
            {
                mnemonic = "setcc";
            }
            else if (opcode >= 0x40 && opcode <= 0x4F)
            {
                mnemonic = "cmovcc";
            }
            else if (opcode >= 0xC8 && opcode <= 0xCF)
            {
                mnemonic = "bswap";
                hasModRm = false;
                int reg = opcode & 7;
                result.Mnemonic = mnemonic;
                result.Operands = is64Bit ? Registers64[reg + rexB * 8] : Registers32[reg];
                result.Length = Math.Max(1, position - offset);
                return result;
            }
            else
            {
                switch (opcode)
                {
                    case 0x00: mnemonic = "group6"; break;
                    case 0x01: mnemonic = "group7"; break;
                    case 0x02: mnemonic = "lar"; break;
                    case 0x03: mnemonic = "lsl"; break;
                    case 0x05: mnemonic = "syscall"; hasModRm = false; break;
                    case 0x06: mnemonic = "clts"; hasModRm = false; break;
                    case 0x07: mnemonic = "sysret"; hasModRm = false; break;
                    case 0x08: mnemonic = "invd"; hasModRm = false; break;
                    case 0x09: mnemonic = "wbinvd"; hasModRm = false; break;
                    case 0x0B: mnemonic = "ud2"; hasModRm = false; break;
                    case 0x0D: mnemonic = "prefetch"; break;
                    case 0x0E: mnemonic = "femms"; hasModRm = false; break;
                    case 0x0F: mnemonic = "3dnow"; break;
                    case 0x10: case 0x11: case 0x12: case 0x13:
                    case 0x14: case 0x15: case 0x16: case 0x17:
                    case 0x28: case 0x29: case 0x2A: case 0x2B:
                    case 0x2C: case 0x2D: case 0x2E: case 0x2F:
                        mnemonic = "sse"; break;
                    case 0x18: mnemonic = "prefetch"; break;
                    case 0x1F: mnemonic = "nop"; break;
                    case 0x20: mnemonic = "mov"; break;
                    case 0x21: mnemonic = "mov"; break;
                    case 0x22: mnemonic = "mov"; break;
                    case 0x23: mnemonic = "mov"; break;
                    case 0x30: mnemonic = "wrmsr"; hasModRm = false; break;
                    case 0x31: mnemonic = "rdtsc"; hasModRm = false; break;
                    case 0x32: mnemonic = "rdmsr"; hasModRm = false; break;
                    case 0x33: mnemonic = "rdpmc"; hasModRm = false; break;
                    case 0x34: mnemonic = "sysenter"; hasModRm = false; break;
                    case 0x35: mnemonic = "sysexit"; hasModRm = false; break;
                    case 0x37: mnemonic = "getsec"; hasModRm = false; break;
                    case 0x38: mnemonic = "map38"; break;
                    case 0x3A: mnemonic = "map3a"; immediateSize = 1; break;
                    case 0x77: mnemonic = "emms"; hasModRm = false; break;
                    case 0xA0: mnemonic = "push"; hasModRm = false; break;
                    case 0xA1: mnemonic = "pop"; hasModRm = false; break;
                    case 0xA2: mnemonic = "cpuid"; hasModRm = false; break;
                    case 0xA3: mnemonic = "bt"; break;
                    case 0xA4: mnemonic = "shld"; immediateSize = 1; break;
                    case 0xA5: mnemonic = "shld"; break;
                    case 0xA8: mnemonic = "push"; hasModRm = false; break;
                    case 0xA9: mnemonic = "pop"; hasModRm = false; break;
                    case 0xAA: mnemonic = "rsm"; hasModRm = false; break;
                    case 0xAB: mnemonic = "bts"; break;
                    case 0xAC: mnemonic = "shrd"; immediateSize = 1; break;
                    case 0xAD: mnemonic = "shrd"; break;
                    case 0xAE: mnemonic = "group15"; break;
                    case 0xAF: mnemonic = "imul"; break;
                    case 0xB0: mnemonic = "cmpxchg"; break;
                    case 0xB1: mnemonic = "cmpxchg"; break;
                    case 0xB2: mnemonic = "lss"; break;
                    case 0xB3: mnemonic = "btr"; break;
                    case 0xB4: mnemonic = "lfs"; break;
                    case 0xB5: mnemonic = "lgs"; break;
                    case 0xB6: mnemonic = "movzx"; break;
                    case 0xB7: mnemonic = "movzx"; break;
                    case 0xB8: mnemonic = "popcnt"; break;
                    case 0xB9: mnemonic = "ud1"; break;
                    case 0xBA: mnemonic = "group8"; immediateSize = 1; break;
                    case 0xBB: mnemonic = "btc"; break;
                    case 0xBC: mnemonic = "bsf"; break;
                    case 0xBD: mnemonic = "bsr"; break;
                    case 0xBE: mnemonic = "movsx"; break;
                    case 0xBF: mnemonic = "movsx"; break;
                    case 0xC0: mnemonic = "xadd"; break;
                    case 0xC1: mnemonic = "xadd"; break;
                    case 0xC7: mnemonic = "group9"; break;
                    case 0xC8: case 0xC9: case 0xCA: case 0xCB:
                    case 0xCC: case 0xCD: case 0xCE: case 0xCF:
                        mnemonic = "bswap"; hasModRm = false; break;
                    default:
                        if (opcode >= 0x60 && opcode <= 0x6F) { mnemonic = "mmx"; }
                        else if (opcode >= 0x70 && opcode <= 0x7F) { mnemonic = "mmx"; immediateSize = 1; }
                        else if (opcode >= 0xD0 && opcode <= 0xFF) { mnemonic = "sse2"; }
                        else if (opcode >= 0x50 && opcode <= 0x5F) { mnemonic = "sse"; }
                        else if (opcode >= 0x10 && opcode <= 0x1F) { mnemonic = "sse"; }
                        else if (opcode >= 0x80 && opcode <= 0x8F) { mnemonic = "jcc"; }
                        else { mnemonic = "op" + opcode.ToString("x2"); }
                        break;
                }
            }

            string operands = string.Empty;
            if (hasModRm && position < limit)
            {
                byte modRm = code[position++];
                int mod = (modRm >> 6) & 3;
                int reg = ((modRm >> 3) & 7) + rexR * 8;
                int rm = (modRm & 7) + rexB * 8;

                string regText = is64Bit ? Registers64[reg & 15] : (operand16 ? Registers16[reg & 7] : Registers32[reg & 7]);
                string rmText;
                if (mod == 3)
                {
                    rmText = is64Bit ? Registers64[rm & 15] : (operand16 ? Registers16[rm & 7] : Registers32[rm & 7]);
                }
                else
                {
                    int displacementSize = mod == 1 ? 1 : (mod == 2 ? 4 : 0);
                    if ((rm & 7) == 4 && position < limit)
                    {
                        position++; // SIB
                        if (mod == 0 && (code[position - 1] & 7) == 5)
                        {
                            displacementSize = 4;
                        }
                    }
                    else if (mod == 0 && (rm & 7) == 5)
                    {
                        displacementSize = 4;
                    }

                    long displacement = 0;
                    if (displacementSize > 0 && position + displacementSize <= limit)
                    {
                        if (displacementSize == 1)
                        {
                            displacement = (sbyte)code[position];
                        }
                        else
                        {
                            displacement = BitConverter.ToInt32(code, position);
                        }
                        position += displacementSize;
                    }
                    rmText = "[mem" + FormatDisplacement((int)displacement) + "]";
                }
                operands = regText + ", " + rmText;
            }

            if (immediateSize > 0 && position + immediateSize <= limit)
            {
                long immediate = immediateSize == 1 ? (sbyte)code[position] : BitConverter.ToInt32(code, position);
                position += immediateSize;
                if (mnemonic == "jcc")
                {
                    ulong target = address + (ulong)(position - offset) + (ulong)immediate;
                    operands = "0x" + target.ToString("x");
                }
                else
                {
                    operands = (operands.Length > 0 ? operands + ", " : string.Empty) + FormatImmediate(immediate, immediateSize);
                }
            }

            result.Mnemonic = mnemonic;
            result.Operands = operands;
            result.Length = Math.Max(1, position - offset);
            return result;
        }

        private static string Register(byte[] code, int index, int rex, bool is64Bit, bool operand16, bool rexW)
        {
            if (is64Bit)
            {
                return Registers64[(index + rex * 8) & 15];
            }
            return operand16 ? Registers16[index & 7] : Registers32[index & 7];
        }

        private static string Register8(byte[] code, int index, int rex)
        {
            return Registers8[index & 7];
        }

        private static string ArithmeticMnemonic(int index)
        {
            switch (index & 7)
            {
                case 0: return "add";
                case 1: return "or";
                case 2: return "adc";
                case 3: return "sbb";
                case 4: return "and";
                case 5: return "sub";
                case 6: return "xor";
                default: return "cmp";
            }
        }

        private static string ConditionalMnemonic(int condition)
        {
            switch (condition & 0x0F)
            {
                case 0x0: return "jo";
                case 0x1: return "jno";
                case 0x2: return "jb";
                case 0x3: return "jae";
                case 0x4: return "je";
                case 0x5: return "jne";
                case 0x6: return "jbe";
                case 0x7: return "ja";
                case 0x8: return "js";
                case 0x9: return "jns";
                case 0xA: return "jp";
                case 0xB: return "jnp";
                case 0xC: return "jl";
                case 0xD: return "jge";
                case 0xE: return "jle";
                default: return "jg";
            }
        }

        private static string FormatDisplacement(int displacement)
        {
            if (displacement == 0)
            {
                return string.Empty;
            }
            return displacement > 0
                ? "+" + displacement.ToString("x")
                : "-" + (-displacement).ToString("x");
        }

        private static string FormatImmediate(long value, int size)
        {
            if (size == 1 && (value < 0 || value > 9))
            {
                return "0x" + (value & 0xFF).ToString("x2");
            }
            if (value < 0)
            {
                return "-0x" + (-value).ToString("x");
            }
            return value <= 9 ? value.ToString(CultureInfo.InvariantCulture) : "0x" + value.ToString("x");
        }
    }
}
