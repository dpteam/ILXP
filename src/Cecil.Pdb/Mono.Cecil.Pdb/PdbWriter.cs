using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb;

public class PdbWriter : Mono.Cecil.Cil.ISymbolWriter, IDisposable
{
	private readonly ModuleDefinition module;

	private readonly SymWriter writer;

	private readonly Dictionary<string, SymDocumentWriter> documents;

	internal PdbWriter(ModuleDefinition module, SymWriter writer)
	{
		this.module = module;
		this.writer = writer;
		documents = new Dictionary<string, SymDocumentWriter>();
	}

	public bool GetDebugHeader(out ImageDebugDirectory directory, out byte[] header)
	{
		header = writer.GetDebugInfo(out directory);
		return true;
	}

	public void Write(MethodBody body)
	{
		SymbolToken method = new SymbolToken(body.Method.MetadataToken.ToInt32());
		Collection<Instruction> collection = CollectInstructions(body);
		if (collection.Count != 0)
		{
			int num = 0;
			int codeSize = body.CodeSize;
			writer.OpenMethod(method);
			writer.OpenScope(num);
			DefineSequencePoints(collection);
			DefineVariables(body, num, codeSize);
			writer.CloseScope(codeSize);
			writer.CloseMethod();
		}
	}

	private Collection<Instruction> CollectInstructions(MethodBody body)
	{
		Collection<Instruction> collection = new Collection<Instruction>();
		Collection<Instruction> instructions = body.Instructions;
		for (int i = 0; i < instructions.Count; i++)
		{
			Instruction instruction = instructions[i];
			SequencePoint sequencePoint = instruction.SequencePoint;
			if (sequencePoint != null)
			{
				GetDocument(sequencePoint.Document);
				collection.Add(instruction);
			}
		}
		return collection;
	}

	private void DefineVariables(MethodBody body, int start_offset, int end_offset)
	{
		if (body.HasVariables)
		{
			SymbolToken local_var_token = new SymbolToken(body.LocalVarToken.ToInt32());
			Collection<VariableDefinition> variables = body.Variables;
			for (int i = 0; i < variables.Count; i++)
			{
				VariableDefinition variable = variables[i];
				CreateLocalVariable(variable, local_var_token, start_offset, end_offset);
			}
		}
	}

	private void DefineSequencePoints(Collection<Instruction> instructions)
	{
		for (int i = 0; i < instructions.Count; i++)
		{
			Instruction instruction = instructions[i];
			SequencePoint sequencePoint = instruction.SequencePoint;
			writer.DefineSequencePoints(GetDocument(sequencePoint.Document), new int[1] { instruction.Offset }, new int[1] { sequencePoint.StartLine }, new int[1] { sequencePoint.StartColumn }, new int[1] { sequencePoint.EndLine }, new int[1] { sequencePoint.EndColumn });
		}
	}

	private void CreateLocalVariable(VariableDefinition variable, SymbolToken local_var_token, int start_offset, int end_offset)
	{
		writer.DefineLocalVariable2(variable.Name, FieldAttributes.CompilerControlled, local_var_token, SymAddressKind.ILOffset, variable.Index, 0, 0, start_offset, end_offset);
	}

	private SymDocumentWriter GetDocument(Document document)
	{
		if (document == null)
		{
			return null;
		}
		if (documents.TryGetValue(document.Url, out var value))
		{
			return value;
		}
		value = writer.DefineDocument(document.Url, document.Language.ToGuid(), document.LanguageVendor.ToGuid(), document.Type.ToGuid());
		documents[document.Url] = value;
		return value;
	}

	public void Write(MethodSymbols symbols)
	{
		SymbolToken method = new SymbolToken(symbols.MethodToken.ToInt32());
		int num = 0;
		int codeSize = symbols.CodeSize;
		writer.OpenMethod(method);
		writer.OpenScope(num);
		DefineSequencePoints(symbols);
		DefineVariables(symbols, num, codeSize);
		writer.CloseScope(codeSize);
		writer.CloseMethod();
	}

	private void DefineSequencePoints(MethodSymbols symbols)
	{
		Collection<InstructionSymbol> instructions = symbols.instructions;
		for (int i = 0; i < instructions.Count; i++)
		{
			InstructionSymbol instructionSymbol = instructions[i];
			SequencePoint sequencePoint = instructionSymbol.SequencePoint;
			writer.DefineSequencePoints(GetDocument(sequencePoint.Document), new int[1] { instructionSymbol.Offset }, new int[1] { sequencePoint.StartLine }, new int[1] { sequencePoint.StartColumn }, new int[1] { sequencePoint.EndLine }, new int[1] { sequencePoint.EndColumn });
		}
	}

	private void DefineVariables(MethodSymbols symbols, int start_offset, int end_offset)
	{
		if (symbols.HasVariables)
		{
			SymbolToken local_var_token = new SymbolToken(symbols.LocalVarToken.ToInt32());
			Collection<VariableDefinition> variables = symbols.Variables;
			for (int i = 0; i < variables.Count; i++)
			{
				VariableDefinition variable = variables[i];
				CreateLocalVariable(variable, local_var_token, start_offset, end_offset);
			}
		}
	}

	public void Dispose()
	{
		MethodDefinition entryPoint = module.EntryPoint;
		if (entryPoint != null)
		{
			writer.SetUserEntryPoint(new SymbolToken(entryPoint.MetadataToken.ToInt32()));
		}
		writer.Close();
	}
}
