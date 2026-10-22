using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;

namespace Microsoft.Cci.Pdb;

internal class PdbFile
{
	private PdbFile()
	{
	}

	private static void LoadGuidStream(BitAccess bits, out Guid doctype, out Guid language, out Guid vendor)
	{
		bits.ReadGuid(out language);
		bits.ReadGuid(out vendor);
		bits.ReadGuid(out doctype);
	}

	private static Dictionary<string, int> LoadNameIndex(BitAccess bits, out int age, out Guid guid)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		bits.ReadInt32(out var _);
		bits.ReadInt32(out var _);
		bits.ReadInt32(out age);
		bits.ReadGuid(out guid);
		bits.ReadInt32(out var value3);
		int position = bits.Position;
		int position2 = bits.Position + value3;
		bits.Position = position2;
		bits.ReadInt32(out var value4);
		bits.ReadInt32(out var value5);
		BitSet bitSet = new BitSet(bits);
		if (!new BitSet(bits).IsEmpty)
		{
			throw new PdbDebugException("Unsupported PDB deleted bitset is not empty.");
		}
		int num = 0;
		for (int i = 0; i < value5; i++)
		{
			if (bitSet.IsSet(i))
			{
				bits.ReadInt32(out var value6);
				bits.ReadInt32(out var value7);
				int position3 = bits.Position;
				bits.Position = position + value6;
				bits.ReadCString(out var value8);
				bits.Position = position3;
				dictionary.Add(value8.ToUpperInvariant(), value7);
				num++;
			}
		}
		if (num != value4)
		{
			throw new PdbDebugException("Count mismatch. ({0} != {1})", num, value4);
		}
		return dictionary;
	}

	private static IntHashTable LoadNameStream(BitAccess bits)
	{
		IntHashTable intHashTable = new IntHashTable();
		bits.ReadUInt32(out var value);
		bits.ReadInt32(out var value2);
		bits.ReadInt32(out var value3);
		if (value != 4026462206u || value2 != 1)
		{
			throw new PdbDebugException("Unsupported Name Stream version. (sig={0:x8}, ver={1})", value, value2);
		}
		int position = bits.Position;
		int position2 = bits.Position + value3;
		bits.Position = position2;
		bits.ReadInt32(out var value4);
		position2 = bits.Position;
		for (int i = 0; i < value4; i++)
		{
			bits.ReadInt32(out var value5);
			if (value5 != 0)
			{
				int position3 = bits.Position;
				bits.Position = position + value5;
				bits.ReadCString(out var value6);
				bits.Position = position3;
				intHashTable.Add(value5, value6);
			}
		}
		bits.Position = position2;
		return intHashTable;
	}

	private static int FindFunction(PdbFunction[] funcs, ushort sec, uint off)
	{
		PdbFunction value = new PdbFunction
		{
			segment = sec,
			address = off
		};
		return Array.BinarySearch(funcs, value, PdbFunction.byAddress);
	}

	private static void LoadManagedLines(PdbFunction[] funcs, IntHashTable names, BitAccess bits, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, uint limit)
	{
		Array.Sort(funcs, PdbFunction.byAddressAndToken);
		int position = bits.Position;
		IntHashTable intHashTable = ReadSourceFileInfo(bits, limit, names, dir, nameIndex, reader);
		bits.Position = position;
		CV_LineSection cV_LineSection = default(CV_LineSection);
		CV_SourceFile cV_SourceFile = default(CV_SourceFile);
		CV_SourceFile cV_SourceFile2 = default(CV_SourceFile);
		CV_Line cV_Line = default(CV_Line);
		while (bits.Position < limit)
		{
			bits.ReadInt32(out var value);
			bits.ReadInt32(out var value2);
			int num = bits.Position + value2;
			DEBUG_S_SUBSECTION dEBUG_S_SUBSECTION = (DEBUG_S_SUBSECTION)value;
			if (dEBUG_S_SUBSECTION == DEBUG_S_SUBSECTION.LINES)
			{
				bits.ReadUInt32(out cV_LineSection.off);
				bits.ReadUInt16(out cV_LineSection.sec);
				bits.ReadUInt16(out cV_LineSection.flags);
				bits.ReadUInt32(out cV_LineSection.cod);
				int i = FindFunction(funcs, cV_LineSection.sec, cV_LineSection.off);
				if (i >= 0)
				{
					PdbFunction pdbFunction = funcs[i];
					if (pdbFunction.lines == null)
					{
						while (i > 0)
						{
							PdbFunction pdbFunction2 = funcs[i - 1];
							if (pdbFunction2.lines != null || pdbFunction2.segment != cV_LineSection.sec || pdbFunction2.address != cV_LineSection.off)
							{
								break;
							}
							pdbFunction = pdbFunction2;
							i--;
						}
					}
					else
					{
						for (; i < funcs.Length - 1; i++)
						{
							if (pdbFunction.lines == null)
							{
								break;
							}
							PdbFunction pdbFunction3 = funcs[i + 1];
							if (pdbFunction3.segment != cV_LineSection.sec || pdbFunction3.address != cV_LineSection.off)
							{
								break;
							}
							pdbFunction = pdbFunction3;
						}
					}
					if (pdbFunction.lines == null)
					{
						int position2 = bits.Position;
						int num2 = 0;
						while (bits.Position < num)
						{
							bits.ReadUInt32(out cV_SourceFile.index);
							bits.ReadUInt32(out cV_SourceFile.count);
							bits.ReadUInt32(out cV_SourceFile.linsiz);
							int num3 = (int)cV_SourceFile.count * (8 + ((((uint)cV_LineSection.flags & (true ? 1u : 0u)) != 0) ? 4 : 0));
							bits.Position += num3;
							num2++;
						}
						pdbFunction.lines = new PdbLines[num2];
						int num4 = 0;
						bits.Position = position2;
						while (bits.Position < num)
						{
							bits.ReadUInt32(out cV_SourceFile2.index);
							bits.ReadUInt32(out cV_SourceFile2.count);
							bits.ReadUInt32(out cV_SourceFile2.linsiz);
							PdbLines pdbLines = new PdbLines((PdbSource)intHashTable[(int)cV_SourceFile2.index], cV_SourceFile2.count);
							pdbFunction.lines[num4++] = pdbLines;
							PdbLine[] lines = pdbLines.lines;
							int position3 = bits.Position;
							int num5 = bits.Position + (int)(8 * cV_SourceFile2.count);
							for (int j = 0; j < cV_SourceFile2.count; j++)
							{
								CV_Column cV_Column = default(CV_Column);
								bits.Position = position3 + 8 * j;
								bits.ReadUInt32(out cV_Line.offset);
								bits.ReadUInt32(out cV_Line.flags);
								uint num6 = cV_Line.flags & 0xFFFFFFu;
								uint num7 = (cV_Line.flags & 0x7F000000) >> 24;
								if (((uint)cV_LineSection.flags & (true ? 1u : 0u)) != 0)
								{
									bits.Position = num5 + 4 * j;
									bits.ReadUInt16(out cV_Column.offColumnStart);
									bits.ReadUInt16(out cV_Column.offColumnEnd);
								}
								lines[j] = new PdbLine(cV_Line.offset, num6, cV_Column.offColumnStart, num6 + num7, cV_Column.offColumnEnd);
							}
						}
					}
				}
			}
			bits.Position = num;
		}
	}

	private static void LoadFuncsFromDbiModule(BitAccess bits, DbiModuleInfo info, IntHashTable names, ArrayList funcList, bool readStrings, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader)
	{
		PdbFunction[] array = null;
		bits.Position = 0;
		bits.ReadInt32(out var value);
		if (value != 4)
		{
			throw new PdbDebugException("Invalid signature. (sig={0})", value);
		}
		bits.Position = 4;
		array = PdbFunction.LoadManagedFunctions(bits, (uint)info.cbSyms, readStrings);
		if (array != null)
		{
			bits.Position = info.cbSyms + info.cbOldLines;
			LoadManagedLines(array, names, bits, dir, nameIndex, reader, (uint)(info.cbSyms + info.cbOldLines + info.cbLines));
			for (int i = 0; i < array.Length; i++)
			{
				funcList.Add(array[i]);
			}
		}
	}

	private static void LoadDbiStream(BitAccess bits, out DbiModuleInfo[] modules, out DbiDbgHdr header, bool readStrings)
	{
		DbiHeader dbiHeader = new DbiHeader(bits);
		header = default(DbiDbgHdr);
		ArrayList arrayList = new ArrayList();
		int num = bits.Position + dbiHeader.gpmodiSize;
		while (bits.Position < num)
		{
			DbiModuleInfo value = new DbiModuleInfo(bits, readStrings);
			arrayList.Add(value);
		}
		if (bits.Position != num)
		{
			throw new PdbDebugException("Error reading DBI stream, pos={0} != {1}", bits.Position, num);
		}
		if (arrayList.Count > 0)
		{
			modules = (DbiModuleInfo[])arrayList.ToArray(typeof(DbiModuleInfo));
		}
		else
		{
			modules = null;
		}
		bits.Position += dbiHeader.secconSize;
		bits.Position += dbiHeader.secmapSize;
		bits.Position += dbiHeader.filinfSize;
		bits.Position += dbiHeader.tsmapSize;
		bits.Position += dbiHeader.ecinfoSize;
		num = bits.Position + dbiHeader.dbghdrSize;
		if (dbiHeader.dbghdrSize > 0)
		{
			header = new DbiDbgHdr(bits);
		}
		bits.Position = num;
	}

	internal static PdbFunction[] LoadFunctions(Stream read, out Dictionary<uint, PdbTokenLine> tokenToSourceMapping, out string sourceServerData, out int age, out Guid guid)
	{
		tokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
		BitAccess bitAccess = new BitAccess(524288);
		PdbFileHeader pdbFileHeader = new PdbFileHeader(read, bitAccess);
		PdbReader reader = new PdbReader(read, pdbFileHeader.pageSize);
		MsfDirectory msfDirectory = new MsfDirectory(reader, pdbFileHeader, bitAccess);
		DbiModuleInfo[] modules = null;
		msfDirectory.streams[1].Read(reader, bitAccess);
		Dictionary<string, int> dictionary = LoadNameIndex(bitAccess, out age, out guid);
		if (!dictionary.TryGetValue("/NAMES", out var value))
		{
			throw new PdbException("No `name' stream");
		}
		msfDirectory.streams[value].Read(reader, bitAccess);
		IntHashTable names = LoadNameStream(bitAccess);
		if (!dictionary.TryGetValue("SRCSRV", out var value2))
		{
			sourceServerData = string.Empty;
		}
		else
		{
			DataStream obj = msfDirectory.streams[value2];
			byte[] array = new byte[obj.contentSize];
			obj.Read(reader, bitAccess);
			sourceServerData = bitAccess.ReadBString(array.Length);
		}
		msfDirectory.streams[3].Read(reader, bitAccess);
		LoadDbiStream(bitAccess, out modules, out var header, readStrings: true);
		ArrayList arrayList = new ArrayList();
		if (modules != null)
		{
			foreach (DbiModuleInfo dbiModuleInfo in modules)
			{
				if (dbiModuleInfo.stream > 0)
				{
					msfDirectory.streams[dbiModuleInfo.stream].Read(reader, bitAccess);
					if (dbiModuleInfo.moduleName == "TokenSourceLineInfo")
					{
						LoadTokenToSourceInfo(bitAccess, dbiModuleInfo, names, msfDirectory, dictionary, reader, tokenToSourceMapping);
					}
					else
					{
						LoadFuncsFromDbiModule(bitAccess, dbiModuleInfo, names, arrayList, readStrings: true, msfDirectory, dictionary, reader);
					}
				}
			}
		}
		PdbFunction[] array2 = (PdbFunction[])arrayList.ToArray(typeof(PdbFunction));
		if (header.snTokenRidMap != 0 && header.snTokenRidMap != ushort.MaxValue)
		{
			msfDirectory.streams[header.snTokenRidMap].Read(reader, bitAccess);
			uint[] array3 = new uint[msfDirectory.streams[header.snTokenRidMap].Length / 4];
			bitAccess.ReadUInt32(array3);
			PdbFunction[] array4 = array2;
			foreach (PdbFunction pdbFunction in array4)
			{
				pdbFunction.token = 0x6000000u | array3[pdbFunction.token & 0xFFFFFF];
			}
		}
		Array.Sort(array2, PdbFunction.byAddressAndToken);
		return array2;
	}

	private static void LoadTokenToSourceInfo(BitAccess bits, DbiModuleInfo module, IntHashTable names, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<uint, PdbTokenLine> tokenToSourceMapping)
	{
		bits.Position = 0;
		bits.ReadInt32(out var value);
		if (value != 4)
		{
			throw new PdbDebugException("Invalid signature. (sig={0})", value);
		}
		bits.Position = 4;
		OemSymbol oemSymbol = default(OemSymbol);
		while (bits.Position < module.cbSyms)
		{
			bits.ReadUInt16(out var value2);
			int position = bits.Position;
			int position2 = bits.Position + value2;
			bits.Position = position;
			bits.ReadUInt16(out var value3);
			switch ((SYM)value3)
			{
			case SYM.S_OEM:
				bits.ReadGuid(out oemSymbol.idOem);
				bits.ReadUInt32(out oemSymbol.typind);
				if (oemSymbol.idOem == PdbFunction.msilMetaData)
				{
					if (bits.ReadString() == "TSLI")
					{
						bits.ReadUInt32(out var value4);
						bits.ReadUInt32(out var value5);
						bits.ReadUInt32(out var value6);
						bits.ReadUInt32(out var value7);
						bits.ReadUInt32(out var value8);
						bits.ReadUInt32(out var value9);
						if (!tokenToSourceMapping.TryGetValue(value4, out var value10))
						{
							tokenToSourceMapping.Add(value4, new PdbTokenLine(value4, value5, value6, value7, value8, value9));
						}
						else
						{
							while (value10.nextLine != null)
							{
								value10 = value10.nextLine;
							}
							value10.nextLine = new PdbTokenLine(value4, value5, value6, value7, value8, value9);
						}
					}
					bits.Position = position2;
					break;
				}
				throw new PdbDebugException("OEM section: guid={0} ti={1}", oemSymbol.idOem, oemSymbol.typind);
			case SYM.S_END:
				bits.Position = position2;
				break;
			default:
				bits.Position = position2;
				break;
			}
		}
		bits.Position = module.cbSyms + module.cbOldLines;
		int limit = module.cbSyms + module.cbOldLines + module.cbLines;
		IntHashTable intHashTable = ReadSourceFileInfo(bits, (uint)limit, names, dir, nameIndex, reader);
		foreach (PdbTokenLine value11 in tokenToSourceMapping.Values)
		{
			value11.sourceFile = (PdbSource)intHashTable[(int)value11.file_id];
		}
	}

	private static IntHashTable ReadSourceFileInfo(BitAccess bits, uint limit, IntHashTable names, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader)
	{
		IntHashTable intHashTable = new IntHashTable();
		_ = bits.Position;
		CV_FileCheckSum cV_FileCheckSum = default(CV_FileCheckSum);
		while (bits.Position < limit)
		{
			bits.ReadInt32(out var value);
			bits.ReadInt32(out var value2);
			int position = bits.Position;
			int num = bits.Position + value2;
			DEBUG_S_SUBSECTION dEBUG_S_SUBSECTION = (DEBUG_S_SUBSECTION)value;
			if (dEBUG_S_SUBSECTION == DEBUG_S_SUBSECTION.FILECHKSMS)
			{
				while (bits.Position < num)
				{
					int key = bits.Position - position;
					bits.ReadUInt32(out cV_FileCheckSum.name);
					bits.ReadUInt8(out cV_FileCheckSum.len);
					bits.ReadUInt8(out cV_FileCheckSum.type);
					string text = (string)names[(int)cV_FileCheckSum.name];
					Guid doctype = SymDocumentType.Text;
					Guid language = Guid.Empty;
					Guid vendor = Guid.Empty;
					if (nameIndex.TryGetValue("/SRC/FILES/" + text.ToUpperInvariant(), out var value3))
					{
						BitAccess bits2 = new BitAccess(256);
						dir.streams[value3].Read(reader, bits2);
						LoadGuidStream(bits2, out doctype, out language, out vendor);
					}
					PdbSource value4 = new PdbSource(text, doctype, language, vendor);
					intHashTable.Add(key, value4);
					bits.Position += cV_FileCheckSum.len;
					bits.Align(4);
				}
				bits.Position = num;
			}
			else
			{
				bits.Position = num;
			}
		}
		return intHashTable;
	}
}
