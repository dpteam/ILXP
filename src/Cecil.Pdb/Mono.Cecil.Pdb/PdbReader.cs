using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci.Pdb;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb;

public class PdbReader : ISymbolReader, IDisposable
{
	private int age;

	private Guid guid;

	private readonly Stream pdb_file;

	private readonly Dictionary<string, Document> documents = new Dictionary<string, Document>();

	private readonly Dictionary<uint, PdbFunction> functions = new Dictionary<uint, PdbFunction>();

	internal PdbReader(Stream file)
	{
		pdb_file = file;
	}

	public bool ProcessDebugHeader(ImageDebugDirectory directory, byte[] header)
	{
		if (directory.Type != 2)
		{
			return false;
		}
		if (directory.MajorVersion != 0 || directory.MinorVersion != 0)
		{
			return false;
		}
		if (header.Length < 24)
		{
			return false;
		}
		if (ReadInt32(header, 0) != 1396986706)
		{
			return false;
		}
		byte[] array = new byte[16];
		Buffer.BlockCopy(header, 4, array, 0, 16);
		guid = new Guid(array);
		age = ReadInt32(header, 20);
		return PopulateFunctions();
	}

	private static int ReadInt32(byte[] bytes, int start)
	{
		return bytes[start] | (bytes[start + 1] << 8) | (bytes[start + 2] << 16) | (bytes[start + 3] << 24);
	}

	private bool PopulateFunctions()
	{
		using (pdb_file)
		{
			Dictionary<uint, PdbTokenLine> tokenToSourceMapping;
			string sourceServerData;
			int num;
			Guid guid;
			PdbFunction[] array = PdbFile.LoadFunctions(pdb_file, out tokenToSourceMapping, out sourceServerData, out num, out guid);
			if (this.guid != guid)
			{
				return false;
			}
			PdbFunction[] array2 = array;
			foreach (PdbFunction pdbFunction in array2)
			{
				functions.Add(pdbFunction.token, pdbFunction);
			}
		}
		return true;
	}

	public void Read(MethodBody body, InstructionMapper mapper)
	{
		MetadataToken metadataToken = body.Method.MetadataToken;
		if (functions.TryGetValue(metadataToken.ToUInt32(), out var value))
		{
			ReadSequencePoints(value, mapper);
			ReadScopeAndLocals(value.scopes, null, body, mapper);
		}
	}

	private static void ReadScopeAndLocals(PdbScope[] scopes, Scope parent, MethodBody body, InstructionMapper mapper)
	{
		for (int i = 0; i < scopes.Length; i++)
		{
			ReadScopeAndLocals(scopes[i], parent, body, mapper);
		}
		CreateRootScope(body);
	}

	private static void CreateRootScope(MethodBody body)
	{
		if (body.HasVariables)
		{
			Collection<Instruction> instructions = body.Instructions;
			Scope scope = new Scope();
			scope.Start = instructions[0];
			scope.End = instructions[instructions.Count - 1];
			Collection<VariableDefinition> variables = body.Variables;
			for (int i = 0; i < variables.Count; i++)
			{
				scope.Variables.Add(variables[i]);
			}
			body.Scope = scope;
		}
	}

	private static void ReadScopeAndLocals(PdbScope scope, Scope parent, MethodBody body, InstructionMapper mapper)
	{
		if (scope == null)
		{
			return;
		}
		PdbSlot[] slots = scope.slots;
		foreach (PdbSlot pdbSlot in slots)
		{
			int slot = (int)pdbSlot.slot;
			if (slot >= 0 && slot < body.Variables.Count)
			{
				body.Variables[slot].Name = pdbSlot.name;
			}
		}
		ReadScopeAndLocals(scope.scopes, null, body, mapper);
	}

	private void ReadSequencePoints(PdbFunction function, InstructionMapper mapper)
	{
		if (function.lines != null)
		{
			PdbLines[] lines = function.lines;
			foreach (PdbLines lines2 in lines)
			{
				ReadLines(lines2, mapper);
			}
		}
	}

	private void ReadLines(PdbLines lines, InstructionMapper mapper)
	{
		Document document = GetDocument(lines.file);
		PdbLine[] lines2 = lines.lines;
		for (int i = 0; i < lines2.Length; i++)
		{
			ReadLine(lines2[i], document, mapper);
		}
	}

	private static void ReadLine(PdbLine line, Document document, InstructionMapper mapper)
	{
		Instruction instruction = mapper((int)line.offset);
		if (instruction != null)
		{
			SequencePoint sequencePoint = new SequencePoint(document);
			sequencePoint.StartLine = (int)line.lineBegin;
			sequencePoint.StartColumn = line.colBegin;
			sequencePoint.EndLine = (int)line.lineEnd;
			sequencePoint.EndColumn = line.colEnd;
			instruction.SequencePoint = sequencePoint;
		}
	}

	private Document GetDocument(PdbSource source)
	{
		string name = source.name;
		if (documents.TryGetValue(name, out var value))
		{
			return value;
		}
		value = new Document(name)
		{
			Language = source.language.ToLanguage(),
			LanguageVendor = source.vendor.ToVendor(),
			Type = source.doctype.ToType()
		};
		documents.Add(name, value);
		return value;
	}

	public void Read(MethodSymbols symbols)
	{
		if (functions.TryGetValue(symbols.MethodToken.ToUInt32(), out var value))
		{
			ReadSequencePoints(value, symbols);
			ReadLocals(value.scopes, symbols);
		}
	}

	private void ReadLocals(PdbScope[] scopes, MethodSymbols symbols)
	{
		foreach (PdbScope scope in scopes)
		{
			ReadLocals(scope, symbols);
		}
	}

	private void ReadLocals(PdbScope scope, MethodSymbols symbols)
	{
		if (scope == null)
		{
			return;
		}
		PdbSlot[] slots = scope.slots;
		foreach (PdbSlot pdbSlot in slots)
		{
			int slot = (int)pdbSlot.slot;
			if (slot >= 0 && slot < symbols.Variables.Count)
			{
				symbols.Variables[slot].Name = pdbSlot.name;
			}
		}
		ReadLocals(scope.scopes, symbols);
	}

	private void ReadSequencePoints(PdbFunction function, MethodSymbols symbols)
	{
		if (function.lines != null)
		{
			PdbLines[] lines = function.lines;
			foreach (PdbLines lines2 in lines)
			{
				ReadLines(lines2, symbols);
			}
		}
	}

	private void ReadLines(PdbLines lines, MethodSymbols symbols)
	{
		for (int i = 0; i < lines.lines.Length; i++)
		{
			PdbLine pdbLine = lines.lines[i];
			symbols.Instructions.Add(new InstructionSymbol((int)pdbLine.offset, new SequencePoint(GetDocument(lines.file))
			{
				StartLine = (int)pdbLine.lineBegin,
				StartColumn = pdbLine.colBegin,
				EndLine = (int)pdbLine.lineEnd,
				EndColumn = pdbLine.colEnd
			}));
		}
	}

	public void Dispose()
	{
		pdb_file.Close();
	}
}
