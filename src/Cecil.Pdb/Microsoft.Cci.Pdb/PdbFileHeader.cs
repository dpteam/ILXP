using System;
using System.IO;
using System.Text;

namespace Microsoft.Cci.Pdb;

internal class PdbFileHeader
{
	private const string MAGIC = "Microsoft C/C++ MSF 7.00";

	internal readonly byte[] magic;

	internal readonly int pageSize;

	internal int freePageMap;

	internal int pagesUsed;

	internal int directorySize;

	internal readonly int zero;

	internal int[] directoryRoot;

	private string Magic => StringFromBytesUTF8(magic, 0, "Microsoft C/C++ MSF 7.00".Length);

	internal PdbFileHeader(Stream reader, BitAccess bits)
	{
		bits.MinCapacity(56);
		reader.Seek(0L, SeekOrigin.Begin);
		bits.FillBuffer(reader, 52);
		magic = new byte[32];
		bits.ReadBytes(magic);
		bits.ReadInt32(out pageSize);
		bits.ReadInt32(out freePageMap);
		bits.ReadInt32(out pagesUsed);
		bits.ReadInt32(out directorySize);
		bits.ReadInt32(out zero);
		if (Magic != "Microsoft C/C++ MSF 7.00")
		{
			throw new InvalidOperationException("Magic is wrong.");
		}
		int num = ((directorySize + pageSize - 1) / pageSize * 4 + pageSize - 1) / pageSize;
		directoryRoot = new int[num];
		bits.FillBuffer(reader, num * 4);
		bits.ReadInt32(directoryRoot);
	}

	private static string StringFromBytesUTF8(byte[] bytes)
	{
		return StringFromBytesUTF8(bytes, 0, bytes.Length);
	}

	private static string StringFromBytesUTF8(byte[] bytes, int offset, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (bytes[offset + i] < 32)
			{
				length = i;
			}
		}
		return Encoding.UTF8.GetString(bytes, offset, length);
	}
}
