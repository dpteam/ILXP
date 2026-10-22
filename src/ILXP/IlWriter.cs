using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ILXP
{
    public sealed class IlWriter : LanguageWriter
    {
    	[DebuggerNonUserCode]
    	public IlWriter()
    	{
    	}

    	public override string WriteAssembly(AssemblyItem gclass0_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("<<g>>" + GetDialectHeader().TrimEnd() + "<<>>");
    		stringBuilder.AppendLine("<<g>>" + GetRuntimeHeader().TrimEnd() + "<<>>");
    		stringBuilder.AppendLine("<<g>>//Name: " + gclass0_0.Assembly.Name.Name + "<<>>");
    		stringBuilder.AppendLine("<<g>>//Path: " + gclass0_0.FilePath + "<<>>");
    		stringBuilder.AppendLine("<<g>>//Version: " + $"{gclass0_0.Assembly.Name.Version.Major}:{gclass0_0.Assembly.Name.Version.MajorRevision}:{gclass0_0.Assembly.Name.Version.Minor}:{gclass0_0.Assembly.Name.Version.MinorRevision}" + "<<>>");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("<<b>>.assembly<<>> " + LanguageWriter.GetReferenceKey(gclass0_0) + gclass0_0.Assembly.Name.Name + "<<>>");
    		stringBuilder.AppendLine("{");
    		stringBuilder.AppendLine("    <<b>>.ver<<>> " + $"{gclass0_0.Assembly.Name.Version.Major}:{gclass0_0.Assembly.Name.Version.MajorRevision}:{gclass0_0.Assembly.Name.Version.Minor}:{gclass0_0.Assembly.Name.Version.MinorRevision}");
    		stringBuilder.AppendLine(Conversions.ToString(Operators.ConcatenateObject("    <<b>>.hash algorithm<<>> 0x" + Convert.ToString(checked((int)gclass0_0.Assembly.Name.HashAlgorithm), 16), Interaction.IIf(gclass0_0.Assembly.Name.HashAlgorithm == AssemblyHashAlgorithm.SHA1, " <<g>>// SHA1<<>>", ""))));
    		WriteCustomAttributes(stringBuilder, gclass0_0.Assembly.CustomAttributes);
    		stringBuilder.Append("}");
    		return stringBuilder.ToString();
    	}

    	public override string WriteNamespace(ModuleItem gclass6_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		if (!string.IsNullOrEmpty(gclass6_0.Namespace))
    		{
    			stringBuilder.AppendLine("<<b>>.namespace<<>> " + LanguageWriter.GetReferenceKey(gclass6_0) + gclass6_0.Namespace + "<<>>");
    		}
    		stringBuilder.AppendLine("{");
    		StringBuilder stringBuilder2 = new StringBuilder();
    		foreach (TypeDefinition type in gclass6_0.Module.Types)
    		{
    			if (Operators.CompareString(type.Namespace, gclass6_0.Namespace, TextCompare: false) == 0)
    			{
    				stringBuilder2.AppendLine(WriteType(type, bool_0: true));
    				stringBuilder2.AppendLine();
    			}
    		}
    		stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + "    ");
    		stringBuilder.AppendLine("    " + stringBuilder2.ToString().Trim());
    		stringBuilder.Append("}");
    		return stringBuilder.ToString();
    	}

    	public override string WriteType(TypeDefinition typeDefinition_0)
    	{
    		return WriteType(typeDefinition_0, bool_0: false);
    	}

    	public string WriteType(TypeDefinition typeDefinition_0, bool bool_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>");
    		stringBuilder.Append(".class ");
    		if (typeDefinition_0.IsPublic)
    		{
    			stringBuilder.Append("public ");
    		}
    		else if (typeDefinition_0.IsNotPublic)
    		{
    			stringBuilder.Append("private ");
    		}
    		else if (typeDefinition_0.IsNestedPrivate)
    		{
    			stringBuilder.Append("nested private ");
    		}
    		else if (typeDefinition_0.IsNestedPublic)
    		{
    			stringBuilder.Append("nested public ");
    		}
    		else if (typeDefinition_0.IsNestedAssembly)
    		{
    			stringBuilder.Append("nested assembly ");
    		}
    		else if (typeDefinition_0.IsNestedFamily)
    		{
    			stringBuilder.Append("nested family ");
    		}
    		else if (typeDefinition_0.IsNestedFamilyAndAssembly)
    		{
    			stringBuilder.Append("nested famandassem ");
    		}
    		else if (typeDefinition_0.IsNestedFamilyOrAssembly)
    		{
    			stringBuilder.Append("nested famorassem ");
    		}
    		if (typeDefinition_0.IsSequentialLayout)
    		{
    			stringBuilder.Append("sequential ");
    		}
    		if (typeDefinition_0.IsExplicitLayout)
    		{
    			stringBuilder.Append("explicit ");
    		}
    		if (typeDefinition_0.IsAutoLayout)
    		{
    			stringBuilder.Append("auto ");
    		}
    		if (typeDefinition_0.IsAnsiClass)
    		{
    			stringBuilder.Append("ansi ");
    		}
    		if (typeDefinition_0.IsAutoClass)
    		{
    			stringBuilder.Append("auto ");
    		}
    		if (typeDefinition_0.IsUnicodeClass)
    		{
    			stringBuilder.Append("unicode ");
    		}
    		if (typeDefinition_0.IsInterface)
    		{
    			stringBuilder.Append("interface ");
    		}
    		if (typeDefinition_0.IsSpecialName)
    		{
    			stringBuilder.Append("specialname ");
    		}
    		if (typeDefinition_0.IsRuntimeSpecialName)
    		{
    			stringBuilder.Append("rtspecialname ");
    		}
    		if (typeDefinition_0.IsImport)
    		{
    			stringBuilder.Append("import ");
    		}
    		if (typeDefinition_0.IsAbstract)
    		{
    			stringBuilder.Append("abstract ");
    		}
    		if (typeDefinition_0.IsSerializable)
    		{
    			stringBuilder.Append("serializable ");
    		}
    		if (typeDefinition_0.IsBeforeFieldInit)
    		{
    			stringBuilder.Append("beforefieldinit ");
    		}
    		if (typeDefinition_0.IsSealed)
    		{
    			stringBuilder.Append("sealed ");
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(LanguageWriter.GetReferenceKey(typeDefinition_0));
    		stringBuilder.AppendLine(LanguageWriter.GetShortTypeName(typeDefinition_0) + "<<>>");
    		if (typeDefinition_0.BaseType != null)
    		{
    			stringBuilder.AppendLine("    <<b>>extends<<>> " + GetTypeName(typeDefinition_0.BaseType));
    		}
    		checked
    		{
    			if (typeDefinition_0.Interfaces.Count > 0)
    			{
    				stringBuilder.Append("    <<b>>implements<<>> ");
    				foreach (TypeReference @interface in typeDefinition_0.Interfaces)
    				{
    					stringBuilder.Append(GetTypeName(@interface) + ", ");
    				}
    				stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				stringBuilder.AppendLine();
    			}
    			stringBuilder.AppendLine("{");
    			WriteCustomAttributes(stringBuilder, typeDefinition_0.CustomAttributes);
    			if (bool_0)
    			{
    				stringBuilder.Append("}");
    				return stringBuilder.ToString();
    			}
    			if (typeDefinition_0.HasLayoutInfo)
    			{
    				stringBuilder.AppendLine($"    <<b>>.pack<<>> {typeDefinition_0.PackingSize}");
    				stringBuilder.AppendLine($"    <<b>>.size<<>> {typeDefinition_0.ClassSize}");
    			}
    			StringBuilder stringBuilder2 = new StringBuilder();
    			if (typeDefinition_0.NestedTypes.Count > 0)
    			{
    				stringBuilder2.AppendLine("<<g>>// Nested Types<<>>");
    			}
    			foreach (TypeDefinition nestedType in typeDefinition_0.NestedTypes)
    			{
    				stringBuilder2.AppendLine(WriteType(nestedType, bool_0: true));
    				stringBuilder2.AppendLine();
    			}
    			if (typeDefinition_0.Fields.Count > 0)
    			{
    				stringBuilder2.AppendLine("<<g>>// Fields<<>>");
    			}
    			foreach (FieldDefinition field in typeDefinition_0.Fields)
    			{
    				stringBuilder2.AppendLine(WriteField(field));
    				stringBuilder2.AppendLine();
    			}
    			if (typeDefinition_0.Properties.Count > 0)
    			{
    				stringBuilder2.AppendLine("<<g>>// Properties<<>>");
    			}
    			foreach (PropertyDefinition property in typeDefinition_0.Properties)
    			{
    				stringBuilder2.AppendLine(WriteProperty(property));
    				stringBuilder2.AppendLine();
    			}
    			List<string> list = new List<string>();
    			foreach (PropertyDefinition property2 in typeDefinition_0.Properties)
    			{
    				if (property2.SetMethod != null)
    				{
    					list.Add(property2.SetMethod.Name);
    				}
    				if (property2.GetMethod != null)
    				{
    					list.Add(property2.GetMethod.Name);
    				}
    			}
    			foreach (EventDefinition @event in typeDefinition_0.Events)
    			{
    				if (@event.AddMethod != null)
    				{
    					list.Add(@event.AddMethod.Name);
    				}
    				if (@event.RemoveMethod != null)
    				{
    					list.Add(@event.RemoveMethod.Name);
    				}
    				foreach (MethodDefinition otherMethod in @event.OtherMethods)
    				{
    					if (otherMethod != null)
    					{
    						list.Add(otherMethod.Name);
    					}
    				}
    			}
    			if (typeDefinition_0.Methods.Count - list.Count > 0)
    			{
    				stringBuilder2.AppendLine("<<g>>// Methods<<>>");
    			}
    			foreach (MethodDefinition method in typeDefinition_0.Methods)
    			{
    				if (!list.Contains(method.Name))
    				{
    					stringBuilder2.AppendLine(WriteMethod(method, bool_0: true));
    					stringBuilder2.AppendLine();
    				}
    			}
    			if (typeDefinition_0.Events.Count > 0)
    			{
    				stringBuilder2.AppendLine("<<g>>// Events<<>>");
    			}
    			foreach (EventDefinition event2 in typeDefinition_0.Events)
    			{
    				stringBuilder2.AppendLine(WriteEvent(event2));
    				stringBuilder2.AppendLine();
    			}
    			stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + "    ");
    			stringBuilder.AppendLine("    " + stringBuilder2.ToString().Trim());
    			stringBuilder.Append("}");
    			return stringBuilder.ToString();
    		}
    	}

    	public override string WriteModule(ModuleDefinition moduleDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>.module<<>> ");
    		stringBuilder.AppendLine(moduleDefinition_0.Name);
    		stringBuilder.AppendLine("<<g>>// MVID: " + moduleDefinition_0.Mvid.ToString() + "<<>>");
    		stringBuilder.Append("<<g>>// Target Runtime Version: " + moduleDefinition_0.Runtime.ToString() + "<<>>");
    		return stringBuilder.ToString();
    	}

    	public override string WriteMethod(MethodDefinition methodDefinition_0)
    	{
    		return WriteMethod(methodDefinition_0, bool_0: false);
    	}

    	public string WriteMethod(MethodDefinition methodDefinition_0, bool bool_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>");
    		stringBuilder.Append(".method ");
    		if (methodDefinition_0.IsPrivate)
    		{
    			stringBuilder.Append("private ");
    		}
    		else if (methodDefinition_0.IsFamilyAndAssembly)
    		{
    			stringBuilder.Append("famandassem ");
    		}
    		else if (methodDefinition_0.IsAssembly)
    		{
    			stringBuilder.Append("assembly ");
    		}
    		else if (methodDefinition_0.IsFamily)
    		{
    			stringBuilder.Append("family ");
    		}
    		else if (methodDefinition_0.IsFamilyOrAssembly)
    		{
    			stringBuilder.Append("famorassem ");
    		}
    		else if (methodDefinition_0.IsPublic)
    		{
    			stringBuilder.Append("public ");
    		}
    		if (methodDefinition_0.IsHideBySig)
    		{
    			stringBuilder.Append("hidebysig ");
    		}
    		if (methodDefinition_0.IsNewSlot)
    		{
    			stringBuilder.Append("newslot ");
    		}
    		if (methodDefinition_0.IsSpecialName)
    		{
    			stringBuilder.Append("specialname ");
    		}
    		if (methodDefinition_0.IsConstructor)
    		{
    			stringBuilder.Append("rtspecialname ");
    		}
    		if (methodDefinition_0.IsVirtual)
    		{
    			stringBuilder.Append("virtual ");
    		}
    		if (methodDefinition_0.IsFinal)
    		{
    			stringBuilder.Append("final ");
    		}
    		if (methodDefinition_0.IsAbstract)
    		{
    			stringBuilder.Append("abstract ");
    		}
    		if (methodDefinition_0.IsPInvokeImpl)
    		{
    			stringBuilder.Append("pinvokeimpl<<>>(<<r>>\"" + methodDefinition_0.PInvokeInfo.Module.Name + "\"<<r>>)<<b>> ");
    		}
    		if (methodDefinition_0.IsUnmanagedExport)
    		{
    			stringBuilder.Append("export ");
    		}
    		if ((methodDefinition_0.Attributes & MethodAttributes.RequireSecObject) != 0)
    		{
    			stringBuilder.Append("requiresecobj ");
    		}
    		if (methodDefinition_0.IsStatic)
    		{
    			stringBuilder.Append("static ");
    		}
    		if (methodDefinition_0.HasThis)
    		{
    			stringBuilder.Append("instance ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.C)
    		{
    			stringBuilder.Append("unmanaged cdecl ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.StdCall)
    		{
    			stringBuilder.Append("unmanaged stdcall ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.ThisCall)
    		{
    			stringBuilder.Append("unmanaged thiscall ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.FastCall)
    		{
    			stringBuilder.Append("unmanaged fastcall ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.VarArg)
    		{
    			stringBuilder.Append("vararg ");
    		}
    		if (methodDefinition_0.CallingConvention == MethodCallingConvention.Generic)
    		{
    			stringBuilder.Append("generic ");
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(_003C(methodDefinition_0.ReturnType) + " ");
    		stringBuilder.Append(LanguageWriter.GetReferenceKey(methodDefinition_0) + methodDefinition_0.Name + "<<>>");
    		stringBuilder.Append("(");
    		checked
    		{
    			if (methodDefinition_0.Parameters.Count > 0)
    			{
    				foreach (ParameterDefinition parameter in methodDefinition_0.Parameters)
    				{
    					stringBuilder.Append($"{_003C(parameter.ParameterType)} {parameter.Name}, ");
    				}
    				stringBuilder.Remove(stringBuilder.Length - 2, 2);
    			}
    			stringBuilder.Append(") <<b>>");
    			if (methodDefinition_0.IsNative)
    			{
    				stringBuilder.Append("native ");
    			}
    			if (methodDefinition_0.IsIL)
    			{
    				stringBuilder.Append("cil ");
    			}
    			if (methodDefinition_0.IsRuntime)
    			{
    				stringBuilder.Append("runtime ");
    			}
    			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.OPTIL) != 0)
    			{
    				stringBuilder.Append("optil ");
    			}
    			if (methodDefinition_0.IsManaged)
    			{
    				stringBuilder.Append("managed");
    			}
    			else
    			{
    				stringBuilder.Append("unmanaged");
    			}
    			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.Synchronized) != 0)
    			{
    				stringBuilder.Append(" synchronized");
    			}
    			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.NoInlining) != 0)
    			{
    				stringBuilder.Append(" noinlining");
    			}
    			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.NoOptimization) != 0)
    			{
    				stringBuilder.Append(" nooptimization");
    			}
    			stringBuilder.Append("<<>>");
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("{");
    			if (bool_0 | (methodDefinition_0.Body == null))
    			{
    				stringBuilder.Append("}");
    				return stringBuilder.ToString();
    			}
    			MethodBody body = methodDefinition_0.Body;
    			stringBuilder.AppendLine($"    <<g>>// Method Start RVA 0x{methodDefinition_0.RVA:x4}<<>>");
    			stringBuilder.AppendLine(string.Format("    <<g>>// Code Size {0} (0x{0:x})<<>>", body.CodeSize));
    			WriteCustomAttributes(stringBuilder, methodDefinition_0.CustomAttributes);
    			if (methodDefinition_0.Module.Assembly.EntryPoint == methodDefinition_0)
    			{
    				stringBuilder.AppendLine("    <<b>>.entrypoint<<>>");
    			}
    			if (body != null)
    			{
    				stringBuilder.AppendLine(string.Format("    <<b>>.maxstack<<>> {0}", body.MaxStackSize));
    			}
    			if (body.Variables.Count > 0)
    			{
    				List<string> list = new List<string>();
    				foreach (VariableDefinition variable in body.Variables)
    				{
    					variable.Name = variable.Name.Replace("&", "");
    					if (!string.IsNullOrEmpty(variable.Name))
    					{
    						list.Add(variable.Name);
    					}
    				}
    				foreach (VariableDefinition variable2 in body.Variables)
    				{
    					if (string.IsNullOrEmpty(variable2.Name))
    					{
    						variable2.Name = CreateUniqueLocalName(variable2.VariableType, list);
    					}
    				}
    				if (body.InitLocals)
    				{
    					stringBuilder.AppendLine("    <<b>>.locals init<<>>");
    				}
    				else
    				{
    					stringBuilder.AppendLine("    <<b>>.locals<<>>");
    				}
    				stringBuilder.AppendLine("    (");
    				foreach (VariableDefinition variable3 in body.Variables)
    				{
    					stringBuilder.Append($"        [{variable3.Index}] {_003C(variable3.VariableType)} ");
    					if (variable3.IsPinned)
    					{
    						stringBuilder.Append("<<b>>pinned<<>> ");
    					}
    					stringBuilder.Append(variable3.Name);
    					if (variable3 != body.Variables[body.Variables.Count - 1])
    					{
    						stringBuilder.Append(",");
    					}
    					stringBuilder.AppendLine();
    				}
    				stringBuilder.AppendLine("    )");
    			}
    			foreach (Instruction instruction2 in body.Instructions)
    			{
    				string text = "";
    				if (instruction2.Operand == null)
    				{
    					text = "";
    				}
    				else if (instruction2.Operand is string)
    				{
    					text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>>\"", instruction2.Operand), "\"<<>>"));
    				}
    				else if (instruction2.Operand is Instruction)
    				{
    					text = LanguageWriter.FormatIlLabel(((Instruction)instruction2.Operand).Offset);
    				}
    				else if (instruction2.Operand is TypeReference)
    				{
    					text = GetTypeName((TypeReference)instruction2.Operand);
    				}
    				else if (instruction2.Operand is MethodReference)
    				{
    					text = "";
    					if (((MethodReference)instruction2.Operand).HasThis)
    					{
    						text += "<<b>>instance<<>> ";
    					}
    					if (((MethodReference)instruction2.Operand).ReturnType != null)
    					{
    						text = text + _003C(((MethodReference)instruction2.Operand).ReturnType) + " ";
    					}
    					text = text + GetTypeName(((MethodReference)instruction2.Operand).DeclaringType) + "::" + LanguageWriter.GetReferenceKey((MethodReference)instruction2.Operand) + ((MethodReference)instruction2.Operand).Name + "<<>>";
    					text += "(";
    					foreach (ParameterDefinition parameter2 in ((MethodReference)instruction2.Operand).Parameters)
    					{
    						text = text + _003C(parameter2.ParameterType) + ",";
    					}
    					text = text.TrimEnd(',');
    					text += ")";
    				}
    				else if (instruction2.Operand is VariableDefinition)
    				{
    					text = ((VariableDefinition)instruction2.Operand).Name;
    				}
    				else if (instruction2.Operand is FieldReference)
    				{
    					text = GetTypeName(((FieldReference)instruction2.Operand).DeclaringType) + "::" + LanguageWriter.GetReferenceKey((FieldReference)instruction2.Operand) + ((FieldReference)instruction2.Operand).Name + "<<>>";
    				}
    				else if (instruction2.Operand is Instruction[])
    				{
    					Instruction[] array = (Instruction[])instruction2.Operand;
    					foreach (Instruction instruction in array)
    					{
    						text = text + LanguageWriter.FormatIlLabel(instruction.Offset) + ", ";
    					}
    					text.Remove(text.Length - 2, 2);
    				}
    				else
    				{
    					text = ((!(instruction2.Operand is ParameterDefinition)) ? Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>>", instruction2.Operand), "<<>>")) : ("<<r>>" + ((ParameterDefinition)instruction2.Operand).Name + "<<>>"));
    				}
    				stringBuilder.AppendLine(string.Format("    {0}: <<b>>{1}<<>> {2}", LanguageWriter.FormatIlLabel(instruction2.Offset).Substring(5).Replace("<<>>", ""), instruction2.OpCode, text));
    			}
    			foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
    			{
    				stringBuilder.Append("    <<b>>.try<<>> ");
    				stringBuilder.Append(LanguageWriter.FormatIlLabel(exceptionHandler.TryStart.Offset));
    				stringBuilder.Append(" <<b>>to<<>> ");
    				stringBuilder.Append(LanguageWriter.FormatIlLabel(exceptionHandler.TryEnd.Offset));
    				switch (exceptionHandler.HandlerType)
    				{
    				default:
    					stringBuilder.Append(" <<b>>catch<<>> ");
    					if (exceptionHandler.CatchType != null)
    					{
    						stringBuilder.Append(GetTypeName(exceptionHandler.CatchType) + " ");
    					}
    					stringBuilder.Append("<<b>>handler<<>> ");
    					break;
    				case ExceptionHandlerType.Filter:
    					stringBuilder.Append(" <<b>>filter<<>> " + LanguageWriter.FormatIlLabel(exceptionHandler.FilterStart.Offset) + " <<b>>handler<<>> ");
    					break;
    				case ExceptionHandlerType.Finally:
    					stringBuilder.Append(" <<b>>finally handler<<>> ");
    					break;
    				}
    				stringBuilder.Append(LanguageWriter.FormatIlLabel(exceptionHandler.HandlerStart.Offset));
    				stringBuilder.Append(" <<b>>to<<>> ");
    				stringBuilder.Append(LanguageWriter.FormatIlLabel(exceptionHandler.HandlerEnd.Offset));
    				stringBuilder.AppendLine();
    			}
    			stringBuilder.Append("}");
    			return stringBuilder.ToString();
    		}
    	}

    	public override string WriteProperty(PropertyDefinition propertyDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>");
    		stringBuilder.Append(".property ");
    		if (propertyDefinition_0.IsSpecialName)
    		{
    			stringBuilder.Append("specialname ");
    		}
    		if (propertyDefinition_0.IsRuntimeSpecialName)
    		{
    			stringBuilder.Append("rtspecialname ");
    		}
    		if (propertyDefinition_0.HasDefault)
    		{
    			stringBuilder.Append("hasdefault ");
    		}
    		if (propertyDefinition_0.GetMethod != null && propertyDefinition_0.GetMethod.IsStatic)
    		{
    			stringBuilder.Append("class ");
    		}
    		else if (propertyDefinition_0.SetMethod != null && propertyDefinition_0.SetMethod.IsStatic)
    		{
    			stringBuilder.Append("class ");
    		}
    		else
    		{
    			stringBuilder.Append("instance ");
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(_003C(propertyDefinition_0.PropertyType) + " ");
    		stringBuilder.AppendLine(LanguageWriter.GetReferenceKey(propertyDefinition_0) + propertyDefinition_0.Name + "<<>>");
    		stringBuilder.AppendLine("{");
    		checked
    		{
    			if (propertyDefinition_0.GetMethod != null)
    			{
    				MethodDefinition getMethod = propertyDefinition_0.GetMethod;
    				stringBuilder.Append("    <<b>>.get<<>> ");
    				if (!getMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(getMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(getMethod.DeclaringType), LanguageWriter.GetReferenceKey(getMethod) + getMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (getMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter in getMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter.ParameterType)}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			if (propertyDefinition_0.SetMethod != null)
    			{
    				MethodDefinition setMethod = propertyDefinition_0.SetMethod;
    				stringBuilder.Append("    <<b>>.set<<>> ");
    				if (!setMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(setMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(setMethod.DeclaringType), LanguageWriter.GetReferenceKey(setMethod) + setMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (setMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter2 in setMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter2.ParameterType)}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			foreach (MethodDefinition otherMethod in propertyDefinition_0.OtherMethods)
    			{
    				stringBuilder.Append("    <<b>>.method<<>> ");
    				if (!otherMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(otherMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(otherMethod.DeclaringType), LanguageWriter.GetReferenceKey(otherMethod) + otherMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (otherMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter3 in otherMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter3.ParameterType)}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			WriteCustomAttributes(stringBuilder, propertyDefinition_0.CustomAttributes);
    			stringBuilder.Append("}");
    			return stringBuilder.ToString();
    		}
    	}

    	public override string WriteField(FieldDefinition fieldDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>");
    		stringBuilder.Append(".field ");
    		if (fieldDefinition_0.IsPrivate)
    		{
    			stringBuilder.Append("private ");
    		}
    		else if (fieldDefinition_0.IsFamilyAndAssembly)
    		{
    			stringBuilder.Append("famandassem ");
    		}
    		else if (fieldDefinition_0.IsAssembly)
    		{
    			stringBuilder.Append("assembly ");
    		}
    		else if (fieldDefinition_0.IsFamily)
    		{
    			stringBuilder.Append("family ");
    		}
    		else if (fieldDefinition_0.IsFamilyOrAssembly)
    		{
    			stringBuilder.Append("famorassem ");
    		}
    		else if (fieldDefinition_0.IsPublic)
    		{
    			stringBuilder.Append("public ");
    		}
    		if (fieldDefinition_0.IsStatic)
    		{
    			stringBuilder.Append("static ");
    		}
    		if (fieldDefinition_0.IsLiteral)
    		{
    			stringBuilder.Append("literal ");
    		}
    		if (fieldDefinition_0.IsSpecialName)
    		{
    			stringBuilder.Append("specialname ");
    		}
    		if (fieldDefinition_0.IsRuntimeSpecialName)
    		{
    			stringBuilder.Append("rtspecialname ");
    		}
    		if (fieldDefinition_0.IsInitOnly)
    		{
    			stringBuilder.Append("initonly ");
    		}
    		if (fieldDefinition_0.IsNotSerialized)
    		{
    			stringBuilder.Append("notserialized ");
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(_003C(fieldDefinition_0.FieldType) + " ");
    		stringBuilder.Append(LanguageWriter.GetReferenceKey(fieldDefinition_0) + fieldDefinition_0.Name + "<<>>");
    		if (fieldDefinition_0.Constant != null)
    		{
    			stringBuilder.Append(" = ");
    			if (fieldDefinition_0.Constant is string)
    			{
    				stringBuilder.Append($"{_003C(fieldDefinition_0.FieldType)}(<<r>>'{RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)}'<<>>)");
    			}
    			else
    			{
    				stringBuilder.Append($"{_003C(fieldDefinition_0.FieldType)}(<<r>>{RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)}<<>>)");
    			}
    		}
    		if (fieldDefinition_0.CustomAttributes.Count > 0)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("{");
    			WriteCustomAttributes(stringBuilder, fieldDefinition_0.CustomAttributes);
    			stringBuilder.Append("}");
    		}
    		return stringBuilder.ToString();
    	}

    	public override string WriteEvent(EventDefinition eventDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.Append("<<b>>");
    		stringBuilder.Append(".event ");
    		if (eventDefinition_0.IsSpecialName)
    		{
    			stringBuilder.Append("specialname ");
    		}
    		if (eventDefinition_0.IsRuntimeSpecialName)
    		{
    			stringBuilder.Append("rtspecialname ");
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(GetTypeName(eventDefinition_0.EventType) + " ");
    		stringBuilder.AppendLine(LanguageWriter.GetReferenceKey(eventDefinition_0) + eventDefinition_0.Name + "<<>>");
    		stringBuilder.AppendLine("{");
    		checked
    		{
    			if (eventDefinition_0.AddMethod != null)
    			{
    				MethodDefinition addMethod = eventDefinition_0.AddMethod;
    				stringBuilder.Append("    <<b>>.addon<<>> ");
    				if (!addMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(addMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(addMethod.DeclaringType), LanguageWriter.GetReferenceKey(addMethod) + addMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (addMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter in addMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter.ParameterType)} {parameter.Name}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			if (eventDefinition_0.RemoveMethod != null)
    			{
    				MethodDefinition removeMethod = eventDefinition_0.RemoveMethod;
    				stringBuilder.Append("    <<b>>.removeon<<>> ");
    				if (!removeMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(removeMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(removeMethod.DeclaringType), LanguageWriter.GetReferenceKey(removeMethod) + removeMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (removeMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter2 in removeMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter2.ParameterType)} {parameter2.Name}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			if (eventDefinition_0.InvokeMethod != null)
    			{
    				MethodDefinition invokeMethod = eventDefinition_0.InvokeMethod;
    				stringBuilder.Append("    <<b>>.invokeon<<>> ");
    				if (!invokeMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(invokeMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(invokeMethod.DeclaringType), LanguageWriter.GetReferenceKey(invokeMethod) + invokeMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (invokeMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter3 in invokeMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter3.ParameterType)} {parameter3.Name}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			foreach (MethodDefinition otherMethod in eventDefinition_0.OtherMethods)
    			{
    				stringBuilder.Append("    <<b>>.method<<>> ");
    				if (!otherMethod.IsStatic)
    				{
    					stringBuilder.Append("<<b>>instance<<>> ");
    				}
    				stringBuilder.Append(_003C(otherMethod.ReturnType) + " ");
    				stringBuilder.Append(string.Format("{0}::{1}", GetTypeName(otherMethod.DeclaringType), LanguageWriter.GetReferenceKey(otherMethod) + otherMethod.Name + "<<>>"));
    				stringBuilder.Append("(");
    				if (otherMethod.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter4 in otherMethod.Parameters)
    					{
    						stringBuilder.Append($"{_003C(parameter4.ParameterType)} {parameter4.Name}, ");
    					}
    					stringBuilder.Remove(stringBuilder.Length - 2, 2);
    				}
    				stringBuilder.AppendLine(")");
    			}
    			WriteCustomAttributes(stringBuilder, eventDefinition_0.CustomAttributes);
    			stringBuilder.Append("}");
    			return stringBuilder.ToString();
    		}
    	}

    	private void WriteCustomAttributes(StringBuilder stringBuilder_0, Collection<CustomAttribute> collection_0)
    	{
    		checked
    		{
    			foreach (CustomAttribute item in collection_0)
    			{
    				stringBuilder_0.Append("    <<b>>.custom instance void<<>> " + GetTypeName(item.AttributeType) + "::.ctor(");
    				if (item.Constructor.Parameters.Count > 0)
    				{
    					foreach (ParameterDefinition parameter in item.Constructor.Parameters)
    					{
    						stringBuilder_0.Append(_003C(parameter.ParameterType) + ", ");
    					}
    					stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
    				}
    				stringBuilder_0.Append(")");
    				if (item.ConstructorArguments.Count > 0)
    				{
    					stringBuilder_0.Append(" = { ");
    					foreach (CustomAttributeArgument constructorArgument in item.ConstructorArguments)
    					{
    						if (constructorArgument.Value is string)
    						{
    							stringBuilder_0.Append($"{_003C(constructorArgument.Type)}(<<r>>'{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}'<<>>), ");
    						}
    						else
    						{
    							stringBuilder_0.Append($"{_003C(constructorArgument.Type)}(<<r>>{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}<<>>), ");
    						}
    					}
    					if (item.ConstructorArguments.Count > 0)
    					{
    						stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
    					}
    					stringBuilder_0.Append(" }");
    				}
    				stringBuilder_0.AppendLine();
    			}
    		}
    	}

    	private string _003C(TypeReference typeReference_0)
    	{
    		if (typeReference_0.IsGenericParameter)
    		{
    			return typeReference_0.Name;
    		}
    		string name = typeReference_0.Name;
    		string text = "";
    		string text2 = "";
    		if (typeReference_0.IsArray)
    		{
    			text = "[]";
    		}
    		if (typeReference_0.IsPointer)
    		{
    			text2 = "*";
    		}
    		if (name.EndsWith("&"))
    		{
    			text2 = "&";
    		}
    		typeReference_0 = typeReference_0.GetElementType();
    		if (typeReference_0 == null)
    		{
    			name = "<<b>>void<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Void", TextCompare: false) == 0)
    		{
    			name = "<<b>>void<<>>";
    		}
    		else if (typeReference_0.IsArray)
    		{
    			name = "<<b>>array<<>> " + GetTypeName(typeReference_0);
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "String", TextCompare: false) == 0)
    		{
    			name = "<<b>>string<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Char", TextCompare: false) == 0)
    		{
    			name = "<<b>>char<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Boolean", TextCompare: false) == 0)
    		{
    			name = "<<b>>bool<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Object", TextCompare: false) == 0)
    		{
    			name = "<<b>>object<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Int16", TextCompare: false) == 0)
    		{
    			name = "<<b>>int16<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Int32", TextCompare: false) == 0)
    		{
    			name = "<<b>>int32<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Int64", TextCompare: false) == 0)
    		{
    			name = "<<b>>int64<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "UInt16", TextCompare: false) == 0)
    		{
    			name = "<<b>>uint16<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "UInt32", TextCompare: false) == 0)
    		{
    			name = "<<b>>uint32<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "UInt64", TextCompare: false) == 0)
    		{
    			name = "<<b>>uint64<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Single", TextCompare: false) == 0)
    		{
    			name = "<<b>>float32<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Double", TextCompare: false) == 0)
    		{
    			name = "<<b>>float64<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "Byte", TextCompare: false) == 0)
    		{
    			name = "<<b>>uint8<<>>";
    		}
    		else if (Operators.CompareString(typeReference_0.Name, "SByte", TextCompare: false) == 0)
    		{
    			name = "<<b>>int8<<>>";
    		}
    		else
    		{
    			if (Operators.CompareString(typeReference_0.Name, "Type", TextCompare: false) != 0)
    			{
    				if (typeReference_0.IsValueType)
    				{
    					return "<<b>>valuetype<<>> " + GetTypeName(typeReference_0) + text + text2;
    				}
    				return "<<b>>class<<>> " + GetTypeName(typeReference_0) + text + text2;
    			}
    			name = "<<b>>type<<>>";
    		}
    		return LanguageWriter.GetReferenceKey(typeReference_0) + name + "<<>>" + text + text2;
    	}

    	private string GetTypeName(TypeReference typeReference_0)
    	{
    		return LanguageWriter.GetReferenceKey(LanguageWriter.GetAssemblyName(typeReference_0.Scope)) + "[" + LanguageWriter.GetAssemblyName(typeReference_0.Scope).Name + "]<<>>" + LanguageWriter.GetReferenceKey(typeReference_0) + typeReference_0.Namespace + "." + LanguageWriter.GetShortTypeName(typeReference_0) + "<<>>";
    	}

    	public override string ToString()
    	{
    		return "IL";
    	}
    }

}
