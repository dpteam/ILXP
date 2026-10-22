using System;
using System.IO;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb;

public class PdbWriterProvider : ISymbolWriterProvider
{
	public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
	{
		return new PdbWriter(module, PdbHelper.CreateWriter(module, PdbHelper.GetPdbFileName(fileName)));
	}

	public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
	{
		throw new NotImplementedException();
	}
}
