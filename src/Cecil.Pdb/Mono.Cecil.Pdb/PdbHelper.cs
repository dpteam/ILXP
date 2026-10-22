using System.IO;

namespace Mono.Cecil.Pdb;

internal class PdbHelper
{
	public static SymWriter CreateWriter(ModuleDefinition module, string pdb)
	{
		SymWriter symWriter = new SymWriter();
		if (File.Exists(pdb))
		{
			File.Delete(pdb);
		}
		symWriter.Initialize(new ModuleMetadata(module), pdb, fFullBuild: true);
		return symWriter;
	}

	public static string GetPdbFileName(string assemblyFileName)
	{
		return Path.ChangeExtension(assemblyFileName, ".pdb");
	}
}
