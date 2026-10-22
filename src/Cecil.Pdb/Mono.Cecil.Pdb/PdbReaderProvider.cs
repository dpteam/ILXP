using System.IO;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb;

public class PdbReaderProvider : ISymbolReaderProvider
{
	public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName)
	{
		return new PdbReader(File.OpenRead(PdbHelper.GetPdbFileName(fileName)));
	}

	public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream)
	{
		return new PdbReader(symbolStream);
	}
}
